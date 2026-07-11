// ═══════════════════════════════════════════════════════════════════════════════
// COMPLETE FILE — replace WorldExplorer/WorldExplorer/ExecutableEditorWindow.xaml.cs
//
// Three tabs: XP per Level, Starting Stats, Feats & Spells.
// A bad value in ANY tab aborts the save with a message; nothing is written.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JetBlackEngineLib.Data.Executable;
using Microsoft.Win32;

namespace WorldExplorer;

public partial class ExecutableEditorWindow : Window
{
    public sealed class XpRow
    {
        public int Level { get; init; }
        public string Xp { get; set; } = "";
    }

    public sealed class StatRow
    {
        public string ClassName { get; init; } = "";
        public string STR { get; set; } = "";
        public string INT { get; set; } = "";
        public string WIS { get; set; } = "";
        public string DEX { get; set; } = "";
        public string CON { get; set; } = "";
        public string CHA { get; set; } = "";

        public string[] All => new[] { STR, INT, WIS, DEX, CON, CHA };
    }

    public sealed class FeatRow
    {
        public int Index { get; init; }
        public string Name { get; init; } = "";
        public string Energy { get; set; } = "";
        public string PerSecond { get; set; } = "";
        public string RangeMin { get; set; } = "";
        public string RangeMax { get; set; } = "";
        public string R { get; set; } = "";
        public string G { get; set; } = "";
        public string B { get; set; } = "";
    }

    private readonly BgdaExecutable _exe;
    private readonly List<XpRow> _xpRows;
    private readonly List<StatRow> _statRows;
    private readonly List<FeatRow> _featRows;

    public ExecutableEditorWindow(BgdaExecutable exe)
    {
        InitializeComponent();
        _exe = exe;

        var xp = _exe.GetXpTable();
        _xpRows = xp.Select((v, i) => new XpRow { Level = i + 1, Xp = v.ToString() }).ToList();
        xpGrid.ItemsSource = _xpRows;

        var stats = _exe.GetStatsTable();
        _statRows = new List<StatRow>();
        for (var c = 0; c < BgdaExecutable.StatsClassCount; c++)
        {
            _statRows.Add(new StatRow
            {
                ClassName = BgdaExecutable.ClassNames[c],
                STR = stats[c, 0].ToString(),
                INT = stats[c, 1].ToString(),
                WIS = stats[c, 2].ToString(),
                DEX = stats[c, 3].ToString(),
                CON = stats[c, 4].ToString(),
                CHA = stats[c, 5].ToString()
            });
        }

        statsGrid.ItemsSource = _statRows;

        _featRows = _exe.GetFeatSpells().Select(f => new FeatRow
        {
            Index = f.Index,
            Name = f.Name,
            Energy = FormatEnergy(f.EnergyCost),
            PerSecond = f.EnergyPerSecond.ToString("0.##"),
            RangeMin = f.DisplayMin.ToString(),
            RangeMax = f.DisplayMax.ToString(),
            R = f.ColorR.ToString(),
            G = f.ColorG.ToString(),
            B = f.ColorB.ToString()
        }).ToList();
        featGrid.ItemsSource = _featRows;
    }

    /// <summary>Trims trailing zeros: 10.0 shows as "10", 0.25 stays "0.25".</summary>
    private static string FormatEnergy(float value) => value.ToString("0.####");

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(xpGrid);
        CommitGrid(statsGrid);
        CommitGrid(featGrid);

        try
        {
            ApplyXpEdits();
            ApplyStatEdits();
            ApplyFeatEdits();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Invalid value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return; // nothing written to disk
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PS2 Executable|SLES_506.72;SLUS_*;*.*|All Files|*.*",
            FileName = System.IO.Path.GetFileName(_exe.FilePath)
        };
        if (dialog.ShowDialog(this) != true) return;

        _exe.Save(dialog.FileName);
        MessageBox.Show(this,
            "Saved. Replace the executable in your game image with this file " +
            "(keep your original as a backup). Changes affect new characters and future level-ups.",
            "Executable Tuning", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void CommitGrid(DataGrid grid)
    {
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private void ApplyXpEdits()
    {
        var xp = new int[BgdaExecutable.XpTableCount];
        for (var i = 0; i < xp.Length; i++)
        {
            if (!int.TryParse(_xpRows[i].Xp?.Trim(), out xp[i]))
                throw new ArgumentException($"Level {i + 1}: '{_xpRows[i].Xp}' is not a whole number.");
        }

        _exe.SetXpTable(xp); // validates ascending / starts at zero
    }

    private void ApplyStatEdits()
    {
        for (var c = 0; c < _statRows.Count; c++)
        {
            var values = _statRows[c].All;
            for (var s = 0; s < BgdaExecutable.StatsPerClass; s++)
            {
                if (!int.TryParse(values[s]?.Trim(), out var v))
                {
                    throw new ArgumentException(
                        $"{_statRows[c].ClassName} {BgdaExecutable.StatNames[s]}: not a whole number.");
                }

                _exe.SetStat((BgdaClass)c, s, v); // validates 1..30
            }
        }
    }

    private void ApplyFeatEdits()
    {
        foreach (var row in _featRows)
        {
            if (!float.TryParse(row.Energy?.Trim(), out var energy))
                throw new ArgumentException($"{row.Name}: energy '{row.Energy}' is not a number.");
            _exe.SetFeatEnergy(row.Index, energy); // validates 0..1000

            if (!int.TryParse(row.RangeMin?.Trim(), out var min) ||
                !int.TryParse(row.RangeMax?.Trim(), out var max))
            {
                throw new ArgumentException($"{row.Name}: range values must be whole numbers.");
            }

            _exe.SetFeatRange(row.Index, min, max); // validates min <= max

            if (!int.TryParse(row.R?.Trim(), out var r) ||
                !int.TryParse(row.G?.Trim(), out var g) ||
                !int.TryParse(row.B?.Trim(), out var b))
            {
                throw new ArgumentException($"{row.Name}: colour channels must be whole numbers.");
            }

            _exe.SetFeatColor(row.Index, r, g, b); // validates 0..255
        }
    }
}
