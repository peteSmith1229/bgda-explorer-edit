/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using JetBlackEngineLib.Data.Armour;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Textures;

/// <summary>
/// A .YAK file (Dark Alliance's ARMOR.YAK): a 0x1000-byte header of entries, each four children of
/// {u32 VIF length, u32 file offset, u32 total length}, then each child's VIF model followed by its texture
/// on 0x800-byte sectors. Children can be replaced and entries added; <see cref="Write"/> keeps every child
/// where it was when it still fits, so an unedited file comes back byte for byte.
/// </summary>
public class YakFile
{
    /// <summary>A child's model and texture. The arrays are never changed: an edit makes a new child.</summary>
    public sealed record Child(byte[] Vif, byte[] Tex)
    {
        public int Length => Vif.Length + Tex.Length;
    }

    public EngineVersion EngineVersion { get; }

    /// <summary>The entries as read from <see cref="FileData"/> (offsets of the file as opened).</summary>
    public readonly List<YakEntry> Entries = new();

    /// <summary>
    /// The raw data of the .yak file as opened.
    /// </summary>
    public readonly byte[] FileData;

    /// <summary>
    /// The .yak file name.
    /// </summary>
    public readonly string Name;

    private List<Child[]>? _original;
    private List<Child[]>? _current;
    private List<Child[]>? _saved;

    public YakFile(EngineVersion engineVersion, string name, byte[] data)
    {
        EngineVersion = engineVersion;
        Name = name;
        FileData = data;
    }

    public void ReadEntries()
    {
        Entries.Clear();
        _original = new List<Child[]>();
        for (var e = 0; e < ArmorYakInfo.MaxEntries; e++)
        {
            var o = e * ArmorYakInfo.EntrySize;
            if (o + ArmorYakInfo.EntrySize > FileData.Length) break;
            var children = new YakEntryChild[4];
            var bytes = new Child[4];
            if (BitConverter.ToInt32(FileData, o) == 0) break;
            for (var c = 0; c < 4; c++)
            {
                var t = BitConverter.ToInt32(FileData, o + 12 * c);
                var v = BitConverter.ToInt32(FileData, o + 12 * c + 4);
                var l = BitConverter.ToInt32(FileData, o + 12 * c + 8);
                if (t <= 0 || t > l || v < 0 || v + l > FileData.Length)
                    throw new InvalidOperationException($"{Name}: entry {e} child {c} points outside the file.");
                children[c] = new YakEntryChild { TextureOffset = t, VifOffset = v, VifLength = l, Index = c };
                bytes[c] = new Child(FileData.AsSpan(v, t).ToArray(), FileData.AsSpan(v + t, l - t).ToArray());
            }

            Entries.Add(new YakEntry(children) { Index = e });
            _original.Add(bytes);
        }

        _current = _original.Select(e => (Child[])e.Clone()).ToList();
        _saved = null;
    }

    private List<Child[]> Current
    {
        get
        {
            if (_current == null) ReadEntries();
            return _current!;
        }
    }

    /// <summary>Entries now, with added ones.</summary>
    public int EntryCount => Current.Count;

    /// <summary>The entries the file had when it was opened.</summary>
    public int OriginalEntryCount
    {
        get
        {
            _ = Current;
            return _original!.Count;
        }
    }

    /// <summary>The child's model and texture, with any edit.</summary>
    public Child GetChild(int entry, int child) => Current[entry][child];

    /// <summary>Replaces a child's model and texture (not saved until <see cref="Write"/>).</summary>
    public void SetChild(int entry, int child, byte[] vif, byte[] tex)
    {
        Current[entry][child] = new Child(vif, tex);
    }

    /// <summary>Puts back a child as it was when the file was opened.</summary>
    public void RevertChild(int entry, int child)
    {
        if (entry >= OriginalEntryCount) throw new InvalidOperationException("An added entry has no original.");
        Current[entry][child] = _original![entry][child];
    }

    /// <summary>True when the child differs from the file as opened.</summary>
    public bool IsChildEdited(int entry, int child)
    {
        if (entry >= OriginalEntryCount) return true;
        var now = Current[entry][child];
        var then = _original![entry][child];
        return !ReferenceEquals(now, then) && !(now.Vif.AsSpan().SequenceEqual(then.Vif) && now.Tex.AsSpan().SequenceEqual(then.Tex));
    }

