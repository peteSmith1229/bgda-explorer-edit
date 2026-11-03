// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/Da2TuningModel.cs
//
// What the Dark Alliance II Executable Tuning window shows and edits (Da2TuningWindow), tab by tab in
// Dark Alliance 1's order:
//   XP per Level     the level table, the kill XP shares and the XP bonuses (Da2Executable.Rules)
//   Starting Stats   the classes' ability scores
//   Feats & Spells   the ability templates, the class feat tables and the DA1 spells' price tables,
//                    and the mod's feat price group
//   Items            the base items, the mod's shop group, and the loot: each creature's drop values,
//                    the drop rules, the quality table, the gold / item value of a level, potions, Ransack
//   Monster HP       the creature multipliers with the HP they give on every difficulty, fixed-HP bosses
//   Three Players    the mod's groups and settings, the two-player factors (three players use them),
//                    the difficulty scaling
//   Creatures        creatures the mod places in levels, with the room each level has
//   Levels           monster level of every level, and its pools and memory at load (Da2LevelBudget)
// Rows hold text exactly as typed; Apply() checks every value (ArgumentException names the first
// bad one) and makes the cheat file. Nothing is written to the executable: the edits become the
// "[WorldExplorer - executable tuning]" group of the cheat file.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    /// <summary>In the game's order (STR, DEX, CON, INT, WIS, CHA); the grid shows Dark Alliance 1's.</summary>
    public string[] All => new[] { STR, DEX, CON, INT, WIS, CHA };
}

public sealed class Da2AbilityRow : ObservableObject
{
    private string _energy = "";
    public Da2Ability Source { get; init; } = null!;
    public string Name => Source.Name;
    public string Feat => Da2Names.IsFeat(Source.FeatId) ? Da2Names.Feat(Source.FeatId) : "";
    public string Type => Source.Type.ToString(CultureInfo.InvariantCulture) + (Source.IsStream ? " (per frame)" : "");

    public string Energy
    {
        get => _energy;
        set { if (SetProperty(ref _energy, value)) OnPropertyChanged(nameof(PerSecond)); }
    }

    /// <summary>The energy a held stream spell costs a second (60 frames).</summary>
    public string PerSecond => Source.IsStream && float.TryParse(_energy, NumberStyles.Float, CultureInfo.InvariantCulture, out var e)
        ? (e * 60).ToString("0.#", CultureInfo.InvariantCulture)
        : "";

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

    /// <summary>The level at load in the stock game (null for the few the scan has no file for).</summary>
    public Da2LevelBudget? Budget { get; init; }

    public string Creatures => Budget?.Creatures.ToString(CultureInfo.InvariantCulture) ?? "";
    public string CreatureSlots => Budget == null ? "" : $"{Budget.CreatureSized} of {Da2LevelBudget.CreatureSlots}";
    public string Room => Budget == null ? "" : Budget.Room.ToString(CultureInfo.InvariantCulture);
    public string SmallObjects => Budget == null ? "" : $"{Budget.Small} of {Da2LevelBudget.SmallSlots}";
    public string HeapFree => Budget?.HeapFreeKb.ToString("N0", CultureInfo.InvariantCulture) ?? "";
    public string Lumps => Budget == null ? "" : $"{Budget.Lumps} of {Da2LevelBudget.LumpSlots}";

    /// <summary>"Brush.Danger" when the level has no room to spare at load, "Brush.Warning" when it is near.</summary>
    public string RoomBrush => Budget == null ? "Brush.Text" : Budget.Room < 0 ? "Brush.Danger" : Budget.Room < 20 ? "Brush.Warning" : "Brush.Text";

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

    private string _easy = "", _normal = "", _hard = "", _extreme = "", _damage = "", _xp = "";
    public string EasyHp { get => _easy; set => SetProperty(ref _easy, value); }
    public string NormalHp { get => _normal; set => SetProperty(ref _normal, value); }
    public string HardHp { get => _hard; set => SetProperty(ref _hard, value); }
    public string ExtremeHp { get => _extreme; set => SetProperty(ref _extreme, value); }

