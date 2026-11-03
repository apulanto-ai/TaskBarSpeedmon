using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

/// <summary>
/// Windows-11-style flyout that opens above the widget: large current values,
/// a 60-second history graph, peaks and session totals.
/// </summary>
class SpeedFlyout : Form
{
    private readonly NetworkMonitor _monitor;
    private AppSettings _settings;
    private Theme       _theme;
    private readonly float _scale;
    private DateTime    _lastHidden = DateTime.MinValue;

    public SpeedFlyout(NetworkMonitor monitor, AppSettings settings)
    {
        _monitor  = monitor;
        _settings = settings;
        _theme    = Theme.Resolve(settings.TextTheme);

        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;
        TopMost         = true;
        Text            = "Netzwerk";
        ClientSize      = new Size(S(360), S(300));
        DoubleBuffered  = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.TrySetDwmAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, NativeMethods.DWMWCP_ROUND);
    }

    private int S(float v)
    {
        return (int)Math.Round(v * _scale);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _theme    = Theme.Resolve(settings.TextTheme);
        if (Visible) Invalidate();
    }

    /// <summary>Opens the flyout next to the given widget rectangle, or closes it if open.</summary>
    public void Toggle(Rectangle anchor, bool verticalTaskbar)
    {
        if (Visible)
        {
            Hide();
            return;
        }
        // A click on the widget first deactivates (and hides) the flyout; don't reopen it immediately.
        if ((DateTime.Now - _lastHidden).TotalMilliseconds < 300) return;

        _theme = Theme.Resolve(_settings.TextTheme);
        BackColor = _theme.Background;
        PlaceNear(anchor, verticalTaskbar);
        Show();
        Activate();
    }

    private void PlaceNear(Rectangle anchor, bool vertical)
    {
        Rectangle wa = Screen.FromRectangle(anchor.IsEmpty ? new Rectangle(Cursor.Position, new Size(1, 1)) : anchor).WorkingArea;
        int margin = S(12);
        int x, y;

        if (anchor.IsEmpty)
        {
            x = wa.Right - Width - margin;
            y = wa.Bottom - Height - margin;
        }
        else if (!vertical)
        {
            x = anchor.Right - Width;
            y = anchor.Top >= wa.Bottom - S(4) || anchor.Top > wa.Top + wa.Height / 2
                ? wa.Bottom - Height - margin      // taskbar at the bottom
                : wa.Top + margin;                 // taskbar at the top
        }
        else
        {
            y = anchor.Bottom - Height;
            x = anchor.Left > wa.Left + wa.Width / 2
                ? wa.Right - Width - margin        // taskbar on the right
                : wa.Left + margin;                // taskbar on the left
        }

        x = Math.Max(wa.Left + margin, Math.Min(x, wa.Right - Width - margin));
        y = Math.Max(wa.Top + margin, Math.Min(y, wa.Bottom - Height - margin));
        Location = new Point(x, y);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
        _lastHidden = DateTime.Now;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) Hide();
    }

    // ── Painting ──────────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(_theme.Background);

        int pad = S(20);
        int w   = ClientSize.Width;

        using (var title   = new Font("Segoe UI Semibold", S(14), GraphicsUnit.Pixel))
        using (var caption = new Font("Segoe UI", S(12), GraphicsUnit.Pixel))
        using (var big     = new Font("Segoe UI Semibold", S(26), GraphicsUnit.Pixel))
        using (var text    = new SolidBrush(_theme.Text))
        using (var muted   = new SolidBrush(_theme.TextSecondary))
        using (var down    = new SolidBrush(_settings.DownloadColor))
        using (var up      = new SolidBrush(_settings.UploadColor))
        {
            // Header
            g.DrawString("Netzwerk", title, text, pad, S(16));
            var right = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString("Letzte 60 Sekunden", caption, muted, new RectangleF(pad, S(18), w - pad * 2, S(20)), right);

            // Current values
            int colW = (w - pad * 2) / 2;
            int y = S(48);
            DrawValue(g, "Download", _monitor.Download, pad,        y, caption, big, muted, text, down);
            DrawValue(g, "Upload",   _monitor.Upload,   pad + colW, y, caption, big, muted, text, up);

            // Graph
            var graph = new Rectangle(pad, S(120), w - pad * 2, S(100));
            DrawGraph(g, graph, caption, muted);

            // Footer
            int fy = graph.Bottom + S(14);
            string totals = "Seit Start:   ↓ " + SpeedFormatter.FormatBytes(_monitor.TotalReceived)
                          + "   ↑ " + SpeedFormatter.FormatBytes(_monitor.TotalSent);
            string peaks  = "Spitzenwert:  ↓ " + SpeedFormatter.Format(_monitor.PeakDownload, _settings.Unit)
                          + "   ↑ " + SpeedFormatter.Format(_monitor.PeakUpload, _settings.Unit);
            g.DrawString(totals, caption, muted, pad, fy);
            g.DrawString(peaks,  caption, muted, pad, fy + S(20));
        }

        if (Environment.OSVersion.Version.Build < 22000)
        {
            // Windows 10 has no DWM border for borderless windows; draw a thin one.
            using (var pen = new Pen(_theme.Border))
                g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
    }

    private void DrawValue(Graphics g, string label, double value, int x, int y,
        Font caption, Font big, Brush muted, Brush text, Brush accent)
    {
        int dot = S(8);
        g.FillEllipse(accent, x, y + S(5), dot, dot);
        g.DrawString(label, caption, muted, x + dot + S(6), y);
        g.DrawString(SpeedFormatter.Format(value, _settings.Unit), big, text, x - S(3), y + S(18));
    }

    private void DrawGraph(Graphics g, Rectangle r, Font caption, Brush muted)
    {
        using (GraphicsPath bg = TaskbarWidget.RoundedRect(r, S(6)))
        using (var surface = new SolidBrush(_theme.Surface))
            g.FillPath(surface, bg);

        double[] downH = _monitor.GetDownloadHistory();
        double[] upH   = _monitor.GetUploadHistory();
        int n = downH.Length;

        double max = 16 * 1024;
        for (int i = 0; i < n; i++)
            max = Math.Max(max, Math.Max(downH[i], upH[i]));
        max = SpeedFormatter.NiceCeiling(max, _settings.Unit);

        Rectangle plot = Rectangle.Inflate(r, -S(8), -S(8));

        // Grid lines at 50% and 100%
        using (var grid = new Pen(_theme.GridLine) { DashStyle = DashStyle.Dash })
        {
            g.DrawLine(grid, plot.Left, plot.Top, plot.Right, plot.Top);
            g.DrawLine(grid, plot.Left, plot.Top + plot.Height / 2, plot.Right, plot.Top + plot.Height / 2);
        }
        var right = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString(SpeedFormatter.Format(max, _settings.Unit), caption, muted,
            new RectangleF(plot.Left, plot.Top + S(2), plot.Width, S(18)), right);

        DrawSeries(g, plot, downH, max, _settings.DownloadColor, true);
        DrawSeries(g, plot, upH,   max, _settings.UploadColor,   false);
    }

    private void DrawSeries(Graphics g, Rectangle plot, double[] data, double max, Color color, bool filled)
    {
        int n = data.Length;
        if (n < 2) return;

        var pts = new PointF[n];
        for (int i = 0; i < n; i++)
        {
            float x = plot.Left + plot.Width * i / (float)(n - 1);
            float y = plot.Bottom - (float)(data[i] / max) * plot.Height;
            pts[i] = new PointF(x, y);
        }

        if (filled)
        {
            var poly = new PointF[n + 2];
            Array.Copy(pts, poly, n);
            poly[n]     = new PointF(plot.Right, plot.Bottom);
            poly[n + 1] = new PointF(plot.Left,  plot.Bottom);
            using (var fill = new LinearGradientBrush(plot, Color.FromArgb(110, color), Color.FromArgb(10, color),
                LinearGradientMode.Vertical))
                g.FillPolygon(fill, poly);
        }

        using (var pen = new Pen(color, 2f * _scale) { LineJoin = LineJoin.Round })
            g.DrawLines(pen, pts);
    }
}
