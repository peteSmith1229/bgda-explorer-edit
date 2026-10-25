// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/Da2TuningModel.cs
//
// What the Dark Alliance II Executable Tuning window shows and edits (Da2TuningWindow):
//   - the executable's tables (Da2Executable): XP per level, starting stats, abilities, class
//     feats and the four spells' price tables, items, levels, creature multipliers, boss HP and
//     the difficulty / two-player factors
//   - the three-player mod's groups and their settings (Da2ThreePlayerMod): the main group, HUD
//     sizes, player 3's character groups, the test helpers, placed creatures, shops, feat prices
// Rows hold text exactly as typed; Apply() checks every value (ArgumentException names the first
// bad one) and makes the cheat file. Nothing is written to the executable: the edits become the
// "[WorldExplorer - executable tuning]" group of the cheat file.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Pnach;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>A value in a grid cell, editable or not; cells that show the same constant share one.</summary>
public sealed class Da2Cell : ObservableObject
{
    private string _text;

    public Da2Cell(string text, bool editable)
    {
        _text = text;
        IsEditable = editable;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (!IsEditable) return;
            if (SetProperty(ref _text, value)) Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsEditable { get; }
    public bool IsReadOnly => !IsEditable;
    public event EventHandler? Changed;

    /// <summary>Sets the text even on a read-only cell (when the model refreshes it).</summary>
    public void Show(string text)
    {
        if (_text == text) return;
        _text = text;
        OnPropertyChanged(nameof(Text));
    }
}

public sealed class Da2XpRow
{
    public int Level { get; init; }
    public string Xp { get; set; } = "";
}

public sealed class Da2StatRow
{
    public int ClassId { get; init; }
    public string ClassName { get; init; } = "";
    public string STR { get; set; } = "";
    public string DEX { get; set; } = "";
    public string CON { get; set; } = "";
    public string INT { get; set; } = "";
    public string WIS { get; set; } = "";
    public string CHA { get; set; } = "";
    public string[] All => new[] { STR, DEX, CON, INT, WIS, CHA };
}

public sealed class Da2AbilityRow
{
    public Da2Ability Source { get; init; } = null!;
    public string Name => Source.Name;
    public string Feat => Da2Names.IsFeat(Source.FeatId) ? Da2Names.Feat(Source.FeatId) : "";
    public string Type => Source.Type.ToString(CultureInfo.InvariantCulture) + (Source.IsStream ? " (per frame)" : "");
    public string Energy { get; set; } = "";
    public string PerRank { get; set; } = "";
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

public sealed class Da2ClassFeatRow : ObservableObject
{
    private string _maxRank = "", _baseCost = "";
    public Da2ClassFeat Source { get; init; } = null!;
    public Func<int, int[]?> SpecialPrices { get; init; } = _ => null;
    public string ClassName => Da2Names.ClassName(Source.ClassId);
    public string Feat => Source.Name;

    public string MaxRank
    {
        get => _maxRank;
        set { if (SetProperty(ref _maxRank, value)) OnPropertyChanged(nameof(Prices)); }
    }

    public string BaseCost
    {
        get => _baseCost;
        set { if (SetProperty(ref _baseCost, value)) OnPropertyChanged(nameof(Prices)); }
    }

    /// <summary>The game's price of each rank for the values typed (the mod's feat group may override them).</summary>
    public string Prices
    {
        get
        {
            if (!int.TryParse(_maxRank, out var max) || !int.TryParse(_baseCost, out var cost) || max is < 1 or > 5 || cost < 1) return "";
            return string.Join(", ", Enumerable.Range(0, max).Select(r => Da2Executable.GamePrice(Source.FeatId, cost, r, SpecialPrices)));
        }
    }

    public void RefreshPrices() => OnPropertyChanged(nameof(Prices));
}

public sealed class Da2SpecialPriceRow
{
    public int FeatId { get; init; }
    public string Name => Da2Names.Feat(FeatId);
    public string R1 { get; set; } = "";
    public string R2 { get; set; } = "";
    public string R3 { get; set; } = "";
    public string R4 { get; set; } = "";
    public string R5 { get; set; } = "";
    public string[] All => new[] { R1, R2, R3, R4, R5 };
}

public sealed class Da2ItemRow
{
    public Da2Item Source { get; init; } = null!;
    public string Name => Source.Name;
    public string Kind => Source.Kind;
    public string List => Source.ListName;
    public string Weight { get; set; } = "";
    public string Value { get; set; } = "";
    public string Armour { get; set; } = "";
}

public sealed class Da2LevelRow : ObservableObject
{
    private string _monsterLevel = "";
    public Da2Level Source { get; init; } = null!;
    public string Name => Source.Name;
    public int Act => Source.Act;
    public int FormulaSet => Source.FormulaSet;
    public string BasicItems => Source.BasicItemsOnly ? "yes" : "";

    public string MonsterLevel
    {
        get => _monsterLevel;
        set { if (SetProperty(ref _monsterLevel, value)) Changed?.Invoke(this, EventArgs.Empty); }
    }

