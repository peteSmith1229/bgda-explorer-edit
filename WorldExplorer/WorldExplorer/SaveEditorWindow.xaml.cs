// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/SaveEditorWindow.xaml (+ this .cs)
//
// Per-slot difficulty editor for a BGDA PS2 save (.psu export). Difficulty at
// offset 0x1D4 within each save region was confirmed by controlled diff AND verified
// in-game (edited Easy->Extreme loaded and applied). No save checksum blocks editing.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JetBlackEngineLib.Data.Save;
using Microsoft.Win32;

namespace WorldExplorer;

public partial class SaveEditorWindow : Window
{
    public sealed class SlotRow
    {
        public int SlotNumber { get; init; }
        public string CharacterName { get; init; } = "";
        public string LevelName { get; init; } = "";
        /// <summary>Bound to the combo; one of the DifficultyNames values.</summary>
        public string DifficultyText { get; set; } = "";
    }

    private static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Extreme" };

    private readonly BgdaSave _save;
    private readonly List<SlotRow> _rows;

    public SaveEditorWindow(BgdaSave save)
    {
        InitializeComponent();
        _save = save;

        difficultyColumn.ItemsSource = DifficultyNames;

        _rows = _save.GetSlots().Select(s => new SlotRow
        {
            SlotNumber    = s.SlotNumber,
            CharacterName = s.CharacterName,
            LevelName     = s.LevelName,
            DifficultyText = DifficultyNames[(int)s.Difficulty]
        }).ToList();
        slotGrid.ItemsSource = _rows;
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        slotGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
        slotGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);

        try
        {
            foreach (var row in _rows)
            {
                var idx = Array.IndexOf(DifficultyNames, row.DifficultyText);
                if (idx < 0)
                    throw new ArgumentException($"Slot {row.SlotNumber}: choose a difficulty.");
                _save.SetDifficulty(row.SlotNumber, (BgdaDifficulty)idx);
            }
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Invalid value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PS2 Save Export|*.psu|All Files|*.*",
            FileName = System.IO.Path.GetFileName(_save.FilePath)
        };
        if (dialog.ShowDialog(this) != true) return;

        _save.Save(dialog.FileName);
        MessageBox.Show(this,
            "Saved. Re-import this .psu to your memory card, then load the slot in-game. " +
            "Keep your original card backed up.",
            "Save Editor", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}