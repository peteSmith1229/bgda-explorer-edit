// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.BorrowedSpells.cs
//
// BORROWED SPELLS (the Enhancements tab): four spells the heroes learn from the game's monsters and two feats
// the game has but never offers, each a feat on a hero's level-up screen, each turned on or off by itself:
//   Xantam's Fire for Adrianna    the beholder Xantam's fire trails, which burn the monsters in their path;
//   Sorcerer's Blood for Adrianna the game's own cut feat, passive: each level she gains once she has it gives
//                                 her 1 more mana a rank ("+1 Mana Point gain per level. This is not
//                                 retroactive."); 1 rank by default, up to 5;
//   Power Attack for Kromlech     the game's own cut feat: casting it readies his weapon, and his next hits
//                                 (one at rank 1, up to five at rank 5) within 10 seconds do double damage;
//   Blade Barrier for Kromlech    Ilivarra's nine whirling axes: once a second every monster in reach is hit;
//   Antimagic Field for Kromlech  Ilivarra's field: fire, cold, electricity and magic do him no harm;
//   Righteous Might for Kromlech  Ilivarra's might: he grows, his weapon hits do more damage and take less.
// It is featpatch.py version 7, written from the data in BgdaFeatPatch.cs (generated from that script), so
// the bytes are the ones tested in the emulator.
//
// POWER ATTACK is the game's: its ability record (0x0030E2E0, bit 34, cast type 18, 5 mana), its grant, its
// HUD icon and line ("Damage: Double Normal Melee Damage") and its texts in five languages were all left in,
// but no hero's list offers it and its cast case does nothing. The patch adds its list entry to Kromlech's
// list and its cast case, and doubles his hits in the melee hit (where Righteous Might raises them). Its
// mana is its record's, which the Feats & Spells tab edits; it needs nothing in GTEXT.LMP or ICONS.LMP.
//
// SORCERER'S BLOOD is the game's too: its record (0x0030E3D0, bit 32, type 24, the passives') and its texts in
// five languages are in, but no list offers it and nothing reads its rank. The game works a hero's maximum
// mana out afresh (FUN_00167390: class, INT, level, Willpower), so a bonus that isn't retroactive needs a
// total kept from level to level: each level she gains with the feat adds her rank to the last rank byte of
// her record (+0x123, list entry 63's, which no list reaches; the game saves the whole record), and the
// maximum mana is the game's plus that total. Its list entry goes after Xantam's Fire in her list. It needs
// nothing in GTEXT.LMP or ICONS.LMP (a passive has no HUD slot).
//
// WHERE IT GOES. The pool: 61 stretches of the game's own code that nothing ever runs (unused library
// functions and leftover routines, 32,724 bytes; see featpool.py). They are the same addresses and bytes
// in the stock executable and in every three-player build, the game's own segment loads them, and no
// other patch WorldExplorer knows writes there. The patch takes 7,416 bytes of it: its data block at
// 0x00132D90, its code in ten pieces and four vtable copies. The code is the same whichever spells are
// on; only the hooks of the spells that are on are written (16 words when all six are), and the data
// block says which ones are. Nothing else changes: no program header, no start-up copy, the same file
// length.
//
// SO IT NEEDS NO THREE-PLAYER BUILD, and the Three Players tab can turn three players on or off
// underneath it: the three-player patch never touches these bytes, and the code finds the class lists
// (0x002A0478 in the stock game, 0x000D0100 with three players) from FUN_00162B78's own instructions while
// the game runs. (Checked: installing and switching three players commute.) Corrosive and Glow can be in
// as well: Righteous Might and Power Attack hook the instruction before the melee hit's call, which
// Corrosive changes.
//
// STATES. Off: the hooks and the pool are the game's. On: version 7 is in, with the spells its data
// block names. Outdated: version 6 (power-attack-test1: the five without Sorcerer's Blood), version 5
// (kromlech-spells-test1: the four borrowed spells), or Xantam's Fire alone as the fourth or third test
// version (test4: in the pool; test3: a 12 KB spell area copied into low memory at start-up); saving takes
// it out and puts version 7 in, every spell it had with its settings. Unrecognized: anything else (earlier
// test versions, or another patch in those places).
//
// SETTINGS (the data block; the code reads them while the game runs): each spell's level-up price
// (0-30, priced as the game prices its own feats: from 5 on a rank costs price + rank^1.6, so 16 gives
// 16, 17, 19, 21 and 25; below 5, (rank + 1) x price; 0 makes every rank free), the mana a cast takes (not
// Power Attack's: its record's; Sorcerer's Blood has none), and two values or one by rank: Xantam's Fire
// and Blade Barrier the least and most damage of a burn or a hit, Power Attack the hits it doubles,
// Antimagic Field its seconds, Righteous Might the percentage more damage his weapon hits do and the
// percentage less weapon hits on him do. Sorcerer's Blood has the number of ranks its entry offers (1-5;
// each adds 1 mana a level) in their place.
//
// THE GAME'S FILES. GTEXT.LMP needs each borrowed spell's name and description (and two HUD lines;
// BorrowedSpellTexts, in the five languages), or the level-up screen and the HUD show NF:<name>_name.
// ICONS.LMP can take their HUD icons (BorrowedSpellIcons); without one the HUD shows a game icon in its
// place. Power Attack's and Sorcerer's Blood's texts (and Power Attack's icon) are the game's own: neither
// file changes for them.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Text;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The spells the patch can give the heroes, in the order the data block lists them.</summary>
public enum BorrowedSpell
{
    XantamFire,
    PowerAttack,
    BladeBarrier,
    AntimagicField,
    RighteousMight,
    SorcerersBlood
}

public enum BorrowedSpellsState
{
    /// <summary>The game's code and the free pool: the spells can be put in.</summary>
    Off,
    /// <summary>Version 7 is in: which spells, and their settings, can be read and changed.</summary>
    On,
    /// <summary>Version 6 (the five without Sorcerer's Blood) or 5 (the four borrowed spells), or Xantam's Fire alone as the fourth or third test version: saving brings it to version 7.</summary>
    Outdated,
    /// <summary>Neither: an earlier test version, or another patch where it goes.</summary>
    Unrecognized
}

