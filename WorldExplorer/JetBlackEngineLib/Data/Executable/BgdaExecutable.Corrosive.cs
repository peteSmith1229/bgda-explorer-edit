// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.Corrosive.cs
//
// Corrosive and Corrosive Burst (SLES_506.72, PAL): two weapon modifiers with acid damage on property
// bits 59 and 60, which no item of the game uses. The Corrosive patch written here is venompatch.py
// version 3, byte for byte (the same words; tried in the game: the damage, names, prices and poison of
// version 2, then version 3 with the acid icon):
//   Corrosive (item text "acid"): every hit does 1-6 acid more, inside the hit, as Flaming does fire:
//     halved when the target resists acid (+0x3C & 0x40000), halved again with 0x10000, nothing when it
//     is immune (0x80000). Value +6/+6 and power +1, as Flaming (ItemFormulas).
//   Corrosive Burst ("acidburst"): the same 1-6, and on a critical hit (1-10) x (multiplier - 1) more,
//     the acid arrow's splash and 5 s of poison (1 s when it resists; never shortened), where Flaming
//     Burst sets the target alight. Value +8/+16 and power +2, as Flaming Burst.
//   The names "it_corrosive" / "it_corrosiveburst" in GTEXT.LMP (Corrosive.AddNames), +1/+6 on the
//   damage the item card and the character screen show, as for fire, and an acid drop on the item
//   card after the element icons: tinyacid.tex in INVENTRY.LMP (CorrosiveIcon), which the patch
//   looks up when the interface loads its icons (none shows when INVENTRY.LMP hasn't got it).
//
// WHERE IT GOES. A second program header (0x54: PT_LOAD, file 0x100-0xFFF -> VA 0x000FF100-0x000FFFFF,
// the same file-to-VA offset as the game's own segment) makes the zero padding of the ELF header code;
// nothing in the game or the three-player builds uses that memory:
//   0x000FF100  "VENM", u32 version (3); u64 bit 59, u64 bit 60
//   0x000FF118  s32 die (6), s32 critical-hit die (10); f32 poison seconds (5), when resisted (1)
//   0x000FF128  f32 value bonuses: Corrosive's min and max (6, 6), Corrosive Burst's (8, 16)
//   0x000FF138  s32 power bonuses (1, 2)
//   0x000FF140  "acid", "acidburst", "it_corrosive", "it_corrosiveburst" (32 bytes each)
//   0x000FF1C0  u32 the acid icon's texture (0 in the file); 0x000FF1D0 "tinyacid.tex"
//   0x000FF1E0  the code (CorrosiveCode, 395 instructions)
// and 16 places in the game's code jump to it (CorrosiveHooks). Before it goes in, the hooks, 66 other
// instructions the code relies on and two strings are checked against the game's (the stock executable,
// ADRIANNA26 and the glow builds have them all), and the padding has to be empty. Versions 1 and 2 of
// venompatch.py are recognised (Outdated) and replaced, keeping version 2's dice and poison times.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Text;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>Whether an executable has the Corrosive patch.</summary>
public enum CorrosiveState
{
    /// <summary>Not in; it can be put in.</summary>
    Off,

    /// <summary>Version 3 is in (the acid icon included).</summary>
    On,

    /// <summary>Version 1 or 2 is in: it works, and is replaced by version 3 when the patch is written.</summary>
    Outdated,

    /// <summary>Something else is where the patch goes, or the code it relies on isn't the game's: it can't be used.</summary>
    Unknown
}

/// <summary>The settings of the Corrosive patch: the dice and the poison of a Corrosive Burst critical hit.</summary>
public sealed record CorrosiveSettings(int Die, int CriticalDie, float PoisonSeconds, float ResistedPoisonSeconds)
{
    public const int MaxDie = 1000;
    public const float MaxPoisonSeconds = 60f;

    /// <summary>What was tried in the game: 1-6 acid a hit, 1-10 a step of the critical multiplier, 5 s of poison (1 s when resisted).</summary>
    public static CorrosiveSettings Default { get; } = new(6, 10, 5f, 1f);

    /// <summary>Why these can't be written, or null.</summary>
    public string? Problem()
    {
        if (Die is < 1 or > MaxDie) return $"The acid of every hit: a die of 1 to {MaxDie}.";
        if (CriticalDie is < 1 or > MaxDie) return $"The acid of a critical hit: a die of 1 to {MaxDie}.";
        if (!float.IsFinite(PoisonSeconds) || PoisonSeconds is < 0f or > MaxPoisonSeconds)
            return $"The poison lasts 0 to {MaxPoisonSeconds:0} seconds.";
        if (!float.IsFinite(ResistedPoisonSeconds) || ResistedPoisonSeconds < 0f || ResistedPoisonSeconds > PoisonSeconds)
            return "The poison when the target resists acid lasts 0 seconds up to the poison's own time.";
        return null;
    }
}

/// <summary>Corrosive and Corrosive Burst: their property bits and their names in GTEXT.LMP.</summary>
public static class Corrosive
{
    /// <summary>Bits 59 (Corrosive) and 60 (Corrosive Burst).</summary>
    public const ulong Bits = 1UL << ItemBits.Corrosive | 1UL << ItemBits.CorrosiveBurst;

    /// <summary>True when <paramref name="flags"/> has Corrosive or Corrosive Burst.</summary>
    public static bool Uses(ulong flags) => (flags & Bits) != 0;

    /// <summary>
    /// The names in each language of GTEXT.LMP (0 English ... 4 Spanish), in the forms the game's naming code asks for
    /// (FUN_00171818: the key in the item name's gender, _m/_f, then _M/_F, then the plain key), like Flaming's and Flaming
    /// Burst's: the same texts gtext_corrosive.py writes, tried in the game.
    /// </summary>
    public static IReadOnlyList<(int Language, string Key, string Text)> Names { get; } = new[]
    {
        (0, "it_corrosive", "Corrosive"), (0, "it_corrosiveburst", "Corrosive Burst"),
        (1, "it_corrosive", "corrosif"), (1, "it_corrosive_M", "corrosif"), (1, "it_corrosive_F", "corrosive"),
        (1, "it_corrosiveburst", "de vitriol"),
        (2, "it_corrosive", "der Säure"), (2, "it_corrosiveburst", "des Säureinfernos"),
        (3, "it_corrosive", "Corrosivo"), (3, "it_corrosive_m", "Corrosivo"), (3, "it_corrosive_f", "Corrosiva"),
        (3, "it_corrosiveburst", "Esplosivo Corrosivo"), (3, "it_corrosiveburst_m", "Esplosivo Corrosivo"),
        (3, "it_corrosiveburst_f", "Esplosiva Corrosiva"),
        (4, "it_corrosive", "corrosivo"), (4, "it_corrosive_m", "corrosivo"), (4, "it_corrosive_f", "corrosiva"),
        (4, "it_corrosiveburst", "explosivo corrosivo"), (4, "it_corrosiveburst_m", "explosivo corrosivo"),
        (4, "it_corrosiveburst_f", "explosiva corrosiva")
    };

