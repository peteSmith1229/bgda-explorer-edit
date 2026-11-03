// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Executable.Rules.cs
//
// The single numbers in the game's code that decide how kill XP is shared, how a drop is rolled, how
// much gold and item value a level is worth, which potion drops, and Borador's Ransack. Each is an
// immediate of one or two instructions, used for nothing else (checked against every path of its
// function), and no group of the three-player mod writes it:
//
// KILL XP. The general death code 0x25C2A0 (every creature but Lyran, trolls and owlbears):
//   kill = floor(floor((XP rating ÷ the players' levels added up) ^ 1.5) × 0.75), and with two or more
//   players alive each gets floor(kill × 0.6), a lone survivor all of it (0x25E104, 0x25E140,
//   0x25E178). Lyran (0x176608) and trolls / owlbears (0x1CD000) have their own: ^1.5, then half
//   each with two or more alive, all of it alone.
// giveXP 0x2B8468: +3 % for Vahn and Adrianna (classes 0, 1), +9 % for Drizzt and Artemis (3, 9),
//   +5 % per point of the equipment's XP bonus (record + 1096), together at most +100 %. The game adds
//   XP ÷ trunc(1 ÷ bonus) (9 % gives 1/11, 3 % gives 1/33).
// DROPS, the drop routine 0x264970 (the creature's own values are in Da2Executable.Loot.cs):
//   a drop chance below 1 is halved; + 1 % per point of "Increased Treasure Drops" (the better of
//   players 1 and 2); gold: 1 pile (93.1 %), 2 (4.6 %) or 3 (2.3 %), each placed with the pile chance,
//   of (40 + 0..39) × gold scale × 0.1 × the level's gold value; then a potion with twice the drop
//   chance; then with the drop chance an item of value quality factor × 200 × the level's item value ×
//   (0.8 + 0..0.4). The quality factor is 0.5, 1 or 3, rolled from the quality table (Loot.cs).
//   With two players counted (the mod keeps the count at 2 for three) gold and the drop chance are
//   doubled (add.s, fixed) and the pile chance × 1.5 (a double built by ori + dsll32, 0x264AA4).
// GOLD AND ITEM VALUE OF A LEVEL, 0x2539F0 / 0x253B28: ((monster level + act offset) × per level +
//   base) × 0.5 × scale, monster level + 60 on Extreme (+ 30 more in the act-1 levels); gold: act 1
//   L × 0.3, act 2 + 10 and act 3 + 25 (one tail), act 4 + 30; value: act 1 L × 0.2, act 2 + 10, act 3
//   + 25 and act 4 + 63 (one tail), scale 1.8.
// POTIONS, 0x257F18: tier = monster level × 0.1 (+ 0.6 / 1.2 / 2.0 in acts 2 / 3 / 4; 10 on Extreme);
//   healing (rand < 30000 of 65536), recall (rand > 59999; lesser healing in the opening levels) or
//   rejuvenation; tier + 0..0.2 up to 1 = lesser, up to 2 = normal, above = extra.
// RANSACK (Borador's feat 102), in the player's code 0x2831B8: the object drops loot with chance
//   0.1 + 0.03 × rank, gold scale 1 + 0.2 × rank, quality 1.2 × rank.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>How a <see cref="Da2Setting"/> is held in the code and shown.</summary>
public enum Da2SettingKind
{
    /// <summary>A float built by lui (and ori): without an ori only the upper half can change.</summary>
    Float,

    /// <summary>The immediate of an addiu (a whole number).</summary>
    Count,

    /// <summary>The immediate T of an ori that a 16-bit random number is compared with (it happens when rand &gt; T), shown in %.</summary>
    ChanceAbove,

    /// <summary>The immediate T of an slti (it happens when rand &lt; T, so at most 50 %), shown in %.</summary>
    ChanceBelow,

    /// <summary>The immediate of an ori shifted into the top of a double (ori x, zero, imm; dsll32 x, x, 14): 0 to 1.98.</summary>
    DoubleHigh,
}