/// <summary>
/// What a spell is (its name, its hero, where his or her level-up list has it, its feat bit and cast type), what its settings
/// by rank mean and the values they may take, and its defaults (what featpatch.py writes). <paramref name="GameFeat"/>: the
/// game's own feat (Power Attack, Sorcerer's Blood), whose ability record (and so its mana), icon and texts are the game's.
/// <paramref name="Passive"/>: a passive feat (Sorcerer's Blood), no cast and no values by rank; the number of ranks its entry
/// offers is its setting instead.
/// </summary>
public sealed record BorrowedSpellInfo(
    BorrowedSpell Spell, string Title, string Hero, string After, int Bit, int CastType,
    string Values, string FirstName, int FirstMin, int FirstMax, string? SecondName, int SecondMin, int SecondMax,
    bool Range, BorrowedSpellSettings Default, bool GameFeat = false, bool Passive = false)
{
    /// <summary>The spell has two values by rank (damage least and most, or more and less).</summary>
    public bool HasSecond => SecondName != null;

    /// <summary>The spell has values by rank (every one but Sorcerer's Blood).</summary>
    public bool HasValues => !Passive;

    /// <summary>The patch writes the mana a cast takes (not for Power Attack, whose record's mana the Feats &amp; Spells tab edits, nor a passive).</summary>
    public bool HasMana => !GameFeat;
}

/// <summary>
/// One spell's settings: the level-up price byte, the mana a cast takes (0 for Power Attack: its record's; 0 for Sorcerer's
/// Blood: a passive), and its values by rank (ranks 1-5): Xantam's Fire and Blade Barrier the least (<see cref="First"/>) and
/// most (<see cref="Second"/>) damage, Power Attack the hits it doubles and Antimagic Field the seconds (<see cref="First"/>;
/// <see cref="Second"/> empty), Righteous Might the percentage more damage (<see cref="First"/>) and less damage taken
/// (<see cref="Second"/>); Sorcerer's Blood none (both empty). <see cref="RankCount"/>: the ranks its list entry offers, 5 for
/// every spell but Sorcerer's Blood, whose setting it is (1-5; each rank adds 1 mana a level).
/// </summary>
public sealed record BorrowedSpellSettings(int Price, float Mana, IReadOnlyList<int> First, IReadOnlyList<int> Second,
                                           int RankCount = 5)
{
    public const int Ranks = 5, MaxPrice = 30, MaxRankCount = 5;
    public const float MaxMana = 100f;

    /// <summary>
    /// What the next rank costs on the level-up screen when the hero holds <paramref name="held"/> ranks (0-4), as the game prices
    /// its feats (FUN_0017C620): a price below 5 costs (held + 1) x price, from 5 on price + held^1.6.
    /// </summary>
    public static int RankCost(int price, int held) => price < 5 ? (held + 1) * price : price + (int)MathF.Pow(held, 1.6f);

    /// <summary>Why these can't be written for <paramref name="spell"/>, or null.</summary>
    public string? Problem(BorrowedSpell spell)
    {
        var info = BorrowedSpells.Info(spell);
        if (Price is < 0 or > MaxPrice) return $"{info.Title}: the price is 0 to {MaxPrice} (0 makes every rank free).";
        if (info.HasMana && (!float.IsFinite(Mana) || Mana <= 0f || Mana > MaxMana))
            return $"{info.Title}: the mana a cast takes is above 0 and at most {MaxMana:0}.";
        if (!info.HasMana && Mana != 0f)
            return info.Passive
                ? $"{info.Title}: a passive feat takes no mana (0)."
                : $"{info.Title}: its mana is its own ability record's (the Feats & Spells tab), so it has none here (0).";
        if (info.Passive)
        {
            if (RankCount is < 1 or > MaxRankCount) return $"{info.Title}: its level-up entry offers 1 to {MaxRankCount} ranks.";
            if (First is null || First.Count != 0 || Second is null || Second.Count != 0)
                return $"{info.Title}: it has no values by rank.";
            return null;
        }

        if (RankCount != Ranks) return $"{info.Title}: its level-up entry offers {Ranks} ranks.";
        if (First is null || First.Count != Ranks || Second is null || Second.Count != (info.HasSecond ? Ranks : 0))
            return $"{info.Title}: {info.Values} is given for each of the {Ranks} ranks.";
        for (var r = 0; r < Ranks; r++)
        {
            if (First[r] < info.FirstMin || First[r] > info.FirstMax)
                return $"{info.Title}, rank {r + 1}: {info.FirstName} is {info.FirstMin} to {info.FirstMax}.";
            if (!info.HasSecond) continue;
            if (Second[r] < info.SecondMin || Second[r] > info.SecondMax)
                return $"{info.Title}, rank {r + 1}: {info.SecondName} is {info.SecondMin} to {info.SecondMax}.";
            if (info.Range && First[r] > Second[r]) return $"{info.Title}, rank {r + 1}: the least damage is more than the most.";
        }

        return null;
    }

    public bool Equals(BorrowedSpellSettings? other) =>
        other is not null && Price == other.Price && Mana.Equals(other.Mana) && RankCount == other.RankCount &&
        First.SequenceEqual(other.First) && Second.SequenceEqual(other.Second);

    public override int GetHashCode() => HashCode.Combine(Price, Mana, First.Count, Second.Count, RankCount);
}

/// <summary>The six spells: what each is, its defaults (what featpatch.py writes) and the test files' settings.</summary>
public static class BorrowedSpells
{
    /// <summary>Every spell, in the order of <see cref="BorrowedSpell"/> (the data block's).</summary>
    public static IReadOnlyList<BorrowedSpell> All { get; } =
        new[]
        {
            BorrowedSpell.XantamFire, BorrowedSpell.PowerAttack, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField,
            BorrowedSpell.RighteousMight, BorrowedSpell.SorcerersBlood
        };

