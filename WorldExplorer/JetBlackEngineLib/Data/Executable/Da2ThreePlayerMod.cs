// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2ThreePlayerMod.cs
//
// The Dark Alliance II three-player mod (SLUS_206.75 v1.03, PCSX2 CRC B0859096) as a PCSX2
// cheat file: SLUS-20675_B0859096.pnach in PCSX2's cheats folder. The mod is not a patched
// executable (that would change the CRC the file is named after): every change is a
// "patch=" line, and each option is a [group] that is ticked on or off in PCSX2.
//
// The tested build 51 (da2_3p_build51.pnach, embedded) is reproduced byte for byte with the
// default settings. Its groups:
//   [3 Player mod]                    the mod itself (~21,500 words: cave code at 0xA0000,
//                                     relocated tables, ~130 hooks)
//   [HUD size 100% / 85% / 60% / 50%] V_HUDOPT 0xA1C34 = 0x48530000 + percent x 256 / 100
//                                     (none ticked: 70 %); any size from 25 to 100 % works
//   [P3 character - <name>]           V_P3CLASS 0xA1BE8: 0x5033nnnn = the class of a player 3 who
//                                     joins a loaded game without one, 0x5034nnnn = forced (DA1 heroes)
//   [DA1 heroes - shouts kept in sound memory]   V_SNDOPT 0xA1FA4 = 0x53440002
//   [TEST - all players always have N feat points]   patch=1 on record + 0xE0 of the 3 players
//   [TEST - party always has N gold]  patch=1 on record[0] + 0xDC
//   [TEST - Trade Way goblins with player spells]   cave3 at 0xAC000 + 8 hooks
//   [TEST - DA1 boss: <name> in Trollbark Forest]   cave4 at 0xAE000 (spawn table 0xAE010),
//                                     an enable word per boss and the spawn hook at 0x271C10
//   [Shops - fresh random stock every visit, more items]   cave5 at 0xAF000: gold value bounds
//                                     per act and kind (0xAF000), items per tab (0xAF060), Extreme
//                                     floors (0xAF06C), then the code; hooks 0x2A4C2C / 0x2A6A08
//   [Feats - new rank costs, Vahn + Borador + Kromlech extras]   cave6 at 0xE0100: the price
//                                     table CostTab ({feat id, 5 prices} rows, a zero row ends it)
//                                     is the last thing in the code area, so it may grow up to 0xE1000
//
// Reading a cheat file back: the [3 Player mod] group is identified by a SHA-256 of its patch
// lines (builds 16-51 are known; 43-47 and 48-50 share theirs), the options are recognised by
// name and their settings read from their words, groups of older builds that were renamed are
// mapped (shouts from the disc, 50000 gold, player 3's feat points, the build 50 feat group),
// and every other group is kept as it is. Saving always writes the build 51 groups.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using JetBlackEngineLib.Data.Pnach;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The random shop stock of the shops group: gold value bounds per act and kind, items per tab, Extreme floors.</summary>
public sealed class Da2ShopSettings
{
    public const int Acts = 4, Kinds = 3;

    /// <summary>Weapons, armour, other (potions, jewellery, gems).</summary>
    public static readonly string[] KindNames = { "Weapons", "Armour", "Other" };

    /// <summary>Lowest gold value of a random item, [act * 3 + kind] (act 0 = act 1).</summary>
    public float[] Low { get; init; } = new float[Acts * Kinds];

    /// <summary>Highest gold value of a random item, [act * 3 + kind].</summary>
    public float[] High { get; init; } = new float[Acts * Kinds];

    /// <summary>Random items added to the weapons, armour and other tabs.</summary>
    public int[] Counts { get; init; } = new int[Kinds];

    /// <summary>On Extreme, the lowest gold value per kind (from act 2 on).</summary>
    public float[] ExtremeFloor { get; init; } = new float[Kinds];

    public Da2ShopSettings Clone() => new()
    {
        Low = (float[])Low.Clone(), High = (float[])High.Clone(),
        Counts = (int[])Counts.Clone(), ExtremeFloor = (float[])ExtremeFloor.Clone()
    };

    public bool SameAs(Da2ShopSettings o) =>
        Bits(Low).SequenceEqual(Bits(o.Low)) && Bits(High).SequenceEqual(Bits(o.High)) &&
        Counts.SequenceEqual(o.Counts) && Bits(ExtremeFloor).SequenceEqual(Bits(o.ExtremeFloor));

    private static IEnumerable<int> Bits(float[] f) => f.Select(BitConverter.SingleToInt32Bits);
}

/// <summary>What goes into the cheat file: which of the mod's groups, and their settings.</summary>
public sealed class Da2ModSettings
{
    /// <summary>[3 Player mod] - the mod itself.</summary>
    public bool IncludeMain { get; set; } = true;

    /// <summary>One "[HUD size N%]" group per entry (25-100); with none ticked in PCSX2 the HUD is 70 %.</summary>
    public List<int> HudSizes { get; set; } = new() { 100, 85, 60, 50 };

