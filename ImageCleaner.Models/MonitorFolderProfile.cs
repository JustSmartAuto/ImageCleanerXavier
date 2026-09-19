using System;

namespace ImageCleaner.Models;

public class MonitorFolderProfile
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string Path { get; set; } = string.Empty;

	public bool Enabled { get; set; } = true;

	public int MonitorIntervalSeconds { get; set; } = 2;

	public bool IncludeSubdirectories { get; set; } = true;

	public DeleteConditions DeleteConditions { get; set; } = new DeleteConditions();

	public MonitorFolderProfile Clone()
	{
		return new MonitorFolderProfile
		{
			Id = Id,
			Path = Path,
			Enabled = Enabled,
			MonitorIntervalSeconds = MonitorIntervalSeconds,
			IncludeSubdirectories = IncludeSubdirectories,
			DeleteConditions = (DeleteConditions ?? new DeleteConditions()).Clone()
		};
	}
}
