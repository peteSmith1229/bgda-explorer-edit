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
/// Serialises a <see cref="GobFile"/> back to the on-disk binary format used
/// by Baldur's Gate: Dark Alliance.
///
/// <para>The on-disk layout is:</para>
/// <code>
///   [n × 40]   directory — one 40-byte slot per LMP entry:
///                [0x00..0x1F]  null-terminated ASCII name  (max 31 chars + NUL)
///                [0x20..0x23]  lmpOffset — absolute byte offset to the LMP blob (int32 LE)
///                [0x24..0x27]  lmpLength (int32 LE)
///   [1]        null-terminator byte  (read loop stops at the first zero-name slot)
///   [padding]  zero-fill to the next 4-byte boundary
///   [variable] LMP data blobs, packed sequentially, each 4-byte aligned
/// </code>
///
/// <para>
/// Each contained <see cref="LmpFile"/> is re-packed via <see cref="LmpWriter"/>
/// so any pending edits, deletions, and additions inside individual LMPs are
/// applied automatically.  The resulting byte array is a fully self-contained
/// GOB file with corrected directory offsets.
/// </para>
///
/// <para>
/// <b>Alignment note:</b> the original BGDA GOB files do not appear to enforce
/// a fixed sector alignment on individual LMP blobs, so this writer uses
/// 4-byte alignment as a conservative baseline.  If you discover a specific
/// game that requires a larger alignment (e.g. 0x800 for DVD sectors), adjust
/// <see cref="LmpAlignment"/> accordingly.
/// </para>
/// </summary>
public static class GobWriter
{
    /// <summary>
    /// Byte alignment applied between packed LMP blobs.  4 bytes is safe for
    /// all known BGDA variants; raise to a power-of-two sector size if needed.
    /// </summary>
    public const int LmpAlignment = 2048;   // BGDA GOBs sector-align every LMP blob

    private const int DirectoryEntrySize   = 0x28; // 40 bytes
    private const int FilenameFieldSize    = 0x20; // 32 bytes (max 31 chars + NUL)
    private const int OffsetFieldRelative  = 0x20; // within the entry
    private const int LengthFieldRelative  = 0x24; // within the entry

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Packs <paramref name="gobFile"/> — re-packing every contained
    /// <see cref="LmpFile"/> via <see cref="LmpWriter"/> so that any pending
    /// edits are applied — and returns the result as a byte array ready to be
    /// written to disk.
    /// </summary>
    public static byte[] Pack(GobFile gobFile)
    {
        var entries = gobFile.Directory.ToList(); // preserve insertion order
        return PackEntries(gobFile.EngineVersion, entries);
    }

    /// <summary>
    /// Packs an arbitrary ordered list of (name, LmpFile) pairs into a GOB
    /// binary using the standard BGDA directory format.
    ///
    /// LMPs with pending edits are re-packed via <see cref="LmpWriter"/> (which
    /// applies the edits); untouched LMPs are copied byte-for-byte from their
    /// original data, so lazy-loaded archives whose directories were never read
    /// are preserved exactly.
    /// </summary>
    public static byte[] PackEntries(EngineVersion engineVersion,
        IList<KeyValuePair<string, LmpFile>> entries)
    {
        int n = entries.Count;
 
        var packedLmps = new byte[n][];
        for (int i = 0; i < n; i++)
        {
            var lmp = entries[i].Value;
            if (lmp.IsDirty)
            {
                if (lmp.Directory.Count == 0)
                    lmp.ReadDirectory();
                packedLmps[i] = LmpWriter.Pack(lmp);
            }
            else
            {
                packedLmps[i] = lmp.GetRawData();
            }
        }
 
        int directoryBytes = n * DirectoryEntrySize + 1;     // +1 null terminator
        int dataStart      = AlignUp(directoryBytes, LmpAlignment);
 
        var lmpOffsets = new int[n];
        var cursor     = dataStart;
        var totalSize  = dataStart;
        for (int i = 0; i < n; i++)
        {
            lmpOffsets[i] = cursor;
            var end       = cursor + packedLmps[i].Length;
            totalSize     = end;                              // last blob's true end
            cursor        = AlignUp(end, LmpAlignment);       // next LMP start
        }
 
        var result = new byte[totalSize];                     // no trailing sector pad
 
        for (int i = 0; i < n; i++)
        {
            int slotOff   = i * DirectoryEntrySize;
            var nameBytes = Encoding.ASCII.GetBytes(entries[i].Key);
            var copyLen   = Math.Min(nameBytes.Length, FilenameFieldSize - 1);
            Array.Copy(nameBytes, 0, result, slotOff, copyLen);
 
            BitConverter.GetBytes(lmpOffsets[i])       .CopyTo(result, slotOff + OffsetFieldRelative);
            BitConverter.GetBytes(packedLmps[i].Length).CopyTo(result, slotOff + LengthFieldRelative);
 
            packedLmps[i].CopyTo(result, lmpOffsets[i]);
        }
 
        return result;
    }
    
    /// <summary>
/// Attempts to apply all pending edits to <paramref name="gobFile"/> WITHOUT
/// re-packing, by overwriting each edited entry's bytes in place inside a
/// clone of the original GOB data.
///
/// <para>
/// This succeeds only when every edit is <b>length-preserving</b>: no LMP has
/// pending additions or deletions, and every pending entry replacement has
/// exactly the same byte length as the entry it replaces.  A world-element
/// transform edit (move/rotate) always satisfies this, because
/// <see cref="WorldExplorer"/>'s element patcher rewrites the struct in place
/// at its original size.
/// </para>
///
/// <para>
/// Returns the patched byte array (identical to the original except for the
/// edited regions — same size, same alignment, trailing data preserved), or
/// <see langword="null"/> if any edit changes a length, in which case the
/// caller should fall back to <see cref="Pack"/>.
/// </para>
/// </summary>
public static byte[]? TryPatchInPlace(GobFile gobFile)
{
    // All LMPs in a GOB share the same underlying byte array (the whole GOB).
    var result = (byte[])gobFile.RawFileData.Clone();
 
    foreach (var lmp in gobFile.Directory.Values)
    {
        // Structural changes can't be expressed as an in-place overwrite.
        if (lmp.PendingAdditions.Count > 0 || lmp.PendingDeletions.Count > 0)
            return null;
 
        if (lmp.PendingEdits.Count == 0)
            continue; // untouched LMP — its bytes are already in the clone.
 
        // The pending-edit overlay is keyed against Directory entries; make
        // sure the directory is populated (it is lazily loaded by the tree).
        if (lmp.Directory.Count == 0)
            lmp.ReadDirectory();
 
        foreach (var (name, newBytes) in lmp.PendingEdits)
        {
            if (!lmp.Directory.TryGetValue(name, out var entry))
                return null; // edit refers to an entry we can't locate — be safe.
 
            if (newBytes.Length != entry.Length)
                return null; // length changed — must re-pack.
 
            // entry.StartOffset is absolute within the GOB byte array.
            Buffer.BlockCopy(newBytes, 0, result, entry.StartOffset, newBytes.Length);
        }
    }
 
    return result;
}
 

    // -------------------------------------------------------------------------

    private static int AlignUp(int value, int alignment) =>
        (value + alignment - 1) & ~(alignment - 1);
}