/// <summary>One number of the game's rules (kill XP, drops, potions ...).</summary>
public sealed record Da2Setting(string Key, string Group, string Name, Da2SettingKind Kind, uint Address, uint Ori,
    float Min, float Max, string Note)
{
    /// <summary>False for a lui-only float (rounded to the upper half of the float) and the DoubleHigh factor.</summary>
    public bool FullPrecision => Kind switch
    {
        Da2SettingKind.Float => Ori != 0,
        Da2SettingKind.DoubleHigh => false,
        _ => true
    };

    public bool IsPercent => Kind is Da2SettingKind.ChanceAbove or Da2SettingKind.ChanceBelow;
}

public sealed partial class Da2Executable
{
    public const string GroupKillXp = "Kill XP", GroupXpBonus = "XP bonuses", GroupDrops = "Drop rolls",
        GroupGold = "Gold value", GroupValue = "Item value", GroupPotions = "Potions", GroupRansack = "Ransack",
        GroupTwoPlayers = "Two players";

    private static Da2Setting F(string key, string group, string name, uint lui, uint ori, float min, float max, string note) =>
        new(key, group, name, Da2SettingKind.Float, lui, ori, min, max, note);

    /// <summary>Every rule WorldExplorer edits, in the order the tabs show them.</summary>
    public static readonly IReadOnlyList<Da2Setting> Settings = new[]
    {
        // ---- kill XP ----
        F("xp.exponent", GroupKillXp, "Kill XP exponent", 0x0025E104, 0, 0.5f, 2f,
            "A kill is worth floor((XP rating ÷ the players' levels added up) ^ this)."),
        F("xp.share", GroupKillXp, "Share of the kill", 0x0025E140, 0, 0f, 10f,
            "The kill's worth times this is shared out."),
        F("xp.shareEach", GroupKillXp, "Each player's part (two or more alive)", 0x0025E178, 0x0025E17C, 0f, 10f,
            "Each living player gets this part of the share; a lone survivor gets all of it."),
        F("xp.lyranExponent", GroupKillXp, "Lyran: kill XP exponent", 0x001766A8, 0, 0.5f, 2f,
            "Lyran's own death code: no share factor."),
        F("xp.lyranShareEach", GroupKillXp, "Lyran: each player's part (two or more alive)", 0x001766E8, 0, 0f, 10f,
            "A lone survivor gets all of it."),
        F("xp.trollExponent", GroupKillXp, "Trolls and owlbears: kill XP exponent", 0x001CD0A0, 0, 0.5f, 2f,
            "Their own death code: no share factor."),
        F("xp.trollShareEach", GroupKillXp, "Trolls and owlbears: each player's part (two or more alive)", 0x001CD0E0, 0, 0f, 10f,
            "A lone survivor gets all of it."),

        // ---- XP bonuses (giveXP) ----
        F("xp.bonusVahnAdrianna", GroupXpBonus, "Vahn and Adrianna", 0x002B84B4, 0x002B84B8, 0f, 1f,
            "Extra XP for classes 0 and 1 (0.03 = 3 %)."),
        F("xp.bonusDrizztArtemis", GroupXpBonus, "Drizzt and Artemis", 0x002B84D8, 0x002B84DC, 0f, 1f,
            "Extra XP for classes 3 and 9 (0.09 = 9 %)."),
        F("xp.itemBonus", GroupXpBonus, "Per point of the equipment's XP bonus", 0x002B84F4, 0x002B84F8, 0f, 1f,
            "Added for each point of \"+N XP\" the character's items give (0.05 = 5 %)."),

        // ---- drop rolls ----
        F("loot.lowChance", GroupDrops, "Drop chance below 1 ×", 0x00264A20, 0, 0f, 1f,
            "A creature's drop chance below 1.0 is multiplied by this first (0.15 becomes 0.075); 1.0 and more are not."),
        F("loot.treasurePerPoint", GroupDrops, "Per point of Increased Treasure Drops", 0x00264A70, 0x00264A74, 0f, 1f,
            "Added to the drop chance for each point of the better of players 1 and 2 (pearls in armour)."),
        new("loot.twoPiles", GroupDrops, "Two or three gold piles (%)", Da2SettingKind.ChanceAbove, 0x00264BE0, 0, 0f, 100f,
            "Otherwise one pile."),
        new("loot.threePiles", GroupDrops, "Three gold piles (%)", Da2SettingKind.ChanceAbove, 0x00264C00, 0, 0f, 100f,
            "Part of the line above."),
        new("loot.pileBase", GroupDrops, "Gold pile: least", Da2SettingKind.Count, 0x00264CA8, 0, 0f, 10000f,
            "A pile is (this + 0 .. spread − 1) × gold scale × the gold factor × the level's gold value."),
        new("loot.pileSpread", GroupDrops, "Gold pile: spread", Da2SettingKind.Count, 0x00264C4C, 0, 1f, 10000f,
            "The random part of a pile."),
        F("loot.goldFactor", GroupDrops, "Gold factor", 0x00264C34, 0x00264C38, 0f, 100f,
            "Scales every gold pile of every creature."),
        F("loot.itemValue", GroupDrops, "Item value", 0x00264E94, 0, 0f, 100000f,
            "An item is worth quality factor × this × the level's item value × (least + 0 .. spread)."),
        F("loot.itemFloor", GroupDrops, "Item value: least", 0x00264EAC, 0x00264EB0, 0f, 10f, ""),
        F("loot.itemSpread", GroupDrops, "Item value: spread", 0x00264E9C, 0x00264EA0, 0f, 10f, ""),
        F("loot.qualityLow", GroupDrops, "Quality factor of a poor roll", 0x00264BB8, 0, 0f, 100f,
            "The item value factor the first column of the quality table gives (the middle one is 1)."),
        F("loot.qualityHigh", GroupDrops, "Quality factor of a good roll", 0x00264BD4, 0, 0f, 100f,
            "The item value factor the last column gives."),

        // ---- gold value of a level (0x2539F0) ----
        F("gold.act1PerLevel", GroupGold, "Act 1: per monster level", 0x00253AB4, 0x00253AB8, 0f, 100f, ""),
        F("gold.act1Base", GroupGold, "Act 1: base", 0x00253AC0, 0x00253AC4, 0f, 1000f, ""),
        F("gold.act2Offset", GroupGold, "Act 2: monster level +", 0x00253A98, 0, 0f, 1000f, ""),
        F("gold.act3Offset", GroupGold, "Act 3: monster level +", 0x00253A4C, 0, 0f, 1000f, ""),
        F("gold.act23PerLevel", GroupGold, "Acts 2 and 3: per monster level", 0x00253A54, 0x00253A58, 0f, 100f, ""),
        F("gold.act23Base", GroupGold, "Acts 2 and 3: base", 0x00253A6C, 0x00253A70, 0f, 1000f, ""),
        F("gold.act4Offset", GroupGold, "Act 4: monster level +", 0x00253AE8, 0, 0f, 1000f, ""),
        F("gold.act4PerLevel", GroupGold, "Act 4: per monster level", 0x00253AF0, 0x00253AF4, 0f, 100f, ""),
        F("gold.act4Base", GroupGold, "Act 4: base", 0x00253B08, 0x00253B0C, 0f, 1000f, ""),
        F("gold.scale", GroupGold, "Scale", 0x002539FC, 0, 0f, 1000f, "Every act."),
        F("gold.extreme", GroupGold, "Extreme: monster level +", 0x00253A14, 0, 0f, 1000f, "On Extreme only."),
        F("gold.extremeAct1", GroupGold, "Extreme, act-1 levels: + more", 0x00253A2C, 0, 0f, 1000f, "Levels with the act 1 flag, on Extreme."),

        // ---- item value of a level (0x253B28) ----
        F("value.act1PerLevel", GroupValue, "Act 1: per monster level", 0x00253BF0, 0x00253BF4, 0f, 100f, ""),
        F("value.act1Base", GroupValue, "Act 1: base", 0x00253BFC, 0x00253C00, 0f, 1000f, ""),
        F("value.act2Offset", GroupValue, "Act 2: monster level +", 0x00253C24, 0, 0f, 1000f, ""),
        F("value.act2PerLevel", GroupValue, "Act 2: per monster level", 0x00253C2C, 0x00253C30, 0f, 100f, ""),
        F("value.act2Base", GroupValue, "Act 2: base", 0x00253C44, 0x00253C48, 0f, 1000f, ""),
        F("value.act3Offset", GroupValue, "Act 3: monster level +", 0x00253B88, 0, 0f, 1000f, ""),
        F("value.act4Offset", GroupValue, "Act 4: monster level +", 0x00253BD4, 0, 0f, 1000f, ""),
        F("value.act34PerLevel", GroupValue, "Acts 3 and 4: per monster level", 0x00253B90, 0x00253B94, 0f, 100f, ""),
        F("value.act34Base", GroupValue, "Acts 3 and 4: base", 0x00253BA8, 0x00253BAC, 0f, 1000f, ""),
        F("value.scale", GroupValue, "Scale", 0x00253B34, 0x00253B38, 0f, 1000f, "Every act."),
        F("value.extreme", GroupValue, "Extreme: monster level +", 0x00253B50, 0, 0f, 1000f, "On Extreme only."),
        F("value.extremeAct1", GroupValue, "Extreme, act-1 levels: + more", 0x00253B68, 0, 0f, 1000f, "Levels with the act 1 flag, on Extreme."),

        // ---- potions (0x257F18) ----
        new("potion.healing", GroupPotions, "Healing potion (%)", Da2SettingKind.ChanceBelow, 0x00258014, 0, 0f, 50f,
            "The kind of potion a drop is; recall below, rejuvenation the rest."),
        new("potion.recall", GroupPotions, "Recall potion (%)", Da2SettingKind.ChanceAbove, 0x002580A4, 0, 0f, 100f,
            "A lesser healing potion instead in the opening levels (basic items only)."),
        F("potion.perLevel", GroupPotions, "Tier per monster level", 0x00257F24, 0x00257F28, 0f, 10f,
            "The tier decides lesser / normal / extra."),
        F("potion.act2", GroupPotions, "Tier: act 2 +", 0x00257F60, 0x00257F64, 0f, 100f, ""),
        F("potion.act3", GroupPotions, "Tier: act 3 +", 0x00257F84, 0x00257F88, 0f, 100f, ""),
        F("potion.act4", GroupPotions, "Tier: act 4 +", 0x00257FA4, 0, 0f, 100f, ""),
        F("potion.extreme", GroupPotions, "Tier on Extreme", 0x00257FF0, 0, 0f, 100f, "Instead of the above: extra potions only."),
        F("potion.healSpread", GroupPotions, "Healing: random part of the tier", 0x0025802C, 0x00258030, 0f, 10f, "0 to this is added."),
        F("potion.healLesser", GroupPotions, "Healing: lesser up to tier", 0x00258038, 0, 0f, 100f, ""),
        F("potion.healNormal", GroupPotions, "Healing: normal up to tier", 0x00258070, 0, 0f, 100f, "Extra above."),
        F("potion.rejuvSpread", GroupPotions, "Rejuvenation: random part of the tier", 0x002580C0, 0x002580C4, 0f, 10f, ""),
        F("potion.rejuvLesser", GroupPotions, "Rejuvenation: lesser up to tier", 0x002580CC, 0, 0f, 100f, ""),
        F("potion.rejuvNormal", GroupPotions, "Rejuvenation: normal up to tier", 0x0025811C, 0, 0f, 100f, "Extra above."),

        // ---- Ransack (feat 102) ----
        F("ransack.chance", GroupRansack, "Chance", 0x00284600, 0x00284604, 0f, 1f, "0.1 = 10 %."),
        F("ransack.chancePerRank", GroupRansack, "Chance per rank", 0x002845E8, 0x002845EC, 0f, 1f, ""),
        F("ransack.goldPerRank", GroupRansack, "Gold scale per rank", 0x00284638, 0x0028463C, 0f, 100f, "On top of 1."),
        F("ransack.qualityPerRank", GroupRansack, "Quality per rank", 0x00284654, 0x00284658, 0f, 9f, "The quality level is at most 9."),

        // ---- two players counted ----
        new("loot.twoPlayerPiles", GroupTwoPlayers, "Gold pile chance ×", Da2SettingKind.DoubleHigh, 0x00264AA4, 0, 0f, 1.984375f,
            "Each pile's chance with two players counted (three with the mod): 0 to 1.98 in steps of 1/64 above 1."),
    };

