using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AntdUI;
using ImageCleaner.Localization;
using ImageCleaner.Models;
using ImageCleaner.Services;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;
using Button = AntdUI.Button;
using Timer = System.Windows.Forms.Timer;

namespace ImageCleaner;

public class MainForm : AntdUI.Window
{
    private sealed class FolderListItem
    {
        public MonitorFolderProfile Profile { get; }

        public FolderListItem(MonitorFolderProfile profile)
        {
            Profile = profile;
        }

        public override string ToString()
        {
            string flag = Profile.Enabled ? L("Enabled") : L("Disabled");
            string path = string.IsNullOrWhiteSpace(Profile.Path) ? "-" : Profile.Path;
            return string.Format("[{0}] {1}  ({2}{3})", flag, path, Profile.MonitorIntervalSeconds, L("Seconds"));
        }
    }

    private sealed class ComboItem
    {
        public string Text { get; }
        public string Value { get; }

        public ComboItem(string text, string value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString() => Text;
    }

    private TrayApplicationContext _tray;

    private ConfigService _configService;
    private MonitorEngine _engine;
    private AutoStartService _autoStart;
    private DiskInfoService _diskInfo;

    private Timer _uiTimer;
    private bool _loadingUi;
    private bool _statusLedOn = true;
    private bool _allowClose;

    // 需随语言切换重设的标题/标签：(控件, 资源键)
    private readonly List<KeyValuePair<Label, string>> _i18nLabels = new List<KeyValuePair<Label, string>>();

    // 标题栏 / 工具栏 / 状态栏
    private AntdUI.PageHeader header;
    private Panel toolbar;
    private Panel status;
    private Label timeText;
    private Label versionText;
    private Button monitorBtn;
    private Button themeBtn;
    private Button langBtn;
    private Button aboutBtn;
    private Panel pnlStatusLed;
    private Label valMonitorStatus;

    // 左栏：设置
    private ComboBox cmbCleanupMode;
    private Label lblFolderPrefix;
    private TextBox txtFolderPrefix;
    private CheckBox chkEnablePrefix;
    private ListBox lstFolders;
    private Button btnAdd;
    private Button btnRemove;
    private Button btnToggleEnabled;
    private NumericUpDown numInterval;
    private ComboBox cmbSubdirs;
    private ComboBox cmbLanguage;
    private CheckBox chkBackground;
    private CheckBox chkMinimizeToTray;
    private CheckBox chkAutoStartMonitoring;
    private CheckBox chkAutoStartWindows;
    private RadioButton rdoDeleteShared;
    private RadioButton rdoDeletePerFolder;
    private Label lblDeleteHint;
    private Label grpDeleteTitle;
    private Label lblImageCountUnit;
    private Label lblTotalImagesText;
    private Label lblImageCountCaption;
    private Label lblReadTimeCaption;
    private NumericUpDown numStorage;
    private ComboBox cmbStorageUnit;
    private CheckBox chkImageCount;
    private NumericUpDown numImageCount;
    private CheckBox chkDiskSpace;
    private NumericUpDown numDiskSpace;
    private Label lblDiskUnit;

    // 右栏：信息显示
    private Label valCurrentPath;
    private Label valReadTime;
    private Label valActualCycle;
    private Label valImageCount;
    private Label valDisk;
    private Label valDiskFree;
    private Label valDiskTotal;
    private Label valDeleted;
    private Label valTotalFolders;
    private Label valEnabledFolders;
    private Label valTotalImages;
    private Button btnViewImages;

    public bool IsMonitoring => _engine?.IsMonitoring ?? false;

    public bool ShouldStartHidden
    {
        get
        {
            if (_configService == null) return false;
            if (_tray != null && !_tray.IsTrayAvailable) return false;
            AppConfig cfg = _configService.Config;
            return cfg.RunInBackground && cfg.AutoStartMonitoring;
        }
    }

    public event EventHandler MonitoringChanged;

    public MainForm(TrayApplicationContext tray)
    {
        _tray = tray;
        _configService = new ConfigService();
        _engine = new MonitorEngine();
        _autoStart = new AutoStartService();
        _diskInfo = new DiskInfoService();
        LocalizationManager.Initialize(_configService.Config.Language ?? "zh-CN");
        ThemeManager.Apply(string.IsNullOrEmpty(_configService.Config.Theme) ? "light" : _configService.Config.Theme);

        BuildUi();
        LoadConfigToUi();

        _uiTimer = new Timer { Interval = 500 };
        _uiTimer.Tick += UiTimer_Tick;
        _uiTimer.Start();
        _engine.StatsUpdated += Engine_StatsUpdated;
        if (_configService.Config.AutoStartMonitoring)
        {
            StartMonitoring();
        }
    }

    private Label I18nLabel(string key, bool bold = false, float size = 9f)
    {
        var label = new Label
        {
            Text = L(key),
            AutoSize = true,
            ForeColor = ThemeManager.Fg,
            Font = bold ? SafeUiFont.Bold(size) : SafeUiFont.Regular(size),
            Margin = new Padding(4, 6, 4, 2),
        };
        _i18nLabels.Add(new KeyValuePair<Label, string>(label, key));
        return label;
    }

