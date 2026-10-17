using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JetBlackEngineLib;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Text;
using Microsoft.Win32;

namespace WorldExplorer.Views;

/// <summary>
/// The Items tab of Executable Tuning: items, models, shops, loot and effects (DataContext: ItemEditorModel).
/// Everything is edited in the model; this only opens GTEXT.LMP and EQUIPMNT.LMP and asks before an added item is removed.
/// </summary>
public partial class ItemEditorView : UserControl
{
    public ItemEditorView()
    {
        InitializeComponent();
    }

    private ItemEditorModel? Model => DataContext as ItemEditorModel;

    /// <summary>Reads GTEXT.LMP into <paramref name="model"/>; says why when it can't (unless <paramref name="quiet"/>).</summary>
    public static bool TryLoadNames(ItemEditorModel model, string path, Window? owner, bool quiet = false)
    {
        try
        {
            model.LoadNames(GameText.Read(File.ReadAllBytes(path)), path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (!quiet) Show(owner, $"{path} couldn't be read: {ex.Message}");
            return false;
        }
    }

    /// <summary>Reads the file names in EQUIPMNT.LMP into <paramref name="model"/>.</summary>
    public static bool TryLoadEquipment(ItemEditorModel model, string path, Window? owner, bool quiet = false)
    {
        try
        {
            var data = File.ReadAllBytes(path);
            var lmp = new LmpFile(EngineVersion.DarkAlliance, Path.GetFileName(path), data, 0, data.Length);
            lmp.ReadDirectory();
            var names = lmp.Directory.Keys.ToList();
            if (!names.Any(n => n.EndsWith(".vif", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("It has no models (.vif files).");
            model.LoadEquipment(names, path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or
                                       IndexOutOfRangeException)
        {
            if (!quiet) Show(owner, $"{path} isn't an archive of models: {ex.Message}");
            return false;
        }
    }

    private static void Show(Window? owner, string message)
    {
        if (owner != null) MessageBox.Show(owner, message, "Items", MessageBoxButton.OK, MessageBoxImage.Warning);
        else MessageBox.Show(message, "Items", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static bool Ask(Window? owner, string message) =>
        (owner != null
            ? MessageBox.Show(owner, message, "Items", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            : MessageBox.Show(message, "Items", MessageBoxButton.OKCancel, MessageBoxImage.Question)) == MessageBoxResult.OK;

    private void OpenNames_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not { } model) return;
        var owner = Window.GetWindow(this);
        if (model.NamesNeedSaving && !Ask(owner, "The names you changed haven't been saved. Open another GTEXT.LMP and lose them?")) return;

        var dialog = new OpenFileDialog
        {
            Title = "Open GTEXT.LMP (the game's text, with the item names)",
            Filter = "Game text|GTEXT.LMP|LMP archives|*.lmp|All files|*.*"
        };
        if (model.NamesPath is { } current && Path.GetDirectoryName(current) is { } folder && Directory.Exists(folder))
            dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(owner) == true) TryLoadNames(model, dialog.FileName, owner);
    }

    private void OpenEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not { } model) return;
        var dialog = new OpenFileDialog
        {
            Title = "Open EQUIPMNT.LMP (the models of the items)",
            Filter = "Item models|EQUIPMNT.LMP|LMP archives|*.lmp|All files|*.*"
        };
        var known = model.EquipmentPath ?? model.NamesPath;
        if (known != null && Path.GetDirectoryName(known) is { } folder && Directory.Exists(folder)) dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) TryLoadEquipment(model, dialog.FileName, Window.GetWindow(this));
    }

    private void RevertItem_Click(object sender, RoutedEventArgs e) => Model?.SelectedItem?.Revert();

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (Model?.AddItem() is { } row) itemGrid.ScrollIntoView(row);
    }

    /// <summary>Removes the selected added item; asks first when items already in the executable (which saves may hold) are affected.</summary>
    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not { } model || model.SelectedItem is not { IsAdded: true } row) return;
        var saved = model.Items.Count(r => r.Index >= row.Index && !r.IsNew);
        if (saved > 0)
        {
            var later = model.Items.Count - 1 - row.Index;
            var message = $"Remove {row.DisplayName} (item {row.Index})? " +
                          (later > 0 ? $"The {later} item{(later == 1 ? "" : "s")} after it move down a place. " : "") +
                          "Saves keep items by their number, so a save that holds " + (later > 0 ? "any of them" : "it") +
                          " won't load " + (later > 0 ? "them" : "it") + " properly with the executable you save.";
            if (!Ask(Window.GetWindow(this), message)) return;
        }

        model.RemoveItem(row);
    }

    /// <summary>
    /// A grid inside the page's ScrollViewer would swallow the mouse wheel with its own scroll viewer, so the
    /// wheel is handed on to the page.
    /// </summary>
    private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not FrameworkElement { Parent: UIElement parent }) return;
        e.Handled = true;
        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        });
    }
}
