// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Executable.cs
//
// Tuning tables of the Baldur's Gate: Dark Alliance II executable (SLUS_206.75 v1.03, NTSC,
// 2,582,584 bytes, PCSX2 CRC B0859096 = the XOR of its 32-bit words). One LOAD segment: file
// offset = address - 0xFF000 for 0x100000 <= address < 0x361C38.
//
// Edits are not written into the executable: an edited executable has another CRC, and PCSX2
// would then no longer load the three-player mod's cheat file (SLUS-20675_B0859096.pnach). They
// become one more group of that file instead, "[WorldExplorer - executable tuning]": a patch=0
// word for every word that differs from the stock executable (written once, when the game boots,
// before any of it runs), and reading the group back gives the edits again.
//
// The partial class is split by topic:
//   Da2Executable.cs           this file: opening, words, the tuning group, XP per level, starting stats
//   Da2Executable.Feats.cs     ability templates (energy, damage, colours), class feat tables (ranks,
//                              base cost), the four DA1 spells' price tables
//   Da2Executable.Items.cs     base items (value, weight, armour), levels (monster level)
//   Da2Executable.Monsters.cs  difficulty and two-player factors, creature multipliers, boss HP
//   Da2Executable.Rules.cs     kill XP shares, XP bonuses, drop rolls, gold / item value curves,
//                              potions, Ransack, the two-player pile chance
//   Da2Executable.Loot.cs      each creature's drop values (Da2LootData), the quality table
//   (Da2LevelBudget: the stock levels' object pools and heap at load, read from the game's files)
//
// XP per level, 0x313920: 41 int32. Entry n (0-based) is the total XP for level n + 1 (level 1 = 0);
//   giveXP 0x2B8468 compares the XP with entry[level] while the level byte (record + 85) is below 40;
//   entry 40 (99,999,999) is never reached and stays as it is.
// Starting stats, 0x352128: 10 classes x 6 int32 in the order STR, DEX, CON, INT, WIS, CHA (copied
//   by initPlayers 0x292CF8 into record + 116..136 for a new character). The class order is the
//   game's: 0 Vahn, 1 Adrianna, 2 Kromlech (the DA1 heroes' leftover rows), 3 Drizzt, 4 Dorn,
//   5 Vhaidra, 6 Ysuran, 7 Borador, 8 Allessia, 9 Artemis.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using JetBlackEngineLib.Data.Iso;
using JetBlackEngineLib.Data.Pnach;

namespace JetBlackEngineLib.Data.Executable;

public sealed partial class Da2Executable
{
    public const int FileLength = 2_582_584;
    public const uint Crc = 0xB0859096;
    public const string FileName = "SLUS_206.75";
    public const uint ImageStart = 0x00100000, ImageEnd = 0x00361C38;
    private const uint OffsetDelta = 0x000FF000;   // file offset = address - 0xFF000

    public const string TuningGroupName = "WorldExplorer - executable tuning";

    private readonly byte[] _stock;
    private readonly byte[] _data;

    /// <summary>Where the executable was read from (the file, or the ISO it is in).</summary>
    public string Source { get; }

    private Da2Executable(byte[] data, string source)
    {
        _stock = data;
        _data = (byte[])data.Clone();
        Source = source;
    }

    /// <summary>PCSX2's CRC of an executable: the XOR of its little-endian 32-bit words.</summary>
    public static uint PcsxCrc(byte[] data)
    {
        uint x = 0;
        for (var i = 0; i + 4 <= data.Length; i += 4) x ^= BitConverter.ToUInt32(data, i);
        return x;
    }