    /// <summary>Highest damage on Easy / Normal / Hard / Extreme.</summary>
    public string Damage { get => _damage; set => SetProperty(ref _damage, value); }

    /// <summary>XP rating on Easy to Hard / on Extreme (what a kill is worth before it is shared out).</summary>
    public string XpRating { get => _xp; set => SetProperty(ref _xp, value); }

    public string Editable => Cells.Count(c => c.IsEditable) switch
    {
        0 => "none (computed / shared)",
        5 => "all five",
        var n => $"{n} of 5"
    };
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

/// <summary>A named value with the game's own next to it: a rule, a two-player factor.</summary>
public sealed class Da2FactorRow
{
    public string Name { get; init; } = "";
    public Da2Cell Cell { get; init; } = null!;
    public string Stock { get; init; } = "";
    public string Note { get; init; } = "";

    /// <summary>The rule behind it (null for a difficulty factor).</summary>
    public Da2Setting? Setting { get; init; }

    /// <summary>The difficulty property behind it (null for a rule).</summary>
    public string? Property { get; init; }
}

/// <summary>An act of the gold / item value curves (cells shared where two acts share one constant).</summary>
public sealed class Da2CurveRow
{
    public string Name { get; init; } = "";
    public Da2Cell GoldOffset { get; init; } = null!;
    public Da2Cell GoldPerLevel { get; init; } = null!;
    public Da2Cell GoldBase { get; init; } = null!;
    public Da2Cell ValueOffset { get; init; } = null!;
    public Da2Cell ValuePerLevel { get; init; } = null!;
    public Da2Cell ValueBase { get; init; } = null!;
}

/// <summary>A drop call of a creature's death code.</summary>
public sealed class Da2DropRow : ObservableObject
{
    public Da2Drop Source { get; init; } = null!;
    public string Creature => Source.Creature;
    public string Note => Source.Note;
    public string Names => string.Join(", ", Source.Names);
    public Da2Cell[] Cells { get; init; } = Array.Empty<Da2Cell>();

    private string _one = "", _more = "";

    /// <summary>"item 7.5 % · potion 15 %" with one player.</summary>
    public string OnePlayer { get => _one; set => SetProperty(ref _one, value); }

    /// <summary>The same with two players counted (and so with three).</summary>
    public string MorePlayers { get => _more; set => SetProperty(ref _more, value); }
}

/// <summary>A row of the quality table: the chance of a poor and a plain roll; good is the rest.</summary>
public sealed class Da2QualityRow : ObservableObject
{
    private string _poor = "", _plain = "";
    public int Level { get; init; }

    public string Poor
    {
        get => _poor;
        set { if (SetProperty(ref _poor, value)) OnPropertyChanged(nameof(Good)); }
    }

    public string Plain
    {
        get => _plain;
        set { if (SetProperty(ref _plain, value)) OnPropertyChanged(nameof(Good)); }
    }

    public string Good =>
        float.TryParse(_poor, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) &&
        float.TryParse(_plain, NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
            ? MathF.Max(0, 1 - a - b).ToString("0.####", CultureInfo.InvariantCulture)
            : "";
}

public sealed class Da2PlacementRow : ObservableObject
{
    private bool _include = true;
    private string _name = "", _level = "", _type = "", _x = "", _y = "", _z = "", _facing = "", _properties = "", _room = "";
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

    /// <summary>The creature-sized objects its level has at load with the creatures placed there.</summary>
    public string Room { get => _room; set => SetProperty(ref _room, value); }

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

    /// <summary>The factors three players get (the game's two-player ones, the mod keeps its count at 2).</summary>
    public List<Da2FactorRow> TwoPlayer { get; private set; } = new();