    /// <summary>The ten "[P3 character - ...]" groups.</summary>
    public bool IncludePlayerThreeCharacters { get; set; } = true;

    /// <summary>[DA1 heroes - shouts kept in sound memory].</summary>
    public bool IncludeShoutsInSoundMemory { get; set; } = true;

    public bool IncludeFeatPoints { get; set; } = true;
    public int FeatPoints { get; set; } = Da2ThreePlayerMod.StockFeatPoints;

    public bool IncludeGold { get; set; } = true;
    public int Gold { get; set; } = Da2ThreePlayerMod.StockGold;

    public bool IncludeTradeWayGoblins { get; set; } = true;

    /// <summary>Creatures placed when a level loads, one group each (build 51: the three DA1 bosses in Trollbark Forest).</summary>
    public List<Da2Placement> Placements { get; set; } = Da2ThreePlayerMod.StockPlacements();

    public bool IncludeShops { get; set; } = true;
    public Da2ShopSettings Shops { get; set; } = Da2ThreePlayerMod.StockShops();

    public bool IncludeFeatPrices { get; set; } = true;

    /// <summary>Feat id -> price of ranks 1..n (1-5 prices, each 1-255).</summary>
    public SortedDictionary<int, int[]> FeatPrices { get; set; } = Da2ThreePlayerMod.StockFeatPrices();

    public Da2ModSettings Clone() => new()
    {
        IncludeMain = IncludeMain,
        HudSizes = new List<int>(HudSizes),
        IncludePlayerThreeCharacters = IncludePlayerThreeCharacters,
        IncludeShoutsInSoundMemory = IncludeShoutsInSoundMemory,
        IncludeFeatPoints = IncludeFeatPoints,
        FeatPoints = FeatPoints,
        IncludeGold = IncludeGold,
        Gold = Gold,
        IncludeTradeWayGoblins = IncludeTradeWayGoblins,
        Placements = Placements.Select(p => p.Clone()).ToList(),
        IncludeShops = IncludeShops,
        Shops = Shops.Clone(),
        IncludeFeatPrices = IncludeFeatPrices,
        FeatPrices = new SortedDictionary<int, int[]>(FeatPrices.ToDictionary(p => p.Key, p => (int[])p.Value.Clone()))
    };
}

/// <summary>How the [3 Player mod] group of a cheat file compares with the builds WorldExplorer knows.</summary>
public enum Da2ModMainState
{
    /// <summary>The file has no [3 Player mod] group.</summary>
    Missing,
    /// <summary>The current build (51).</summary>
    Current,
    /// <summary>An older tested build; saving brings every group up to the current build.</summary>
    Older,
    /// <summary>Not a build WorldExplorer knows (changed by hand?); saving replaces it with the current build.</summary>
    Unknown
}

/// <summary>A cheat file read back: the mod's settings, which build its main group is, and everything else in it.</summary>
public sealed class Da2ModFile
{
    public Da2ModSettings Settings { get; init; } = new();
    public Da2ModMainState MainState { get; init; }

    /// <summary>The builds whose main group the file has (several builds can share one), empty if none.</summary>
    public IReadOnlyList<int> MainBuilds { get; init; } = Array.Empty<int>();

    /// <summary>The executable tuning group written by WorldExplorer, if the file has one.</summary>
    public PnachGroup? Tuning { get; init; }

    /// <summary>Groups that are not the mod's or WorldExplorer's (the user's own cheats): kept as they are.</summary>
    public List<PnachGroup> OtherGroups { get; init; } = new();

    /// <summary>Notes about groups of older builds that were mapped or left out.</summary>
    public List<string> Notes { get; init; } = new();

    /// <summary>The "gametitle=" line, or null.</summary>
    public string? GameTitle { get; init; }
}

public static partial class Da2ThreePlayerMod
{
    public const int CurrentBuild = 51;
    public const string FileName = "SLUS-20675_B0859096.pnach";
    public const string GameTitleLine = "gametitle=Baldur's Gate - Dark Alliance II (SLUS-20675)";
    public const string Author = "H + Claude";

    public const string MainGroup = "3 Player mod";
    public const string ShoutsGroup = "DA1 heroes - shouts kept in sound memory";
    public const string TradeWayGroup = "TEST - Trade Way goblins with player spells";
    public const string ShopsGroup = "Shops - fresh random stock every visit, more items";
    public const string FeatsGroup = "Feats - new rank costs, Vahn + Borador + Kromlech extras";
    public static readonly string[] BossNames = { "Beholder", "Lilivarra", "Minotaur" };
    public static string BossGroup(int i) => $"TEST - DA1 boss: {BossNames[i]} in Trollbark Forest";

    /// <summary>The ten player 3 character groups, in the file's order.</summary>
    public static readonly string[] PlayerThreeGroups =
    {
        "P3 character - Dorn", "P3 character - Vhaidra", "P3 character - Ysuran", "P3 character - Borador",
        "P3 character - Allessia", "P3 character - Drizzt", "P3 character - Artemis",
        "P3 character - Vahn (DA1 hero, test)", "P3 character - Adrianna (DA1 hero, test)",
        "P3 character - Kromlech (DA1 hero, test)"
    };

