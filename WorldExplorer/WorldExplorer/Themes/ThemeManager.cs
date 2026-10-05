using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WorldExplorer.Themes;

public enum AppTheme
{
    Dark,
    Light
}

/// <summary>
/// Switches the colour dictionary at runtime and keeps the native Windows
/// title bar in step with it (dark caption on Windows 10 1809+/11).
/// </summary>
public static class ThemeManager
{
    private static readonly Uri DarkUri = new("pack://application:,,,/Themes/Dark.xaml", UriKind.Absolute);
    private static readonly Uri LightUri = new("pack://application:,,,/Themes/Light.xaml", UriKind.Absolute);

    public static AppTheme Current { get; private set; } = AppTheme.Dark;

    public static event EventHandler? ThemeChanged;

    public static AppTheme Parse(string? value)
        => string.Equals(value, nameof(AppTheme.Light), StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Light
            : AppTheme.Dark;

    /// <summary>
    /// Replaces the active colour dictionary (the first merged dictionary in
    /// App.xaml) and re-themes the title bars of every open window.
    /// </summary>
    public static void Apply(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        var dictionaries = app.Resources.MergedDictionaries;
        var colours = new ResourceDictionary { Source = theme == AppTheme.Light ? LightUri : DarkUri };

        // App.xaml declares the initial colours with a relative Source, so match
        // on the file name rather than the exact Uri.
        var existing = dictionaries.FirstOrDefault(IsColourDictionary);
        if (existing != null)
        {
            dictionaries[dictionaries.IndexOf(existing)] = colours;
        }
        else
        {
            dictionaries.Insert(0, colours);
        }

        Current = theme;
        foreach (Window window in app.Windows)
        {
            ApplyTitleBar(window);
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    private static bool IsColourDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        return source != null &&
               (source.EndsWith("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Call from a window's constructor: themes its title bar as soon as the
    /// native handle exists, before the window is first shown.
    /// </summary>
    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) => ApplyTitleBar(window);
    }

    private static void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        try
        {
            var dark = Current == AppTheme.Dark ? 1 : 0;
            // Attribute 20 on Windows 10 2004+ / 11, 19 on earlier Windows 10 builds.
            if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref dark, sizeof(int));
            }

            // Windows 11: tint the caption to match the app chrome. Ignored elsewhere.
            if (Application.Current?.TryFindResource("Brush.Chrome") is SolidColorBrush chrome)
            {
                var c = chrome.Color;
                var colorRef = c.R | (c.G << 8) | (c.B << 16);
                DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref colorRef, sizeof(int));
            }
        }
        catch (DllNotFoundException)
        {
            // Not running on a DWM-capable Windows (e.g. a compatibility layer).
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaCaptionColor = 35;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
