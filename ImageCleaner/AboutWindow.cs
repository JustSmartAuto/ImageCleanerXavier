using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using AntdUI;
using ImageCleaner.Localization;

namespace ImageCleaner;

public class AboutWindow : AntdUI.Window
{
    private readonly AntdUI.PageHeader titleBar;

    public AboutWindow()
    {
        Text = L("AppTitle");
        Size = new Size(440, 420);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Resizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        Mode = ThemeManager.TAMode;
        Icon = AppAssets.LoadAppIcon();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(24) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // 头部：Logo + 应用名
        var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 72, WrapContents = false };
        var logoBox = new PictureBox
        {
            Image = AppAssets.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(64, 64),
            Margin = new Padding(0, 0, 12, 0),
        };
        header.Controls.Add(logoBox);
        var title = new AntdUI.Label
        {
            Text = L("AppTitle"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold),
            Margin = new Padding(0, 18, 0, 0),
        };
        header.Controls.Add(title);
        root.Controls.Add(header, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var ver = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";
        var plus = ver.IndexOf('+');
        if (plus > 0) ver = ver[..plus];

        var verLabel = MakeLine(L("version") + " " + ver);
        var descLabel = MakeLine(I18n.T("aboutDesc"), true);
        var techLabel = MakeLine(I18n.T("aboutTech"), true);
        body.Controls.Add(verLabel, 0, 0);
        body.Controls.Add(descLabel, 0, 1);
        body.Controls.Add(techLabel, 0, 2);
        root.Controls.Add(body, 0, 1);

        var closeRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var closeBtn = new AntdUI.Button
        {
            Text = I18n.T("aboutClose"),
            Width = 90,
            Height = 30,
            Type = TTypeMini.Primary,
        };
        closeBtn.Click += (s, e) => Close();
        closeRow.Controls.Add(closeBtn);
        root.Controls.Add(closeRow, 0, 2);

        titleBar = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 36,
            Text = L("AppTitle"),
            ShowButton = true,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ThemeManager.Bg2,
        };

        Controls.Add(root);
        Controls.Add(titleBar);

        I18n.LanguageChanged += UpdateTexts;
        ThemeManager.ThemeChanged += ApplyTheme;
        FormClosed += (s, e) =>
        {
            I18n.LanguageChanged -= UpdateTexts;
            ThemeManager.ThemeChanged -= ApplyTheme;
        };
        UpdateTexts();
        ApplyTheme();
    }

    private static string L(string key) => I18n.T(key);

    private void UpdateTexts()
    {
        Text = L("AppTitle");
        titleBar.Text = L("AppTitle");
    }

    private void ApplyTheme()
    {
        Mode = ThemeManager.TAMode;
        titleBar.BackColor = ThemeManager.Bg2;
    }

    private static System.Windows.Forms.Label MakeLine(string text, bool wrap = false) => new System.Windows.Forms.Label
    {
        Text = text,
        AutoSize = true,
        MaximumSize = wrap ? new Size(370, 0) : Size.Empty,
        ForeColor = ThemeManager.Fg,
        Margin = new Padding(0, 5, 0, 5),
    };
}