    public static readonly int[] StockHudSizes = { 100, 85, 60, 50 };
    public const int MinHudSize = 25, MaxHudSize = 100, MaxHudGroups = 12;
    public const int StockFeatPoints = 200, StockGold = 500000;
    public const int MaxFeatPoints = 9999, MaxGold = 99_999_999;
    public const int MaxShopItems = 64;
    public const float MinShopValue = 1f, MaxShopValue = 5_000_000f;
    public const int MaxFeatPrice = 255;

    // ---- addresses in the groups ----
    public const uint HudOption = 0x000A1C34, HudMagic = 0x48530000;
    public const uint PlayerThreeClass = 0x000A1BE8;
    public const uint SoundOption = 0x000A1FA4;
    private const uint RecordBase = 0x000B0000, RecordSize = 25392, FeatPointsField = 0xE0, GoldField = 0xDC;
    private const uint ShopBudget = 0x000AF000, ShopCounts = 0x000AF060, ShopFloor = 0x000AF06C;
    private const uint FeatHook = 0x002BB158, FeatAreaEnd = 0x000E1000;

    /// <summary>The words of the feat price table end on a 16-byte boundary (zero padding), as in build 51.</summary>
    private const int TableAlign = 16;

    private const string HudDescription =
        "description=Size of the three player panels at the top (without any of these ticked: 70%).";

    // ---- the builds whose [3 Player mod] group is known (SHA-256 of its patch lines) ----

    private static readonly (string Sha256, int[] Builds)[] KnownMainGroups =
    {
        ("5b042c3de1dad94e34a205e80b2cecc6c7de4145d93558836f1279c6b0326b86", new[] { 16 }),
        ("4658838ed244112b47479c548b23224a35aa73b063174402a1ad30ca7b50e5b8", new[] { 17 }),
        ("21374cb8ca06ccabe686a2fe7205f7ccd256edbcab283ba4e7aea8f08c6ad298", new[] { 20 }),
        ("f735396fdd0b8bfb6f423892ec376191dcf189d2a8fcc6b9941922e8572e4abc", new[] { 21 }),
        ("95f667ca9e6aa5d4e54bb6f066d8387608700bf2e7caafb16c15ff51656261a4", new[] { 22 }),
        ("e355c914e0e84285b6c0005ac7357b9791f28e7227c82b20afc903416840452b", new[] { 23 }),
        ("b63e9386ad5139b34ac7c22976e2c97477cc1f05203cd7dfd7842e821d396fc9", new[] { 24 }),
        ("1d2720c274379ff139cbd4c6dd94bc519d3389c7c6ec18c16f7a8d8e1eec068a", new[] { 25 }),
        ("4e3f49502da97cd56f27b6173be384607e2424241411ba13f20d776f83096e6b", new[] { 26 }),
        ("ca9e16c0102cf7e16bfa4c4d262e7809705fed3e29f66eb7222fb70462515b86", new[] { 27 }),
        ("7d39cd00bdb02a968728f6b523ec743248f43466bf24265625898b420f662f14", new[] { 28 }),
        ("ecfc64460593a0cafa3532e8bce9a88e31683f9d5120e5f12d41b2c66d56f8bc", new[] { 29 }),
        ("e55f5edf703e4e6da03757ccc2531820918f37964e15616aaa19be67db81519c", new[] { 30 }),
        ("53018ac37c8406b79502c716e9b2611234a05d89ab9df1f743d944cdcf38d47e", new[] { 31 }),
        ("ff66e1f987c73a876e70fe81df0bddc52f298cae3b82235a547ec5211261b8b6", new[] { 32 }),
        ("5cbc3333db5aaaf2be1fa752bcdb360f95f66c6b3ccffa5f71f8d4307ab96f66", new[] { 33 }),
        ("dd3418285bce3baa1b5422dbf50806eb02a2e33b392c7c06e7213269073dcf9a", new[] { 34 }),
        ("e4f8fe5cd8c82043db25eead7d2d322f4321cacfb95d7e642c574bea6a43196f", new[] { 35 }),
        ("7f3e38f429034ea82996b16308e6e444e27ec425cfcbfcd269f09390b67f0749", new[] { 36 }),
        ("91cea738e18341e33b6f829d425c49ddd2487c5a5e6c0da26d91210be5c3bf0b", new[] { 37 }),
        ("20b695eb3433cc85979d02557ed3a8003d6d9503eff970f15fecf7716fed5567", new[] { 38 }),
        ("ff8943dc8d4516f56d45060b2dd9cd23d53898fa6db858536abe70ee16348dc7", new[] { 39 }),
        ("51f62566814765fc3754727d5f4d479820e13132c1d194ea2bf79eea65c40915", new[] { 40 }),
        ("866cb098cfbb07577091086c18aa13a5038adbfa9c1fb415e7b52a53f5bd8220", new[] { 41 }),
        ("cf9a2e11c9ad6d5abb750b9ca8bf781ed2d555f9a819d134206a676babd87623", new[] { 42 }),
        ("1ef899a7059f66a7926c07c14a0adb650bb5b7333536d3693fdd9393b3fb95ae", new[] { 43, 44, 45, 46, 47 }),
        ("db3f417cecf901115fb11b4d5fb4058aa32f934512db9a4da7482b484e56bb5c", new[] { 48, 49, 50 }),
        ("73e5abe2e1f84f4c4f8593e7aec4bd3c52bf05606ff290c3b0d4c887e15e4583", new[] { 51 }),
    };

