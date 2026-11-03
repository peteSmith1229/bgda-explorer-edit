// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/Da2IsoWindow.xaml.cs
//
// Build Three-Player ISO (Dark Alliance II): the window of Da2ThreePlayerIso.Build, which does what
// the mod's make_3p_iso.bat / da2_make_3p_iso.py did. The files are found in the folders of
// extracted disc files (Champions of Norrath for the multitap driver, Dark Alliance 1 for the DA1
// lumps and voices) or picked by hand; the paths are remembered. The original image is checked
// to be the USA v1.03 disc first (SLUS_206.75 and its CRC); the build runs in the background.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Iso;
using Microsoft.Win32;
using WorldExplorer.Infrastructure;
using WorldExplorer.Themes;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace WorldExplorer;

public partial class Da2IsoWindow : Window
{
    private const string IsoSetting = "Da2.OriginalIso";
    private const string OutputSetting = "Da2.ThreePlayerIso";
    private const string NorrathSetting = "Da2.NorrathFolder";
    private const string Da1Setting = "Da2.Da1Folder";

    public sealed class FileRow : ObservableObject
    {
        private bool _include;
        private string _sourcePath = "";
        private string _status = "";

        public Da2IsoFileInfo Info { get; init; } = null!;
        public string NameOnDisc => Info.NameOnDisc;
        public string What => Info.What + (Info.Required ? " (needed)" : "");
        public string From => Info.Game + (Info.SourceName != Info.NameOnDisc ? ": " + Info.SourceName : "");