    public static Da2Setting Setting(string key) =>
        Settings.FirstOrDefault(s => s.Key == key) ?? throw new ArgumentOutOfRangeException(nameof(key), key);

    /// <summary>The words a setting is written to (one, or lui and ori).</summary>
    public static IEnumerable<uint> SettingWords(Da2Setting s) => s.Ori != 0 ? new[] { s.Address, s.Ori } : new[] { s.Address };

    /// <summary>True when the instruction(s) at the setting's address are what WorldExplorer expects.</summary>
    public bool IsSettingCode(Da2Setting s)
    {
        var w = Word(s.Address);
        return s.Kind switch
        {
            Da2SettingKind.Float => IsConstant(s.Address, s.Ori),
            Da2SettingKind.Count => w >> 26 == 0x09,                                          // addiu rt, rs, imm
            Da2SettingKind.ChanceAbove => w >> 26 == 0x0D && ((w >> 21) & 31) == 0,           // ori rt, zero, imm
            Da2SettingKind.ChanceBelow => w >> 26 == 0x0A,                                    // slti rt, rs, imm
            Da2SettingKind.DoubleHigh => w >> 26 == 0x0D && ((w >> 21) & 31) == 0 &&          // ori rt, zero, imm; dsll32 rt, rt, 14
                                         Word(s.Address + 4) == (0x0000003Cu | ((w >> 16) & 31) << 16 | ((w >> 16) & 31) << 11 | 14u << 6),
            _ => false
        };
    }