    // ---- rules (Da2Executable.Settings) ----
    public List<Da2FactorRow> KillXp { get; private set; } = new();
    public List<Da2FactorRow> XpBonuses { get; private set; } = new();
    public List<Da2FactorRow> DropRules { get; private set; } = new();
    public List<Da2FactorRow> CurveRules { get; private set; } = new();
    public List<Da2CurveRow> Curves { get; private set; } = new();
    public List<Da2FactorRow> PotionRules { get; private set; } = new();
    public List<Da2FactorRow> RansackRules { get; private set; } = new();
    public List<Da2DropRow> Drops { get; private set; } = new();
    public List<Da2QualityRow> Quality { get; private set; } = new();

    /// <summary>The box of each editable creature constant (variants that share a constant share the box).</summary>
    private readonly Dictionary<Da2Constant, Da2Cell> _constantCells = new();

    /// <summary>The first creature and multiplier of each editable constant (for messages).</summary>
    private readonly Dictionary<Da2Constant, (string Class, int Argument)> _constantOwners = new();

    private readonly Dictionary<string, Da2Cell> _difficultyCells = new();
    private readonly Dictionary<string, Da2Cell> _settingCells = new();

    /// <summary>The exact value each rule's box was filled with (a percentage shows only two decimals).</summary>
    private readonly Dictionary<string, float> _settingExact = new();
    private readonly Dictionary<Da2Constant, Da2Cell> _dropCells = new();
    private readonly Dictionary<Da2Constant, string> _dropOwners = new();

    /// <summary>Creature types for the placement grid: the factory's names, and the DA1 bosses the mod adds.</summary>
    public List<string> CreatureTypes { get; }

    public List<string> LevelNames { get; }

