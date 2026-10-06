// ═══════════════════════════════════════════════════════════════════════════════
// COMPLETE REPLACEMENT for WorldExplorer/WorldExplorer/SaveEditorWindow.xaml.cs
// (pair with the matching SaveEditorWindow.xaml)
//
// Two tabs:
//   Difficulty — per-slot difficulty (CONFIRMED in-game).
//   Character  — level, XP, ability scores, HP/MP, gold, feat points, read from the
//                player struct serialised at region+0x1FE. Offsets cross-confirmed
//                against live PCSX2 RAM and the executable's starting-stats table.
//                Writing these is NOT yet confirmed in-game.
//
// Validation runs over BOTH tabs and aborts before writing if anything is invalid,
// so a bad value can never produce a half-written file.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JetBlackEngineLib.Data.Save;
using Microsoft.Win32;
using WorldExplorer.Themes;

namespace WorldExplorer;

public partial class SaveEditorWindow : Window
{
    public sealed class SlotRow
    {
        public int SlotNumber { get; init; }
        public string CharacterName { get; init; } = "";
        public string LevelName { get; init; } = "";
        public string DifficultyText { get; set; } = "";
    }

    public sealed class CharacterRow
    {
        public int SlotNumber { get; init; }
        public string Name { get; init; } = "";
        public string LevelText { get; set; } = "";
        public string XpText { get; set; } = "";
        public string StrText { get; set; } = "";
        public string IntText { get; set; } = "";
        public string WisText { get; set; } = "";
        public string DexText { get; set; } = "";
        public string ConText { get; set; } = "";
        public string ChaText { get; set; } = "";
        public string HpCurrentText { get; set; } = "";
        public string HpMaxText { get; set; } = "";
        public string MpCurrentText { get; set; } = "";
        public string MpMaxText { get; set; } = "";
        public string GoldText { get; set; } = "";
        public string FeatPointsText { get; set; } = "";
    }

    public sealed class EnemyRow
    {
        /// <summary>The enemy this row came from; used to write HP back.</summary>
        public BgdaEnemy Source { get; init; } = null!;
        public int SlotNumber { get; init; }
        public string TypeName { get; init; } = "";
        public uint TypeId { get; init; }
        public string HpText { get; set; } = "";
        public string StateText { get; init; } = "";
        public string Position { get; init; } = "";
        public string RecordText { get; init; } = "";
    }

    private static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Extreme" };

    private readonly BgdaSave _save;
    private readonly List<SlotRow> _slotRows;
    private readonly List<CharacterRow> _charRows;
    private readonly List<EnemyRow> _enemyRows;

    public SaveEditorWindow(BgdaSave save)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _save = save;

        difficultyColumn.ItemsSource = DifficultyNames;

        _slotRows = _save.GetSlots().Select(s => new SlotRow
        {
            SlotNumber     = s.SlotNumber,
            CharacterName  = s.CharacterName,
            LevelName      = s.LevelName,
            DifficultyText = DifficultyNames[(int)s.Difficulty]
        }).ToList();
        slotGrid.ItemsSource = _slotRows;

        _charRows = _save.GetCharacters().Select(c => new CharacterRow
        {
            SlotNumber     = c.SlotNumber,
            Name           = c.Name,
            LevelText      = c.Level.ToString(CultureInfo.InvariantCulture),
            XpText         = c.Experience.ToString(CultureInfo.InvariantCulture),
            StrText        = c.Abilities[(int)BgdaAbility.Strength].ToString(CultureInfo.InvariantCulture),
            IntText        = c.Abilities[(int)BgdaAbility.Intelligence].ToString(CultureInfo.InvariantCulture),
            WisText        = c.Abilities[(int)BgdaAbility.Wisdom].ToString(CultureInfo.InvariantCulture),
            DexText        = c.Abilities[(int)BgdaAbility.Dexterity].ToString(CultureInfo.InvariantCulture),
            ConText        = c.Abilities[(int)BgdaAbility.Constitution].ToString(CultureInfo.InvariantCulture),
            ChaText        = c.Abilities[(int)BgdaAbility.Charisma].ToString(CultureInfo.InvariantCulture),
            HpCurrentText  = FormatFloat(c.HpCurrent),
            HpMaxText      = FormatFloat(c.HpMax),
            MpCurrentText  = FormatFloat(c.MpCurrent),
            MpMaxText      = FormatFloat(c.MpMax),
            GoldText       = c.Gold.ToString(CultureInfo.InvariantCulture),
            FeatPointsText = c.FeatPoints.ToString(CultureInfo.InvariantCulture)
        }).ToList();
        characterGrid.ItemsSource = _charRows;

