// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Placements.cs
//
// Creatures placed when a level loads (the three-player mod's build 48 path, cave4 at 0xAE000):
// SpawnHook (0xAE0D8, called instead of jal 0x246F08 at 0x271C10, after the level loader's record
// loop) walks a table of 40-byte rows
//     +0 pointer to the row's enable word   +4 V_DA1FILES bits the creature needs (8 = LILVARRA.LMP,
//     16 = MINOTAUR.LMP on the [3P] ISO)   +8 level name   +12 creature type   +16 properties
//     (char*[], 0-terminated: "hpscale=2" ...)   +20/24/28 f32 x, y, z   +32 s16 facing   +36 pad
// ended by a row whose enable pointer is 0, and for each row whose enable word is set, whose files
// are on the disc and whose level is the one loading, calls the creature factory 0x26A050 as the
// loader does. Only on the FIRST visit (loader mode 0): a surviving creature is then saved with the
// level's own and a killed one stays dead, like any placed creature.
//
// Each creature is its own [group], so any combination can be ticked in PCSX2: every group writes the
// same code and table and sets only its own enable word (the words of the others stay zero).
//
// Build 51's three bosses in Trollbark Forest are written exactly as in build 51 (table 0xAE010,
// enable words 0xAE000 / 4 / 8). Any other set of creatures is written with the table moved to
// 0xAE200 (the two instructions of SpawnHook that load its address, 0xAE0F4 / 0xAE0F8, point there),
// enable words from 0xAE1A0, names and properties after the table, all below the shops' code at
// 0xAF000. Lilivarra's groups also make her hostile at once (her DA1 dialogue gate, two words).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBlackEngineLib.Data.Pnach;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>A creature placed in a level when the level loads for the first time.</summary>
public sealed class Da2Placement
{
    /// <summary>The cheat group's name in PCSX2.</summary>
    public string Name { get; set; } = "";

    /// <summary>Whether its group is in the file.</summary>
    public bool Include { get; set; } = true;

    /// <summary>The level's name (e.g. "trollb" = Trollbark Forest).</summary>
    public string Level { get; set; } = "";

    /// <summary>The creature factory's type name (e.g. "Minotaur", "Goblin").</summary>
    public string Type { get; set; } = "";

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    /// <summary>Facing, 65536 to a turn (16384 = a quarter turn).</summary>
    public int Facing { get; set; }

    /// <summary>Placement properties ("hpscale=2", "damagescale=1.5" ...).</summary>
    public List<string> Properties { get; set; } = new();

    /// <summary>The group's description (null: made from the settings).</summary>
    public string? Description { get; set; }

    /// <summary>V_DA1FILES bits the type needs: its DA1 lump on the [3P] ISO.</summary>
    public int NeedsFiles => Type switch { "Lilivarra" => 8, "Minotaur" => 16, _ => 0 };

    public Da2Placement Clone() => new()
    {
        Name = Name, Include = Include, Level = Level, Type = Type, X = X, Y = Y, Z = Z, Facing = Facing,
        Properties = new List<string>(Properties), Description = Description
    };

    /// <summary>Same creature in the same place (the name, inclusion and description aside).</summary>
    public bool SamePlace(Da2Placement o) =>
        Level == o.Level && Type == o.Type && Facing == o.Facing && Properties.SequenceEqual(o.Properties) &&
        BitConverter.SingleToInt32Bits(X) == BitConverter.SingleToInt32Bits(o.X) &&
        BitConverter.SingleToInt32Bits(Y) == BitConverter.SingleToInt32Bits(o.Y) &&
        BitConverter.SingleToInt32Bits(Z) == BitConverter.SingleToInt32Bits(o.Z);
}

public static partial class Da2ThreePlayerMod
{
    public const uint SpawnHookSite = 0x00271C10;
    private const uint SpawnHook = 0x000AE0D8, SpawnCodeEnd = 0x000AE1A0, SpawnTableLui = 0x000AE0F4, SpawnTableAddiu = 0x000AE0F8;
    private const uint StockSpawnTable = 0x000AE010, MovedEnables = 0x000AE1A0, MovedTable = 0x000AE200, SpawnAreaEnd = 0x000AF000;
    public const int MaxPlacements = 24;
    private const int SpawnRow = 40;
    private static readonly (uint Address, uint Value)[] LilivarraHostile = { (0x0012446C, 0x24020001), (0x001245D8, 0x24020001) };

