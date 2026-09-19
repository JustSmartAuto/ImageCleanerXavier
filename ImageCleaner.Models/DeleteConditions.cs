namespace ImageCleaner.Models;

public class DeleteConditions
{
	public int StorageTimeValue { get; set; } = 20;

	public TimeUnit StorageTimeUnit { get; set; }

	public bool ImageCountEnabled { get; set; }

	public int ImageCountThreshold { get; set; } = 3000;

	public bool DiskSpaceEnabled { get; set; }

	public double DiskSpaceThresholdGb { get; set; } = 8.0;

	public double StorageTimeSeconds => StorageTimeUnit switch
	{
		TimeUnit.Minutes => (double)StorageTimeValue * 60.0, 
		TimeUnit.Hours => (double)StorageTimeValue * 3600.0, 
		TimeUnit.Days => (double)StorageTimeValue * 86400.0, 
		_ => StorageTimeValue, 
	};

	public DeleteConditions Clone()
	{
		return new DeleteConditions
		{
			StorageTimeValue = StorageTimeValue,
			StorageTimeUnit = StorageTimeUnit,
			ImageCountEnabled = ImageCountEnabled,
			ImageCountThreshold = ImageCountThreshold,
			DiskSpaceEnabled = DiskSpaceEnabled,
			DiskSpaceThresholdGb = DiskSpaceThresholdGb
		};
	}

	public void CopyFrom(DeleteConditions other)
	{
		if (other != null)
		{
			StorageTimeValue = other.StorageTimeValue;
			StorageTimeUnit = other.StorageTimeUnit;
			ImageCountEnabled = other.ImageCountEnabled;
			ImageCountThreshold = other.ImageCountThreshold;
			DiskSpaceEnabled = other.DiskSpaceEnabled;
			DiskSpaceThresholdGb = other.DiskSpaceThresholdGb;
		}
	}
}