    public event EventHandler? Changed;
    public override string ToString() => $"{Name} (act {Act}, level {_monsterLevel})";
}

public sealed class Da2CreatureRow : ObservableObject
{
    public Da2Creature Source { get; init; } = null!;
    public string Name => Source.Class;
    public string Variant => Source.Variants > 1 ? $"{Source.Variant + 1} of {Source.Variants}" : "";
    public string Names => string.Join(", ", Source.Names);
    public Da2Cell[] Cells { get; init; } = Array.Empty<Da2Cell>();
    public Da2Cell Hp => Cells[0];
    public Da2Cell Armour => Cells[1];
    public Da2Cell Damage => Cells[2];
    public Da2Cell Attack => Cells[3];
    public Da2Cell Xp => Cells[4];

    private string _predictedHp = "", _predictedDamage = "", _predictedXp = "";
    public string PredictedHp { get => _predictedHp; set => SetProperty(ref _predictedHp, value); }
    public string PredictedDamage { get => _predictedDamage; set => SetProperty(ref _predictedDamage, value); }
    public string PredictedXp { get => _predictedXp; set => SetProperty(ref _predictedXp, value); }
}

public sealed class Da2BossRow : ObservableObject
{
    private string _hp = "";
    private string _easy = "", _normal = "", _hard = "", _extreme = "";
    public Da2Boss Source { get; init; } = null!;
    public string Name => Source.Class + (Source.Note.Length > 0 ? " (" + Source.Note + ")" : "");
    public bool FullPrecision => Source.Constant.FullPrecision;

    public string Hp
    {
        get => _hp;
        set { if (SetProperty(ref _hp, value)) Changed?.Invoke(this, EventArgs.Empty); }
    }