    private static readonly BorrowedSpellInfo[] Infos =
    {
        new(BorrowedSpell.XantamFire, "Xantam's Fire", "Adrianna", "Targetting", 26, 10,
            "the damage of each burn", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(16, 15f, new[] { 1, 1, 2, 2, 3 }, new[] { 2, 3, 4, 5, 6 })),
        new(BorrowedSpell.PowerAttack, "Power Attack", "Kromlech", "his own feats", 34, 18,
            "the number of hits it doubles", "the number of hits doubled", 1, 20, null, 0, 0, false,
            new BorrowedSpellSettings(5, 0f, new[] { 1, 2, 3, 4, 5 }, Array.Empty<int>()), GameFeat: true),
        new(BorrowedSpell.BladeBarrier, "Blade Barrier", "Kromlech", "his own feats", 43, 32,
            "the damage of each hit", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(10, 15f, new[] { 4, 6, 8, 10, 12 }, new[] { 8, 10, 13, 16, 20 })),
        new(BorrowedSpell.AntimagicField, "Antimagic Field", "Kromlech", "his own feats", 44, 33,
            "how long it lasts", "the time in seconds", 1, 60, null, 0, 0, false,
            new BorrowedSpellSettings(8, 10f, new[] { 4, 5, 6, 7, 8 }, Array.Empty<int>())),
        new(BorrowedSpell.RighteousMight, "Righteous Might", "Kromlech", "his own feats", 45, 34,
            "the percentages", "the percentage more damage", 0, 200, "the percentage less damage taken", 0, 90, false,
            new BorrowedSpellSettings(12, 15f, new[] { 10, 20, 30, 40, 50 }, new[] { 10, 20, 30, 40, 50 })),
        new(BorrowedSpell.SorcerersBlood, "Sorcerer's Blood", "Adrianna", "her own feats and Xantam's Fire", 32, 24,
            "nothing", "", 0, 0, null, 0, 0, false,
            new BorrowedSpellSettings(3, 0f, Array.Empty<int>(), Array.Empty<int>(), RankCount: 1), GameFeat: true, Passive: true)
    };

    public static BorrowedSpellInfo Info(BorrowedSpell spell) => Infos[(int)spell];

    /// <summary>The test files' settings: every rank free, 4 mana a cast (Power Attack: its record's; Sorcerer's Blood: none), the default values.</summary>
    public static BorrowedSpellSettings TestSettings(BorrowedSpell spell) =>
        Info(spell).Default with { Price = 0, Mana = Info(spell).HasMana ? 4f : 0f };

    internal static SpellPatchData Data(BorrowedSpell spell) => FeatPatch.Spells[(int)spell];

    internal static int Mask(BorrowedSpell spell) => Data(spell).Mask;
}

/// <summary>
/// The spells' texts in GTEXT.LMP: the level-up screen's names and descriptions, the HUD's names and two of its lines. Power
/// Attack and Sorcerer's Blood have none here: their texts (powerattack_name, powerattack_desc, PowerSpellDamageWeapon;
/// SorcerersBlood, SorcerersBloodDesc) are the game's own, never added or taken out.
/// </summary>
public static class BorrowedSpellTexts
{
    /// <summary>The keys and texts of <paramref name="spell"/> in each language (0 English ... 4 Spanish): what gtext_feats5.py writes (none for Power Attack and Sorcerer's Blood).</summary>
    public static IReadOnlyList<(int Language, string Key, string Text)> Of(BorrowedSpell spell)
    {
        var key = BorrowedSpells.Data(spell).Key;
        return FeatPatch.Texts.Where(t => t.Spell == key).Select(t => (t.Language, t.Key, t.Text)).ToList();
    }

    /// <summary>The key of the spell's name ("bladebarrier_name"; "SorcerersBlood"): its list entry's, what the game shows when it is missing, after "NF:".</summary>
    public static string NameKey(BorrowedSpell spell) =>
        Encoding.ASCII.GetString(Convert.FromBase64String(BorrowedSpells.Data(spell).EntryBase64), 8, 0x20).TrimEnd('\0');

    /// <summary>True when <paramref name="text"/> has every one of the spell's texts as written here.</summary>
    public static bool HasNames(GameText text, BorrowedSpell spell) => Of(spell).All(n => text.Get(n.Key, n.Language) == n.Text);

    /// <summary>True when <paramref name="text"/> has any of the spell's keys.</summary>
    public static bool HasAnyName(GameText text, BorrowedSpell spell) => Of(spell).Any(n => text.Get(n.Key, n.Language) != null);

    /// <summary>
    /// Puts the texts of <paramref name="spells"/> in <paramref name="text"/>, spell by spell in the order of
    /// <see cref="BorrowedSpells.All"/> (a key it has already is changed in place, a new one goes at the end).
    /// </summary>
    public static void AddNames(GameText text, IEnumerable<BorrowedSpell> spells)
    {
        var set = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Where(set.Contains))
        foreach (var (language, key, name) in Of(spell))
            text.SetLanguageText(language, key, name);
    }

    /// <summary>Takes the keys of <paramref name="spells"/> out of <paramref name="text"/>.</summary>
    public static void RemoveNames(GameText text, IEnumerable<BorrowedSpell> spells)
    {
        foreach (var spell in spells.Distinct())
        foreach (var (language, key, _) in Of(spell))
            text.RemoveLanguageText(language, key);
    }
}

/// <summary>
/// The spells' HUD icons in ICONS.LMP (icon_xantamfire.tex, icon_bladebarrier.tex, icon_antimagicfield.tex,
/// icon_righteousmight.tex). Power Attack's, icon_powerattack.tex, is the game's own: never added or taken out. Sorcerer's
/// Blood, a passive feat, has no HUD slot and no icon.
/// </summary>
public static class BorrowedSpellIcons
{
    public static string EntryName(BorrowedSpell spell) => BorrowedSpells.Data(spell).IconName;

    /// <summary>The spell brings an icon of its own (every one but Power Attack, whose icon the game has, and Sorcerer's Blood, which needs none).</summary>
    public static bool HasOwnIcon(BorrowedSpell spell) => !BorrowedSpells.Info(spell).GameFeat;

    /// <summary>The texture: 44 x 44, 8-bit with 256 colours, in the layout of the game's spell icons (empty for Power Attack and Sorcerer's Blood).</summary>
    public static byte[] Texture(BorrowedSpell spell) => Convert.FromBase64String(BorrowedSpells.Data(spell).IconBase64);

    /// <summary>The game's icon the HUD shows when the spell's own isn't in ICONS.LMP.</summary>
    public static string FallbackName(BorrowedSpell spell) => spell switch
    {
        BorrowedSpell.XantamFire => "icon_protection.tex (Fire Shield's)",
        BorrowedSpell.PowerAttack => "icon_powerattack.tex (its own, the game's)",
        BorrowedSpell.BladeBarrier => "icon_whirlwind.tex (Whirlwind Attack's)",
        BorrowedSpell.AntimagicField => "icon_protection.tex (Fire Shield's)",
        BorrowedSpell.SorcerersBlood => "none (a passive feat has no HUD slot)",
        _ => "icon_powerattack.tex (Power Attack's)"
    };