    // ---- the embedded build ----

    private static readonly Lazy<PnachFile> StockFile = new(LoadStock);

    /// <summary>Build 51 exactly as it was deployed.</summary>
    public static string StockText => StockFile.Value.ToText();

    private static PnachFile LoadStock()
    {
        const string name = "JetBlackEngineLib.Data.Executable.Da2.da2_3p_build51.pnach";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                           ?? throw new InvalidDataException("The built-in three-player mod (build 51) is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var file = PnachFile.Parse(reader.ReadToEnd());
        var main = file.Find(MainGroup) ?? throw new InvalidDataException("The built-in three-player mod is damaged.");
        if (!BuildsOf(main).SequenceEqual(new[] { CurrentBuild }))
            throw new InvalidDataException("The built-in three-player mod (build 51) is damaged.");
        return file;
    }

    private static PnachGroup Stock(string name) =>
        StockFile.Value.Find(name) ?? throw new InvalidDataException($"The built-in mod has no [{name}] group.");

    /// <summary>The group's lines without the blank lines at its end.</summary>
    private static List<string> Body(PnachGroup g)
    {
        var lines = new List<string>(g.Lines);
        while (lines.Count > 0 && lines[^1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    /// <summary>SHA-256 of the group's patch lines (normalised, one per line).</summary>
    public static string Fingerprint(PnachGroup g)
    {
        var text = string.Join("\n", g.Patches.Select(p => p.ToLine()));
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(text))).ToLowerInvariant();
    }

    /// <summary>The builds whose [3 Player mod] group this is, empty if it is none of them.</summary>
    public static int[] BuildsOf(PnachGroup main)
    {
        var h = Fingerprint(main);
        foreach (var (sha, builds) in KnownMainGroups)
        {
            if (sha == h) return builds;
        }

        return Array.Empty<int>();
    }

    // ---- stock settings ----

    public static Da2ModSettings DefaultSettings() => new();

    /// <summary>The shop settings of build 51 (read from its words).</summary>
    public static Da2ShopSettings StockShops() => ReadShops(Stock(ShopsGroup).BootWords())!;

    /// <summary>The feat prices of build 51 (H's list of 2026-10-08).</summary>
    public static SortedDictionary<int, int[]> StockFeatPrices() =>
        ReadFeatPrices(Stock(FeatsGroup).BootWords(), out _, out _)!;

    // ---- writing ----

    /// <summary>
    /// The cheat file: the mod's groups for these settings in build 51's order, then the
    /// executable tuning group and the other groups. Throws <see cref="ArgumentException"/>
    /// naming the first bad setting (nothing is produced).
    /// </summary>
    public static PnachFile Write(Da2ModSettings settings, PnachGroup? tuning = null,
        IEnumerable<PnachGroup>? otherGroups = null)
    {
        Validate(settings);
        var others = otherGroups?.ToList() ?? new List<PnachGroup>();
        var groups = BuildGroups(settings);
        var notPlaced = BuildGroups(settings, placements: false).Concat(others).Select(g => g.Name)
            .Concat(tuning != null ? new[] { tuning.Name } : Array.Empty<string>());
        ValidatePlacements(settings.Placements, notPlaced);
        if (tuning != null) groups.Add(tuning);
        groups.AddRange(others);
        return Compose(new[] { GameTitleLine }, groups);
    }

    /// <summary>Header, then each group after a blank line (trailing blank lines of the groups dropped).</summary>
    public static PnachFile Compose(IEnumerable<string> header, IEnumerable<PnachGroup> groups)
    {
        var head = header.ToList();
        while (head.Count > 0 && head[^1].Trim().Length == 0) head.RemoveAt(head.Count - 1);
        var list = groups.Select(g => new PnachGroup(g.Name, Body(g))).ToList();
        if (list.Count > 0) head.Add("");
        for (var i = 0; i < list.Count - 1; i++) list[i].Lines.Add("");
        return new PnachFile(head, list) { NewLine = "\n", EndsWithNewLine = true };
    }

    /// <summary>The mod's groups for the settings (not validated).</summary>
    private static List<PnachGroup> BuildGroups(Da2ModSettings s, bool placements = true)
    {
        var groups = new List<PnachGroup>();
        if (s.IncludeMain) groups.Add(Stock(MainGroup).Clone());

        foreach (var size in s.HudSizes) groups.Add(HudGroup(size));

        if (s.IncludePlayerThreeCharacters)
        {
            groups.AddRange(PlayerThreeGroups.Select(n => Stock(n).Clone()));
        }

        if (s.IncludeShoutsInSoundMemory) groups.Add(Stock(ShoutsGroup).Clone());
        if (s.IncludeFeatPoints) groups.Add(FeatPointsGroup(s.FeatPoints));
        if (s.IncludeGold) groups.Add(GoldGroup(s.Gold));
        if (s.IncludeTradeWayGoblins) groups.Add(Stock(TradeWayGroup).Clone());
        if (placements) groups.AddRange(PlacementGroups(s.Placements));

        if (s.IncludeShops) groups.Add(ShopsGroupFor(s.Shops));
        if (s.IncludeFeatPrices) groups.Add(FeatsGroupFor(s.FeatPrices));
        return groups;
    }

    public static string HudGroupName(int percent) =>
        string.Create(CultureInfo.InvariantCulture, $"HUD size {percent}%");

    /// <summary>The word the HUD option holds for a size (0x48530000 + percent x 256 / 100).</summary>
    public static uint HudWord(int percent) => HudMagic + (uint)Math.Round(percent * 256 / 100.0, MidpointRounding.AwayFromZero);

    private static PnachGroup HudGroup(int percent)
    {
        if (StockHudSizes.Contains(percent)) return Stock(HudGroupName(percent)).Clone();
        return new PnachGroup(HudGroupName(percent), new[]
        {
            "author=" + Author, HudDescription, PnachPatch.Word(HudOption, HudWord(percent)).ToLine()
        });
    }

    public static string FeatPointsGroupName(int points) =>
        string.Create(CultureInfo.InvariantCulture, $"TEST - all players always have {points} feat points");

    private static PnachGroup FeatPointsGroup(int points)
    {
        var stock = Stock(FeatPointsGroupName(StockFeatPoints));
        if (points == StockFeatPoints) return stock.Clone();
        var lines = Body(stock).Where(l => PnachPatch.TryParse(l) == null).ToList();
        for (uint p = 0; p < 3; p++)
            lines.Add(PnachPatch.EveryFrame(RecordBase + p * RecordSize + FeatPointsField, (uint)points).ToLine());
        return new PnachGroup(FeatPointsGroupName(points), lines);
    }

    public static string GoldGroupName(int gold) =>
        string.Create(CultureInfo.InvariantCulture, $"TEST - party always has {gold} gold");

    private static PnachGroup GoldGroup(int gold)
    {
        var stock = Stock(GoldGroupName(StockGold));
        if (gold == StockGold) return stock.Clone();
        return new PnachGroup(GoldGroupName(gold), new[]
        {
            "author=" + Author,
            "description=TEST ONLY. For buying weapons and armour to try (e.g. a warhammer for Kromlech, or the random " +
            "shop stock). Untick it again afterwards.",
            PnachPatch.EveryFrame(RecordBase + GoldField, (uint)gold).ToLine()
        });
    }

    /// <summary>The shops group with these settings: build 51's lines with the data words replaced.</summary>
    private static PnachGroup ShopsGroupFor(Da2ShopSettings shops)
    {
        var stock = Stock(ShopsGroup);
        if (shops.SameAs(StockShops())) return stock.Clone();

        var words = ShopWords(shops);
        var lines = new List<string>();
        foreach (var line in Body(stock))
        {
            if (line.StartsWith("description=", StringComparison.Ordinal))
            {
                lines.Add(ShopsDescription(shops));
                continue;
            }

            if (PnachPatch.TryParse(line) is { } p && words.TryGetValue(p.Address, out var w))
                lines.Add((p with { Value = w }).ToLine());
            else
                lines.Add(line);
        }

        return new PnachGroup(ShopsGroup, lines);
    }

    private static string N(float v) => ((double)v).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The description for edited shop settings, in the words of build 51's own.</summary>
    public static string ShopsDescription(Da2ShopSettings s)
    {
        var text = "description=Shops roll their stock afresh each time they open: the weapons tab is " +
                   $"{s.Counts[0]} random weapons and the armour tab {s.Counts[1]} random armour pieces (no fixed " +
                   $"lists any more); the other tab keeps its usual potions and gems plus {s.Counts[2]} random items, " +
                   "priced anywhere from cheap to very expensive with the whole range rising by act. On Extreme the " +
                   $"random items are worth at least {N(s.ExtremeFloor[0])} (weapons) / {N(s.ExtremeFloor[1])} (armour) / " +
                   $"{N(s.ExtremeFloor[2])} (other) gold from act 2 on; potions and gems keep their prices. (The game " +
                   "only had random stock on Extreme, 17 / 17 / 8 items that came out the same every visit.) Takes " +
                   "effect at the next shop visit.";
        var stock = StockShops();
        if (!s.Low.SequenceEqual(stock.Low) || !s.High.SequenceEqual(stock.High))
            text += " Price ranges per act edited in WorldExplorer.";
        return text;
    }

    private static Dictionary<uint, uint> ShopWords(Da2ShopSettings s)
    {
        var words = new Dictionary<uint, uint>();
        for (var i = 0; i < Da2ShopSettings.Acts * Da2ShopSettings.Kinds; i++)
        {
            words[ShopBudget + (uint)(8 * i)] = FloatBits(s.Low[i]);
            words[ShopBudget + (uint)(8 * i + 4)] = FloatBits(s.High[i]);
        }

        for (var k = 0; k < Da2ShopSettings.Kinds; k++)
        {
            words[ShopCounts + (uint)(4 * k)] = (uint)s.Counts[k];
            words[ShopFloor + (uint)(4 * k)] = FloatBits(s.ExtremeFloor[k]);
        }

        return words;
    }

    private static uint FloatBits(float f) => (uint)BitConverter.SingleToInt32Bits(f);
    private static float FloatOf(uint w) => BitConverter.Int32BitsToSingle((int)w);

    /// <summary>Reads the shop settings from a shops group's words; null when they aren't all there.</summary>
    private static Da2ShopSettings? ReadShops(Dictionary<uint, uint> words)
    {
        var s = new Da2ShopSettings();
        for (var i = 0; i < Da2ShopSettings.Acts * Da2ShopSettings.Kinds; i++)
        {
            if (!words.TryGetValue(ShopBudget + (uint)(8 * i), out var lo) ||
                !words.TryGetValue(ShopBudget + (uint)(8 * i + 4), out var hi)) return null;
            s.Low[i] = FloatOf(lo);
            s.High[i] = FloatOf(hi);
        }

        for (var k = 0; k < Da2ShopSettings.Kinds; k++)
        {
            if (!words.TryGetValue(ShopCounts + (uint)(4 * k), out var c) ||
                !words.TryGetValue(ShopFloor + (uint)(4 * k), out var f)) return null;
            s.Counts[k] = (int)c;
            s.ExtremeFloor[k] = FloatOf(f);
        }

        return s;
    }

    /// <summary>
    /// The feat group with these prices: build 51's lines with the price table rebuilt (it is the
    /// last thing in the group's code area, so it may grow or shrink).
    /// </summary>
    private static PnachGroup FeatsGroupFor(SortedDictionary<int, int[]> prices)
    {
        var stock = Stock(FeatsGroup);
        var stockPrices = ReadFeatPrices(stock.BootWords(), out var table, out var tableEnd)!;
        if (SamePrices(prices, stockPrices)) return stock.Clone();

        var rows = new List<uint>();
        foreach (var (id, p) in prices)
        {
            rows.Add((uint)id);
            for (var r = 0; r < 5; r++) rows.Add((uint)p[Math.Min(r, p.Length - 1)]);
        }

        rows.AddRange(new uint[6]);                          // the zero row ends the table
        while ((table + 4 * rows.Count) % TableAlign != 0) rows.Add(0);
        if (table + 4 * rows.Count > FeatAreaEnd)
            throw new ArgumentException($"The feat price table has room for {(FeatAreaEnd - table) / 24 - 1} feats.");

        var lines = new List<string>();
        var inserted = false;
        foreach (var line in Body(stock))
        {
            if (line.StartsWith("description=", StringComparison.Ordinal))
            {
                lines.Add(line + " Prices edited in WorldExplorer.");
                continue;
            }

            if (PnachPatch.TryParse(line) is { } p && p.Address >= table && p.Address < tableEnd)
            {
                if (inserted) continue;
                for (var i = 0; i < rows.Count; i++)
                    lines.Add(PnachPatch.Word(table + (uint)(4 * i), rows[i]).ToLine());
                inserted = true;
                continue;
            }

            lines.Add(line);
        }

        return new PnachGroup(FeatsGroup, lines);
    }

    private static bool SamePrices(SortedDictionary<int, int[]> a, SortedDictionary<int, int[]> b) =>
        a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var q) && Padded(p.Value).SequenceEqual(Padded(q)));

