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

using JetBlackEngineLib.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JetBlackEngineLib.Data.Textures;

/// <summary>
/// Encodes an image into a palettised PS2 <c>.tex</c> by inverting
/// <see cref="TexDecoder"/>. Supports two formats, chosen from the template:
///
/// <list type="bullet">
///   <item><b>256-colour (PSMT8)</b> — palette swizzled (32-block involution),
///         pixels 1 byte each, destWBytes = (w+0x7f)&amp;~0x7f.</item>
///   <item><b>16-colour (PSMT4)</b> — palette stored linearly (not swizzled),
///         pixels 4 bits each (nibble swizzle), destWBytes = (w+0x3f)&amp;~0x3f.</item>
/// </list>
///
/// <para><b>Template-based.</b> The encoder takes the original <c>.tex</c> being
/// replaced and swaps only the palette bytes and the swizzled pixel bytes in
/// place, preserving the header and GIF/DMA setup — so the output has the same
/// byte length and structure as the original. The replacement image must match
/// the original's dimensions.</para>
///
/// <para>Both paths were validated in Python: the 256-colour path re-encodes
/// real game textures byte-for-byte; the 16-colour swizzle inverse is proven
/// (WriteTexPSMT4 ∘ ReadTexPSMT4 == identity) and a full 16-colour round-trip is
/// pixel-perfect.</para>
/// </summary>
public static class TexEncoder
{
    private const int BITBLTBUF = 0x50;
    private const int TRXPOS    = 0x51;
    private const int TRXREG    = 0x52;

