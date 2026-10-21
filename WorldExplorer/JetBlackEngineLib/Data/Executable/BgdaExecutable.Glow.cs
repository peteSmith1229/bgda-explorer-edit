// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.Glow.cs
//
// Glow styles (SLES_506.72, PAL): an enchant glow for each item and for each group of modifiers.
//
// THE GAME. FUN_00174A50(item, size) chooses every enchant glow: weapons and shields in a
// character's hands (FUN_0015BEB8), the turning item models in the shop and inventory lists
// (FUN_00175278) and items in the level (0x0011333C, 0x0011EC00, 0x001B2A9C). An item glows when
// its power (FUN_00174098, ItemFormulas.Power) is above 0 and it isn't Flaming or Flaming Burst.
// Every item gets the two layers of the table at 0x002A79B8 (the ENCHANT GLOW colours); only
// their alpha changes: 64 and 72 times f = min(1, 0.6 + 0.025 x power). The draw copies the
// colours as each model is drawn, so they can be chosen item by item. Worn body armour, helmets,
// gloves and boots never glow on a character: their models (ARMOR.YAK) can't take a glow.
//
// GLOW. Glow styles replace FUN_00174A50 with GLOW: its first two words become
// "j 0x001633F8; nop", and GLOW goes in FUN_001633F8, the debug starting kit nothing calls.
// Its 736 bytes then hold:
//   0x001633F8  GLOW, 103 instructions (GlowCode; the same words glowpatch.py writes)
//   0x001635C0  16 styles x {halo R, G, B, strength, core R, G, B, strength}
//   0x00163640  16 rules: the u64 property bits each looks for (0 = unused)
//   0x001636C0  16 x u8: the style a rule gives, or 255 = no glow
//   0x001636D0  "GLOW" and u32 version 1
// and the byte +0x2C of each item record (padding the game never reads, zero in all 81) is
// the item's own setting: 0 = automatic, 1-16 = style 0-15, 255 = never glows.
// How GLOW decides (GlowFunction.Decide does the same):
//   1. never: no glow;
//   2. its own style: the item glows in it when its power is above 0 or a rule matches it;
//   3. automatic: the first rule that shares a bit with the item decides, a style (even at
//      power 0) or no glow; with no matching rule, the game's own rule, in style 0.
//   The alpha of each layer is f x the style's strength (f as the game's); sizes are the game's.
// With every style the table's colours at 64 and 72, no rules and every item automatic, GLOW
// does exactly what FUN_00174A50 does (checked in an emulator on the stock executable and on
// ADRIANNA26), and the test styles were tried in the game on ADRIANNA26. The three-player patch
// (every tested build) touches none of these bytes.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>One layer of a glow style: its colour and its strength (the alpha at full power, 0-255; the game's are 64 and 72).</summary>
public readonly record struct GlowLayer(Rgb Colour, int Strength);

/// <summary>A glow style: a large faint halo round the item and a small bright core.</summary>
public readonly record struct GlowStyle(GlowLayer Halo, GlowLayer Core)
{
    /// <summary>The game's own strengths: 64 for the halo, 72 for the core.</summary>
    public const int GameHaloStrength = 64, GameCoreStrength = 72;

    /// <summary>These colours at the game's own strengths.</summary>
    public static GlowStyle FromColours(EnchantGlow colours) =>
        new(new GlowLayer(colours.Halo, GameHaloStrength), new GlowLayer(colours.Core, GameCoreStrength));

    /// <summary>The game's own glow: halo (10, 10, 68), core (60, 60, 68).</summary>
    public static GlowStyle Game => FromColours(new EnchantGlow(new Rgb(10, 10, 68), new Rgb(60, 60, 68)));
}

/// <summary>A glow rule: an item with any of <see cref="Bits"/> glows in <see cref="Style"/> (0-15), or not at all (null).</summary>
public readonly record struct GlowRule(ulong Bits, int? Style);

