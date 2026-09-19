using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using ImageCleaner.Models;

namespace ImageCleaner.Services;

public class ConfigService : IDisposable
{
	private readonly string _configPath;

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	private readonly object _saveLock = new object();

	private readonly Timer _saveTimer;

	private AppConfig _config;

	public AppConfig Config => _config;

	public event EventHandler ConfigChanged;

	public ConfigService()
	{
		AppPaths.Initialize();
		_configPath = (string.IsNullOrEmpty(AppPaths.ConfigPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json") : AppPaths.ConfigPath);
		_config = Load();
		_saveTimer = new Timer(delegate
		{
			SaveInternal();
		}, null, -1, -1);
	}

	public AppConfig Load()
	{
		try
		{
			if (File.Exists(_configPath))
			{
				string json = File.ReadAllText(_configPath);
				AppConfig loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
				if (loaded != null)
				{
					if (loaded.Folders == null)
					{
						loaded.Folders = new List<MonitorFolderProfile>();
					}
					if (loaded.SharedDeleteConditions == null)
					{
						loaded.SharedDeleteConditions = new DeleteConditions();
					}
					if (string.IsNullOrWhiteSpace(loaded.FolderPrefix))
					{
						loaded.FolderPrefix = "MX";
					}
					foreach (MonitorFolderProfile folder in loaded.Folders)
					{
						if (folder.DeleteConditions == null)
						{
							folder.DeleteConditions = new DeleteConditions();
						}
					}
					_config = loaded;
					return _config;
				}
			}
		}
		catch (Exception ex)
		{
			LogService.Instance.Warn("加载配置失败: " + ex.Message);
		}
		_config = CreateDefault();
		return _config;
	}

	public void ScheduleSave()
	{
		_saveTimer.Change(500, -1);
	}

	public void SaveNow()
	{
		_saveTimer.Change(-1, -1);
		SaveInternal();
	}

	private void SaveInternal()
	{
		lock (_saveLock)
		{
			try
			{
				string json = JsonSerializer.Serialize(_config, JsonOptions);
				File.WriteAllText(_configPath, json);
				this.ConfigChanged?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				LogService.Instance.Error("保存配置失败: " + ex.Message);
			}
		}
	}

	public void Dispose()
	{
		_saveTimer.Dispose();
	}

	private static AppConfig CreateDefault()
	{
		return new AppConfig
		{
			Language = "zh-CN",
			MinimizeToTrayOnClose = true,
			RunInBackground = false,
			AutoStartMonitoring = true,
			AutoStartWithWindows = false,
			UseSharedDeleteConditions = true,
			SharedDeleteConditions = new DeleteConditions(),
			CleanupMode = CleanupMode.Image,
			FolderPrefix = "MX",
			Folders = new List<MonitorFolderProfile>()
		};
	}
}