    /// <summary>
    /// Adds <paramref name="count"/> entries at the end, copies of <paramref name="like"/> and the ones after it (a
    /// look and its enchanted look). They share the copied bytes until edited. Returns the first new entry.
    /// </summary>
    public int AddEntries(int like, int count = 2)
    {
        if (like < 0 || like + count > EntryCount) throw new ArgumentOutOfRangeException(nameof(like));
        if (EntryCount + count > ArmorYakInfo.MaxEntries)
            throw new InvalidOperationException($"The header holds at most {ArmorYakInfo.MaxEntries} entries; " +
                                                $"{ArmorYakInfo.MaxEntries - EntryCount} more fit.");
        var first = EntryCount;
        for (var k = 0; k < count; k++) Current.Add((Child[])Current[like + k].Clone());
        return first;
    }

    /// <summary>Takes back the last entry when it was added since the file was opened.</summary>
    public void RemoveLastAddedEntry()
    {
        if (EntryCount <= OriginalEntryCount) throw new InvalidOperationException("Only added entries can be taken out.");
        Current.RemoveAt(Current.Count - 1);
    }

    /// <summary>True when something differs from the file as opened.</summary>
    public bool IsDirty
    {
        get
        {
            if (_current == null) return false;
            if (EntryCount != OriginalEntryCount) return true;
            for (var e = 0; e < EntryCount; e++)
            {
                for (var c = 0; c < 4; c++)
                {
                    if (!ReferenceEquals(Current[e][c], _original![e][c])) return true;
                }
            }

            return false;
        }
    }

    /// <summary>True when something changed since <see cref="MarkSaved"/> (or since it was opened).</summary>
    public bool HasUnsavedChanges => CountUnsavedChanges() > 0;

    /// <summary>True when the child changed since <see cref="MarkSaved"/> (or since it was opened).</summary>
    public bool IsChildUnsaved(int entry, int child)
    {
        var now = Current[entry][child];
        var baseline = _saved ?? _original!;
        return entry >= baseline.Count || !ReferenceEquals(now, baseline[entry][child]);
    }

    /// <summary>Children changed since the last save, plus entries added or taken out since then.</summary>
    public int CountUnsavedChanges()
    {
        if (_current == null) return 0;
        var baseline = _saved ?? _original!;
        var count = Math.Abs(EntryCount - baseline.Count);
        for (var e = 0; e < Math.Min(EntryCount, baseline.Count); e++)
        {
            for (var c = 0; c < 4; c++)
            {
                if (!ReferenceEquals(Current[e][c], baseline[e][c])) count++;
            }
        }

        return count;
    }

    /// <summary>Records the current state as written to the opened file.</summary>
    public void MarkSaved()
    {
        _saved = Current.Select(e => (Child[])e.Clone()).ToList();
    }

    private sealed class ChildKey : IEquatable<ChildKey>
    {
        private readonly Child _child;
        private readonly int _hash;

        public ChildKey(Child child)
        {
            _child = child;
            var h = new HashCode();
            h.AddBytes(child.Vif);
            h.AddBytes(child.Tex);
            _hash = h.ToHashCode();
        }

        public bool Equals(ChildKey? other) => other != null && (ReferenceEquals(_child, other._child) ||
                                                                 _child.Vif.AsSpan().SequenceEqual(other._child.Vif) &&
                                                                 _child.Tex.AsSpan().SequenceEqual(other._child.Tex));

        public override bool Equals(object? obj) => Equals(obj as ChildKey);
        public override int GetHashCode() => _hash;
    }

