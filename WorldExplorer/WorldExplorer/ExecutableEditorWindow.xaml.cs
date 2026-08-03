// ═══════════════════════════════════════════════════════════════════════════════
// COMPLETE FILE — replace WorldExplorer/WorldExplorer/ExecutableEditorWindow.xaml.cs
//
// Three tabs: XP per Level, Starting Stats, Feats & Spells.
// A bad value in ANY tab aborts the save with a message; nothing is written.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        public bool DamageIsEditable { get; init; }
        /// <summary>Name annotated so the grid shows which spells have live damage.</summary>
        public string DisplayName => DamageIsEditable ? Name + "  \u2605" : Name;
        public string Energy { get; set; } = "";
        public string PerSecond { get; set; } = "";
        public string DamageMin { get; set; } = "";
        public string DamageMax { get; set; } = "";
        public string GlowR { get; set; } = "";
        public string GlowG { get; set; } = "";
        public string GlowB { get; set; } = "";
        public string LightR { get; set; } = "";
        public string LightG { get; set; } = "";
        public string LightB { get; set; } = "";
        public string Coefficient { get; set; } = "";
    }

    /// <summary>
    /// A row in the Monster HP tab. Implements INotifyPropertyChanged so the four
    /// predicted-HP columns refresh as soon as the user edits the multiplier.
    /// </summary>
    public sealed class MonsterRow : INotifyPropertyChanged
    {
        // ── HP model, derived from the executable and verified against in-game saves ──
        //   HP = (rand%6 + 12) x ((f07 + extremeBonus + 2.33) x 0.3) x (f12 x difficulty)
        // Difficulty dispatch at 0x0010D7C4:
        //   Easy x0.7 | Normal x1.0 | Hard x1.3 | Extreme x1.3 AND f07 += 30
        // Predictions using the default f07 (1.0) reproduce measured values exactly for
        // Kobold (4-5 / 5-8 / 7-11 / 77-110) and closely for SmallSpider. A few monsters
        // carry a higher f07 (Slime measured 3.0), so estimates for those read low.
        private const float RollMin = 12f, RollMax = 17f;
        private const float StatBase = 2.33f, StatScale = 0.3f;
        private const float ExtremeBonus = 30f, DefaultF07 = 1f;
        private static readonly float[] DifficultyScale = { 0.7f, 1.0f, 1.3f, 1.3f };
        // Bosses that write HP directly are scaled by a SEPARATE table (0x0012C930),
        // where Extreme is 5.0 rather than 1.3. Verified against Eldrith (literal 1500):
        // measured 1128 / 1508 / 2010 / 7500, the last read exactly from memory.
        //
        // That routine also applies a x1.7 CO-OP multiplier when the player count is 2,
        // stacked on top of the difficulty scalar. Confirmed live: Eldrith in 2-player
        // Extreme read exactly 12750 = 1500 x 5.0 x 1.7. The columns below show
        // SINGLE-PLAYER values; multiply by 1.7 for two players.
        private static readonly float[] BossDifficultyScale = { 0.7f, 1.0f, 1.3f, 5.0f };

        public MonsterHp Source { get; init; } = null!;
        public string Name { get; init; } = "";
        public string KindText { get; init; } = "";
        public string Notes { get; init; } = "";

        private string _multiplier = "";
        public string Multiplier
        {
            get => _multiplier;
            set
            {
                if (_multiplier == value) return;
                _multiplier = value;
                OnChanged(nameof(Multiplier));
                OnChanged(nameof(EasyHp));
                OnChanged(nameof(NormalHp));
                OnChanged(nameof(HardHp));
                OnChanged(nameof(ExtremeHp));
            }
        }

        public string EasyHp    => Predict(0);
        public string NormalHp  => Predict(1);
        public string HardHp    => Predict(2);
        public string ExtremeHp => Predict(3);

        private string Predict(int difficulty)
        {
            // Bosses write HP straight to the entity, overriding the stat routine, so
            // the value is the same on every difficulty.
            if (Source.Kind == MonsterHpKind.DirectHp)
            {
                if (!int.TryParse(_multiplier, NumberStyles.Integer,
                                  CultureInfo.InvariantCulture, out var flat) || flat <= 0)
                    return "";
                return ((int)(flat * BossDifficultyScale[difficulty]))
                       .ToString(CultureInfo.InvariantCulture);
            }

            if (!float.TryParse(_multiplier, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var mult) || mult <= 0f)
                return "";
            var f07 = DefaultF07 + (difficulty == 3 ? ExtremeBonus : 0f);
            var factor = (f07 + StatBase) * StatScale * mult * DifficultyScale[difficulty];
            var lo = (int)(RollMin * factor);
            var hi = (int)(RollMax * factor);
            if (hi < 1) hi = 1;
            if (lo < 1) lo = 1;
            return lo == hi ? lo.ToString(CultureInfo.InvariantCulture)
                            : $"{lo}-{hi}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly BgdaExecutable _exe;
    private readonly List<XpRow> _xpRows;
    private readonly List<StatRow> _statRows;
    private readonly List<FeatRow> _featRows;
    private readonly BgdaMonsters _monsters;
    private readonly List<MonsterRow> _monsterRows;

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
            DamageIsEditable = f.DamageIsEditable,
            Energy = FormatEnergy(f.EnergyCost),
            PerSecond = f.EnergyPerSecond.ToString("0.##"),
            DamageMin = f.DamageMin.ToString(),
            DamageMax = f.DamageMax.ToString(),
            GlowR = f.GlowR.ToString(),
            GlowG = f.GlowG.ToString(),
            GlowB = f.GlowB.ToString(),
            LightR = f.LightR.ToString(),
            LightG = f.LightG.ToString(),
            LightB = f.LightB.ToString(),
            Coefficient = FormatEnergy(f.Coefficient)
        }).ToList();
        featGrid.ItemsSource = _featRows;

        _monsters = BgdaMonsters.Open(exe.FilePath);
        _monsterRows = _monsters.GetMonsters().Select(mon => new MonsterRow
        {
            Source     = mon,
            Name       = mon.Name,
            Multiplier = mon.Kind == MonsterHpKind.DirectHp
                ? (mon.DirectHp?.ToString(CultureInfo.InvariantCulture) ?? "")
                : (mon.Multiplier.HasValue
                    ? mon.Multiplier.Value.ToString("0.####")
                    : ""),
            KindText   = mon.Kind switch
            {
                MonsterHpKind.Immediate   => "Editable",
                MonsterHpKind.DirectHp    => "Editable (HP)",
                MonsterHpKind.Global      => "Global",
                _                         => "MultiBranch"
            },
            Notes      = mon.Kind switch
            {
                MonsterHpKind.Immediate => $"multiplier — patch at 0x{mon.PatchFileOffset:X6}",
                MonsterHpKind.DirectHp  => $"fixed HP, overrides the formula — patch at 0x{mon.PatchFileOffset:X6}",
                MonsterHpKind.Global    => "shared global value — read-only here",
                _                       => $"{mon.BranchCount} runtime branches — read-only here"
            }
        }).ToList();
        monsterGrid.ItemsSource = _monsterRows;
    }

    /// <summary>Trims trailing zeros: 10.0 shows as "10", 0.25 stays "0.25".</summary>
    private static string FormatEnergy(float value) => value.ToString("0.####");

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(xpGrid);
        CommitGrid(statsGrid);
        CommitGrid(featGrid);
        CommitGrid(monsterGrid);

        try
        {
            ApplyXpEdits();
            ApplyStatEdits();
            ApplyFeatEdits();
            ApplyMonsterEdits();
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
        WriteMonsterEdits(dialog.FileName);
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

    /// <summary>
    /// Validates the monster multiplier column. The write itself happens in
    /// <see cref="WriteMonsterEdits"/> AFTER the executable is saved: BgdaExecutable
    /// and BgdaMonsters each hold their own copy of the file, so writing both here
    /// would make one overwrite the other's edits.
    /// </summary>
    private void ApplyMonsterEdits()
    {
        _monstersChanged = false;
        foreach (var row in _monsterRows)
        {
            if (!row.Source.IsEditable) continue;

            if (row.Source.Kind == MonsterHpKind.DirectHp)
            {
                if (!int.TryParse(row.Multiplier, NumberStyles.Integer,
                                  CultureInfo.InvariantCulture, out var hp))
                    throw new ArgumentException(
                        $"{row.Name}: HP must be a whole number.");
                if (hp < 1 || hp > 65535)
                    throw new ArgumentException($"{row.Name}: HP must be 1-65535.");
                if (row.Source.DirectHp != hp) _monstersChanged = true;
                continue;
            }

            if (!float.TryParse(row.Multiplier, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var value))
                throw new ArgumentException(
                    $"{row.Name}: HP multiplier must be a number.");
            if (value < BgdaMonsters.MinMultiplier || value > BgdaMonsters.MaxMultiplier)
                throw new ArgumentException(
                    $"{row.Name}: HP multiplier must be " +
                    $"{BgdaMonsters.MinMultiplier}-{BgdaMonsters.MaxMultiplier}.");
            if (row.Source.Multiplier.HasValue &&
                Math.Abs(value - row.Source.Multiplier.Value) < 1e-6f) continue;
            _monstersChanged = true;
        }
    }

    /// <summary>Re-applies monster edits to the file just written by BgdaExecutable.</summary>
    private void WriteMonsterEdits(string path)
    {
        if (!_monstersChanged) return;
        var monsters = BgdaMonsters.Open(path);
        var byName = new Dictionary<(string, int), MonsterHp>();
        foreach (var mon in monsters.GetMonsters())
            byName[(mon.Name, mon.PatchFileOffset)] = mon;

        foreach (var row in _monsterRows)
        {
            if (!row.Source.IsEditable) continue;
            if (!byName.TryGetValue((row.Source.Name, row.Source.PatchFileOffset),
                                    out var target)) continue;

            if (row.Source.Kind == MonsterHpKind.DirectHp)
            {
                if (!int.TryParse(row.Multiplier, NumberStyles.Integer,
                                  CultureInfo.InvariantCulture, out var hp)) continue;
                if (row.Source.DirectHp == hp) continue;
                monsters.SetDirectHp(target, hp);
                continue;
            }

            if (!float.TryParse(row.Multiplier, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var value)) continue;
            if (row.Source.Multiplier.HasValue &&
                Math.Abs(value - row.Source.Multiplier.Value) < 1e-6f) continue;
            monsters.SetMultiplier(target, value, out _);
        }
        monsters.Save(path);
    }

    private bool _monstersChanged;

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

            if (!int.TryParse(row.DamageMin?.Trim(), out var min) ||
                !int.TryParse(row.DamageMax?.Trim(), out var max))
            {
                throw new ArgumentException($"{row.Name}: damage values must be whole numbers.");
            }

            _exe.SetFeatRange(row.Index, min, max); // validates 0..65535 (no max>=min: see library)

            if (!int.TryParse(row.GlowR?.Trim(), out var gr) ||
                !int.TryParse(row.GlowG?.Trim(), out var gg) ||
                !int.TryParse(row.GlowB?.Trim(), out var gb))
            {
                throw new ArgumentException($"{row.Name}: glow colour channels must be whole numbers.");
            }

            _exe.SetFeatGlow(row.Index, gr, gg, gb); // validates 0..255

            if (!int.TryParse(row.LightR?.Trim(), out var lr) ||
                !int.TryParse(row.LightG?.Trim(), out var lg) ||
                !int.TryParse(row.LightB?.Trim(), out var lb))
            {
                throw new ArgumentException($"{row.Name}: light colour channels must be whole numbers.");
            }

            _exe.SetFeatLight(row.Index, lr, lg, lb); // validates 0..65535

            if (!float.TryParse(row.Coefficient?.Trim(), out var coef))
                throw new ArgumentException($"{row.Name}: coefficient '{row.Coefficient}' is not a number.");
            _exe.SetFeatCoefficient(row.Index, coef); // validates 0..1000
        }
    }
}