/// <summary>An item's own glow setting (the byte at +0x2C of its record): automatic, a style, or never.</summary>
public readonly record struct ItemGlow(byte Value)
{
    public static ItemGlow Automatic => new(0);
    public static ItemGlow Never => new(255);

    public static ItemGlow InStyle(int style) =>
        style is >= 0 and < GlowSettings.StyleCount
            ? new ItemGlow((byte)(style + 1))
            : throw new ArgumentOutOfRangeException(nameof(style), "A style is 0 to 15.");

    public bool IsAutomatic => Value == 0;
    public bool IsNever => Value == 255;

    /// <summary>The style it glows in (what GLOW makes of the byte), or null for automatic and never.</summary>
    public int? Style => IsAutomatic || IsNever ? null : (Value - 1) & (GlowSettings.StyleCount - 1);

    /// <summary>A value WorldExplorer writes: 0, 1-16 or 255.</summary>
    public bool IsValid => Value <= GlowSettings.StyleCount || Value == 255;

    public override string ToString() => IsAutomatic ? "Automatic" : IsNever ? "Never" : $"Style {Style}";
}

/// <summary>The 16 styles and up to 16 rules of glow styles.</summary>
public sealed class GlowSettings : IEquatable<GlowSettings>
{
    public const int StyleCount = 16, RuleCount = 16;

    public GlowSettings(IEnumerable<GlowStyle> styles, IEnumerable<GlowRule> rules)
    {
        Styles = styles.ToList().AsReadOnly();
        Rules = rules.ToList().AsReadOnly();
    }

    /// <summary>16 styles; style 0 is the one items glow in when no rule or setting picks another.</summary>
    public IReadOnlyList<GlowStyle> Styles { get; }

    /// <summary>Up to 16 rules, in order: the first that matches an item decides.</summary>
    public IReadOnlyList<GlowRule> Rules { get; }

    /// <summary>Every style in these colours at the game's strengths and no rules: GLOW then does what the game does.</summary>
    public static GlowSettings Plain(EnchantGlow colours) =>
        new(Enumerable.Repeat(GlowStyle.FromColours(colours), StyleCount), Array.Empty<GlowRule>());

    /// <summary>Why these can't be written, or null.</summary>
    public string? Problem()
    {
        if (Styles.Count != StyleCount) return $"There are {StyleCount} glow styles.";
        for (var i = 0; i < StyleCount; i++)
        {
            foreach (var (layer, name) in new[] { (Styles[i].Halo, "halo"), (Styles[i].Core, "core") })
            {
                var c = layer.Colour;
                if (c.R is < 0 or > 255 || c.G is < 0 or > 255 || c.B is < 0 or > 255)
                    return $"Glow style {i}, {name}: colours are 0 to 255 per channel.";
                if (layer.Strength is < 0 or > 255) return $"Glow style {i}, {name}: the strength is 0 to 255.";
            }
        }

        if (Rules.Count > RuleCount) return $"There can be at most {RuleCount} glow rules.";
        for (var i = 0; i < Rules.Count; i++)
        {
            if (Rules[i].Bits == 0) return $"Glow rule {i + 1} looks for nothing: give it a modifier or remove it.";
            if (Rules[i].Style is { } s && s is < 0 or >= StyleCount) return $"Glow rule {i + 1}: a style is 0 to 15.";
        }

        return null;
    }

    public bool Equals(GlowSettings? other) =>
        other != null && Styles.SequenceEqual(other.Styles) && Rules.SequenceEqual(other.Rules);

    public override bool Equals(object? obj) => Equals(obj as GlowSettings);

    public override int GetHashCode() => HashCode.Combine(Styles.Count > 0 ? Styles[0] : default, Rules.Count);
}

/// <summary>Whether an executable has glow styles.</summary>
public enum GlowStylesState
{
    /// <summary>The game's own glow function (glow styles can be put in).</summary>
    Off,