    /// <summary>
    /// Opens SLUS_206.75, or the game's ISO (the executable is read from its root folder). Throws
    /// <see cref="NotSupportedException"/> for anything but the v1.03 executable the mod is made for.
    /// </summary>
    public static Da2Executable Open(string path)
    {
        // only the first bytes: the game's ISO is several gigabytes
        var head = new byte[4];
        int read;
        using (var f = File.OpenRead(path)) read = f.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        if (read == 4 && !(head[0] == 0x7F && head[1] == (byte)'E' && head[2] == (byte)'L' && head[3] == (byte)'F'))
        {
            byte[]? elf = null;
            try
            {
                elf = Da2ThreePlayerIso.ReadRootFile(path, FileName);
            }
            catch (InvalidDataException)
            {
                // not an ISO either
            }

            if (elf == null)
                throw new NotSupportedException("This is neither a PS2 executable nor a Dark Alliance II disc image with SLUS_206.75 on it.");
            return FromBytes(elf, path + " (" + FileName + ")");
        }

        var length = new FileInfo(path).Length;
        if (length > 64 << 20)
            throw new NotSupportedException($"This file is {length:N0} bytes: not the game's executable ({FileLength:N0} bytes).");
        return FromBytes(File.ReadAllBytes(path), path);
    }

    public static Da2Executable FromBytes(byte[] data, string source)
    {
        if (data.Length < 4 || data[0] != 0x7F || data[1] != (byte)'E' || data[2] != (byte)'L' || data[3] != (byte)'F')
            throw new NotSupportedException("Not a PS2 executable (missing ELF header).");
        var crc = PcsxCrc(data);
        if (data.Length != FileLength || crc != Crc)
        {
            throw new NotSupportedException(
                $"This executable isn't Baldur's Gate: Dark Alliance II (USA, SLUS_206.75 v1.03): its CRC is {crc:X8} and it is " +
                $"{data.Length:N0} bytes, the game's is {Crc:X8} and {FileLength:N0} bytes. The tables are at addresses of that " +
                "build, and the three-player mod's cheat file is made for it.");
        }

        return new Da2Executable(data, source);
    }

    // ---- words ----

    public static bool InImage(uint address) => address >= ImageStart && address + 4 <= ImageEnd;

    private static int Offset(uint address)
    {
        if (!InImage(address)) throw new ArgumentOutOfRangeException(nameof(address), $"0x{address:X6} is not in the executable.");
        return (int)(address - OffsetDelta);
    }

    public uint Word(uint address) => BitConverter.ToUInt32(_data, Offset(address));
    public uint StockWord(uint address) => BitConverter.ToUInt32(_stock, Offset(address));
    public void SetWord(uint address, uint value) => BitConverter.GetBytes(value).CopyTo(_data, Offset(address));

    private int I32(uint a) => (int)Word(a);
    private void SetI32(uint a, int v) => SetWord(a, (uint)v);
    private float F32(uint a) => BitConverter.Int32BitsToSingle((int)Word(a));
    private void SetF32(uint a, float v) => SetWord(a, (uint)BitConverter.SingleToInt32Bits(v));
    private ushort U16(uint a) => BitConverter.ToUInt16(_data, Offset(a & ~3u) + (int)(a & 3));
    private void SetU16(uint a, int v) => BitConverter.GetBytes((ushort)v).CopyTo(_data, Offset(a & ~3u) + (int)(a & 3));
    private byte U8(uint a) => _data[Offset(a & ~3u) + (int)(a & 3)];
    private void SetU8(uint a, int v) => _data[Offset(a & ~3u) + (int)(a & 3)] = (byte)v;

    /// <summary>The NUL-terminated ASCII string at an address (in the executable), or "".</summary>
    public string String(uint address, int max = 64)
    {
        if (!InImage(address)) return "";
        var off = Offset(address & ~3u) + (int)(address & 3);
        var end = off;
        while (end < _data.Length && end < off + max && _data[end] != 0) end++;
        return Encoding.ASCII.GetString(_data, off, end - off);
    }

    private string StringAt(uint pointerAddress) => String(Word(pointerAddress));

    // ---- edits as a cheat group ----

