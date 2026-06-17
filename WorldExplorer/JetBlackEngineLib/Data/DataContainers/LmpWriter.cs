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
using System.Text;

namespace JetBlackEngineLib.Data.DataContainers;

/// <summary>
/// Serialises an <see cref="LmpFile"/> (including any pending edits, deletions,
/// and additions) back to the on-disk binary format.
///
/// <para><b>BGDA / BoS on-disk layout (default):</b></para>
/// <code>
///   [4]        numEntries  (int32 LE)
///   [n × 64]   directory — one 64-byte slot per entry:
///                [0..55]  null-terminated ASCII filename (max 55 chars + NUL)
///                [56..59] dataOffset — byte offset from the start of the file (int32 LE)
///                [60..63] dataLength (int32 LE)
///   [variable] entry data blobs, packed sequentially, 4-byte aligned
/// </code>
///
/// <para><b>RTA / JLH on-disk layout:</b></para>
/// <code>
///   [4]        numEntries  (int32 LE)
///   [n × 12]   directory entries:
///                [0..3]   stringOffset — byte offset within file to the null-terminated filename (int32 LE)
///                [4..7]   dataOffset   — byte offset within file to entry data (int32 LE)
///                [8..11]  dataLength   (int32 LE)
///   [variable] null-terminated filename strings, packed sequentially
///   [variable] entry data blobs, 4-byte aligned
/// </code>
///
/// <para>
/// <b>Round-trip note:</b> this writer always produces a <em>standalone</em> LMP
/// (start-offset = 0). Embedded LMPs inside GOB archives carry an additional
/// base offset that offsets all directory pointers. If you need to re-embed a
/// repacked LMP into an existing GOB you must adjust the GOB entry length and,
/// for BGDA-format GOBs, patch its own directory entry size field accordingly.
/// </para>
/// </summary>
public static class LmpWriter
{
    private const int BgdaHeaderEntryBytes = 64;
    private const int BgdaFilenameSizeBytes = 56; // 55 usable chars + NUL
    private const int RtaDirectoryEntryBytes = 12;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Packs <paramref name="lmpFile"/> — applying any pending edits, deletions,
    /// and additions — and returns the resulting byte array ready to be written
    /// to disk.  If the archive has no pending edits its original bytes are
    /// returned verbatim.
    /// </summary>
    public static byte[] Pack(LmpFile lmpFile)
    {
        // Untouched archive → byte-identical copy, no re-pack.
        if (!lmpFile.IsDirty)
            return lmpFile.GetRawData();
 
        // Defensive: the directory is lazily loaded by the tree view; make sure
        // it is populated before building the effective entry list, otherwise
        // the original entries would be silently dropped.
        if (lmpFile.Directory.Count == 0)
            lmpFile.ReadDirectory();
 
        var entries = BuildEffectiveEntries(lmpFile);
        return PackEntries(lmpFile.EngineVersion, entries);
    }