    /// <summary>GLOW is in (version 1).</summary>
    On,

    /// <summary>The glow function or the debug kit is something else: glow styles can't be used.</summary>
    Unknown
}

/// <summary>How an item glows: its style, the alpha of each layer and the rule that picked it (null: none).</summary>
public readonly record struct GlowResult(int Style, int HaloAlpha, int CoreAlpha, int? Rule);

/// <summary>What the glow function does with an item, in C# (GLOW's and the game's own).</summary>
public static class GlowFunction
{
    /// <summary>Flaming and Flaming Burst: the game's own glow leaves these out (they have flames).</summary>
    public const ulong Flaming = (1UL << ItemBits.Fire) | (1UL << ItemBits.FireBurst);

    /// <summary>The strength factor of the game: min(1, 0.6 + 0.025 x power), in single precision.</summary>
    public static float Factor(int power)
    {
        var f = power * 0.025f + 0.6f;
        return 1f < f ? 1f : f;
    }

    private static GlowResult Result(int style, int power, GlowStyle s, int? rule)
    {
        var f = Factor(power);
        return new GlowResult(style, (int)(f * s.Halo.Strength), (int)(f * s.Core.Strength), rule);
    }

    /// <summary>What GLOW gives an item with these property bits and this setting; null: no glow.</summary>
    public static GlowResult? Decide(GlowSettings settings, ItemGlow item, ulong bits)
    {
        if (item.IsNever) return null;
        var power = ItemFormulas.Power(bits);
        int? rule = null;
        var ruleStyle = -1;
        for (var i = 0; i < settings.Rules.Count && i < GlowSettings.RuleCount; i++)
        {
            if ((settings.Rules[i].Bits & bits) == 0) continue;
            rule = i;
            ruleStyle = settings.Rules[i].Style ?? 255;
            break;
        }

        int style;
        if (!item.IsAutomatic)
        {
            if (power <= 0 && rule == null) return null;
            style = item.Style!.Value;
        }
        else if (ruleStyle == 255)
        {
            return null;
        }
        else if (rule != null)
        {
            style = ruleStyle & (GlowSettings.StyleCount - 1);
        }
        else if (power <= 0 || (bits & Flaming) != 0)
        {
            return null;
        }
        else
        {
            style = 0;
        }

        return Result(style, power, settings.Styles[style], rule);
    }

    /// <summary>What the game's own glow function gives (no glow styles): style 0 at 64 and 72, or null.</summary>
    public static GlowResult? GameDecide(ulong bits)
    {
        var power = ItemFormulas.Power(bits);
        if (power <= 0 || (bits & Flaming) != 0) return null;
        return Result(0, power, GlowStyle.Game, null);
    }
}

public partial class BgdaExecutable
{
    /// <summary>The game's glow function, FUN_00174A50.</summary>
    public const int GlowFunctionVa = 0x00174A50;

    /// <summary>Where GLOW goes: FUN_001633F8, the debug starting kit (736 bytes, to 0x001636D8).</summary>
    public const int GlowCodeVa = 0x001633F8, GlowCodeEndVa = 0x001636D8;

    public const int GlowStylesVa = 0x001635C0, GlowRuleBitsVa = 0x00163640, GlowRuleStylesVa = 0x001636C0, GlowMarkerVa = 0x001636D0;

    /// <summary>The byte of an item record that holds its glow setting (padding the game never reads).</summary>
    public const int ItemGlowOffset = 0x2C;

    private const uint GlowVersion = 1;

    private static readonly byte[] GlowMark = { (byte)'G', (byte)'L', (byte)'O', (byte)'W' };

    /// <summary>The first two words of the game's FUN_00174A50: addiu sp, sp, -48; sd s0, 0(sp).</summary>
    private static readonly uint[] GameGlowStart = { 0x27BDFFD0, 0xFFB00000 };