    /// <summary>
    /// The first value WorldExplorer edits whose instructions aren't what the game has there (a group made by
    /// hand changed them), or null when every one is as expected.
    /// </summary>
    public string? UnexpectedCode()
    {
        foreach (var s in Settings)
        {
            if (!IsSettingCode(s)) return $"{s.Group}, {s.Name} (0x{s.Address:X6})";
        }

        foreach (var c in Da2LootData.Constants)
        {
            if (!IsConstant(c.Lui, c.Ori)) return $"a creature's drop value (0x{c.Lui:X6})";
        }

        foreach (var c in Da2MonsterData.Constants)
        {
            if (!IsConstant(c.Lui, c.Ori)) return $"a creature multiplier (0x{c.Lui:X6})";
        }

        foreach (var b in Da2MonsterData.BossHp)
        {
            if (!IsConstant(b.Lui, b.Ori)) return $"{b.Class}'s hit points (0x{b.Lui:X6})";
        }

        foreach (var (name, lui, ori) in DifficultyConstants)
        {
            if (!IsConstant(lui, ori)) return $"{DescribeFactor(name)} (0x{lui:X6})";
        }

        return null;
    }

    private void RequireSettingCode(Da2Setting s)
    {
        if (!IsSettingCode(s)) throw new InvalidOperationException($"{s.Group}, {s.Name}: unexpected code at 0x{s.Address:X6}.");
    }

