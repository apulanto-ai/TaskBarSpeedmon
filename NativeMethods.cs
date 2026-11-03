using System;
using System.Runtime.InteropServices;

static class NativeMethods
{
    // ── Window styles ─────────────────────────────────────────────────────────
    public const int WS_POPUP          = unchecked((int)0x80000000);
    public const int WS_EX_LAYERED     = 0x00080000;
    public const int WS_EX_TOOLWINDOW  = 0x00000080;
    public const int WS_EX_TOPMOST     = 0x00000008;
    public const int WS_EX_NOACTIVATE  = 0x08000000;

    // ── SetWindowPos ──────────────────────────────────────────────────────────
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public const uint SWP_NOSIZE       = 0x0001;
    public const uint SWP_NOMOVE       = 0x0002;
    public const uint SWP_NOACTIVATE   = 0x0010;
    public const uint SWP_SHOWWINDOW   = 0x0040;

    // ── UpdateLayeredWindow ───────────────────────────────────────────────────
    public const int  ULW_ALPHA        = 0x02;
    public const byte AC_SRC_OVER      = 0x00;
    public const byte AC_SRC_ALPHA     = 0x01;

    // ── Messages ──────────────────────────────────────────────────────────────
    public const int WM_MOUSEACTIVATE  = 0x0021;
    public const int MA_NOACTIVATE     = 3;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int Width, Height;
        public SIZE(int w, int h) { Width = w; Height = h; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width  { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst,
        ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowName);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr handle);

    // ── Fullscreen detection ──────────────────────────────────────────────────
    public const int QUNS_BUSY                    = 2;
    public const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
    public const int QUNS_PRESENTATION_MODE       = 4;

    [DllImport("shell32.dll")]
    public static extern int SHQueryUserNotificationState(out int pquns);

    // ── DWM (rounded corners / dark title on Windows 11) ──────────────────────
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE   = 20;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE  = 33;
    public const int DWMWCP_ROUND                    = 2;

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void TrySetDwmAttribute(IntPtr hwnd, int attr, int value)
    {
        try { DwmSetWindowAttribute(hwnd, attr, ref value, sizeof(int)); }
        catch { /* Not available on this Windows version */ }
    }

    public static bool IsFullscreenAppRunning()
    {
        try
        {
            int state;
            if (SHQueryUserNotificationState(out state) != 0) return false;
            return state == QUNS_BUSY || state == QUNS_RUNNING_D3D_FULL_SCREEN || state == QUNS_PRESENTATION_MODE;
        }
        catch { return false; }
    }
}
