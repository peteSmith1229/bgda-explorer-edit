// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.XantamFire.cs
//
// XANTAM'S FIRE (the Enhancements tab): Adrianna learns the beholder Xantam's fire trails, a new
// spell on her level-up screen (5 ranks), which burns the monsters in its path. It is
// featpatch.py version 4, written from the data in BgdaFeatPatch.cs (generated from that script),
// so the bytes are the ones tested in the emulator and in the game.
//
// WHERE IT GOES. The pool: 61 stretches of the game's own code that nothing ever runs (unused
// library functions and leftover routines, 32,724 bytes; see featpool.py). They are the same
// addresses and bytes in the stock executable and in every three-player build, the game's own
// segment loads them, and no other patch WorldExplorer knows writes there. Xantam's Fire takes two
// of them: its data at 0x00132D90 (0x600 bytes) and its code and two vtable copies at
// 0x00221B88-0x002222EF. Six words of the game's code send the game there. Nothing else changes:
// no program header, no start-up copy, the same file length.
//
// SO IT NEEDS NO THREE-PLAYER BUILD, and the Three Players tab can turn three players on or off
// underneath it: the three-player patch never touches these bytes, and the code finds the class
// lists (0x002A0478 in the stock game, 0x000D0100 with three players) from FUN_00162B78's own
// instructions while the game runs. (Checked: installing and switching three players commute.)
//
// STATES. Off: the six words and both stretches are the game's. On: version 4 is in. Outdated: the
// third test version (test3: a 12 KB spell area copied into low memory at start-up, three-player
// builds only); saving takes it out and puts version 4 in with its price and mana. Unrecognized:
// anything else (earlier test versions, or another patch in those places).
//
// SETTINGS (the data block; the code reads them while the game runs): the list entry's price byte
// (0-30, priced as the game prices its own feats: from 5 on a rank costs price + rank^1.6, so 16
// gives 16, 17, 19, 21 and 25; below 5, (rank + 1) x price; 0 makes every rank free), the mana a
// cast takes, and the damage each burn does at ranks 1-5.
//
// THE GAME'S FILES. GTEXT.LMP needs the spell's name and description (XantamFire.Names, in the five
// languages), or the level-up screen and the HUD show NF:xantamfire_name. ICONS.LMP can take the
// HUD icon (XantamFireIcon); without it the HUD shows Fire Shield's.
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

public enum XantamFireState
{
    /// <summary>The game's code and the free pool: Xantam's Fire can be put in.</summary>
    Off,
    /// <summary>Version 4 is in: its settings can be read and changed.</summary>
    On,
    /// <summary>The third test version (the spell area): saving brings it to version 4.</summary>
    Outdated,
    /// <summary>Neither: an earlier test version, or another patch where it goes.</summary>
    Unrecognized
}

/// <summary>What one burn of a trail does at one rank (at least 0, at most 255, Min at most Max).</summary>
public readonly record struct XantamFireDamage(int Min, int Max);

/// <summary>Xantam's Fire's settings: the level-up price byte, the mana a cast takes, and the damage of each burn by rank.</summary>
public sealed record XantamFireSettings(int Price, float Mana, IReadOnlyList<XantamFireDamage> Damage)
{
    public const int Ranks = 5, MaxPrice = 30, MaxDamage = 255;
    public const float MaxMana = 100f;

    /// <summary>What featpatch.py writes: price 16 and 15 mana (as Ball Lightning), 1-2, 1-3, 2-4, 2-5 and 3-6.</summary>
    public static XantamFireSettings Default { get; } =
        new(16, 15f, new XantamFireDamage[] { new(1, 2), new(1, 3), new(2, 4), new(2, 5), new(3, 6) });

    /// <summary>
    /// What the next rank costs on the level-up screen when the hero holds <paramref name="held"/> ranks (0-4), as the game prices
    /// its feats (FUN_0017C620): a price below 5 costs (held + 1) x price, from 5 on price + held^1.6.
    /// </summary>
    public static int RankCost(int price, int held) => price < 5 ? (held + 1) * price : price + (int)MathF.Pow(held, 1.6f);

    /// <summary>Why these can't be written, or null.</summary>
    public string? Problem()
    {
        if (Price is < 0 or > MaxPrice) return $"The price is 0 to {MaxPrice} (0 makes every rank free).";
        if (!float.IsFinite(Mana) || Mana <= 0f || Mana > MaxMana) return $"The mana a cast takes is above 0 and at most {MaxMana:0}.";
        if (Damage is null || Damage.Count != Ranks) return $"Damage is given for each of the {Ranks} ranks.";
        for (var r = 0; r < Ranks; r++)
        {
            var (min, max) = (Damage[r].Min, Damage[r].Max);
            if (min is < 0 or > MaxDamage || max is < 0 or > MaxDamage) return $"Rank {r + 1}: damage is 0 to {MaxDamage}.";
            if (min > max) return $"Rank {r + 1}: the least damage is more than the most.";
        }

        return null;
    }