        /// <summary>Whether the file goes on the disc (always for the multitap driver: the box ticks itself again).</summary>
        public bool Include
        {
            get => _include;
            set
            {
                if (!SetProperty(ref _include, value || Info.Required) && value != _include) OnPropertyChanged();
            }
        }

        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (!SetProperty(ref _sourcePath, value)) return;
                Status = value.Trim().Length == 0 ? "not found" : File.Exists(value.Trim()) ? "found" : "missing";
                if (Status == "found") Include = true;
            }
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        /// <summary>The path was picked by hand (a folder change doesn't replace it).</summary>
        public bool Picked { get; set; }
    }

    private readonly List<FileRow> _rows;
    private CancellationTokenSource? _cancel;
    private bool _outputEdited;

    public Da2IsoWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _rows = Da2ThreePlayerIso.Files.Select(f => new FileRow { Info = f, Include = f.Required }).ToList();
        fileGrid.ItemsSource = _rows;

        isoBox.Text = App.Settings.Get(IsoSetting, "") ?? "";
        var output = App.Settings.Get(OutputSetting, "") ?? "";
        if (output.Length > 0)
        {
            outputBox.Text = output;
            _outputEdited = true;
        }

        norrathBox.Text = App.Settings.Get(NorrathSetting, "") ?? "";
        // WorldExplorer's data folder is often Dark Alliance 1's BG\DATA
        var da1 = App.Settings.Get(Da1Setting, "") ?? "";
        da1Box.Text = da1.Length > 0 ? da1 : App.Settings.Get("Files.DataPath", "") ?? "";
        FindFiles();
    }

    // ═══════════════════════════════ paths ═══════════════════════════════

    /// <summary>A file in a folder of extracted disc files: the folder itself, its BG\0MOD or BG\DATA, or 0MOD / DATA.</summary>
    private static string? FindIn(string folder, string name)
    {
        folder = folder.Trim();
        if (folder.Length == 0 || !Directory.Exists(folder)) return null;
        foreach (var sub in new[] { "", Path.Combine("BG", "0MOD"), Path.Combine("BG", "DATA"), "0MOD", "DATA" })
        {
            var dir = Path.Combine(folder, sub);
            if (!Directory.Exists(dir)) continue;
            var file = Directory.EnumerateFiles(dir).FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase));
            if (file != null) return file;
        }

        return null;
    }

    private void FindFiles()
    {
        foreach (var row in _rows)
        {
            if (row.Picked && File.Exists(row.SourcePath)) continue;
            var folder = row.Info.Required ? norrathBox.Text : da1Box.Text;
            row.SourcePath = FindIn(folder, row.Info.SourceName) ?? "";
            row.Picked = false;
        }
    }

    private void Folder_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_rows != null) FindFiles();
    }

    private void IsoBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var iso = isoBox.Text.Trim();
        if (!_outputEdited && iso.Length > 0) outputBox.Text = Da2ThreePlayerIso.DefaultOutputPath(iso);
    }

    private void BrowseIso_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open the original Dark Alliance II disc image",
            Filter = "Disc image|*.iso|All Files|*.*"
        };
        if (Path.GetDirectoryName(isoBox.Text.Trim()) is { Length: > 0 } dir && Directory.Exists(dir)) dialog.InitialDirectory = dir;
        if (dialog.ShowDialog(this) == true) isoBox.Text = dialog.FileName;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var current = outputBox.Text.Trim();
        var dialog = new SaveFileDialog
        {
            Title = "Where to make the [3P] disc image",
            Filter = "Disc image|*.iso|All Files|*.*",
            FileName = current.Length > 0 ? Path.GetFileName(current) : "",
            OverwritePrompt = false // an existing [3P] copy is updated, not replaced
        };
        if (Path.GetDirectoryName(current) is { Length: > 0 } dir && Directory.Exists(dir)) dialog.InitialDirectory = dir;
        if (dialog.ShowDialog(this) != true) return;
        outputBox.Text = dialog.FileName;
        _outputEdited = true;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var box = (string)((Button)sender).Tag == "norrath" ? norrathBox : da1Box;
        using var dialog = new FolderBrowserDialog
        {
            Description = box == norrathBox
                ? "The folder with Champions of Norrath's disc files (BG\\0MOD\\MTAPMAN.IRX)"
                : "The folder with Dark Alliance's disc files (BG\\DATA)",
            UseDescriptionForTitle = true
        };
        if (Directory.Exists(box.Text.Trim())) dialog.SelectedPath = box.Text.Trim();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) box.Text = dialog.SelectedPath;
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not FileRow row) return;
        var dialog = new OpenFileDialog
        {
            Title = $"{row.Info.SourceName} of {row.Info.Game}",
            Filter = $"{row.Info.SourceName}|{row.Info.SourceName}|All Files|*.*"
        };
        if (Path.GetDirectoryName(row.SourcePath) is { Length: > 0 } dir && Directory.Exists(dir)) dialog.InitialDirectory = dir;
        if (dialog.ShowDialog(this) != true) return;
        row.SourcePath = dialog.FileName;
        row.Picked = true;
    }

    // ═══════════════════════════════ building ═══════════════════════════════

    private void Log(string line)
    {
        logBox.AppendText(line + Environment.NewLine);
        logBox.ScrollToEnd();
    }

    private async void Build_Click(object sender, RoutedEventArgs e)
    {
        fileGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var iso = isoBox.Text.Trim();
        var output = outputBox.Text.Trim();
        if (output.Length == 0 && iso.Length > 0) output = Da2ThreePlayerIso.DefaultOutputPath(iso);

        var inputs = new List<Da2IsoInput>();
        foreach (var row in _rows.Where(r => r.Include))
        {
            var path = row.SourcePath.Trim();
            if (!File.Exists(path))
            {
                MessageBox.Show(this, $"{row.NameOnDisc}: pick the file ({row.Info.SourceName} of {row.Info.Game})" +
                                      (row.Info.Required ? "." : ", or untick it."), "Build Three-Player ISO",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            inputs.Add(new Da2IsoInput(path, row.NameOnDisc));
        }

        logBox.Clear();
        SetBusy(true);
        _cancel = new CancellationTokenSource();
        var token = _cancel.Token;
        try
        {
            // the right disc first: the mod is made for SLUS_206.75 v1.03 (read from the image's root)
            await Task.Run(() =>
            {
                if (!File.Exists(iso)) throw new InvalidDataException("The original ISO was not found: " + iso);
                Da2Executable.Open(iso);
            }, token);
            Log($"{Path.GetFileName(iso)}: Dark Alliance II (USA) with {Da2Executable.FileName} v1.03.");

            var result = await Task.Run(() => Da2ThreePlayerIso.Build(iso, inputs, output,
                line => Dispatcher.InvokeAsync(() => Log(line)), token), token);

            App.Settings[IsoSetting] = iso;
            App.Settings[OutputSetting] = _outputEdited ? output : "";
            App.Settings[NorrathSetting] = norrathBox.Text.Trim();
            App.Settings[Da1Setting] = da1Box.Text.Trim();
            App.SaveSettings();

            await Dispatcher.InvokeAsync(() => { }); // the last log lines first
            MessageBox.Show(this,
                (result.Changed || result.Copied ? "The [3P] disc image is ready: " : "The [3P] disc image was already up to date: ") +
                result.OutputPath + "\n\nPlay it in PCSX2 with the multitap on port 1 and the cheat file of the mod (Executable Tuning).",
                "Build Three-Player ISO", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            Log("Cancelled.");
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            Log("Failed: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Build Three-Player ISO", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _cancel.Dispose();
            _cancel = null;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        buildButton.IsEnabled = !busy;
        cancelButton.IsEnabled = busy;
        closeButton.IsEnabled = !busy;
        fileGrid.IsEnabled = !busy;
        progress.Visibility = busy ? Visibility.Visible : Visibility.Hidden;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _cancel?.Cancel();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_cancel == null) return;
        e.Cancel = true;
        MessageBox.Show(this, "The [3P] disc image is being built: wait for it to finish, or press Cancel first.",
            "Build Three-Player ISO", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