    /// <summary>True when <paramref name="text"/> has every one of <see cref="Names"/> as written here.</summary>
    public static bool HasNames(GameText text) => Names.All(n => text.Get(n.Key, n.Language) == n.Text);

    /// <summary>Puts <see cref="Names"/> in <paramref name="text"/> (a key it has already is changed in place, a new one goes at the end).</summary>
    public static void AddNames(GameText text)
    {
        foreach (var (language, key, name) in Names) text.SetLanguageText(language, key, name);
    }

    /// <summary>Takes the keys of <see cref="Names"/> out of <paramref name="text"/>.</summary>
    public static void RemoveNames(GameText text)
    {
        foreach (var (language, key, _) in Names) text.RemoveLanguageText(language, key);
    }

    /// <summary>The acid glow: a yellow-green halo (60, 110, 0) and core (190, 255, 40) at the game's strengths, 64 and 72.</summary>
    public static GlowStyle AcidGlow { get; } = new(new GlowLayer(new Rgb(60, 110, 0), GlowStyle.GameHaloStrength),
                                                    new GlowLayer(new Rgb(190, 255, 40), GlowStyle.GameCoreStrength));

    /// <summary>
    /// Glow settings with an acid style and the rule "Corrosive or Corrosive Burst: acid" first, the other rules moved down
    /// (what glow_acid_rule.py writes). The style (<paramref name="style"/>) is <paramref name="useStyle"/> when given, otherwise
    /// the first after style 0 that no rule uses and that still has style 0's colours, as every style after it has. Throws
    /// <see cref="InvalidOperationException"/> when all 16 rules are in use, a rule already looks for Corrosive or Corrosive
    /// Burst, or there is no such style.
    /// </summary>
    public static GlowSettings WithAcidGlow(GlowSettings settings, out int style, int? useStyle = null)
    {
        if (settings.Rules.Count >= GlowSettings.RuleCount)
            throw new InvalidOperationException($"All {GlowSettings.RuleCount} glow rules are in use: remove one first.");
        if (settings.Rules.FirstOrDefault(r => (r.Bits & Bits) != 0) is { Bits: not 0 } rule)
            throw new InvalidOperationException($"Glow rule {settings.Rules.ToList().IndexOf(rule) + 1} already looks for Corrosive or Corrosive Burst.");
        var styles = settings.Styles.ToList();
        if (useStyle is { } given)
        {
            if (given is < 0 or >= GlowSettings.StyleCount) throw new ArgumentOutOfRangeException(nameof(useStyle), "A style is 0 to 15.");
            style = given;
        }
        else
        {
            var taken = settings.Rules.Where(r => r.Style.HasValue).Select(r => r.Style!.Value).Append(0).ToHashSet();
            style = Enumerable.Range(1, GlowSettings.StyleCount - 1)
                              .FirstOrDefault(i => !taken.Contains(i) && styles.Skip(i).All(s => s == styles[0]), -1);
            if (style < 0)
                throw new InvalidOperationException("No glow style is free for the acid glow (one after the styles in use, still in style 0's colours).");
        }

        styles[style] = AcidGlow;
        return new GlowSettings(styles, settings.Rules.Prepend(new GlowRule(Bits, style)));
    }
}

/// <summary>The item card's acid drop: tinyacid.tex, which the Corrosive patch looks up in INVENTRY.LMP.</summary>
public static class CorrosiveIcon
{
    public const string EntryName = "tinyacid.tex";

    /// <summary>
    /// The texture, 1,760 bytes: a 16 x 16 yellow-green drop with bubbles, in the layout of the game's tinyab.tex (its header,
    /// GIF tags and transfers; 8-bit with a 256-colour palette, the rows stored as two fields, colours at half brightness) -
    /// what make_tinyacid.py makes, tried in the game.
    /// </summary>
    public static byte[] Texture() => Convert.FromBase64String(TextureBase64);

    /// <summary>Why <paramref name="inventry"/> isn't an INVENTRY.LMP the drop can go in or come out of, or null.</summary>
    public static string? Problem(byte[] inventry)
    {
        List<GameArchive.Entry> entries;
        try
        {
            entries = GameArchive.ReadDirectory(inventry);
        }
        catch (InvalidDataException e)
        {
            return e.Message;
        }

        if (!entries.Any(e => e.Name == "tinyab.tex") || !entries.Any(e => e.Name == "tinycritical.tex"))
            return "It isn't INVENTRY.LMP: it has no tinyab.tex and tinycritical.tex (the item card's icons).";
        return GameArchive.Problem(inventry);
    }

    /// <summary>True when <paramref name="inventry"/> has tinyacid.tex.</summary>
    public static bool IsIn(byte[] inventry) => GameArchive.ReadDirectory(inventry).Any(e => e.Name == EntryName);

    /// <summary>
    /// INVENTRY.LMP with tinyacid.tex added where the game finds it (GameArchive.Insert: between tinyab.tex and tinycritical.tex,
    /// its file before tinycritical.tex's); the same bytes when it is there already. Throws <see cref="InvalidDataException"/>
    /// when it isn't an INVENTRY.LMP the game reads as it should.
    /// </summary>
    public static byte[] AddTo(byte[] inventry)
    {
        if (Problem(inventry) is { } problem) throw new InvalidDataException(problem);
        return IsIn(inventry) ? inventry : GameArchive.Insert(inventry, EntryName, Texture());
    }

