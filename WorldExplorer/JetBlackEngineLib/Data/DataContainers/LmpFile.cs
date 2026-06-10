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

        PendingDeletions.Add(entryName);
        PendingEdits.Remove(entryName);
        PendingAdditions.RemoveAll(
            a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Schedules a new entry to be appended when the archive is next packed.
    /// If an entry with the same name already exists in <see cref="Directory"/>
    /// it is treated as a replacement instead.
    /// </summary>
    public void AddEntry(string entryName, byte[] data)
    {
        if (Directory.ContainsKey(entryName))
        {
            // Treat as a replacement of an existing entry.
            ReplaceEntry(entryName, data);
            return;
        }

        // Remove any prior pending addition with the same name.
        PendingAdditions.RemoveAll(
            a => string.Equals(a.Name, entryName, StringComparison.OrdinalIgnoreCase));
        PendingDeletions.Remove(entryName);
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

    // -------------------------------------------------------------------------
    // Existing read-directory logic (unchanged)
    // -------------------------------------------------------------------------

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
