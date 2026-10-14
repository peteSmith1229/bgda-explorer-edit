using JetBlackEngineLib.Data.Textures;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WorldExplorer.Themes;
using WorldExplorer.Tools;

namespace WorldExplorer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static readonly string SettingsFileName = "settings.ini";
    public static Section Settings = new();

    /// <summary>
    /// "Force opaque (ignore alpha)": on unless the user turned it off, so
    /// textures with stray alpha values don't render half-transparent.
    /// </summary>
    public static bool ForceOpaqueSetting => Settings.Get("Textures.ForceOpaque", true);

    /// <summary>How many bytes a hex dump in Details shows unless the user changes it.</summary>
    public const int DefaultHexDumpLength = 256;

    /// <summary>Fewest and most bytes the Hex dump length setting allows (a line of the dump is 16 bytes).</summary>
    public const int MinHexDumpLength = 16;
    public const int MaxHexDumpLength = 1024 * 1024;

    /// <summary>
    /// "Hex dump length": how many bytes of an entry the Details view shows as
    /// a hex dump when there is no decoder for it or decoding it fails.
    /// </summary>
    public static int HexDumpLengthSetting
    {
        get
        {
            try
            {
                return Math.Clamp(Settings.Get("Details.HexDumpLength", DefaultHexDumpLength),
                    MinHexDumpLength, MaxHexDumpLength);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                return DefaultHexDumpLength; // not a number, e.g. in a hand-edited settings.ini
            }
        }
    }

    /// <summary>A file passed on the command line (e.g. "Open with…"), opened at startup.</summary>
    public static string? StartupFile { get; private set; }

    private DateTime _lastErrorTime;
    private string? _lastErrorMessage;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Resolve command-line paths before LoadSettings changes the working directory.
        StartupFile = e.Args
            .Select(arg => { try { return Path.GetFullPath(arg); } catch { return null; } })
            .FirstOrDefault(path => path != null && File.Exists(path));

        LoadSettings();
        PalEntry.ForceOpaque = ForceOpaqueSetting;
        ThemeManager.Apply(ThemeManager.Parse(Settings.Get("Appearance.Theme", nameof(AppTheme.Dark))));

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        base.OnStartup(e);
    }

    public static void LoadSettings()
    {
        var appDirectory = Path.GetDirectoryName(ResourceAssembly.Location);
        if (appDirectory != null)
            Environment.CurrentDirectory = appDirectory;
        try
        {
            Settings = File.Exists(SettingsFileName)
                ? SettingsIO.Ini.ParseFile(SettingsFileName)
                : new Section();
        }
        catch (Exception)
        {
            // A corrupt settings file shouldn't stop the editor from starting.
            Settings = new Section();
        }
    }

    public static void SaveSettings()
    {
        try
        {
            SettingsIO.Ini.WriteFile(SettingsFileName, Settings);
        }
        catch (Exception)
        {
            // e.g. installed to a read-only folder; settings simply won't persist.
        }
    }

    /// <summary>
    /// Keeps the editor alive on unexpected errors (a decoder choking on an
    /// unusual file, say) instead of crashing and losing unsaved edits.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        // Don't flood the user if the same error repeats every frame.
        var now = DateTime.UtcNow;
        var repeated = e.Exception.Message == _lastErrorMessage && (now - _lastErrorTime).TotalSeconds < 2;
        _lastErrorTime = now;
        _lastErrorMessage = e.Exception.Message;
        if (repeated) return;

        if (MainWindow is MainWindow main && main.IsLoaded)
        {
            main.ReportError("Unexpected error", e.Exception);
        }
        else
        {
            MessageBox.Show(e.Exception.ToString(), "WorldExplorer — unexpected error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
