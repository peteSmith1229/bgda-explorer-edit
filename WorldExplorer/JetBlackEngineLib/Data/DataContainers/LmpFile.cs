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

using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.DataContainers;

public class LmpFile
{
    protected readonly int _dataLen;

    // Changed from private → protected so subclasses (ClpFile) and the new
    // public EngineVersion property can reference it directly.
    protected readonly EngineVersion _engineVersion;

    protected readonly int _startOffset;

    /// <summary>
    /// A directory of embedded files where the file names are the keys.
    /// </summary>
    public readonly Dictionary<string, EntryInfo> Directory = new();

    /// <summary>
    /// The raw data of the .lmp file.
    /// </summary>
    public readonly byte[] FileData;

    /// <summary>
    /// The .lmp file name.
    /// </summary>
    public readonly string Name;

    // -------------------------------------------------------------------------
    // Edit-state (NEW)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pending entry-data replacements keyed by entry name (case-insensitive).
    /// When the archive is repacked via <see cref="LmpWriter"/>, each key's
    /// value overrides the original bytes from <see cref="FileData"/>.
    /// Does <em>not</em> modify <see cref="FileData"/> in-place.
    /// </summary>
    public readonly Dictionary<string, byte[]> PendingEdits =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Entry names scheduled for deletion.  Applied when packing via
    /// <see cref="LmpWriter"/>.
    /// </summary>
    public readonly HashSet<string> PendingDeletions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// New entries that do not yet exist in <see cref="Directory"/> but will be
    /// appended when the archive is next packed.  Each element is (entryName, rawBytes).
    /// </summary>
    public readonly List<(string Name, byte[] Data)> PendingAdditions = new();

    /// <summary>
    /// True when there are unsaved edits (replacements, deletions, or additions).
    /// The title bar and Save-Archive menu item observe this.
    /// </summary>
    public bool IsDirty =>
        PendingEdits.Count > 0 ||
        PendingDeletions.Count > 0 ||
        PendingAdditions.Count > 0;

    /// <summary>
    /// Longest entry name in a Dark Alliance archive: the directory's name field
    /// is 56 bytes, terminator included.
    /// </summary>
    public const int MaxEntryNameLength = 55;

    private bool _directoryRead;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public LmpFile(EngineVersion engineVersion, string name, byte[] data,
                   int startOffset, int dataLen)
    {
        _engineVersion = engineVersion;
        Name           = name;
        FileData       = data;
        _startOffset   = startOffset;
        _dataLen       = dataLen;
    }

    // -------------------------------------------------------------------------
    // Public properties (NEW)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Exposes the engine version so <see cref="LmpWriter"/> can pick the
    /// correct on-disk serialisation format.
    /// </summary>
    public EngineVersion EngineVersion => _engineVersion;

    /// <summary>Length of the archive as loaded (its size inside the GOB, or the file size).</summary>
    public int DataLength => _dataLen;

    // -------------------------------------------------------------------------
    // Edit helpers (NEW)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Schedules <paramref name="newData"/> to replace the bytes for
    /// <paramref name="entryName"/> in the next <see cref="LmpWriter.Pack"/>
    /// call.  Throws if the entry does not exist in <see cref="Directory"/>.
    /// </summary>
    public void ReplaceEntry(string entryName, byte[] newData)
    {
        var added = PendingAdditions.FindIndex(
            a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));
        if (added >= 0)
        {
            // A new entry: replacing it changes what will be added.
            PendingAdditions[added] = (PendingAdditions[added].Name, newData);
            return;
        }

        if (!Directory.ContainsKey(entryName))
            throw new ArgumentException(
                $"Entry '{entryName}' not found in archive '{Name}'.",
                nameof(entryName));