    private static int[] Padded(int[] p) => Enumerable.Range(0, 5).Select(r => p[Math.Min(r, p.Length - 1)]).ToArray();

    /// <summary>
    /// Reads the price table of a feat group (build 50 or 51): it is found through the hook at
    /// 0x2BB158 (j FeatCostX), whose code loads the table with lui v1 / addiu v1 just before
    /// "lw a2, 0(v1)". Returns null when the words aren't there. <paramref name="table"/> and
    /// <paramref name="tableEnd"/> give the table's words (padding included).
    /// </summary>
    private static SortedDictionary<int, int[]>? ReadFeatPrices(Dictionary<uint, uint> words, out uint table, out uint tableEnd)
    {
        table = tableEnd = 0;
        if (!words.TryGetValue(FeatHook, out var hook) || hook >> 26 != 2) return null;
        var code = (hook & 0x03FFFFFF) << 2;
        for (var a = code; a < code + 0x100; a += 4)
        {
            if (!words.TryGetValue(a, out var lui) || !words.TryGetValue(a + 4, out var addiu) ||
                !words.TryGetValue(a + 8, out var lw)) continue;
            if ((lui & 0xFFFF0000) != 0x3C030000 || (addiu & 0xFFFF0000) != 0x24630000 || lw != 0x8C660000) continue;
            table = ((lui & 0xFFFF) << 16) + (uint)(short)(addiu & 0xFFFF);
            break;
        }

        if (table == 0) return null;
        var prices = new SortedDictionary<int, int[]>();
        var at = table;
        while (true)
        {
            if (!words.TryGetValue(at, out var id)) return null;
            if (id == 0) break;
            var p = new int[5];
            for (var r = 0; r < 5; r++)
            {
                if (!words.TryGetValue(at + 4 + (uint)(4 * r), out var v)) return null;
                p[r] = (int)v;
            }

            prices[(int)id] = Trim(p);
            at += 24;
            if (at >= FeatAreaEnd) return null;
        }

        tableEnd = at + 24;
        while (tableEnd % TableAlign != 0 && words.TryGetValue(tableEnd, out var pad) && pad == 0) tableEnd += 4;
        return prices;
    }

