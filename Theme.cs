using System;
using System.Drawing;
using Microsoft.Win32;

/// <summary>Resolves the colors used by the widget and the flyout.</summary>
class Theme
{
    public bool  IsDark        { get; private set; }
    public Color Text          { get; private set; }
    public Color TextSecondary { get; private set; }
    public Color Background    { get; private set; }
    public Color Surface       { get; private set; }
    public Color GridLine      { get; private set; }
    public Color Border        { get; private set; }

    public static Theme Resolve(TextTheme setting)
    {
        bool dark;
        switch (setting)
        {
            case TextTheme.Light: dark = true;  break;   // light text => dark surroundings
            case TextTheme.Dark:  dark = false; break;
            default:              dark = !IsSystemLight(); break;
        }
        return dark ? CreateDark() : CreateLight();
    }

    private static Theme CreateDark()
    {
        return new Theme
        {
            IsDark        = true,
            Text          = Color.FromArgb(245, 245, 245),
            TextSecondary = Color.FromArgb(165, 165, 165),
            Background    = Color.FromArgb(32, 32, 32),
            Surface       = Color.FromArgb(43, 43, 43),
            GridLine      = Color.FromArgb(58, 58, 58),
            Border        = Color.FromArgb(70, 70, 70),
        };
    }

    private static Theme CreateLight()
    {
        return new Theme
        {
            IsDark        = false,
            Text          = Color.FromArgb(26, 26, 26),
            TextSecondary = Color.FromArgb(96, 96, 96),
            Background    = Color.FromArgb(249, 249, 249),
            Surface       = Color.FromArgb(238, 238, 238),
            GridLine      = Color.FromArgb(222, 222, 222),
            Border        = Color.FromArgb(200, 200, 200),
        };
    }

    /// <summary>True when Windows uses the light theme for the taskbar.</summary>
    private static bool IsSystemLight()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key == null) return false;
                object v = key.GetValue("SystemUsesLightTheme");
                return v is int && (int)v != 0;
            }
        }
        catch { return false; }
    }
}