    /// <summary>Why <paramref name="icons"/> isn't an ICONS.LMP the icons can go in or come out of, or null.</summary>
    public static string? Problem(byte[] icons)
    {
        List<GameArchive.Entry> entries;
        try
        {
            entries = GameArchive.ReadDirectory(icons);
        }
        catch (InvalidDataException e)
        {
            return e.Message;
        }

        if (!entries.Any(e => e.Name == "icon_protection.tex") || !entries.Any(e => e.Name == "icon_healthpotion.tex"))
            return "It isn't ICONS.LMP: it has no icon_protection.tex and icon_healthpotion.tex (the spell icons).";
        return GameArchive.Problem(icons);
    }

    /// <summary>True when <paramref name="icons"/> has the spell's icon.</summary>
    public static bool IsIn(byte[] icons, BorrowedSpell spell) => GameArchive.ReadDirectory(icons).Any(e => e.Name == EntryName(spell));

    /// <summary>
    /// ICONS.LMP with the icons of <paramref name="spells"/> added where the game finds them (where each name sorts; when the
    /// directory is full every file first moves on by 0x80): icons_lmp5.py's result, byte for byte. An icon that is in already
    /// is left as it is, and Power Attack and Sorcerer's Blood bring none (the game's is used; a passive needs none).
    /// </summary>
    public static byte[] AddTo(byte[] icons, IEnumerable<BorrowedSpell> spells)
    {
        if (Problem(icons) is { } problem) throw new InvalidDataException(problem);
        var set = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Where(s => set.Contains(s) && HasOwnIcon(s)))
        {
            if (!IsIn(icons, spell)) icons = GameArchive.Insert(icons, EntryName(spell), Texture(spell), growDirectory: true);
        }

        return icons;
    }

    /// <summary>
    /// ICONS.LMP without the icons of <paramref name="spells"/>, its directory back to its smallest size: as it was before they went
    /// in. The game's own icon_powerattack.tex always stays.
    /// </summary>
    public static byte[] RemoveFrom(byte[] icons, IEnumerable<BorrowedSpell> spells)
    {
        if (Problem(icons) is { } problem) throw new InvalidDataException(problem);
        var set = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Reverse().Where(s => set.Contains(s) && HasOwnIcon(s)))
        {
            if (IsIn(icons, spell)) icons = GameArchive.Remove(icons, EntryName(spell), shrinkDirectory: true);
        }

        return icons;
    }
}

public partial class BgdaExecutable
{
    private static readonly byte[] FeatMark = "FEAT"u8.ToArray();

    private static readonly Lazy<(byte[] Pool, (int Va, byte[] Bytes)[] Code, byte[] Data, byte[] V4Code)> FeatPatchData =
        new(LoadFeatPatchData);

    private static (byte[] Pool, (int Va, byte[] Bytes)[] Code, byte[] Data, byte[] V4Code) LoadFeatPatchData()
    {
        using var packed = new MemoryStream(Convert.FromBase64String(FeatPatch.PoolOriginalBase64));
        using var unpack = new ZLibStream(packed, CompressionMode.Decompress);
        using var pool = new MemoryStream();
        unpack.CopyTo(pool);
        var bytes = pool.ToArray();
        if (bytes.Length != FeatPatch.PoolBytes ||
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != FeatPatch.PoolOriginalSha256)
            throw new InvalidDataException("The built-in feats patch data is damaged.");
        var code = FeatPatch.Code.Select(c => (c.Va, Convert.FromBase64String(c.Base64))).ToArray();
        var data = Convert.FromBase64String(FeatPatch.DataCommonBase64);
        var v4 = Convert.FromBase64String(FeatPatch.V4CodeBase64);
        if (data.Length != FeatPatch.DataSize || v4.Length != FeatPatch.V4CodeEndVa - FeatPatch.V4CodeVa)
            throw new InvalidDataException("The built-in feats patch data is damaged.");
        return (bytes, code, data, v4);
    }

