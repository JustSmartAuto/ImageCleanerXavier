using System;
using System.IO;

namespace ImageCleaner.Services;

public class LogService
{
	private readonly object _lock = new object();

	private readonly string _logDirectory;

	public static LogService Instance { get; } = new LogService();

	private LogService()
	{
		try
		{
			AppPaths.Initialize();
			_logDirectory = AppPaths.LogDirectory;
			if (!string.IsNullOrEmpty(_logDirectory) && !Directory.Exists(_logDirectory))
			{
				Directory.CreateDirectory(_logDirectory);
			}
		}
		catch
		{
			_logDirectory = null;
		}
	}

	public void Info(string message)
	{
		Write("INFO", message);
	}

	public void Warn(string message)
	{
		Write("WARN", message);
	}

	public void Error(string message)
	{
		Write("ERROR", message);
	}

	public void Delete(string folderPath, string filePath)
	{
		Write("DELETE", folderPath + " | " + filePath);
	}

	private void Write(string level, string message)
	{
		if (string.IsNullOrEmpty(_logDirectory))
		{
			return;
		}
		string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
		lock (_lock)
		{
			try
			{
				File.AppendAllText(Path.Combine(_logDirectory, $"ImageCleaner_{DateTime.Now:yyyyMMdd}.log"), line + Environment.NewLine);
			}
			catch
			{
			}
		}
	}
}
