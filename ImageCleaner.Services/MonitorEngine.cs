using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImageCleaner.Models;

namespace ImageCleaner.Services;

public class MonitorEngine : IDisposable
{
	private readonly ImageScanner _scanner = new ImageScanner();

	private readonly FolderScanner _folderScanner = new FolderScanner();

	private readonly DeleteConditionEvaluator _evaluator = new DeleteConditionEvaluator();

	private readonly DiskInfoService _diskInfoService = new DiskInfoService();

	private readonly Dictionary<Guid, FolderWorker> _workers = new Dictionary<Guid, FolderWorker>();

	private Timer _scheduler;

	private AppConfig _config;

	private bool _isMonitoring;

	private readonly object _sync = new object();

	public bool IsMonitoring => _isMonitoring;

	public event EventHandler StatsUpdated;

	public void Start(AppConfig config)
	{
		lock (_sync)
		{
			_config = config;
			SyncWorkers();
			_isMonitoring = true;
			if (_scheduler == null)
			{
				_scheduler = new Timer(OnSchedulerTick, null, 0, 500);
			}
		}
		LogService.Instance.Info((_config.CleanupMode == CleanupMode.Folder) ? ("监控已启动（文件清理，前缀 " + _config.GetFolderPrefix() + "）") : "监控已启动（图片清理）");
	}

	public void Stop()
	{
		lock (_sync)
		{
			_isMonitoring = false;
		}
		LogService.Instance.Info("监控已停止");
	}

	public void UpdateConfig(AppConfig config)
	{
		lock (_sync)
		{
			_config = config;
			SyncWorkers();
		}
	}

	public MonitorStats GetStats(Guid folderId)
	{
		lock (_sync)
		{
			if (_workers.TryGetValue(folderId, out var worker))
			{
				return worker.LastStats;
			}
		}
		return new MonitorStats
		{
			FolderId = folderId
		};
	}

	public GlobalMonitorStats GetGlobalStats()
	{
		lock (_sync)
		{
			GlobalMonitorStats stats = new GlobalMonitorStats
			{
				IsMonitoring = _isMonitoring,
				TotalFolders = (_config?.Folders?.Count).GetValueOrDefault(),
				EnabledFolders = (_config?.Folders?.Count((MonitorFolderProfile f) => f.Enabled)).GetValueOrDefault()
			};
			foreach (FolderWorker worker in _workers.Values)
			{
				stats.TotalImageCount += worker.LastStats.ImageCount;
			}
			return stats;
		}
	}

	public IEnumerable<MonitorStats> GetAllStats()
	{
		lock (_sync)
		{
			return _workers.Values.Select((FolderWorker w) => w.LastStats).ToList();
		}
	}

	private void SyncWorkers()
	{
		if (_config?.Folders == null)
		{
			return;
		}
		HashSet<Guid> activeIds = new HashSet<Guid>(from f in _config.Folders
			where f.Enabled
			select f.Id);
		foreach (Guid id in _workers.Keys.ToList())
		{
			if (!activeIds.Contains(id))
			{
				_workers.Remove(id);
			}
		}
		CleanupMode mode = _config.CleanupMode;
		string prefix = _config.GetFolderPrefix();
		foreach (MonitorFolderProfile folder in _config.Folders.Where((MonitorFolderProfile f) => f.Enabled))
		{
			DeleteConditions effective = _config.GetEffectiveDeleteConditions(folder);
			if (_workers.TryGetValue(folder.Id, out var worker))
			{
				worker.UpdateProfile(folder, effective, mode, prefix);
			}
			else
			{
				_workers[folder.Id] = new FolderWorker(folder, _scanner, _folderScanner, _evaluator, _diskInfoService, effective, mode, prefix);
			}
		}
	}

	private void OnSchedulerTick(object state)
	{
		if (!_isMonitoring)
		{
			return;
		}
		List<FolderWorker> dueWorkers;
		lock (_sync)
		{
			if (_config?.Folders == null)
			{
				return;
			}
			DateTime now = DateTime.UtcNow;
			dueWorkers = _workers.Values.Where(delegate(FolderWorker w)
			{
				int num = Math.Max(1, w.Profile.MonitorIntervalSeconds);
				return (now - w.LastRunUtc).TotalSeconds >= (double)num;
			}).ToList();
		}
		if (dueWorkers.Count == 0)
		{
			return;
		}
		foreach (FolderWorker worker in dueWorkers)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					worker.TryRun();
					this.StatsUpdated?.Invoke(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					LogService.Instance.Error("监控任务异常: " + ex);
				}
			});
		}
	}

	public void Dispose()
	{
		_scheduler?.Dispose();
	}
}
