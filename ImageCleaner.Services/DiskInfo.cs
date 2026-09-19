namespace ImageCleaner.Services;

public class DiskInfo
{
	public string DriveLetter { get; set; } = string.Empty;

	public double FreeSpaceGb { get; set; }

	public double TotalSpaceGb { get; set; }

	public bool IsValid { get; set; }
}
