using System;
using System.Collections.Generic;

namespace ImageCleaner.Localization;

/// <summary>
/// 中英双语字典，风格同 CameraViewer.I18n。
/// </summary>
public static class I18n
{
    public static string Language = "zh";

    public static event Action LanguageChanged;

    private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
["CleanupModeLabel"] = new[] { "清理模式", "Cleanup Mode" },
["CleanupModeImage"] = new[] { "图片清理", "Image Cleanup" },
["FolderDeleteConditionsHint"] = new[] { "多条件 AND；保存时间以文件夹创建时间为准；仅删除指定前缀文件夹", "AND logic; storage time uses folder creation time; only prefixed folders" },
["DeleteConditionsHint"] = new[] { "多条件 AND；保存时间以文件创建时间为准", "AND logic; storage time uses file creation time" },
["ImagesUnit"] = new[] { "张", "images" },
["ImageCount"] = new[] { "图片数目 >", "Image Count >" },
["GbUnit"] = new[] { "GB", "GB" },
["SelectFolderHint"] = new[] { "请选择一个监控路径以编辑设置", "Select a monitor path to edit its settings" },
["NoFolderSelected"] = new[] { "请先选择一个监控路径", "Please select a monitor path first" },
["TrayStart"] = new[] { "开始监控", "Start Monitoring" },
["MsUnit"] = new[] { "ms", "ms" },
["InfoDisplay"] = new[] { "信息显示", "Information" },
["CannotOpenFolderDialog"] = new[] { "无法打开文件夹选择对话框。", "Cannot open the folder picker dialog." },
["Seconds"] = new[] { "秒", "sec" },
["CurrentFolder"] = new[] { "当前路径", "Current Path" },
["RemoveFolder"] = new[] { "删除选中", "Remove" },
["AutoStartMonitoring"] = new[] { "程序启动自动运行监控", "Auto Start Monitoring" },
["CannotOpenExplorer"] = new[] { "无法打开资源管理器。当前环境可能未运行 Explorer。", "Cannot open Explorer. The shell may not be running on this machine." },
["FolderCountLabel"] = new[] { "文件夹数目", "Folder Count" },
["UnitSeconds"] = new[] { "秒", "sec" },
["UnitDays"] = new[] { "天", "day" },
["FolderScanTime"] = new[] { "扫描耗时", "Scan Time" },
["AutoStartWindows"] = new[] { "开机自启", "Start with Windows" },
["ViewFolders"] = new[] { "查看文件夹", "View Folder" },
["AddFolder"] = new[] { "添加目录", "Add Folder" },
["MinimizeToTray"] = new[] { "关闭窗口时最小化至托盘", "Minimize to Tray on Close" },
["CleanupModeFolder"] = new[] { "文件清理", "File Cleanup" },
["ViewImages"] = new[] { "查看图片", "View Images" },
["IncludeSubdirs"] = new[] { "是否包含子目录", "Include Subdirectories" },
["DriveSuffix"] = new[] { "盘", "DriveSuffix" },
["UnitMinutes"] = new[] { "分", "min" },
["DeletedLastRun"] = new[] { "上次删除", "Deleted Last Run" },
["ConfirmTitle"] = new[] { "确认", "Confirm" },
["ConfirmRemove"] = new[] { "确定删除选中的监控路径吗？", "Remove selected monitor path?" },
["DuplicatePath"] = new[] { "该目录已在监控列表中", "This folder is already in the monitor list" },
["Chinese"] = new[] { "中文(简体)", "Chinese (Simplified)" },
["ToggleEnabled"] = new[] { "启用/禁用", "Enable/Disable" },
["TrayShow"] = new[] { "显示主窗口", "Show Window" },
["TrayStop"] = new[] { "停止监控", "Stop Monitoring" },
["TrayExit"] = new[] { "退出", "Exit" },
["MonitoredDisk"] = new[] { "被监控盘", "Monitored Drive" },
["MonitorBasicSettings"] = new[] { "监控基本设置", "Basic Monitor Settings" },
["English"] = new[] { "英文", "English" },
["Enabled"] = new[] { "已启用", "Enabled" },
["No"] = new[] { "否", "No" },
["AlreadyRunning"] = new[] { "图片清理已在运行。", "Image Cleaner is already running." },
["ActualCycle"] = new[] { "实际周期", "Actual Interval" },
["Yes"] = new[] { "是", "Yes" },
["TotalImages"] = new[] { "合计图片数", "Total Images" },
["StopMonitoring"] = new[] { "停止监控", "Stop Monitoring" },
["FoldersUnit"] = new[] { "个", "folders" },
["DiskFree"] = new[] { "磁盘余量", "Disk Free" },
["Disabled"] = new[] { "已禁用", "Disabled" },
["FolderCount"] = new[] { "文件夹数目 >", "Folder Count >" },
["ReadTime"] = new[] { "读图耗时", "Scan Time" },
["DeleteModeShared"] = new[] { "统一设置", "Shared" },
["LanguageSettings"] = new[] { "多语言设置", "Language" },
["RunSettings"] = new[] { "运行设置", "Run Settings" },
["DiskTotal"] = new[] { "磁盘总量", "Disk Total" },
["DiskSpace"] = new[] { "磁盘余量 <", "Disk Free <" },
["PathNotExist"] = new[] { "路径不存在", "Path does not exist" },
["StatusStopped"] = new[] { "已停止", "Stopped" },
["ImageCountLabel"] = new[] { "图片数目", "Image Count" },
["StatusRunning"] = new[] { "监控中", "Running" },
["AppTitle"] = new[] { "图片清理", "Image Cleaner" },
["FolderDeleteConditions"] = new[] { "删文件夹条件设置", "Folder Delete Conditions" },
["MonitorInterval"] = new[] { "监控周期", "Monitor Interval" },
["MonitorStatus"] = new[] { "监控状态", "Monitor Status" },
["TotalFolderItems"] = new[] { "合计文件夹数", "Total Folders" },
["RunInBackground"] = new[] { "后台运行", "Run in Background" },
["UnitHours"] = new[] { "时", "hour" },
["GlobalSummary"] = new[] { "全局汇总", "Global Summary" },
["StorageTime"] = new[] { "保存时间 >", "Storage Time >" },
["MonitorPaths"] = new[] { "监控路径", "Monitor Paths" },
["EnabledFolders"] = new[] { "启用路径数", "Enabled Paths" },
["FolderPrefix"] = new[] { "文件夹前缀", "Folder Prefix" },
["DeleteConditions"] = new[] { "删图条件设置", "Delete Conditions" },
["SelectFolder"] = new[] { "选择监控目录", "Select Monitor Folder" },
["DeleteModePerFolder"] = new[] { "单独设置", "Per folder" },
["TotalFolders"] = new[] { "监控路径数", "Total Paths" },
["StartMonitoring"] = new[] { "开始监控", "Start Monitoring" },
["aboutDesc"] = new[] { "工控环境图片自动清理工具，支持图片/文件夹两种清理模式、多条件删除策略、托盘后台运行与开机自启。", "Industrial PC image auto-cleanup tool with image/folder cleanup modes, multi-condition delete policies, tray background running and auto-start." },
["aboutTech"] = new[] { "技术栈：WinForms + AntdUI (.NET 8)", "Tech stack: WinForms + AntdUI (.NET 8)" },
["aboutClose"] = new[] { "关闭", "Close" },
["version"] = new[] { "版本号：", "Version:" },
["about"] = new[] { "关于", "About" },
["themeLight"] = new[] { "明亮", "Light" },
["themeDark"] = new[] { "暗黑", "Dark" },
["languageZh"] = new[] { "中文", "中文" },
["languageEn"] = new[] { "English", "English" },
["systemTime"] = new[] { "系统时间：", "System Time:" },
["enablePrefixEdit"] = new[] { "启用更改", "Enable Edit" },
    };

    public static string T(string key)
    {
        if (Map.TryGetValue(key, out var v))
            return Language == "en" ? v[1] : v[0];
        return key;
    }

    public static void SetLanguage(string lang)
    {
        if (lang != "en") lang = "zh";
        if (Language == lang) return;
        Language = lang;
        LanguageChanged?.Invoke();
    }
}