    private static uint Jal(uint target) => 0x0C000000 | (target >> 2);

    /// <summary>Build 51's three bosses in Trollbark Forest.</summary>
    public static List<Da2Placement> StockPlacements() =>
        ReadPlacements(BossNames.Select((_, i) => Stock(BossGroup(i))).ToList(), out _);

    private static bool StockLayout(List<Da2Placement> placements)
    {
        var stock = StockPlacements();
        return placements.Count == stock.Count &&
               placements.Zip(stock).All(p => p.First.SamePlace(p.Second) && p.First.Name == p.Second.Name);
    }

    /// <summary>The placement groups for the settings, in order.</summary>
    private static IEnumerable<PnachGroup> PlacementGroups(List<Da2Placement> placements)
    {
        if (StockLayout(placements))
        {
            var stock = StockPlacements();
            for (var i = 0; i < placements.Count; i++)
            {
                if (!placements[i].Include) continue;
                var g = Stock(stock[i].Name).Clone();
                if (placements[i].Description is { } d && d != stock[i].Description) SetDescription(g, d);
                yield return g;
            }

            yield break;
        }

        var included = placements.Where(p => p.Include).ToList();
        if (included.Count == 0) yield break;
        var words = MovedLayoutWords(included, out var enables);
        for (var i = 0; i < included.Count; i++)
        {
            var p = included[i];
            var lines = new List<string> { "author=" + Author, "description=" + (p.Description ?? DescribePlacement(p)) };
            lines.AddRange(words.Select(w => PnachPatch.Word(w.Address, w.Value).ToLine()));
            lines.Add(PnachPatch.Word(enables[i], 1).ToLine());
            lines.Add(PnachPatch.Word(SpawnHookSite, Jal(SpawnHook)).ToLine());
            if (p.Type == "Lilivarra")
                lines.AddRange(LilivarraHostile.Select(w => PnachPatch.Word(w.Address, w.Value).ToLine()));
            yield return new PnachGroup(p.Name, lines);
        }
    }

    private static void SetDescription(PnachGroup g, string description)
    {
        var i = g.Lines.FindIndex(l => l.StartsWith("description=", StringComparison.Ordinal));
        if (i >= 0) g.Lines[i] = "description=" + description;
        else g.Lines.Insert(0, "description=" + description);
    }

    private static string F(float v) => ((double)v).ToString("0.###", CultureInfo.InvariantCulture);

    public static string DescribePlacement(Da2Placement p)
    {
        var text = $"Places a {p.Type} in the level {p.Level} at ({F(p.X)}, {F(p.Y)}, {F(p.Z)}), facing {p.Facing}, " +
                   "the first time the level is entered; a killed one stays dead like the level's own creatures";
        if (p.Properties.Count > 0) text += " (" + string.Join(" ", p.Properties) + ")";
        text += ".";
        if (p.NeedsFiles == 8) text += " NEEDS the [3P] ISO with LILVARRA.LMP; without it nothing is placed. She attacks on sight (her DA1 dialogue is skipped).";
        if (p.NeedsFiles == 16) text += " NEEDS the [3P] ISO with MINOTAUR.LMP; without it nothing is placed.";
        return text + " Restart the game after ticking.";
    }

