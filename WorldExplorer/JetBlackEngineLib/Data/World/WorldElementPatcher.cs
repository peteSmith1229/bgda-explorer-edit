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
}
