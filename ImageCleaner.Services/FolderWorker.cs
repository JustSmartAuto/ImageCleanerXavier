using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ImageCleaner.Models;

namespace ImageCleaner.Services;

public class FolderWorker
{
	private readonly MonitorFolderProfile _profile;

	private readonly ImageScanner _scanner;

	private readonly FolderScanner _folderScanner;

	private readonly DeleteConditionEvaluator _evaluator;

	private readonly DiskInfoService _diskInfoService;

	private CleanupMode _cleanupMode;

	private string _folderPrefix;

	private int _isRunning;

	private MonitorStats _lastStats = new MonitorStats();

	public MonitorFolderProfile Profile => _profile;

	public DateTime LastRunUtc { get; set; } = DateTime.MinValue;

	public MonitorStats LastStats => _lastStats;

	public FolderWorker(MonitorFolderProfile profile, ImageScanner scanner, FolderScanner folderScanner, DeleteConditionEvaluator evaluator, DiskInfoService diskInfoService, DeleteConditions effectiveDeleteConditions, CleanupMode cleanupMode, string folderPrefix)
	{
		_scanner = scanner;
		_folderScanner = folderScanner;
		_evaluator = evaluator;
		_diskInfoService = diskInfoService;
		_profile = profile.Clone();
		ApplyEffectiveDeleteConditions(effectiveDeleteConditions);
		ApplyCleanupSettings(cleanupMode, folderPrefix);
		_lastStats.FolderId = profile.Id;
		_lastStats.Path = profile.Path;
	}

	public void UpdateProfile(MonitorFolderProfile profile, DeleteConditions effectiveDeleteConditions, CleanupMode cleanupMode, string folderPrefix)
	{
		_profile.Id = profile.Id;
		_profile.Path = profile.Path;
		_profile.Enabled = profile.Enabled;
		_profile.MonitorIntervalSeconds = profile.MonitorIntervalSeconds;
		_profile.IncludeSubdirectories = profile.IncludeSubdirectories;
		ApplyEffectiveDeleteConditions(effectiveDeleteConditions);
		ApplyCleanupSettings(cleanupMode, folderPrefix);
		_lastStats.Path = profile.Path;
	}

	private void ApplyEffectiveDeleteConditions(DeleteConditions effectiveDeleteConditions)
	{
		_profile.DeleteConditions = (effectiveDeleteConditions ?? new DeleteConditions()).Clone();
	}

	private void ApplyCleanupSettings(CleanupMode cleanupMode, string folderPrefix)
	{
		_cleanupMode = cleanupMode;
		_folderPrefix = (string.IsNullOrWhiteSpace(folderPrefix) ? "MX" : folderPrefix.Trim());
	}

	public bool TryRun()
	{
		if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
		{
			return false;
		}
		try
		{
			DateTime previousRun = LastRunUtc;
			RunInternal();
			LastRunUtc = DateTime.UtcNow;
			if (previousRun != DateTime.MinValue)
			{
				_lastStats.ActualIntervalMs = (long)(LastRunUtc - previousRun).TotalMilliseconds;
			}
			return true;
		}
		catch (Exception ex)
		{
			LogService.Instance.Error("路径任务失败 [" + _profile.Path + "]: " + ex.Message);
			return false;
		}
		finally
		{
			Interlocked.Exchange(ref _isRunning, 0);
		}
	}

	private void RunInternal()
	{
		if (_cleanupMode == CleanupMode.Folder)
		{
			RunFolderCleanup();
		}
		else
		{
			RunImageCleanup();
		}
	}

	private void RunImageCleanup()
	{
		ScanResult scan = _scanner.Scan(_profile);
		DiskInfo disk = _diskInfoService.GetDiskInfo(_profile.Path);
		int deleted = 0;
		List<CleanupTarget> items = new List<CleanupTarget>(scan.Items);
		if (_profile.Enabled && items.Count > 0)
		{
			deleted = DeleteExpiredImages(items, disk);
		}
		ApplyStats(items.Count, scan.ElapsedMs, deleted);
	}