    /// <summary>INVENTRY.LMP without tinyacid.tex: as it was before <see cref="AddTo"/>, byte for byte.</summary>
    public static byte[] RemoveFrom(byte[] inventry)
    {
        if (Problem(inventry) is { } problem) throw new InvalidDataException(problem);
        return IsIn(inventry) ? GameArchive.Remove(inventry, EntryName) : inventry;
    }

    private const string TextureBase64 =
        "EAAQAAUAZgBEEAAAwkQUAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA" +
        "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAFSAGGw8aBAQRHAUHLjwPcCEuCkUkMQtN" +
        "V2oefzFBEHBedSB/Xncff1p0HX9HYBZ/HSwIRSIwC09SaBt/P1EVfhcjBxwaJggjRFYXf2N6In9ach5/KjsNaxEcBAhcdR5/WHMcf1RxGn9Qbhh/" +
        "MUgOfhQhBRwYJQclP1MVf192KX9Ycx5/UnAZf05tF39KaxR/Q2QRfyE1CG0NGQMFQ2YRfzZUDX8XJwVCGCUHLENbFX9acyd/e39kf2Z2Qn8OGQME" +
        "LD0OcVpxJn95fl9/Znc+f0xsFX9IahN/RGcRf0ZpEn9DZxF/Tmoif19xPX87Xg5/IDUHdwsWAgIeLghHSWUVf01sF39YcCx/Smoaf0JmEH9AYw9/" +
        "PWAPfztdD39DYhh/V2szf0xkJ380VQx/JD0Ifw0ZAxYWJQU0OVcOfzhaDX8mPwl/DRkDFB0tCEpEYxN/RmgSf0FlEH8/Yg9/O10Of1NpMH87XBB/" +
        "VGgyfzBPC38cMQZ8CxYCBQ8bAwklPAl5PF4Of0JgGX9WajV/P1saf1JmMn9GXCZ/KEQIfxcqBXENGQMLEyIELh4yB3YoQwl/KkYJfydCCH8fNgZ/" +
        "FCQETxUkBTApQwp/N1gNf0NeHX9aazt/NVMRfy1MCn8UJQRhDRoDEAwYAwYQHgMrEiEEPhEfAzkOGgMbAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAYAAAAAAABAOAAAA" +
        "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAACAAAAAgAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA" +
        "EIAAAAAAAAgAAAAAAAAAAAAPAgAAEAAAABEAAAASAAAAAAATAAAAFAAAABUBAAAAAB8IJwAgCQAAIQAAACIAAAAAACMAAAAkBgAAJQceACY1AD1k" +
        "NgA+ZTdfAGY4YABnAEY5aABhOgAzYjsANGM8AE0AVHY5AFV3TgAAeE8AAHkAclAAAHNRAEt0UgBMdVMAABYFAAAXAAAAGAAAABkAAAAAABoAAAAb" +
        "AwAAHAQAAB0AKg0yACsOAAAsAgAALQAAAAAALgoAAC8LKAAwDCkAMUEASW5CAEpvQwAAZ0RpAHAAakVxAGtGAD9sRwBAbUgARABdfUUAXn5GAAAA" +
        "WAAAAAAAWQAAeloAVntbAFd8XAAIgAAAAAAAEA4AAAAAAAAAAAAAAAAAAABiAAAAAAAAAAAAAAAAAAAAYgAAAAAAAAAAAAAAAAAAAGIAAAAAAAAA" +
        "AAAAAAAAAABiAAAAAAAAAAAAAAAAAAAAYgAAAAAAAAAAAAAAAAAAAGIAAAAAAAAAAAAAAAAAAABiAAAAAAAAAAAAAAAAAAAAYgAAAAAAAAA=";
}

public partial class BgdaExecutable
{
    /// <summary>Where the Corrosive patch's segment is loaded: the ELF header's padding (file 0x100-0xFFF).</summary>
    public const int CorrosiveSegmentVa = 0x000FF100, CorrosiveSegmentEndVa = 0x00100000;

    /// <summary>Its data: "VENM", the version, the bits, the settings and the texts; the code starts at <see cref="CorrosiveCodeVa"/>.</summary>
    public const int CorrosiveDataVa = CorrosiveSegmentVa, CorrosiveCodeVa = 0x000FF1E0;

    /// <summary>The version of the Corrosive patch WorldExplorer writes (venompatch.py version 3).</summary>
    public const int CorrosivePatchVersion = 3;

    private const int ElfProgramHeaderOffset = 0x1C, ElfProgramHeaderSize = 0x2A, ElfProgramHeaderCount = 0x2C;
    private const int FirstProgramHeader = 0x34, SecondProgramHeader = 0x54, ElfPaddingEnd = 0x1000;

    // the data, from CorrosiveDataVa
    private const int CorVersion = 0x04, CorMaskC = 0x08, CorMaskB = 0x10, CorDie = 0x18, CorCriticalDie = 0x1C, CorPoison = 0x20,
        CorPoisonResisted = 0x24, CorValueC = 0x28, CorValueB = 0x30, CorPowerC = 0x38, CorPowerB = 0x3C, CorTexts = 0x40,
        CorTextRoom = 32, CorIcon = 0xC0, CorIconName = 0xD0, CorIconNameRoom = 16;

    private static readonly byte[] CorrosiveMark = { (byte)'V', (byte)'E', (byte)'N', (byte)'M' };

    /// <summary>The second program header: PT_LOAD, file 0x100, VA 0x000FF100 (twice), 0xF00 bytes in the file and in memory, RWX, 0x1000.</summary>
    private static readonly uint[] CorrosiveProgramHeader = { 1, 0x100, 0x000FF100, 0x000FF100, 0xF00, 0xF00, 7, 0x1000 };

    /// <summary>The value bonuses (as Flaming's 6/6 and Flaming Burst's 8/16) and power bonuses (as theirs, 1 and 2) of the data.</summary>
    private static readonly float[] CorrosiveValueBonuses = { 6f, 6f, 8f, 16f };

    private static readonly int[] CorrosivePowerBonuses = { 1, 2 };

    /// <summary>The item text words and GTEXT keys of the data, 32 bytes each from 0x000FF140.</summary>
    private static readonly string[] CorrosiveTexts = { "acid", "acidburst", "it_corrosive", "it_corrosiveburst" };