    /// <summary>SpawnHook (table moved to 0xAE200), the table and its strings; the enable word of each row.</summary>
    private static List<(uint Address, uint Value)> MovedLayoutWords(List<Da2Placement> rows, out uint[] enables)
    {
        var code = Stock(BossGroup(0)).BootWords();
        var words = new SortedDictionary<uint, uint>();
        for (var a = SpawnHook; a < SpawnCodeEnd; a += 4) words[a] = code[a];
        var hi = (MovedTable + 0x8000) >> 16;
        var lo = MovedTable - (hi << 16);
        words[SpawnTableLui] = (code[SpawnTableLui] & 0xFFFF0000) | hi;
        words[SpawnTableAddiu] = (code[SpawnTableAddiu] & 0xFFFF0000) | (lo & 0xFFFF);

        enables = Enumerable.Range(0, rows.Count).Select(i => MovedEnables + (uint)(4 * i)).ToArray();
        var bytes = new List<byte>();
        var strings = new Dictionary<string, uint>();
        var dataStart = MovedTable + (uint)(SpawnRow * (rows.Count + 1));
        uint Str(string s)
        {
            if (strings.TryGetValue(s, out var at)) return at;
            at = dataStart + (uint)bytes.Count;
            bytes.AddRange(Encoding.ASCII.GetBytes(s));
            bytes.Add(0);
            strings[s] = at;
            return at;
        }

        var propArrays = new uint[rows.Count];
        var stringsOf = rows.Select(r => (Str(r.Level), Str(r.Type), r.Properties.Select(Str).ToArray())).ToList();
        while (bytes.Count % 4 != 0) bytes.Add(0);
        for (var i = 0; i < rows.Count; i++)
        {
            propArrays[i] = dataStart + (uint)bytes.Count;
            foreach (var p in stringsOf[i].Item3) bytes.AddRange(BitConverter.GetBytes(p));
            bytes.AddRange(new byte[4]);
        }

        if (dataStart + bytes.Count > SpawnAreaEnd)
            throw new ArgumentException("The placed creatures' names and properties don't fit: use fewer or shorter ones.");

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = MovedTable + (uint)(SpawnRow * i);
            words[row] = enables[i];
            words[row + 4] = (uint)r.NeedsFiles;
            words[row + 8] = stringsOf[i].Item1;
            words[row + 12] = stringsOf[i].Item2;
            words[row + 16] = propArrays[i];
            words[row + 20] = FloatBits(r.X);
            words[row + 24] = FloatBits(r.Y);
            words[row + 28] = FloatBits(r.Z);
            words[row + 32] = (uint)(short)r.Facing;
            words[row + 36] = 0;
        }