    /// <summary>
    /// The file with every edit. A child stays where the opened file had it when it still fits its room (up to
    /// the next child); one that grew, and new entries, go after the end on a 0x800 sector. A child identical
    /// to one already placed points at it. An unedited file comes back byte for byte, and same-size edits keep
    /// the file's size.
    /// </summary>
    public byte[] Write()
    {
        var current = Current;
        if (current.Count > ArmorYakInfo.MaxEntries)
            throw new InvalidOperationException($"{current.Count} entries: the header holds at most {ArmorYakInfo.MaxEntries}.");
        var layout = Entries.Select(e => e.Children.Select(c => (V: c!.VifOffset, L: c.VifLength)).ToArray()).ToList();
        var origEnd = layout.Count == 0 ? ArmorYakInfo.HeaderSize : layout.SelectMany(x => x).Max(x => x.V + x.L);
        var starts = layout.SelectMany(x => x).Select(x => x.V).Distinct().OrderBy(v => v).Append(origEnd).ToList();
        var room = new Dictionary<int, int>();
        for (var i = 0; i < starts.Count - 1; i++) room[starts[i]] = starts[i + 1] - starts[i];

        var output = new List<byte>(origEnd + 0x100000);
        output.AddRange(new byte[origEnd]);
        var header = new byte[ArmorYakInfo.HeaderSize];
        var placed = new Dictionary<ChildKey, int>();
        var used = new Dictionary<int, ChildKey>();
        var end = origEnd;
        var last = 0;
        for (var e = 0; e < current.Count; e++)
        {
            for (var c = 0; c < 4; c++)
            {
                var child = current[e][c];
                var key = new ChildKey(child);
                var data = child.Vif.Concat(child.Tex).ToArray();
                int at;
                if (e < layout.Count && data.Length <= room[layout[e][c].V] &&
                    (!used.TryGetValue(layout[e][c].V, out var holder) || holder.Equals(key)))
                {
                    at = layout[e][c].V;
                    var r = room[at];
                    for (var i = 0; i < r; i++) output[at + i] = i < data.Length ? data[i] : (byte)0;
                    placed[key] = at;
                    used[at] = key;
                }
                else if (placed.TryGetValue(key, out var existing))
                {
                    at = existing;
                }
                else
                {
                    at = ArmorYakInfo.ReadSize(end);
                    output.AddRange(new byte[at - output.Count]);
                    output.AddRange(data);
                    end = output.Count;
                    placed[key] = at;
                    used[at] = key;
                }

                var h = e * ArmorYakInfo.EntrySize + 12 * c;
                BitConverter.TryWriteBytes(header.AsSpan(h, 4), child.Vif.Length);
                BitConverter.TryWriteBytes(header.AsSpan(h + 4, 4), at);
                BitConverter.TryWriteBytes(header.AsSpan(h + 8, 4), child.Length);
                last = Math.Max(last, at + child.Length);
            }
        }

        for (var i = 0; i < header.Length; i++) output[i] = header[i];
        var length = Math.Max(last, origEnd);
        return output.Take(length).ToArray();
    }

    /// <summary>
    /// What the game would trip over in an ARMOR.YAK: too many entries, children off a sector, children bigger
    /// than their slot's buffer (or than any buffer, for added entries), and models or textures it can't use.
    /// </summary>
    public static List<string> Problems(byte[] data)
    {
        var problems = new List<string>();
        var yak = new YakFile(EngineVersion.DarkAlliance, "check", data);
        try
        {
            yak.ReadEntries();
        }
        catch (InvalidOperationException ex)
        {
            problems.Add(ex.Message);
            return problems;
        }

        for (var e = 0; e < yak.EntryCount; e++)
        {
            var slot = ArmorYakInfo.SlotOf(e);
            for (var c = 0; c < 4; c++)
            {
                var where = $"Entry {e} ({ArmorYakInfo.EntryName(e)}), {ArmorYakInfo.Characters[c]}";
                var info = yak.Entries[e].Children[c]!;
                var child = yak.GetChild(e, c);
                if (info.VifOffset % ArmorYakInfo.Sector != 0) problems.Add($"{where}: not on a 0x800 sector.");
                var read = ArmorYakInfo.ReadSize(child.Length);
                var capacity = ArmorYakInfo.Capacity(slot);
                if (read > capacity)
                {
                    problems.Add(slot == ArmorSlot.Unknown
                        ? $"{where}: {read:N0} bytes are read, more than any buffer holds ({capacity:N0})."
                        : $"{where}: {read:N0} bytes are read; the {slot.ToString().ToLowerInvariant()} buffer holds {capacity:N0}.");
                }

                if (ArmorModel.Problem(child.Vif) is { } model) problems.Add($"{where}: model: {model}");
                if (ArmorTexture.Problem(child.Tex) is { } texture) problems.Add($"{where}: texture: {texture}");
            }
        }

        return problems;
    }
}