    /// <summary>The strings the code uses (the acid arrow's effect), checked before it goes in.</summary>
    private static readonly (int Va, string Text)[] CorrosiveStrings = { (0x00324058, "fx.lmp"), (0x00311188, "spel_hit_acid.anm") };

    /// <summary>One place in the game's code that jumps to the patch: the game's words there and the words version 3 writes.</summary>
    private sealed record CorrosiveHook(string Name, int Va, uint[] Game, uint[] Patched);

    /// <summary>The hooks of the earlier versions (each began with a jump into the segment, then the same words as version 3).</summary>
    private static readonly string[] CorrosiveVersion1Hooks = { "arrow", "bow", "melee", "name", "value1", "value2", "power", "parse" },
        CorrosiveVersion2Hooks = { "arrow", "arrow2", "arrow3", "bow", "melee", "burst", "hit", "name", "value1", "value2", "power", "parse", "card", "stats" };

    // 395 instructions at 0x000FF1E0-0x000FF80B, assembled by venompatch.py version 3
    private static readonly uint[] CorrosiveCode =
    {
        0x3C196000, 0x01595025, 0x01595026, 0x1200000F, 0x00000000, 0x3C190010, 0x2739F100, 0xDE180008,
        0xDF2F0008, 0xDF390010, 0x01F97825, 0x030F7824, 0x11E00006, 0x0319C824, 0x3C0F4000, 0x13200003,
        0x014F5025, 0x3C0F2000, 0x014F5025, 0x08047E0E, 0x00000000, 0x3C196000, 0x0159C024, 0x13000012,
        0x00000000, 0x8CB8000C, 0x3C0F0008, 0x030FC024, 0x1300000B, 0x00000000, 0x8CB8003C, 0x13000008,
        0x00000000, 0x8F18000C, 0x13000005, 0x00000000, 0x8F18000C, 0x33180020, 0x17000003, 0x00000000,
        0x01595025, 0x01595026, 0x08047E0E, 0x00000000, 0x3C190010, 0x2739F100, 0xDF380008, 0xDF390010,
        0x03190825, 0x00410824, 0x0059C824, 0x0002103C, 0x0002103F, 0x3C186000, 0x00581025, 0x00581026,
        0x10200005, 0x3C184000, 0x00581025, 0x13200002, 0x3C182000, 0x00581025, 0x03E00008, 0x00000000,
        0x3C194000, 0x02F9C824, 0x13200023, 0x8E27003C, 0x3C190008, 0x00F9C824, 0x1720001F, 0x00000000,
        0x3C190010, 0x2739F100, 0x8F84A49C, 0x3C020019, 0x3442660D, 0x3C033C6E, 0x00822018, 0x3463F35F,
        0x8F250018, 0x00832021, 0x00041402, 0xAF84A49C, 0x0045001B, 0x00000000, 0x00000000, 0x00001810,
        0x24630001, 0x3C180004, 0x00F8C024, 0x13000003, 0x00000000, 0x24630001, 0x00031843, 0x3C180001,
        0x00F8C024, 0x13000003, 0x00000000, 0x24630001, 0x00031843, 0x0283A021, 0x8FA200BC, 0x10400003,
        0x00000000, 0x08048176, 0x00000000, 0x08048164, 0x00000000, 0x3C192000, 0x02F9C824, 0x1320003C,
        0x8E27003C, 0x3C190008, 0x00F9C824, 0x17200038, 0x3C180004, 0x3C190010, 0x2739F100, 0x00F8C024,
        0xC7200020, 0x13000002, 0xC621028C, 0xC7200024, 0x46000834, 0x00000000, 0x45000002, 0x00000000,
        0xE620028C, 0x3C040032, 0x3C050031, 0x24844058, 0x0C04EA12, 0x24A51188, 0x3C013F80, 0x44816000,
        0x0040202D, 0x0C07C9D0, 0x0220282D, 0x3C190010, 0x2739F100, 0x8F84A49C, 0x3C020019, 0x3442660D,
        0x3C033C6E, 0x00822018, 0x3463F35F, 0x8F25001C, 0x00832021, 0x00041402, 0xAF84A49C, 0x0045001B,
        0x00000000, 0x00000000, 0x00001810, 0x24630001, 0x2607FFFF, 0x00671818, 0x8E27003C, 0x3C180004,
        0x00F8C024, 0x13000003, 0x00000000, 0x24630001, 0x00031843, 0x3C180001, 0x00F8C024, 0x13000003,
        0x00000000, 0x24630001, 0x00031843, 0x0283A021, 0x32F94000, 0x13200003, 0x24020010, 0x080480EF,
        0x00000000, 0x080480FE, 0x00000000, 0x27BDFFE0, 0xFFBF0000, 0xFFA40008, 0x0C0899A8, 0x00000000,
        0x10400014, 0xDFA40008, 0x3C050010, 0x0C0899A8, 0x24A5F140, 0x10400007, 0x24180008, 0xDFA40008,
        0x3C050010, 0x0C0899A8, 0x24A5F160, 0x14400009, 0x24180010, 0x3C190010, 0x2739F100, 0x0338C821,
        0xDF380000, 0xDE080008, 0x01184025, 0xFE080008, 0x24020001, 0xDFBF0000, 0x03E00008, 0x27BD0020,
        0x27BDFFE0, 0xFFA60000, 0x3C190010, 0x2739F100, 0xDF380008, 0x00D8C024, 0x13000004, 0x3C040010,
        0x0C03FD64, 0x2484F180, 0xDFA60000, 0x3C190010, 0x2739F100, 0xDF380010, 0x00D8C024, 0x13000004,
        0x3C040010, 0x0C03FD64, 0x2484F1A0, 0xDFA60000, 0x27BD0020, 0x30C2000F, 0x14400003, 0x00000000,
        0x0805CEF3, 0x00000000, 0x0805CEEA, 0x00000000, 0x27BDFFE0, 0xFFBF0000, 0xFFA40008, 0x3C04005E,
        0x2484C368, 0x3C050032, 0x0C04D878, 0x24A54CD0, 0x0280202D, 0x0C05C606, 0xDFA50008, 0x3C04005E,
        0x2484C368, 0x0C04D890, 0x0040282D, 0xDFBF0000, 0x03E00008, 0x27BD0020, 0x020F8021, 0x30F80810,
        0x13000003, 0x00000000, 0x26100001, 0x26310006, 0x3C190010, 0x2739F100, 0xDF380008, 0xDF2F0010,
        0x030FC025, 0x00F8C024, 0x13000004, 0x00000000, 0x8F380018, 0x26100001, 0x02388821, 0x03E00008,
        0x00000000, 0x30980100, 0x13000004, 0x3C0140C0, 0x44810000, 0x4600A500, 0x4600AD40, 0x3C190010,
        0x2739F100, 0xDF380008, 0x0098C024, 0x13000005, 0x00000000, 0xC7200028, 0xC721002C, 0x4600AD40,
        0x4601A500, 0xDF380010, 0x0098C024, 0x13000005, 0x00000000, 0xC7200030, 0xC7210034, 0x4600AD40,
        0x4601A500, 0x03E00008, 0x00000000, 0x3C014220, 0x44810000, 0x3C020080, 0x34427BDF, 0x00621024,
        0x3C190010, 0x2739F100, 0xDF380008, 0xDF390010, 0x0319C025, 0x0078C024, 0x03E00008, 0x00581025,
        0x27BDFFE0, 0xFFBF0000, 0xFFA40008, 0x0C03FDC7, 0x00000000, 0xDFA40008, 0xDC880008, 0x3C190010,
        0x2739F100, 0xDF380008, 0x0118C024, 0x13000003, 0x00000000, 0x8F290038, 0x00491021, 0xDF380010,
        0x0118C024, 0x13000003, 0xDFBF0000, 0x8F29003C, 0x00491021, 0x03E00008, 0x27BD0020, 0x27BDFFF0,
        0x0805D028, 0x3C0C0080, 0x27BDFFE0, 0xFFA20000, 0x8F84C0CC, 0x3C050010, 0x0C04EB96, 0x24A5F1D0,
        0x10400002, 0x00000000, 0x8C420038, 0x3C190010, 0x2739F100, 0xAF2200C0, 0xDFA20000, 0x27BD0020,
        0x0805B7E2, 0xDFBF0140, 0x27BDFFC0, 0xFFBF0030, 0xDEB80008, 0x3C190010, 0x2739F100, 0xDF2F0008,
        0xDF2E0010, 0x01EE7825, 0x030F7824, 0x11E0001B, 0x8F2400C0, 0x10800019, 0x330F1040, 0x000F782B,
        0x02CF7821, 0x000F7100, 0x000F7880, 0x01EE7821, 0x8FA50084, 0x24A5005E, 0x00AF2821, 0x27C60004,
        0x240E027F, 0xAFAE0000, 0x240E01FF, 0xAFAE0008, 0xAFA00010, 0xAFA00018, 0x3C098000, 0x02694825,
        0x02547025, 0x012E4825, 0x24070004, 0x24080001, 0x0000502D, 0x0C050484, 0x240B005C, 0xDFBF0030,
        0x27BD0040, 0x03E00008, 0x3C03002A
    };

