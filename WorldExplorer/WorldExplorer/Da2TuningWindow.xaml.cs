// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/Da2TuningWindow.xaml.cs
//
// Executable Tuning for Baldur's Gate: Dark Alliance II (SLUS_206.75 v1.03) and its three-player mod.
// What it shows and edits is in Da2TuningModel; this file has the window's events: opening the
// cheat file (the one of the last session, else PCSX2's cheats folder), saving it (a .bak copy of
// the file it overwrites), the row buttons and the multiplier columns that are editable cell by cell.
// A bad value in ANY tab stops the save with a message; nothing is written.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using JetBlackEngineLib.Data.Executable;
using Microsoft.Win32;
using WorldExplorer.Themes;

namespace WorldExplorer;

public partial class Da2TuningWindow : Window
{
    /// <summary>The cheat file of the last session.</summary>
    public const string CheatFileSetting = "Da2.CheatFile";

    private sealed record FeatChoice(int Id, string Name);

    private readonly Da2TuningModel _model;

    /// <summary>Files a .bak copy was made of in this window (one copy each: the file as it was before).</summary>
    private readonly HashSet<string> _backedUp = new(StringComparer.OrdinalIgnoreCase);

    public Da2TuningWindow(Da2Executable exe)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _model = new Da2TuningModel(exe);
        DataContext = _model;

        for (var i = 0; i < Da2Creature.Multipliers.Length; i++)
            creatureGrid.Columns.Insert(2 + i, CellColumn(Da2Creature.Multipliers[i] + " ×", $"Cells[{i}]", 62));
        difficultyGrid.Columns.Add(CellColumn("Monster HP ×", nameof(Da2DifficultyRow.Hp), 110));
        difficultyGrid.Columns.Add(CellColumn("Monster damage ×", nameof(Da2DifficultyRow.Damage), 130));
        difficultyGrid.Columns.Add(CellColumn("Monster level +", nameof(Da2DifficultyRow.Level), 120));
        difficultyGrid.Columns.Add(CellColumn("Boss HP ×", nameof(Da2DifficultyRow.Boss), 100));
        twoPlayerGrid.Columns.Insert(1, CellColumn("Value", nameof(Da2TwoPlayerRow.Cell), 100));

        featPriceCombo.ItemsSource = Da2Names.FeatIds.Where(id => id > 0)
            .Select(id => new FeatChoice(id, $"{Da2Names.Feat(id)} ({id})")).ToList();
        changesText.Text = "Executable: " + exe.Source;

