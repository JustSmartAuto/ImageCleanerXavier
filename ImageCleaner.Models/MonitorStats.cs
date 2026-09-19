using System;

namespace ImageCleaner.Models;

public class MonitorStats
{
	public Guid FolderId { get; set; }

	public string Path { get; set; } = string.Empty;

	public int ImageCount { get; set; }

	public long ScanElapsedMs { get; set; }

	public DateTime LastScanUtc { get; set; }

	public string DriveLetter { get; set; } = string.Empty;

	public double FreeSpaceGb { get; set; }

	public double TotalSpaceGb { get; set; }

	public int DeletedCountLastRun { get; set; }

	public long ActualIntervalMs { get; set; }
}
