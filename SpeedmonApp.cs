using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

/// <summary>Wires together monitor, taskbar widget, flyout, tray icon and menu.</summary>
class SpeedmonApp : ApplicationContext
{
    private const string AppName      = "TaskBarSpeedmon";
    private const string AutostartKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private readonly NetworkMonitor    _monitor;
    private readonly TaskbarWidget     _widget;
    private readonly SpeedFlyout       _flyout;
    private readonly NotifyIcon        _trayIcon;
    private readonly ContextMenuStrip  _menu;
    private readonly ToolStripMenuItem _speedItem;
    private readonly ToolStripMenuItem _autostartItem;
    private readonly Timer             _timer;
    private readonly Icon              _appIcon;

    private AppSettings  _settings;
    private SettingsForm _settingsForm;

    public SpeedmonApp()
    {
        _settings = AppSettings.Load();
        _monitor  = new NetworkMonitor();
        _appIcon  = CreateAppIcon();

        // ── Context menu (shared by widget and tray icon) ─────────────────────
        _speedItem = new ToolStripMenuItem("Wird gemessen …") { Enabled = false };
        _autostartItem = new ToolStripMenuItem("Mit Windows starten") { CheckOnClick = true };
        _autostartItem.Checked = IsAutostartEnabled();
        _autostartItem.CheckedChanged += delegate { SetAutostart(_autostartItem.Checked); };

        var details = new ToolStripMenuItem("Details anzeigen", null, delegate { ToggleFlyout(); });
        details.Font = new Font(details.Font, FontStyle.Bold);

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_speedItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(details);
        _menu.Items.Add("Einstellungen …", null, delegate { ShowSettings(); });
        _menu.Items.Add("Position zurücksetzen", null, delegate
        {
            _settings.Offset = 0;
            _settings.Save();
            _widget.ApplySettings(_settings);
        });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_autostartItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Beenden", null, delegate { ExitThread(); });

        // ── Taskbar widget ────────────────────────────────────────────────────
        _widget = new TaskbarWidget(_settings);
        _widget.Clicked         += delegate { ToggleFlyout(); };
        _widget.RightClicked    += delegate { _menu.Show(Cursor.Position); };
        _widget.OffsetCommitted += delegate
        {
            if (_settingsForm != null) _settingsForm.SyncOffset(_settingsForm.Result.Offset);
            else _settings.Save();
        };

        _flyout = new SpeedFlyout(_monitor, _settings);

        // ── Tray icon ─────────────────────────────────────────────────────────
        _trayIcon = new NotifyIcon
        {
            Icon             = _appIcon,
            Text             = AppName,
            ContextMenuStrip = _menu,
            Visible          = _settings.ShowTrayIcon,
        };
        _trayIcon.MouseClick += delegate(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) ToggleFlyout();
        };

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _widget.Reposition(true);

        _timer = new Timer { Interval = 1000 };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object sender, EventArgs e)
    {
        _monitor.Update();
        _widget.UpdateData(_monitor);

        string down = SpeedFormatter.Format(_monitor.Download, CurrentSettings.Unit);
        string up   = SpeedFormatter.Format(_monitor.Upload,   CurrentSettings.Unit);
        _speedItem.Text = "↓ " + down + "     ↑ " + up;

        string tip = "↓ " + down + "   ↑ " + up;
        _trayIcon.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;

        if (_flyout.Visible) _flyout.Invalidate();
    }

    /// <summary>The settings currently shown (live preview while the dialog is open).</summary>
    private AppSettings CurrentSettings
    {
        get { return _settingsForm != null ? _settingsForm.Result : _settings; }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // Fired when the user switches between light and dark mode.
        if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.VisualStyle)
            _widget.RefreshTheme();
    }

    private void ToggleFlyout()
    {
        _flyout.Toggle(_widget.Visible ? _widget.WidgetBounds : Rectangle.Empty, _widget.IsVerticalTaskbar);
    }

    private void ShowSettings()
    {
        if (_settingsForm != null)
        {
            _settingsForm.Activate();
            return;
        }

        _flyout.Hide();
        using (_settingsForm = new SettingsForm(_settings, IsAutostartEnabled(), _appIcon))
        {
            _settingsForm.Preview += delegate { Apply(_settingsForm.Result); };

            if (_settingsForm.ShowDialog() == DialogResult.OK)
            {
                _settings = _settingsForm.Result;
                _settings.Save();
                SetAutostart(_settingsForm.AutostartEnabled);
                _autostartItem.Checked = _settingsForm.AutostartEnabled;
            }
        }
        _settingsForm = null;
        Apply(_settings);
    }

    private void Apply(AppSettings s)
    {
        _widget.ApplySettings(s);
        _flyout.ApplySettings(s);
        _trayIcon.Visible = s.ShowTrayIcon;
    }

    // ── Autostart ─────────────────────────────────────────────────────────────

    private static bool IsAutostartEnabled()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutostartKey))
                return key != null && key.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    private static void SetAutostart(bool enabled)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutostartKey, true))
            {
                if (key == null) return;
                if (enabled) key.SetValue(AppName, "\"" + Application.ExecutablePath + "\"");
                else         key.DeleteValue(AppName, false);
            }
        }
        catch { /* No permission – leave as is */ }
    }

    // ── App icon (drawn at runtime, no .ico file needed) ──────────────────────

    private static Icon CreateAppIcon()
    {
        using (var bmp = new Bitmap(32, 32))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (GraphicsPath bg = TaskbarWidget.RoundedRect(new Rectangle(1, 1, 30, 30), 7))
                using (var brush = new SolidBrush(Color.FromArgb(32, 32, 32)))
                    g.FillPath(brush, bg);

                using (var down = new SolidBrush(Color.FromArgb(46, 204, 113)))
                    g.FillPolygon(down, new[] { new Point(5, 12), new Point(15, 12), new Point(10, 25) });
                using (var downStem = new SolidBrush(Color.FromArgb(46, 204, 113)))
                    g.FillRectangle(downStem, 8, 6, 4, 8);

                using (var up = new SolidBrush(Color.FromArgb(255, 159, 67)))
                    g.FillPolygon(up, new[] { new Point(17, 20), new Point(27, 20), new Point(22, 7) });
                using (var upStem = new SolidBrush(Color.FromArgb(255, 159, 67)))
                    g.FillRectangle(upStem, 20, 18, 4, 8);
            }

            IntPtr hIcon = bmp.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
            NativeMethods.DestroyIcon(hIcon);
            return icon;
        }
    }

    // ── Shutdown ──────────────────────────────────────────────────────────────

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _trayIcon.Visible = false;
        _flyout.Close();
        _widget.Close();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _trayIcon.Dispose();
            _menu.Dispose();
            _flyout.Dispose();
            _widget.Dispose();
            _appIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