    public Da2TuningModel(Da2Executable exe)
    {
        Exe = exe;
        CreatureTypes = exe.GetCreatures().SelectMany(c => c.Names).Concat(Da2ThreePlayerMod.BossNames)
            .Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        LevelNames = exe.GetLevels().Select(l => l.Name).ToList();
        Placements.CollectionChanged += Placements_CollectionChanged;
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

            if (Exe.UnexpectedCode() is { } where)
            {
                Exe.ResetToStock();
                Notes.Add($"[{Da2Executable.TuningGroupName}] wasn't read: it changes the code of {where} in a way " +
                          "WorldExplorer doesn't write; saving replaces it.");
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
        RefreshRoom();
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
        var previousLootLevel = _lootLevel?.Name;
        Levels = exe.GetLevels().Select(l =>
        {
            var row = new Da2LevelRow { Source = l, MonsterLevel = F(l.MonsterLevel), Budget = Da2LevelBudget.Find(l.Name) };
            row.Changed += (_, _) => RefreshPredictions();
            return row;
        }).ToList();
        _predictionLevel = Levels.FirstOrDefault(l => l.Name == previousLevel);
        _lootLevel = Levels.FirstOrDefault(l => l.Name == previousLootLevel);

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

        LoadRules(exe.GetSettings());
        LoadDrops();
        LoadQuality(exe.GetQualityTable());
        LoadDifficulty(exe.GetDifficulty());
        RefreshPredictions();
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Puts difficulty factors in the boxes of the Three Players tab.</summary>
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

        string Note(string p) => Da2Executable.DifficultyFullPrecision(p) ? "" : "Rounded (upper half of a float).";
        var factors = new[]
        {
            (nameof(Da2Difficulty.TwoPlayerHp), "Monster HP ×"), (nameof(Da2Difficulty.TwoPlayerDamage), "Monster damage ×"),
            (nameof(Da2Difficulty.TwoPlayerXp), "Monster XP rating ×"), (nameof(Da2Difficulty.BossTwoPlayer), "Boss HP ×")
        }.Select(f => new Da2FactorRow { Property = f.Item1, Name = f.Item2, Cell = Cell(f.Item1), Stock = F(Get(stock, f.Item1)), Note = Note(f.Item1) }).ToList();

        // the loot factors of two players counted: the pile chance is a rule, gold and the drop chance are doubled by an add
        var piles = Da2Executable.Setting("loot.twoPlayerPiles");
        if (_settingCells.TryGetValue(piles.Key, out var pileCell))
            factors.Add(new Da2FactorRow { Setting = piles, Name = piles.Name, Cell = pileCell, Stock = F(Exe.Stock().GetSetting(piles)), Note = piles.Note });
        factors.Add(new Da2FactorRow { Name = "Gold per pile ×", Cell = new Da2Cell("2", editable: false), Stock = "2", Note = "Fixed in the code (the gold scale is added to itself)." });
        factors.Add(new Da2FactorRow { Name = "Drop chance ×", Cell = new Da2Cell("2", editable: false), Stock = "2", Note = "Fixed in the code; a potion's chance is twice the drop chance." });
        TwoPlayer = factors;
        OnPropertyChanged(nameof(Difficulty));
        OnPropertyChanged(nameof(TwoPlayer));
        RefreshPredictions();
    }

    /// <summary>Puts the rules (kill XP, drops, curves, potions, Ransack) in their boxes.</summary>
    public void LoadRules(IReadOnlyDictionary<string, float> values)
    {
        var stock = Exe.Stock();
        _settingCells.Clear();
        _settingExact.Clear();
        foreach (var s in Da2Executable.Settings)
        {
            _settingExact[s.Key] = values[s.Key];
            var cell = new Da2Cell(SettingText(s, values[s.Key]), editable: true);
            cell.Changed += (_, _) => RefreshPredictions();
            _settingCells[s.Key] = cell;
        }

        Da2FactorRow Row(Da2Setting s) => new()
        {
            Setting = s, Name = s.Name, Cell = _settingCells[s.Key], Stock = SettingText(s, stock.GetSetting(s)),
            Note = s.Note + (s.Kind == Da2SettingKind.Float && !s.FullPrecision ? (s.Note.Length > 0 ? " " : "") + "Rounded (upper half of a float)." : "")
        };

        List<Da2FactorRow> Group(string g) => Da2Executable.Settings.Where(s => s.Group == g).Select(Row).ToList();
        KillXp = Group(Da2Executable.GroupKillXp);
        XpBonuses = Group(Da2Executable.GroupXpBonus);
        DropRules = Group(Da2Executable.GroupDrops);
        PotionRules = Group(Da2Executable.GroupPotions);
        RansackRules = Group(Da2Executable.GroupRansack);
        CurveRules = new[] { "gold.scale", "gold.extreme", "gold.extremeAct1", "value.scale", "value.extreme", "value.extremeAct1" }
            .Select(k => Row(Da2Executable.Setting(k))).ToList();
        CurveRules = CurveRules.Select(r => new Da2FactorRow
        {
            Setting = r.Setting, Cell = r.Cell, Stock = r.Stock, Note = r.Note,
            Name = (r.Setting!.Group == Da2Executable.GroupGold ? "Gold: " : "Item value: ") + r.Name
        }).ToList();

        Da2Cell C(string key) => _settingCells[key];
        var none = new Da2Cell("0", editable: false);
        Curves = new List<Da2CurveRow>
        {
            new() { Name = "Act 1", GoldOffset = none, GoldPerLevel = C("gold.act1PerLevel"), GoldBase = C("gold.act1Base"),
                ValueOffset = none, ValuePerLevel = C("value.act1PerLevel"), ValueBase = C("value.act1Base") },
            new() { Name = "Act 2", GoldOffset = C("gold.act2Offset"), GoldPerLevel = C("gold.act23PerLevel"), GoldBase = C("gold.act23Base"),
                ValueOffset = C("value.act2Offset"), ValuePerLevel = C("value.act2PerLevel"), ValueBase = C("value.act2Base") },
            new() { Name = "Act 3", GoldOffset = C("gold.act3Offset"), GoldPerLevel = C("gold.act23PerLevel"), GoldBase = C("gold.act23Base"),
                ValueOffset = C("value.act3Offset"), ValuePerLevel = C("value.act34PerLevel"), ValueBase = C("value.act34Base") },
            new() { Name = "Act 4", GoldOffset = C("gold.act4Offset"), GoldPerLevel = C("gold.act4PerLevel"), GoldBase = C("gold.act4Base"),
                ValueOffset = C("value.act4Offset"), ValuePerLevel = C("value.act34PerLevel"), ValueBase = C("value.act34Base") },
        };
        OnPropertyChanged(nameof(KillXp));
        OnPropertyChanged(nameof(XpBonuses));
        OnPropertyChanged(nameof(DropRules));
        OnPropertyChanged(nameof(PotionRules));
        OnPropertyChanged(nameof(RansackRules));
        OnPropertyChanged(nameof(CurveRules));
        OnPropertyChanged(nameof(Curves));
    }

    /// <summary>A rule's value as its box shows it (percentages with two decimals).</summary>
    public static string SettingText(Da2Setting s, float v) => s.IsPercent ? v.ToString("0.##", Inv) : F(v);

    private void LoadDrops()
    {
        _dropCells.Clear();
        _dropOwners.Clear();
        Drops = Exe.GetDrops().Select(d =>
        {
            var cells = new Da2Cell[4];
            for (var i = 0; i < 4; i++)
            {
                if (d.Constants[i] is { } k)
                {
                    if (!_dropCells.TryGetValue(k, out var cell))
                    {
                        cell = new Da2Cell(F(d.Values[i]), editable: true);
                        cell.Changed += (_, _) => RefreshDrops();
                        _dropCells[k] = cell;
                        _dropOwners[k] = d.Creature;
                    }

                    cells[i] = cell;
                }
                else
                {
                    cells[i] = new Da2Cell(float.IsNaN(d.Values[i]) ? "varies" : F(d.Values[i]), editable: false);
                }
            }

            return new Da2DropRow { Source = d, Cells = cells };
        }).ToList();
        OnPropertyChanged(nameof(Drops));
    }

    public void LoadQuality(float[][] table)
    {
        Quality = table.Select((r, i) => new Da2QualityRow { Level = i, Poor = F(r[0]), Plain = F(r[1]) }).ToList();
        OnPropertyChanged(nameof(Quality));
    }

    /// <summary>Back to the game's own loot rules, drop values and quality table (the other tabs stay).</summary>
    public void StockLoot()
    {
        var stock = Exe.Stock();
        var values = TypedSettings();
        foreach (var s in Da2Executable.Settings.Where(s => s.Group is Da2Executable.GroupDrops or Da2Executable.GroupGold
                     or Da2Executable.GroupValue or Da2Executable.GroupPotions or Da2Executable.GroupRansack))
            values[s.Key] = stock.GetSetting(s);
        foreach (var (k, cell) in _dropCells) cell.Text = F(stock.ConstantValue(k));
        LoadRules(values);
        LoadDifficulty(TypedDifficulty());
        LoadQuality(stock.GetQualityTable());
        RefreshPredictions();
    }

    /// <summary>Back to the game's difficulty scaling and two-player factors (the pile chance included).</summary>
    public void StockDifficulty()
    {
        var stock = Exe.Stock();
        var piles = Da2Executable.Setting("loot.twoPlayerPiles");
        _settingCells[piles.Key].Text = SettingText(piles, stock.GetSetting(piles));
        LoadDifficulty(stock.GetDifficulty());
    }

    /// <summary>Back to the game's kill XP rules and bonuses.</summary>
    public void StockXpRules()
    {
        var stock = Exe.Stock();
        foreach (var s in Da2Executable.Settings.Where(s => s.Group is Da2Executable.GroupKillXp or Da2Executable.GroupXpBonus))
            _settingCells[s.Key].Text = SettingText(s, stock.GetSetting(s));
    }

    private static float Get(Da2Difficulty d, string property) => (float)typeof(Da2Difficulty).GetProperty(property)!.GetValue(d)!;
    private static void Set(Da2Difficulty d, string property, float v) => typeof(Da2Difficulty).GetProperty(property)!.SetValue(d, v);

    // ═══════════════════════════════ room in the levels (Creatures tab) ═══════════════════════════════

    private void Placements_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Da2PlacementRow r in e.NewItems) r.PropertyChanged += Placement_PropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (Da2PlacementRow r in e.OldItems) r.PropertyChanged -= Placement_PropertyChanged;
        }

