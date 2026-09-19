using System.Collections.Generic;

namespace ImageCleaner.Services;

public class ScanResult
{
	public List<CleanupTarget> Items { get; set; } = new List<CleanupTarget>();

	public long ElapsedMs { get; set; }
}
