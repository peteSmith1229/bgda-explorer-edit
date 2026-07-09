using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JetBlackEngineLib.Data.Executable;
using Microsoft.Win32;

namespace WorldExplorer;

public partial class ExecutableEditorWindow : Window
{
    public sealed class XpRow { public int Level { get; init; } public string Xp { get; set; } = ""; }

    public sealed class StatRow
    {
        public string ClassName { get; init; } = "";
        public string STR { get; set; } = ""; public string INT { get; set; } = "";
        public string WIS { get; set; } = ""; public string DEX { get; set; } = "";
        public string CON { get; set; } = ""; public string CHA { get; set; } = "";
        public string[] All => new[] { STR, INT, WIS, DEX, CON, CHA };
    }

    private readonly BgdaExecutable _exe;
    private readonly List<XpRow> _xpRows;
    private readonly List<StatRow> _statRows;

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
            _statRows.Add(new StatRow
            {
                ClassName = BgdaExecutable.ClassNames[c],
                STR = stats[c, 0].ToString(), INT = stats[c, 1].ToString(),
                WIS = stats[c, 2].ToString(), DEX = stats[c, 3].ToString(),
                CON = stats[c, 4].ToString(), CHA = stats[c, 5].ToString()
            });
        statsGrid.ItemsSource = _statRows;
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        xpGrid.CommitEdit(); statsGrid.CommitEdit();
        try
        {
            // XP table
            var xp = new int[BgdaExecutable.XpTableCount];
            for (var i = 0; i < xp.Length; i++)
            {
                if (!int.TryParse(_xpRows[i].Xp?.Trim(), out xp[i]))
                    throw new ArgumentException($"Level {i + 1}: '{_xpRows[i].Xp}' is not a whole number.");
            }
            _exe.SetXpTable(xp);   // validates ascending / starts-at-0

            // stats table
            for (var c = 0; c < _statRows.Count; c++)
                for (var s = 0; s < BgdaExecutable.StatsPerClass; s++)
                {
                    if (!int.TryParse(_statRows[c].All[s]?.Trim(), out var v))
                        throw new ArgumentException(
                            $"{_statRows[c].ClassName} {BgdaExecutable.StatNames[s]}: not a whole number.");
                    _exe.SetStat((BgdaClass)c, s, v);   // validates 1..30
                }
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;   // nothing written to disk
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
}