    /// <summary>The value as the tabs show it (a factor, a whole number, or a percentage).</summary>
    public float GetSetting(Da2Setting s)
    {
        RequireSettingCode(s);
        var imm = Word(s.Address) & 0xFFFF;
        return s.Kind switch
        {
            Da2SettingKind.Float => ConstantValue(s.Address, s.Ori),
            Da2SettingKind.Count => (short)imm,
            Da2SettingKind.ChanceAbove => (65535 - imm) * 100f / 65536f,
            Da2SettingKind.ChanceBelow => (short)imm * 100f / 65536f,
            Da2SettingKind.DoubleHigh => (float)BitConverter.Int64BitsToDouble((long)imm << 46),
            _ => float.NaN
        };
    }

    /// <summary>The value the game will really have for <paramref name="value"/> (rounded to what the code can hold).</summary>
    public static float RepresentableSetting(Da2Setting s, float value) => s.Kind switch
    {
        Da2SettingKind.Float => Representable(value, s.Ori != 0),
        Da2SettingKind.Count => MathF.Round(value),
        Da2SettingKind.ChanceAbove => (65535 - AboveImmediate(value)) * 100f / 65536f,
        Da2SettingKind.ChanceBelow => BelowImmediate(value) * 100f / 65536f,
        Da2SettingKind.DoubleHigh => (float)BitConverter.Int64BitsToDouble((long)DoubleHighImmediate(value) << 46),
        _ => value
    };