    public bool Equals(XantamFireSettings? other) =>
        other is not null && Price == other.Price && Mana.Equals(other.Mana) &&
        Damage.Count == other.Damage.Count && Damage.SequenceEqual(other.Damage);

    public override int GetHashCode() => HashCode.Combine(Price, Mana, Damage.Count);
}

/// <summary>Xantam's Fire's texts in GTEXT.LMP: the level-up screen's name and description, and the HUD's name.</summary>
public static class XantamFire
{
    /// <summary>The feat's bit in the known mask, its cast type and its ranks.</summary>
    public const int Bit = 26, CastType = 10, Ranks = 5;

    /// <summary>The keys and texts in each language (0 English ... 4 Spanish): what gtext_feats.py writes, tried in the game.</summary>
    public static IReadOnlyList<(int Language, string Key, string Text)> Names { get; } = new[]
    {
        (0, "xantamfire_name", "Xantam's Fire"),
        (0, "xantamfire_desc", "Like the beholder Xantam, the caster sends trails of fire across the ground, burning all foes in their path. More trails at level 3, homing at level 5."),
        (1, "xantamfire_name", "Feu de Xantam"),
        (1, "xantamfire_desc", "Comme le tyrannoeil Xantam, le lanceur projette au sol des traînées de feu qui brûlent les adversaires. Plus de traînées au niveau 3, guidées au niveau 5."),
        (2, "xantamfire_name", "Xantams Feuer"),
        (2, "xantamfire_desc", "Wie der Betrachter Xantam schickt der Zaubernde Feuerspuren über den Boden, die alle Gegner verbrennen. Mehr Spuren ab Stufe 3, gelenkt ab Stufe 5."),
        (3, "xantamfire_name", "Fuoco di Xantam"),
        (3, "xantamfire_desc", "Come il beholder Xantam, l'incantatore scaglia scie di fuoco sul terreno che bruciano tutti i nemici. Più scie al livello 3, a ricerca al livello 5."),
        (4, "xantamfire_name", "Fuego de Xantam"),
        (4, "xantamfire_desc", "Como el contemplador Xantam, el lanzador envía estelas de fuego por el suelo que queman a los enemigos. Más estelas en el nivel 3, buscadoras en el 5.")
    };

    /// <summary>True when <paramref name="text"/> has every one of <see cref="Names"/> as written here.</summary>
    public static bool HasNames(GameText text) => Names.All(n => text.Get(n.Key, n.Language) == n.Text);

    /// <summary>True when <paramref name="text"/> has any of the keys.</summary>
    public static bool HasAnyName(GameText text) => Names.Any(n => text.Get(n.Key, n.Language) != null);

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
}

/// <summary>The HUD icon of Xantam's Fire: icon_xantamfire.tex in ICONS.LMP.</summary>
public static class XantamFireIcon
{
    public const string EntryName = "icon_xantamfire.tex";

    /// <summary>The texture: 44 x 44, 8-bit with 256 colours, in the layout of the game's spell icons (make_xantam_icon.py).</summary>
    public static byte[] Texture() => Convert.FromBase64String(FeatPatch.IconBase64);

    /// <summary>Why <paramref name="icons"/> isn't an ICONS.LMP the icon can go in or come out of, or null.</summary>
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

    /// <summary>True when <paramref name="icons"/> has icon_xantamfire.tex.</summary>
    public static bool IsIn(byte[] icons) => GameArchive.ReadDirectory(icons).Any(e => e.Name == EntryName);

    /// <summary>
    /// ICONS.LMP with icon_xantamfire.tex added where the game finds it (between icon_whirlwind.tex and levelup.tex; the
    /// game's ICONS.LMP has a full directory, so every file first moves on by 0x80): icons_lmp.py's result, byte for byte.
    /// The same bytes when it is there already.
    /// </summary>
    public static byte[] AddTo(byte[] icons)
    {
        if (Problem(icons) is { } problem) throw new InvalidDataException(problem);
        return IsIn(icons) ? icons : GameArchive.Insert(icons, EntryName, Texture(), growDirectory: true);
    }