    /// <summary>Drops the repeats of the last price (3-rank feats are padded to five words).</summary>
    private static int[] Trim(int[] p)
    {
        var n = p.Length;
        while (n > 1 && p[n - 1] == p[n - 2]) n--;
        return p.Take(n).ToArray();
    }

    // ---- validation ----

    /// <summary>Throws <see cref="ArgumentException"/> naming the first bad setting.</summary>
    public static void Validate(Da2ModSettings s)
    {
        if (s.HudSizes.Count > MaxHudGroups)
            throw new ArgumentException($"At most {MaxHudGroups} HUD sizes can be offered.");
        foreach (var size in s.HudSizes)
        {
            if (size < MinHudSize || size > MaxHudSize)
                throw new ArgumentException($"HUD sizes must be from {MinHudSize} to {MaxHudSize} %.");
        }

        if (s.HudSizes.Distinct().Count() != s.HudSizes.Count)
            throw new ArgumentException("Each HUD size can be offered once.");
        if (s.FeatPoints < 1 || s.FeatPoints > MaxFeatPoints)
            throw new ArgumentException($"The test feat points must be from 1 to {MaxFeatPoints}.");
        if (s.Gold < 1 || s.Gold > MaxGold)
            throw new ArgumentException($"The test gold must be from 1 to {MaxGold:N0}.");
        ValidateShops(s.Shops);
        ValidateFeatPrices(s.FeatPrices);
    }

