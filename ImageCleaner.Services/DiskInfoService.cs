using System;
using System.IO;

namespace ImageCleaner.Services;

public class DiskInfoService
{
	public DiskInfo GetDiskInfo(string folderPath)
	{
		DiskInfo result = new DiskInfo();
		if (string.IsNullOrWhiteSpace(folderPath))
		{
			return result;
		}
		try
		{
			string root = Path.GetPathRoot(folderPath);
			if (string.IsNullOrEmpty(root))
			{
				return result;
			}
			DriveInfo drive = new DriveInfo(root);
			if (!drive.IsReady)
			{
				return result;
			}
			result.DriveLetter = drive.Name.TrimEnd('\\');
			result.FreeSpaceGb = Math.Round((double)drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2);
			result.TotalSpaceGb = Math.Round((double)drive.TotalSize / 1024.0 / 1024.0 / 1024.0, 2);
			result.IsValid = true;
		}
		catch
		{
		}
		return result;
	}
}
