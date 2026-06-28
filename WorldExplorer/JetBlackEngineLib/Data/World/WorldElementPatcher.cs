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

namespace JetBlackEngineLib.Data.World;

/// <summary>
/// Patches edited <see cref="WorldElement"/> transform fields (position,
/// rotation flags, cos/sin) back into a copy of the raw <c>.world</c> entry
/// bytes — the write-side counterpart of <see cref="WorldFileDecoder"/>.
///
/// <para>
/// The <c>.world</c> file has a complex internal layout (VIF data, texture
/// chunk tables, …) that we never restructure.  Instead, this patcher
/// performs a surgical read-modify-write of <em>only</em> the transform
/// fields inside each fixed-size element struct, located at
/// <c>header.ElementArrayStart + ElementIndex × structSize</c>.  Everything
/// else in the file is byte-for-byte identical to the original.
/// </para>
///
/// <para>Patched fields per engine version:</para>
/// <list type="bullet">
///   <item><b>BGDA (<see cref="WorldV1Element"/>, 0x38):</b>
///         Pos (3 × i16 at +0x2A), Flags (i32 at +0x30), SinAlpha (i16 at +0x34)</item>
///   <item><b>BoS (<see cref="WorldV1BoSElement"/>, 0x50):</b>
///         Pos (3 × i16 at +0x36), Flags (i32 at +0x3C), SinAlpha (i16 at +0x40)</item>
///   <item><b>RTA / JLH (<see cref="WorldV2Element"/>, 0x3C):</b>
///         Pos (3 × i32 at +0x28), RotFlags (i32 at +0x36)</item>
/// </list>
/// </summary>
public static class WorldElementPatcher
{
    // ── WorldV1Element (BGDA, 0x38 bytes) field offsets ──────────────────────
    //   VifDataOffset +0x00, Tex2 +0x04, VifLength +0x08,
    //   Bounds1 +0x0C (12), Bounds2 +0x18 (12),
    //   TextureNum +0x24, TexCellXY +0x28, Pos +0x2A (3×i16),
    //   Flags +0x30, SinAlpha +0x34, (2 pad bytes)
    private const int V1Size      = 0x38;
    private const int V1PosOff    = 0x2A;
    private const int V1FlagsOff  = 0x30;
    private const int V1SinOff    = 0x34;

    // ── WorldV1BoSElement (BoS, 0x50 bytes) field offsets ────────────────────
    private const int BosSize     = 0x50;
    private const int BosPosOff   = 0x36;
    private const int BosFlagsOff = 0x3C;
    private const int BosSinOff   = 0x40;

    // ── WorldV2Element (RTA/JLH, 0x3C bytes) field offsets ──────────────────
    //   VifDataOffset +0x00, VifLength +0x04,
    //   Bounds1 +0x08 (12), Bounds2 +0x14 (12),
    //   TextureNum +0x20, UnknownFlag36 +0x24, Pos +0x28 (3×i32),
    //   TexCellXY +0x34, RotFlags +0x36, Unknown58 +0x3A
    private const int V2Size      = 0x3C;
    private const int V2PosOff    = 0x28;
    private const int V2RotOff    = 0x36;

    /// <summary>
    /// Returns a copy of <paramref name="originalWorldBytes"/> with the
    /// transform fields of every element in <paramref name="elements"/>
    /// patched to match the in-memory values.  The original array is not
    /// modified.
    /// </summary>
    /// <param name="originalWorldBytes">
    /// The complete raw bytes of the <c>.world</c> entry (entry-relative;
    /// i.e. <c>FileData[entry.StartOffset .. entry.StartOffset+Length]</c>).
    /// </param>
    /// <param name="elements">
    /// The decoded world elements with current (possibly edited) values.
    /// Each element's <see cref="WorldElement.ElementIndex"/> identifies its
    /// slot in the on-disk element array.
    /// </param>
    /// <param name="engineVersion">Determines the struct layout to patch.</param>
    public static byte[] Patch(byte[] originalWorldBytes,
                               IEnumerable<WorldElement> elements,
                               EngineVersion engineVersion)
    {
        var result = (byte[])originalWorldBytes.Clone();

        // Element array start lives at header offset +36 (ElementArrayStart —
        // the 10th int32 in WorldFileHeader).
        var elementArrayStart = BitConverter.ToInt32(result, 36);
        var numberOfElements  = BitConverter.ToInt32(result, 0);

        var (structSize, layout) = engineVersion switch
        {
            EngineVersion.ReturnToArms        => (V2Size,  Layout.V2),
            EngineVersion.JusticeLeagueHeroes => (V2Size,  Layout.V2),
            EngineVersion.BrotherhoodOfSteel  => (BosSize, Layout.V1Bos),
            _                                 => (V1Size,  Layout.V1),
        };

        foreach (var element in elements)
        {
            var idx = element.ElementIndex;
            if (idx < 0 || idx >= numberOfElements)
                continue; // defensive — never write out of the element array

            var baseOff = elementArrayStart + idx * structSize;
            if (baseOff + structSize > result.Length)
                continue;

            switch (layout)
            {
                case Layout.V1:
                    PatchV1Style(result, baseOff, V1PosOff, V1FlagsOff, V1SinOff, element);
                    break;
                case Layout.V1Bos:
                    PatchV1Style(result, baseOff, BosPosOff, BosFlagsOff, BosSinOff, element);
                    break;
                case Layout.V2:
                    PatchV2Style(result, baseOff, element);
                    break;
            }
        }

        return result;
    }
    