    /// <summary>The end of everything the patch reads or writes in the game's part of the file (a shorter file isn't the game's).</summary>
    private static readonly Lazy<int> FeatEndVa = new(() =>
        new[] { FeatPatch.Pool[^1].End, FeatPatch.AbilityTableEndVa + 4, FeatPatch.GameCastTableVa + 4 * FeatPatch.GameCastTypes }
            .Concat(FeatPatch.Checks.Select(c => c.Va + 4))
            .Concat(FeatPatch.Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V6Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V6Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V6VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V5Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V5Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V5VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Append(FeatPatch.SorcerersRecordVa + 0x50)
            .Concat(FeatPatch.V4Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.Strings.Select(s => s.Va + s.Text.Length + 1))
            .Concat(FeatPatch.VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Max());

    /// <summary>How much game code nothing runs there is for new feats (the pool), and in how many stretches.</summary>
    public static int FeatPoolBytes => FeatPatch.PoolBytes;

    public static int FeatPoolStretches => FeatPatch.Pool.Length;

    /// <summary>How much of the pool the borrowed spells take (any of them: the code is the same), and so how much is left.</summary>
    public static int BorrowedSpellsBytes => FeatPatch.PoolUsedBytes;

    /// <summary>The game's bytes at <paramref name="start"/>-<paramref name="end"/>, inside one stretch of the pool.</summary>
    private static ReadOnlySpan<byte> PoolOriginal(int start, int end)
    {
        var at = 0;
        foreach (var (a, b) in FeatPatch.Pool)
        {
            if (a <= start && end <= b) return FeatPatchData.Value.Pool.AsSpan(at + start - a, end - start);
            at += b - a;
        }

        throw new ArgumentOutOfRangeException(nameof(start), "Not inside one stretch of the pool.");
    }

    private bool BytesAre(int va, ReadOnlySpan<byte> bytes) =>
        _data.Length >= FileOffset(va) + bytes.Length && _data.AsSpan(FileOffset(va), bytes.Length).SequenceEqual(bytes);

    private int FeatDataAt(int offset) => FileOffset(FeatPatch.DataVa) + offset;

    private bool PoolIsGame() => FeatPatch.Pool.All(s => BytesAre(s.Start, PoolOriginal(s.Start, s.End)));

    /// <summary>A vtable copy as the patch writes it: the game's vtable as this file has it, with the patch's words.</summary>
    private byte[] FeatVTable((int Va, int GameVa, (int At, uint Word)[] Words) vt)
    {
        var image = _data.AsSpan(FileOffset(vt.GameVa), FeatPatch.VTableSize).ToArray();
        foreach (var (at, word) in vt.Words) BitConverter.GetBytes(word).CopyTo(image, at);
        return image;
    }

    private static int FeatMask(IEnumerable<BorrowedSpell> spells) => spells.Aggregate(0, (m, s) => m | BorrowedSpells.Mask(s));

    private static List<BorrowedSpell> SpellsOf(int mask) => BorrowedSpells.All.Where(s => (mask & BorrowedSpells.Mask(s)) != 0).ToList();

    private static string Names(IReadOnlyList<BorrowedSpell> spells)
    {
        var names = spells.Select(s => BorrowedSpells.Info(s).Title).ToList();
        return names.Count < 2 ? string.Join("", names) : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];
    }

    /// <summary>The spells the data block says are in when it has the mark of <paramref name="version"/>, or null.</summary>
    private List<BorrowedSpell>? MarkedSpells(int version)
    {
        if (!BytesAre(FeatPatch.DataVa, FeatMark) || ReadU32(FeatDataAt(FeatPatch.VersionAt)) != version) return null;
        return SpellsOf((int)ReadU32(FeatDataAt(FeatPatch.SpellsAt)));
    }

    /// <summary>The spells version 7 says it has (its data block's mask), or null when the file doesn't have version 7's mark.</summary>
    private List<BorrowedSpell>? CurrentSpells() => MarkedSpells(FeatPatch.Version);

    /// <summary>
    /// An earlier version as WorldExplorer or featpatch.py wrote it: its mark, a mask of its own spells (none of
    /// <paramref name="without"/>, which it didn't have), their hooks (and only theirs), its code (the SHA-256 of its pieces) and
    /// its vtable copies. Null when it isn't.
    /// </summary>
    private List<BorrowedSpell>? EarlierSpells(int version, BorrowedSpell[] without, (int Va, uint Game, uint Patched, int Spells)[] hooks,
                                               (int Va, int Length)[] code, string sha256,
                                               (int Va, int GameVa, (int At, uint Word)[] Words)[] vtables)
    {
        if (MarkedSpells(version) is not { } spells) return null;
        var mask = (int)ReadU32(FeatDataAt(FeatPatch.SpellsAt));
        if (spells.Count == 0 || spells.Any(without.Contains) || mask != FeatMask(spells)) return null;
        if (!hooks.All(h => ReadU32(FileOffset(h.Va)) == ((h.Spells & mask) != 0 ? h.Patched : h.Game))) return null;
        if (!vtables.All(v => BytesAre(v.Va, FeatVTable(v)))) return null;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (va, length) in code)
        {
            if (_data.Length < FileOffset(va) + length) return null;
            sha.AppendData(_data, FileOffset(va), length);
        }

        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant() == sha256 ? spells : null;
    }

    /// <summary>Version 6 (the five spells, without Sorcerer's Blood): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V6Spells() =>
        EarlierSpells(FeatPatch.V6Version, new[] { BorrowedSpell.SorcerersBlood }, FeatPatch.V6Hooks, FeatPatch.V6Code,
                      FeatPatch.V6CodeSha256, FeatPatch.V6VTables);

    /// <summary>Version 5 (the four borrowed spells, without Power Attack): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V5Spells() =>
        EarlierSpells(FeatPatch.V5Version, new[] { BorrowedSpell.PowerAttack, BorrowedSpell.SorcerersBlood }, FeatPatch.V5Hooks,
                      FeatPatch.V5Code, FeatPatch.V5CodeSha256, FeatPatch.V5VTables);