        var start = StartingCheatFile();
        if (start != null) Open(start, quiet: true);
        RefreshStatus();
    }

    /// <summary>PCSX2's cheats folder in the user's documents (PCSX2 2.x), or null when there is none.</summary>
    public static string? DefaultCheatsFolder()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PCSX2", "cheats");
        return Directory.Exists(folder) ? folder : null;
    }

    /// <summary>The cheat file of the last session, else the mod's file in PCSX2's cheats folder, if it exists.</summary>
    private static string? StartingCheatFile()
    {
        var last = App.Settings.Get(CheatFileSetting, "") ?? "";
        if (last.Length > 0 && File.Exists(last)) return last;
        var folder = DefaultCheatsFolder();
        var file = folder == null ? null : Path.Combine(folder, Da2ThreePlayerMod.FileName);
        return file != null && File.Exists(file) ? file : null;
    }

    // ═══════════════════════════════ the cheat file ═══════════════════════════════

    private bool Open(string path, bool quiet)
    {
        try
        {
            _model.LoadCheatFile(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            if (!quiet || File.Exists(path))
                MessageBox.Show(this, $"{path} couldn't be read: {ex.Message}", "Open cheat file", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        App.Settings[CheatFileSetting] = path;
        App.SaveSettings();
        return true;
    }

    private void RefreshStatus()
    {
        cheatPathText.Text = _model.CheatPath ??
                             $"None yet: Save asks where to write {Da2ThreePlayerMod.FileName}" +
                             (DefaultCheatsFolder() is { } folder ? $" (PCSX2's cheats folder is {folder})." : " (in PCSX2's cheats folder).");
        mainStatusText.Text = _model.MainStatus();
        notesList.ItemsSource = _model.Notes.ToList();
        var others = _model.OtherGroups;
        otherGroupsText.Text = others.Count == 0
            ? ""
            : $"Kept as they are: {others.Count} group{(others.Count == 1 ? "" : "s")} of your own — " +
              string.Join(", ", others.Take(6).Select(g => "[" + g.Name + "]")) + (others.Count > 6 ? " …" : "") + ".";
    }

    private void OpenCheatFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open the Dark Alliance II cheat file",
            Filter = "PCSX2 cheat file|*.pnach|All Files|*.*",
            FileName = Da2ThreePlayerMod.FileName
        };
        var folder = _model.CheatPath is { } current ? Path.GetDirectoryName(current) : DefaultCheatsFolder();
        if (folder != null && Directory.Exists(folder)) dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(this) != true) return;
        if (Open(dialog.FileName, quiet: false)) RefreshStatus();
    }

    private void Build51_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this,
                $"Put every tab back to build {Da2ThreePlayerMod.CurrentBuild} of the mod and the game's own tables? " +
                "The groups of your own stay; nothing is written until you save.",
                "Start again", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        _model.StartFromBuild51();
        RefreshStatus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_model.CheatPath is not { } path)
        {
            SaveAs_Click(sender, e);
            return;
        }

        if (TryApply(out var file)) Write(path, file!);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApply(out var file)) return; // checked first, so a bad value doesn't wait for a file name
        var dialog = new SaveFileDialog
        {
            Title = "Save the Dark Alliance II cheat file",
            Filter = "PCSX2 cheat file|*.pnach|All Files|*.*",
            FileName = _model.CheatPath is { } current ? Path.GetFileName(current) : Da2ThreePlayerMod.FileName
        };
        var folder = _model.CheatPath is { } p ? Path.GetDirectoryName(p) : DefaultCheatsFolder();
        if (folder != null && Directory.Exists(folder)) dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(this) != true) return;
        Write(dialog.FileName, file!);
    }

    private bool TryApply(out JetBlackEngineLib.Data.Pnach.PnachFile? file)
    {
        CommitGrids(tabs);
        try
        {
            file = _model.Apply();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
            file = null;
            return false; // nothing written to disk
        }

        var conflicts = _model.Conflicts;
        if (conflicts.Count == 0) return true;
        var groups = string.Join(", ", conflicts.Select(c => "[" + c.Group + "]").Distinct());
        return MessageBox.Show(this,
            $"The executable edits change {conflicts.Select(c => c.Address).Distinct().Count()} word(s) that {groups} " +
            $"also write(s), e.g. 0x{conflicts[0].Address:X6}. With both groups ticked in PCSX2, the one applied last wins.\n\nSave anyway?",
            "Executable Tuning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private void Write(string path, JetBlackEngineLib.Data.Pnach.PnachFile file)
    {
        var backup = path + ".bak";
        try
        {
            if (File.Exists(path) && _backedUp.Add(Path.GetFullPath(path))) File.Copy(path, backup, overwrite: true);
            file.Save(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"{path} couldn't be written: {ex.Message}", "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _model.Saved(path, file);
        App.Settings[CheatFileSetting] = path;
        App.SaveSettings();
        RefreshStatus();

        var name = Path.GetFileName(path);
        var inCheats = string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "cheats", StringComparison.OrdinalIgnoreCase);
        var tuning = _model.Exe.HasChanges ? $" The executable edits are the group [{Da2Executable.TuningGroupName}]: tick it too." : "";
        MessageBox.Show(this,
            $"Saved {path}" + (_backedUp.Contains(Path.GetFullPath(path)) && File.Exists(backup) ? $" (the file as it was before is kept as {Path.GetFileName(backup)})." : ".") +
            (string.Equals(name, Da2ThreePlayerMod.FileName, StringComparison.OrdinalIgnoreCase) && inCheats
                ? ""
                : $"\n\nPCSX2 reads it as {Da2ThreePlayerMod.FileName} in its cheats folder.") +
            "\n\nIn PCSX2, turn cheats on, tick the groups you want in the game's cheat list and restart the game." + tuning,
            "Executable Tuning", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Ends any edit in progress in the grids (so a value being typed is part of the save).</summary>
    private static void CommitGrids(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is DataGrid grid)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            CommitGrids(child);
        }
    }

    // ═══════════════════════════════ columns editable cell by cell ═══════════════════════════════

    /// <summary>A text column for a <see cref="Da2Cell"/>: grey and read-only where the cell can't be changed.</summary>
    private DataGridTextColumn CellColumn(string header, string cellPath, double width)
    {
        var shown = new Style(typeof(TextBlock));
        var fixedCell = new DataTrigger { Binding = new Binding(cellPath + "." + nameof(Da2Cell.IsEditable)), Value = false };
        fixedCell.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension("Brush.TextMuted")));
        shown.Triggers.Add(fixedCell);

        var editing = new Style(typeof(TextBox), (Style)FindResource("DataGrid.EditingTextBox"));
        editing.Setters.Add(new Setter(TextBoxBase.IsReadOnlyProperty, new Binding(cellPath + "." + nameof(Da2Cell.IsReadOnly))));

        return new DataGridTextColumn
        {
            Header = header,
            Width = width,
            Binding = new Binding(cellPath + "." + nameof(Da2Cell.Text)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
            ElementStyle = shown,
            EditingElementStyle = editing
        };
    }

    /// <summary>
    /// The small grids sit inside a tab's ScrollViewer; their own scroll viewer would swallow the
    /// mouse wheel, so the wheel is handed on to the page.
    /// </summary>
    private void NestedGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not FrameworkElement { Parent: UIElement parent }) return;
        e.Handled = true;
        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        });
    }

    // ═══════════════════════════════ Creatures ═══════════════════════════════

    private string UniqueName(string name)
    {
        var names = _model.Placements.Select(p => p.Name.Trim()).ToHashSet(StringComparer.Ordinal);
        if (!names.Contains(name)) return name;
        for (var i = 2; ; i++)
        {
            var candidate = $"{name} {i}";
            if (!names.Contains(candidate)) return candidate;
        }
    }

    private void ShowPlacement(Da2PlacementRow row)
    {
        placementGrid.SelectedItem = row;
        placementGrid.ScrollIntoView(row);
    }

    private void AddPlacement_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(placementGrid);
        if (_model.Placements.Count >= Da2ThreePlayerMod.MaxPlacements)
        {
            MessageBox.Show(this, $"The spawn table holds {Da2ThreePlayerMod.MaxPlacements} creatures.", "Creatures",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var row = Da2TuningModel.ToRow(new Da2Placement { Name = UniqueName("Creature - Goblin in trollb"), Level = "trollb", Type = "Goblin" });
        row.Original = null;
        _model.Placements.Add(row);
        ShowPlacement(row);
    }

    private void DuplicatePlacement_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(placementGrid);
        if (placementGrid.SelectedItem is not Da2PlacementRow source) return;
        if (_model.Placements.Count >= Da2ThreePlayerMod.MaxPlacements)
        {
            MessageBox.Show(this, $"The spawn table holds {Da2ThreePlayerMod.MaxPlacements} creatures.", "Creatures",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var row = new Da2PlacementRow
        {
            Include = source.Include, Name = UniqueName(source.Name.Trim() + " (copy)"), Level = source.Level, Type = source.Type,
            X = source.X, Y = source.Y, Z = source.Z, Facing = source.Facing, Properties = source.Properties
        };
        _model.Placements.Insert(_model.Placements.IndexOf(source) + 1, row);
        ShowPlacement(row);
    }

    private void RemovePlacement_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(placementGrid);
        if (placementGrid.SelectedItem is Da2PlacementRow row) _model.Placements.Remove(row);
    }

    private void StockPlacements_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(placementGrid);
        _model.LoadPlacements(Da2ThreePlayerMod.StockPlacements());
    }

    // ═══════════════════════════════ Shops, Feat Prices ═══════════════════════════════

    private void StockShops_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(shopActGrid);
        CommitGrids(shopKindGrid);
        _model.LoadShops(Da2ThreePlayerMod.StockShops());
    }

    private void AddFeatPrice_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(featPriceGrid);
        if (featPriceCombo.SelectedItem is not FeatChoice choice) return;
        var existing = _model.FeatPrices.FirstOrDefault(r => r.FeatId == choice.Id);
        if (existing == null)
        {
            // start from the game's prices of the feat (rank 1 = 1 when no DA2 class has it)
            var game = _model.Exe.GetClassFeats().FirstOrDefault(f => f.FeatId == choice.Id);
            var prices = game?.Prices(_model.Exe.GetSpecialPrices) ?? new[] { 1 };
            existing = _model.NewFeatPriceRow(choice.Id, prices.Select(p => Math.Clamp(p, 1, Da2ThreePlayerMod.MaxFeatPrice)).ToArray());
            var at = _model.FeatPrices.TakeWhile(r => r.FeatId < choice.Id).Count();
            _model.FeatPrices.Insert(at, existing);
        }

        featPriceGrid.SelectedItem = existing;
        featPriceGrid.ScrollIntoView(existing);
    }

    private void RemoveFeatPrice_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(featPriceGrid);
        if (featPriceGrid.SelectedItem is Da2FeatPriceRow row) _model.FeatPrices.Remove(row);
    }

    private void StockFeatPrices_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(featPriceGrid);
        _model.LoadFeatPrices(Da2ThreePlayerMod.StockFeatPrices());
    }

    // ═══════════════════════════════ Class Feats, Difficulty ═══════════════════════════════

    /// <summary>The class feats' price column uses the spell price tables: refreshed once the edit is in.</summary>
    private void SpecialPrices_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e) =>
        Dispatcher.InvokeAsync(() =>
        {
            foreach (var row in _model.ClassFeats) row.RefreshPrices();
        }, DispatcherPriority.Background);

    private void StockDifficulty_Click(object sender, RoutedEventArgs e)
    {
        CommitGrids(difficultyGrid);
        CommitGrids(twoPlayerGrid);
        _model.LoadDifficulty(_model.Exe.Stock().GetDifficulty());
    }
}
