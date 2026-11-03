// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/DataContainers/GameArchive.cs
//
// A Dark Alliance LMP archive as the game loads it, and an entry added to one (or taken out)
// so that the game still reads every other entry exactly as before, byte for byte.
//
// THE ARCHIVE. i32 count; count x 64-byte entries {name[56], i32 offset, i32 length}; then the
// files, each starting at a multiple of 0x80.
//
// WHAT THE GAME DOES WITH IT (FUN_001390E8, checked in an emulator on INVENTRY.LMP):
//   - each entry's offset becomes a pointer. An entry whose offset isn't past the offsets of all the
//     entries before it is a second name for an earlier entry's file (an alias: cursorleft.tex is
//     arrowleft.tex) and gets nothing more, so the files must lie in the order of their entries: a file
//     put anywhere else turns the entries after it into "aliases", and their textures are never set up;
//   - entries named ntsc_... (PAL) or pal_... (NTSC) are dropped, their files cut out and the later files
//     moved down; the other region's prefix is cut from the names;
//   - each file is handed to its type's code by its extension (tex, vif, anm, ...).
// It finds a name with a binary search (FUN_0013AE58, strcmp), so the entries must be sorted by
// their names as they are after that: by LmpFile.GameSortKey, which is how the stored order runs.
//
// So Insert puts a new entry where its name sorts and its file just before the next entry's file;
// every later file moves by the new file's size rounded up to 0x80, and nothing else changes. The
// directory has to have room for one more entry before the first file. Remove takes an inserted
// entry out and gives the archive back byte for byte. Both check the result as the game will read it
// (Problem): sorted in both regions, every name found by the game's search, the files in order.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Data.DataContainers;

/// <summary>A Dark Alliance LMP archive as the game loads it (FUN_001390E8) and finds names in it (FUN_0013AE58).</summary>
public static class GameArchive
{
    /// <summary>Files start at a multiple of this.</summary>
    public const int Alignment = 0x80;

    private const int EntrySize = 64, NameSize = 56;

    /// <summary>One directory entry: its name, where its file is and how long it is, and its place in the directory.</summary>
    public readonly record struct Entry(string Name, int Offset, int Length, int Index);

    private static int Align(int n) => (n + Alignment - 1) & ~(Alignment - 1);

    /// <summary>The directory; throws <see cref="InvalidDataException"/> when <paramref name="data"/> isn't an LMP archive.</summary>
    public static List<Entry> ReadDirectory(byte[] data)
    {
        if (data.Length < 4) throw new InvalidDataException("Not an LMP archive: it is too short.");
        var n = BitConverter.ToInt32(data, 0);
        if (n is <= 0 or > 4096 || 4 + (long)EntrySize * n > data.Length) throw new InvalidDataException("Not an LMP archive: its directory doesn't fit.");
        var list = new List<Entry>(n);
        for (var i = 0; i < n; i++)
        {
            var h = 4 + EntrySize * i;
            var end = Array.IndexOf(data, (byte)0, h, NameSize);
            var name = Encoding.ASCII.GetString(data, h, (end < 0 ? h + NameSize : end) - h);
            var offset = BitConverter.ToInt32(data, h + NameSize);
            var length = BitConverter.ToInt32(data, h + NameSize + 4);
            if (offset <= 0 || length < 0 || (long)offset + length > data.Length)
                throw new InvalidDataException($"Not an LMP archive: entry {i} ({name}) points outside the file.");
            list.Add(new Entry(name, offset, length, i));
        }

        return list;
    }

    /// <summary>The name the game shows after loading, or null when it drops the entry (the other region's).</summary>
    public static string? LoadedName(string name, bool pal = true)
    {
        var (drop, cut) = pal ? ("ntsc_", "pal_") : ("pal_", "ntsc_");
        if (name.StartsWith(drop, StringComparison.Ordinal)) return null;
        return name.StartsWith(cut, StringComparison.Ordinal) ? name[cut.Length..] : name;
    }

