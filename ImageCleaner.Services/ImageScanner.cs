using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImageCleaner.Models;

namespace ImageCleaner.Services;

public class ImageScanner
{
	private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".gif", ".webp" };

	public ScanResult Scan(MonitorFolderProfile profile)
	{
		Stopwatch sw = Stopwatch.StartNew();
		List<CleanupTarget> files = new List<CleanupTarget>();
		if (string.IsNullOrWhiteSpace(profile.Path) || !Directory.Exists(profile.Path))
		{
			sw.Stop();
			return new ScanResult
			{
				Items = files,
				ElapsedMs = sw.ElapsedMilliseconds
			};
		}
		try
		{
			SearchOption option = (profile.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			foreach (string file in Directory.EnumerateFiles(profile.Path, "*.*", option))
			{
				string ext = Path.GetExtension(file);
				if (ImageExtensions.Contains(ext))
				{
					try
					{
						FileInfo fi = new FileInfo(file);
						files.Add(new CleanupTarget
						{
							FullPath = fi.FullName,
							CreationTimeUtc = fi.CreationTimeUtc
						});
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex)
		{
			LogService.Instance.Warn("扫描路径失败 [" + profile.Path + "]: " + ex.Message);
		}
		sw.Stop();
		return new ScanResult
		{
			Items = files.OrderBy((CleanupTarget f) => f.CreationTimeUtc).ToList(),
			ElapsedMs = sw.ElapsedMilliseconds
		};
	}
}