    private static uint AboveImmediate(float percent) => (uint)Math.Clamp(Math.Round(65535 - percent / 100.0 * 65536), 0, 65535);
    private static uint BelowImmediate(float percent) => (uint)Math.Clamp(Math.Round(percent / 100.0 * 65536), 0, 32767);

    private static uint DoubleHighImmediate(float value)
    {
        if (value <= 0) return 0;
        var bits = (ulong)BitConverter.DoubleToInt64Bits(value);
        var imm = (bits + (1UL << 45)) >> 46;          // nearest
        return (uint)Math.Min(imm, 0xFFFF);
    }

    /// <summary>Checks the range and writes the setting (rounded as <see cref="RepresentableSetting"/>).</summary>
    public void SetSetting(Da2Setting s, float value)
    {
        RequireSettingCode(s);
        if (float.IsNaN(value) || value < s.Min || value > s.Max)
            throw new ArgumentException($"{s.Group}, {s.Name}: must be from {Text(s.Min)} to {Text(s.Max)}{(s.IsPercent ? " %" : "")}.");
        var w = Word(s.Address);
        switch (s.Kind)
        {
            case Da2SettingKind.Float:
                SetConstant(s.Address, s.Ori, value);
                break;
            case Da2SettingKind.Count:
                SetWord(s.Address, (w & 0xFFFF0000) | ((uint)(short)MathF.Round(value) & 0xFFFF));
                break;
            case Da2SettingKind.ChanceAbove:
                SetWord(s.Address, (w & 0xFFFF0000) | AboveImmediate(value));
                break;
            case Da2SettingKind.ChanceBelow:
                SetWord(s.Address, (w & 0xFFFF0000) | BelowImmediate(value));
                break;
            case Da2SettingKind.DoubleHigh:
                SetWord(s.Address, (w & 0xFFFF0000) | DoubleHighImmediate(value));
                break;
        }
    }

    /// <summary>
    /// Writes several settings at once after checking all of them, and the rules that tie two together
    /// (three piles no likelier than two or three; healing and recall at most 100 %). Nothing is written
    /// when one is wrong.
    /// </summary>
    public void SetSettings(IReadOnlyDictionary<string, float> values)
    {
        foreach (var (key, v) in values)
        {
            var s = Setting(key);
            RequireSettingCode(s);
            if (float.IsNaN(v) || v < s.Min || v > s.Max)
                throw new ArgumentException($"{s.Group}, {s.Name}: must be from {Text(s.Min)} to {Text(s.Max)}{(s.IsPercent ? " %" : "")}.");
        }

        float Value(string key) => values.TryGetValue(key, out var v) ? v : GetSetting(Setting(key));
        if (Value("loot.threePiles") > Value("loot.twoPiles"))
            throw new ArgumentException($"{GroupDrops}: three gold piles can't be likelier than two or three.");
        if (RepresentableSetting(Setting("potion.healing"), Value("potion.healing")) +
            RepresentableSetting(Setting("potion.recall"), Value("potion.recall")) > 100.001f)
            throw new ArgumentException($"{GroupPotions}: healing and recall potions together can be at most 100 %.");
        foreach (var (key, v) in values) SetSetting(Setting(key), v);
    }