    public static void ValidateShops(Da2ShopSettings s)
    {
        for (var a = 0; a < Da2ShopSettings.Acts; a++)
        {
            for (var k = 0; k < Da2ShopSettings.Kinds; k++)
            {
                var lo = s.Low[a * 3 + k];
                var hi = s.High[a * 3 + k];
                var what = $"Shops, act {a + 1}, {Da2ShopSettings.KindNames[k].ToLowerInvariant()}";
                if (float.IsNaN(lo) || lo < MinShopValue || lo > MaxShopValue || float.IsNaN(hi) || hi < MinShopValue || hi > MaxShopValue)
                    throw new ArgumentException($"{what}: gold values must be from {MinShopValue:0} to {MaxShopValue:N0}.");
                if (hi < lo)
                    throw new ArgumentException($"{what}: the highest value is below the lowest.");
            }
        }

        for (var k = 0; k < Da2ShopSettings.Kinds; k++)
        {
            if (s.Counts[k] < 0 || s.Counts[k] > MaxShopItems)
                throw new ArgumentException($"Shops: random {Da2ShopSettings.KindNames[k].ToLowerInvariant()} must be from 0 to {MaxShopItems} (a tab holds 64 items).");
            var f = s.ExtremeFloor[k];
            if (float.IsNaN(f) || f < 0 || f > MaxShopValue)
                throw new ArgumentException($"Shops: the Extreme floor for {Da2ShopSettings.KindNames[k].ToLowerInvariant()} must be from 0 to {MaxShopValue:N0}.");
        }
    }

    public static void ValidateFeatPrices(SortedDictionary<int, int[]> prices)
    {
        foreach (var (id, p) in prices)
        {
            var name = Da2Names.Feat(id);
            if (id < 1 || id > 127) throw new ArgumentException($"Feat id {id} is not a feat (1-127).");
            if (p.Length < 1 || p.Length > 5) throw new ArgumentException($"{name}: give one to five prices.");
            if (p.Any(v => v < 1 || v > MaxFeatPrice))
                throw new ArgumentException($"{name}: prices must be from 1 to {MaxFeatPrice}.");
        }
    }

    // ---- reading ----