    /// <summary>The places of the entries the game treats as files of their own (offset past every earlier one's).</summary>
    public static List<int> FilesOfTheirOwn(IReadOnlyList<Entry> entries)
    {
        var list = new List<int>();
        var max = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Offset <= max) continue;
            list.Add(i);
            max = entries[i].Offset;
        }

        return list;
    }

    /// <summary>What the game has after loading <paramref name="data"/>: each name it keeps, in order, and its file's bytes.</summary>
    public static List<(string Name, byte[] Data)> AsLoaded(byte[] data, bool pal = true)
    {
        var entries = ReadDirectory(data);
        var own = new HashSet<int>(FilesOfTheirOwn(entries));
        var list = new List<(string, byte[])>();
        foreach (var e in entries)
        {
            var name = own.Contains(e.Index) ? LoadedName(e.Name, pal) : e.Name;
            if (name != null) list.Add((name, data.AsSpan(e.Offset, e.Length).ToArray()));
        }

        return list;
    }

    /// <summary>The game's lookup (FUN_0013AE58): a binary search with strcmp. The place of <paramref name="name"/>, or -1.</summary>
    public static int Find(IReadOnlyList<string> names, string name)
    {
        int lo = 0, hi = names.Count - 1;
        while (true)
        {
            var mid = (lo + hi) >> 1;
            if (mid < 0) return -1;
            var c = string.CompareOrdinal(name, names[mid]);
            if (c == 0) return mid;
            if (lo >= hi) return -1;
            if (c < 0) hi = mid - 1;
            else lo = mid + 1;
        }
    }

    /// <summary>True when the game finds an entry called <paramref name="name"/> in <paramref name="data"/> (PAL).</summary>
    public static bool Contains(byte[] data, string name)
    {
        var names = AsLoaded(data).Select(f => f.Name).ToList();
        return Find(names, name) >= 0;
    }

    /// <summary>Why the game wouldn't read <paramref name="data"/> as it should, or null.</summary>
    public static string? Problem(byte[] data)
    {
        List<Entry> entries;
        try
        {
            entries = ReadDirectory(data);
        }
        catch (InvalidDataException e)
        {
            return e.Message;
        }

        var first = entries.Min(e => e.Offset);
        if (4 + EntrySize * entries.Count > first) return "The directory runs into the first file.";
        var own = FilesOfTheirOwn(entries);
        var ownSet = new HashSet<int>(own);
        for (var i = 0; i < entries.Count; i++)
        {
            if (ownSet.Contains(i)) continue;
            var e = entries[i];
            if (!entries.Take(i).Any(o => o.Offset == e.Offset && o.Length == e.Length))
                return $"{e.Name}'s file comes before the files of the entries ahead of it: the game would never set it up.";
        }

        if (own.Any(i => entries[i].Offset % Alignment != 0)) return "A file doesn't start at a multiple of 0x80.";
        for (var k = 1; k < own.Count; k++)
        {
            var (a, b) = (entries[own[k - 1]], entries[own[k]]);
            if (a.Offset + a.Length > b.Offset) return $"{a.Name} runs into {b.Name}.";
        }

        foreach (var pal in new[] { true, false })
        {
            var names = AsLoaded(data, pal).Select(f => f.Name).ToList();
            for (var k = 1; k < names.Count; k++)
            {
                if (string.CompareOrdinal(names[k - 1], names[k]) >= 0)
                    return $"The directory isn't in the game's order ({names[k - 1]} before {names[k]}), so the game would miss names.";
            }

            if (names.FirstOrDefault(n => Find(names, n) < 0) is { } lost) return $"The game wouldn't find {lost}.";
        }

        return null;
    }

    /// <summary>
    /// <paramref name="data"/> with an entry <paramref name="name"/> holding <paramref name="file"/>: in the directory where its
    /// name sorts, its file just before the next entry's file (or at the end), later files moved by its size rounded up to 0x80.
    /// Throws <see cref="InvalidDataException"/> when the archive can't take it (no room in the directory, already there, or the
    /// archive isn't one the game reads as it should).
    /// </summary>
    public static byte[] Insert(byte[] data, string name, byte[] file)
    {
        if (name.Length == 0 || name.Length >= NameSize || name.Any(c => c <= ' ' || c > '~'))
            throw new ArgumentException($"\"{name}\" can't be an entry name: up to {NameSize - 1} letters, digits and punctuation.");
        if (Problem(data) is { } problem) throw new InvalidDataException(problem);
        var entries = ReadDirectory(data);
        if (entries.Any(e => e.Name == name)) throw new InvalidDataException($"The archive already has {name}.");
        var first = entries.Min(e => e.Offset);
        if (4 + EntrySize * (entries.Count + 1) > first)
            throw new InvalidDataException("The archive's directory has no room for another entry before its first file.");
        var directoryEnd = 4 + EntrySize * entries.Count;
        if (data.AsSpan(directoryEnd, first - directoryEnd).IndexOfAnyExcept((byte)0) >= 0)
            throw new InvalidDataException("The space between the archive's directory and its first file isn't empty.");

        // the place where the name sorts, and where the file goes: before the next file of its own after that place
        var key = LmpFile.GameSortKey(name);
        var k = entries.FindIndex(e => string.CompareOrdinal(LmpFile.GameSortKey(e.Name), key) > 0);
        if (k < 0) k = entries.Count;
        var next = FilesOfTheirOwn(entries).Where(i => i >= k).Select(i => (int?)i).FirstOrDefault();
        var at = next is { } n ? entries[n].Offset : Align(data.Length);
        var size = Align(file.Length);

        var list = entries.Select(e => e with { Offset = e.Offset >= at && next != null ? e.Offset + size : e.Offset }).ToList();
        list.Insert(k, new Entry(name, at, file.Length, k));
        var total = next != null ? data.Length + size : at + file.Length;
        var output = new byte[total];
        WriteDirectory(output, list, data);
        if (next != null)
        {
            Array.Copy(data, first, output, first, at - first);
            Array.Copy(file, 0, output, at, file.Length);
            Array.Copy(data, at, output, at + size, data.Length - at);
        }
        else
        {
            Array.Copy(data, first, output, first, data.Length - first);
            Array.Copy(file, 0, output, at, file.Length);
        }

        if (Problem(output) is { } wrong) throw new InvalidDataException("The archive wouldn't be read as it should: " + wrong);
        return output;
    }

    /// <summary>
    /// <paramref name="data"/> without the entry <paramref name="name"/> and its file (later files moved back): the archive as it
    /// was before <see cref="Insert"/>. Throws <see cref="InvalidDataException"/> when the entry isn't there or its file is shared.
    /// </summary>
    public static byte[] Remove(byte[] data, string name)
    {
        var entries = ReadDirectory(data);
        var k = entries.FindIndex(e => e.Name == name);
        if (k < 0) throw new InvalidDataException($"The archive has no {name}.");
        var gone = entries[k];
        if (entries.Any(e => e.Index != k && e.Offset == gone.Offset)) throw new InvalidDataException($"Another entry shares {name}'s file.");
        if (!FilesOfTheirOwn(entries).Contains(k)) throw new InvalidDataException($"{name} shares an earlier entry's file.");

        var first = entries.Min(e => e.Offset);
        var rest = entries.Where(e => e.Index != k).ToList();
        var later = entries.Where(e => e.Offset > gone.Offset).Select(e => e.Offset).DefaultIfEmpty(-1).Min();
        byte[] output;
        if (later > 0)
        {
            var size = later - gone.Offset;
            if (data.Skip(gone.Offset + gone.Length).Take(size - gone.Length).Any(b => b != 0))
                throw new InvalidDataException($"The bytes after {name}'s file aren't the zeros an insert leaves.");
            rest = rest.Select(e => e with { Offset = e.Offset > gone.Offset ? e.Offset - size : e.Offset }).ToList();
            output = new byte[data.Length - size];
            WriteDirectory(output, rest, data);
            Array.Copy(data, first, output, first, gone.Offset - first);
            Array.Copy(data, later, output, gone.Offset, data.Length - later);
        }
        else
        {
            var end = rest.Max(e => e.Offset + e.Length);
            if (data.Skip(end).Take(gone.Offset - end).Any(b => b != 0))
                throw new InvalidDataException($"The bytes before {name}'s file aren't the zeros an insert leaves.");
            output = new byte[end];
            WriteDirectory(output, rest, data);
            Array.Copy(data, first, output, first, end - first);
        }

        return output;
    }

    /// <summary>The count, the entries (each name's 56 bytes as they were, or a new name padded with zeros) and zeros to the first file.</summary>
    private static void WriteDirectory(byte[] output, IReadOnlyList<Entry> entries, byte[] original)
    {
        var old = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var e in ReadDirectory(original)) old.TryAdd(e.Name, e.Index);
        BitConverter.GetBytes(entries.Count).CopyTo(output, 0);
        for (var i = 0; i < entries.Count; i++)
        {
            var h = 4 + EntrySize * i;
            var e = entries[i];
            if (old.TryGetValue(e.Name, out var was)) Array.Copy(original, 4 + EntrySize * was, output, h, NameSize);
            else Encoding.ASCII.GetBytes(e.Name).CopyTo(output, h);
            BitConverter.GetBytes(e.Offset).CopyTo(output, h + NameSize);
            BitConverter.GetBytes(e.Length).CopyTo(output, h + NameSize + 4);
        }
    }
}