    /// <summary>
    /// Rewrites each element's world-space bounding box (min/max) into its record,
    /// in place, using the array at the header's ElementArrayStart. Element slots
    /// are taken from <see cref="WorldElement.ElementIndex"/>, so this matches the
    /// surgical <see cref="Patch"/> path (count unchanged). V1 bounds live at 0x0C
    /// (min) / 0x18 (max); V2 at 0x08 / 0x14; BoS bounds offset is unknown and
    /// skipped.
    /// </summary>
    public static void PatchBounds(byte[] world, IReadOnlyList<WorldElement> elements,
        EngineVersion engineVersion)
    {
        int structSize, boundsOff;
        switch (engineVersion)
        {
            case EngineVersion.ReturnToArms:
            case EngineVersion.JusticeLeagueHeroes: structSize = 0x3C; boundsOff = 0x08; break;
            case EngineVersion.BrotherhoodOfSteel:  structSize = 0x50; boundsOff = -1;   break;
            default:                                structSize = 0x38; boundsOff = 0x0C; break;
        }
        if (boundsOff < 0) return;   // unsupported layout

        var arrayStart = BitConverter.ToInt32(world, 36);
        foreach (var el in elements)
        {
            var rec = arrayStart + el.ElementIndex * structSize;
            var min = rec + boundsOff;       // 3 floats
            var max = rec + boundsOff + 12;  // 3 floats
            if (min < 0 || max + 12 > world.Length) continue;

            var bb = el.BoundingBox;
            WriteFloat(world, min + 0, (float)bb.X);
            WriteFloat(world, min + 4, (float)bb.Y);
            WriteFloat(world, min + 8, (float)bb.Z);
            WriteFloat(world, max + 0, (float)(bb.X + bb.SizeX));
            WriteFloat(world, max + 4, (float)(bb.Y + bb.SizeY));
            WriteFloat(world, max + 8, (float)(bb.Z + bb.SizeZ));
        }
    }
    
        /// <summary>
    /// Slides each moved element's 2D footprint in the 0x20 "topo" array (minX,
    /// minY, maxX, maxY shorts at record start) by the same delta its bounding box
    /// moved. The delta is (current BoundingBox min) − (bounds min still present in
    /// <paramref name="world"/>), so this MUST be called before <see cref="PatchBounds"/>
    /// overwrites the record bounds. Slot = <see cref="WorldElement.ElementIndex"/>
    /// (matches the surgical patch). BGDA layout only.
    /// </summary>
    public static void PatchTopoBounds(byte[] world, IReadOnlyList<WorldElement> elements,
                                       EngineVersion engineVersion)
    {
        if (engineVersion != EngineVersion.DarkAlliance) return;

        const int structSize = 0x38, recBoundsOff = 0x0C, topoRecSize = 0x1C;
        var arrayStart = BitConverter.ToInt32(world, 36);
        var offset20   = BitConverter.ToInt32(world, 0x20);
        var count1c    = BitConverter.ToInt32(world, 0x1C);
        if (offset20 <= 0) return;

        foreach (var el in elements)
        {
            var idx = el.ElementIndex;
            if (idx < 0 || idx >= count1c) continue;

            var rec = arrayStart + idx * structSize;
            if (rec + recBoundsOff + 8 > world.Length) continue;

            // Move delta = new bounds min − bounds min currently in the buffer.
            var origMinX = BitConverter.ToSingle(world, rec + recBoundsOff + 0);
            var origMinY = BitConverter.ToSingle(world, rec + recBoundsOff + 4);
            var dX = (int)Math.Round(el.BoundingBox.X - origMinX);
            var dY = (int)Math.Round(el.BoundingBox.Y - origMinY);
            if (dX == 0 && dY == 0) continue;

            var t = offset20 + idx * topoRecSize;
            if (t + 8 > world.Length) continue;
            WriteInt16(world, t + 0, (short)(BitConverter.ToInt16(world, t + 0) + dX)); // minX
            WriteInt16(world, t + 2, (short)(BitConverter.ToInt16(world, t + 2) + dY)); // minY
            WriteInt16(world, t + 4, (short)(BitConverter.ToInt16(world, t + 4) + dX)); // maxX
            WriteInt16(world, t + 6, (short)(BitConverter.ToInt16(world, t + 6) + dY)); // maxY
        }
    }
    