	private void RunFolderCleanup()
	{
		ScanResult scan = _folderScanner.Scan(_profile, _folderPrefix);
		DiskInfo disk = _diskInfoService.GetDiskInfo(_profile.Path);
		int deleted = 0;
		List<CleanupTarget> items = new List<CleanupTarget>(scan.Items);
		if (_profile.Enabled && items.Count > 0)
		{
			deleted = DeleteExpiredFolders(items, disk);
		}
		ApplyStats(items.Count, scan.ElapsedMs, deleted);
	}

	private void ApplyStats(int remainingCount, long scanElapsedMs, int deleted)
	{
		DiskInfo disk = _diskInfoService.GetDiskInfo(_profile.Path);
		_lastStats = new MonitorStats
		{
			FolderId = _profile.Id,
			Path = _profile.Path,
			ImageCount = remainingCount,
			ScanElapsedMs = scanElapsedMs,
			LastScanUtc = DateTime.UtcNow,
			DriveLetter = disk.DriveLetter,
			FreeSpaceGb = disk.FreeSpaceGb,
			TotalSpaceGb = disk.TotalSpaceGb,
			DeletedCountLastRun = deleted
		};
	}

	private int DeleteExpiredImages(List<CleanupTarget> files, DiskInfo disk)
	{
		int deleted = 0;
		DeleteEvaluationResult evaluation = _evaluator.Evaluate(_profile, files, disk);
		if (!evaluation.AllowDelete)
		{
			return 0;
		}
		while (evaluation.AllowDelete && evaluation.ExpiredFiles.Count > 0)
		{
			CleanupTarget target = evaluation.ExpiredFiles[0];
			if (TryDeleteFile(target.FullPath))
			{
				files.Remove(target);
				deleted++;
				disk = _diskInfoService.GetDiskInfo(_profile.Path);
				evaluation = _evaluator.Evaluate(_profile, files, disk);
			}
			else
			{
				files.Remove(target);
				evaluation = _evaluator.Evaluate(_profile, files, disk);
			}
		}
		return deleted;
	}

	private int DeleteExpiredFolders(List<CleanupTarget> folders, DiskInfo disk)
	{
		int deleted = 0;
		DeleteEvaluationResult evaluation = EvaluateFolders(folders, disk);
		if (!evaluation.AllowDelete)
		{
			return 0;
		}
		while (evaluation.AllowDelete && evaluation.ExpiredFiles.Count > 0)
		{
			CleanupTarget target = evaluation.ExpiredFiles[0];
			if (TryDeleteFolder(target.FullPath))
			{
				folders.RemoveAll((CleanupTarget item) => FolderScanner.IsSameOrUnder(item.FullPath, target.FullPath));
				deleted++;
				disk = _diskInfoService.GetDiskInfo(_profile.Path);
				evaluation = EvaluateFolders(folders, disk);
			}
			else
			{
				folders.Remove(target);
				evaluation = EvaluateFolders(folders, disk);
			}
		}
		return deleted;
	}

	private DeleteEvaluationResult EvaluateFolders(List<CleanupTarget> folders, DiskInfo disk)
	{
		DeleteEvaluationResult deleteEvaluationResult = _evaluator.Evaluate(_profile, folders, disk);
		deleteEvaluationResult.ExpiredFiles.Sort(delegate(CleanupTarget a, CleanupTarget b)
		{
			int num = b.Depth.CompareTo(a.Depth);
			return (num != 0) ? num : a.CreationTimeUtc.CompareTo(b.CreationTimeUtc);
		});
		return deleteEvaluationResult;
	}

	private bool TryDeleteFile(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			File.Delete(path);
			LogService.Instance.Delete(_profile.Path, path);
			return true;
		}
		catch (Exception ex)
		{
			LogService.Instance.Warn("删除失败 [" + path + "]: " + ex.Message);
			return false;
		}
	}

	private bool TryDeleteFolder(string path)
	{
		try
		{
			if (FolderScanner.IsSamePath(path, _profile.Path))
			{
				LogService.Instance.Warn("跳过监控根目录，永不删除 [" + path + "]");
				return false;
			}
			if (!Directory.Exists(path))
			{
				return false;
			}
			Directory.Delete(path, recursive: true);
			LogService.Instance.Delete(_profile.Path, path);
			return true;
		}
		catch (Exception ex)
		{
			LogService.Instance.Warn("删除失败 [" + path + "]: " + ex.Message);
			return false;
		}
	}
}
