// ═══════════════════════════════════════════════════════════════════════════════
// COMPLETE FILE — replace WorldExplorer/WorldExplorer/ExecutableEditorWindow.xaml.cs
//
// Tabs: XP per Level, Starting Stats, Feats & Spells, Items, Monster HP, Three Players.
// A bad value in ANY tab aborts the save with a message; nothing is written.
//
// Items (Views/ItemEditorView, ItemEditorModel): weapons, armour and other items, their models,
// the shops, materials, stance weights, the enchant glow and the random-loot lists, written all
// together (BgdaExecutable.ApplyItemEdits). The names the game shows are in GTEXT.LMP, found next
// to the executable or in the data folder of the settings; changed names are saved to it after
// the executable. Items can be added after the game's own on three-player executables (the patch
// frees the room), so three players can't be turned off while there are any.
//
// Three Players: switches the tested three-player patch (ADRIANNA26) on or off,
// updates an older three-player build (ADRIANNA25 to 19) to ADRIANNA26 on
// save, edits its balance / marker colours / small map, and edits the difficulty
// scaling for every player count (BgdaExecutable.ThreePlayer.cs / .Difficulty.cs).
// With three players on, a second box (ticked by default) also puts player 3 in the
// game's own cutscenes when you save: only those cutscene entries of the game's GOB
// files change (ThreePlayerCutscenes, found like GTEXT.LMP). Turning three players
// off offers to put the game's own cutscenes back when one with player 3 needs ADRIANNA26.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using Microsoft.Win32;
using WorldExplorer.Themes;
using WorldExplorer.Views;

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
    /// Difficulty factors used by the Monster HP predictions. One instance per window,
    /// filled from the executable and updated live from the difficulty scaling grid on
    /// the Three Players tab. Index 0 = Easy ... 3 = Extreme.
    /// </summary>
    public sealed class DifficultyModel
    {
        public float[] Hp { get; } = { 0.7f, 1.0f, 1.3f, 1.3f };
        public float[] Level { get; } = { 0f, 0f, 0f, 30f };
        public float[] Boss { get; } = { 0.7f, 1.0f, 1.3f, 5.0f };
    }

    /// <summary>A row of the difficulty scaling grid (Three Players tab).</summary>
    public sealed class DifficultyRow : INotifyPropertyChanged
    {
        public int Index { get; init; }
        public string Name { get; init; } = "";

        private string _hp = "", _damage = "", _level = "", _boss = "";
        public string Hp { get => _hp; set => Set(ref _hp, value, nameof(Hp)); }
        public string Damage { get => _damage; set => Set(ref _damage, value, nameof(Damage)); }
        public string Level { get => _level; set => Set(ref _level, value, nameof(Level)); }
        public string Boss { get => _boss; set => Set(ref _boss, value, nameof(Boss)); }

        private void Set(ref string field, string value, string name)
        {
            if (field == value) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// A row in the Monster HP tab. Implements INotifyPropertyChanged so the four
    /// predicted-HP columns refresh as soon as the user edits the multiplier (or the
    /// difficulty scaling on the Three Players tab).
    /// </summary>
    public sealed class MonsterRow : INotifyPropertyChanged
    {
        // ── HP model, derived from the executable and verified against in-game saves ──
        //   HP = (rand%6 + 12) x ((f07 + levelBonus + 2.33) x 0.3) x (f12 x difficulty)
        // Difficulty dispatch at 0x0010D7C4 (stock; editable on the Three Players tab):
        //   Easy x0.7 | Normal x1.0 | Hard x1.3 | Extreme x1.3 AND f07 += 30
        // Predictions using the default f07 (1.0) reproduce measured values exactly for
        // Kobold (4-5 / 5-8 / 7-11 / 77-110) and closely for SmallSpider. A few monsters
        // carry a higher f07 (Slime measured 3.0), so estimates for those read low.
        private const float RollMin = 12f, RollMax = 17f;
        private const float StatBase = 2.33f, StatScale = 0.3f;
        private const float DefaultF07 = 1f;
        // Bosses that write HP directly are scaled by a SEPARATE table (0x0012C930),
        // where Extreme is 5.0 rather than 1.3. Verified against Eldrith (literal 1500):
        // measured 1128 / 1508 / 2010 / 7500, the last read exactly from memory.
        //
        // That routine also applies a x1.7 CO-OP multiplier when the player count is 2,
        // stacked on top of the difficulty scalar. Confirmed live: Eldrith in 2-player
        // Extreme read exactly 12750 = 1500 x 5.0 x 1.7. The columns below show
        // SINGLE-PLAYER values; multiply by 1.7 for two players.
        // The scaling factors themselves come from Model (stock: boss 0.7/1/1.3/5).
        public DifficultyModel Model { get; init; } = new();

        public MonsterHp Source { get; init; } = null!;
        public string Name { get; init; } = "";
        public string KindText { get; init; } = "";
        public string Resistances { get; init; } = "";
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
                RefreshPredictions();
            }
        }

        /// <summary>Re-evaluates the four predicted-HP columns.</summary>
        public void RefreshPredictions()
        {
            OnChanged(nameof(EasyHp));
            OnChanged(nameof(NormalHp));
            OnChanged(nameof(HardHp));
            OnChanged(nameof(ExtremeHp));
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
                return ((int)(flat * Model.Boss[difficulty]))
                       .ToString(CultureInfo.InvariantCulture);
            }

            if (!float.TryParse(_multiplier, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var mult) || mult <= 0f)
                return "";
            var f07 = DefaultF07 + Model.Level[difficulty];
            var factor = (f07 + StatBase) * StatScale * mult * Model.Hp[difficulty];
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
    private readonly DifficultyModel _difficultyModel = new();
    private readonly List<DifficultyRow> _difficultyRows;
    private readonly bool _difficultyEditable;
    private readonly ItemEditorModel _items;

    /// <summary>Where the game's files are looked for: next to the executable, then the data folder in Settings.</summary>
    private readonly string?[] _gameFolders;

    /// <summary>What each cutscene with player 3 is in the game's GOB files (Three Players tab).</summary>
    private List<ThreePlayerCutsceneStatus> _cutscenes = new();

    public ExecutableEditorWindow(BgdaExecutable exe)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _exe = exe;
        _gameFolders = new[] { System.IO.Path.GetDirectoryName(_exe.FilePath), App.Settings.Get("Files.DataPath", "") };

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

        // ── Items tab: the names and model files come from the game's data folder when they can be found ──
        _items = new ItemEditorModel(_exe);
        itemsView.DataContext = _items;
        _items.PropertyChanged += (_, e) =>
        {
            // added items keep three players on: the Three Players tab says so as soon as there are any
            if (e.PropertyName == nameof(ItemEditorModel.AddedCount) && threePlayerStatus is not null) UpdateThreePlayerStatus();
        };
        if (_items.IsSupported)
        {
            if (ItemEditorModel.FindGameFile("GTEXT.LMP", _gameFolders) is { } gtext)
                ItemEditorView.TryLoadNames(_items, gtext, this, quiet: true);
            if (ItemEditorModel.FindGameFile("EQUIPMNT.LMP", _gameFolders) is { } equipment)
                ItemEditorView.TryLoadEquipment(_items, equipment, this, quiet: true);
        }

        _monsters = BgdaMonsters.Open(exe.FilePath);
        _monsterRows = _monsters.GetMonsters().Select(mon => new MonsterRow
        {
            Model      = _difficultyModel,
            Source     = mon,
            Name       = mon.Name,
            Multiplier = mon.Kind == MonsterHpKind.DirectHp
                ? (mon.DirectHp?.ToString(CultureInfo.InvariantCulture) ?? "")
                : (mon.Multiplier.HasValue
                    ? mon.Multiplier.Value.ToString("0.####", CultureInfo.InvariantCulture)
                    : ""),
            Resistances = BgdaMonsters.DescribeResistances(mon.ResistMask),
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

        // ── Three Players tab ──
        var tpState = _exe.GetThreePlayerState();
        var tpOn = tpState is ThreePlayerState.On or ThreePlayerState.Outdated;
        threePlayerCheck.IsChecked = tpOn;
        threePlayerCheck.IsEnabled = tpState != ThreePlayerState.Unrecognized;
        ShowThreePlayerSettings(tpOn
            ? _exe.GetThreePlayerSettings()
            : BgdaExecutable.DefaultThreePlayerSettings());
        threePlayerCutscenesCheck.ToolTip = CutscenesToolTip();
        RefreshCutscenes();
        UpdateThreePlayerStatus();

        var difficultyState = _exe.GetDifficultyState();
        _difficultyEditable = difficultyState != DifficultyCodeState.Unrecognized;
        var scaling = _difficultyEditable ? _exe.GetDifficultyScaling() : BgdaExecutable.StockDifficulty();
        _difficultyRows = Enumerable.Range(0, 4).Select(i => new DifficultyRow
        {
            Index = i,
            Name = BgdaExecutable.DifficultyNames[i],
            Hp = Format(scaling.MonsterHp[i]),
            Damage = Format(scaling.MonsterDamage[i]),
            Level = Format(scaling.LevelBonus[i]),
            Boss = Format(scaling.BossHp[i])
        }).ToList();
        foreach (var row in _difficultyRows) row.PropertyChanged += (_, _) => RefreshMonsterPredictions();
        difficultyGrid.ItemsSource = _difficultyRows;
        difficultyGrid.IsReadOnly = !_difficultyEditable;
        difficultyStockButton.IsEnabled = _difficultyEditable;
        UpdateDifficultyStatus();
        RefreshMonsterPredictions();
    }

    /// <summary>Shortest text that reads back as the same float (0.7, 1.3, 2.4 ...).</summary>
    private static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Trims trailing zeros: 10.0 shows as "10", 0.25 stays "0.25".</summary>
    private static string FormatEnergy(float value) => value.ToString("0.####");

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(xpGrid);
        CommitGrid(statsGrid);
        CommitGrid(featGrid);
        CommitGrid(monsterGrid);
        CommitGrid(difficultyGrid);

        var wantThreePlayers = threePlayerCheck.IsChecked == true;
        ThreePlayerSettings? threePlayerSettings;
        DifficultyScaling? difficulty;
        try
        {
            // the Three Players tab is only read and checked here; it is applied once a
            // file name has been chosen, so cancelling leaves the loaded executable as it was
            threePlayerSettings = wantThreePlayers ? ReadThreePlayerSettings() : null;
            difficulty = _difficultyEditable ? ReadDifficulty() : null;

            ApplyXpEdits();
            ApplyStatEdits();
            ApplyFeatEdits();
            ApplyMonsterEdits();
            _items.Apply(); // checks every item box, then writes them all or none
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
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

        var stateBefore = _exe.GetThreePlayerState();
        try
        {
            ApplyThreePlayers(wantThreePlayers, threePlayerSettings);
            if (difficulty is not null) _exe.SetDifficultyScaling(difficulty);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Executable Tuning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return; // nothing written to disk
        }

        _exe.Save(dialog.FileName);
        WriteMonsterEdits(dialog.FileName);
        _items.RoomChanged(); // three players turned on make room for added items
        UpdateThreePlayerStatus();
        UpdateDifficultyStatus();
        var namesSaved = _items.NamesNeedSaving && SaveNames();
        var cutscenes = UpdateGameCutscenes(wantThreePlayers, stateBefore);
        MessageBox.Show(this,
            "Saved. Replace the executable in your game image with this file " +
            (namesSaved ? "and GTEXT.LMP with the one you saved " : "") +
            "(keep your originals as a backup). Stats and XP affect new characters and future level-ups; " +
            "items, shops and loot every game." +
            (cutscenes is null ? "" : "\n\n" + cutscenes),
            "Executable Tuning", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Puts the changed item names in GTEXT.LMP and saves it where the user says; false when not saved.</summary>
    private bool SaveNames()
    {
        try
        {
            _items.ApplyNames();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message + " The names weren't saved.", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save the item names (GTEXT.LMP)",
            Filter = "Game text|GTEXT.LMP|All Files|*.*",
            FileName = "GTEXT.LMP"
        };
        if (_items.NamesPath is { } loaded && System.IO.Path.GetDirectoryName(loaded) is { } folder && Directory.Exists(folder))
            dialog.InitialDirectory = folder;
        if (dialog.ShowDialog(this) != true) return false;

        var bytes = _items.Names!.ToArray();
        File.WriteAllBytes(dialog.FileName, bytes);
        _items.NamesSaved(GameText.Read(bytes), dialog.FileName);
        return true;
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

    // ═══════════════════════════ Three Players tab ═══════════════════════════

    private TextBox[] MarkerBoxes => new[]
    {
        m1RBox, m1GBox, m1BBox, m2RBox, m2GBox, m2BBox, m3RBox, m3GBox, m3BBox
    };

    private void ShowThreePlayerSettings(ThreePlayerSettings s)
    {
        tpHpBox.Text = Format(s.MonsterHp);
        tpDamageBox.Text = Format(s.MonsterDamage);
        tpBossBox.Text = Format(s.BossHp);
        tpXpBox.Text = Format(s.XpValue);
        tpGoldBox.Text = Format(s.GoldPerDrop);
        tpGoldChanceBox.Text = Format(s.GoldChance);
        tpItemBox.Text = Format(s.ItemChance);
        tpOrbBox.Text = s.OrbDivisor.ToString(CultureInfo.InvariantCulture);
        var boxes = MarkerBoxes;
        for (var p = 0; p < 3; p++)
        {
            boxes[p * 3].Text = s.Markers[p].R.ToString(CultureInfo.InvariantCulture);
            boxes[p * 3 + 1].Text = s.Markers[p].G.ToString(CultureInfo.InvariantCulture);
            boxes[p * 3 + 2].Text = s.Markers[p].B.ToString(CultureInfo.InvariantCulture);
        }

        smXBox.Text = Format(s.SmallMapX);
        smYBox.Text = Format(s.SmallMapY);
        smWBox.Text = Format(s.SmallMapWidth);
        smHBox.Text = Format(s.SmallMapHeight);
        UpdateMarkerSwatches();
    }

    /// <summary>Reads and checks every three-player box (throws <see cref="ArgumentException"/>).</summary>
    private ThreePlayerSettings ReadThreePlayerSettings()
    {
        var boxes = MarkerBoxes;
        Rgb Marker(int p) => new(
            ParseInt(boxes[p * 3], $"Player {p + 1} marker red"),
            ParseInt(boxes[p * 3 + 1], $"Player {p + 1} marker green"),
            ParseInt(boxes[p * 3 + 2], $"Player {p + 1} marker blue"));

        var s = new ThreePlayerSettings
        {
            MonsterHp = ParseFloat(tpHpBox, "Three-player monster HP"),
            MonsterDamage = ParseFloat(tpDamageBox, "Three-player monster damage"),
            BossHp = ParseFloat(tpBossBox, "Three-player boss HP"),
            XpValue = ParseFloat(tpXpBox, "Three-player XP value"),
            GoldPerDrop = ParseFloat(tpGoldBox, "Three-player gold per drop"),
            GoldChance = ParseFloat(tpGoldChanceBox, "Three-player gold drop chance"),
            ItemChance = ParseFloat(tpItemBox, "Three-player item drop chance"),
            OrbDivisor = ParseInt(tpOrbBox, "Orb of Undeath divisor"),
            Markers = new[] { Marker(0), Marker(1), Marker(2) },
            SmallMapX = ParseFloat(smXBox, "Small-map x"),
            SmallMapY = ParseFloat(smYBox, "Small-map y"),
            SmallMapWidth = ParseFloat(smWBox, "Small-map width"),
            SmallMapHeight = ParseFloat(smHBox, "Small-map height")
        };
        BgdaExecutable.ValidateThreePlayerSettings(s);
        return s;
    }

    /// <summary>
    /// Turns the patch on or off as ticked (an older three-player build is updated to the
    /// current one), then writes the settings.
    /// </summary>
    private void ApplyThreePlayers(bool want, ThreePlayerSettings? settings)
    {
        var state = _exe.GetThreePlayerState();
        if (want && state == ThreePlayerState.Off) _exe.EnableThreePlayers();
        else if (want && state == ThreePlayerState.Outdated) _exe.UpgradeThreePlayers();
        else if (!want && state is (ThreePlayerState.On or ThreePlayerState.Outdated)) _exe.DisableThreePlayers();
        if (want && settings is not null) _exe.SetThreePlayerSettings(settings);
    }

    private void UpdateThreePlayerStatus()
    {
        var state = _exe.GetThreePlayerState(out var detail);
        var want = threePlayerCheck.IsChecked == true;
        threePlayerStatus.Text = state switch
        {
            ThreePlayerState.Off when want =>
                "Three players will be turned on when you save. The file grows from " +
                $"{MegaBytes(BgdaExecutable.StockFileLength)} to {MegaBytes(BgdaExecutable.ThreePlayerFileLength)}.",
            ThreePlayerState.On or ThreePlayerState.Outdated when !want && _items.AddedCount > 0 =>
                $"Three players can't be turned off while the Items tab has {_items.AddedCount} added item" +
                $"{(_items.AddedCount == 1 ? "" : "s")}: they use the room the three-player patch frees. Remove them there first.",
            ThreePlayerState.On or ThreePlayerState.Outdated when !want =>
                "Three players will be turned off when you save: the original code is restored and the " +
                "three-player settings below are discarded.",
            _ => detail
        };
        threePlayerSettingsPanel.IsEnabled = want && state != ThreePlayerState.Unrecognized;
        UpdateCutsceneStatus();
    }

    private void ThreePlayerCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || threePlayerStatus is null) return;
        UpdateThreePlayerStatus();
    }

    // ── The game's cutscenes: player 3 in the GOB files (ThreePlayerCutscenes) ──

    private const string WhereGobFilesAreLooked =
        "The GOB files are looked for next to the executable (and in DATA and BG\\DATA below it), then in the data " +
        "folder set in Settings.";

    /// <summary>Set when the cutscene data in WorldExplorer can't be used; shown instead of the plan.</summary>
    private string? _cutsceneProblem;

    private string? FindGob(string name) => ItemEditorModel.FindGameFile(name, _gameFolders);

    /// <summary>Looks again at what the cutscenes with player 3 are in the game's GOB files.</summary>
    private void RefreshCutscenes()
    {
        try
        {
            _cutscenes = ThreePlayerCutscenes.Inspect(FindGob);
        }
        catch (InvalidDataException ex)
        {
            _cutscenes = new List<ThreePlayerCutsceneStatus>();
            _cutsceneProblem = ex.Message;
            threePlayerCutscenesCheck.IsChecked = false;
        }
    }

    private static string CutscenesToolTip()
    {
        try
        {
            return "When you save with three players on, these cutscenes get a cast member called Player3, so player 3 " +
                   "appears in them in three-player games: " +
                   string.Join(", ", ThreePlayerCutscenes.All.Select(c => c.ToString())) + ". Only those entries of the " +
                   "GOB files change, and only where a file still has the game's own cutscene: one changed in any other " +
                   "way is left as it is. " + WhereGobFilesAreLooked;
        }
        catch (InvalidDataException ex)
        {
            return ex.Message;
        }
    }

    /// <summary>The line under the cutscene box: what saving will do to the game's GOB files.</summary>
    private void UpdateCutsceneStatus()
    {
        if (threePlayerCutscenesStatus is null) return; // still loading
        var state = _exe.GetThreePlayerState();
        var want = threePlayerCheck.IsChecked == true;
        string? text;
        if (_cutsceneProblem is not null)
        {
            text = _cutsceneProblem;
        }
        else if (want && threePlayerCutscenesCheck.IsChecked == true)
        {
            text = ThreePlayerCutscenes.DescribePlan(_cutscenes);
            if (_cutscenes.All(s => s.Path == null)) text += " " + WhereGobFilesAreLooked;
        }
        else if (want)
        {
            text = "The cutscenes in the game's GOB files are left as they are.";
        }
        else
        {
            // three players about to be turned off: say what that means for cutscenes with player 3
            text = state is ThreePlayerState.On or ThreePlayerState.Outdated ? ThreePlayerCutscenes.DescribeTurnOff(_cutscenes) : null;
        }

        threePlayerCutscenesStatus.Text = text ?? "";
        threePlayerCutscenesStatus.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ThreePlayerCutscenesCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        UpdateCutsceneStatus();
    }

    /// <summary>
    /// After the executable is saved: with three players on and the cutscene box ticked, puts player 3 in the game's
    /// cutscenes. When three players have just been turned off and a cutscene with player 3 in the game's GOB files
    /// needs ADRIANNA26, asks whether to put the game's own back. Returns what was done for the saved message, or null.
    /// </summary>
    private string? UpdateGameCutscenes(bool wantThreePlayers, ThreePlayerState stateBefore)
    {
        var addPlayer3 = wantThreePlayers && threePlayerCutscenesCheck.IsChecked == true && _cutsceneProblem is null;
        var turnedOff = !wantThreePlayers && stateBefore is (ThreePlayerState.On or ThreePlayerState.Outdated);
        if (!addPlayer3 && !turnedOff) return null;

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            ThreePlayerCutsceneResult result;
            if (addPlayer3)
            {
                result = ThreePlayerCutscenes.Apply(FindGob, withPlayer3: true);
            }
            else
            {
                var now = ThreePlayerCutscenes.Inspect(FindGob);
                if (!ThreePlayerCutscenes.ShouldOfferPuttingBack(now)) return null;
                Mouse.OverrideCursor = null;
                if (MessageBox.Show(this, ThreePlayerCutscenes.PuttingBackQuestion(now), "Three Players",
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes)
                    return "The cutscenes with player 3 were left in the game's GOB files.";
                Mouse.OverrideCursor = Cursors.Wait;
                result = ThreePlayerCutscenes.Apply(FindGob, withPlayer3: false);
            }

            var text = result.Describe();
            if (result.Changed.Count > 0)
                text += " If WorldExplorer has one of these GOB files open, open it again before you save it.";
            return text;
        }
        catch (InvalidDataException ex)
        {
            return "The cutscenes weren't changed: " + ex.Message;
        }
        finally
        {
            Mouse.OverrideCursor = null;
            RefreshCutscenes();
            UpdateCutsceneStatus();
        }
    }

    private void ThreePlayerDefaults_Click(object sender, RoutedEventArgs e) =>
        ShowThreePlayerSettings(BgdaExecutable.DefaultThreePlayerSettings());

    private void Marker_TextChanged(object sender, TextChangedEventArgs e) => UpdateMarkerSwatches();

    private void UpdateMarkerSwatches()
    {
        if (m3Swatch is null) return;   // still loading
        var boxes = MarkerBoxes;
        var swatches = new[] { m1Swatch, m2Swatch, m3Swatch };
        for (var p = 0; p < 3; p++)
        {
            byte Channel(TextBox b) =>
                int.TryParse(b.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
                    ? (byte)Math.Clamp(v, 0, 255)
                    : (byte)0;
            swatches[p].Background = new SolidColorBrush(Color.FromRgb(
                Channel(boxes[p * 3]), Channel(boxes[p * 3 + 1]), Channel(boxes[p * 3 + 2])));
        }
    }

    /// <summary>Reads and checks the difficulty grid (throws <see cref="ArgumentException"/>).</summary>
    private DifficultyScaling ReadDifficulty()
    {
        var d = new DifficultyScaling();
        foreach (var row in _difficultyRows)
        {
            d.MonsterHp[row.Index] = ParseFloat(row.Hp, $"{row.Name} monster HP");
            d.MonsterDamage[row.Index] = ParseFloat(row.Damage, $"{row.Name} monster damage");
            d.LevelBonus[row.Index] = ParseFloat(row.Level, $"{row.Name} level bonus");
            d.BossHp[row.Index] = ParseFloat(row.Boss, $"{row.Name} boss HP");
        }

        BgdaExecutable.ValidateDifficulty(d);
        return d;
    }

    private void UpdateDifficultyStatus() =>
        difficultyStatus.Text = _exe.GetDifficultyState() switch
        {
            DifficultyCodeState.Stock => "This executable has the original difficulty code.",
            DifficultyCodeState.Table => "This executable has edited difficulty scaling.",
            _ => "The difficulty code in this executable is not recognised (another patch?), so it is shown " +
                 "read-only with the stock values."
        };

    private static string MegaBytes(int bytes) =>
        (bytes / 1_000_000.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB";

    /// <summary>
    /// The difficulty grid sits inside the tab's ScrollViewer; its own scroll viewer would
    /// swallow the mouse wheel, so the wheel is handed on to the page.
    /// </summary>
    private void DifficultyGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not FrameworkElement { Parent: UIElement parent }) return;
        e.Handled = true;
        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        });
    }

    private void DifficultyStock_Click(object sender, RoutedEventArgs e)
    {
        CommitGrid(difficultyGrid);
        var stock = BgdaExecutable.StockDifficulty();
        foreach (var row in _difficultyRows)
        {
            row.Hp = Format(stock.MonsterHp[row.Index]);
            row.Damage = Format(stock.MonsterDamage[row.Index]);
            row.Level = Format(stock.LevelBonus[row.Index]);
            row.Boss = Format(stock.BossHp[row.Index]);
        }
    }

    /// <summary>Feeds the difficulty grid into the Monster HP predictions (bad cells are skipped).</summary>
    private void RefreshMonsterPredictions()
    {
        foreach (var row in _difficultyRows)
        {
            if (TryParse(row.Hp, out var hp) && hp > 0f) _difficultyModel.Hp[row.Index] = hp;
            if (TryParse(row.Level, out var level) && level >= 0f) _difficultyModel.Level[row.Index] = level;
            if (TryParse(row.Boss, out var boss) && boss > 0f) _difficultyModel.Boss[row.Index] = boss;
        }

        foreach (var row in _monsterRows) row.RefreshPredictions();
    }

    private static bool TryParse(string? text, out float value) =>
        float.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static float ParseFloat(string? text, string what)
    {
        if (!TryParse(text, out var v))
            throw new ArgumentException($"{what}: '{text}' is not a number.");
        return v;
    }

    private static float ParseFloat(TextBox box, string what) => ParseFloat(box.Text, what);

    private static int ParseInt(TextBox box, string what)
    {
        if (!int.TryParse(box.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new ArgumentException($"{what}: '{box.Text}' is not a whole number.");
        return v;
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