    /// <summary>Every word that differs from the stock executable, by address.</summary>
    public List<(uint Address, uint Value)> ChangedWords()
    {
        var list = new List<(uint, uint)>();
        for (var off = 0x1000; off + 4 <= _data.Length && off + OffsetDelta + 4 <= ImageEnd; off += 4)
        {
            var v = BitConverter.ToUInt32(_data, off);
            if (v != BitConverter.ToUInt32(_stock, off)) list.Add(((uint)(off + OffsetDelta), v));
        }

        return list;
    }

    public bool HasChanges => ChangedWords().Count > 0;

    /// <summary>The tuning group for the changed words, or null when nothing is changed.</summary>
    public PnachGroup? ToTuningGroup()
    {
        var words = ChangedWords();
        if (words.Count == 0) return null;
        var lines = new List<string>
        {
            "author=WorldExplorer",
            "description=Executable Tuning made in WorldExplorer: " + DescribeChanges() + ". Written once when the game boots " +
            "(restart the game after ticking it). Open the file in WorldExplorer's Executable Tuning to change it again."
        };
        lines.AddRange(words.Select(w => PnachPatch.Word(w.Address, w.Value).ToLine()));
        return new PnachGroup(TuningGroupName, lines);
    }

    /// <summary>
    /// Puts the words of a tuning group (one this class wrote) on top of the stock executable. Words
    /// outside the executable's image are refused (InvalidDataException) and nothing is changed.
    /// </summary>
    public void ApplyTuningGroup(PnachGroup group)
    {
        var words = new List<(uint, uint)>();
        foreach (var p in group.Patches)
        {
            if (p.Place != 0 || !p.Cpu.Equals("EE", StringComparison.OrdinalIgnoreCase) ||
                !p.Type.Equals("word", StringComparison.OrdinalIgnoreCase) || !InImage(p.Address) || (p.Address & 3) != 0)
                throw new InvalidDataException($"[{group.Name}] has a line WorldExplorer didn't write: {p.ToLine()}");
            words.Add((p.Address, p.Value));
        }

        foreach (var (a, v) in words) SetWord(a, v);
    }

    /// <summary>Back to the stock executable (no edits).</summary>
    public void ResetToStock() => Array.Copy(_stock, _data, _data.Length);

    /// <summary>The stock executable without this one's edits (to read the game's own values).</summary>
    public Da2Executable Stock() => new(_stock, Source);

    /// <summary>Addresses written both by the tuning group and by one of the given groups (they would fight).</summary>
    public List<(uint Address, string Group)> Conflicts(IEnumerable<PnachGroup> groups)
    {
        var mine = ChangedWords().Select(w => w.Address).ToHashSet();
        var found = new List<(uint, string)>();
        foreach (var g in groups)
        {
            foreach (var p in g.Patches)
            {
                if (mine.Contains(p.Address)) found.Add((p.Address, g.Name));
            }
        }

        return found;
    }

