using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

/// <summary>
/// A borderless, per-pixel transparent window that sits directly on the taskbar,
/// next to the notification area, and shows the current download/upload rate in
/// a readable size. It can be dragged along the taskbar with the mouse.
/// </summary>
class TaskbarWidget : Form
{
    public event EventHandler Clicked;
    public event EventHandler RightClicked;
    public event EventHandler OffsetCommitted;

    private readonly Timer _positionTimer;
    private AppSettings _settings;
    private Theme       _theme;
    private float       _scale = 1f;

    private double   _download, _upload;
    private double[] _downHistory = new double[0];
    private double[] _upHistory   = new double[0];

    private Rectangle _bounds = Rectangle.Empty;
    private bool      _vertical;
    private bool      _hover;

    // Dragging
    private bool  _mouseDown;
    private bool  _dragging;
    private Point _dragStart;
    private int   _dragStartOffset;

    public TaskbarWidget(AppSettings settings)
    {
        _settings = settings;
        _theme    = Theme.Resolve(settings.TextTheme);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.Manual;
        TopMost         = true;
        Text            = "TaskBarSpeedmon";

        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        _positionTimer = new Timer { Interval = 250 };
        _positionTimer.Tick += delegate { Reposition(false); };
        _positionTimer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.Style    = NativeMethods.WS_POPUP;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW
                        | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    protected override void WndProc(ref Message m)
    {
        // Never steal the focus from the user's active window when clicked.
        if (m.Msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            m.Result = new IntPtr(NativeMethods.MA_NOACTIVATE);
            return;
        }
        base.WndProc(ref m);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _theme    = Theme.Resolve(settings.TextTheme);
        Reposition(true);
    }

    public void RefreshTheme()
    {
        _theme = Theme.Resolve(_settings.TextTheme);
        Render();
    }

    public Theme CurrentTheme
    {
        get { return _theme; }
    }

    public void UpdateData(NetworkMonitor monitor)
    {
        _download    = monitor.Download;
        _upload      = monitor.Upload;
        _downHistory = monitor.GetDownloadHistory();
        _upHistory   = monitor.GetUploadHistory();
        Render();
    }

    /// <summary>Screen rectangle the widget currently occupies.</summary>
    public Rectangle WidgetBounds
    {
        get { return _bounds; }
    }

    public bool IsVerticalTaskbar
    {
        get { return _vertical; }
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private Font CreateValueFont()
    {
        float px = _settings.FontSize * 96f / 72f * _scale;
        return new Font("Segoe UI", px, _settings.BoldValues ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private Font CreateArrowFont()
    {
        float px = _settings.FontSize * 96f / 72f * _scale;
        return new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private bool UseTwoLines
    {
        get { return _vertical || _settings.Layout == WidgetLayout.TwoLines; }
    }

    private int S(float v)
    {
        return (int)Math.Round(v * _scale);
    }

    /// <summary>Content size (independent of taskbar thickness).</summary>
    private Size MeasureContent(out int arrowWidth, out int valueWidth, out int lineHeight)
    {
        using (var bmp = new Bitmap(1, 1))
        using (Graphics g = Graphics.FromImage(bmp))
        using (Font vf = CreateValueFont())
        using (Font af = CreateArrowFont())
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            StringFormat sf = StringFormat.GenericTypographic;

            arrowWidth = (int)Math.Ceiling(g.MeasureString("↓", af, 1000, sf).Width);
            float w = 0;
            foreach (string sample in SpeedFormatter.WidestSamples(_settings.Unit))
                w = Math.Max(w, g.MeasureString(sample, vf, 1000, sf).Width);
            valueWidth = (int)Math.Ceiling(w);
            lineHeight = (int)Math.Ceiling(vf.GetHeight(g));
        }

        int pad = S(8), gap = S(5);
        if (UseTwoLines)
            return new Size(pad + arrowWidth + gap + valueWidth + pad, lineHeight * 2 + S(6));
        return new Size(pad + (arrowWidth + gap + valueWidth) * 2 + S(14) + pad, lineHeight + S(6));
    }

    /// <summary>Finds the taskbar and places the widget next to the notification area.</summary>
    public void Reposition(bool forceRender)
    {
        Rectangle target;
        bool visible = ComputeTarget(out target);

        if (!visible)
        {
            if (Visible) Hide();
            return;
        }

        bool changed = target != _bounds;
        _bounds = target;
        if (changed) Bounds = target;

        if (!Visible)
        {
            Render();
            Show();
            changed = false;
        }

        if (changed || forceRender) Render();

        // The taskbar raises itself when clicked; keep the widget above it.
        NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private bool ComputeTarget(out Rectangle target)
    {
        target = Rectangle.Empty;

        if (_settings.HideOnFullscreen && NativeMethods.IsFullscreenAppRunning())
            return false;

        IntPtr tray = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (tray == IntPtr.Zero || !NativeMethods.IsWindowVisible(tray)) return false;

        NativeMethods.RECT tr;
        if (!NativeMethods.GetWindowRect(tray, out tr)) return false;
        Rectangle taskbar = Rectangle.FromLTRB(tr.Left, tr.Top, tr.Right, tr.Bottom);

        // Auto-hidden taskbar: (almost) entirely off-screen.
        Rectangle screen = Screen.FromRectangle(taskbar).Bounds;
        Rectangle onScreen = Rectangle.Intersect(screen, taskbar);
        if (onScreen.Width < S(8) || onScreen.Height < S(8)) return false;

        _vertical = taskbar.Height > taskbar.Width;

        Rectangle notify = Rectangle.Empty;
        IntPtr notifyWnd = NativeMethods.FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
        NativeMethods.RECT nr;
        if (notifyWnd != IntPtr.Zero && NativeMethods.GetWindowRect(notifyWnd, out nr))
        {
            notify = Rectangle.FromLTRB(nr.Left, nr.Top, nr.Right, nr.Bottom);
            if (notify.Width <= 0 || notify.Height <= 0 || !taskbar.Contains(notify))
                notify = Rectangle.Empty;
        }

        int aw, vw, lh;
        Size content = MeasureContent(out aw, out vw, out lh);
        int offset = _settings.Offset;

        if (!_vertical)
        {
            int right = notify != Rectangle.Empty ? notify.Left : taskbar.Right - S(220);
            right -= offset;
            int width = content.Width;
            int x = Math.Max(taskbar.Left, right - width);
            target = new Rectangle(x, taskbar.Top, width, taskbar.Height);
        }
        else
        {
            int bottom = notify != Rectangle.Empty ? notify.Top : taskbar.Bottom - S(220);
            bottom -= offset;
            int height = content.Height + S(10);
            int y = Math.Max(taskbar.Top, bottom - height);
            target = new Rectangle(taskbar.Left, y, taskbar.Width, height);
        }
        return true;
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    private void Render()
    {
        if (_bounds.Width <= 0 || _bounds.Height <= 0) return;

        using (var bmp = new Bitmap(_bounds.Width, _bounds.Height, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bmp))
                DrawWidget(g, _bounds.Size);
            PushToScreen(bmp);
        }
    }

    private void DrawWidget(Graphics g, Size size)
    {
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        int inset = S(3);
        Rectangle card = _vertical
            ? new Rectangle(inset, 0, size.Width - inset * 2, size.Height)
            : new Rectangle(0, inset, size.Width, size.Height - inset * 2);

        // Background: nearly invisible so the whole area stays clickable, subtle highlight on hover.
        int bgAlpha = (_hover || _dragging) ? (_theme.IsDark ? 28 : 22) : 1;
        using (GraphicsPath path = RoundedRect(card, S(5)))
        using (var bg = new SolidBrush(Color.FromArgb(bgAlpha, _theme.IsDark ? Color.White : Color.Black)))
            g.FillPath(bg, path);

        if (_settings.ShowGraph)
            PaintGraph(g, card);

        int aw, vw, lh;
        Size content = MeasureContent(out aw, out vw, out lh);

        using (Font vf = CreateValueFont())
        using (Font af = CreateArrowFont())
        using (var textBrush = new SolidBrush(_theme.Text))
        using (var downBrush = new SolidBrush(_settings.DownloadColor))
        using (var upBrush   = new SolidBrush(_settings.UploadColor))
        {
            StringFormat sf = StringFormat.GenericTypographic;
            int pad = S(8), gap = S(5);
            string down = SpeedFormatter.Format(_download, _settings.Unit);
            string up   = SpeedFormatter.Format(_upload,   _settings.Unit);

            int left = (size.Width - content.Width) / 2 + pad;

            if (UseTwoLines)
            {
                float top = (size.Height - lh * 2) / 2f;
                DrawRow(g, "↓", down, af, vf, downBrush, textBrush, sf, left, top, aw, gap, vw);
                DrawRow(g, "↑", up,   af, vf, upBrush,   textBrush, sf, left, top + lh, aw, gap, vw);
            }
            else
            {
                float top = (size.Height - lh) / 2f;
                DrawRow(g, "↓", down, af, vf, downBrush, textBrush, sf, left, top, aw, gap, vw);
                DrawRow(g, "↑", up,   af, vf, upBrush,   textBrush, sf, left + aw + gap + vw + S(14), top, aw, gap, vw);
            }
        }
    }

    private static void DrawRow(Graphics g, string arrow, string value, Font af, Font vf,
        Brush arrowBrush, Brush textBrush, StringFormat sf, float x, float y, int aw, int gap, int vw)
    {
        g.DrawString(arrow, af, arrowBrush, x, y, sf);
        // Right-align the value so digits stay put as the number changes.
        float w = g.MeasureString(value, vf, 1000, sf).Width;
        g.DrawString(value, vf, textBrush, x + aw + gap + (vw - w), y, sf);
    }

    private void PaintGraph(Graphics g, Rectangle area)
    {
        int n = _downHistory.Length;
        if (n < 2) return;

        double max = 16 * 1024; // Don't amplify background noise into a full-height graph.
        for (int i = 0; i < n; i++)
            max = Math.Max(max, Math.Max(_downHistory[i], _upHistory[i]));

        Rectangle r = Rectangle.Inflate(area, -S(2), -S(2));
        float graphHeight = r.Height * 0.75f;

        PointF[] downPts = new PointF[n + 2];
        PointF[] upPts   = new PointF[n];
        for (int i = 0; i < n; i++)
        {
            float x = r.Left + r.Width * i / (float)(n - 1);
            downPts[i] = new PointF(x, r.Bottom - (float)(_downHistory[i] / max) * graphHeight);
            upPts[i]   = new PointF(x, r.Bottom - (float)(_upHistory[i]   / max) * graphHeight);
        }
        downPts[n]     = new PointF(r.Right, r.Bottom);
        downPts[n + 1] = new PointF(r.Left,  r.Bottom);

        Region oldClip = g.Clip;
        using (GraphicsPath clip = RoundedRect(area, S(5)))
        {
            g.SetClip(clip);
            using (var fill = new SolidBrush(Color.FromArgb(_theme.IsDark ? 50 : 40, _settings.DownloadColor)))
                g.FillPolygon(fill, downPts);
            using (var pen = new Pen(Color.FromArgb(_theme.IsDark ? 110 : 140, _settings.UploadColor), Math.Max(1f, _scale)))
                g.DrawLines(pen, upPts);
        }
        g.Clip = oldClip;
    }

    private void PushToScreen(Bitmap bmp)
    {
        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memDc    = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr hBitmap  = IntPtr.Zero;
        IntPtr oldBmp   = IntPtr.Zero;

        try
        {
            hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            oldBmp  = NativeMethods.SelectObject(memDc, hBitmap);

            var size   = new NativeMethods.SIZE(bmp.Width, bmp.Height);
            var src    = new NativeMethods.POINT(0, 0);
            var dst    = new NativeMethods.POINT(_bounds.Left, _bounds.Top);
            var blend  = new NativeMethods.BLENDFUNCTION
            {
                BlendOp             = NativeMethods.AC_SRC_OVER,
                BlendFlags          = 0,
                SourceConstantAlpha = 255,
                AlphaFormat         = NativeMethods.AC_SRC_ALPHA
            };

            NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0,
                ref blend, NativeMethods.ULW_ALPHA);
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero)
            {
                NativeMethods.SelectObject(memDc, oldBmp);
                NativeMethods.DeleteObject(hBitmap);
            }
            NativeMethods.DeleteDC(memDc);
        }
    }

    internal static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Mouse handling ────────────────────────────────────────────────────────

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Render();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Render();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _mouseDown       = true;
        _dragging        = false;
        _dragStart       = Cursor.Position;
        _dragStartOffset = _settings.Offset;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_mouseDown) return;

        Point p = Cursor.Position;
        int delta = _vertical ? _dragStart.Y - p.Y : _dragStart.X - p.X;

        if (!_dragging && Math.Abs(delta) < S(5)) return;
        _dragging = true;
        Cursor = _vertical ? Cursors.SizeNS : Cursors.SizeWE;

        _settings.Offset = Math.Max(0, _dragStartOffset + delta);
        Reposition(true);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Right)
        {
            if (RightClicked != null) RightClicked(this, EventArgs.Empty);
            return;
        }
        if (e.Button != MouseButtons.Left || !_mouseDown) return;

        _mouseDown = false;
        if (_dragging)
        {
            _dragging = false;
            Cursor = Cursors.Default;
            Render();
            if (OffsetCommitted != null) OffsetCommitted(this, EventArgs.Empty);
        }
        else if (Clicked != null)
        {
            Clicked(this, EventArgs.Empty);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _positionTimer.Dispose();
        base.Dispose(disposing);
    }
}