        _enemyRows = _save.GetAllEnemies().Select(en => new EnemyRow
        {
            Source     = en,
            SlotNumber = en.SlotNumber,
            TypeName   = en.TypeName,
            TypeId     = en.TypeId,
            HpText     = en.Hp.ToString(CultureInfo.InvariantCulture),
            StateText  = en.IsDead ? "dead" : "alive",
            Position   = string.Format(CultureInfo.InvariantCulture,
                             "({0:0.#}, {1:0.#}, {2:0.#})", en.X, en.Y, en.Z),
            RecordText = string.Format(CultureInfo.InvariantCulture,
                             "0x{0:X5} len 0x{1:X2}", en.RecordOffset, en.RecordLength)
        }).ToList();
        enemyGrid.ItemsSource = _enemyRows;
    }

    private static string FormatFloat(float value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(slotGrid);
        CommitGrid(characterGrid);
        CommitGrid(enemyGrid);

        try
        {
            ApplyDifficulty();
            ApplyCharacters();
            ApplyEnemies();
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

    private static void CommitGrid(DataGrid grid)
    {
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private void ApplyDifficulty()
    {
        foreach (var row in _slotRows)
        {
            var idx = Array.IndexOf(DifficultyNames, row.DifficultyText);
            if (idx < 0)
                throw new ArgumentException($"Slot {row.SlotNumber}: choose a difficulty.");
            _save.SetDifficulty(row.SlotNumber, (BgdaDifficulty)idx);
        }
    }

    private void ApplyCharacters()
    {
        foreach (var row in _charRows)
        {
            var slot = row.SlotNumber;
            _save.SetLevel(slot, ParseInt(row.LevelText, slot, "Level"));
            _save.SetExperience(slot, ParseUInt(row.XpText, slot, "XP"));

            _save.SetAbility(slot, BgdaAbility.Strength,     ParseUInt(row.StrText, slot, "STR"));
            _save.SetAbility(slot, BgdaAbility.Intelligence, ParseUInt(row.IntText, slot, "INT"));
            _save.SetAbility(slot, BgdaAbility.Wisdom,       ParseUInt(row.WisText, slot, "WIS"));
            _save.SetAbility(slot, BgdaAbility.Dexterity,    ParseUInt(row.DexText, slot, "DEX"));
            _save.SetAbility(slot, BgdaAbility.Constitution, ParseUInt(row.ConText, slot, "CON"));
            _save.SetAbility(slot, BgdaAbility.Charisma,     ParseUInt(row.ChaText, slot, "CHA"));


            _save.SetGold(slot, ParseUInt(row.GoldText, slot, "Gold"));
            _save.SetFeatPoints(slot, ParseUInt(row.FeatPointsText, slot, "Feat Points"));
        }
    }

    private void ApplyEnemies()
    {
        foreach (var row in _enemyRows)
        {
            if (row.Source.IsDead) continue;   // dead enemies are not editable
            if (!ushort.TryParse(row.HpText, NumberStyles.Integer,
                                 CultureInfo.InvariantCulture, out var hp))
                throw new ArgumentException(
                    $"Slot {row.SlotNumber}: {row.TypeName} HP must be a whole number.");
            if (hp == row.Source.Hp) continue; // unchanged
            _save.SetEnemyHp(row.Source, hp);
        }
    }

    private static int ParseInt(string text, int slot, string field)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new ArgumentException($"Slot {slot}: {field} must be a whole number.");
        return v;
    }

    private static uint ParseUInt(string text, int slot, string field)
    {
        if (!uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new ArgumentException($"Slot {slot}: {field} must be a non-negative whole number.");
        return v;
    }

}

// ── MainWindow.xaml — under the _Tools menu (unchanged from the difficulty-only build)
//    <MenuItem Header="Edit Save (.psu)…" Click="MenuEditSaveClick" />
//
// ── MainWindow.xaml.cs — the handler (unchanged) ────────────────────────────────
//
//     private void MenuEditSaveClick(object sender, RoutedEventArgs e)
//     {
//         var open = new Microsoft.Win32.OpenFileDialog
//         {
//             Title  = "Open a BGDA PS2 save export (.psu)",
//             Filter = "PS2 Save Export|*.psu|All Files|*.*"
//         };
//         if (open.ShowDialog(this) != true) return;
//
//         var save = JetBlackEngineLib.Data.Save.BgdaSave.Open(open.FileName);
//         if (save.GetSlots().Count == 0)
//         {
//             MessageBox.Show(this,
//                 "No BGDA save slots found in this file. Expected a .psu export of a " +
//                 "BESLES-50672 save.", "Unsupported file",
//                 MessageBoxButton.OK, MessageBoxImage.Warning);
//             return;
//         }
//         new SaveEditorWindow(save) { Owner = this }.ShowDialog();
//     }