    /// <summary>ICONS.LMP without icon_xantamfire.tex, its directory back to its smallest size: as it was before <see cref="AddTo"/>.</summary>
    public static byte[] RemoveFrom(byte[] icons)
    {
        if (Problem(icons) is { } problem) throw new InvalidDataException(problem);
        return IsIn(icons) ? GameArchive.Remove(icons, EntryName, shrinkDirectory: true) : icons;
    }
}

public partial class BgdaExecutable
{
    private static readonly byte[] FeatMark = "FEAT"u8.ToArray();

    private static readonly Lazy<(byte[] Pool, byte[] Code, byte[] Data)> FeatPatchData = new(LoadFeatPatchData);

    private static (byte[] Pool, byte[] Code, byte[] Data) LoadFeatPatchData()
    {
        using var packed = new MemoryStream(Convert.FromBase64String(FeatPatch.PoolOriginalBase64));
        using var unpack = new ZLibStream(packed, CompressionMode.Decompress);
        using var pool = new MemoryStream();
        unpack.CopyTo(pool);
        var bytes = pool.ToArray();
        if (bytes.Length != FeatPatch.PoolBytes ||
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != FeatPatch.PoolOriginalSha256)
            throw new InvalidDataException("The built-in feats patch data is damaged.");
        var code = Convert.FromBase64String(FeatPatch.CodeBase64);
        var data = Convert.FromBase64String(FeatPatch.DataBase64);
        if (code.Length != FeatPatch.CodeEndVa - FeatPatch.CodeVa || data.Length != FeatPatch.DataSize)
            throw new InvalidDataException("The built-in feats patch data is damaged.");
        return (bytes, code, data);
    }

    /// <summary>How much game code nothing runs there is for new feats (the pool), and in how many stretches.</summary>
    public static int FeatPoolBytes => FeatPatch.PoolBytes;

    public static int FeatPoolStretches => FeatPatch.Pool.Length;

    /// <summary>How much of the pool Xantam's Fire takes (its data block, its code and its two vtables).</summary>
    public static int XantamFireBytes => FeatPatch.DataSize + (FeatPatch.CodeEndVa - FeatPatch.CodeVa);

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

    /// <summary>The test files' tripwire over <paramref name="start"/>-<paramref name="end"/> of stretch <paramref name="n"/>: 'j' and 'nop' in turn, ending with the nop.</summary>
    private static byte[] Tripwire(int start, int end, int n)
    {
        var j = 0x08000000u | (((FeatPatch.TripBase + (uint)n * 0x1000) >> 2) & 0x03FFFFFFu);
        var k = (end - start) / 4;
        var bytes = new byte[end - start];
        for (var i = 0; i < k; i++)
        {
            if ((k - 1 - i) % 2 == 1) BitConverter.GetBytes(j).CopyTo(bytes, 4 * i);
        }

        return bytes;
    }

    /// <summary>The stretches of the pool outside Xantam's Fire's own two: (start, end, stretch number).</summary>
    private static IEnumerable<(int Start, int End, int N)> PoolOutsideXantamFire()
    {
        var used = new[] { (FeatPatch.DataVa, FeatPatch.DataVa + FeatPatch.DataSize), (FeatPatch.CodeVa, FeatPatch.CodeEndVa) };
        for (var n = 0; n < FeatPatch.Pool.Length; n++)
        {
            var (a, b) = FeatPatch.Pool[n];
            var at = a;
            foreach (var (ua, ub) in used.Where(u => a <= u.Item1 && u.Item1 < b).OrderBy(u => u.Item1))
            {
                if (ua > at) yield return (at, ua, n);
                at = ub;
            }

            if (at < b) yield return (at, b, n);
        }
    }

    private bool BytesAre(int va, ReadOnlySpan<byte> bytes) =>
        _data.Length >= FileOffset(va) + bytes.Length && _data.AsSpan(FileOffset(va), bytes.Length).SequenceEqual(bytes);

    private int XantamDataAt(int offset) => FileOffset(FeatPatch.DataVa) + offset;