    /// <summary>
    /// Off, On, Outdated (version 6 or 5, or Xantam's Fire alone as the fourth or third test version: updated on save) or
    /// Unrecognized; <paramref name="detail"/> says what was found, in a sentence.
    /// </summary>
    public BorrowedSpellsState GetBorrowedSpellsState(out string detail)
    {
        if (_data.Length < FileOffset(FeatEndVa.Value))
        {
            detail = "The file is too short to be this game's executable.";
            return BorrowedSpellsState.Unrecognized;
        }

        var (_, code, _, v4Code) = FeatPatchData.Value;
        if (CurrentSpells() is { } spells)
        {
            var mask = FeatMask(spells);
            var hooks = FeatPatch.Hooks.All(h => ReadU32(FileOffset(h.Va)) == ((h.Spells & mask) != 0 ? h.Patched : h.Game));
            if (spells.Count > 0 && hooks && code.All(c => BytesAre(c.Va, c.Bytes)) && FeatPatch.VTables.All(v => BytesAre(v.Va, FeatVTable(v))))
            {
                detail = $"In (version 7, in game code nothing runs): {Names(spells)}. It works with or without three players.";
                return BorrowedSpellsState.On;
            }

            detail = "The file has the borrowed spells' data block, but its code or hooks aren't what WorldExplorer writes.";
            return BorrowedSpellsState.Unrecognized;
        }

        if (V6Spells() is { } six)
        {
            detail = $"In as version 6 (the first test of Power Attack): {Names(six)}. Saving brings it to version 7, which adds " +
                     "Sorcerer's Blood for Adrianna; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V5Spells() is { } old)
        {
            detail = $"In as version 5 (the first test of Kromlech's spells): {Names(old)}. Saving brings it to version 7, which adds " +
                     "Power Attack for Kromlech and Sorcerer's Blood for Adrianna; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V4Present(v4Code))
        {
            detail = "Xantam's Fire is in as the fourth test version (Adrianna's spell alone). Saving brings it to version 7, which " +
                     "also has Sorcerer's Blood and Kromlech's spells; its price, mana and damage are kept.";
            return BorrowedSpellsState.Outdated;
        }

        if (V3Present())
        {
            detail = "Xantam's Fire is in as the third test version (its spell area, which only a three-player build loads). " +
                     "Saving brings it to version 7, which works with or without three players; its price, mana and damage are kept.";
            return BorrowedSpellsState.Outdated;
        }

        var other = FeatPatch.Hooks.Select(h => (h.Va, h.Game)).Concat(FeatPatch.V4Hooks.Select(h => (h.Va, h.Game)))
            .FirstOrDefault(h => ReadU32(FileOffset(h.Va)) != h.Game);
        if (other.Va != 0)
        {
            detail = $"0x{other.Va:X8}, where the borrowed spells hook in, isn't the game's code: an earlier test version (take it " +
                     "out with featpatch.py --remove) or another patch.";
            return BorrowedSpellsState.Unrecognized;
        }

        if (BorrowedSpellsProblem() is { } problem)
        {
            detail = problem;
            return BorrowedSpellsState.Unrecognized;
        }

        detail = "No borrowed spells in: they can be put in, with or without three players.";
        return BorrowedSpellsState.Off;
    }

    public BorrowedSpellsState GetBorrowedSpellsState() => GetBorrowedSpellsState(out _);

    /// <summary>Why the patch can't go in this file (its hooks being the game's), or null.</summary>
    private string? BorrowedSpellsProblem()
    {
        foreach (var (va, word) in FeatPatch.Checks)
        {
            if (ReadU32(FileOffset(va)) != word) return $"0x{va:X8} isn't the game's code, which the borrowed spells rely on.";
        }

        if (ReadU32(FileOffset(FeatPatch.MeleeCallVa)) >> 26 != 3)
            return $"0x{FeatPatch.MeleeCallVa:X8} (the hero's melee hit) isn't a call, the game's or Corrosive's.";
        foreach (var (va, text) in FeatPatch.Strings)
        {
            if (!BytesAre(va, Encoding.ASCII.GetBytes(text + "\0"))) return $"\"{text}\" isn't where the game keeps it.";
        }

        if (!BytesAre(FeatPatch.AbilityTableEndVa, "END\0"u8))
            return "The ability table doesn't end where the game's does.";

        // the game's Power Attack and Sorcerer's Blood: their names, cast types and bits (Power Attack's mana, damage and
        // colours are the Feats & Spells tab's)
        foreach (var (spell, recordVa, name) in new[]
                 {
                     (BorrowedSpell.PowerAttack, FeatPatch.PowerRecordVa, FeatPatch.PowerRecordName),
                     (BorrowedSpell.SorcerersBlood, FeatPatch.SorcerersRecordVa, FeatPatch.SorcerersRecordName)
                 })
        {
            var record = FileOffset(recordVa);
            var info = BorrowedSpells.Info(spell);
            if (!BytesAre(recordVa, Encoding.ASCII.GetBytes(name + "\0")) ||
                BitConverter.ToInt16(_data, record + 0x20) != info.CastType || ReadU64(record + 0x48) != 1UL << info.Bit)
                return $"The game's {info.Title} record (0x{recordVa:X8}) isn't the game's any more.";
        }

        for (var va = FeatPatch.AbilityTableVa; va < FeatPatch.AbilityTableEndVa; va += 0x50)
        {
            var at = FileOffset(va);
            foreach (var spell in BorrowedSpells.All)
            {
                var info = BorrowedSpells.Info(spell);
                if (info.GameFeat) continue;            // Power Attack, Sorcerer's Blood: the game's records are the ones they use
                if ((ReadU64(at + 0x48) & 1UL << info.Bit) != 0 || BitConverter.ToInt16(_data, at + 0x20) == info.CastType)
                    return $"An ability already uses bit {info.Bit} or cast type {info.CastType}, which {info.Title} needs.";
            }

            var sb = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood);
            if (va != FeatPatch.SorcerersRecordVa && (ReadU64(at + 0x48) & 1UL << sb.Bit) != 0)
                return $"Another ability (0x{va:X8}) has bit {sb.Bit}, Sorcerer's Blood's.";
        }

        // the game's own segment loads the pool (the first program header: type 1, file 0x1000, address 0x00100000)
        var ph = (int)ReadU32(0x1C);
        if (ph <= 0 || ph + 32 > _data.Length || ReadU32(ph) != 1 || ReadU32(ph + 4) != 0x1000 || ReadU32(ph + 8) != 0x00100000 ||
            ReadU32(ph + 4) + ReadU32(ph + 16) < (uint)FileOffset(FeatPatch.Pool[^1].End))
            return "The executable's first program header isn't the game's.";
        var lui = ReadU32(FileOffset(FeatPatch.ListsLuiVa));
        var addiu = ReadU32(FileOffset(FeatPatch.ListsAddiuVa));
        var lists = (int)((lui & 0xFFFF) << 16) + (short)(addiu & 0xFFFF);
        if (lui >> 16 != 0x3C03 || addiu >> 16 != 0x246A || lists is not (0x002A0478 or 0x000D0100))
            return "FUN_00162B78, which builds the level-up lists, isn't the game's or the three-player builds'.";
        if (!PoolIsGame())
            return "The unused game code where the borrowed spells go holds something else: another patch uses it.";
        return null;
    }

    /// <summary>The fourth test version: its "FEAT" version 4 mark in the data block, its six hooks and its code.</summary>
    private bool V4Present(byte[] v4Code) =>
        BytesAre(FeatPatch.DataVa, FeatMark) && ReadU32(FeatDataAt(FeatPatch.VersionAt)) == 4 &&
        FeatPatch.V4Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Patched) && BytesAre(FeatPatch.V4CodeVa, v4Code);