    /// <summary>GLOW, at 0x001633F8 (assembled by glowpatch.py, which documents each instruction).</summary>
    private static readonly uint[] GlowCode =
    {
        0x27BDFFD0, 0xFFB00000, 0xE7B40020, 0x0080802D, 0xFFBF0010, 0x0C05D026, 0x46006506, 0x8E080000,
        0xDE090008, 0x910A002C, 0x240B00FF, 0x114B0053, 0x3C0C0016, 0x258C3640, 0x258D0080, 0x240E0010,
        0x240FFFFF, 0xDD980000, 0x0309C024, 0x17000006, 0x25CEFFFF, 0x258C0008, 0x15C0FFFA, 0x25AD0001,
        0x10000002, 0x00000000, 0x91AF0000, 0x11400007, 0x00000000, 0x1C40000D, 0x254AFFFF, 0x05E0003F,
        0x00000000, 0x10000009, 0x00000000, 0x11EB003B, 0x00000000, 0x05E10005, 0x01E0502D, 0x18400037,
        0x31380810, 0x17000035, 0x0000502D, 0x314A000F, 0x000A50C0, 0x3C0B0016, 0x256B35C0, 0x016A5821,
        0x44820000, 0x46800020, 0xC78189DC, 0xC78289E0, 0x3C013F80, 0x44811800, 0x46010002, 0x46020000,
        0x46001834, 0x00000000, 0x45030001, 0x46001806, 0x3C03002A, 0x246379B8, 0x8D6C0000, 0x8D6D0004,
        0xAC6C0000, 0xAC6D0008, 0x000C6602, 0x000D6E02, 0x448C2000, 0x448D2800, 0x46802120, 0x46802960,
        0x46040102, 0x46050142, 0x46002124, 0x46002964, 0x440C2000, 0x440D2800, 0xA06C0003, 0xA06D000B,
        0x3C014348, 0x44810800, 0x4601A042, 0xE4610004, 0x3C0142C8, 0x44811000, 0x4602A082, 0xE462000C,
        0x0060202D, 0x24050002, 0xDFBF0010, 0xDFB00000, 0xC7B40020, 0x0804FED4, 0x27BD0030, 0x3C04002A,
        0x248479B8, 0x0000282D, 0xDFBF0010, 0xDFB00000, 0xC7B40020, 0x0804FED4, 0x27BD0030
    };