    private static readonly Regex HudName = new(@"^HUD size (\d+)%$", RegexOptions.CultureInvariant);
    private static readonly Regex FeatPointsName = new(@"^TEST - (all players|player 3) always ha(?:ve|s) (\d+) feat points$", RegexOptions.CultureInvariant);
    private static readonly Regex GoldName = new(@"^TEST - party always has (\d+) gold$", RegexOptions.CultureInvariant);
    private const string OldFeatsGroup = "Feats - new rank costs, Vahn + Borador extras";
    private const string OldShoutsGroup = "DA1 heroes - shouts from the disc";

    /// <summary>Reads a cheat file: the mod's settings, the main group's build, the tuning group and the rest.</summary>
    public static Da2ModFile Read(PnachFile file, string tuningGroupName)
    {
        var s = new Da2ModSettings
        {
            IncludeMain = false, HudSizes = new List<int>(), IncludePlayerThreeCharacters = false,
            IncludeShoutsInSoundMemory = false, IncludeFeatPoints = false, IncludeGold = false,
            IncludeTradeWayGoblins = false, Placements = new List<Da2Placement>(), IncludeShops = false,
            IncludeFeatPrices = false
        };
        var state = Da2ModMainState.Missing;
        int[] builds = Array.Empty<int>();
        PnachGroup? tuning = null;
        var others = new List<PnachGroup>();
        var notes = new List<string>();

        s.Placements = ReadPlacements(file.Groups, out var placementGroups);
        foreach (var g in file.Groups)
        {
            if (placementGroups.Contains(g)) continue;
            var name = g.Name.Trim();
            var words = g.BootWords();
            if (name == MainGroup)
            {
                s.IncludeMain = true;
                builds = BuildsOf(g);
                state = builds.Length == 0 ? Da2ModMainState.Unknown
                    : builds.Contains(CurrentBuild) ? Da2ModMainState.Current : Da2ModMainState.Older;
            }
            else if (HudName.Match(name) is { Success: true } && words.TryGetValue(HudOption, out var hud) &&
                     (hud & 0xFFFF0000) == HudMagic)
            {
                var pct = (int)Math.Round((hud & 0xFFFF) * 100 / 256.0, MidpointRounding.AwayFromZero);
                if (pct >= MinHudSize && pct <= MaxHudSize && !s.HudSizes.Contains(pct)) s.HudSizes.Add(pct);
            }
            else if (PlayerThreeGroups.Contains(name))
            {
                s.IncludePlayerThreeCharacters = true;
            }
            else if (name == ShoutsGroup)
            {
                s.IncludeShoutsInSoundMemory = true;
            }
            else if (name == OldShoutsGroup)
            {
                notes.Add($"[{OldShoutsGroup}] (build 42) is left out: since build 43 the shouts are streamed from the " +
                          $"disc by default, and [{ShoutsGroup}] is the way back.");
            }
            else if (FeatPointsName.Match(name) is { Success: true } fp)
            {
                s.IncludeFeatPoints = true;
                var every = g.Patches.Where(p => p.Place == 1).Select(p => p.Value).ToList();
                s.FeatPoints = every.Count > 0 ? (int)Math.Clamp(every[0], 1u, MaxFeatPoints) : StockFeatPoints;
                if (fp.Groups[1].Value != "all players")
                    notes.Add($"[{name}] (builds 36-37) becomes [{FeatPointsGroupName(s.FeatPoints)}], for all three players.");
            }
            else if (GoldName.Match(name) is { Success: true })
            {
                s.IncludeGold = true;
                var every = g.Patches.Where(p => p.Place == 1).Select(p => p.Value).ToList();
                s.Gold = every.Count > 0 ? (int)Math.Clamp(every[0], 1u, MaxGold) : StockGold;
            }
            else if (name == TradeWayGroup)
            {
                s.IncludeTradeWayGoblins = true;
            }
            else if (name == ShopsGroup)
            {
                s.IncludeShops = true;
                if (ReadShops(words) is { } shops) s.Shops = shops;
            }
            else if (name == FeatsGroup || name == OldFeatsGroup)
            {
                s.IncludeFeatPrices = true;
                if (ReadFeatPrices(words, out _, out _) is { } prices) s.FeatPrices = prices;
                if (name == OldFeatsGroup)
                    notes.Add($"[{OldFeatsGroup}] (build 50) becomes [{FeatsGroup}] (build 51: Kromlech's three feats).");
            }
            else if (name == tuningGroupName)
            {
                tuning = g;
            }
            else
            {
                others.Add(g);
            }
        }

        return new Da2ModFile
        {
            Settings = s, MainState = state, MainBuilds = builds, Tuning = tuning, OtherGroups = others, Notes = notes,
            GameTitle = file.GameTitle
        };
    }

    /// <summary>A sentence on the main group's build for the window, e.g. "build 51 (current)".</summary>
    public static string DescribeBuilds(IReadOnlyList<int> builds)
    {
        if (builds.Count == 0) return "an unknown build";
        return builds.Count == 1 ? $"build {builds[0]}" : $"builds {builds[0]}-{builds[^1]}";
    }
}