    /// <summary>
    /// True if <paramref name="templateTex"/> is a 256- or 16-colour texture
    /// this encoder can target (used to enable the import UI).
    /// </summary>
    public static bool CanEncodeInto(ReadOnlySpan<byte> templateTex)
    {
        try
        {
            var tp = ParseTemplate(templateTex);
            return tp.PaletteLen == 256 * 4 || tp.PaletteLen == 16 * 4;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encodes <paramref name="image"/> into a TEX using
    /// <paramref name="templateTex"/> for header + GIF structure. Throws if the
    /// template isn't a supported palettised texture or the dimensions differ.
    /// </summary>
    public static byte[] Encode(byte[] templateTex, BitmapSource image)
    {
        var tp = ParseTemplate(templateTex);
        if (tp.PaletteLen != 256 * 4 && tp.PaletteLen != 16 * 4)
            throw new NotSupportedException(
                "Only 256-colour (PSMT8) and 16-colour (PSMT4) textures can be replaced.");

        if (image.PixelWidth != tp.FinalW || image.PixelHeight != tp.FinalH)
            throw new ArgumentException(
                $"Replacement image must be {tp.FinalW}×{tp.FinalH} to match the texture " +
                $"(got {image.PixelWidth}×{image.PixelHeight}).");

        var bgra = ToBgra32(image);

        byte[] palBytes;
        byte[] imgBytes;
        if (tp.PaletteLen == 256 * 4)
            EncodePsmt8(tp, bgra, out palBytes, out imgBytes);
        else
            EncodePsmt4(tp, bgra, out palBytes, out imgBytes);

        if (palBytes.Length != tp.PaletteLen)
            throw new InvalidOperationException("Palette size mismatch with template.");
        if (imgBytes.Length != tp.ImageLen)
            throw new InvalidOperationException(
                $"Encoded image size {imgBytes.Length} != template {tp.ImageLen}.");

        var outBytes = (byte[])templateTex.Clone();
        Array.Copy(palBytes, 0, outBytes, tp.PaletteData, palBytes.Length);
        Array.Copy(imgBytes, 0, outBytes, tp.ImageData, imgBytes.Length);
        return outBytes;
    }

    // ── 256-colour (PSMT8) ─────────────────────────────────────────────────────

    private static void EncodePsmt8(TemplateInfo tp, byte[] bgra,
        out byte[] palBytes, out byte[] imgBytes)
    {
        var w = tp.FinalW;
        var h = tp.FinalH;

        Quantize(bgra, w, h, 256, out var palette, out var indices);

        var palEntries = new (byte R, byte G, byte B, byte A)[256];
        for (var i = 0; i < 256; i++)
        {
            var (r, g, b, a) = palette[i];
            palEntries[i] = (r, g, b, ToPs2Alpha(a));
        }
        SwizzlePalette256(palEntries);   // 256-entry palettes ARE swizzled

        palBytes = new byte[256 * 4];
        for (var i = 0; i < 256; i++)
        {
            palBytes[i * 4 + 0] = palEntries[i].R;
            palBytes[i * 4 + 1] = palEntries[i].G;
            palBytes[i * 4 + 2] = palEntries[i].B;
            palBytes[i * 4 + 3] = palEntries[i].A;
        }

        var destWBytes = (w + 0x7f) & ~0x7f;
        var linear = new byte[destWBytes * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
            linear[y * destWBytes + x] = indices[y * w + x];

        var gs = new GsMemory();
        gs.WriteTexPSMT8(tp.Dbp, destWBytes / 0x40, 0, 0, destWBytes, h, linear);
        imgBytes = gs.ReadTexPSMCT32(tp.Dbp, tp.Dbw, tp.Sx, tp.Sy, tp.Rrw, tp.Rrh);
    }

    // ── 16-colour (PSMT4) ──────────────────────────────────────────────────────

    private static void EncodePsmt4(TemplateInfo tp, byte[] bgra,
        out byte[] palBytes, out byte[] imgBytes)
    {
        var w = tp.FinalW;
        var h = tp.FinalH;

        Quantize(bgra, w, h, 16, out var palette, out var indices);

        // 16-entry palettes are stored LINEARLY (UnswizzlePalette is a no-op
        // for non-256 palettes), so no palette swizzle here.
        palBytes = new byte[16 * 4];
        for (var i = 0; i < 16; i++)
        {
            var (r, g, b, a) = palette[i];
            palBytes[i * 4 + 0] = r;
            palBytes[i * 4 + 1] = g;
            palBytes[i * 4 + 2] = b;
            palBytes[i * 4 + 3] = ToPs2Alpha(a);
        }

        // PSMT4 alignment: width to 0x3f, height to 0x0f. One index per element.
        var destWBytes = (w + 0x3f) & ~0x3f;
        var destHBytes = (h + 0x0f) & ~0x0f;
        var expanded = new byte[destWBytes * destHBytes];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
            expanded[y * destWBytes + x] = indices[y * w + x];

        var gs = new GsMemory();
        // PSMT4 read region starts at (Sx,Sy) in the decoder, so write there too.
        gs.WriteTexPSMT4(tp.Dbp, destWBytes / 0x40, tp.Sx, tp.Sy, destWBytes, destHBytes, expanded);
        imgBytes = gs.ReadTexPSMCT32(tp.Dbp, tp.Dbw, tp.Sx, tp.Sy, tp.Rrw, tp.Rrh);
    }

    // ── Template parsing ─────────────────────────────────────────────────────

    private readonly struct TemplateInfo
    {
        public int FinalW { get; init; }
        public int FinalH { get; init; }
        public int PaletteData { get; init; }
        public int PaletteLen { get; init; }
        public int ImageData { get; init; }
        public int ImageLen { get; init; }
        public int Dbp { get; init; }
        public int Dbw { get; init; }
        public int Rrw { get; init; }
        public int Rrh { get; init; }
        public int Sx { get; init; }
        public int Sy { get; init; }
    }

    private static TemplateInfo ParseTemplate(ReadOnlySpan<byte> t)
    {
        int finalW = DataUtil.GetLeShort(t, 0);
        int finalH = DataUtil.GetLeShort(t, 2);
        var length = DataUtil.GetLeShort(t, 6) * 16;
        var off = DataUtil.GetLeInt(t, 16);
        var end = off + length;

        GIFTag g = new();
        g.Parse(t[off..]);                       // palette setup (nloop == 4)
        off += g.Length;

        GIFTag g2 = new();
        g2.Parse(t[off..]);                      // palette IMAGE
        var palData = off + GIFTag.Size;
        var palLen = g2.nloop * 16;
        off += g2.Length;

        int dbp = 0, dbw = 0, rrw = 0, rrh = 0, sx = 0, sy = 0;
        var cur = off;
        while (cur < end - GIFTag.Size)
        {
            GIFTag g3 = new();
            g3.Parse(t[cur..]);
            if (g3.IsImage)
            {
                return new TemplateInfo
                {
                    FinalW = finalW, FinalH = finalH,
                    PaletteData = palData, PaletteLen = palLen,
                    ImageData = cur + GIFTag.Size, ImageLen = g3.nloop * 16,
                    Dbp = dbp, Dbw = dbw, Rrw = rrw, Rrh = rrh, Sx = sx, Sy = sy,
                };
            }

            for (var i = 0; i < g3.nloop; i++)
            {
                var basePos = cur + 0x10 + (i * 0x10);
                var reg = DataUtil.GetLeInt(t, basePos + 8);
                switch (reg)
                {
                    case BITBLTBUF:
                        dbp = DataUtil.GetLeShort(t, basePos + 4) & 0x3FFF;
                        dbw = t[basePos + 6] & 0x3F;
                        break;
                    case TRXREG:
                        rrw = DataUtil.GetLeShort(t, basePos) & 0xFFF;
                        rrh = DataUtil.GetLeShort(t, basePos + 4) & 0xFFF;
                        break;
                    case TRXPOS:
                        sx = DataUtil.GetLeShort(t, basePos + 4) & 0x7FF;
                        sy = DataUtil.GetLeShort(t, basePos + 6) & 0x7FF;
                        break;
                }
            }

            cur += g3.Length;
        }

        throw new InvalidOperationException("No image GIF tag found — not a palettised TEX.");
    }

    // ── Pixel helpers ────────────────────────────────────────────────────────

    private static byte[] ToBgra32(BitmapSource image)
    {
        var src = image.Format == PixelFormats.Bgra32
            ? image
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var stride = src.PixelWidth * 4;
        var px = new byte[stride * src.PixelHeight];
        src.CopyPixels(px, stride, 0);
        return px; // B,G,R,A per pixel
    }

    /// <summary>Straight 0..255 alpha → PS2 alpha byte (0x00 opaque, 0x80 transparent).</summary>
    private static byte ToPs2Alpha(byte a)
    {
        if (a >= 255) return 0x00;
        if (a == 0) return 0x80;
        var v = (int)Math.Round((255 - a) / 2.0);
        return (byte)Math.Clamp(v, 1, 127);
    }

    /// <summary>256-entry palette swizzle — an involution, identical to the
    /// decoder's UnswizzlePalette, so it is its own inverse.</summary>
    private static void SwizzlePalette256((byte R, byte G, byte B, byte A)[] pal)
    {
        var tmp = new (byte, byte, byte, byte)[256];
        Array.Copy(pal, tmp, 256);
        for (var i = 0; i < 256; i += 32)
        {
            for (var k = 0; k < 8; k++)
            {
                pal[i + k]      = tmp[i + k];
                pal[i + 16 + k] = tmp[i + 8 + k];
                pal[i + 8 + k]  = tmp[i + 16 + k];
                pal[i + 24 + k] = tmp[i + 24 + k];
            }
        }
    }

    // ── Quantisation (median cut) ──────────────────────────────────────────────

    /// <summary>
    /// Quantises BGRA pixels to ≤ <paramref name="maxColors"/> colours (256 or
    /// 16). Exact when the image already has ≤ maxColors distinct colours;
    /// otherwise a median-cut over RGB with per-bucket average alpha.
    /// </summary>
    private static void Quantize(byte[] bgra, int w, int h, int maxColors,
        out (byte R, byte G, byte B, byte A)[] palette, out byte[] indices)
    {
        var n = w * h;
        indices = new byte[n];

        var distinctMap = new Dictionary<uint, int>();
        var pixelKeys = new uint[n];
        for (var i = 0; i < n; i++)
        {
            var b = bgra[i * 4 + 0];
            var g = bgra[i * 4 + 1];
            var r = bgra[i * 4 + 2];
            var a = bgra[i * 4 + 3];
            var key = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
            pixelKeys[i] = key;
            if (!distinctMap.ContainsKey(key))
                distinctMap[key] = distinctMap.Count;
        }

        palette = new (byte, byte, byte, byte)[maxColors];

        if (distinctMap.Count <= maxColors)
        {
            // Lossless: one palette slot per distinct colour.
            foreach (var (key, slot) in distinctMap)
                palette[slot] = ((byte)((key >> 16) & 0xFF), (byte)((key >> 8) & 0xFF),
                                 (byte)(key & 0xFF), (byte)((key >> 24) & 0xFF));
            for (var i = 0; i < n; i++)
                indices[i] = (byte)distinctMap[pixelKeys[i]];
            return;
        }

        // Median-cut on RGB; alpha averaged per bucket.
        var pixels = new (byte R, byte G, byte B, byte A)[n];
        for (var i = 0; i < n; i++)
            pixels[i] = (bgra[i * 4 + 2], bgra[i * 4 + 1], bgra[i * 4 + 0], bgra[i * 4 + 3]);

        var boxes = new List<List<int>> { Enumerable.Range(0, n).ToList() };
        while (boxes.Count < maxColors)
        {
            var bestBox = -1;
            var bestRange = -1;
            var bestChannel = 0;
            for (var bi = 0; bi < boxes.Count; bi++)
            {
                if (boxes[bi].Count < 2) continue;
                RangeOf(pixels, boxes[bi], out var widest, out var ch);
                if (widest > bestRange) { bestRange = widest; bestBox = bi; bestChannel = ch; }
            }
            if (bestBox < 0) break;

            var box = boxes[bestBox];
            box.Sort((p, q) => Channel(pixels[p], bestChannel).CompareTo(Channel(pixels[q], bestChannel)));
            var mid = box.Count / 2;
            var lo = box.GetRange(0, mid);
            var hi = box.GetRange(mid, box.Count - mid);
            boxes[bestBox] = lo;
            boxes.Add(hi);
        }

        for (var bi = 0; bi < boxes.Count; bi++)
        {
            long sr = 0, sg = 0, sb = 0, sa = 0;
            foreach (var pi in boxes[bi])
            {
                sr += pixels[pi].R; sg += pixels[pi].G; sb += pixels[pi].B; sa += pixels[pi].A;
            }
            var c = Math.Max(1, boxes[bi].Count);
            palette[bi] = ((byte)(sr / c), (byte)(sg / c), (byte)(sb / c), (byte)(sa / c));
            foreach (var pi in boxes[bi])
                indices[pi] = (byte)bi;
        }
    }

    private static int Channel((byte R, byte G, byte B, byte A) p, int ch)
        => ch == 0 ? p.R : ch == 1 ? p.G : p.B;

    private static void RangeOf((byte R, byte G, byte B, byte A)[] px, List<int> idx,
        out int widestRange, out int widestChannel)
    {
        int rMin = 255, rMax = 0, gMin = 255, gMax = 0, bMin = 255, bMax = 0;
        foreach (var i in idx)
        {
            var p = px[i];
            if (p.R < rMin) rMin = p.R; if (p.R > rMax) rMax = p.R;
            if (p.G < gMin) gMin = p.G; if (p.G > gMax) gMax = p.G;
            if (p.B < bMin) bMin = p.B; if (p.B > bMax) bMax = p.B;
        }
        var rRange = rMax - rMin;
        var gRange = gMax - gMin;
        var bRange = bMax - bMin;
        widestChannel = rRange >= gRange && rRange >= bRange ? 0 : gRange >= bRange ? 1 : 2;
        widestRange = Math.Max(rRange, Math.Max(gRange, bRange));
    }
}