    /// <summary>The hooks: where, the game's words there, and the words version 3 writes (the first jumps into the code).</summary>
    private static readonly CorrosiveHook[] CorrosiveHooks =
    {
        new("arrow", 0x00110AA4, new uint[] { 0x0C047E0E }, new uint[] { 0x0C03FC8D }),
        new("arrow2", 0x001A9930, new uint[] { 0x0C047E0E }, new uint[] { 0x0C03FC8D }),
        new("arrow3", 0x001A9958, new uint[] { 0x0C047E0E }, new uint[] { 0x0C03FC8D }),
        new("bow", 0x0015EE50, new uint[] { 0x0002103C, 0x0002103F }, new uint[] { 0x0C03FCA4, 0x00000000 }),
        new("melee", 0x00167B04, new uint[] { 0x0C047E0E }, new uint[] { 0x0C03FC78 }),
        new("burst", 0x001203B4, new uint[] { 0x10400010 }, new uint[] { 0x0803FCE5 }),
        new("hit", 0x00120588, new uint[] { 0x14400013 }, new uint[] { 0x0803FCB8 }),
        new("name", 0x00173B9C, new uint[] { 0x30C2000F, 0x1040000A }, new uint[] { 0x0803FD48, 0x00000000 }),
        new("value1", 0x00173D0C, new uint[] { 0x10400005, 0x3C020080, 0x3C0140C0, 0x44810000, 0x4600A500, 0x4600AD40 }, new uint[] { 0x0C03FD89, 0x00000000, 0x3C020080, 0x00000000, 0x00000000, 0x00000000 }),
        new("value2", 0x00173E14, new uint[] { 0x3C014220, 0x44810000, 0x3C020080, 0x34427BDF, 0x00621024 }, new uint[] { 0x0C03FDA3, 0x00000000, 0x00000000, 0x00000000, 0x00000000 }),
        new("power", 0x00174098, new uint[] { 0x27BDFFF0, 0x3C0C0080 }, new uint[] { 0x0803FDB0, 0x00000000 }),
        new("parse", 0x00174F80, new uint[] { 0x0C0899A8 }, new uint[] { 0x0C03FD2B }),
        new("card", 0x001770F0, new uint[] { 0x30E20810, 0x10400003, 0x02038021, 0x26100001, 0x26310006 }, new uint[] { 0x0C03FD76, 0x0060782D, 0x00000000, 0x00000000, 0x00000000 }),
        new("stats", 0x00178910, new uint[] { 0x10A00003, 0x02028021, 0x26100001, 0x26310006 }, new uint[] { 0x0C03FD76, 0x0040782D, 0x00000000, 0x00000000 }),
        new("icon_load", 0x0016DF80, new uint[] { 0xDFBF0140 }, new uint[] { 0x0C03FDCA }),
        new("icon_draw", 0x00177344, new uint[] { 0x3C03002A }, new uint[] { 0x0C03FDDA })
    };