        PendingEdits[entryName] = newData;
        PendingDeletions.Remove(entryName);
    }

    /// <summary>
    /// Schedules <paramref name="entryName"/> for removal in the next pack.
    /// </summary>
    public void DeleteEntry(string entryName)
    {
        bool inDirectory  = Directory.ContainsKey(entryName);
        bool inAdditions  = PendingAdditions.Any(
            a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));

        if (!inDirectory && !inAdditions)
            throw new ArgumentException(
                $"Entry '{entryName}' not found in archive '{Name}'.",
                nameof(entryName));

        if (!inDirectory)
        {
            // Only ever added: forgetting it leaves the archive as it was.
            PendingAdditions.RemoveAll(
                a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));
            return;
        }

        PendingDeletions.Add(entryName);
        PendingEdits.Remove(entryName);
        PendingAdditions.RemoveAll(
            a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Schedules a new entry to be added when the archive is next packed.
    /// If an entry with the same name (ignoring case) already exists in
    /// <see cref="Directory"/> it is treated as a replacement instead.
    /// </summary>
    public void AddEntry(string entryName, byte[] data)
    {
        // The directory is read lazily; an entry added before it is read must
        // still be checked against (and saved with) the entries already there.
        EnsureDirectory();

        if (FindDirectoryName(entryName) is { } existing)
        {
            // Treat as a replacement of the existing entry: two names differing
            // only in case would share their pending edits.
            ReplaceEntry(existing, data);
            return;
        }

        if (IsAdded(entryName))
        {
            // Added already: this replaces its data, under the name it was added as.
            ReplaceEntry(entryName, data);
            return;
        }

        PendingAdditions.Add((entryName, data));
    }

    /// <summary>
    /// Clears all pending edits — call this after a successful
    /// <see cref="LmpWriter.Pack"/> + disk-write so <see cref="IsDirty"/>
    /// returns false again.
    /// </summary>
    public void ClearPendingEdits()
    {
        PendingEdits.Clear();
        PendingDeletions.Clear();
        PendingAdditions.Clear();
    }

    /// <summary>True when <paramref name="entryName"/> is a new entry, added since the archive was opened.</summary>
    public bool IsAdded(string entryName) => PendingAdditions.Any(
        a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The directory's spelling of <paramref name="entryName"/> (ignoring case,
    /// an exact match first), or null when the directory has no such entry.
    /// </summary>
    public string? FindDirectoryName(string entryName)
    {
        EnsureDirectory();
        if (Directory.ContainsKey(entryName)) return entryName;
        return Directory.Keys.FirstOrDefault(n => string.Equals(n, entryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The name an entry added as <paramref name="entryName"/> is listed and
    /// saved under: the existing entry's (ignoring case) when it replaces one.
    /// </summary>
    public string AddedEntryName(string entryName) =>
        FindDirectoryName(entryName) ??
        PendingAdditions.Select(a => a.Name).FirstOrDefault(n => string.Equals(n, entryName, StringComparison.OrdinalIgnoreCase)) ??
        entryName;

    /// <summary>True when the archive has (or will have) an entry named <paramref name="entryName"/>, ignoring case.</summary>
    public bool HasEntry(string entryName) => FindDirectoryName(entryName) != null || IsAdded(entryName);

    /// <summary>
    /// The entry's bytes as they would be saved (an added entry, an edit or the
    /// original data), or null when it isn't in the archive or is deleted. Don't
    /// modify the result: it may be the pending data itself.
    /// </summary>
    public byte[]? GetEntryBytes(string entryName)
    {
        foreach (var (name, data) in PendingAdditions)
        {
            if (string.Equals(name, entryName, StringComparison.OrdinalIgnoreCase)) return data;
        }

        if (PendingDeletions.Contains(entryName)) return null;
        if (PendingEdits.TryGetValue(entryName, out var edit)) return edit;

        EnsureDirectory();
        if (!Directory.TryGetValue(entryName, out var info)) return null;
        var bytes = new byte[info.Length];
        Buffer.BlockCopy(FileData, info.StartOffset, bytes, 0, info.Length);
        return bytes;
    }

    /// <summary>
    /// The name the game sorts and finds an entry by. When it loads an archive (FUN_001390E8) the game drops
    /// the other region's pictures and cuts "pal_" (PAL) or "ntsc_" (NTSC) off the names of its own before it
    /// searches, so "ntsc_back.tex" and "pal_back.tex" sit where "back.tex" sorts.
    /// </summary>
    public static string GameSortKey(string name) =>
        name.StartsWith("ntsc_", StringComparison.Ordinal) ? name[5..] :
        name.StartsWith("pal_", StringComparison.Ordinal) ? name[4..] : name;

    /// <summary>
    /// <paramref name="names"/> in the game's order: by <see cref="GameSortKey"/>, byte by byte, names with the
    /// same key (an ntsc_ and pal_ pair) keeping their order.
    /// </summary>
    public static List<string> InGameOrder(IEnumerable<string> names) =>
        names.OrderBy(GameSortKey, StringComparer.Ordinal).ToList();

    /// <summary>
    /// True when the directory is in the game's name order (<see cref="GameSortKey"/>, compared byte by byte as
    /// the game does). The game finds an entry by binary search, so its archives are.
    /// </summary>
    public bool IsDirectorySorted()
    {
        string? previous = null;
        foreach (var name in Directory.Keys)
        {
            var key = GameSortKey(name);
            if (previous != null && string.CompareOrdinal(previous, key) > 0) return false;
            previous = key;
        }

        return true;
    }

    /// <summary>
    /// The names of the entries as they would be saved: the directory without
    /// deleted entries, plus added ones. An archive in name order stays in name
    /// order (see <see cref="IsDirectorySorted"/>); otherwise new entries go last.
    /// </summary>
    public List<string> EffectiveEntryNames()
    {
        EnsureDirectory();
        var names = Directory.Keys.Where(n => !PendingDeletions.Contains(n)).ToList();
        names.AddRange(PendingAdditions.Select(a => a.Name).Where(n => !PendingDeletions.Contains(n)));
        return IsDirectorySorted() ? InGameOrder(names) : names;
    }

    /// <summary>Why <paramref name="name"/> can't name an entry, or null when it can.</summary>
    public static string? CheckEntryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Enter a name.";
        if (name.Length > MaxEntryNameLength) return $"Use {MaxEntryNameLength} characters or fewer.";
        foreach (var c in name)
        {
            if (c < ' ' || c > '~' || c is '/' or '\\') return "Use plain letters, digits and punctuation, without slashes.";
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Existing read-directory logic (unchanged)
    // -------------------------------------------------------------------------

    /// <summary>Reads the directory unless it has been read already (the explorer reads it lazily).</summary>
    public void EnsureDirectory()
    {
        if (_directoryRead) return;
        ReadDirectory();
        _directoryRead = true;
    }

    public virtual void ReadDirectory()
    {
        DataReader reader = new(FileData, _startOffset, _dataLen);
        var numEntries = reader.ReadInt32();

        for (var entry = 0; entry < numEntries; ++entry)
        {
            if (_engineVersion is EngineVersion.ReturnToArms
                               or EngineVersion.JusticeLeagueHeroes)
            {
                var stringOffset = reader.ReadInt32();
                var dataOffset   = reader.ReadInt32();
                var dataLength   = reader.ReadInt32();

                var tempOffset = reader.Offset;
                reader.SetOffset(stringOffset);
                var name = reader.ReadZString();
                reader.SetOffset(tempOffset);

                Directory[name] = new(name, dataOffset + _startOffset, dataLength);
            }
            else
            {
                var headerOffset = _startOffset + 4 + (entry * 64);
                var subFileName  = DataUtil.GetString(FileData, headerOffset);
                var subOffset    = BitConverter.ToInt32(FileData, headerOffset + 56);
                var subLen       = BitConverter.ToInt32(FileData, headerOffset + 60);

                Directory[subFileName] = new(subFileName, subOffset + _startOffset, subLen);
            }
        }
    }

    public EntryInfo? FindFirstEntryWithSuffix(string suffix)
    {
        foreach (var (key, value) in Directory)
        {
            if (key.EndsWith(suffix)) return value;
        }
        return null;
    }

    public EntryInfo? FindFile(string file)
    {
        foreach (var ent in Directory)
        {
            if (string.Compare(ent.Key, file, StringComparison.InvariantCultureIgnoreCase) == 0)
                return ent.Value;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // EntryInfo (unchanged)
    // -------------------------------------------------------------------------

    public class EntryInfo
    {
        public readonly int Length;
        public string Name;
        public readonly int StartOffset;

        public EntryInfo(string name, int startOffset, int length)
        {
            Length      = length;
            Name        = name;
            StartOffset = startOffset;
        }
    }
    /// <summary>
    /// Returns the original raw bytes of this LMP exactly as they appear on
    /// disk.  For a standalone LMP this is <see cref="FileData"/> itself; for an
    /// LMP embedded in a GOB it is the slice
    /// <c>FileData[_startOffset .. _startOffset + _dataLen]</c>.
    /// Used by <see cref="GobWriter"/> to copy untouched LMPs verbatim instead
    /// of re-packing them.
    /// </summary>
    public byte[] GetRawData()
    {
        if (_startOffset == 0 && _dataLen == FileData.Length)
            return FileData;
 
        var result = new byte[_dataLen];
        Buffer.BlockCopy(FileData, _startOffset, result, 0, _dataLen);
        return result;
    }
}
