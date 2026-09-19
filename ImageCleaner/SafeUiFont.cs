using System;
using System.Drawing;

namespace ImageCleaner;

public static class SafeUiFont
{
	private static readonly string[] FamilyCandidates = new string[8] { "微软雅黑", "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Tahoma", "宋体", "SimSun", "Arial" };

	public static Font Regular(float sizeEm)
	{
		return Create(sizeEm, FontStyle.Regular);
	}

	public static Font Bold(float sizeEm)
	{
		return Create(sizeEm, FontStyle.Bold);
	}

	public static Font Create(float sizeEm, FontStyle style)
	{
		string[] familyCandidates = FamilyCandidates;
		foreach (string name in familyCandidates)
		{
			try
			{
				if (!FamilyInstalled(name))
				{
					continue;
				}
				return new Font(name, sizeEm, style, GraphicsUnit.Point);
			}
			catch
			{
			}
		}
		try
		{
			return new Font(FontFamily.GenericSansSerif, sizeEm, style, GraphicsUnit.Point);
		}
		catch
		{
			return SystemFonts.DefaultFont;
		}
	}

	private static bool FamilyInstalled(string name)
	{
		try
		{
			FontFamily[] families = FontFamily.Families;
			for (int i = 0; i < families.Length; i++)
			{
				if (string.Equals(families[i].Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}
}