    /// <summary>The game's instructions the code relies on, checked before it goes in (venompatch.py's CHECKS).</summary>
    private static readonly (int Va, uint Word)[] CorrosiveChecks =
    {
        (0x0011F844, 0x0140B82D), (0x0011F86C, 0x0080882D), (0x001200A8, 0x3C020008), (0x001200AC, 0x240A0001),
        (0x001200B0, 0xAFAA00BC), (0x001200BC, 0x24100002), (0x00120118, 0x0290A018), (0x00120134, 0x3C040032),
        (0x0012013C, 0x24844058), (0x00120144, 0x0C04EA12), (0x00120150, 0x4600A306), (0x00120154, 0x0C07C9D0),
        (0x00120158, 0x0220282D), (0x001202F0, 0xE620028C), (0x001203B0, 0x32E24000), (0x001203B8, 0x24020010),
        (0x001203BC, 0x82230158), (0x001203F8, 0x32E20810), (0x00120414, 0x8F84A49C), (0x00120584, 0x8FA200BC),
        (0x0012058C, 0x0000402D), (0x00120590, 0x8F84A49C), (0x001205D8, 0x32A20100), (0x0016D8CC, 0xAF82C0CC),
        (0x0016DF60, 0x8F84C0CC), (0x0016DF68, 0x0C04EB96), (0x0016DF70, 0x8C430038), (0x0016DF84, 0xDFBE0130),
        (0x0016DF88, 0xDFB70120), (0x0016DFA8, 0x03E00008), (0x0016DFAC, 0x27BD0150), (0x00177084, 0x00C0F02D),
        (0x00177094, 0x0000B02D), (0x0017709C, 0x00E0A82D), (0x001770A4, 0x0100982D), (0x001770D0, 0xAFA50044),
        (0x001771A0, 0x00139400), (0x001771A8, 0x0013A200), (0x001771E8, 0x24160001), (0x001771F4, 0x00139400),
        (0x001771F8, 0x0013A200), (0x00177264, 0x26D60001), (0x001772D4, 0x26D60001), (0x001772DC, 0x30621040),
        (0x001772E0, 0x10400018), (0x001772E8, 0x24050014), (0x001772F0, 0x02C52818), (0x001772F8, 0x240601FF),
        (0x001772FC, 0x2403027F), (0x00177308, 0x02694825), (0x0017730C, 0x8FA30044), (0x00177310, 0x02541025),
        (0x00177314, 0x24A5005E), (0x00177328, 0x27C60004), (0x00177330, 0x24070004), (0x00177334, 0x24080001),
        (0x00177338, 0x0000502D), (0x0017733C, 0x0C050484), (0x00177340, 0x240B005C), (0x00177348, 0x3C108000),
        (0x0017734C, 0x24625CB8), (0x00190CB4, 0x3C040032), (0x00190CB8, 0x3C050031), (0x00190CC0, 0x0C04EA12),
        (0x00190CC4, 0x24A51188), (0x00190CD4, 0x0C07C9D0)
    };

    // ═══════════════════════════════ state ═══════════════════════════════

    private int CorrosiveAt(int offset) => FileOffset(CorrosiveDataVa) + offset;

    /// <summary>The first hook, instruction or string the patch relies on that isn't the game's, or null.</summary>
    private string? CorrosiveGameCodeProblem(IEnumerable<CorrosiveHook> hooks)
    {
        foreach (var h in hooks)
        {
            for (var i = 0; i < h.Game.Length; i++)
            {
                if (ReadU32(FileOffset(h.Va) + 4 * i) != h.Game[i]) return $"the game's code at 0x{h.Va + 4 * i:X8} has been changed";
            }
        }

        foreach (var (va, word) in CorrosiveChecks)
        {
            if (ReadU32(FileOffset(va)) != word) return $"the game's code at 0x{va:X8} has been changed";
        }

        foreach (var (va, text) in CorrosiveStrings)
        {
            var bytes = Encoding.ASCII.GetBytes(text + "\0");
            if (!_data.AsSpan(FileOffset(va), bytes.Length).SequenceEqual(bytes)) return $"\"{text}\" isn't at 0x{va:X8}";
        }

        return null;
    }

    /// <summary>The version of the patch in the file (0: none), read from its data without checking anything else.</summary>
    private uint CorrosiveVersionInFile() =>
        ReadU16(ElfProgramHeaderCount) == 2 && _data.AsSpan(CorrosiveAt(0), 4).SequenceEqual(CorrosiveMark) ? ReadU32(CorrosiveAt(CorVersion)) : 0;

    /// <summary>The hooks an installed version wrote, or null when one of them isn't what that version writes.</summary>
    private List<CorrosiveHook>? InstalledCorrosiveHooks(uint version)
    {
        var names = version switch
        {
            1 => CorrosiveVersion1Hooks,
            2 => CorrosiveVersion2Hooks,
            _ => CorrosiveHooks.Select(h => h.Name).ToArray()
        };
        var hooks = CorrosiveHooks.Where(h => names.Contains(h.Name)).ToList();
        foreach (var h in hooks)
        {
            var at = FileOffset(h.Va);
            var first = ReadU32(at);
            if (version == CorrosivePatchVersion ? first != h.Patched[0]
                    : first >> 26 is not (2 or 3) || (first & 0x03FFFFFF) << 2 is < CorrosiveSegmentVa or >= CorrosiveSegmentEndVa)
                return null;
            for (var i = 1; i < h.Patched.Length; i++)
            {
                if (ReadU32(at + 4 * i) != h.Patched[i]) return null;
            }
        }

        return hooks;
    }