    public Dictionary<string, float> GetSettings() => Settings.ToDictionary(s => s.Key, GetSetting);

    private static string Text(float v) => v.ToString("0.######", CultureInfo.InvariantCulture);

    // ---- predictions ----

    /// <summary>
    /// The XP each living player gets for a kill of a creature with <paramref name="rating"/> (the creature's XP
    /// rating) when the players' levels add up to <paramref name="levels"/> and <paramref name="alive"/> are
    /// alive, before the class / item bonus. <paramref name="kind"/>: 0 general, 1 Lyran, 2 trolls and owlbears.
    /// </summary>
    public int KillXp(float rating, int levels, int alive, int kind = 0)
    {
        if (levels < 1 || alive < 1) return 0;
        var (e, share, each) = kind switch
        {
            1 => (GetSetting(Setting("xp.lyranExponent")), 1f, GetSetting(Setting("xp.lyranShareEach"))),
            2 => (GetSetting(Setting("xp.trollExponent")), 1f, GetSetting(Setting("xp.trollShareEach"))),
            _ => (GetSetting(Setting("xp.exponent")), GetSetting(Setting("xp.share")), GetSetting(Setting("xp.shareEach")))
        };
        return KillXp(rating, levels, alive, e, share, each);
    }

    public static int KillXp(float rating, int levels, int alive, float exponent, float share, float each)
    {
        if (levels < 1 || alive < 1) return 0;
        var worth = Trunc(MathF.Pow(rating / levels, exponent));
        var kill = Trunc(worth * share);
        return alive >= 2 ? Trunc(kill * each) : kill;
    }

    /// <summary>The share giveXP adds on top for a bonus (0.09 = 9 %): XP ÷ trunc(1 ÷ bonus), at most +100 %.</summary>
    public static float EffectiveXpBonus(float bonus)
    {
        if (bonus <= 0) return 0;
        var b = MathF.Min(bonus, 1f);
        var divisor = Trunc(1f / b);
        return divisor <= 0 ? 0 : 1f / divisor;
    }

    private static int Trunc(float v) => float.IsNaN(v) ? 0 : (int)Math.Clamp(MathF.Truncate(v), int.MinValue, int.MaxValue);

    /// <summary>The gold value (curve 0x2539F0) or item value (0x253B28) of a level, as the game works it out.</summary>
    public float LevelLootValue(bool gold, float monsterLevel, uint flags, int difficulty) =>
        LevelLootValue(key => GetSetting(Setting(key)), gold, monsterLevel, flags, difficulty);

    /// <summary>The same with the rules' values given by <paramref name="setting"/> (key → value), e.g. as typed.</summary>
    public static float LevelLootValue(Func<string, float> setting, bool gold, float monsterLevel, uint flags, int difficulty)
    {
        float S(string key) => setting((gold ? "gold." : "value.") + key);
        var l = monsterLevel;
        if (difficulty == 3)
        {
            l += S("extreme");
            if ((flags & 0x100000) != 0) l += S("extremeAct1");
        }

        float tail(float x, string perLevel, string b) => (x * S(perLevel) + S(b)) * 0.5f;
        float v;
        if (gold)
        {
            if ((flags & 0x400000) != 0) v = tail(l + S("act3Offset"), "act23PerLevel", "act23Base");
            else if ((flags & 0x200000) != 0) v = tail(l + S("act2Offset"), "act23PerLevel", "act23Base");
            else if ((flags & 0x800000) != 0) v = tail(l + S("act4Offset"), "act4PerLevel", "act4Base");
            else v = tail(l, "act1PerLevel", "act1Base");
        }
        else
        {
            if ((flags & 0x400000) != 0) v = tail(l + S("act3Offset"), "act34PerLevel", "act34Base");
            else if ((flags & 0x800000) != 0) v = tail(l + S("act4Offset"), "act34PerLevel", "act34Base");
            else if ((flags & 0x200000) != 0) v = tail(l + S("act2Offset"), "act2PerLevel", "act2Base");
            else v = tail(l, "act1PerLevel", "act1Base");
        }

        return v * S("scale");
    }
}
