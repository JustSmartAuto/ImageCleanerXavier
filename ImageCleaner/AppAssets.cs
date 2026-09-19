using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ImageCleaner;

/// <summary>
/// 应用资源：内嵌 cleaner.png 徽标与程序图标。
/// </summary>
public static class AppAssets
{
    private static Image _logo;

    public static Image Logo
    {
        get
        {
            if (_logo == null)
            {
                try
                {
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("cleaner.png");
                    _logo = stream != null ? Image.FromStream(stream) : SystemIcons.Application.ToBitmap();
                }
                catch
                {
                    _logo = SystemIcons.Application.ToBitmap();
                }
            }
            return _logo;
        }
    }

    public static Icon LoadAppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application; }
        catch { return SystemIcons.Application; }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