    /// <summary>
    /// Packs an arbitrary ordered list of (name, data) pairs into a new LMP
    /// binary using the encoding appropriate for <paramref name="version"/>.
    /// </summary>
    public static byte[] PackEntries(EngineVersion version,
                                     IList<(string Name, byte[] Data)> entries)
    {
        return version is EngineVersion.ReturnToArms or EngineVersion.JusticeLeagueHeroes
            ? PackRta(entries)
            : PackBgda(entries);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Merges the original archive directory with pending edits/deletions/
    /// additions into a flat ordered list of (name, bytes) pairs.
    /// </summary>
    private static List<(string Name, byte[] Data)> BuildEffectiveEntries(LmpFile src)
    {
        var result = new List<(string Name, byte[] Data)>(src.Directory.Count);

        foreach (var (name, info) in src.Directory)
        {
            // Skip entries the user has scheduled for deletion.
            if (src.PendingDeletions.Contains(name))
                continue;

            byte[] data;
            if (src.PendingEdits.TryGetValue(name, out var replaced))
            {
                // Use the replacement bytes queued by the user.
                data = replaced;
            }
            else
            {
                // Slice the original bytes out of FileData.
                data = new byte[info.Length];
                Buffer.BlockCopy(src.FileData, info.StartOffset, data, 0, info.Length);
            }

            result.Add((name, data));
        }

        // Append brand-new entries that don't exist in the original Directory.
        foreach (var (name, data) in src.PendingAdditions)
        {
            if (!src.PendingDeletions.Contains(name))
                result.Add((name, data));
        }

        return result;
    }

    // ---- BGDA / BoS format --------------------------------------------------

    private const int BgdaEntryAlignment = 128;   // matches original BGDA LMP layout
    private static int Align(int v, int a) => (v + a - 1) & ~(a - 1);
 
    private static byte[] PackBgda(IList<(string Name, byte[] Data)> entries)
    {
        var n = entries.Count;
        var dirSize = 4 + n * BgdaHeaderEntryBytes;
 
        var dataOffsets = new int[n];
        var cursor    = Align(dirSize, BgdaEntryAlignment);   // first entry 128-aligned
        var totalSize = cursor;
        for (var i = 0; i < n; i++)
        {
            dataOffsets[i] = cursor;
            var end        = cursor + entries[i].Data.Length;
            totalSize      = end;                             // last entry's true end
            cursor         = Align(end, BgdaEntryAlignment);  // next entry start
        }
 
        var result = new byte[totalSize];                     // no trailing pad
 
        BitConverter.GetBytes(n).CopyTo(result, 0);
 
        for (var i = 0; i < n; i++)
        {
            var headerOff = 4 + i * BgdaHeaderEntryBytes;
            var nameBytes = Encoding.ASCII.GetBytes(entries[i].Name);
            var copyLen   = Math.Min(nameBytes.Length, BgdaFilenameSizeBytes - 1);
            Array.Copy(nameBytes, 0, result, headerOff, copyLen);
 
            BitConverter.GetBytes(dataOffsets[i]).CopyTo(result, headerOff + 56);
            BitConverter.GetBytes(entries[i].Data.Length).CopyTo(result, headerOff + 60);
 
            entries[i].Data.CopyTo(result, dataOffsets[i]);
        }
 
        return result;
    }

    // ---- RTA / JLH format ---------------------------------------------------

    private static byte[] PackRta(IList<(string Name, byte[] Data)> entries)
    {
        var n = entries.Count;

        // Build string table and record each name's offset within it.
        var stringOffsets = new int[n];
        var stringBytes   = new List<byte>();
        for (var i = 0; i < n; i++)
        {
            stringOffsets[i] = stringBytes.Count;
            stringBytes.AddRange(Encoding.ASCII.GetBytes(entries[i].Name));
            stringBytes.Add(0); // NUL terminator
        }
        var stringTable = stringBytes.ToArray();

        // Layout:
        //   [4]               numEntries
        //   [n * 12]          directory
        //   [stringTable]     packed filenames
        //   (padding to 4)
        //   [data blobs]      4-byte aligned

        var entriesBlockStart = 4;
        var stringBlockStart  = entriesBlockStart + n * RtaDirectoryEntryBytes;
        var dataBlockStart    = Align4(stringBlockStart + stringTable.Length);

        var dataOffsets = new int[n];
        var cursor = dataBlockStart;
        for (var i = 0; i < n; i++)
        {
            dataOffsets[i] = cursor;
            cursor += entries[i].Data.Length;
            cursor = Align4(cursor);
        }

        var result = new byte[cursor];

        // Entry count
        BitConverter.GetBytes(n).CopyTo(result, 0);

        for (var i = 0; i < n; i++)
        {
            var slotOff = entriesBlockStart + i * RtaDirectoryEntryBytes;
            // String offset = absolute position within new file
            BitConverter.GetBytes(stringBlockStart + stringOffsets[i]).CopyTo(result, slotOff);
            BitConverter.GetBytes(dataOffsets[i]).CopyTo(result, slotOff + 4);
            BitConverter.GetBytes(entries[i].Data.Length).CopyTo(result, slotOff + 8);

            // Payload
            entries[i].Data.CopyTo(result, dataOffsets[i]);
        }

        // String table
        stringTable.CopyTo(result, stringBlockStart);

        return result;
    }

    // ---- Utility ------------------------------------------------------------

    private static int Align4(int v) => (v + 3) & ~3;
}
