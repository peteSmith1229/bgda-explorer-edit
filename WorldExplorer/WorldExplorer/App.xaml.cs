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