        for (var k = 0; k < SpawnRow; k += 4) words[MovedTable + (uint)(SpawnRow * rows.Count + k)] = 0;   // the end row
        for (var k = 0; k < bytes.Count; k += 4) words[dataStart + (uint)k] = BitConverter.ToUInt32(bytes.ToArray(), k);
        return words.Select(p => (p.Key, p.Value)).ToList();
    }

    /// <summary>
    /// Reads the creature placements of a file's placement groups (any group that hooks 0x271C10 with
    /// SpawnHook): the table is decoded from the groups' words, and each row belongs to the group that
    /// sets its enable word. Rows of build 51's table whose group is not in the file come back with
    /// Include = false. <paramref name="groups"/> gets the groups that were read.
    /// </summary>
    public static List<Da2Placement> ReadPlacements(IEnumerable<PnachGroup> fileGroups, out List<PnachGroup> groups)
    {
        groups = fileGroups.Where(g => g.BootWords().TryGetValue(SpawnHookSite, out var w) && w == Jal(SpawnHook)).ToList();
        var placements = new List<Da2Placement>();
        if (groups.Count == 0) return placements;

        // every group writes the same code and table (and only its own enable word)
        var words = new Dictionary<uint, uint>();
        foreach (var g in groups)
        {
            foreach (var (a, v) in g.BootWords())
            {
                if (a >= 0xAE000 && a < SpawnAreaEnd) words[a] = v;
            }
        }

        if (!words.TryGetValue(SpawnTableLui, out var lui) || !words.TryGetValue(SpawnTableAddiu, out var addiu)) return placements;
        var table = ((lui & 0xFFFF) << 16) + (uint)(short)(addiu & 0xFFFF);
        uint W(uint a) => words.TryGetValue(a, out var v) ? v : 0;
        string S(uint a)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < 64; i++)
            {
                var b = (byte)(W((a + (uint)i) & ~3u) >> (int)(8 * ((a + (uint)i) & 3)));
                if (b == 0) break;
                sb.Append((char)b);
            }

            return sb.ToString();
        }

        var stockNames = new Dictionary<uint, string> { [0xAE000] = BossGroup(0), [0xAE004] = BossGroup(1), [0xAE008] = BossGroup(2) };
        for (var row = table; W(row) != 0 && row < SpawnAreaEnd; row += SpawnRow)
        {
            var enable = W(row);
            var owner = groups.FirstOrDefault(g => g.BootWords().TryGetValue(enable, out var on) && on == 1);
            var props = new List<string>();
            for (var p = W(row + 16); p != 0 && W(p) != 0 && props.Count < 16; p += 4) props.Add(S(W(p)));
            placements.Add(new Da2Placement
            {
                Name = owner?.Name ?? (stockNames.TryGetValue(enable, out var n) ? n : $"Creature at 0x{row:X5}"),
                Include = owner != null,
                Level = S(W(row + 8)),
                Type = S(W(row + 12)),
                X = FloatOf(W(row + 20)),
                Y = FloatOf(W(row + 24)),
                Z = FloatOf(W(row + 28)),
                Facing = (short)(W(row + 32) & 0xFFFF),
                Properties = props,
                Description = owner?.Description
            });
        }

        if (table == StockSpawnTable)
        {
            // build 51's layout: a row without its group in the file keeps the stock description
            var stockGroups = BossNames.Select((_, i) => Stock(BossGroup(i))).ToList();
            foreach (var p in placements.Where(p => p.Description == null))
                p.Description = stockGroups.FirstOrDefault(g => g.Name == p.Name)?.Description;
        }

        // build 51's table lists the Minotaur first, its file the Beholder: its bosses come in the file's
        // order; a moved table is in the order of its groups already
        var stockOrder = Enumerable.Range(0, BossNames.Length).Select(BossGroup).ToList();
        int Rank(Da2Placement p) => table == StockSpawnTable && stockOrder.IndexOf(p.Name) is var i and >= 0 ? i : int.MaxValue;
        return placements.Select((p, i) => (p, i)).OrderBy(x => Rank(x.p)).ThenBy(x => x.i).Select(x => x.p).ToList();
    }

    public static void ValidatePlacements(List<Da2Placement> placements, IEnumerable<string> otherGroupNames)
    {
        var included = placements.Where(p => p.Include).ToList();
        if (included.Count > MaxPlacements) throw new ArgumentException($"At most {MaxPlacements} creatures can be placed.");
        var names = new HashSet<string>(otherGroupNames, StringComparer.Ordinal);
        foreach (var p in included)
        {
            var what = string.IsNullOrWhiteSpace(p.Name) ? "A placed creature" : $"[{p.Name}]";
            if (string.IsNullOrWhiteSpace(p.Name) || p.Name.IndexOfAny(new[] { '[', ']', '\n', '\r' }) >= 0)
                throw new ArgumentException($"{what}: the group needs a name without brackets.");
            if (!names.Add(p.Name)) throw new ArgumentException($"{what}: another group has that name.");
            if (!IsName(p.Level) || p.Level.Length > 15) throw new ArgumentException($"{what}: give the level's name (e.g. trollb).");
            if (!IsName(p.Type) || p.Type.Length > 31) throw new ArgumentException($"{what}: give the creature's type (e.g. Minotaur).");
            foreach (var v in new[] { p.X, p.Y, p.Z })
            {
                if (float.IsNaN(v) || Math.Abs(v) > 100000) throw new ArgumentException($"{what}: the position must be within ±100000.");
            }

            if (p.Facing < short.MinValue || p.Facing > short.MaxValue)
                throw new ArgumentException($"{what}: the facing must be from -32768 to 32767.");
            if (p.Properties.Count > 8 || p.Properties.Any(x => !x.Contains('=') || x.Length > 40 || x.Any(c => c <= ' ' || c > '~')))
                throw new ArgumentException($"{what}: properties are up to eight name=value words, e.g. hpscale=2.");
        }
    }

    private static bool IsName(string s) => s.Length > 0 && s.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');
}
