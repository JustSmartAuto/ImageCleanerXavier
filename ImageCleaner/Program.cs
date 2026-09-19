using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageCleaner.Localization;
using ImageCleaner.Services;

namespace ImageCleaner;

internal static class Program
{
    private const string MutexName = "ImageCleaner.SingleInstance.Mutex";

    [STAThread]
    private static void Main()
    {
        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += (s, args) => args.SetObserved();

            LocalizationManager.Initialize("zh-CN");
            AppPaths.Initialize();
            LogService.Instance.Info("启动: ImageCleaner (.NET 8)");

            using Mutex mutex = CreateMutex(out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(LocalizationManager.Get("AlreadyRunning"), LocalizationManager.Get("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            Application.Run(new TrayApplicationContext(IsShellTrayAvailable()));
        }
        catch (Exception ex)
        {
            try
            {
                LogService.Instance.Error("启动失败: " + ex);
            }
            catch
            {
            }
            ShowStartupError(BuildStartupError(ex));
        }
    }

    private static Mutex CreateMutex(out bool createdNew)
    {
        try
        {
            return new Mutex(initiallyOwned: true, MutexName, out createdNew);
        }
        catch (AbandonedMutexException)
        {
            createdNew = true;
            return null;
        }
        catch (Exception ex)
        {
            LogService.Instance.Warn("创建互斥锁失败，继续启动: " + ex.Message);
            createdNew = true;
            return null;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    private static bool IsShellTrayAvailable()
    {
        try
        {
            return FindWindow("Shell_TrayWnd", null) != IntPtr.Zero;
        }
        catch
        {
            return true;
        }
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        try
        {
            LogService.Instance.Error("UI 异常: " + e.Exception);
        }
        catch
        {
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            LogService.Instance.Error("未处理异常: " + e.ExceptionObject);
        }
        catch
        {
        }
    }

    private static string BuildStartupError(Exception ex)
    {
        string os = "未知";
        try
        {
            os = Environment.OSVersion.ToString();
        }
        catch
        {
        }
        return "程序启动失败。" + Environment.NewLine + Environment.NewLine + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + Environment.NewLine + "当前系统: " + os + Environment.NewLine + "详细信息已写入 logs 目录。若刚升级过程序，请关闭旧进程后重新打开。";
    }

    private static void ShowStartupError(string message)
    {
        try
        {
            MessageBox.Show(message, "图片清理 - 无法启动", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        catch
        {
        }
    }
}