    /// <summary>
    /// Off, On, Outdated (the third test version, updated on save) or Unrecognized; <paramref name="detail"/> says what was
    /// found, in a sentence.
    /// </summary>
    public XantamFireState GetXantamFireState(out string detail)
    {
        if (_data.Length < FileOffset(FeatPatch.Pool[^1].End) || _data.Length < FileOffset(FeatPatch.AbilityTableEndVa) + 4)
        {
            detail = "The file is too short to be this game's executable.";
            return XantamFireState.Unrecognized;
        }

        var (_, code, _) = FeatPatchData.Value;
        var hooksGame = FeatPatch.Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Game);
        var hooksOurs = FeatPatch.Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Patched);
        if (hooksOurs)
        {
            if (BytesAre(FeatPatch.CodeVa, code) && BytesAre(FeatPatch.DataVa, FeatMark) &&
                ReadU32(XantamDataAt(FeatPatch.VersionAt)) == FeatPatch.Version)
            {
                detail = "Xantam's Fire is in (version 4, in game code nothing runs): it works with or without three players.";
                return XantamFireState.On;
            }

            detail = "The game's code jumps to Xantam's Fire, but what is there isn't what WorldExplorer writes.";
            return XantamFireState.Unrecognized;
        }

        if (V3Present())
        {
            detail = "Xantam's Fire is in as the third test version (its spell area, which only a three-player build loads). " +
                     "Saving updates it to version 4, which works with or without three players; its price and mana are kept.";
            return XantamFireState.Outdated;
        }

        if (!hooksGame)
        {
            var (va, _, _) = FeatPatch.Hooks.First(h => ReadU32(FileOffset(h.Va)) != h.Game);
            detail = $"0x{va:X8}, where Xantam's Fire hooks in, isn't the game's code: an earlier test version (take it out " +
                     "with featpatch.py --remove) or another patch.";
            return XantamFireState.Unrecognized;
        }

        if (XantamFireProblem() is { } problem)
        {
            detail = problem;
            return XantamFireState.Unrecognized;
        }

        detail = "Xantam's Fire isn't in: it can be put in, with or without three players.";
        return XantamFireState.Off;
    }

    public XantamFireState GetXantamFireState() => GetXantamFireState(out _);

    /// <summary>Why the patch can't go in this file (its hooks being the game's), or null.</summary>
    private string? XantamFireProblem()
    {
        foreach (var (va, word) in FeatPatch.Checks)
        {
            if (ReadU32(FileOffset(va)) != word) return $"0x{va:X8} isn't the game's code, which Xantam's Fire relies on.";
        }

        if (!BytesAre(FeatPatch.AbilityTableEndVa, "END\0"u8))
            return "The ability table doesn't end where the game's does.";
        for (var va = FeatPatch.AbilityTableVa; va < FeatPatch.AbilityTableEndVa; va += 0x50)
        {
            var at = FileOffset(va);
            if ((ReadU64(at + 0x48) & 1UL << XantamFire.Bit) != 0 || BitConverter.ToInt16(_data, at + 0x20) == XantamFire.CastType)
                return $"An ability already uses bit {XantamFire.Bit} or cast type {XantamFire.CastType}, which Xantam's Fire needs.";
        }

        if (!BytesAre(FeatPatch.FallbackNameVa, Encoding.ASCII.GetBytes(FeatPatch.FallbackName + "\0")))
            return $"\"{FeatPatch.FallbackName}\" isn't where the game keeps it.";
        var lui = ReadU32(FileOffset(FeatPatch.ListsLuiVa));
        var addiu = ReadU32(FileOffset(FeatPatch.ListsAddiuVa));
        var lists = (int)((lui & 0xFFFF) << 16) + (short)(addiu & 0xFFFF);
        if (lui >> 16 != 0x3C03 || addiu >> 16 != 0x246A || lists is not (0x002A0478 or 0x000D0100))
            return "FUN_00162B78, which builds the level-up lists, isn't the game's or the three-player builds'.";
        if (!BytesAre(FeatPatch.DataVa, PoolOriginal(FeatPatch.DataVa, FeatPatch.DataVa + FeatPatch.DataSize)) ||
            !BytesAre(FeatPatch.CodeVa, PoolOriginal(FeatPatch.CodeVa, FeatPatch.CodeEndVa)))
            return "The unused game code where Xantam's Fire goes holds something else: another patch uses it.";
        return null;
    }

    /// <summary>The third test version: its "FEAT" version 3 mark in the spell area's bytes and its seven hooks.</summary>
    private bool V3Present() =>
        _data.Length >= FeatPatch.V3AreaFile + FeatPatch.V3AreaSize &&
        _data.AsSpan(FeatPatch.V3AreaFile, 4).SequenceEqual(FeatMark) && ReadU32(FeatPatch.V3AreaFile + 4) == 3 &&
        FeatPatch.V3Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Patched);

    /// <summary>The settings in the file (On or Outdated), or the default ones (Off).</summary>
    public XantamFireSettings GetXantamFireSettings()
    {
        var state = GetXantamFireState(out _);
        if (state is not (XantamFireState.On or XantamFireState.Outdated)) return XantamFireSettings.Default;
        var block = state == XantamFireState.On ? XantamDataAt(0) : FeatPatch.V3AreaFile;   // the same layout in both
        var damage = new XantamFireDamage[XantamFireSettings.Ranks];
        for (var r = 0; r < damage.Length; r++)
            damage[r] = new XantamFireDamage(_data[block + FeatPatch.DamageMinAt + r + 1], _data[block + FeatPatch.DamageMaxAt + r + 1]);
        return new XantamFireSettings(_data[block + FeatPatch.PriceAt], BitConverter.ToSingle(_data, block + FeatPatch.ManaAt), damage);
    }

    /// <summary>
    /// Puts Xantam's Fire in with <paramref name="settings"/> (Off), brings the third test version to version 4 with them
    /// (Outdated), or changes its settings (On). Throws <see cref="ArgumentException"/> on bad settings and
    /// <see cref="InvalidOperationException"/> when the state is Unrecognized; nothing is changed then.
    /// </summary>
    public void SetXantamFire(XantamFireSettings settings)
    {
        if (settings.Problem() is { } problem) throw new ArgumentException(problem, nameof(settings));
        var state = GetXantamFireState(out var detail);
        if (state == XantamFireState.Unrecognized)
            throw new InvalidOperationException("Xantam's Fire can't be put in: " + detail);
        if (state == XantamFireState.Outdated)
        {
            RemoveV3();
            if (XantamFireProblem() is { } left)
                throw new InvalidOperationException("Xantam's Fire can't be updated: " + left);
        }

        var (_, code, template) = FeatPatchData.Value;
        var block = (byte[])template.Clone();
        block[FeatPatch.PriceAt] = (byte)settings.Price;
        BitConverter.GetBytes(settings.Mana).CopyTo(block, FeatPatch.ManaAt);
        for (var r = 0; r < XantamFireSettings.Ranks; r++)
        {
            block[FeatPatch.DamageMinAt + r + 1] = (byte)settings.Damage[r].Min;
            block[FeatPatch.DamageMaxAt + r + 1] = (byte)settings.Damage[r].Max;
        }

        // the ability record's damage: the HUD's line until the first cast (CAST then writes the rank's into the slot)
        BitConverter.GetBytes((ushort)settings.Damage[0].Min).CopyTo(block, FeatPatch.AbilityDamageAt);
        BitConverter.GetBytes((ushort)settings.Damage[0].Max).CopyTo(block, FeatPatch.AbilityDamageAt + 2);
        block.CopyTo(_data, FileOffset(FeatPatch.DataVa));
        if (state != XantamFireState.On)
        {
            code.CopyTo(_data, FileOffset(FeatPatch.CodeVa));
            foreach (var (va, _, patched) in FeatPatch.Hooks) WriteInt(FileOffset(va), (int)patched);
        }
    }

    /// <summary>
    /// Takes Xantam's Fire out (On or Outdated): the game's words back at the hooks and the game's own bytes back in the
    /// pool (also where a test file's tripwire is). Nothing happens when it is Off; throws
    /// <see cref="InvalidOperationException"/> when Unrecognized.
    /// </summary>
    public void RemoveXantamFire()
    {
        switch (GetXantamFireState(out var detail))
        {
            case XantamFireState.Off:
                return;
            case XantamFireState.Unrecognized:
                throw new InvalidOperationException("Xantam's Fire can't be taken out: " + detail);
            case XantamFireState.Outdated:
                RemoveV3();
                return;
        }

        foreach (var (va, game, _) in FeatPatch.Hooks) WriteInt(FileOffset(va), (int)game);
        PoolOriginal(FeatPatch.DataVa, FeatPatch.DataVa + FeatPatch.DataSize).CopyTo(_data.AsSpan(FileOffset(FeatPatch.DataVa)));
        PoolOriginal(FeatPatch.CodeVa, FeatPatch.CodeEndVa).CopyTo(_data.AsSpan(FileOffset(FeatPatch.CodeVa)));
        foreach (var (a, b, n) in PoolOutsideXantamFire())
        {
            if (BytesAre(a, Tripwire(a, b, n))) PoolOriginal(a, b).CopyTo(_data.AsSpan(FileOffset(a)));
        }
    }

    /// <summary>Takes the third test version out: the game's words back at its seven hooks, its spell area's bytes zero again.</summary>
    private void RemoveV3()
    {
        foreach (var (va, game, _) in FeatPatch.V3Hooks) WriteInt(FileOffset(va), (int)game);
        Array.Clear(_data, FeatPatch.V3AreaFile, FeatPatch.V3AreaSize);
    }
}
