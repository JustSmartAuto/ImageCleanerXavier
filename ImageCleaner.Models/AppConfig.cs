using System.Collections.Generic;

namespace ImageCleaner.Models;

public class AppConfig
{
	public string Language { get; set; } = "zh-CN";

	public string Theme { get; set; } = "light";

	public bool MinimizeToTrayOnClose { get; set; } = true;

	public bool RunInBackground { get; set; }

	public bool AutoStartMonitoring { get; set; } = true;

	public bool AutoStartWithWindows { get; set; }

	public List<MonitorFolderProfile> Folders { get; set; } = new List<MonitorFolderProfile>();

	public bool MaxRunTime { get; set; } = true;

	public bool UseSharedDeleteConditions { get; set; }

	public DeleteConditions SharedDeleteConditions { get; set; } = new DeleteConditions();

	public CleanupMode CleanupMode { get; set; }

	public string FolderPrefix { get; set; } = "MX";

	public string GetFolderPrefix()
	{
		if (!string.IsNullOrWhiteSpace(FolderPrefix))
		{
			return FolderPrefix.Trim();
		}
		return "MX";
	}

	public DeleteConditions GetEffectiveDeleteConditions(MonitorFolderProfile folder)
	{
		if (UseSharedDeleteConditions)
		{
			return SharedDeleteConditions ?? new DeleteConditions();
		}
		return folder?.DeleteConditions ?? new DeleteConditions();
	}
}
