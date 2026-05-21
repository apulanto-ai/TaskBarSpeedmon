using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

class AppSettings
{
    public Color DownloadColor  { get; set; }
    public Color UploadColor    { get; set; }
    public Color BackgroundColor { get; set; }
    public float FontSize       { get; set; }
    public int   IconSize       { get; set; }

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskBarSpeedmon");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.ini");

    public AppSettings()
    {
        DownloadColor   = Color.FromArgb(60, 220, 100);
        UploadColor     = Color.FromArgb(255, 165, 0);
        BackgroundColor = Color.Black;
        FontSize        = 7f;
        IconSize        = 32;
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            DownloadColor   = DownloadColor,
            UploadColor     = UploadColor,
            BackgroundColor = BackgroundColor,
            FontSize        = FontSize,
            IconSize        = IconSize
        };
    }

    public void Save()
    {
        if (!Directory.Exists(SettingsDir))
            Directory.CreateDirectory(SettingsDir);

        using (var w = new StreamWriter(SettingsFile, false, Encoding.UTF8))
        {
            w.WriteLine("DownloadColor="   + DownloadColor.ToArgb().ToString(CultureInfo.InvariantCulture));
            w.WriteLine("UploadColor="     + UploadColor.ToArgb().ToString(CultureInfo.InvariantCulture));
            w.WriteLine("BackgroundColor=" + BackgroundColor.ToArgb().ToString(CultureInfo.InvariantCulture));
            w.WriteLine("FontSize="        + FontSize.ToString(CultureInfo.InvariantCulture));
            w.WriteLine("IconSize="        + IconSize.ToString(CultureInfo.InvariantCulture));
        }
    }

    public static AppSettings Load()
    {
        var s = new AppSettings();
        if (!File.Exists(SettingsFile)) return s;

        try
        {
            foreach (string line in File.ReadAllLines(SettingsFile, Encoding.UTF8))
            {
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "DownloadColor":
                        s.DownloadColor = Color.FromArgb(int.Parse(val, CultureInfo.InvariantCulture));
                        break;
                    case "UploadColor":
                        s.UploadColor = Color.FromArgb(int.Parse(val, CultureInfo.InvariantCulture));
                        break;
                    case "BackgroundColor":
                        s.BackgroundColor = Color.FromArgb(int.Parse(val, CultureInfo.InvariantCulture));
                        break;
                    case "FontSize":
                        s.FontSize = float.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case "IconSize":
                        s.IconSize = int.Parse(val, CultureInfo.InvariantCulture);
                        break;
                }
            }
        }
        catch { }

        return s;
    }
}
