using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ImageCleaner;

internal static class TrayIcons
{
	private static class NativeMethods
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool DestroyIcon(IntPtr handle);
	}

	public static Icon Create(Color color)
	{
		Bitmap bmp = new Bitmap(16, 16);
		using (Graphics g = Graphics.FromImage(bmp))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.Clear(Color.Transparent);
			using (SolidBrush brush = new SolidBrush(color))
			{
				g.FillEllipse(brush, 1, 1, 13, 13);
			}
			using Pen pen = new Pen(Color.FromArgb(50, 50, 50));
			g.DrawEllipse(pen, 1, 1, 13, 13);
		}
		IntPtr hicon = bmp.GetHicon();
		Icon icon = (Icon)Icon.FromHandle(hicon).Clone();
		NativeMethods.DestroyIcon(hicon);
		bmp.Dispose();
		return icon;
	}
}
