using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace ImageCleaner.Services;

public class AutoStartService
{
	private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string AppName = "ImageCleaner";

	public bool IsEnabled()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
			return key?.GetValue("ImageCleaner") != null;
		}
		catch
		{
			return false;
		}
	}

	public void SetEnabled(bool enabled)
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
			if (key != null)
			{
				if (enabled)
				{
					string exePath = Environment.ProcessPath;
					if (string.IsNullOrEmpty(exePath))
					{
						exePath = Assembly.GetExecutingAssembly().Location;
					}
					if (string.IsNullOrEmpty(exePath))
					{
						exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCleaner.exe");
					}
					key.SetValue("ImageCleaner", "\"" + exePath + "\"");
				}
				else
				{
					key.DeleteValue("ImageCleaner", throwOnMissingValue: false);
				}
			}
		}
		catch (Exception ex)
		{
			LogService.Instance.Error("设置开机自启失败: " + ex.Message);
		}
	}
}