    /// <summary>
    /// Re-serialises the element array with a DIFFERENT set/count of elements
    /// (used for add / delete). The resized array is appended at the end of the
    /// file (16-byte aligned) and the header's <c>NumberOfElements</c> (+0x00) and
    /// <c>ElementArrayStart</c> (+0x24) are repointed at it. Nothing before the new
    /// array moves, so all existing absolute offsets (VIF data, texture tables)
    /// stay valid; the old array becomes unreferenced dead space.
    ///
    /// <para>Each element's on-disk record is copied from its
    /// <see cref="WorldElement.SourceIndex"/> slot in the ORIGINAL array (so
    /// VifDataOffset, Tex2, bounds, texture fields are preserved), then only its
    /// transform fields are overwritten. Duplicates therefore reuse their source's
    /// VIF geometry.</para>
    /// </summary>
    /// <param name="originalWorldBytes">Pristine bytes of the .world entry.</param>
    /// <param name="elements">The desired element list, in final order.</param>
    /// <param name="engineVersion">Selects the record layout.</param>
    public static byte[] Rebuild(byte[] originalWorldBytes,
                                 IReadOnlyList<WorldElement> elements,
                                 EngineVersion engineVersion)
    {
        var (structSize, layout) = engineVersion switch
        {
            EngineVersion.ReturnToArms        => (V2Size,  Layout.V2),
            EngineVersion.JusticeLeagueHeroes => (V2Size,  Layout.V2),
            EngineVersion.BrotherhoodOfSteel  => (BosSize, Layout.V1Bos),
            _                                 => (V1Size,  Layout.V1),
        };

        var origArrayStart = BitConverter.ToInt32(originalWorldBytes, 36);
        var origCount      = BitConverter.ToInt32(originalWorldBytes, 0);

        // ── Build the new array ──────────────────────────────────────────────
        var newArray = new byte[elements.Count * structSize];
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var dstOff  = i * structSize;

            // Copy the original record for this element's template slot.
            var srcIdx = element.SourceIndex;
            if (srcIdx >= 0 && srcIdx < origCount)
            {
                var srcOff = origArrayStart + srcIdx * structSize;
                if (srcOff + structSize <= originalWorldBytes.Length)
                    Array.Copy(originalWorldBytes, srcOff, newArray, dstOff, structSize);
            }

            // Overwrite only the transform fields with current values.
            switch (layout)
            {
                case Layout.V1:
                    PatchV1Style(newArray, dstOff, V1PosOff, V1FlagsOff, V1SinOff, element);
                    break;
                case Layout.V1Bos:
                    PatchV1Style(newArray, dstOff, BosPosOff, BosFlagsOff, BosSinOff, element);
                    break;
                case Layout.V2:
                    PatchV2Style(newArray, dstOff, element);
                    break;
            }
        }

        // ── Append at the end, 16-byte aligned, and repoint the header ───────
        var newArrayStart = (originalWorldBytes.Length + 15) & ~15;
        var result = new byte[newArrayStart + newArray.Length];
        Array.Copy(originalWorldBytes, 0, result, 0, originalWorldBytes.Length);
        Array.Copy(newArray, 0, result, newArrayStart, newArray.Length);

        WriteInt32(result, 0,  elements.Count);   // NumberOfElements
        WriteInt32(result, 36, newArrayStart);    // ElementArrayStart