        RefreshRoom();
    }

    private void Placement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Da2PlacementRow.Level) or nameof(Da2PlacementRow.Include)) RefreshRoom();
    }

    /// <summary>For each placed creature: the creature-sized objects of its level at load with the ones placed there.</summary>
    public void RefreshRoom()
    {
        var placed = Placements.Where(p => p.Include).GroupBy(p => p.Level.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        foreach (var p in Placements)
        {
            var b = Da2LevelBudget.Find(p.Level.Trim());
            if (b == null)
            {
                p.Room = p.Level.Trim().Length == 0 ? "" : "level not in the scan";
                continue;
            }

            var n = b.CreatureSized + (placed.TryGetValue(p.Level.Trim(), out var k) ? k : 0);
            p.Room = $"{n} of {Da2LevelBudget.CreatureSlots}" +
                     (n > Da2LevelBudget.CreatureSlots ? " — FULL: crashes" : n > Da2LevelBudget.SafeCreatureSlots ? " — too tight" : "");
        }
    }

    // ═══════════════════════════════ predictions ═══════════════════════════════

    private Da2LevelRow? _predictionLevel;
    private bool _predictionTwoPlayers = true;
    private Da2LevelRow? _lootLevel;
    private int _lootDifficulty = 1;

    public Da2LevelRow? PredictionLevel
    {
        get => _predictionLevel ?? Levels.FirstOrDefault(l => l.Name == "trollb");
        set
        {
            if (value == null) return; // the combo box clears it while its list is replaced
            _predictionLevel = value;
            RefreshPredictions();
        }
    }

    public bool PredictionTwoPlayers
    {
        get => _predictionTwoPlayers;
        set { _predictionTwoPlayers = value; RefreshPredictions(); }
    }

    /// <summary>The level of the loot example (Items ▸ Loot).</summary>
    public Da2LevelRow? LootLevel
    {
        get => _lootLevel ?? Levels.FirstOrDefault(l => l.Name == "trollb");
        set
        {
            if (value == null) return;
            _lootLevel = value;
            RefreshPredictions();
        }
    }

    public int LootDifficulty
    {
        get => _lootDifficulty;
        set { _lootDifficulty = Math.Clamp(value, 0, 3); RefreshPredictions(); }
    }

    // the kill XP example (XP per Level tab)
    /// <summary>The XP rating of the example: the creature's with two or three players (a Trollbark goblin: 98).</summary>
    private string _exampleRating = "98", _exampleLevel = "1";
    private int _examplePlayers = 3;

    public string ExampleRating { get => _exampleRating; set { _exampleRating = value; RefreshPredictions(); } }
    public string ExampleLevel { get => _exampleLevel; set { _exampleLevel = value; RefreshPredictions(); } }

    /// <summary>0-2 for one to three players.</summary>
    public int ExamplePlayers { get => _examplePlayers - 1; set { _examplePlayers = Math.Clamp(value + 1, 1, 3); RefreshPredictions(); } }

    private string _xpExample = "", _lootExample = "";
    public string XpExample { get => _xpExample; private set => SetProperty(ref _xpExample, value); }
    public string LootExample { get => _lootExample; private set => SetProperty(ref _lootExample, value); }

    private static bool TryF(string? text, out float v) => float.TryParse(text?.Trim(), NumberStyles.Float, Inv, out v);

    /// <summary>The difficulty factors as typed (the executable's where a box doesn't hold a number).</summary>
    private Da2Difficulty TypedDifficulty()
    {
        var d = Exe.GetDifficulty();
        foreach (var (p, cell) in _difficultyCells)
        {
            if (TryF(cell.Text, out var v)) Set(d, p, v);
        }

        return d;
    }

    /// <summary>The rules as typed (the executable's where a box doesn't hold a number).</summary>
    private Dictionary<string, float> TypedSettings()
    {
        var values = Exe.GetSettings();
        foreach (var (k, cell) in _settingCells)
        {
            if (TryF(cell.Text, out var v)) values[k] = v;
        }

        return values;
    }

    public void RefreshPredictions()
    {
        if (Levels.Count == 0 || _difficultyCells.Count == 0 || _settingCells.Count == 0) return;
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
                row.EasyHp = row.NormalHp = row.HardHp = row.ExtremeHp = row.Damage = row.XpRating = "";
                continue;
            }

            var s = Enumerable.Range(0, 4).Select(k => Da2Executable.Predict(m, l, set, k, _predictionTwoPlayers, d)).ToArray();
            static string Hp(Da2CreatureStats x) => x.HpMin == x.HpMax ? x.HpMin.ToString(Inv) : $"{x.HpMin}-{x.HpMax}";
            row.EasyHp = Hp(s[0]);
            row.NormalHp = Hp(s[1]);
            row.HardHp = Hp(s[2]);
            row.ExtremeHp = Hp(s[3]);
            row.Damage = string.Join(" / ", s.Select(x => x.DamageMax.ToString(Inv)));
            row.XpRating = s[1].XpRating.ToString("0.#", Inv) + " / " + s[3].XpRating.ToString("0.#", Inv);
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

        var rules = TypedSettings();
        RefreshXpExample(rules);
        RefreshDrops(rules);
        RefreshLootExample(rules);
    }

    private void RefreshXpExample(Dictionary<string, float> r)
    {
        if (!TryF(_exampleRating, out var rating) || rating < 0 ||
            !int.TryParse(_exampleLevel?.Trim(), NumberStyles.Integer, Inv, out var level) || level < 1)
        {
            XpExample = "";
            return;
        }

        // the rating typed is the creature's with two or three players; alone the game doesn't multiply it
        var twoPlayerXp = TypedDifficulty().TwoPlayerXp;
        var solo = twoPlayerXp > 0 ? rating / twoPlayerXp : rating;
        int Kill(int players) => Da2Executable.KillXp(players == 1 ? solo : rating, level * players, players,
            r["xp.exponent"], r["xp.share"], r["xp.shareEach"]);
        int Troll(int players) => Da2Executable.KillXp(players == 1 ? solo : rating, level * players, players,
            r["xp.trollExponent"], 1f, r["xp.trollShareEach"]);
        var n = _examplePlayers;
        string Each(int p) => p == 1 ? $"one player (rated {solo:0.#}): {Kill(1):N0}" : $"{(p == 2 ? "two" : "three")} players: {Kill(p):N0} each";
        var others = Enumerable.Range(1, 3).Where(p => p != n).Select(Each);
        XpExample = $"{(n == 1 ? $"Alone (rated {solo:0.#}) the player gets" : "Each of the " + n + " players gets")} {Kill(n):N0} XP for the kill " +
                    $"({string.Join("; ", others)}). A troll rated the same pays {Troll(n):N0}{(n == 1 ? "" : " each")}. Class and item bonuses come on top.";
    }

    private void RefreshDrops() => RefreshDrops(TypedSettings());

    private void RefreshDrops(Dictionary<string, float> r)
    {
        var low = r["loot.lowChance"];
        foreach (var row in Drops)
        {
            if (!TryF(row.Cells[3].Text, out var chance))
            {
                row.OnePlayer = row.MorePlayers = "";
                continue;
            }

            var c = chance < 1f ? chance * low : chance;
            static string P(float v) => (Math.Clamp(v, 0f, 1f) * 100).ToString("0.#", Inv) + " %";
            row.OnePlayer = $"item {P(c)} · potion {P(c + c)}";
            row.MorePlayers = $"item {P(c + c)} · potion {P(4 * c)}";
        }
    }

    private void RefreshLootExample(Dictionary<string, float> r)
    {
        var level = LootLevel;
        if (level == null || !TryF(level.MonsterLevel, out var l))
        {
            LootExample = "";
            return;
        }

        var flags = level.Source.Flags;
        var gold = Da2Executable.LevelLootValue(k => r[k], true, l, flags, _lootDifficulty);
        var value = Da2Executable.LevelLootValue(k => r[k], false, l, flags, _lootDifficulty);
        // the goblin: gold scale 2.0 (x 2 with two players counted), quality 1.8
        var pile = r["loot.goldFactor"] * gold * 2f * 2f;
        var lo = (int)(r["loot.pileBase"] * pile);
        var hi = (int)((r["loot.pileBase"] + r["loot.pileSpread"] - 1) * pile);
        var item = r["loot.itemValue"] * value;
        LootExample = string.Create(Inv,
            $"{level.Name} (act {level.Act}, monster level {l:0.##}) on {Da2Executable.DifficultyNames[_lootDifficulty]}: gold value {gold:0.###}, " +
            $"item value {value:0.###}. A goblin's gold pile with two or three players: {lo:N0}–{hi:N0} gold; an item of a plain roll " +
            $"is worth about {item * r["loot.itemFloor"]:N0}–{item * (r["loot.itemFloor"] + r["loot.itemSpread"]):N0} gold (× {r["loot.qualityLow"]:0.##} for a " +
            $"poor roll, × {r["loot.qualityHigh"]:0.##} for a good one).");
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

        var rules = new Dictionary<string, float>();
        foreach (var s in Da2Executable.Settings)
            rules[s.Key] = ParseF(_settingCells[s.Key].Text, $"{s.Group}, {s.Name}");
        // a box left as it was keeps the exact value it was filled with (a percentage shows only two decimals)
        foreach (var s in Da2Executable.Settings)
        {
            if (_settingExact.TryGetValue(s.Key, out var exact) && _settingCells[s.Key].Text == SettingText(s, exact)) rules[s.Key] = exact;
        }

        exe.SetSettings(rules);

        foreach (var (constant, cell) in _dropCells)
        {
            var argument = Da2Executable.DropArgument(constant);
            exe.SetDropValue(constant, ParseF(cell.Text, $"{_dropOwners[constant]}, {Da2Drop.Arguments[argument].ToLowerInvariant()}"));
        }

        foreach (var q in Quality)
            exe.SetQualityRow(q.Level, ParseF(q.Poor, $"Quality level {q.Level}, poor"), ParseF(q.Plain, $"Quality level {q.Level}, plain"));

        // show what is written: a constant without an ori keeps only the upper half of the float (2500 -> 2496)
        foreach (var (constant, cell) in _constantCells) cell.Show(F(exe.ConstantValue(constant)));
        foreach (var b in Bosses) b.Hp = F(exe.ConstantValue(b.Source.Constant));
        var written = exe.GetDifficulty();
        foreach (var (p, cell) in _difficultyCells) cell.Show(F(Get(written, p)));
        var writtenRules = exe.GetSettings();
        foreach (var s in Da2Executable.Settings)
        {
            _settingExact[s.Key] = writtenRules[s.Key];
            _settingCells[s.Key].Show(SettingText(s, writtenRules[s.Key]));
        }

        foreach (var (constant, cell) in _dropCells) cell.Show(F(exe.ConstantValue(constant)));
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
