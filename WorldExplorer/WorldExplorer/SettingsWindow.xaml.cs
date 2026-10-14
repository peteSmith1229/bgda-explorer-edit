using JetBlackEngineLib.Data.Textures;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using WorldExplorer.Infrastructure;
using WorldExplorer.Themes;

namespace WorldExplorer;

/// <summary>
/// Application settings. Saving stores everything in <see cref="App.Settings"/>;
/// the caller decides whether the open file needs reloading.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>A line of a hex dump: the Up and Down keys change the length by one.</summary>
    private const int HexDumpLineLength = 16;

    public SettingsWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);

        engineVersionBox.ItemsSource = GameOption.All;
        engineVersionBox.SelectedItem = GameOption.FromSettings();

        darkThemeRadio.IsChecked = ThemeManager.Current == AppTheme.Dark;
        lightThemeRadio.IsChecked = ThemeManager.Current == AppTheme.Light;

        dataPathTextblock.Text = App.Settings.Get("Files.DataPath", "");
        reopenLastFileCheckBox.IsChecked = App.Settings.Get("Files.ReopenLastFile", true);
        forceOpaqueCheckBox.IsChecked = App.ForceOpaqueSetting;
        gizmoSizeSlider.Value = App.Settings.Get("Editor.GizmoScale", 1.0);

        var range = $"{App.MinHexDumpLength} to {App.MaxHexDumpLength:N0} bytes";
        hexDumpLengthHint.Text = "How much of an entry Details shows as a hex dump when there is no decoder for it, " +
                                 $"or decoding it fails: {range}, {HexDumpLineLength} to a line. " +
                                 "Long dumps take a moment to show.";
        hexDumpLengthError.Text = $"Enter a whole number from {range}.";
        hexDumpLengthBox.Text = App.HexDumpLengthSetting.ToString(CultureInfo.InvariantCulture);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Save is disabled while the hex dump length isn't a number in range.
        if (!TryReadHexDumpLength(out var hexDumpLength)) return;

        var game = engineVersionBox.SelectedItem as GameOption ?? GameOption.All[0];
        var theme = lightThemeRadio.IsChecked == true ? AppTheme.Light : AppTheme.Dark;

        GameOption.SaveToSettings(game);
        App.Settings["Appearance.Theme"] = theme.ToString();
        App.Settings["Files.DataPath"] = dataPathTextblock.Text.Trim();
        App.Settings["Files.ReopenLastFile"] = reopenLastFileCheckBox.IsChecked == true;
        App.Settings["Textures.ForceOpaque"] = forceOpaqueCheckBox.IsChecked == true;
        App.Settings["Editor.GizmoScale"] = gizmoSizeSlider.Value;
        App.Settings["Details.HexDumpLength"] = hexDumpLength;
        PalEntry.ForceOpaque = forceOpaqueCheckBox.IsChecked == true;

        if (theme != ThemeManager.Current)
        {
            ThemeManager.Apply(theme);
        }

        App.SaveSettings();
        DialogResult = true;
    }

    private void EngineVersionBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var note = (engineVersionBox.SelectedItem as GameOption)?.Note;
        gameNoteText.Text = note ?? "";
        gameNoteText.Visibility = note == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using FolderBrowserDialog dialog = new() { Description = "Choose the game's data folder", UseDescriptionForTitle = true };

        // Start in the current folder when it's valid.
        if (!string.IsNullOrEmpty(dataPathTextblock.Text) && Directory.Exists(dataPathTextblock.Text))
        {
            dialog.SelectedPath = dataPathTextblock.Text;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            dataPathTextblock.Text = dialog.SelectedPath;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hex dump length: a whole number of bytes
    // ─────────────────────────────────────────────────────────────────────────

    private bool TryReadHexDumpLength(out int length) =>
        int.TryParse(hexDumpLengthBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out length) &&
        length is >= App.MinHexDumpLength and <= App.MaxHexDumpLength;

    private void HexDumpLengthBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var valid = TryReadHexDumpLength(out _);
        SaveButton.IsEnabled = valid;

        // Hidden, not collapsed: the window keeps its size while the error shows.
        hexDumpLengthHint.Visibility = valid ? Visibility.Visible : Visibility.Hidden;
        hexDumpLengthError.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Only digits can be typed.</summary>
    private void HexDumpLengthBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsAsciiDigit);

    /// <summary>Up and Down move to the next or previous whole line of the dump.</summary>
    private void HexDumpLengthBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.Space:
                e.Handled = true; // a space isn't a digit, and typing one doesn't raise PreviewTextInput
                break;

            case System.Windows.Input.Key.Up:
            case System.Windows.Input.Key.Down:
            {
                var current = int.TryParse(hexDumpLengthBox.Text.Trim(), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var typed)
                    ? typed
                    : App.HexDumpLengthSetting;
                var next = e.Key == System.Windows.Input.Key.Up
                    ? (current / HexDumpLineLength + 1) * HexDumpLineLength
                    : (current - 1) / HexDumpLineLength * HexDumpLineLength;

                hexDumpLengthBox.Text = Math.Clamp(next, App.MinHexDumpLength, App.MaxHexDumpLength)
                    .ToString(CultureInfo.InvariantCulture);
                hexDumpLengthBox.CaretIndex = hexDumpLengthBox.Text.Length;
                e.Handled = true;
                break;
            }
        }
    }

    /// <summary>Pasted text must be a number too (surrounding spaces are dropped).</summary>
    private void HexDumpLengthBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        var text = (e.SourceDataObject.GetData(System.Windows.DataFormats.UnicodeText) as string)?.Trim();
        if (string.IsNullOrEmpty(text) || !text.All(char.IsAsciiDigit))
        {
            e.CancelCommand();
            return;
        }

        e.DataObject = new System.Windows.DataObject(System.Windows.DataFormats.UnicodeText, text);
    }
}