    /// <summary>The third test version: its "FEAT" version 3 mark in the spell area's bytes and its seven hooks.</summary>
    private bool V3Present() =>
        _data.Length >= FeatPatch.V3AreaFile + FeatPatch.V3AreaSize &&
        _data.AsSpan(FeatPatch.V3AreaFile, 4).SequenceEqual(FeatMark) && ReadU32(FeatPatch.V3AreaFile + 4) == 3 &&
        FeatPatch.V3Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Patched);

    /// <summary>Where a version's data block keeps the settings (version 7's, 6's and 5's entries are where the others' are).</summary>
    private sealed record FeatLayout(int Entries, int Abilities, int BarrierMin, int BarrierMax, int FieldFrames, int MightDamage,
                                     int MightDefence, int PowerHits);

    private static readonly FeatLayout CurrentLayout = new(FeatPatch.EntriesAt, FeatPatch.AbilitiesAt, FeatPatch.BarrierDamageMinAt,
        FeatPatch.BarrierDamageMaxAt, FeatPatch.FieldFramesAt, FeatPatch.MightDamageAt, FeatPatch.MightDefenceAt, FeatPatch.PowerHitsAt);

    private static readonly FeatLayout V6Layout = new(FeatPatch.V6EntriesAt, FeatPatch.V6AbilitiesAt, FeatPatch.V6BarrierDamageMinAt,
        FeatPatch.V6BarrierDamageMaxAt, FeatPatch.V6FieldFramesAt, FeatPatch.V6MightDamageAt, FeatPatch.V6MightDefenceAt,
        FeatPatch.V6PowerHitsAt);

    private static readonly FeatLayout V5Layout = new(FeatPatch.V5EntriesAt, FeatPatch.V5AbilitiesAt, FeatPatch.V5BarrierDamageMinAt,
        FeatPatch.V5BarrierDamageMaxAt, FeatPatch.V5FieldFramesAt, FeatPatch.V5MightDamageAt, FeatPatch.V5MightDefenceAt, -1);

    /// <summary>
    /// The spells in the file and their settings (On; Outdated: version 6's or 5's spells, or Xantam's Fire's), or none (Off,
    /// Unrecognized). Power Attack's mana is 0: its record's is the Feats &amp; Spells tab's. Sorcerer's Blood's is 0 too: it
    /// is passive.
    /// </summary>
    public IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> GetBorrowedSpells()
    {
        var found = new Dictionary<BorrowedSpell, BorrowedSpellSettings>();
        var state = GetBorrowedSpellsState(out _);
        if (state == BorrowedSpellsState.Outdated && V6Spells() is { } six)
            return ReadAll(six, V6Layout);
        if (state == BorrowedSpellsState.Outdated && V5Spells() is { } old)
            return ReadAll(old, V5Layout);       // every spell had its own ability record, in the order of the entries

        if (state == BorrowedSpellsState.Outdated)
        {
            var block = V3Present() ? FeatPatch.V3AreaFile : FeatDataAt(0);        // the same layout in both
            found[BorrowedSpell.XantamFire] = new BorrowedSpellSettings(
                _data[block + FeatPatch.V4PriceAt], BitConverter.ToSingle(_data, block + FeatPatch.V4ManaAt),
                ByRank(block + FeatPatch.V4DamageMinAt), ByRank(block + FeatPatch.V4DamageMaxAt));
            return found;
        }

        return state == BorrowedSpellsState.On ? ReadAll(CurrentSpells()!, CurrentLayout) : found;
    }

    /// <summary>The settings of <paramref name="spells"/> (in the data block's order) where <paramref name="layout"/> keeps them.</summary>
    private Dictionary<BorrowedSpell, BorrowedSpellSettings> ReadAll(List<BorrowedSpell> spells, FeatLayout layout)
    {
        var found = new Dictionary<BorrowedSpell, BorrowedSpellSettings>();
        var own = 0;
        for (var i = 0; i < spells.Count; i++)
        {
            var spell = spells[i];
            var record = BorrowedSpells.Data(spell).OwnRecord ? own++ : -1;
            found[spell] = ReadSettings(spell, layout.Entries + FeatPatch.EntryStride * i,
                                        record < 0 ? -1 : layout.Abilities + FeatPatch.AbilityStride * record, layout);
        }

        return found;
    }

    /// <summary>
    /// One spell's settings from the data block: the price (and Sorcerer's Blood's ranks) from its entry (at
    /// <paramref name="entry"/>), the mana from its ability record (at <paramref name="ability"/>; -1: none, Power Attack and
    /// Sorcerer's Blood), its values by rank where <paramref name="layout"/>'s version keeps them.
    /// </summary>
    private BorrowedSpellSettings ReadSettings(BorrowedSpell spell, int entry, int ability, FeatLayout layout)
    {
        var price = _data[FeatDataAt(entry + FeatPatch.EntryPriceAt)];
        var mana = ability < 0 ? 0f : BitConverter.ToSingle(_data, FeatDataAt(ability + FeatPatch.AbilityManaAt));
        return spell switch
        {
            BorrowedSpell.XantamFire => new BorrowedSpellSettings(price, mana, ByRank(FeatDataAt(FeatPatch.XantamDamageMinAt)),
                                                                  ByRank(FeatDataAt(FeatPatch.XantamDamageMaxAt))),
            BorrowedSpell.PowerAttack => new BorrowedSpellSettings(price, 0f, ByRank(FeatDataAt(layout.PowerHits)), Array.Empty<int>()),
            BorrowedSpell.BladeBarrier => new BorrowedSpellSettings(price, mana, ByRank(FeatDataAt(layout.BarrierMin)),
                                                                    ByRank(FeatDataAt(layout.BarrierMax))),
            BorrowedSpell.AntimagicField => new BorrowedSpellSettings(price, mana,
                Enumerable.Range(1, BorrowedSpellSettings.Ranks).Select(r => ReadU16(FeatDataAt(layout.FieldFrames + 2 * r)) / 50).ToArray(),
                Array.Empty<int>()),
            BorrowedSpell.SorcerersBlood => new BorrowedSpellSettings(price, 0f, Array.Empty<int>(), Array.Empty<int>(),
                                                                      _data[FeatDataAt(entry + FeatPatch.EntryRanksAt)]),
            _ => new BorrowedSpellSettings(price, mana, ByRank(FeatDataAt(layout.MightDamage)), ByRank(FeatDataAt(layout.MightDefence)))
        };
    }

    /// <summary>Ranks 1-5 of a by-rank byte table (its entry 0 is rank 0's).</summary>
    private int[] ByRank(int at) => Enumerable.Range(1, BorrowedSpellSettings.Ranks).Select(r => (int)_data[at + r]).ToArray();

    /// <summary>
    /// Puts in, updates (Outdated: to version 7), changes or takes out the borrowed spells so that exactly
    /// <paramref name="spells"/> are in, with their settings (none: <see cref="RemoveBorrowedSpells"/>). Throws
    /// <see cref="ArgumentException"/> on bad settings and <see cref="InvalidOperationException"/> when the state is
    /// Unrecognized; nothing is changed then.
    /// </summary>
    public void SetBorrowedSpells(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        foreach (var (spell, settings) in spells)
        {
            if (settings.Problem(spell) is { } problem) throw new ArgumentException(problem, nameof(spells));
        }

        if (spells.Count == 0)
        {
            RemoveBorrowedSpells();
            return;
        }

        var state = GetBorrowedSpellsState(out var detail);
        if (state == BorrowedSpellsState.Unrecognized)
            throw new InvalidOperationException("The borrowed spells can't be put in: " + detail);
        if (state == BorrowedSpellsState.Outdated)
        {
            var copy = (byte[])_data.Clone();
            RemoveOutdated();
            if (BorrowedSpellsProblem() is { } left)
            {
                _data = copy;
                throw new InvalidOperationException("The borrowed spells can't be updated: " + left);
            }
        }

        var (_, code, _, _) = FeatPatchData.Value;
        if (state != BorrowedSpellsState.On)
        {
            foreach (var (va, bytes) in code) bytes.CopyTo(_data, FileOffset(va));
            foreach (var vt in FeatPatch.VTables) FeatVTable(vt).CopyTo(_data, FileOffset(vt.Va));
        }

        FeatDataBlock(spells).CopyTo(_data, FeatDataAt(0));
        var mask = FeatMask(spells.Keys);
        foreach (var (va, game, patched, need) in FeatPatch.Hooks) WriteInt(FileOffset(va), (int)((need & mask) != 0 ? patched : game));
    }

    /// <summary>
    /// The data block for <paramref name="spells"/>, as featpatch.py's data_block makes it (the game's cast table from this file):
    /// a list entry for each, in the order of <see cref="BorrowedSpells.All"/>, an ability record for each but Power Attack and
    /// Sorcerer's Blood, and a cast case for each but Sorcerer's Blood.
    /// </summary>
    private byte[] FeatDataBlock(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data.Clone();
        var on = BorrowedSpells.All.Where(spells.ContainsKey).ToList();
        var own = on.Count(s => BorrowedSpells.Data(s).OwnRecord);
        BitConverter.GetBytes(on.Count).CopyTo(block, FeatPatch.CountAt);
        BitConverter.GetBytes(own).CopyTo(block, FeatPatch.AbilityCountAt);
        BitConverter.GetBytes(FeatMask(on)).CopyTo(block, FeatPatch.SpellsAt);
        var table = new uint[FeatPatch.CastTypes];
        for (var t = 0; t < table.Length; t++)
            table[t] = t < FeatPatch.GameCastTypes ? ReadU32(FileOffset(FeatPatch.GameCastTableVa + 4 * t)) : FeatPatch.CastDefault;
        var record = 0;
        for (var i = 0; i < on.Count; i++)
        {
            var spell = on[i];
            var s = spells[spell];
            var data = BorrowedSpells.Data(spell);
            var entry = FeatPatch.EntriesAt + FeatPatch.EntryStride * i;
            Convert.FromBase64String(data.EntryBase64).CopyTo(block, entry);
            block[entry + FeatPatch.EntryPriceAt] = (byte)s.Price;
            block[entry + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
            if (data.HasCast) table[data.CastType - 1] = data.Cast;
            var ability = -1;
            if (data.OwnRecord)
            {
                ability = FeatPatch.AbilitiesAt + FeatPatch.AbilityStride * record++;
                Convert.FromBase64String(data.AbilityBase64).CopyTo(block, ability);
                BitConverter.GetBytes(s.Mana).CopyTo(block, ability + FeatPatch.AbilityManaAt);
            }

            switch (spell)
            {
                case BorrowedSpell.XantamFire:
                case BorrowedSpell.BladeBarrier:
                    // the ability record's damage: the HUD's line until the first cast (the cast then writes the rank's into the slot)
                    BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, ability + FeatPatch.AbilityDamageAt);
                    BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, ability + FeatPatch.AbilityDamageAt + 2);
                    var (min, max) = spell == BorrowedSpell.XantamFire
                        ? (FeatPatch.XantamDamageMinAt, FeatPatch.XantamDamageMaxAt)
                        : (FeatPatch.BarrierDamageMinAt, FeatPatch.BarrierDamageMaxAt);
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                    {
                        block[min + r + 1] = (byte)s.First[r];
                        block[max + r + 1] = (byte)s.Second[r];
                    }

                    break;
                case BorrowedSpell.PowerAttack:
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++) block[FeatPatch.PowerHitsAt + r + 1] = (byte)s.First[r];
                    break;
                case BorrowedSpell.AntimagicField:
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                        BitConverter.GetBytes((ushort)(50 * s.First[r])).CopyTo(block, FeatPatch.FieldFramesAt + 2 * (r + 1));
                    break;
                case BorrowedSpell.RighteousMight:
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                    {
                        block[FeatPatch.MightDamageAt + r + 1] = (byte)s.First[r];
                        block[FeatPatch.MightDefenceAt + r + 1] = (byte)s.Second[r];
                    }

                    break;
            }
        }

        for (var t = 0; t < table.Length; t++) BitConverter.GetBytes(table[t]).CopyTo(block, FeatPatch.CastTableAt + 4 * t);
        return block;
    }

    /// <summary>
    /// Takes the borrowed spells out (On or Outdated): the game's words back at the hooks and the game's own bytes back in the
    /// whole pool (also where a test file's tripwire is). Nothing happens when it is Off; throws
    /// <see cref="InvalidOperationException"/> when Unrecognized.
    /// </summary>
    public void RemoveBorrowedSpells()
    {
        switch (GetBorrowedSpellsState(out var detail))
        {
            case BorrowedSpellsState.Off:
                return;
            case BorrowedSpellsState.Unrecognized:
                throw new InvalidOperationException("The borrowed spells can't be taken out: " + detail);
            case BorrowedSpellsState.Outdated:
                RemoveOutdated();
                return;
        }

        foreach (var (va, game, _, _) in FeatPatch.Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    private void RestorePool()
    {
        foreach (var (a, b) in FeatPatch.Pool) PoolOriginal(a, b).CopyTo(_data.AsSpan(FileOffset(a)));
    }

    /// <summary>Takes out the earlier version that is in (Outdated): version 6 or 5, or the fourth or third test version.</summary>
    private void RemoveOutdated()
    {
        if (V6Spells() != null) RemoveV6();
        else if (V5Spells() != null) RemoveV5();
        else if (V3Present()) RemoveV3();
        else RemoveV4();
    }

    /// <summary>Takes version 6 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV6()
    {
        foreach (var (va, game, _, _) in FeatPatch.V6Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 5 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV5()
    {
        foreach (var (va, game, _, _) in FeatPatch.V5Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes the fourth test version out: the game's words back at its six hooks, the game's bytes back in the pool.</summary>
    private void RemoveV4()
    {
        foreach (var (va, game, _) in FeatPatch.V4Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes the third test version out: the game's words back at its seven hooks, its spell area's bytes zero again.</summary>
    private void RemoveV3()
    {
        foreach (var (va, game, _) in FeatPatch.V3Hooks) WriteInt(FileOffset(va), (int)game);
        Array.Clear(_data, FeatPatch.V3AreaFile, FeatPatch.V3AreaSize);
    }
}
