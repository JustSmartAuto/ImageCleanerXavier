using System;
using System.IO;

namespace ImageCleaner.Services;

public static class AppPaths
{
	private static readonly object Sync = new object();

	private static bool _initialized;

	public static string ProgramDirectory { get; private set; }

	public static string DataDirectory { get; private set; }

	public static string LogDirectory { get; private set; }

	public static string ConfigPath { get; private set; }

	public static bool UsingFallbackDirectory { get; private set; }

	public static bool IsDataDirectoryWritable { get; private set; }

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		lock (Sync)
		{
			if (!_initialized)
			{
				ProgramDirectory = AppDomain.CurrentDomain.BaseDirectory;
				DataDirectory = ChooseWritableDirectory();
				IsDataDirectoryWritable = !string.IsNullOrEmpty(DataDirectory) && CanWriteTo(DataDirectory);
				UsingFallbackDirectory = IsDataDirectoryWritable && !PathsEqual(DataDirectory, ProgramDirectory);
				if (IsDataDirectoryWritable)
				{
					LogDirectory = Path.Combine(DataDirectory, "logs");
					TryCreateDirectory(LogDirectory);
					ConfigPath = Path.Combine(DataDirectory, "appsettings.json");
					SeedConfigFromProgramDirectory();
				}
				else
				{
					LogDirectory = null;
					ConfigPath = Path.Combine(ProgramDirectory ?? string.Empty, "appsettings.json");
				}
				_initialized = true;
			}
		}
	}

	private static string ChooseWritableDirectory()
	{
		string[] array = new string[3]
		{
			ProgramDirectory,
			CombineDir(GetFolder(Environment.SpecialFolder.LocalApplicationData), "ImageCleaner"),
			CombineDir(Path.GetTempPath(), "ImageCleaner")
		};
		foreach (string dir in array)
		{
			if (!string.IsNullOrWhiteSpace(dir) && CanWriteTo(dir))
			{
				return dir;
			}
		}
		return ProgramDirectory;
	}

	private static void SeedConfigFromProgramDirectory()
	{
		try
		{
			if (UsingFallbackDirectory && !string.IsNullOrEmpty(ConfigPath) && !File.Exists(ConfigPath))
			{
				string bundled = Path.Combine(ProgramDirectory, "appsettings.json");
				if (File.Exists(bundled))
				{
					File.Copy(bundled, ConfigPath, overwrite: false);
				}
			}
		}
		catch
		{
		}
	}

	public static bool CanWriteTo(string directory)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			return false;
		}
		string probe = null;
		try
		{
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			probe = Path.Combine(directory, ".imagecleaner_write_probe");
			File.WriteAllText(probe, "ok");
			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			if (probe != null)
			{
				try
				{
					File.Delete(probe);
				}
				catch
				{
				}
			}
		}
	}

	private static void TryCreateDirectory(string directory)
	{
		try
		{
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
		catch
		{
		}
	}

	private static string CombineDir(string parent, string child)
	{
		if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(child))
		{
			return null;
		}
		try
		{
			return Path.Combine(parent, child);
		}
		catch
		{
			return null;
		}
	}

	private static string GetFolder(Environment.SpecialFolder folder)
	{
		try
		{
			return Environment.GetFolderPath(folder);
		}
		catch
		{
			return null;
		}
	}

	private static bool PathsEqual(string a, string b)
	{
		if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
		{
			return false;
		}
		return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
	}
}