    /// <summary>Whether this executable has the Corrosive patch; <paramref name="detail"/> says why in a sentence.</summary>
    public CorrosiveState GetCorrosiveState(out string detail)
    {
        if (_data.Length < FileOffset(0x00324058) + 32)
        {
            detail = "The file is too short to be this build.";
            return CorrosiveState.Unknown;
        }

        if (ReadU32(ElfProgramHeaderOffset) != FirstProgramHeader || ReadU16(ElfProgramHeaderSize) != 32 ||
            ReadU32(FirstProgramHeader) != 1 || ReadU32(FirstProgramHeader + 8) - ReadU32(FirstProgramHeader + 4) != VaToFile)
        {
            detail = "The executable's program header isn't the game's (one segment, file 0x1000 at 0x00100000), so the Corrosive patch can't go in.";
            return CorrosiveState.Unknown;
        }

        var count = ReadU16(ElfProgramHeaderCount);
        if (count == 1)
        {
            if (_data.AsSpan(SecondProgramHeader, ElfPaddingEnd - SecondProgramHeader).IndexOfAnyExcept((byte)0) >= 0)
            {
                detail = "The ELF header's padding (file 0x54-0xFFF), where the Corrosive patch goes, isn't empty: something else uses it.";
                return CorrosiveState.Unknown;
            }

            if (CorrosiveGameCodeProblem(CorrosiveHooks) is { } changed)
            {
                detail = $"The Corrosive patch can't go in: {changed} (another patch?).";
                return CorrosiveState.Unknown;
            }

            detail = "No Corrosive patch: it goes in when the executable is saved with Corrosive or Corrosive Burst in use.";
            return CorrosiveState.Off;
        }

        var version = CorrosiveVersionInFile();
        if (count != 2 || version == 0 || !CorrosiveProgramHeader.Select((w, i) => ReadU32(SecondProgramHeader + 4 * i) == w).All(x => x))
        {
            detail = "The executable has a second program header that isn't the Corrosive patch's: something else uses the ELF header's padding.";
            return CorrosiveState.Unknown;
        }

        if (version > CorrosivePatchVersion || version == 0)
        {
            detail = $"The Corrosive patch in the executable is version {version}, which WorldExplorer doesn't know.";
            return CorrosiveState.Unknown;
        }

        var hooks = InstalledCorrosiveHooks(version);
        var others = CorrosiveHooks.Where(h => hooks == null || !hooks.Contains(h));
        if (hooks == null || CorrosiveGameCodeProblem(others) is not null)
        {
            detail = $"The Corrosive patch (version {version}) is in, but the code round it has been changed since: WorldExplorer leaves it alone.";
            return CorrosiveState.Unknown;
        }

        if (version < CorrosivePatchVersion)
        {
            detail = $"Version {version} of the Corrosive patch is in. Saving replaces it with version {CorrosivePatchVersion}, which adds the acid " +
                     "icon on the item card" + (version == 1 ? " and Corrosive Burst, and makes Corrosive work like Flaming" : "") + ".";
            return CorrosiveState.Outdated;
        }

        if (!CorrosiveDataIsOurs())
        {
            detail = "The Corrosive patch (version 3) is in, but its code or data aren't what WorldExplorer writes: it leaves them alone.";
            return CorrosiveState.Unknown;
        }

        detail = "The Corrosive patch (version 3) is in.";
        return CorrosiveState.On;
    }

    public CorrosiveState GetCorrosiveState() => GetCorrosiveState(out _);

    /// <summary>True when version 3 of the Corrosive patch is in.</summary>
    public bool HasCorrosive => GetCorrosiveState() == CorrosiveState.On;

    /// <summary>Why the Corrosive patch can't be used with this executable, or null (it is in, or can be put in).</summary>
    public string? CorrosiveProblem()
    {
        if (ItemsProblem() is { } items) return items;
        return GetCorrosiveState(out var detail) == CorrosiveState.Unknown ? detail : null;
    }

    /// <summary>
    /// The code, the bits, the value and power bonuses (which <see cref="ItemFormulas"/> assumes) and the texts of version 3
    /// are the ones written here, and the icon's texture is 0 as in every file (the game sets it in memory).
    /// </summary>
    private bool CorrosiveDataIsOurs()
    {
        if (ReadU64(CorrosiveAt(CorMaskC)) != 1UL << ItemBits.Corrosive || ReadU64(CorrosiveAt(CorMaskB)) != 1UL << ItemBits.CorrosiveBurst) return false;
        if (CorrosiveValueBonuses.Select((v, i) => BitConverter.ToSingle(_data, CorrosiveAt(CorValueC + 4 * i)) == v).Any(same => !same)) return false;
        if (ReadInt(CorrosiveAt(CorPowerC)) != CorrosivePowerBonuses[0] || ReadInt(CorrosiveAt(CorPowerB)) != CorrosivePowerBonuses[1]) return false;
        if (ReadU32(CorrosiveAt(CorIcon)) != 0) return false;
        for (var i = 0; i < CorrosiveTexts.Length; i++)
        {
            if (!TextIs(CorrosiveAt(CorTexts + CorTextRoom * i), CorTextRoom, CorrosiveTexts[i])) return false;
        }

        if (!TextIs(CorrosiveAt(CorIconName), CorIconNameRoom, CorrosiveIcon.EntryName)) return false;
        for (var i = 0; i < CorrosiveCode.Length; i++)
        {
            if (ReadU32(FileOffset(CorrosiveCodeVa) + 4 * i) != CorrosiveCode[i]) return false;
        }

        return true;
    }

    private bool TextIs(int at, int room, string text)
    {
        var bytes = new byte[room];
        Encoding.ASCII.GetBytes(text).CopyTo(bytes, 0);
        return _data.AsSpan(at, room).SequenceEqual(bytes);
    }

    // ═══════════════════════════════ settings ═══════════════════════════════

    /// <summary>The patch's settings: version 3's (and version 2's, which keeps them in the same place), otherwise the defaults.</summary>
    public CorrosiveSettings GetCorrosiveSettings()
    {
        var state = GetCorrosiveState();
        if (state is not (CorrosiveState.On or CorrosiveState.Outdated) || CorrosiveVersionInFile() < 2) return CorrosiveSettings.Default;
        return new CorrosiveSettings(ReadInt(CorrosiveAt(CorDie)), ReadInt(CorrosiveAt(CorCriticalDie)),
                                     BitConverter.ToSingle(_data, CorrosiveAt(CorPoison)), BitConverter.ToSingle(_data, CorrosiveAt(CorPoisonResisted)));
    }

