using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

enum WidgetLayout { TwoLines, OneLine }
enum SpeedUnit    { Bytes, Bits }
enum TextTheme    { Auto, Light, Dark }

class AppSettings
{
    public WidgetLayout Layout        { get; set; }
    public SpeedUnit    Unit          { get; set; }
    public TextTheme    TextTheme     { get; set; }
    public float        FontSize      { get; set; }
    public bool         BoldValues    { get; set; }
    public bool         ShowGraph     { get; set; }
    public Color        DownloadColor { get; set; }
    public Color        UploadColor   { get; set; }
    public int          Offset        { get; set; }   // Pixels left of the notification area
    public bool         ShowTrayIcon  { get; set; }
    public bool         HideOnFullscreen { get; set; }

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskBarSpeedmon");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.ini");

    public AppSettings()
    {
        Layout           = WidgetLayout.TwoLines;
        Unit             = SpeedUnit.Bytes;
        TextTheme        = TextTheme.Auto;
        FontSize         = 10f;
        BoldValues       = true;
        ShowGraph        = true;
        DownloadColor    = Color.FromArgb(46, 204, 113);   // green
        UploadColor      = Color.FromArgb(255, 159, 67);   // orange
        Offset           = 0;
        ShowTrayIcon     = true;
        HideOnFullscreen = true;
    }

    public AppSettings Clone()
    {
        return (AppSettings)MemberwiseClone();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Version=2");
            sb.AppendLine("Layout="           + Layout);
            sb.AppendLine("Unit="             + Unit);
            sb.AppendLine("TextTheme="        + TextTheme);
            sb.AppendLine("FontSize="         + FontSize.ToString(c));
            sb.AppendLine("BoldValues="       + BoldValues);
            sb.AppendLine("ShowGraph="        + ShowGraph);
            sb.AppendLine("DownloadColor="    + ColorTranslator.ToHtml(DownloadColor));
            sb.AppendLine("UploadColor="      + ColorTranslator.ToHtml(UploadColor));
            sb.AppendLine("Offset="           + Offset.ToString(c));
            sb.AppendLine("ShowTrayIcon="     + ShowTrayIcon);
            sb.AppendLine("HideOnFullscreen=" + HideOnFullscreen);
            File.WriteAllText(SettingsFile, sb.ToString(), Encoding.UTF8);
        }
        catch { /* Settings are a convenience; never crash over them */ }
    }

    public static AppSettings Load()
    {
        var s = new AppSettings();
        if (!File.Exists(SettingsFile)) return s;

        string[] lines;
        try { lines = File.ReadAllLines(SettingsFile, Encoding.UTF8); }
        catch { return s; }

        // Settings files from v1 used a different format; ignore them and start fresh.
        bool isV2 = false;
        foreach (string line in lines)
            if (line.Trim() == "Version=2") isV2 = true;
        if (!isV2) return s;

        var c = CultureInfo.InvariantCulture;
        foreach (string line in lines)
        {
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string key = line.Substring(0, eq).Trim();
            string val = line.Substring(eq + 1).Trim();

            try
            {
                switch (key)
                {
                    case "Layout":           s.Layout = (WidgetLayout)Enum.Parse(typeof(WidgetLayout), val); break;
                    case "Unit":             s.Unit = (SpeedUnit)Enum.Parse(typeof(SpeedUnit), val); break;
                    case "TextTheme":        s.TextTheme = (TextTheme)Enum.Parse(typeof(TextTheme), val); break;
                    case "FontSize":         s.FontSize = Clamp(float.Parse(val, c), 7f, 16f); break;
                    case "BoldValues":       s.BoldValues = bool.Parse(val); break;
                    case "ShowGraph":        s.ShowGraph = bool.Parse(val); break;
                    case "DownloadColor":    s.DownloadColor = ColorTranslator.FromHtml(val); break;
                    case "UploadColor":      s.UploadColor = ColorTranslator.FromHtml(val); break;
                    case "Offset":           s.Offset = Math.Max(0, int.Parse(val, c)); break;
                    case "ShowTrayIcon":     s.ShowTrayIcon = bool.Parse(val); break;
                    case "HideOnFullscreen": s.HideOnFullscreen = bool.Parse(val); break;
                }
            }
            catch { /* Keep default for malformed values */ }
        }
        return s;
    }

    private static float Clamp(float v, float min, float max)
    {
        return v < min ? min : (v > max ? max : v);
    }
}
