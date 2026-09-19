using System;

namespace ImageCleaner.Services;

public class CleanupTarget
{
	public string FullPath { get; set; }

	public DateTime CreationTimeUtc { get; set; }

	public int Depth { get; set; }
}