    /// <summary>What is changed, in a few words ("XP per level, 2 items").</summary>
    public string DescribeChanges()
    {
        var parts = new List<string>();
        var changed = ChangedWords().Select(w => w.Address).ToHashSet();
        bool Any(uint start, uint length) => changed.Any(a => a >= start && a < start + length);
        if (Any(XpTable, 4 * XpLevels)) parts.Add("XP per level");
        if (Any(StatsTable, 4 * StatsPerClass * ClassCount)) parts.Add("starting stats");
        var abilities = GetAbilities().Count(a => Any(a.Address, AbilitySize));
        if (abilities > 0) parts.Add(Plural(abilities, "ability", "abilities"));
        var feats = GetClassFeats().Count(f => Any(f.Address, ClassFeatSize));
        if (feats > 0) parts.Add(Plural(feats, "class feat entry", "class feat entries"));
        if (SpecialPriceFeats.Any(id => Any(SpecialPriceTable(id), 20))) parts.Add("spell price tables");
        var items = GetItems().Count(i => Any(i.Address, ItemSize));
        if (items > 0) parts.Add(Plural(items, "item", "items"));
        var levels = GetLevels().Count(l => Any(l.Address, LevelSize));
        if (levels > 0) parts.Add(Plural(levels, "level", "levels"));
        if (DifficultyConstants.Any(c => Any(c.Lui, 4) || (c.Ori != 0 && Any(c.Ori, 4)))) parts.Add("difficulty");
        if (Da2MonsterData.Constants.Any(c => Any(c.Lui, 4) || (c.Ori != 0 && Any(c.Ori, 4)))) parts.Add("creature multipliers");
        if (Da2MonsterData.BossHp.Any(b => Any(b.Lui, 4) || (b.Ori != 0 && Any(b.Ori, 4)))) parts.Add("boss hit points");
        bool AnySetting(params string[] groups) =>
            Settings.Where(s => groups.Contains(s.Group)).Any(s => SettingWords(s).Any(a => Any(a, 4)));
        if (AnySetting(GroupKillXp, GroupXpBonus)) parts.Add("XP rules");
        var drops = Da2LootData.Constants.Count(c => Any(c.Lui, 4) || (c.Ori != 0 && Any(c.Ori, 4)));
        if (drops > 0) parts.Add(Plural(drops, "creature drop value", "creature drop values"));
        if (AnySetting(GroupDrops, GroupGold, GroupValue, GroupTwoPlayers) || Any(QualityTable, 12 * QualityRows)) parts.Add("loot rules");
        if (AnySetting(GroupPotions)) parts.Add("potions");
        if (AnySetting(GroupRansack)) parts.Add("Ransack");
        return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
    }

    private static string Plural(int n, string one, string many) =>
        n.ToString(CultureInfo.InvariantCulture) + " " + (n == 1 ? one : many);

    // ---- XP per level ----

    public const uint XpTable = 0x00313920;
    public const int XpLevels = 40;
    public const int MaxXp = 99_999_998;

    /// <summary>Total XP for levels 1..40 (entry 0 = level 1 = 0).</summary>
    public int[] GetXpTable() => Enumerable.Range(0, XpLevels).Select(i => I32(XpTable + (uint)(4 * i))).ToArray();

    public void SetXpTable(int[] values)
    {
        if (values.Length != XpLevels) throw new ArgumentException($"The XP table needs exactly {XpLevels} values.");
        if (values[0] != 0) throw new ArgumentException("Level 1 must need 0 XP.");
        for (var i = 1; i < XpLevels; i++)
        {
            if (values[i] <= values[i - 1])
                throw new ArgumentException($"XP must increase from level to level (level {i + 1} needs no more than level {i}).");
            if (values[i] > MaxXp) throw new ArgumentException($"Level {i + 1}: at most {MaxXp:N0} XP.");
        }

        for (var i = 0; i < XpLevels; i++) SetI32(XpTable + (uint)(4 * i), values[i]);
    }

    // ---- starting stats ----

    public const uint StatsTable = 0x00352128;
    public const int ClassCount = 10, StatsPerClass = 6;
    public const int MinStat = 1, MaxStat = 30;
    public static readonly string[] StatNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

    public int GetStat(int classId, int stat) => I32(StatsTable + (uint)(4 * (classId * StatsPerClass + stat)));

    public void SetStat(int classId, int stat, int value)
    {
        if (classId is < 0 or >= ClassCount || stat is < 0 or >= StatsPerClass) throw new ArgumentOutOfRangeException(nameof(classId));
        if (value < MinStat || value > MaxStat)
            throw new ArgumentException($"{Da2Names.ClassName(classId)} {StatNames[stat]}: ability scores must be from {MinStat} to {MaxStat}.");
        SetI32(StatsTable + (uint)(4 * (classId * StatsPerClass + stat)), value);
    }
}