    public string Easy { get => _easy; set => SetProperty(ref _easy, value); }
    public string Normal { get => _normal; set => SetProperty(ref _normal, value); }
    public string Hard { get => _hard; set => SetProperty(ref _hard, value); }
    public string Extreme { get => _extreme; set => SetProperty(ref _extreme, value); }
    public event EventHandler? Changed;
}

/// <summary>A row of the difficulty grid; cells the game has no constant for are read-only.</summary>
public sealed class Da2DifficultyRow
{
    public string Name { get; init; } = "";
    public Da2Cell Hp { get; init; } = null!;
    public Da2Cell Damage { get; init; } = null!;
    public Da2Cell Level { get; init; } = null!;
    public Da2Cell Boss { get; init; } = null!;
}

/// <summary>A two-player factor (players 2 and 3 alike: the mod keeps the game's player count at 2).</summary>
public sealed class Da2TwoPlayerRow
{
    public string Property { get; init; } = "";
    public string Name => Da2Executable.DescribeFactor(Property);
    public Da2Cell Cell { get; init; } = null!;
    public string Stock { get; init; } = "";
    public string Precision => Da2Executable.DifficultyFullPrecision(Property) ? "" : "rounded (upper half of a float)";
}

public sealed class Da2PlacementRow : ObservableObject
{
    private bool _include = true;
    private string _name = "", _level = "", _type = "", _x = "", _y = "", _z = "", _facing = "", _properties = "";
    public bool Include { get => _include; set => SetProperty(ref _include, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Level { get => _level; set => SetProperty(ref _level, value); }

    public string Type
    {
        get => _type;
        set { if (SetProperty(ref _type, value)) OnPropertyChanged(nameof(Needs)); }
    }

    public string X { get => _x; set => SetProperty(ref _x, value); }
    public string Y { get => _y; set => SetProperty(ref _y, value); }
    public string Z { get => _z; set => SetProperty(ref _z, value); }
    public string Facing { get => _facing; set => SetProperty(ref _facing, value); }
    public string Properties { get => _properties; set => SetProperty(ref _properties, value); }

    /// <summary>The description read from the file (kept while the creature isn't changed).</summary>
    public string? Description { get; set; }

    /// <summary>The placement as it was read, to tell whether it changed.</summary>
    public Da2Placement? Original { get; set; }

    public string Needs => new Da2Placement { Type = _type.Trim() }.NeedsFiles switch
    {
        8 => "LILVARRA.LMP", 16 => "MINOTAUR.LMP", _ => ""
    };
}

/// <summary>An act of the shops group: the lowest and highest gold value of a random item of each kind.</summary>
public sealed class Da2ShopActRow
{
    public int Act { get; init; }
    public string Name => "Act " + Act.ToString(CultureInfo.InvariantCulture);
    public string WeaponsLow { get; set; } = "";
    public string WeaponsHigh { get; set; } = "";
    public string ArmourLow { get; set; } = "";
    public string ArmourHigh { get; set; } = "";
    public string OtherLow { get; set; } = "";
    public string OtherHigh { get; set; } = "";
    public string[] Low => new[] { WeaponsLow, ArmourLow, OtherLow };
    public string[] High => new[] { WeaponsHigh, ArmourHigh, OtherHigh };
}

/// <summary>A kind of the shops group: random items on its tab and the Extreme floor.</summary>
public sealed class Da2ShopKindRow
{
    public int Kind { get; init; }
    public string Name => Da2ShopSettings.KindNames[Kind];
    public string Count { get; set; } = "";
    public string ExtremeFloor { get; set; } = "";
}

public sealed class Da2FeatPriceRow
{
    public int FeatId { get; init; }
    public string Name => Da2Names.Feat(FeatId);
    public string UsedBy { get; init; } = "";
    public string GamePrices { get; init; } = "";
    public string R1 { get; set; } = "";
    public string R2 { get; set; } = "";
    public string R3 { get; set; } = "";
    public string R4 { get; set; } = "";
    public string R5 { get; set; } = "";
    public string[] All => new[] { R1, R2, R3, R4, R5 };
}

public sealed class Da2TuningModel : ObservableObject
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    public static string F(float v) => v.ToString(Inv);

    public Da2Executable Exe { get; }

    /// <summary>The cheat file the settings were read from, null for a new one.</summary>
    public string? CheatPath { get; private set; }

    public Da2ModMainState MainState { get; private set; } = Da2ModMainState.Missing;
    public IReadOnlyList<int> MainBuilds { get; private set; } = Array.Empty<int>();
    public List<PnachGroup> OtherGroups { get; private set; } = new();
    public List<string> Notes { get; } = new();

    // ---- mod settings ----
    public bool IncludeMain { get; set; } = true;
    public string HudSizes { get; set; } = "";
    public bool IncludePlayerThree { get; set; } = true;
    public bool IncludeShouts { get; set; } = true;
    public bool IncludeFeatPoints { get; set; } = true;
    public string FeatPoints { get; set; } = "";
    public bool IncludeGold { get; set; } = true;
    public string Gold { get; set; } = "";
    public bool IncludeTradeWay { get; set; } = true;
    public bool IncludeShops { get; set; } = true;
    public bool IncludeFeatPrices { get; set; } = true;

    public List<Da2ShopActRow> ShopActs { get; private set; } = new();
    public List<Da2ShopKindRow> ShopKinds { get; private set; } = new();

    public ObservableCollection<Da2PlacementRow> Placements { get; } = new();
    public ObservableCollection<Da2FeatPriceRow> FeatPrices { get; } = new();

    // ---- executable (new lists on every load, so the grids bound to them refresh) ----
    public List<Da2XpRow> Xp { get; private set; } = new();
    public List<Da2StatRow> Stats { get; private set; } = new();
    public List<Da2AbilityRow> Abilities { get; private set; } = new();
    public List<Da2ClassFeatRow> ClassFeats { get; private set; } = new();
    public List<Da2SpecialPriceRow> SpecialPrices { get; private set; } = new();
    public List<Da2ItemRow> Items { get; private set; } = new();
    public List<Da2LevelRow> Levels { get; private set; } = new();
    public List<Da2CreatureRow> Creatures { get; private set; } = new();
    public List<Da2BossRow> Bosses { get; private set; } = new();
    public List<Da2DifficultyRow> Difficulty { get; private set; } = new();

    /// <summary>The two-player factors: the difficulty property and its box.</summary>
    public List<Da2TwoPlayerRow> TwoPlayer { get; private set; } = new();

    /// <summary>The box of each editable creature constant (variants that share a constant share the box).</summary>
    private readonly Dictionary<Da2Constant, Da2Cell> _constantCells = new();

    /// <summary>The first creature and multiplier of each editable constant (for messages).</summary>
    private readonly Dictionary<Da2Constant, (string Class, int Argument)> _constantOwners = new();

    private readonly Dictionary<string, Da2Cell> _difficultyCells = new();

    /// <summary>Creature types for the placement grid: the factory's names, and the DA1 bosses the mod adds.</summary>
    public List<string> CreatureTypes { get; }

    public List<string> LevelNames { get; }

    public Da2TuningModel(Da2Executable exe)
    {
        Exe = exe;
        CreatureTypes = exe.GetCreatures().SelectMany(c => c.Names).Concat(Da2ThreePlayerMod.BossNames)
            .Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        LevelNames = exe.GetLevels().Select(l => l.Name).ToList();
        LoadMod(Da2ThreePlayerMod.DefaultSettings());
        LoadExecutable();
    }

    // ═══════════════════════════════ loading ═══════════════════════════════

    /// <summary>Reads a cheat file: the mod's settings, its tuning group (put on the executable) and the rest.</summary>
    public void LoadCheatFile(string path)
    {
        var read = Da2ThreePlayerMod.Read(PnachFile.Load(path), Da2Executable.TuningGroupName);
        CheatPath = path;
        MainState = read.MainState;
        MainBuilds = read.MainBuilds;
        OtherGroups = read.OtherGroups;
        Notes.Clear();
        Notes.AddRange(read.Notes);
        Exe.ResetToStock();
        if (read.Tuning != null)
        {
            try
            {
                Exe.ApplyTuningGroup(read.Tuning);
            }
            catch (InvalidDataException ex)
            {
                Exe.ResetToStock();
                Notes.Add($"[{Da2Executable.TuningGroupName}] wasn't read ({ex.Message}); saving replaces it.");
            }
        }

        LoadMod(read.Settings);
        LoadExecutable();
    }

    /// <summary>After a save: the file now holds what was written (older-build notes no longer apply).</summary>
    public void Saved(string path, PnachFile file)
    {
        var read = Da2ThreePlayerMod.Read(file, Da2Executable.TuningGroupName);
        CheatPath = path;
        MainState = read.MainState;
        MainBuilds = read.MainBuilds;
        OtherGroups = read.OtherGroups;
        Notes.Clear();
    }

    /// <summary>Back to build 51 and the stock executable (the other groups of the file are kept).</summary>
    public void StartFromBuild51()
    {
        MainState = CheatPath == null ? Da2ModMainState.Missing : MainState;
        Exe.ResetToStock();
        LoadMod(Da2ThreePlayerMod.DefaultSettings());
        LoadExecutable();
    }

    private void LoadMod(Da2ModSettings s)
    {
        IncludeMain = s.IncludeMain;
        HudSizes = string.Join(", ", s.HudSizes);
        IncludePlayerThree = s.IncludePlayerThreeCharacters;
        IncludeShouts = s.IncludeShoutsInSoundMemory;
        IncludeFeatPoints = s.IncludeFeatPoints;
        FeatPoints = s.FeatPoints.ToString(Inv);
        IncludeGold = s.IncludeGold;
        Gold = s.Gold.ToString(Inv);
        IncludeTradeWay = s.IncludeTradeWayGoblins;
        IncludeShops = s.IncludeShops;
        IncludeFeatPrices = s.IncludeFeatPrices;
        LoadShops(s.Shops);
        LoadPlacements(s.Placements);
        LoadFeatPrices(s.FeatPrices);
        OnPropertyChanged(string.Empty);
    }

    public void LoadShops(Da2ShopSettings shops)
    {
        string Low(int act, int kind) => F(shops.Low[act * Da2ShopSettings.Kinds + kind]);
        string High(int act, int kind) => F(shops.High[act * Da2ShopSettings.Kinds + kind]);
        ShopActs = Enumerable.Range(0, Da2ShopSettings.Acts).Select(a => new Da2ShopActRow
        {
            Act = a + 1, WeaponsLow = Low(a, 0), WeaponsHigh = High(a, 0), ArmourLow = Low(a, 1), ArmourHigh = High(a, 1),
            OtherLow = Low(a, 2), OtherHigh = High(a, 2)
        }).ToList();
        ShopKinds = Enumerable.Range(0, Da2ShopSettings.Kinds).Select(k => new Da2ShopKindRow
        {
            Kind = k, Count = shops.Counts[k].ToString(Inv), ExtremeFloor = F(shops.ExtremeFloor[k])
        }).ToList();
        OnPropertyChanged(nameof(ShopActs));
        OnPropertyChanged(nameof(ShopKinds));
    }

    public void LoadPlacements(IEnumerable<Da2Placement> placements)
    {
        Placements.Clear();
        foreach (var p in placements) Placements.Add(ToRow(p));
    }

    public static Da2PlacementRow ToRow(Da2Placement p) => new()
    {
        Include = p.Include, Name = p.Name, Level = p.Level, Type = p.Type, X = F(p.X), Y = F(p.Y), Z = F(p.Z),
        Facing = p.Facing.ToString(Inv), Properties = string.Join(" ", p.Properties), Description = p.Description, Original = p.Clone()
    };

    public void LoadFeatPrices(SortedDictionary<int, int[]> prices)
    {
        FeatPrices.Clear();
        foreach (var (id, p) in prices) FeatPrices.Add(NewFeatPriceRow(id, p));
    }

    public Da2FeatPriceRow NewFeatPriceRow(int id, int[] p)
    {
        string At(int r) => r < p.Length ? p[r].ToString(Inv) : "";
        var users = ClassesWithFeat(id);
        var game = Exe.GetClassFeats().FirstOrDefault(f => f.FeatId == id);
        return new Da2FeatPriceRow
        {
            FeatId = id, R1 = At(0), R2 = At(1), R3 = At(2), R4 = At(3), R5 = At(4),
            UsedBy = string.Join(", ", users),
            GamePrices = game == null ? "" : string.Join(", ", game.Prices(SpecialPricesNow))
        };
    }

    /// <summary>The DA2 classes whose table has the feat, then the characters that get it from the mod.</summary>
    private List<string> ClassesWithFeat(int id)
    {
        var list = Exe.GetClassFeats().Where(f => f.FeatId == id).Select(f => Da2Names.ClassName(f.ClassId)).ToList();
        foreach (var (name, ids) in ModFeats)
        {
            if (ids.Contains(id)) list.Add(name);
        }

        return list.Distinct().ToList();
    }

    /// <summary>
    /// The feats the mod gives (build 51 with the feat group, as in da2_feat_costs.md): the DA1 heroes' whole
    /// lists and Borador's two extras. Vahn's Precise Shot and Kromlech's last three come with the feat group.
    /// </summary>
    private static readonly (string Name, int[] Ids)[] ModFeats =
    {
        ("Vahn", new[] { 1, 4, 37, 36, 6, 24, 5, 39, 16, 23, 17, 18, 19, 20, 3, 21, 22, 25, 101 }),
        ("Adrianna", new[] { 8, 9, 10, 11, 13, 33, 38, 34, 14, 15, 35, 3, 19, 20, 1 }),
        ("Kromlech", new[] { 16, 17, 18, 2, 1, 19, 20, 3, 21, 22, 25, 26, 27, 28, 29, 31, 32, 43, 42 }),
        ("Borador", new[] { 5, 24 }),
    };

    private int[]? SpecialPricesNow(int featId)
    {
        var row = SpecialPrices.FirstOrDefault(r => r.FeatId == featId);
        if (row == null) return Exe.GetSpecialPrices(featId);
        var values = row.All.Select(t => int.TryParse(t, NumberStyles.Integer, Inv, out var v) ? v : 0).ToArray();
        return values.Any(v => v <= 0) ? Exe.GetSpecialPrices(featId) : values;
    }

    /// <summary>Fills every executable row from the executable (as it is now, edits included).</summary>
    public void LoadExecutable()
    {
        var exe = Exe;
        Xp = exe.GetXpTable().Select((v, i) => new Da2XpRow { Level = i + 1, Xp = v.ToString(Inv) }).ToList();

        Stats = Enumerable.Range(0, Da2Executable.ClassCount).Select(c =>
        {
            string S(int s) => exe.GetStat(c, s).ToString(Inv);
            return new Da2StatRow
            {
                ClassId = c, ClassName = Da2Names.Classes[c] + " — " + Da2Names.ClassTitles[c],
                STR = S(0), DEX = S(1), CON = S(2), INT = S(3), WIS = S(4), CHA = S(5)
            };
        }).ToList();

        Abilities = exe.GetAbilities().Select(a => new Da2AbilityRow
        {
            Source = a, Energy = F(a.Energy), PerRank = F(a.EnergyPerRank), DamageMin = a.DamageMin.ToString(Inv),
            DamageMax = a.DamageMax.ToString(Inv), GlowR = a.GlowR.ToString(Inv), GlowG = a.GlowG.ToString(Inv),
            GlowB = a.GlowB.ToString(Inv), LightR = a.LightR.ToString(Inv), LightG = a.LightG.ToString(Inv),
            LightB = a.LightB.ToString(Inv), Coefficient = F(a.Coefficient)
        }).ToList();

        SpecialPrices = Da2Executable.SpecialPriceFeats.Select(id =>
        {
            var p = exe.GetSpecialPrices(id).Select(v => v.ToString(Inv)).ToArray();
            return new Da2SpecialPriceRow { FeatId = id, R1 = p[0], R2 = p[1], R3 = p[2], R4 = p[3], R5 = p[4] };
        }).ToList();

        ClassFeats = exe.GetClassFeats().Select(f => new Da2ClassFeatRow
        {
            Source = f, SpecialPrices = SpecialPricesNow, MaxRank = f.MaxRank.ToString(Inv), BaseCost = f.BaseCost.ToString(Inv)
        }).ToList();

        Items = exe.GetItems().Select(i => new Da2ItemRow
        {
            Source = i, Weight = F(i.Weight), Value = i.Value.ToString(Inv), Armour = i.Armour.ToString(Inv)
        }).ToList();

        var previousLevel = _predictionLevel?.Name;
        Levels = exe.GetLevels().Select(l =>
        {
            var row = new Da2LevelRow { Source = l, MonsterLevel = F(l.MonsterLevel) };
            row.Changed += (_, _) => RefreshPredictions();
            return row;
        }).ToList();
        _predictionLevel = Levels.FirstOrDefault(l => l.Name == previousLevel);

        _constantCells.Clear();
        _constantOwners.Clear();
        var creatures = new List<Da2CreatureRow>();
        foreach (var c in exe.GetCreatures())
        {
            var cells = new Da2Cell[5];
            for (var i = 0; i < 5; i++)
            {
                if (c.Constants[i] is { } k)
                {
                    if (!_constantCells.TryGetValue(k, out var cell))
                    {
                        cell = new Da2Cell(F(c.Values[i]), editable: true);
                        cell.Changed += (_, _) => RefreshPredictions();
                        _constantCells[k] = cell;
                        _constantOwners[k] = (c.Class, i);
                    }

                    cells[i] = cell;
                }
                else
                {
                    cells[i] = new Da2Cell(F(c.Values[i]), editable: false);
                }
            }

            creatures.Add(new Da2CreatureRow { Source = c, Cells = cells });
        }

        Creatures = creatures;

        Bosses = exe.GetBosses().Select(b =>
        {
            var row = new Da2BossRow { Source = b, Hp = F(b.Hp) };
            row.Changed += (_, _) => RefreshPredictions();
            return row;
        }).ToList();

        LoadDifficulty(exe.GetDifficulty());
        RefreshPredictions();
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Puts difficulty factors in the Difficulty tab's boxes.</summary>
    public void LoadDifficulty(Da2Difficulty d)
    {
        var stock = Exe.Stock().GetDifficulty();
        _difficultyCells.Clear();
        Da2Cell Cell(string property)
        {
            var cell = new Da2Cell(F(Get(d, property)), editable: true);
            cell.Changed += (_, _) => RefreshPredictions();
            _difficultyCells[property] = cell;
            return cell;
        }

        Da2Cell Fixed(string text) => new(text, editable: false);
        var easy = Cell(nameof(Da2Difficulty.EasyHpDamage));
        Difficulty = new List<Da2DifficultyRow>
        {
            new() { Name = "Easy", Hp = easy, Damage = easy, Level = Fixed("0"), Boss = Cell(nameof(Da2Difficulty.BossEasy)) },
            new() { Name = "Normal", Hp = Cell(nameof(Da2Difficulty.NormalHp)), Damage = Fixed("1"), Level = Fixed("0"), Boss = Cell(nameof(Da2Difficulty.BossNormal)) },
            new() { Name = "Hard", Hp = Cell(nameof(Da2Difficulty.HardHp)), Damage = Cell(nameof(Da2Difficulty.HardDamage)), Level = Fixed("0"), Boss = Cell(nameof(Da2Difficulty.BossHard)) },
            new() { Name = "Extreme", Hp = Cell(nameof(Da2Difficulty.ExtremeHp)), Damage = Cell(nameof(Da2Difficulty.ExtremeDamage)), Level = Cell(nameof(Da2Difficulty.ExtremeLevel)), Boss = Cell(nameof(Da2Difficulty.BossExtreme)) },
        };
        TwoPlayer = new[] { nameof(Da2Difficulty.TwoPlayerHp), nameof(Da2Difficulty.TwoPlayerDamage), nameof(Da2Difficulty.TwoPlayerXp), nameof(Da2Difficulty.BossTwoPlayer) }
            .Select(p => new Da2TwoPlayerRow { Property = p, Cell = Cell(p), Stock = F(Get(stock, p)) }).ToList();
        OnPropertyChanged(nameof(Difficulty));
        OnPropertyChanged(nameof(TwoPlayer));
        RefreshPredictions();
    }

    private static float Get(Da2Difficulty d, string property) => (float)typeof(Da2Difficulty).GetProperty(property)!.GetValue(d)!;
    private static void Set(Da2Difficulty d, string property, float v) => typeof(Da2Difficulty).GetProperty(property)!.SetValue(d, v);

    // ═══════════════════════════════ predictions (Monsters tab) ═══════════════════════════════

    private Da2LevelRow? _predictionLevel;
    private int _predictionDifficulty = 1;
    private bool _predictionTwoPlayers = true;

    public Da2LevelRow? PredictionLevel
    {
        get => _predictionLevel ?? Levels.FirstOrDefault(l => l.Name == "trollb");
        set { _predictionLevel = value; RefreshPredictions(); }
    }

    public int PredictionDifficulty
    {
        get => _predictionDifficulty;
        set { _predictionDifficulty = Math.Clamp(value, 0, 3); RefreshPredictions(); }
    }

    public bool PredictionTwoPlayers
    {
        get => _predictionTwoPlayers;
        set { _predictionTwoPlayers = value; RefreshPredictions(); }
    }

    private static bool TryF(string? text, out float v) => float.TryParse(text?.Trim(), NumberStyles.Float, Inv, out v);

    /// <summary>The difficulty factors as typed (the stock ones where a box doesn't hold a number).</summary>
    private Da2Difficulty TypedDifficulty()
    {
        var d = Exe.GetDifficulty();
        foreach (var (p, cell) in _difficultyCells)
        {
            if (TryF(cell.Text, out var v)) Set(d, p, v);
        }

        return d;
    }

    public void RefreshPredictions()
    {
        if (Levels.Count == 0 || _difficultyCells.Count == 0) return;
        var level = PredictionLevel;
        var l = level != null && TryF(level.MonsterLevel, out var lv) ? lv : 1f;
        var set = level?.FormulaSet ?? 1;
        var d = TypedDifficulty();
        foreach (var row in Creatures)
        {
            var m = new float[5];
            var ok = true;
            for (var i = 0; i < 5; i++) ok &= TryF(row.Cells[i].Text, out m[i]);
            if (!ok)
            {
                row.PredictedHp = row.PredictedDamage = row.PredictedXp = "";
                continue;
            }

            var s = Da2Executable.Predict(m, l, set, _predictionDifficulty, _predictionTwoPlayers, d);
            row.PredictedHp = s.HpMin == s.HpMax ? s.HpMin.ToString(Inv) : $"{s.HpMin}-{s.HpMax}";
            row.PredictedDamage = "1-" + s.DamageMax.ToString(Inv);
            row.PredictedXp = s.XpRating.ToString("0.#", Inv);
        }

        foreach (var b in Bosses)
        {
            if (!TryF(b.Hp, out var hp))
            {
                b.Easy = b.Normal = b.Hard = b.Extreme = "";
                continue;
            }

            var players = _predictionTwoPlayers ? d.BossTwoPlayer : 1f;
            string At(int i) => ((int)(hp * d.Boss(i) * players)).ToString(Inv);
            b.Easy = At(0);
            b.Normal = At(1);
            b.Hard = At(2);
            b.Extreme = At(3);
        }
    }

    // ═══════════════════════════════ applying ═══════════════════════════════

    private static float ParseF(string? text, string what) =>
        TryF(text, out var v) ? v : throw new ArgumentException($"{what}: '{text}' is not a number.");

    private static int ParseI(string? text, string what) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, Inv, out var v) ? v : throw new ArgumentException($"{what}: '{text}' is not a whole number.");

    /// <summary>
    /// Checks every box, puts the executable edits on a fresh copy of the stock executable and makes the
    /// cheat file (nothing is written to disk here). Throws ArgumentException naming the first bad value.
    /// <see cref="Conflicts"/> then lists the words that a group of the file writes too.
    /// </summary>
    public PnachFile Apply()
    {
        var settings = ReadModSettings();
        Exe.ResetToStock();
        ApplyExecutable();
        var tuning = Exe.ToTuningGroup();
        var file = Da2ThreePlayerMod.Write(settings, tuning, OtherGroups);
        Conflicts = tuning == null
            ? new List<(uint, string)>()
            : Exe.Conflicts(file.Groups.Where(g => g.Name != Da2Executable.TuningGroupName));
        return file;
    }

    /// <summary>
    /// After <see cref="Apply"/>: words the executable edits write that another group writes too (the mod's
    /// groups never do; a group of the user's own can). With both ticked, the one PCSX2 applies last wins.
    /// </summary>
    public List<(uint Address, string Group)> Conflicts { get; private set; } = new();

    public Da2ModSettings ReadModSettings()
    {
        var sizes = new List<int>();
        foreach (var part in (HudSizes ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            sizes.Add(ParseI(part.TrimEnd('%'), "HUD sizes"));

        var shops = new Da2ShopSettings();
        foreach (var row in ShopActs)
        {
            for (var k = 0; k < Da2ShopSettings.Kinds; k++)
            {
                var i = (row.Act - 1) * Da2ShopSettings.Kinds + k;
                var what = $"Shops, act {row.Act}, {Da2ShopSettings.KindNames[k].ToLowerInvariant()}";
                shops.Low[i] = ParseF(row.Low[k], what + ", lowest value");
                shops.High[i] = ParseF(row.High[k], what + ", highest value");
            }
        }

        foreach (var row in ShopKinds)
        {
            var kind = row.Name.ToLowerInvariant();
            shops.Counts[row.Kind] = ParseI(row.Count, $"Shops, random {kind}");
            shops.ExtremeFloor[row.Kind] = ParseF(row.ExtremeFloor, $"Shops, Extreme floor for {kind}");
        }

        var prices = new SortedDictionary<int, int[]>();
        foreach (var row in FeatPrices)
        {
            var list = new List<int>();
            foreach (var t in row.All)
            {
                if (string.IsNullOrWhiteSpace(t)) break;
                list.Add(ParseI(t, $"Feat prices, {row.Name}"));
            }

            if (list.Count == 0) throw new ArgumentException($"Feat prices, {row.Name}: give the price of rank 1 at least.");
            if (!prices.TryAdd(row.FeatId, list.ToArray())) throw new ArgumentException($"Feat prices: {row.Name} is listed twice.");
        }

        var placements = Placements.Select((p, i) =>
        {
            var what = string.IsNullOrWhiteSpace(p.Name) ? $"Creature {i + 1}" : p.Name;
            var placement = new Da2Placement
            {
                Include = p.Include, Name = p.Name.Trim(), Level = p.Level.Trim(), Type = p.Type.Trim(),
                X = ParseF(p.X, what + ", x"), Y = ParseF(p.Y, what + ", y"), Z = ParseF(p.Z, what + ", z"),
                Facing = ParseI(p.Facing, what + ", facing"),
                Properties = p.Properties.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
            // a creature that wasn't changed keeps the description it had
            placement.Description = p.Original != null && placement.SamePlace(p.Original) ? p.Description : null;
            return placement;
        }).ToList();

        return new Da2ModSettings
        {
            IncludeMain = IncludeMain, HudSizes = sizes, IncludePlayerThreeCharacters = IncludePlayerThree,
            IncludeShoutsInSoundMemory = IncludeShouts, IncludeFeatPoints = IncludeFeatPoints,
            FeatPoints = ParseI(FeatPoints, "Test feat points"), IncludeGold = IncludeGold, Gold = ParseI(Gold, "Test gold"),
            IncludeTradeWayGoblins = IncludeTradeWay, Placements = placements, IncludeShops = IncludeShops, Shops = shops,
            IncludeFeatPrices = IncludeFeatPrices, FeatPrices = prices
        };
    }

    private void ApplyExecutable()
    {
        var exe = Exe;
        var xp = Xp.Select(r => ParseI(r.Xp, $"Level {r.Level}")).ToArray();
        exe.SetXpTable(xp);

        foreach (var row in Stats)
        {
            var v = row.All;
            for (var s = 0; s < Da2Executable.StatsPerClass; s++)
                exe.SetStat(row.ClassId, s, ParseI(v[s], $"{Da2Names.Classes[row.ClassId]} {Da2Executable.StatNames[s]}"));
        }

        foreach (var r in Abilities)
        {
            var i = r.Source.Index;
            exe.SetAbilityEnergy(i, ParseF(r.Energy, r.Name + ", energy"), ParseF(r.PerRank, r.Name + ", energy per rank"));
            exe.SetAbilityDamage(i, ParseI(r.DamageMin, r.Name + ", damage min"), ParseI(r.DamageMax, r.Name + ", damage max"));
            exe.SetAbilityGlow(i, ParseI(r.GlowR, r.Name + ", glow"), ParseI(r.GlowG, r.Name + ", glow"), ParseI(r.GlowB, r.Name + ", glow"));
            exe.SetAbilityLight(i, ParseI(r.LightR, r.Name + ", light"), ParseI(r.LightG, r.Name + ", light"), ParseI(r.LightB, r.Name + ", light"));
            exe.SetAbilityCoefficient(i, ParseF(r.Coefficient, r.Name + ", coefficient"));
        }

        foreach (var r in SpecialPrices)
            exe.SetSpecialPrices(r.FeatId, r.All.Select(t => ParseI(t, r.Name + " prices")).ToArray());

        foreach (var r in ClassFeats)
        {
            var what = $"{r.ClassName}, {r.Feat}";
            exe.SetClassFeat(r.Source.Address, ParseI(r.MaxRank, what + ", ranks"), ParseI(r.BaseCost, what + ", base cost"));
        }

        foreach (var r in Items)
            exe.SetItem(r.Source.Index, ParseF(r.Weight, r.Name + ", weight"), ParseI(r.Value, r.Name + ", value"), ParseI(r.Armour, r.Name + ", armour"));

        foreach (var r in Levels)
            exe.SetMonsterLevel(r.Source.Index, ParseF(r.MonsterLevel, r.Name + ", monster level"));

        foreach (var (constant, cell) in _constantCells)
        {
            var (name, argument) = _constantOwners[constant];
            exe.SetCreatureMultiplier(constant, ParseF(cell.Text, $"{name}, {Da2Creature.Multipliers[argument]} multiplier"));
        }

        foreach (var b in Bosses)
            exe.SetBossHp(b.Source.Constant, ParseF(b.Hp, b.Name + " HP"));

        var d = exe.GetDifficulty();
        foreach (var (p, cell) in _difficultyCells)
            Set(d, p, ParseF(cell.Text, Da2Executable.DescribeFactor(p)));
        exe.SetDifficulty(d);

        // show what is written: a constant without an ori keeps only the upper half of the float (2500 -> 2496)
        foreach (var (constant, cell) in _constantCells) cell.Show(F(exe.ConstantValue(constant)));
        foreach (var b in Bosses) b.Hp = F(exe.ConstantValue(b.Source.Constant));
        var written = exe.GetDifficulty();
        foreach (var (p, cell) in _difficultyCells) cell.Show(F(Get(written, p)));
        RefreshPredictions();
    }

    /// <summary>A sentence on the cheat file's [3 Player mod] group for the Three Players tab.</summary>
    public string MainStatus()
    {
        var source = CheatPath == null ? "A new cheat file" : "The cheat file";
        return MainState switch
        {
            Da2ModMainState.Current => $"{source} has the tested build {Da2ThreePlayerMod.CurrentBuild} of the mod.",
            Da2ModMainState.Older => $"{source} has the older {Da2ThreePlayerMod.DescribeBuilds(MainBuilds)}. The tabs show the mod's groups " +
                                     $"it has and their settings; saving writes them as build {Da2ThreePlayerMod.CurrentBuild} has them.",
            Da2ModMainState.Unknown => $"{source}'s [{Da2ThreePlayerMod.MainGroup}] group is not a build WorldExplorer knows (changed by " +
                                       $"hand?). Saving replaces it with build {Da2ThreePlayerMod.CurrentBuild}.",
            _ => CheatPath == null
                ? $"Saving writes build {Da2ThreePlayerMod.CurrentBuild} of the mod with the settings below."
                : $"The cheat file has no [{Da2ThreePlayerMod.MainGroup}] group; saving adds build {Da2ThreePlayerMod.CurrentBuild}'s."
        };
    }
}
