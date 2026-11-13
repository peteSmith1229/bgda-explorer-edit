using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WorldExplorer.Views;

/// <summary>
/// The Enhancements tab of Executable Tuning (DataContext: EnhancementsModel; each spell's card: SpellCard). Everything is
/// edited in the model; this only opens ICONS.LMP and passes the buttons (a card's, Enable all) on to it.
/// </summary>
public partial class EnhancementsView : UserControl
{
    public EnhancementsView()
    {
        InitializeComponent();
    }

    private EnhancementsModel? Model => DataContext as EnhancementsModel;

    /// <summary>Reads ICONS.LMP (the HUD's spell icons) into <paramref name="model"/>; says why when it can't (unless <paramref name="quiet"/>).</summary>
    public static bool TryLoadIcons(EnhancementsModel model, string path, Window? owner, bool quiet = false)
    {
        try
        {
            model.LoadIcons(File.ReadAllBytes(path), path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (!quiet)
            {
                var message = $"{path} can't take the borrowed spells' icons: {ex.Message}";
                if (owner != null) MessageBox.Show(owner, message, "Enhancements", MessageBoxButton.OK, MessageBoxImage.Warning);
                else MessageBox.Show(message, "Enhancements", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }
    }

    /// <summary>A card's "The tested settings" button: that spell's boxes back to what featpatch.py writes.</summary>
    private void SpellDefaults_Click(object sender, RoutedEventArgs e) => ((sender as FrameworkElement)?.DataContext as SpellCard)?.Defaults();

    /// <summary>"Enable all": every spell's box ticked.</summary>
    private void EnableAll_Click(object sender, RoutedEventArgs e) => Model?.EnableAll();

    private void OpenIcons_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not { } model) return;
        var dialog = new OpenFileDialog
        {
            Title = "Open ICONS.LMP (the HUD's spell icons)",
            Filter = "Spell icons|ICONS.LMP|LMP archives|*.lmp|All files|*.*"
        };
        if (model.IconsPath != null && Path.GetDirectoryName(model.IconsPath) is { } folder && Directory.Exists(folder))
            dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) TryLoadIcons(model, dialog.FileName, Window.GetWindow(this));
    }
}