    /// <summary>FUN_001633F8 as the game has it (the debug kit), put back when glow styles are removed.</summary>
    private static readonly uint[] GameDebugKit =
    {
        0x27BDFF70, 0x8F82AA44, 0xFFB10020, 0xFFBF0080, 0x0000882D, 0xFFB60070, 0xFFB50060, 0xFFB40050,
        0xFFB30040, 0xFFB20030, 0x184000A3, 0xFFB00010, 0x3C16002A, 0x3C13002A, 0x3C150031, 0x3C140032,
        0x240238E0, 0x00000000, 0x26D08980, 0x02229018, 0x24030BB8, 0x02501021, 0x16200028, 0xAC4300DC,
        0x3C040031, 0x03A0282D, 0x24849480, 0x0C05D2D2, 0x26100540, 0x0000202D, 0x0C05B00C, 0x03A0282D,
        0x00021100, 0x0000202D, 0x00502821, 0x0C05B242, 0x24060001, 0x3C040031, 0x03A0282D, 0x0C05D2D2,
        0x24849490, 0x0000202D, 0x0C05B00C, 0x03A0282D, 0x00021100, 0x0000202D, 0x00502821, 0x0C05B242,
        0x24060001, 0x3C040031, 0x03A0282D, 0x0C05D2D2, 0x248494A0, 0x0000202D, 0x0C05B00C, 0x03A0282D,
        0x00021100, 0x0000202D, 0x00502821, 0x0C05B242, 0x24060001, 0x10000027, 0x00000000, 0x3C040031,
        0x03A0282D, 0x0C05D2D2, 0x248494B0, 0x0220202D, 0x0C05B00C, 0x03A0282D, 0x26100540, 0x00021100,
        0x02508021, 0x0220202D, 0x02022821, 0x0C05B242, 0x24060001, 0x3C040031, 0x03A0282D, 0x0C05D2D2,
        0x248494C0, 0x0220202D, 0x0C05B00C, 0x03A0282D, 0x00021100, 0x0220202D, 0x02022821, 0x0C05B242,
        0x24060001, 0x3C040031, 0x03A0282D, 0x0C05D2D2, 0x248494D0, 0x0220202D, 0x0C05B00C, 0x03A0282D,
        0x00021100, 0x0220202D, 0x02022821, 0x0C05B242, 0x24060001, 0x56200010, 0x3C040032, 0x3C040031,
        0x03A0282D, 0x0C05D2D2, 0x248494E0, 0x0000202D, 0x0C05B00C, 0x03A0282D, 0x00021100, 0x26658AB8,
        0x00452821, 0x0000202D, 0x0C05B242, 0x24060001, 0x10000011, 0x26A49500, 0x03A0282D, 0x0C05D2D2,
        0x24844A58, 0x0220202D, 0x0C05B00C, 0x03A0282D, 0x240538E0, 0x00021100, 0x02252818, 0x26638AB8,
        0x00431021, 0x0220202D, 0x24060001, 0x0C05B242, 0x00A22821, 0x26A49500, 0x0C05D2D2, 0x03A0282D,
        0x26904A60, 0x0220202D, 0x0C05B00C, 0x03A0282D, 0x240538E0, 0x00021100, 0x02252818, 0x26638AB8,
        0x00431021, 0x0220202D, 0x24060001, 0x0C05B242, 0x00A22821, 0x0200202D, 0x0C05D2D2, 0x03A0282D,
        0x0220202D, 0x0C05B00C, 0x03A0282D, 0x0200202D, 0x0C05D2D2, 0x03A0282D, 0x0220202D, 0x0C05B00C,
        0x03A0282D, 0x0200202D, 0x0C05D2D2, 0x03A0282D, 0x0220202D, 0x0C05B00C, 0x03A0282D, 0x0C05AA8A,
        0x0220202D, 0x26310001, 0x8F82AA44, 0x0222102A, 0x1440FF65, 0x240238E0, 0xDFBF0080, 0xDFB60070,
        0xDFB50060, 0xDFB40050, 0xDFB30040, 0xDFB20030, 0xDFB10020, 0xDFB00010, 0x03E00008, 0x27BD0090
    };

    /// <summary>"j 0x001633F8": what FUN_00174A50 starts with once GLOW is in.</summary>
    private static uint JumpToGlow => (uint)(0x08000000 | ((GlowCodeVa >> 2) & 0x03FFFFFF));

    /// <summary>Whether this executable has glow styles; <paramref name="detail"/> says why when it can't have them.</summary>
    public GlowStylesState GetGlowStylesState(out string detail)
    {
        detail = "";
        if (_data.Length < FileOffset(GlowCodeEndVa) || _data.Length < FileOffset(GlowFunctionVa) + 8)
        {
            detail = "The file is too short to be this build.";
            return GlowStylesState.Unknown;
        }

        var f0 = ReadU32(FileOffset(GlowFunctionVa));
        var f1 = ReadU32(FileOffset(GlowFunctionVa) + 4);
        if (f0 == GameGlowStart[0] && f1 == GameGlowStart[1])
        {
            if (WordsAre(GlowCodeVa, GameDebugKit)) return GlowStylesState.Off;
            detail = "The debug starting kit at 0x001633F8, where glow styles go, isn't the game's: something else uses it.";
            return GlowStylesState.Unknown;
        }

        if (f0 == JumpToGlow && f1 == 0)
        {
            if (!WordsAre(GlowCodeVa, GlowCode))
            {
                detail = "The glow function jumps to 0x001633F8, but what is there isn't the glow styles code WorldExplorer writes.";
                return GlowStylesState.Unknown;
            }

            var marker = FileOffset(GlowMarkerVa);
            if (!_data.AsSpan(marker, 4).SequenceEqual(GlowMark) || ReadU32(marker + 4) != GlowVersion)
            {
                detail = "The glow styles code is there, but not its \"GLOW\" version 1 mark.";
                return GlowStylesState.Unknown;
            }

            return GlowStylesState.On;
        }

        detail = "The glow function (FUN_00174A50) isn't the game's: another patch changed it.";
        return GlowStylesState.Unknown;
    }

