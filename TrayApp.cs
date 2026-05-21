using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

class TrayApp : ApplicationContext
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    private readonly NotifyIcon         _trayIcon;
    private readonly Timer              _timer;
    private readonly NetworkMonitor     _monitor;
    private readonly ToolStripMenuItem  _speedItem;
    private readonly ToolStripMenuItem  _autostartItem;

    private AppSettings _settings;
    private double      _lastDownload;
    private double      _lastUpload;

    private const string APP_NAME    = "TaskBarSpeedmon";
    private const string AUTOSTART_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public TrayApp()
    {
        _settings = AppSettings.Load();
        _monitor  = new NetworkMonitor();

        _speedItem = new ToolStripMenuItem("Wird geladen...");
        _speedItem.Enabled = false;
        _speedItem.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        _autostartItem = new ToolStripMenuItem("Mit Windows starten");
        _autostartItem.CheckOnClick = true;
        _autostartItem.Checked = IsAutostartEnabled();
        _autostartItem.CheckedChanged += OnAutostartChanged;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_speedItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Einstellungen...", null, OnSettingsClick);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_autostartItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Beenden", null, OnExit);

        _trayIcon = new NotifyIcon();
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.Visible = true;
        _trayIcon.Icon = CreateIcon(0, 0, _settings);
        _trayIcon.Text = "Netzwerk Monitor";

        _timer = new Timer();
        _timer.Interval = 1000;
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        _monitor.GetSpeeds(out _lastDownload, out _lastUpload);

        string dlStr = FormatSpeed(_lastDownload);
        string ulStr = FormatSpeed(_lastUpload);

        string tooltip = string.Format("↓ {0}  ↑ {1}", dlStr, ulStr);
        if (tooltip.Length > 63) tooltip = tooltip.Substring(0, 63);
        _trayIcon.Text = tooltip;

        _speedItem.Text = string.Format("↓ {0}   ↑ {1}", dlStr, ulStr);

        Icon oldIcon = _trayIcon.Icon;
        _trayIcon.Icon = CreateIcon(_lastDownload, _lastUpload, _settings);
        if (oldIcon != null) oldIcon.Dispose();
    }

    private void OnSettingsClick(object sender, EventArgs e)
    {
        using (var form = new SettingsForm(_settings))
        {
            if (form.ShowDialog() != DialogResult.OK) return;
            _settings = form.Result;

            Icon oldIcon = _trayIcon.Icon;
            _trayIcon.Icon = CreateIcon(_lastDownload, _lastUpload, _settings);
            if (oldIcon != null) oldIcon.Dispose();
        }
    }

    // ── Speed formatting ──────────────────────────────────────────────────────
    private static string FormatSpeed(double bytesPerSec)
    {
        if (bytesPerSec < 1024)
            return "< 1 KB/s";
        if (bytesPerSec < 1024 * 1024)
            return string.Format("{0:F0} KB/s", bytesPerSec / 1024.0);
        if (bytesPerSec < 1024.0 * 1024 * 1024)
            return string.Format("{0:F1} MB/s", bytesPerSec / (1024.0 * 1024));
        return string.Format("{0:F2} GB/s", bytesPerSec / (1024.0 * 1024 * 1024));
    }

    private static string FormatSpeedShort(double bytesPerSec)
    {
        if (bytesPerSec < 1024)
            return "0K";
        double kb = bytesPerSec / 1024.0;
        if (kb < 1000)
            return string.Format("{0:F0}K", kb);
        double mb = bytesPerSec / (1024.0 * 1024);
        if (mb < 10.0)
            return string.Format("{0:F1}M", mb);
        if (mb < 1000)
            return string.Format("{0:F0}M", mb);
        return string.Format("{0:F1}G", bytesPerSec / (1024.0 * 1024 * 1024));
    }

    // ── Icon rendering ────────────────────────────────────────────────────────
    private static Icon CreateIcon(double download, double upload, AppSettings s)
    {
        int sz = s.IconSize;
        var bmp = new Bitmap(sz, sz);
        using (var g = Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            g.Clear(s.BackgroundColor);

            using (var font   = new Font("Segoe UI", s.FontSize, FontStyle.Bold))
            using (var dlBrush = new SolidBrush(s.DownloadColor))
            using (var ulBrush = new SolidBrush(s.UploadColor))
            {
                string dlText = "↓" + FormatSpeedShort(download);
                string ulText = "↑" + FormatSpeedShort(upload);

                SizeF dlSize = g.MeasureString(dlText, font);
                SizeF ulSize = g.MeasureString(ulText, font);

                g.DrawString(dlText, font, dlBrush, Math.Max(0f, (sz - dlSize.Width) / 2f), 1f);
                g.DrawString(ulText, font, ulBrush, Math.Max(0f, (sz - ulSize.Width) / 2f), sz / 2f + 1f);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return icon;
    }

    // ── Autostart ─────────────────────────────────────────────────────────────
    private static bool IsAutostartEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AUTOSTART_KEY))
        {
            if (key == null) return false;
            return key.GetValue(APP_NAME) != null;
        }
    }

    private void OnAutostartChanged(object sender, EventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(AUTOSTART_KEY, true);
        if (key == null) return;
        using (key)
        {
            if (_autostartItem.Checked)
                key.SetValue(APP_NAME, "\"" + Application.ExecutablePath + "\"");
            else
                key.DeleteValue(APP_NAME, false);
        }
    }

    // ── Exit & Dispose ────────────────────────────────────────────────────────
    private void OnExit(object sender, EventArgs e)
    {
        _timer.Stop();
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_timer != null) _timer.Dispose();
            if (_trayIcon != null) _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