    /// <summary>
    /// Puts the Corrosive patch in with these settings: version 3 as venompatch.py writes it, replacing version 1 or 2, or
    /// only the settings when version 3 is in. Throws <see cref="ArgumentException"/> for a bad setting and
    /// <see cref="InvalidOperationException"/> when the patch can't be used here; nothing changes then.
    /// </summary>
    public void SetCorrosive(CorrosiveSettings settings)
    {
        RequireItems();
        if (settings.Problem() is { } problem) throw new ArgumentException(problem);
        var state = GetCorrosiveState(out var detail);
        if (state == CorrosiveState.Unknown) throw new InvalidOperationException("The Corrosive patch can't be put in: " + detail);
        if (state == CorrosiveState.Outdated) TakeCorrosiveOut();
        if (state != CorrosiveState.On) PutCorrosiveIn();
        WriteInt(CorrosiveAt(CorDie), settings.Die);
        WriteInt(CorrosiveAt(CorCriticalDie), settings.CriticalDie);
        BitConverter.GetBytes(settings.PoisonSeconds).CopyTo(_data, CorrosiveAt(CorPoison));
        BitConverter.GetBytes(settings.ResistedPoisonSeconds).CopyTo(_data, CorrosiveAt(CorPoisonResisted));
    }

    /// <summary>
    /// Takes the Corrosive patch out (any version): the game's code back at every hook, the ELF header as the game has it.
    /// Does nothing when there is none; throws <see cref="InvalidOperationException"/> when what is there can't be recognised.
    /// </summary>
    public void RemoveCorrosive()
    {
        RequireItems();
        var state = GetCorrosiveState(out var detail);
        if (state == CorrosiveState.Off) return;
        if (state == CorrosiveState.Unknown) throw new InvalidOperationException("The Corrosive patch can't be taken out: " + detail);
        TakeCorrosiveOut();
    }

    /// <summary>Writes version 3 with the default settings, as venompatch.py does (the state is Off).</summary>
    private void PutCorrosiveIn()
    {
        for (var i = 0; i < CorrosiveProgramHeader.Length; i++) WriteInt(SecondProgramHeader + 4 * i, (int)CorrosiveProgramHeader[i]);
        WriteU16(ElfProgramHeaderCount, 2);
        CorrosiveMark.CopyTo(_data, CorrosiveAt(0));
        WriteInt(CorrosiveAt(CorVersion), CorrosivePatchVersion);
        BitConverter.GetBytes(1UL << ItemBits.Corrosive).CopyTo(_data, CorrosiveAt(CorMaskC));
        BitConverter.GetBytes(1UL << ItemBits.CorrosiveBurst).CopyTo(_data, CorrosiveAt(CorMaskB));
        var d = CorrosiveSettings.Default;
        WriteInt(CorrosiveAt(CorDie), d.Die);
        WriteInt(CorrosiveAt(CorCriticalDie), d.CriticalDie);
        BitConverter.GetBytes(d.PoisonSeconds).CopyTo(_data, CorrosiveAt(CorPoison));
        BitConverter.GetBytes(d.ResistedPoisonSeconds).CopyTo(_data, CorrosiveAt(CorPoisonResisted));
        for (var i = 0; i < CorrosiveValueBonuses.Length; i++) BitConverter.GetBytes(CorrosiveValueBonuses[i]).CopyTo(_data, CorrosiveAt(CorValueC + 4 * i));
        WriteInt(CorrosiveAt(CorPowerC), CorrosivePowerBonuses[0]);
        WriteInt(CorrosiveAt(CorPowerB), CorrosivePowerBonuses[1]);
        for (var i = 0; i < CorrosiveTexts.Length; i++) Encoding.ASCII.GetBytes(CorrosiveTexts[i]).CopyTo(_data, CorrosiveAt(CorTexts + CorTextRoom * i));
        WriteInt(CorrosiveAt(CorIcon), 0);
        Encoding.ASCII.GetBytes(CorrosiveIcon.EntryName).CopyTo(_data, CorrosiveAt(CorIconName));
        for (var i = 0; i < CorrosiveCode.Length; i++) WriteInt(FileOffset(CorrosiveCodeVa) + 4 * i, (int)CorrosiveCode[i]);
        foreach (var h in CorrosiveHooks)
        {
            for (var i = 0; i < h.Patched.Length; i++) WriteInt(FileOffset(h.Va) + 4 * i, (int)h.Patched[i]);
        }
    }

    /// <summary>Puts the game's words back at the installed version's hooks and empties the ELF header's padding (the state is On or Outdated).</summary>
    private void TakeCorrosiveOut()
    {
        var hooks = InstalledCorrosiveHooks(CorrosiveVersionInFile()) ??
                    throw new InvalidOperationException("The Corrosive patch's hooks have been changed since it was put in.");
        foreach (var h in hooks)
        {
            for (var i = 0; i < h.Game.Length; i++) WriteInt(FileOffset(h.Va) + 4 * i, (int)h.Game[i]);
        }

        Array.Clear(_data, SecondProgramHeader, ElfPaddingEnd - SecondProgramHeader);
        WriteU16(ElfProgramHeaderCount, 1);
    }

    // ═══════════════════════════════ what uses it ═══════════════════════════════

    /// <summary>
    /// What in this executable has Corrosive or Corrosive Burst: item records, shop entries and random-loot lists, in a few words
    /// each (empty when nothing does). Levels, scripts and starting kits name items too, but they aren't in the executable.
    /// </summary>
    public List<string> CorrosiveUses()
    {
        var list = new List<string>();
        if (ItemsProblem() != null) return list;
        var items = GetItems();
        foreach (var item in items.Where(i => Corrosive.Uses(i.Flags))) list.Add($"item {item.Index} ({item.Name})");
        if (ShopsProblem() == null)
        {
            foreach (var e in GetShopEntries())
            {
                if (Corrosive.Uses(ItemText.Parse(e.Text, items).Flags)) list.Add($"{ShopStockName(e.Stock)} ({e.Text})");
            }
        }

        foreach (var (lootList, _, _) in LootLists)
        {
            if (GetLootModifiers(lootList).Any(Corrosive.Uses)) list.Add($"the {lootList} loot list");
        }

        return list;
    }

    /// <summary>Throws <see cref="ArgumentException"/> when something has Corrosive or Corrosive Burst but the patch isn't in.</summary>
    private void CheckCorrosiveUse()
    {
        if (GetCorrosiveState() is CorrosiveState.On or CorrosiveState.Outdated) return;
        if (CorrosiveUses() is { Count: > 0 } uses)
        {
            throw new ArgumentException($"{char.ToUpperInvariant(uses[0][0])}{uses[0][1..]} has Corrosive or Corrosive Burst, which need the Corrosive " +
                                        "patch: put it in (Corrosive page) or take the modifier off.");
        }
    }
}
