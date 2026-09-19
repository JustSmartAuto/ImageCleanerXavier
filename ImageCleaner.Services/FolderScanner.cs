using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImageCleaner.Models;

namespace ImageCleaner.Services;

public class FolderScanner
{
	public ScanResult Scan(MonitorFolderProfile profile, string prefix)
	{
		Stopwatch sw = Stopwatch.StartNew();
		List<CleanupTarget> folders = new List<CleanupTarget>();
		string matchPrefix = (string.IsNullOrWhiteSpace(prefix) ? "MX" : prefix.Trim());
		if (string.IsNullOrWhiteSpace(profile.Path) || !Directory.Exists(profile.Path))
		{
			sw.Stop();
			return new ScanResult
			{
				Items = folders,
				ElapsedMs = sw.ElapsedMilliseconds
			};
		}
		try
		{
			string text = NormalizePath(profile.Path);
			Collect(text, text, matchPrefix, profile.IncludeSubdirectories, folders);
		}
		catch (Exception ex)
		{
			LogService.Instance.Warn("扫描文件夹失败 [" + profile.Path + "]: " + ex.Message);
		}
		sw.Stop();
		return new ScanResult
		{
			Items = (from f in folders
				orderby f.Depth descending, f.CreationTimeUtc
				select f).ToList(),
			ElapsedMs = sw.ElapsedMilliseconds
		};
	}

	private static void Collect(string currentPath, string rootPath, string prefix, bool recursive, List<CleanupTarget> results)
	{
		IEnumerable<string> dirs;
		try
		{
			dirs = Directory.EnumerateDirectories(currentPath);
		}
		catch (Exception ex)
		{
			LogService.Instance.Warn("扫描子目录失败 [" + currentPath + "]: " + ex.Message);
			return;
		}
		foreach (string dir in dirs)
		{
			try
			{
				string fullPath = NormalizePath(dir);
				if (!IsSamePath(fullPath, rootPath))
				{
					string name = Path.GetFileName(fullPath);
					if (!string.IsNullOrEmpty(name) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					{
						DirectoryInfo di = new DirectoryInfo(fullPath);
						results.Add(new CleanupTarget
						{
							FullPath = di.FullName,
							CreationTimeUtc = di.CreationTimeUtc,
							Depth = GetDepth(rootPath, fullPath)
						});
					}
					if (recursive)
					{
						Collect(fullPath, rootPath, prefix, recursive: true, results);
					}
				}
			}
			catch
			{
			}
		}
	}

	public static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}
		return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	public static bool IsSamePath(string a, string b)
	{
		return string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsSameOrUnder(string path, string parent)
	{
		string normalizedParent = NormalizePath(parent);
		string normalizedPath = NormalizePath(path);
		if (string.IsNullOrEmpty(normalizedParent) || string.IsNullOrEmpty(normalizedPath))
		{
			return false;
		}
		if (string.Equals(normalizedPath, normalizedParent, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string prefix = normalizedParent + Path.DirectorySeparatorChar;
		return normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
	}

	private static int GetDepth(string root, string fullPath)
	{
		string normalizedRoot = NormalizePath(root);
		string normalizedFull = NormalizePath(fullPath);
		if (normalizedFull.Length <= normalizedRoot.Length)
		{
			return 0;
		}
		string relative = normalizedFull.Substring(normalizedRoot.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		if (string.IsNullOrEmpty(relative))
		{
			return 0;
		}
		return relative.Split(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}, StringSplitOptions.RemoveEmptyEntries).Length;
	}
}