    public GlowStylesState GetGlowStylesState() => GetGlowStylesState(out _);

    /// <summary>True when GLOW is in.</summary>
    public bool HasGlowStyles => GetGlowStylesState() == GlowStylesState.On;

    /// <summary>Why glow styles can't be used here, or null (they are on, or can be put in).</summary>
    public string? GlowStylesProblem()
    {
        if (ItemsProblem() is { } items) return items;
        return GetGlowStylesState(out var detail) == GlowStylesState.Unknown ? detail : null;
    }

    private bool WordsAre(int va, IReadOnlyList<uint> words)
    {
        var at = FileOffset(va);
        for (var i = 0; i < words.Count; i++)
        {
            if (ReadU32(at + 4 * i) != words[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// The styles and rules. Without glow styles: every style is the ENCHANT GLOW colours at the game's
    /// strengths and there are no rules (what GLOW would do the same as the game with).
    /// </summary>
    public GlowSettings GetGlowSettings()
    {
        RequireItems();
        var state = GetGlowStylesState(out var detail);
        if (state == GlowStylesState.Unknown) throw new InvalidOperationException(detail);
        if (state == GlowStylesState.Off) return GlowSettings.Plain(GetEnchantGlow());

        var styles = new List<GlowStyle>(GlowSettings.StyleCount);
        for (var i = 0; i < GlowSettings.StyleCount; i++)
        {
            var at = FileOffset(GlowStylesVa) + 8 * i;
            styles.Add(new GlowStyle(new GlowLayer(new Rgb(_data[at], _data[at + 1], _data[at + 2]), _data[at + 3]),
                                     new GlowLayer(new Rgb(_data[at + 4], _data[at + 5], _data[at + 6]), _data[at + 7])));
        }

        var rules = new List<GlowRule>();
        for (var i = 0; i < GlowSettings.RuleCount; i++)
        {
            var bits = ReadU64(FileOffset(GlowRuleBitsVa) + 8 * i);
            if (bits == 0) continue;
            var style = _data[FileOffset(GlowRuleStylesVa) + i];
            rules.Add(new GlowRule(bits, style == 255 ? null : style & (GlowSettings.StyleCount - 1)));
        }

        return new GlowSettings(styles, rules);
    }

    /// <summary>
    /// Writes the styles and rules, putting GLOW in first when the executable has the game's own glow
    /// function. The ENCHANT GLOW table takes style 0's colours, so it always shows style 0. Throws
    /// <see cref="ArgumentException"/> for a bad value and <see cref="InvalidOperationException"/> when glow
    /// styles can't be used here; nothing changes then.
    /// </summary>
    public void SetGlowSettings(GlowSettings settings)
    {
        RequireItems();
        if (settings.Problem() is { } problem) throw new ArgumentException(problem);
        var state = GetGlowStylesState(out var detail);
        if (state == GlowStylesState.Unknown) throw new InvalidOperationException("Glow styles can't be used: " + detail);

        if (state == GlowStylesState.Off)
        {
            Array.Clear(_data, FileOffset(GlowCodeVa), GlowCodeEndVa - GlowCodeVa);
            for (var i = 0; i < GlowCode.Length; i++) WriteInt(FileOffset(GlowCodeVa) + 4 * i, (int)GlowCode[i]);
            var marker = FileOffset(GlowMarkerVa);
            GlowMark.CopyTo(_data, marker);
            WriteInt(marker + 4, (int)GlowVersion);
            WriteInt(FileOffset(GlowFunctionVa), (int)JumpToGlow);
            WriteInt(FileOffset(GlowFunctionVa) + 4, 0);
        }

        for (var i = 0; i < GlowSettings.StyleCount; i++)
        {
            var at = FileOffset(GlowStylesVa) + 8 * i;
            var s = settings.Styles[i];
            _data[at] = (byte)s.Halo.Colour.R;
            _data[at + 1] = (byte)s.Halo.Colour.G;
            _data[at + 2] = (byte)s.Halo.Colour.B;
            _data[at + 3] = (byte)s.Halo.Strength;
            _data[at + 4] = (byte)s.Core.Colour.R;
            _data[at + 5] = (byte)s.Core.Colour.G;
            _data[at + 6] = (byte)s.Core.Colour.B;
            _data[at + 7] = (byte)s.Core.Strength;
        }

        for (var i = 0; i < GlowSettings.RuleCount; i++)
        {
            var rule = i < settings.Rules.Count ? settings.Rules[i] : new GlowRule(0, 0);
            BitConverter.GetBytes(rule.Bits).CopyTo(_data, FileOffset(GlowRuleBitsVa) + 8 * i);
            _data[FileOffset(GlowRuleStylesVa) + i] = rule.Style is { } style ? (byte)style : (byte)255;
        }

        WriteTableColours(settings.Styles[0].Halo.Colour, settings.Styles[0].Core.Colour);
    }

    /// <summary>
    /// Puts the game's own glow function and debug kit back and makes every item automatic again. The
    /// ENCHANT GLOW colours stay style 0's. Does nothing when there are no glow styles.
    /// </summary>
    public void RemoveGlowStyles()
    {
        RequireItems();
        var state = GetGlowStylesState(out var detail);
        if (state == GlowStylesState.Off) return;
        if (state == GlowStylesState.Unknown) throw new InvalidOperationException("Glow styles can't be removed: " + detail);

        for (var i = 0; i < GameDebugKit.Length; i++) WriteInt(FileOffset(GlowCodeVa) + 4 * i, (int)GameDebugKit[i]);
        WriteInt(FileOffset(GlowFunctionVa), (int)GameGlowStart[0]);
        WriteInt(FileOffset(GlowFunctionVa) + 4, (int)GameGlowStart[1]);
        var count = CountItems();
        for (var i = 0; i < count; i++) _data[FileOffset(ItemTableVa) + i * ItemRecordSize + ItemGlowOffset] = 0;
    }

    /// <summary>The items whose glow setting isn't automatic (they need glow styles).</summary>
    private List<int> ItemsWithGlowSettings()
    {
        var list = new List<int>();
        var count = CountItems();
        for (var i = 0; i < count; i++)
        {
            if (_data[FileOffset(ItemTableVa) + i * ItemRecordSize + ItemGlowOffset] != 0) list.Add(i);
        }

        return list;
    }

    /// <summary>Throws <see cref="ArgumentException"/> when an item has a glow setting but the executable has no glow styles.</summary>
    private void CheckItemGlows()
    {
        if (HasGlowStyles) return;
        if (ItemsWithGlowSettings() is { Count: > 0 } items)
        {
            throw new ArgumentException($"Item {items[0]} has a glow setting, but glow styles are off: turn them on (Glow tab) " +
                                        "or set the item back to Automatic.");
        }
    }

    private void WriteTableColours(Rgb halo, Rgb core)
    {
        var at = FileOffset(GlowVa);
        _data[at] = (byte)halo.R;
        _data[at + 1] = (byte)halo.G;
        _data[at + 2] = (byte)halo.B;
        _data[at + 8] = (byte)core.R;
        _data[at + 9] = (byte)core.G;
        _data[at + 10] = (byte)core.B;
    }
}