        return result;
    }
    
        /// <summary>
    /// Registers duplicated elements in the 0x18 per-cell render lists so the game
    /// actually draws them. The renderer walks these per-cell index lists (not
    /// numElements), so a clone present only in the element array stays invisible.
    ///
    /// <para>A clone is detected as an element whose <see cref="WorldElement.ElementIndex"/>
    /// differs from its <see cref="WorldElement.SourceIndex"/>: after the post-Rebuild
    /// renumber an original has the two equal, while a clone carries its source's
    /// index. For each clone, its render index is added to every cell list that
    /// already contains the source's index — the clone sits ~2 units from its source
    /// so it shares the same cell(s), letting us mirror the source's membership
    /// instead of computing the world→cell grid mapping.</para>
    ///
    /// <para>Each affected cell list is rewritten by appending a fresh copy (old
    /// entries + new indices + the -1 terminator) at the end of the file and
    /// repointing that cell's offset-table entry. Offsets are absolute file offsets,
    /// so nothing else moves and the old list becomes dead space. BGDA only; returns
    /// the input unchanged when there are no clones.</para>
    ///
    /// <para>NOTE: assumes no deletions this commit. A delete renumbers render
    /// indices, which would desync the entries already baked into the cell lists;
    /// that path needs a full index remap and is handled separately.</para>
    /// </summary>
    public static byte[] PatchCellLists(byte[] world, IReadOnlyList<WorldElement> elements,
                                        EngineVersion engineVersion)
    {
        if (engineVersion != EngineVersion.DarkAlliance) return world;

        var perCellTopo = BitConverter.ToInt32(world, 0x18);
        var cols = BitConverter.ToInt32(world, 0x10);
        var rows = BitConverter.ToInt32(world, 0x14);
        long nCells = (long)cols * rows;
        if (perCellTopo <= 0 || nCells <= 0 || nCells > 100000) return world;
        if (perCellTopo + nCells * 4 > world.Length) return world;

        // Gather, per cell, the clone render-indices that need adding.
        var perCellAdds = new Dictionary<int, List<short>>();
        foreach (var el in elements)
        {
            if (el.ElementIndex == el.SourceIndex) continue;            // original
            if (el.ElementIndex < 0 || el.ElementIndex > short.MaxValue) continue;
            var cloneIdx = (short)el.ElementIndex;
            var srcIdx   = (short)el.SourceIndex;

            for (var c = 0; c < nCells; c++)
            {
                var listOff = BitConverter.ToInt32(world, perCellTopo + c * 4);
                if (listOff <= 0 || listOff > world.Length - 2) continue;
                if (!CellListContains(world, listOff, srcIdx)) continue;

                if (!perCellAdds.TryGetValue(c, out var adds))
                    perCellAdds[c] = adds = new List<short>();
                if (!adds.Contains(cloneIdx)) adds.Add(cloneIdx);
            }
        }
        if (perCellAdds.Count == 0) return world;

        // Append each rewritten list; repoint its offset in-place (header region).
        var buf = new List<byte>(world);
        foreach (var kv in perCellAdds)
        {
            var c       = kv.Key;
            var listOff = BitConverter.ToInt32(world, perCellTopo + c * 4);
            var entries = ReadCellList(world, listOff);
            foreach (var a in kv.Value)
                if (!entries.Contains(a)) entries.Add(a);

            var newOff = buf.Count;
            foreach (var e in entries) { buf.Add((byte)e); buf.Add((byte)(e >> 8)); }
            buf.Add(0xFF); buf.Add(0xFF);                              // -1 terminator

            var slot = perCellTopo + c * 4;                           // absolute offset
            buf[slot + 0] = (byte)newOff;
            buf[slot + 1] = (byte)(newOff >> 8);
            buf[slot + 2] = (byte)(newOff >> 16);
            buf[slot + 3] = (byte)(newOff >> 24);
        }
        return buf.ToArray();
    }

    /// <summary>True if <paramref name="value"/> appears in the -1-terminated short
    /// list at <paramref name="off"/>.</summary>
    private static bool CellListContains(byte[] w, int off, short value)
    {
        for (var p = off; p >= 0 && p <= w.Length - 2; p += 2)
        {
            var v = BitConverter.ToInt16(w, p);
            if (v < 0) return false;                                  // -1 terminator
            if (v == value) return true;
        }
        return false;
    }

    /// <summary>Reads the -1-terminated short list at <paramref name="off"/> (the
    /// terminator is not included).</summary>
    private static List<short> ReadCellList(byte[] w, int off)
    {
        var list = new List<short>();
        for (var p = off; p >= 0 && p <= w.Length - 2; p += 2)
        {
            var v = BitConverter.ToInt16(w, p);
            if (v < 0) break;
            list.Add(v);
        }
        return list;
    }


    // ─────────────────────────────────────────────────────────────────────────

    private enum Layout { V1, V1Bos, V2 }

    /// <summary>
    /// BGDA / BoS style: Pos is 3 × i16 (world units × 16), the low 16 bits
    /// of Flags hold the UsesRotFlags / NegYaxis bits, the high 16 bits hold
    /// either the XYZ rot flags (3 bits) or cos·32767, and SinAlpha is a
    /// separate i16 (sin·32767).
    ///
    /// Read-modify-write: bits we do not model (anything in the low word
    /// other than 0x01/0x40, and high-word bits above the rot-flag mask when
    /// UsesRotFlags is set) are preserved from the original file.
    /// </summary>
    private static void PatchV1Style(byte[] buf, int baseOff,
                                     int posOff, int flagsOff, int sinOff,
                                     WorldElement element)
    {
        // ── Position: Vector3Short = round(Position × 16) ──────────────────
        WriteInt16(buf, baseOff + posOff,     (short)Math.Round(element.Position.X * 16.0));
        WriteInt16(buf, baseOff + posOff + 2, (short)Math.Round(element.Position.Y * 16.0));
        WriteInt16(buf, baseOff + posOff + 4, (short)Math.Round(element.Position.Z * 16.0));

        // ── Flags (read-modify-write) ───────────────────────────────────────
        var origFlags = BitConverter.ToInt32(buf, baseOff + flagsOff);
        var low16     = origFlags & 0xFFFF;

        // Update the bits the editor controls; leave all others untouched.
        low16 = element.UsesRotFlags ? (low16 | 0x01) : (low16 & ~0x01);
        low16 = element.NegYaxis     ? (low16 | 0x40) : (low16 & ~0x40);

        int high16;
        if (element.UsesRotFlags)
        {
            // Preserve any high-word bits beyond the 3 rot-flag bits.
            var origHigh = (origFlags >> 16) & 0xFFFF;
            high16 = (origHigh & ~0x7) | (element.XyzRotFlags & 0x7);
        }
        else
        {
            high16 = (short)Math.Round(element.CosAlpha * 32767.0) & 0xFFFF;
        }

        WriteInt32(buf, baseOff + flagsOff, (high16 << 16) | low16);

        // ── SinAlpha ────────────────────────────────────────────────────────
        if (!element.UsesRotFlags)
        {
            WriteInt16(buf, baseOff + sinOff,
                (short)Math.Round(element.SinAlpha * 32767.0));
        }
        // When UsesRotFlags is set the sin field is unused; leave the
        // original value in place.
    }

    /// <summary>
    /// RTA / JLH style: Pos is 3 × i32 (world units × 16) and the rotation
    /// is a plain int rot-flags field.  Cos/sin do not exist in this layout.
    /// </summary>
    private static void PatchV2Style(byte[] buf, int baseOff, WorldElement element)
    {
        WriteInt32(buf, baseOff + V2PosOff,     (int)Math.Round(element.Position.X * 16.0));
        WriteInt32(buf, baseOff + V2PosOff + 4, (int)Math.Round(element.Position.Y * 16.0));
        WriteInt32(buf, baseOff + V2PosOff + 8, (int)Math.Round(element.Position.Z * 16.0));

        WriteInt32(buf, baseOff + V2RotOff, element.XyzRotFlags);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static void WriteInt16(byte[] buf, int off, short value)
    {
        buf[off]     = (byte)(value & 0xFF);
        buf[off + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteInt32(byte[] buf, int off, int value)
    {
        buf[off]     = (byte)(value & 0xFF);
        buf[off + 1] = (byte)((value >> 8) & 0xFF);
        buf[off + 2] = (byte)((value >> 16) & 0xFF);
        buf[off + 3] = (byte)((value >> 24) & 0xFF);
    }
    
    private static void WriteFloat(byte[] buf, int off, float val)
    {
        var b = BitConverter.GetBytes(val);
        buf[off + 0] = b[0];
        buf[off + 1] = b[1];
        buf[off + 2] = b[2];
        buf[off + 3] = b[3];
    }
    
        /// <summary>
    /// Rebuilds the 0x18 per-cell render lists to match the current element set
    /// after an add/delete, and clamps the 0x20 footprint count for safety.
    ///
    /// <para>Each cell list holds on-disk render indices. A delete renumbers every
    /// survivor, so this translates each surviving entry old→new via
    /// <see cref="WorldElement.OriginalIndex"/> → <see cref="WorldElement.ElementIndex"/>,
    /// drops entries whose element was deleted, and adds each clone
    /// (<c>OriginalIndex == -1</c>) into whatever cell its source occupied (the clone
    /// sits ~2 units from its source). Lists are rewritten by appending the new list
    /// and repointing that cell's absolute offset; unchanged cells are left alone.</para>
    ///
    /// <para>count1c (0x1C) is clamped to min(new element count, original count1c) so
    /// the 0x20 array is never iterated past the new element count after a delete.</para>
    ///
    /// BGDA layout only; returns the input unchanged on a non-BGDA or malformed file.
    /// </summary>
    public static byte[] RebuildCellLists(byte[] world, IReadOnlyList<WorldElement> elements,
                                          EngineVersion engineVersion)
    {
        if (engineVersion != EngineVersion.DarkAlliance) return world;

        var perCellTopo = BitConverter.ToInt32(world, 0x18);
        var cols = BitConverter.ToInt32(world, 0x10);
        var rows = BitConverter.ToInt32(world, 0x14);
        long nCells = (long)cols * rows;
        if (perCellTopo <= 0 || nCells <= 0 || nCells > 100000) return world;
        if (perCellTopo + nCells * 4 > world.Length) return world;

        var buf = new List<byte>(world);

        // (1) Clamp count1c so the 0x20 footprint array isn't read past the new
        //     element count (delete) — and stays put for duplicates.
        var origCount1c = BitConverter.ToInt32(world, 0x1C);
        var safeCount1c = Math.Min(elements.Count, origCount1c);
        buf[0x1C] = (byte)safeCount1c;
        buf[0x1D] = (byte)(safeCount1c >> 8);
        buf[0x1E] = (byte)(safeCount1c >> 16);
        buf[0x1F] = (byte)(safeCount1c >> 24);

        // (2) old on-disk render index -> new render index (surviving originals);
        //     and each clone gathered under its source's original index.
        var remap = new Dictionary<int, int>();
        var cloneAdds = new Dictionary<int, List<int>>();
        foreach (var el in elements)
        {
            if (el.OriginalIndex >= 0)
            {
                remap[el.OriginalIndex] = el.ElementIndex;
            }
            else
            {
                if (!cloneAdds.TryGetValue(el.SourceIndex, out var lst))
                    cloneAdds[el.SourceIndex] = lst = new List<int>();
                lst.Add(el.ElementIndex);
            }
        }

        // (3) Remap every cell list; rewrite only those that actually changed.
        for (var c = 0; c < nCells; c++)
        {
            var listOff = BitConverter.ToInt32(world, perCellTopo + c * 4);
            if (listOff <= 0 || listOff > world.Length - 2) continue;

            var oldList = ReadCellList(world, listOff);
            var newList = new List<int>();
            foreach (var v in oldList)
            {
                if (remap.TryGetValue(v, out var nv) && !newList.Contains(nv))
                    newList.Add(nv);                       // survivor: old -> new
                if (cloneAdds.TryGetValue(v, out var clones))
                    foreach (var cn in clones)             // clones of v go where v was
                        if (!newList.Contains(cn)) newList.Add(cn);
                // (entries not in remap and not a clone source = deleted -> dropped)
            }

            if (CellListEquals(world, listOff, newList)) continue;

            var newOff = buf.Count;
            foreach (var e in newList) { buf.Add((byte)e); buf.Add((byte)(e >> 8)); }
            buf.Add(0xFF); buf.Add(0xFF);                  // -1 terminator

            var slot = perCellTopo + c * 4;                // absolute offset, in place
            buf[slot + 0] = (byte)newOff;
            buf[slot + 1] = (byte)(newOff >> 8);
            buf[slot + 2] = (byte)(newOff >> 16);
            buf[slot + 3] = (byte)(newOff >> 24);
        }

        return buf.ToArray();
    }

    /// <summary>True if the -1-terminated short list at <paramref name="off"/> is
    /// exactly <paramref name="list"/> (used to skip cells that didn't change).</summary>
    private static bool CellListEquals(byte[] w, int off, List<int> list)
    {
        var i = 0;
        for (var p = off; p >= 0 && p <= w.Length - 2; p += 2)
        {
            int v = BitConverter.ToInt16(w, p);
            if (v < 0) return i == list.Count;             // terminator: equal iff all consumed
            if (i >= list.Count || v != list[i]) return false;
            i++;
        }
        return false;
    }

    // ReadCellList(byte[], int) is reused unchanged from the previous cell-list work.
}