    private void BuildUi()
    {
        Text = L("AppTitle");
        Icon = AppAssets.LoadAppIcon();
        Font = SafeUiFont.Regular(9f);
        Size = new Size(1000, 780);
        MinimumSize = new Size(900, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Mode = ThemeManager.TAMode;

        // 标题栏
        header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 40,
            Text = L("AppTitle"),
            ShowIcon = true,
            ShowButton = true,
            BackColor = ThemeManager.Bg2,
        };

        // 工具栏
        toolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = ThemeManager.Bg2 };
        toolbar.Paint += (s, e) =>
        {
            using var pen = new Pen(ThemeManager.Border);
            e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width - 1, toolbar.Height - 1);
        };
        var btns = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
        };
        monitorBtn = MakeButton(110);
        monitorBtn.Click += (s, e) =>
        {
            if (IsMonitoring) StopMonitoring();
            else StartMonitoring();
        };
        btns.Controls.Add(monitorBtn);

        aboutBtn = MakeButton(92);
        aboutBtn.Click += (s, e) => new AboutWindow().ShowDialog(this);
        btns.Controls.Add(aboutBtn);

        langBtn = MakeButton(92);
        langBtn.Click += (s, e) =>
        {
            string next = I18n.Language == "en" ? "zh-CN" : "en-US";
            _configService.Config.Language = next;
            LocalizationManager.SetLanguage(next);
            ApplyLocalizedText();
            PersistConfig(updateEngine: false);
        };
        btns.Controls.Add(langBtn);

        themeBtn = MakeButton(92);
        themeBtn.Click += (s, e) =>
        {
            string next = ThemeManager.Theme == "dark" ? "light" : "dark";
            ThemeManager.Apply(next);
            _configService.Config.Theme = next;
            PersistConfig(updateEngine: false);
        };
        btns.Controls.Add(themeBtn);
        toolbar.Controls.Add(btns);

        // 中部：左设置栏 + 右信息栏
        var center = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Bg, Tag = "bg" };

        var leftScroll = new Panel { Dock = DockStyle.Left, Width = 390, BackColor = ThemeManager.Bg, AutoScroll = true, Tag = "bg" };
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        leftScroll.Controls.Add(leftLayout);
        BuildSettingsSections(leftLayout);

        var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Bg, Padding = new Padding(8), Tag = "bg" };
        rightPanel.Controls.Add(BuildInfoPanel());

        center.Controls.Add(rightPanel);
        center.Controls.Add(leftScroll);

        // 状态栏
        status = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = ThemeManager.Bg2 };
        var st = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        st.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        st.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        timeText = MakeStatusLabel();
        versionText = MakeStatusLabel();
        versionText.Margin = new Padding(16, 5, 0, 0);
        versionText.Text = L("version") + " " + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "");
        st.Controls.Add(timeText, 0, 0);
        st.Controls.Add(versionText, 1, 0);

        var ledHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        pnlStatusLed = new Panel { Size = new Size(16, 16) };
        pnlStatusLed.Paint += pnlStatusLed_Paint;
        valMonitorStatus = MakeStatusLabel();
        valMonitorStatus.Anchor = AnchorStyles.Right;
        ledHost.Controls.Add(valMonitorStatus);
        ledHost.Controls.Add(pnlStatusLed);
        ledHost.Resize += (s, e) =>
        {
            pnlStatusLed.Left = ledHost.Width - pnlStatusLed.Width - 8;
            pnlStatusLed.Top = (ledHost.Height - pnlStatusLed.Height) / 2;
            valMonitorStatus.Left = pnlStatusLed.Left - valMonitorStatus.Width - 6;
            valMonitorStatus.Top = (ledHost.Height - valMonitorStatus.Height) / 2;
        };
        st.Controls.Add(ledHost, 2, 0);
        status.Controls.Add(st);

        // Dock 顺序：后加入的先布局（header 最顶，toolbar 其下，status 底部，center 填充剩余）
        Controls.Add(center);
        Controls.Add(toolbar);
        Controls.Add(status);
        Controls.Add(header);

        FormClosing += (s, e) =>
        {
            if (_tray != null && _tray.IsTrayAvailable && !_tray.IsExiting && _configService != null
                && _configService.Config.MinimizeToTrayOnClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            if (!_allowClose)
            {
                Shutdown();
            }
        };
        FormClosed += (s, e) =>
        {
            _uiTimer?.Stop();
            _uiTimer?.Dispose();
            _engine?.Dispose();
            _configService?.Dispose();
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            ThemeManager.ThemeChanged -= ApplyTheme;
        };

        LocalizationManager.LanguageChanged += OnLanguageChanged;
        ThemeManager.ThemeChanged += ApplyTheme;
    }

    private void BuildSettingsSections(TableLayoutPanel layout)
    {
        // 1. 清理模式
        var secMode = MakeSection(I18nLabel("CleanupModeLabel", bold: true), out var modeBody);
        cmbCleanupMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        cmbCleanupMode.SelectedIndexChanged += cmbCleanupMode_SelectedIndexChanged;
        lblFolderPrefix = I18nLabel("FolderPrefix");
        txtFolderPrefix = new TextBox { MaxLength = 32, Text = "MX", Width = 76, ReadOnly = true };
        txtFolderPrefix.TextChanged += txtFolderPrefix_TextChanged;
        chkEnablePrefix = new CheckBox { AutoSize = true };
        chkEnablePrefix.CheckedChanged += (s, e) => txtFolderPrefix.ReadOnly = !chkEnablePrefix.Checked;
        modeBody.Controls.Add(cmbCleanupMode);
        modeBody.Controls.Add(lblFolderPrefix);
        modeBody.Controls.Add(txtFolderPrefix);
        modeBody.Controls.Add(chkEnablePrefix);
        layout.Controls.Add(secMode);

        // 2. 监控路径
        var secPaths = MakeSection(I18nLabel("MonitorPaths", bold: true), out var pathsBody);
        var pathsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Color.Transparent,
        };
        pathsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pathsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathsBody.Controls.Add(pathsGrid);
        lstFolders = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true,
            IntegralHeight = false,
            ItemHeight = 24,
            Height = 110,
        };
        lstFolders.SelectedIndexChanged += (s, e) => LoadSelectedFolderSettings();
        lstFolders.DoubleClick += (s, e) => OpenSelectedFolder();
        var pathBtns = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
        };
        btnAdd = MakeSmallButton();
        btnAdd.Click += (s, e) => AddFolder();
        btnRemove = MakeSmallButton();
        btnRemove.Click += (s, e) => RemoveSelectedFolder();
        btnToggleEnabled = MakeSmallButton();
        btnToggleEnabled.Click += (s, e) => ToggleSelectedEnabled();
        pathBtns.Controls.Add(btnAdd);
        pathBtns.Controls.Add(btnRemove);
        pathBtns.Controls.Add(btnToggleEnabled);
        pathsGrid.Controls.Add(lstFolders, 0, 0);
        pathsGrid.SetRowSpan(lstFolders, 3);
        pathsGrid.Controls.Add(pathBtns, 1, 0);
        layout.Controls.Add(secPaths);

        // 3. 监控基本设置
        var secMon = MakeSection(I18nLabel("MonitorBasicSettings", bold: true), out var monBody);
        var lblInterval = I18nLabel("MonitorInterval");
        numInterval = new NumericUpDown { Minimum = 1, Maximum = 86400, Value = 2, Width = 76, Margin = new Padding(4, 4, 4, 2) };
        numInterval.ValueChanged += FolderSetting_Changed;
        var lblIntervalUnit = I18nLabel("Seconds");
        var lblSubdirs = I18nLabel("IncludeSubdirs");
        cmbSubdirs = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 88, Margin = new Padding(4, 4, 4, 2) };
        cmbSubdirs.SelectedIndexChanged += FolderSetting_Changed;
        monBody.Controls.Add(lblInterval);
        monBody.Controls.Add(numInterval);
        monBody.Controls.Add(lblIntervalUnit);
        monBody.Controls.Add(lblSubdirs);
        monBody.Controls.Add(cmbSubdirs);
        layout.Controls.Add(secMon);

        // 4. 删除条件
        grpDeleteTitle = I18nLabel("DeleteConditions", bold: true);
        var secDelete = MakeSection(grpDeleteTitle, out var delBody);
        rdoDeleteShared = new RadioButton { AutoSize = true, Checked = true, Margin = new Padding(4, 4, 8, 2) };
        rdoDeletePerFolder = new RadioButton { AutoSize = true, Margin = new Padding(4, 4, 8, 2) };
        rdoDeleteShared.CheckedChanged += DeleteMode_CheckedChanged;
        rdoDeletePerFolder.CheckedChanged += DeleteMode_CheckedChanged;
        lblDeleteHint = new Label
        {
            AutoSize = true,
            ForeColor = ThemeManager.FgDim,
            Margin = new Padding(4, 2, 4, 4),
        };
        var lblStorage = I18nLabel("StorageTime");
        numStorage = new NumericUpDown { Minimum = 1, Maximum = 999999, Value = 20, Width = 76, Margin = new Padding(4, 4, 4, 2) };
        numStorage.ValueChanged += FolderSetting_Changed;
        cmbStorageUnit = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 66, Margin = new Padding(4, 4, 4, 2) };
        cmbStorageUnit.SelectedIndexChanged += FolderSetting_Changed;
        chkImageCount = new CheckBox { AutoSize = true, Margin = new Padding(4, 4, 0, 2) };
        chkImageCount.CheckedChanged += chkImageCount_CheckedChanged;
        numImageCount = new NumericUpDown { Minimum = 1, Maximum = 10000000, Value = 3000, Width = 86, Enabled = false, Margin = new Padding(4, 2, 4, 2) };
        numImageCount.ValueChanged += FolderSetting_Changed;
        lblImageCountUnit = new Label { AutoSize = true, ForeColor = ThemeManager.Fg, Margin = new Padding(4, 6, 4, 2) };
        chkDiskSpace = new CheckBox { AutoSize = true, Margin = new Padding(4, 4, 0, 2) };
        chkDiskSpace.CheckedChanged += chkDiskSpace_CheckedChanged;
        numDiskSpace = new NumericUpDown { Minimum = 0.5m, Maximum = 100000, DecimalPlaces = 2, Increment = 1m, Value = 8, Width = 86, Enabled = false, Margin = new Padding(4, 2, 4, 2) };
        numDiskSpace.ValueChanged += FolderSetting_Changed;
        lblDiskUnit = I18nLabel("GbUnit");
        delBody.Controls.Add(rdoDeleteShared);
        delBody.Controls.Add(rdoDeletePerFolder);
        delBody.Controls.Add(lblDeleteHint);
        delBody.Controls.Add(lblStorage);
        delBody.Controls.Add(numStorage);
        delBody.Controls.Add(cmbStorageUnit);
        delBody.Controls.Add(chkImageCount);
        delBody.Controls.Add(numImageCount);
        delBody.Controls.Add(lblImageCountUnit);
        delBody.Controls.Add(chkDiskSpace);
        delBody.Controls.Add(numDiskSpace);
        delBody.Controls.Add(lblDiskUnit);
        layout.Controls.Add(secDelete);

        // 5. 多语言
        var secLang = MakeSection(I18nLabel("LanguageSettings", bold: true), out var langBody);
        cmbLanguage = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(4, 4, 4, 2) };
        cmbLanguage.SelectedIndexChanged += cmbLanguage_SelectedIndexChanged;
        langBody.Controls.Add(cmbLanguage);
        layout.Controls.Add(secLang);

        // 6. 运行设置
        var secRun = MakeSection(I18nLabel("RunSettings", bold: true), out var runBody);
        chkBackground = MakeCheck(L("RunInBackground"));
        chkMinimizeToTray = MakeCheck(L("MinimizeToTray"));
        chkAutoStartMonitoring = MakeCheck(L("AutoStartMonitoring"));
        chkAutoStartWindows = MakeCheck(L("AutoStartWindows"));
        chkBackground.CheckedChanged += GlobalSetting_Changed;
        chkMinimizeToTray.CheckedChanged += GlobalSetting_Changed;
        chkAutoStartMonitoring.CheckedChanged += GlobalSetting_Changed;
        chkAutoStartWindows.CheckedChanged += chkAutoStartWindows_CheckedChanged;
        runBody.Controls.Add(chkBackground);
        runBody.Controls.Add(chkMinimizeToTray);
        runBody.Controls.Add(chkAutoStartMonitoring);
        runBody.Controls.Add(chkAutoStartWindows);
        layout.Controls.Add(secRun);
    }

    private CheckBox MakeCheck(string text) => new CheckBox
    {
        Text = text,
        AutoSize = true,
        ForeColor = ThemeManager.Fg,
        Margin = new Padding(4, 4, 4, 2),
    };

    private Panel BuildInfoPanel()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Bg2, Tag = "panel", Padding = new Padding(1) };
        var box = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Bg2, Tag = "panel", Padding = new Padding(12) };

        var title = I18nLabel("InfoDisplay", bold: true, size: 10f);
        title.Dock = DockStyle.Top;
        title.Height = 26;

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        valCurrentPath = MakeValueLabel();
        valCurrentPath.AutoSize = false;
        valCurrentPath.Dock = DockStyle.Fill;
        valReadTime = MakeValueLabel();
        valActualCycle = MakeValueLabel();
        valImageCount = MakeValueLabel();
        valDisk = MakeValueLabel();
        valDiskFree = MakeValueLabel();
        valDiskTotal = MakeValueLabel();
        valDeleted = MakeValueLabel();

        lblReadTimeCaption = AddInfoRow(grid, "ReadTime");
        AddInfoRow(grid, "ActualCycle");
        lblImageCountCaption = AddInfoRow(grid, "ImageCountLabel");
        AddInfoRow(grid, "CurrentFolder", valCurrentPath);
        AddInfoRow(grid, "MonitoredDisk", valDisk);
        AddInfoRow(grid, "DiskFree", valDiskFree);
        AddInfoRow(grid, "DiskTotal", valDiskTotal);
        AddInfoRow(grid, "DeletedLastRun", valDeleted);
        AddInfoRow(grid, valReadTime);
        AddInfoRow(grid, valActualCycle);
        AddInfoRow(grid, valImageCount);

        var summaryTitle = I18nLabel("GlobalSummary", bold: true, size: 10f);
        summaryTitle.Margin = new Padding(0, 14, 0, 4);
        int srow = grid.RowCount;
        grid.RowCount = srow + 1;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(summaryTitle, 0, srow);
        grid.SetColumnSpan(summaryTitle, 2);

        valTotalFolders = MakeValueLabel();
        valEnabledFolders = MakeValueLabel();
        valTotalImages = MakeValueLabel();
        lblTotalImagesText = AddInfoRow(grid, "TotalImages");
        AddInfoRow(grid, "TotalFolders", valTotalFolders);
        AddInfoRow(grid, "EnabledFolders", valEnabledFolders);
        AddInfoRow(grid, lblTotalImagesText, valTotalImages);

        btnViewImages = MakeButton(150);
        btnViewImages.Height = 34;
        btnViewImages.Dock = DockStyle.Bottom;
        btnViewImages.Click += (s, e) => OpenSelectedFolder();

        box.Controls.Add(grid);
        box.Controls.Add(title);
        host.Controls.Add(btnViewImages);
        host.Controls.Add(box);
        return host;
    }

    private Label AddInfoRow(TableLayoutPanel grid, string captionKey)
    {
        var caption = MakeInfoCaption(captionKey);
        int row = grid.RowCount;
        grid.RowCount = row + 1;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(caption, 0, row);
        return caption;
    }

    private void AddInfoRow(TableLayoutPanel grid, Label caption)
    {
        int row = grid.RowCount;
        grid.RowCount = row + 1;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(caption, 1, row);
    }

    private void AddInfoRow(TableLayoutPanel grid, string captionKey, Label value)
    {
        var caption = MakeInfoCaption(captionKey);
        int row = grid.RowCount;
        grid.RowCount = row + 1;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(caption, 0, row);
        grid.Controls.Add(value, 1, row);
    }

    private void AddInfoRow(TableLayoutPanel grid, Label caption, Label value)
    {
        int row = grid.RowCount;
        grid.RowCount = row + 1;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(caption, 0, row);
        grid.Controls.Add(value, 1, row);
    }

    private Label MakeInfoCaption(string key)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = L(key),
            ForeColor = ThemeManager.Fg,
            Font = SafeUiFont.Regular(9f),
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 5, 8, 5),
        };
        _i18nLabels.Add(new KeyValuePair<Label, string>(label, key));
        return label;
    }

    private static Label MakeValueLabel() => new Label
    {
        Text = "-",
        AutoSize = true,
        Font = SafeUiFont.Bold(9f),
        ForeColor = ThemeManager.Accent,
        Margin = new Padding(4, 5, 0, 5),
    };

    private static Label MakeStatusLabel() => new Label
    {
        Text = "",
        ForeColor = ThemeManager.FgDim,
        AutoSize = true,
        Font = new Font("Microsoft YaHei UI", 8.25f),
        Margin = new Padding(8, 5, 0, 0),
    };

    private static Button MakeButton(int width) => new Button
    {
        Width = width,
        Height = 30,
        Margin = new Padding(4, 9, 0, 0),
        Type = TTypeMini.Default,
        DefaultBorderColor = ThemeManager.BtnBorder,
        BorderWidth = 1,
    };

    private Button MakeSmallButton() => new Button
    {
        Width = 110,
        Height = 28,
        Margin = new Padding(2, 3, 2, 0),
        Type = TTypeMini.Default,
        DefaultBorderColor = ThemeManager.BtnBorder,
        BorderWidth = 1,
    };

    private Panel MakeSection(Label title, out FlowLayoutPanel body)
    {
        var host = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = Color.Transparent, Padding = new Padding(8, 2, 8, 8) };
        title.Dock = DockStyle.Top;
        title.Height = 24;
        var bodyHost = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = ThemeManager.Bg2, Tag = "panel", Padding = new Padding(8) };
        body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            BackColor = Color.Transparent,
        };
        bodyHost.Controls.Add(body);
        host.Controls.Add(bodyHost);
        host.Controls.Add(title);
        return host;
    }

    // ---------- 主题 / 语言 ----------

    private void ApplyTheme()
    {
        Mode = ThemeManager.TAMode;
        header.BackColor = ThemeManager.Bg2;
        toolbar.BackColor = ThemeManager.Bg2;
        status.BackColor = ThemeManager.Bg2;
        ApplyThemeRecursive(Controls);
        toolbar.Invalidate();
        status.Invalidate();
        pnlStatusLed?.Invalidate();
    }

    private static void ApplyThemeRecursive(Control.ControlCollection controls)
    {
        foreach (Control c in controls)
        {
            if (c is AntdUI.Button || c is AntdUI.PageHeader)
            {
                ApplyThemeRecursive(c.Controls);
                continue;
            }
            switch (c)
            {
                case ListBox lb:
                    lb.BackColor = ThemeManager.Bg3;
                    lb.ForeColor = ThemeManager.Fg;
                    break;
                case TextBox tb:
                    tb.BackColor = ThemeManager.Bg3;
                    tb.ForeColor = ThemeManager.Fg;
                    break;
                case ComboBox cb:
                    cb.BackColor = ThemeManager.Bg3;
                    cb.ForeColor = ThemeManager.Fg;
                    break;
                case NumericUpDown num:
                    num.BackColor = ThemeManager.Bg3;
                    num.ForeColor = ThemeManager.Fg;
                    break;
                case RadioButton:
                case CheckBox:
                    c.ForeColor = ThemeManager.Fg;
                    break;
                case Panel p:
                    if (p.BackColor != Color.Transparent)
                        p.BackColor = Equals(p.Tag, "bg") ? ThemeManager.Bg : ThemeManager.Bg2;
                    break;
            }
            ApplyThemeRecursive(c.Controls);
        }
    }

    private void OnLanguageChanged()
    {
        Text = L("AppTitle");
        header.Text = L("AppTitle");
        ApplyLocalizedText();
        _tray?.RefreshTexts();
    }

    public void ApplyLocalizedText()
    {
        _loadingUi = true;
        foreach (var pair in _i18nLabels)
        {
            pair.Key.Text = L(pair.Value);
        }
        ApplyModeDependentText();
        lblFolderPrefix.Text = L("FolderPrefix");
        chkEnablePrefix.Text = I18n.T("enablePrefixEdit");
        btnAdd.Text = L("AddFolder");
        btnRemove.Text = L("RemoveFolder");
        btnToggleEnabled.Text = L("ToggleEnabled");
        rdoDeleteShared.Text = L("DeleteModeShared");
        rdoDeletePerFolder.Text = L("DeleteModePerFolder");
        chkBackground.Text = L("RunInBackground");
        chkMinimizeToTray.Text = L("MinimizeToTray");
        chkAutoStartMonitoring.Text = L("AutoStartMonitoring");
        chkAutoStartWindows.Text = L("AutoStartWindows");
        chkDiskSpace.Text = L("DiskSpace");
        lblDiskUnit.Text = L("GbUnit");
        RebindLanguageCombo();
        RebindYesNoCombo();
        RebindTimeUnitCombo();
        RebindCleanupModeCombo();
        UpdateMonitorButtonText();
        UpdateToolbarButtons();
        RefreshFolderList(preserveSelection: true);
        RefreshInfo();
        _loadingUi = false;
        _tray?.RefreshTexts();
    }

    private void UpdateToolbarButtons()
    {
        aboutBtn.Text = L("AppTitle") == null ? "" : I18n.T("about");
        aboutBtn.IconSvg = AntIcon.Svg(AntIcon.InfoCircle);
        themeBtn.Text = ThemeManager.Theme == "dark" ? I18n.T("themeLight") : I18n.T("themeDark");
        themeBtn.IconSvg = AntIcon.Svg(ThemeManager.Theme == "dark" ? AntIcon.Sun : AntIcon.Moon);
        langBtn.Text = I18n.Language == "en" ? I18n.T("languageZh") : I18n.T("languageEn");
        langBtn.IconSvg = AntIcon.Svg(AntIcon.Global);
        monitorBtn.IconSvg = AntIcon.Svg(IsMonitoring ? AntIcon.Unlock : AntIcon.Lock);
    }

    // ---------- 托盘 / 监控 ----------

    public void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    public void HideToTray()
    {
        if (_tray != null && _tray.IsTrayAvailable)
        {
            Hide();
            ShowInTaskbar = false;
        }
    }

    public void StartMonitoring()
    {
        if (_engine != null)
        {
            _engine.Start(_configService.Config);
            if (_configService.Config.RunInBackground)
            {
                HideToTray();
            }
            UpdateMonitorButtonText();
            RefreshInfo();
            MonitoringChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void StopMonitoring()
    {
        if (_engine != null)
        {
            _engine.Stop();
            UpdateMonitorButtonText();
            RefreshInfo();
            MonitoringChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Shutdown()
    {
        _allowClose = true;
        _configService?.SaveNow();
        _engine?.Stop();
    }

    // ---------- 事件处理 ----------

    private void UiTimer_Tick(object sender, EventArgs e)
    {
        if (IsMonitoring)
        {
            _statusLedOn = !_statusLedOn;
            pnlStatusLed.Invalidate();
        }
        timeText.Text = L("systemTime") + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        RefreshInfo();
    }

    private void Engine_StatsUpdated(object sender, EventArgs e)
    {
        if (IsHandleCreated && !IsDisposed)
        {
            try { BeginInvoke(new Action(RefreshInfo)); } catch { }
        }
    }

    private void pnlStatusLed_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(!IsMonitoring
            ? Color.Gray
            : (_statusLedOn ? Color.FromArgb(0, 210, 70) : Color.FromArgb(0, 120, 40)));
        e.Graphics.FillEllipse(brush, 1, 1, 13, 13);
        using var pen = new Pen(Color.FromArgb(60, 60, 60));
        e.Graphics.DrawEllipse(pen, 1, 1, 13, 13);
    }

    private void cmbCleanupMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!_loadingUi && _configService != null)
        {
            string value = GetComboValue(cmbCleanupMode);
            _configService.Config.CleanupMode = value == "Folder" ? CleanupMode.Folder : CleanupMode.Image;
            ApplyModeDependentText();
            PersistConfig(updateEngine: true);
            RefreshInfo();
        }
    }

    private void txtFolderPrefix_TextChanged(object sender, EventArgs e)
    {
        if (!_loadingUi && _configService != null)
        {
            _configService.Config.FolderPrefix = txtFolderPrefix.Text;
            PersistConfig(updateEngine: true);
        }
    }

    private void FolderSetting_Changed(object sender, EventArgs e)
    {
        SaveSelectedFolderSettings();
    }

    private void chkImageCount_CheckedChanged(object sender, EventArgs e)
    {
        numImageCount.Enabled = chkImageCount.Checked;
        SaveSelectedFolderSettings();
    }

    private void chkDiskSpace_CheckedChanged(object sender, EventArgs e)
    {
        numDiskSpace.Enabled = chkDiskSpace.Checked;
        SaveSelectedFolderSettings();
    }

    private void DeleteMode_CheckedChanged(object sender, EventArgs e)
    {
        if (_loadingUi || _configService == null || sender is not RadioButton rb || !rb.Checked)
        {
            return;
        }
        AppConfig cfg = _configService.Config;
        if (cfg.UseSharedDeleteConditions = rdoDeleteShared.Checked)
        {
            DeleteConditions source = GetSelectedProfile()?.DeleteConditions ?? cfg.SharedDeleteConditions;
            if (source != null)
            {
                cfg.SharedDeleteConditions.CopyFrom(source);
            }
            foreach (MonitorFolderProfile folder in cfg.Folders)
            {
                folder.DeleteConditions.CopyFrom(cfg.SharedDeleteConditions);
            }
        }
        PersistConfig(updateEngine: true);
        LoadSelectedFolderSettings();
    }

    private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!_loadingUi && _configService != null)
        {
            string language = cmbLanguage.SelectedItem is ComboItem item ? item.Value : "zh-CN";
            _configService.Config.Language = language;
            LocalizationManager.SetLanguage(language);
            ApplyLocalizedText();
            PersistConfig(updateEngine: false);
        }
    }

    private void GlobalSetting_Changed(object sender, EventArgs e)
    {
        SaveGlobalSettings();
    }

    private void chkAutoStartWindows_CheckedChanged(object sender, EventArgs e)
    {
        if (!_loadingUi && _autoStart != null)
        {
            SaveGlobalSettings();
            _autoStart.SetEnabled(chkAutoStartWindows.Checked);
        }
    }

    // ---------- 配置加载 / 保存 ----------

    private static string L(string key) => LocalizationManager.Get(key);

    private void LoadConfigToUi()
    {
        _loadingUi = true;
        try
        {
            AppConfig cfg = _configService.Config;
            RebindTimeUnitCombo();
            RebindYesNoCombo();
            RebindLanguageCombo();
            SelectComboValue(cmbLanguage, string.IsNullOrEmpty(cfg.Language) ? "zh-CN" : cfg.Language);
            chkBackground.Checked = cfg.RunInBackground;
            chkMinimizeToTray.Checked = cfg.MinimizeToTrayOnClose;
            chkAutoStartMonitoring.Checked = cfg.AutoStartMonitoring;
            chkAutoStartWindows.Checked = cfg.AutoStartWithWindows;
            txtFolderPrefix.Text = string.IsNullOrWhiteSpace(cfg.FolderPrefix) ? "MX" : cfg.FolderPrefix;
            chkEnablePrefix.Checked = false;
            txtFolderPrefix.ReadOnly = true;
            RebindCleanupModeCombo();
            if (cfg.UseSharedDeleteConditions) rdoDeleteShared.Checked = true;
            else rdoDeletePerFolder.Checked = true;
            RefreshFolderList(preserveSelection: false);
            if (lstFolders.Items.Count > 0)
                lstFolders.SelectedIndex = 0;
            else
                LoadSelectedFolderSettings();
            ApplyModeDependentText();
            ApplyLocalizedText();
            _engine.UpdateConfig(cfg);
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void RefreshFolderList(bool preserveSelection)
    {
        if (_configService == null) return;
        Guid? selectedId = GetSelectedProfile()?.Id;
        lstFolders.BeginUpdate();
        lstFolders.Items.Clear();
        foreach (MonitorFolderProfile folder in _configService.Config.Folders)
        {
            lstFolders.Items.Add(new FolderListItem(folder));
        }
        lstFolders.EndUpdate();
        if (!preserveSelection || !selectedId.HasValue) return;
        for (int i = 0; i < lstFolders.Items.Count; i++)
        {
            if (((FolderListItem)lstFolders.Items[i]).Profile.Id == selectedId.Value)
            {
                lstFolders.SelectedIndex = i;
                break;
            }
        }
    }

    private void LoadSelectedFolderSettings()
    {
        MonitorFolderProfile profile = GetSelectedProfile();
        AppConfig cfg = _configService?.Config;
        bool shared = cfg?.UseSharedDeleteConditions ?? false;
        bool wasLoading = _loadingUi;
        _loadingUi = true;
        try
        {
            bool folderEnabled = profile != null;
            numInterval.Enabled = folderEnabled;
            cmbSubdirs.Enabled = folderEnabled;
            if (profile != null)
            {
                numInterval.Value = Clamp(profile.MonitorIntervalSeconds, numInterval);
                SelectComboValue(cmbSubdirs, profile.IncludeSubdirectories ? "1" : "0");
            }
            DeleteConditions deleteTarget = shared ? (cfg.SharedDeleteConditions ?? new DeleteConditions()) : profile?.DeleteConditions;
            bool deleteEnabled = shared || profile != null;
            SetDeleteConditionControlsEnabled(deleteEnabled);
            if (deleteTarget != null)
            {
                LoadDeleteConditionsToUi(deleteTarget);
            }
            RefreshInfo();
        }
        finally
        {
            _loadingUi = wasLoading;
        }
    }

    private void LoadDeleteConditionsToUi(DeleteConditions conditions)
    {
        numStorage.Value = Clamp(conditions.StorageTimeValue, numStorage);
        cmbStorageUnit.SelectedIndex = Math.Max(0, Math.Min(3, (int)conditions.StorageTimeUnit));
        chkImageCount.Checked = conditions.ImageCountEnabled;
        numImageCount.Value = Clamp(conditions.ImageCountThreshold, numImageCount);
        numImageCount.Enabled = chkImageCount.Checked;
        chkDiskSpace.Checked = conditions.DiskSpaceEnabled;
        numDiskSpace.Value = Clamp((decimal)conditions.DiskSpaceThresholdGb, numDiskSpace);
        numDiskSpace.Enabled = chkDiskSpace.Checked;
    }

    private void SaveDeleteConditionsFromUi(DeleteConditions conditions)
    {
        conditions.StorageTimeValue = (int)numStorage.Value;
        conditions.StorageTimeUnit = (TimeUnit)Math.Max(0, cmbStorageUnit.SelectedIndex);
        conditions.ImageCountEnabled = chkImageCount.Checked;
        conditions.ImageCountThreshold = (int)numImageCount.Value;
        conditions.DiskSpaceEnabled = chkDiskSpace.Checked;
        conditions.DiskSpaceThresholdGb = (double)numDiskSpace.Value;
    }

    private void SetDeleteConditionControlsEnabled(bool enabled)
    {
        numStorage.Enabled = enabled;
        cmbStorageUnit.Enabled = enabled;
        chkImageCount.Enabled = enabled;
        chkDiskSpace.Enabled = enabled;
        if (!enabled)
        {
            numImageCount.Enabled = false;
            numDiskSpace.Enabled = false;
        }
    }

    private void SaveSelectedFolderSettings()
    {
        if (_loadingUi || _configService == null) return;
        AppConfig cfg = _configService.Config;
        MonitorFolderProfile profile = GetSelectedProfile();
        if (profile != null)
        {
            profile.MonitorIntervalSeconds = (int)numInterval.Value;
            profile.IncludeSubdirectories = cmbSubdirs.SelectedIndex == 1;
        }
        if (cfg.UseSharedDeleteConditions)
        {
            cfg.SharedDeleteConditions ??= new DeleteConditions();
            SaveDeleteConditionsFromUi(cfg.SharedDeleteConditions);
            foreach (MonitorFolderProfile folder in cfg.Folders)
            {
                folder.DeleteConditions.CopyFrom(cfg.SharedDeleteConditions);
            }
        }
        else
        {
            if (profile == null) return;
            SaveDeleteConditionsFromUi(profile.DeleteConditions);
        }
        PersistConfig(updateEngine: true);
        RefreshFolderList(preserveSelection: true);
    }

    private void SaveGlobalSettings()
    {
        if (!_loadingUi && _configService != null)
        {
            AppConfig config = _configService.Config;
            config.RunInBackground = chkBackground.Checked;
            config.MinimizeToTrayOnClose = chkMinimizeToTray.Checked;
            config.AutoStartMonitoring = chkAutoStartMonitoring.Checked;
            config.AutoStartWithWindows = chkAutoStartWindows.Checked;
            PersistConfig(updateEngine: false);
        }
    }

    private bool IsFolderCleanupMode() => _configService?.Config.CleanupMode == CleanupMode.Folder;

    private void ApplyModeDependentText()
    {
        bool folder = IsFolderCleanupMode();
        grpDeleteTitle.Text = L(folder ? "FolderDeleteConditions" : "DeleteConditions");
        lblDeleteHint.Text = L(folder ? "FolderDeleteConditionsHint" : "DeleteConditionsHint");
        chkImageCount.Text = L(folder ? "FolderCount" : "ImageCount");
        lblImageCountUnit.Text = L(folder ? "FoldersUnit" : "ImagesUnit");
        lblImageCountCaption.Text = L(folder ? "FolderCountLabel" : "ImageCountLabel");
        lblTotalImagesText.Text = L(folder ? "TotalFolderItems" : "TotalImages");
        lblReadTimeCaption.Text = L(folder ? "FolderScanTime" : "ReadTime");
        if (btnViewImages != null)
        {
            btnViewImages.Text = L(folder ? "ViewFolders" : "ViewImages");
        }
    }

    private void RebindCleanupModeCombo()
    {
        string selected = GetComboValue(cmbCleanupMode);
        if (selected == null && _configService != null)
        {
            selected = _configService.Config.CleanupMode == CleanupMode.Folder ? "Folder" : "Image";
        }
        cmbCleanupMode.Items.Clear();
        cmbCleanupMode.Items.Add(new ComboItem(L("CleanupModeImage"), "Image"));
        cmbCleanupMode.Items.Add(new ComboItem(L("CleanupModeFolder"), "Folder"));
        SelectComboValue(cmbCleanupMode, selected);
    }

    private void PersistConfig(bool updateEngine)
    {
        _configService.ScheduleSave();
        if (updateEngine)
        {
            _engine.UpdateConfig(_configService.Config);
        }
    }

    // ---------- 文件夹操作 ----------

    private void AddFolder()
    {
        try
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = L("SelectFolder");
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            string path = dialog.SelectedPath;
            if (_configService.Config.Folders.Any(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, L("DuplicatePath"), L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            MonitorFolderProfile profile = new MonitorFolderProfile { Path = path };
            if (_configService.Config.UseSharedDeleteConditions && _configService.Config.SharedDeleteConditions != null)
            {
                profile.DeleteConditions.CopyFrom(_configService.Config.SharedDeleteConditions);
            }
            else
            {
                MonitorFolderProfile selected = GetSelectedProfile();
                if (selected != null)
                {
                    profile.DeleteConditions.CopyFrom(selected.DeleteConditions);
                }
            }
            _configService.Config.Folders.Add(profile);
            PersistConfig(updateEngine: true);
            RefreshFolderList(preserveSelection: false);
            lstFolders.SelectedIndex = lstFolders.Items.Count - 1;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L("CannotOpenFolderDialog") + "\r\n" + ex.Message, L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }

    private void RemoveSelectedFolder()
    {
        MonitorFolderProfile profile = GetSelectedProfile();
        if (profile == null)
        {
            MessageBox.Show(this, L("NoFolderSelected"), L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }
        else if (MessageBox.Show(this, L("ConfirmRemove"), L("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _configService.Config.Folders.Remove(profile);
            PersistConfig(updateEngine: true);
            RefreshFolderList(preserveSelection: false);
            if (lstFolders.Items.Count > 0)
                lstFolders.SelectedIndex = 0;
            else
                LoadSelectedFolderSettings();
        }
    }

    private void ToggleSelectedEnabled()
    {
        MonitorFolderProfile profile = GetSelectedProfile();
        if (profile == null)
        {
            MessageBox.Show(this, L("NoFolderSelected"), L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            return;
        }
        profile.Enabled = !profile.Enabled;
        PersistConfig(updateEngine: true);
        RefreshFolderList(preserveSelection: true);
        RefreshInfo();
    }

    private void OpenSelectedFolder()
    {
        MonitorFolderProfile profile = GetSelectedProfile();
        if (profile == null)
        {
            MessageBox.Show(this, L("NoFolderSelected"), L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            return;
        }
        if (!Directory.Exists(profile.Path))
        {
            MessageBox.Show(this, L("PathNotExist"), L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }
        try
        {
            Process.Start("explorer.exe", "\"" + profile.Path + "\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L("CannotOpenExplorer") + "\r\n" + ex.Message, L("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }

    // ---------- 信息显示 ----------

    private void RefreshInfo()
    {
        if (IsDisposed || _engine == null || _configService == null) return;
        MonitorFolderProfile profile = GetSelectedProfile();
        GlobalMonitorStats global = _engine.GetGlobalStats();
        if (profile == null)
        {
            valCurrentPath.Text = L("SelectFolderHint");
            valReadTime.Text = "-";
            valActualCycle.Text = "-";
            valImageCount.Text = "-";
            valDisk.Text = "-";
            valDiskFree.Text = "-";
            valDiskTotal.Text = "-";
            valDeleted.Text = "-";
        }
        else
        {
            MonitorStats stats = _engine.GetStats(profile.Id);
            DiskInfo disk = _diskInfo.GetDiskInfo(profile.Path);
            valCurrentPath.Text = profile.Path;
            valReadTime.Text = stats.LastScanUtc == default ? "-" : stats.ScanElapsedMs + " " + L("MsUnit");
            valActualCycle.Text = stats.ActualIntervalMs <= 0 ? "-" : stats.ActualIntervalMs + " " + L("MsUnit");
            valImageCount.Text = stats.LastScanUtc == default ? "-" : stats.ImageCount.ToString();
            valDisk.Text = string.IsNullOrEmpty(disk.DriveLetter) ? "-" : disk.DriveLetter.TrimEnd('\\', ':') + L("DriveSuffix");
            valDiskFree.Text = disk.IsValid ? disk.FreeSpaceGb.ToString("0.00") + " " + L("GbUnit") : "-";
            valDiskTotal.Text = disk.IsValid ? disk.TotalSpaceGb.ToString("0.00") + " " + L("GbUnit") : "-";
            valDeleted.Text = stats.LastScanUtc == default ? "-" : stats.DeletedCountLastRun.ToString();
        }
        valTotalFolders.Text = global.TotalFolders.ToString();
        valEnabledFolders.Text = global.EnabledFolders.ToString();
        valTotalImages.Text = global.TotalImageCount.ToString();
        valMonitorStatus.Text = IsMonitoring ? L("StatusRunning") : L("StatusStopped");
        valMonitorStatus.ForeColor = IsMonitoring ? Color.FromArgb(0, 140, 50) : ThemeManager.FgDim;
    }

    private void UpdateMonitorButtonText()
    {
        monitorBtn.Text = IsMonitoring ? L("StopMonitoring") : L("StartMonitoring");
        monitorBtn.Type = IsMonitoring ? TTypeMini.Error : TTypeMini.Primary;
        monitorBtn.IconSvg = AntIcon.Svg(IsMonitoring ? AntIcon.Unlock : AntIcon.Lock);
    }

    private MonitorFolderProfile GetSelectedProfile()
    {
        return lstFolders.SelectedItem is FolderListItem item ? item.Profile : null;
    }

    private void RebindLanguageCombo()
    {
        string selected = GetComboValue(cmbLanguage);
        if (selected == null && _configService != null)
        {
            selected = _configService.Config.Language;
        }
        cmbLanguage.Items.Clear();
        cmbLanguage.Items.Add(new ComboItem(L("Chinese"), "zh-CN"));
        cmbLanguage.Items.Add(new ComboItem(L("English"), "en-US"));
        SelectComboValue(cmbLanguage, selected);
    }

    private void RebindYesNoCombo()
    {
        string selected = GetComboValue(cmbSubdirs);
        int index = cmbSubdirs.SelectedIndex;
        cmbSubdirs.Items.Clear();
        cmbSubdirs.Items.Add(new ComboItem(L("No"), "0"));
        cmbSubdirs.Items.Add(new ComboItem(L("Yes"), "1"));
        if (!string.IsNullOrEmpty(selected))
        {
            SelectComboValue(cmbSubdirs, selected);
        }
        else if (index >= 0 && index < cmbSubdirs.Items.Count)
        {
            cmbSubdirs.SelectedIndex = index;
        }
        else
        {
            cmbSubdirs.SelectedIndex = 1;
        }
    }

    private void RebindTimeUnitCombo()
    {
        int index = cmbStorageUnit.SelectedIndex;
        cmbStorageUnit.Items.Clear();
        cmbStorageUnit.Items.Add(L("UnitSeconds"));
        cmbStorageUnit.Items.Add(L("UnitMinutes"));
        cmbStorageUnit.Items.Add(L("UnitHours"));
        cmbStorageUnit.Items.Add(L("UnitDays"));
        cmbStorageUnit.SelectedIndex = index >= 0 && index < 4 ? index : 0;
    }

    private static string GetComboValue(ComboBox combo)
    {
        return combo.SelectedItem is ComboItem item ? item.Value : null;
    }

    private static void SelectComboValue(ComboBox combo, string value)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboItem item && item.Value == value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private static decimal Clamp(decimal value, NumericUpDown control)
    {
        if (value < control.Minimum) return control.Minimum;
        if (value > control.Maximum) return control.Maximum;
        return value;
    }
}
