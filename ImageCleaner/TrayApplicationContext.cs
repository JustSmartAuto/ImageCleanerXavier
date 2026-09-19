using System;
using System.Drawing;
using System.Windows.Forms;
using ImageCleaner.Localization;

namespace ImageCleaner;

public class TrayApplicationContext : ApplicationContext
{
	private readonly MainForm _form;

	private readonly NotifyIcon _notifyIcon;

	private readonly ContextMenuStrip _menu;

	private readonly ToolStripMenuItem _miShow;

	private readonly ToolStripMenuItem _miToggle;

	private readonly ToolStripMenuItem _miExit;

	private readonly Timer _blinkTimer;

	private readonly Icon _iconIdle;

	private readonly Icon _iconActiveA;

	private readonly Icon _iconActiveB;

	private readonly bool _ownsIcons;

	private bool _blinkState;

	public bool IsExiting { get; private set; }

	public bool IsTrayAvailable => _notifyIcon != null;

	public TrayApplicationContext(bool preferTray = true)
	{
		_ownsIcons = TryCreateIcons(out _iconIdle, out _iconActiveA, out _iconActiveB);
		_miShow = new ToolStripMenuItem();
		_miToggle = new ToolStripMenuItem();
		_miExit = new ToolStripMenuItem();
		_menu = new ContextMenuStrip();
		_menu.Items.Add(_miShow);
		_menu.Items.Add(_miToggle);
		_menu.Items.Add(new ToolStripSeparator());
		_menu.Items.Add(_miExit);
		_blinkTimer = new Timer
		{
			Interval = 500
		};
		_blinkTimer.Tick += delegate
		{
			if (_notifyIcon != null)
			{
				_blinkState = !_blinkState;
				_notifyIcon.Icon = (_blinkState ? _iconActiveA : _iconActiveB);
			}
		};
		_form = new MainForm(this);
		_form.FormClosed += OnFormClosed;
		_form.MonitoringChanged += delegate
		{
			UpdateTrayState();
		};
		_miShow.Click += delegate
		{
			ShowMainWindow();
		};
		_miToggle.Click += delegate
		{
			if (_form.IsMonitoring)
			{
				_form.StopMonitoring();
			}
			else
			{
				_form.StartMonitoring();
			}
		};
		_miExit.Click += delegate
		{
			RequestExit();
		};
		_notifyIcon = (preferTray ? TryCreateNotifyIcon() : null);
		RefreshTexts();
		UpdateTrayState();
		if (!_form.ShouldStartHidden || !IsTrayAvailable)
		{
			ShowMainWindow();
		}
	}

	public void ShowMainWindow()
	{
		_form.ShowFromTray();
	}

	public void RefreshTexts()
	{
		if (_miShow != null && _miToggle != null && _miExit != null)
		{
			_miShow.Text = LocalizationManager.Get("TrayShow");
			_miExit.Text = LocalizationManager.Get("TrayExit");
			_miToggle.Text = ((_form != null && _form.IsMonitoring) ? LocalizationManager.Get("TrayStop") : LocalizationManager.Get("TrayStart"));
			if (_notifyIcon != null)
			{
				_notifyIcon.Text = LocalizationManager.Get("AppTitle") + " - " + ((_form != null && _form.IsMonitoring) ? LocalizationManager.Get("StatusRunning") : LocalizationManager.Get("StatusStopped"));
			}
		}
	}

	public void RequestExit()
	{
		if (!IsExiting)
		{
			IsExiting = true;
			if (_blinkTimer != null)
			{
				_blinkTimer.Stop();
			}
			if (_notifyIcon != null)
			{
				_notifyIcon.Visible = false;
			}
			_form.Close();
			ExitThread();
		}
	}

	private NotifyIcon TryCreateNotifyIcon()
	{
		try
		{
			NotifyIcon notifyIcon = new NotifyIcon();
			notifyIcon.Icon = _iconIdle;
			notifyIcon.Visible = true;
			notifyIcon.ContextMenuStrip = _menu;
			notifyIcon.Text = LocalizationManager.Get("AppTitle");
			notifyIcon.DoubleClick += delegate
			{
				ShowMainWindow();
			};
			return notifyIcon;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static bool TryCreateIcons(out Icon idle, out Icon activeA, out Icon activeB)
	{
		try
		{
			idle = TrayIcons.Create(Color.Gray);
			activeA = TrayIcons.Create(Color.FromArgb(0, 180, 70));
			activeB = TrayIcons.Create(Color.FromArgb(160, 255, 170));
			return true;
		}
		catch
		{
			idle = SystemIcons.Application;
			activeA = SystemIcons.Information;
			activeB = SystemIcons.Application;
			return false;
		}
	}

	private void UpdateTrayState()
	{
		RefreshTexts();
		if (_notifyIcon != null && _form != null)
		{
			if (_form.IsMonitoring)
			{
				_blinkTimer.Start();
				_notifyIcon.Icon = _iconActiveA;
			}
			else
			{
				_blinkTimer.Stop();
				_notifyIcon.Icon = _iconIdle;
			}
		}
	}

	private void OnFormClosed(object sender, FormClosedEventArgs e)
	{
		if (!IsExiting)
		{
			RequestExit();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_blinkTimer != null)
			{
				_blinkTimer.Dispose();
			}
			if (_notifyIcon != null)
			{
				_notifyIcon.Dispose();
			}
			if (_menu != null)
			{
				_menu.Dispose();
			}
			if (_ownsIcons)
			{
				if (_iconIdle != null)
				{
					_iconIdle.Dispose();
				}
				if (_iconActiveA != null)
				{
					_iconActiveA.Dispose();
				}
				if (_iconActiveB != null)
				{
					_iconActiveB.Dispose();
				}
			}
		}
		base.Dispose(disposing);
	}
}
