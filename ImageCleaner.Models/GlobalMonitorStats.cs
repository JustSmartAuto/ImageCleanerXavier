namespace ImageCleaner.Models;

public class GlobalMonitorStats
{
	public int TotalFolders { get; set; }

	public int EnabledFolders { get; set; }

	public int TotalImageCount { get; set; }

	public bool IsMonitoring { get; set; }
}
