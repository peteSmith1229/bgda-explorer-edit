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
using System.Text;

namespace JetBlackEngineLib.Data.World;

/// <summary>
/// Serialises a list of <see cref="ObjectData"/> back to the on-disk
/// <c>objects.ob</c> binary format — the exact inverse of
/// <see cref="ObDecoder.Decode"/>.
///
/// <para>On-disk layout (all little-endian, offsets relative to entry start):</para>
/// <code>
///   Header (8 bytes):
///     +0x00  i16  count
///     +0x02  i16  flags          (opaque — preserved from the original entry)
///     +0x04  i32  stringOffset   (offset to the string table)
///
///   count × records (variable size):
///     +0x00  i32  nameStringOffset   (relative to stringOffset)
///     +0x04  i16  structSize         (20 + 4 × (numProps + 1); the +1 is a null terminator)
///     +0x06  i16  i6
///     +0x08  f32  floats[0]          (X position × 4)
///     +0x0C  f32  floats[1]          (Y position × 4)
///     +0x10  f32  floats[2]          (Z position × 4)
///     +0x14  i32 × numProps          property-string offsets (relative to stringOffset)
///     +....  i32  0                  null terminator for the property array
///
///   String table:
///     null-terminated ASCII strings, packed; duplicate strings are shared.
/// </code>
/// </summary>
public static class ObEncoder
{
    /// <summary>
    /// Encodes <paramref name="objects"/> into a new <c>objects.ob</c> byte
    /// array.
    /// </summary>
    /// <param name="objects">The object list (typically <c>ObjectManager.Objects</c>).</param>
    /// <param name="flags">
    /// The opaque i16 flags value from the original entry's header (+0x02).
    /// <see cref="ObDecoder"/> skips it on read, so callers should fetch it
    /// from the original bytes to preserve it:
    /// <c>BitConverter.ToInt16(lmp.FileData, entry.StartOffset + 2)</c>.
    /// </param>
    public static byte[] Encode(IReadOnlyList<ObjectData> objects, short flags)
    {
        // ── 1. Build the string table with de-duplication ─────────────────
        var stringOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
        var stringTable   = new List<byte>();

        int InternString(string s)
        {
            if (stringOffsets.TryGetValue(s, out var existing))
                return existing;
            var off = stringTable.Count;
            stringOffsets[s] = off;
            stringTable.AddRange(Encoding.ASCII.GetBytes(s));
            stringTable.Add(0);
            return off;
        }

        // Intern all names first, then all property strings, so the layout
        // resembles the original files (names grouped, then properties).
        var nameOffsets = new int[objects.Count];
        for (var i = 0; i < objects.Count; i++)
            nameOffsets[i] = InternString(objects[i].Name ?? "");

        var propOffsets = new int[objects.Count][];
        for (var i = 0; i < objects.Count; i++)
        {
            var props = objects[i].Properties;
            propOffsets[i] = new int[props.Count];
            for (var p = 0; p < props.Count; p++)
                propOffsets[i][p] = InternString(props[p]);
        }

        // ── 2. Compute record sizes and total layout ──────────────────────
        //
        //   header(8) + records + stringTable

        const int headerSize = 8;
        var recordSizes = new int[objects.Count];
        var recordsTotal = 0;
        for (var i = 0; i < objects.Count; i++)
        {
            // 20 fixed bytes + 4 per property + 4 for the null terminator.
            recordSizes[i] = 20 + 4 * (objects[i].Properties.Count + 1);
            recordsTotal  += recordSizes[i];
        }

        var stringTableStart = headerSize + recordsTotal;
        var result = new byte[stringTableStart + stringTable.Count];

        // ── 3. Header ──────────────────────────────────────────────────────
        BitConverter.GetBytes((short)objects.Count).CopyTo(result, 0);
        BitConverter.GetBytes(flags).CopyTo(result, 2);
        BitConverter.GetBytes(stringTableStart).CopyTo(result, 4);

        // ── 4. Records ─────────────────────────────────────────────────────
        var cursor = headerSize;
        for (var i = 0; i < objects.Count; i++)
        {
            var obj = objects[i];

            BitConverter.GetBytes(nameOffsets[i]).CopyTo(result, cursor);
            BitConverter.GetBytes((short)recordSizes[i]).CopyTo(result, cursor + 4);
            BitConverter.GetBytes(obj.I6).CopyTo(result, cursor + 6);
            BitConverter.GetBytes(obj.Floats[0]).CopyTo(result, cursor + 8);
            BitConverter.GetBytes(obj.Floats[1]).CopyTo(result, cursor + 12);
            BitConverter.GetBytes(obj.Floats[2]).CopyTo(result, cursor + 16);

            var propCursor = cursor + 20;
            for (var p = 0; p < obj.Properties.Count; p++)
            {
                BitConverter.GetBytes(propOffsets[i][p]).CopyTo(result, propCursor);
                propCursor += 4;
            }
            // Null terminator int32 — result[] is zero-initialised, so the
            // four bytes at propCursor are already 0.

            cursor += recordSizes[i];
        }

        // ── 5. String table ────────────────────────────────────────────────
        stringTable.CopyTo(result, stringTableStart);

        return result;
    }
}
