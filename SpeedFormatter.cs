using System;
using System.Globalization;

static class SpeedFormatter
{
    private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;

    /// <summary>Formats a rate, e.g. "12,4 MB/s" or "99,2 Mbit/s".</summary>
    public static string Format(double bytesPerSec, SpeedUnit unit)
    {
        string[] units;
        double value, step;

        if (unit == SpeedUnit.Bits)
        {
            value = bytesPerSec * 8;
            step  = 1000;
            units = new[] { "bit/s", "kbit/s", "Mbit/s", "Gbit/s" };
        }
        else
        {
            value = bytesPerSec;
            step  = 1024;
            units = new[] { "B/s", "KB/s", "MB/s", "GB/s" };
        }

        int i = 0;
        while (value >= 999.5 && i < units.Length - 1)
        {
            value /= step;
            i++;
        }

        string number;
        if (i == 0)           number = value.ToString("0", Culture);
        else if (value < 10)  number = value.ToString("0.0", Culture);
        else                  number = value.ToString("0", Culture);

        return number + " " + units[i];
    }

    /// <summary>Formats an amount of data, e.g. "1,23 GB".</summary>
    public static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int i = 0;
        while (value >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }
        string fmt = i == 0 ? "0" : (value < 10 ? "0.00" : (value < 100 ? "0.0" : "0"));
        return value.ToString(fmt, Culture) + " " + units[i];
    }

    /// <summary>Candidates for the widest string the widget can show, used to keep its width stable.</summary>
    public static string[] WidestSamples(SpeedUnit unit)
    {
        return unit == SpeedUnit.Bits
            ? new[] { "888 kbit/s", "888 Mbit/s", "8,8 Gbit/s" }
            : new[] { "888 KB/s", "888 MB/s", "8,8 GB/s", "888 B/s" };
    }

    /// <summary>
    /// Rounds a rate up to 1, 2, 5 × 10^n in the displayed unit, so axis labels read
    /// "5 MB/s" instead of "4,8 MB/s".
    /// </summary>
    public static double NiceCeiling(double bytesPerSec, SpeedUnit unit)
    {
        double factor = unit == SpeedUnit.Bits ? 8 : 1;
        double step   = unit == SpeedUnit.Bits ? 1000 : 1024;
        double value  = bytesPerSec * factor;
        double scale  = 1;
        while (value >= 999.5) { value /= step; scale *= step; }

        double exp = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(value, 1e-9))));
        double f = value / exp;
        double nice = f <= 1 ? 1 : f <= 2 ? 2 : f <= 5 ? 5 : 10;
        return nice * exp * scale / factor;
    }
}
