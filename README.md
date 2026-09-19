# ImageCleaner 图片自动清理工具 / ImageCleaner — Machine Vision Image Auto-Cleanup

基于 .NET 8 的 WinForms 工控机机器视觉图片自动清理软件，AntdUI 界面（明暗主题、中英双语），搭配 .NET Framework 4.8 启动器，解决工控机无 .NET 8 运行时无法运行的问题。最终分发为**单个便携式 exe**（内嵌 .NET 8 运行时安装包 + 主程序），不易被杀毒软件误删 DLL 导致无法启动。

A .NET 8 WinForms auto-cleanup tool for machine-vision images on industrial PCs (HMI). Features an AntdUI interface (light/dark themes, Chinese/English), plus a .NET Framework 4.8 launcher for PCs without the .NET 8 runtime. Shipped as a **single portable exe** (with the .NET 8 runtime installer and the main app embedded), so antivirus DLL deletion won't break startup.

## 功能 / Features

- 按监控文件夹自动清理机器视觉图片（如相机保存的检测图像），支持两种清理模式：按图片（删除过期图片文件）或按文件夹（整目录清理）/ Auto-cleans machine-vision images per monitored folder; two cleanup modes: by image (delete expired image files) or by folder (whole-directory cleanup)
- 多文件夹监控配置：路径列表增删改、每文件夹独立删除条件，也可共享全局删除条件 / Multi-folder monitoring: add/edit/remove paths, per-folder or shared delete conditions
- 删除条件（可任意组合）/ Delete conditions (combinable):
  - 存储时间：超过 N 秒/分钟/小时/天的文件被删除 / Storage time: files older than N seconds/minutes/hours/days
  - 图片数量：目录内图片数超过阈值时触发清理 / Image count: triggers when the image count exceeds a threshold
  - 磁盘空间：磁盘剩余空间低于阈值（GB）时触发清理 / Disk space: triggers when free space drops below a threshold (GB)
- 可配置监控周期，运行时长限制（`MaxRunTime`），文件夹前缀过滤 / Configurable monitor interval, max run time, and folder prefix filter
- 实时信息面板：当前路径、耗时、磁盘占用、全局汇总，支持查看图片 / Live dashboard: current path, elapsed time, disk usage, global stats, image preview
- 删除条件支持自定义表达式脚本（`delete_conditions.js`）/ Custom expression script support for delete conditions (`delete_conditions.js`)
- 关闭最小化到系统托盘（右键 显示界面/退出），托盘图标运行时闪烁提示 / Minimize to tray on close; tray icon blinks while monitoring
- 开机自启、启动后自动开始监控 / Start with Windows and auto-start monitoring
- 中文/English 界面语言实时切换，明亮/暗黑主题 / Chinese/English UI with instant switching; light/dark themes
- 关于窗口：应用名、版本号、功能描述 / About window with logo, version, and description
- 配置 JSON 持久化；exe 旁放置 `portable.txt` 即启用便携模式（配置存 exe 旁），否则存 `%LOCALAPPDATA%\ImageCleaner\` / JSON config persistence; drop `portable.txt` next to the exe for portable mode (config beside the exe), otherwise config lives in `%LOCALAPPDATA%\ImageCleaner\`
- 日志输出到数据目录 `logs\` / Logs written to `logs\` under the data directory

## 技术栈 / Tech Stack

| 部分 / Part | 技术 / Technology |
|---|---|
| 主程序 `ImageCleaner/` | .NET 8 WinForms，纯 C# 实现，**AntdUI 2.4.10**（Ant Design 风格控件/无边框窗口/明暗主题），System.Text.Json |
| 启动器 `ImageCleaner.Launcher/` | .NET Framework 4.8（Win10 内置 .NET 4 即可运行），内嵌 .NET 8 运行时安装包与主程序 exe 资源 |
| UI 风格 / UI Style | AntdUI 控件 + VS Code 配色：暗黑 `#1e1e1e/#252526/#0e639c`，明亮 `#f0f0f0/#0078d4` |

启动器工作流程 / Launcher workflow：检测 `Microsoft.WindowsDesktop.App 8.x`（`dotnet --list-runtimes` + `C:\Program Files\dotnet` 双路探测）→ 缺失则静默安装内嵌的 windowsdesktop-runtime-8.0.29 → 每次覆盖释放主程序到 `%LOCALAPPDATA%\ImageCleaner\App\` 并启动。/ Probes for `Microsoft.WindowsDesktop.App 8.x` (via `dotnet --list-runtimes` and `C:\Program Files\dotnet`); silently installs the embedded windowsdesktop-runtime-8.0.29 if missing; extracts the main app to `%LOCALAPPDATA%\ImageCleaner\App\` on every launch and starts it.

## 构建 / Build

需要：.NET SDK 8+、MSBuild（Visual Studio 或 .NET Framework 4.8 自带）。/ Requires: .NET SDK 8+, MSBuild (Visual Studio or .NET Framework 4.8).

```bash
bash build-with-timestamp.sh
```

产物：`dist/ImageCleaner_<yyyyMMddHHmm>.exe`（约 62MB 单文件，直接拷贝到工控机运行）。/ Output: `dist/ImageCleaner_<yyyyMMddHHmm>.exe` (~62 MB single file — copy to the industrial PC and run).

## 部署注意事项 / Deployment Notes

- 主程序为框架依赖发布，首次在离线工控机上运行由启动器自动安装内嵌的 .NET 8 运行时，无需联网。/ The app is framework-dependent; the launcher installs the embedded .NET 8 runtime automatically on first run — no internet required.
- 建议在正式环境先用测试目录验证删除条件，避免误删。/ Test the delete conditions against a test folder before production use to avoid accidental deletion.
