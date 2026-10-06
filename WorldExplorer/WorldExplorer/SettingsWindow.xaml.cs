using JetBlackEngineLib;
using JetBlackEngineLib.Data.Textures;
using System.IO;
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
    public SettingsWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);

        engineVersionBox.ItemsSource = GameOption.All;
        engineVersionBox.SelectedItem = GameOption.For(App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance));

        darkThemeRadio.IsChecked = ThemeManager.Current == AppTheme.Dark;
        lightThemeRadio.IsChecked = ThemeManager.Current == AppTheme.Light;

        dataPathTextblock.Text = App.Settings.Get("Files.DataPath", "");
        reopenLastFileCheckBox.IsChecked = App.Settings.Get("Files.ReopenLastFile", true);
        forceOpaqueCheckBox.IsChecked = App.Settings.Get("Textures.ForceOpaque", false);
        gizmoSizeSlider.Value = App.Settings.Get("Editor.GizmoScale", 1.0);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var game = engineVersionBox.SelectedItem as GameOption ?? GameOption.All[0];
        var theme = lightThemeRadio.IsChecked == true ? AppTheme.Light : AppTheme.Dark;

        App.Settings["Core.EngineVersion"] = game.Version;
        App.Settings["Appearance.Theme"] = theme.ToString();
        App.Settings["Files.DataPath"] = dataPathTextblock.Text.Trim();
        App.Settings["Files.ReopenLastFile"] = reopenLastFileCheckBox.IsChecked == true;
        App.Settings["Textures.ForceOpaque"] = forceOpaqueCheckBox.IsChecked == true;
        App.Settings["Editor.GizmoScale"] = gizmoSizeSlider.Value;
        PalEntry.ForceOpaque = forceOpaqueCheckBox.IsChecked == true;

        if (theme != ThemeManager.Current)
        {
            ThemeManager.Apply(theme);
        }

        App.SaveSettings();
        DialogResult = true;
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
}
