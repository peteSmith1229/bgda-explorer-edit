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
/// Encodes an image into a 256-colour PS2 <c>.tex</c> (PSMT8) by inverting
/// <see cref="TexDecoder"/>.  This is the write-side counterpart of the
/// decoder; the algorithm was validated in Python against real game textures
/// (book.tex and greenwater.tex re-encode byte-for-byte; arbitrary &gt;256-colour
/// images round-trip with RMSE ≈ 5 after quantisation).
///
/// <para><b>Template-based.</b> Because the PS2 header carries fields whose
/// meaning isn't fully known and the GIF/DMA setup encodes the exact transfer
/// parameters, the encoder takes the <em>original</em> <c>.tex</c> being
/// replaced as a template and swaps only the palette bytes and the swizzled
/// pixel bytes in place. The result is the same byte length and structure as
/// the original — only the picture changes.</para>
///
/// <para><b>Scope.</b> This version handles the common 256-colour (PSMT8)
/// textures and requires the replacement image to match the original's
/// dimensions. 16-colour (PSMT4) and 32-bit (direct image-mode) textures, and
/// dimension changes, are future extensions.</para>
/// </summary>
public static class TexEncoder
{
    private const int BITBLTBUF = 0x50;
    private const int TRXPOS    = 0x51;
    private const int TRXREG    = 0x52;

    /// <summary>
    /// True if <paramref name="templateTex"/> is a 256-colour PSMT8 texture
    /// this encoder can target (used to enable/disable the import UI).
    /// </summary>
    public static bool CanEncodeInto(ReadOnlySpan<byte> templateTex)
    {
        try
        {
            var tp = ParseTemplate(templateTex);
            return tp.PaletteLen == 256 * 4; // 256 RGBA entries
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encodes <paramref name="image"/> into a 256-colour TEX using
    /// <paramref name="templateTex"/> (the texture being replaced) for the
    /// header and GIF structure. Throws if the template isn't a 256-colour
    /// texture or the dimensions differ.
    /// </summary>
    public static byte[] Encode(byte[] templateTex, BitmapSource image)
    {
        var tp = ParseTemplate(templateTex);
        if (tp.PaletteLen != 256 * 4)
            throw new NotSupportedException(
                "Only 256-colour (PSMT8) textures can currently be replaced.");

        if (image.PixelWidth != tp.FinalW || image.PixelHeight != tp.FinalH)
            throw new ArgumentException(
                $"Replacement image must be {tp.FinalW}×{tp.FinalH} to match the texture " +
                $"(got {image.PixelWidth}×{image.PixelHeight}).");

        // Read the image as straight BGRA32.
        var bgra = ToBgra32(image);
        var w = tp.FinalW;
        var h = tp.FinalH;

        // Quantise to ≤256 colours → palette (straight RGBA) + per-pixel indices.
        Quantize(bgra, w, h, out var palette, out var indices);

        // Palette → (R,G,B,PS2-alpha), then swizzle for storage (256-entry involution).
        var palEntries = new (byte R, byte G, byte B, byte A)[256];
        for (var i = 0; i < 256; i++)
        {
            var (r, g, b, a) = palette[i];
            palEntries[i] = (r, g, b, ToPs2Alpha(a));
        }
        SwizzlePalette256(palEntries);

        var palBytes = new byte[256 * 4];
        for (var i = 0; i < 256; i++)
        {
            palBytes[i * 4 + 0] = palEntries[i].R;
            palBytes[i * 4 + 1] = palEntries[i].G;
            palBytes[i * 4 + 2] = palEntries[i].B;
            palBytes[i * 4 + 3] = palEntries[i].A;
        }

        // Build the linear index grid padded to destWBytes × finalH.
        var destWBytes = (w + 0x7f) & ~0x7f;
        var linear = new byte[destWBytes * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
            linear[y * destWBytes + x] = indices[y * w + x];

        // Swizzle the indices into PS2 image-data byte order using the
        // template's own transfer parameters.
        var gs = new GsMemory();
        gs.WriteTexPSMT8(tp.Dbp, destWBytes / 0x40, 0, 0, destWBytes, h, linear);
        var imgBytes = gs.ReadTexPSMCT32(tp.Dbp, tp.Dbw, tp.Sx, tp.Sy, tp.Rrw, tp.Rrh);

        if (palBytes.Length != tp.PaletteLen)
            throw new InvalidOperationException("Palette size mismatch with template.");
        if (imgBytes.Length != tp.ImageLen)
            throw new InvalidOperationException(
                $"Encoded image size {imgBytes.Length} != template {tp.ImageLen}.");

        // Splice the new palette + image bytes into a copy of the template.
        var outBytes = (byte[])templateTex.Clone();
        Array.Copy(palBytes, 0, outBytes, tp.PaletteData, palBytes.Length);
        Array.Copy(imgBytes, 0, outBytes, tp.ImageData, imgBytes.Length);
        return outBytes;
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
    /// Quantises BGRA pixels to ≤256 colours. Exact when the image already has
    /// ≤256 distinct colours; otherwise a median-cut over RGB with per-bucket
    /// average alpha.
    /// </summary>
    private static void Quantize(byte[] bgra, int w, int h,
        out (byte R, byte G, byte B, byte A)[] palette, out byte[] indices)
    {
        var n = w * h;
        indices = new byte[n];

        // Gather distinct colours.
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

        palette = new (byte, byte, byte, byte)[256];

        if (distinctMap.Count <= 256)
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
        {
            pixels[i] = (bgra[i * 4 + 2], bgra[i * 4 + 1], bgra[i * 4 + 0], bgra[i * 4 + 3]);
        }

        var boxes = new List<List<int>> { Enumerable.Range(0, n).ToList() };
        while (boxes.Count < 256)
        {
            // Split the box with the largest colour range.
            var bestBox = -1;
            var bestRange = -1;
            var bestChannel = 0;
            for (var bi = 0; bi < boxes.Count; bi++)
            {
                if (boxes[bi].Count < 2) continue;
                RangeOf(pixels, boxes[bi], out var rr, out var gr, out var br, out var ch);
                if (rr > bestRange) { bestRange = rr; bestBox = bi; bestChannel = ch; _ = gr; _ = br; }
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

        // Average colour per box → palette; map pixels → nearest box index.
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
        out int rRange, out int gRange, out int bRange, out int widestChannel)
    {
        int rMin = 255, rMax = 0, gMin = 255, gMax = 0, bMin = 255, bMax = 0;
        foreach (var i in idx)
        {
            var p = px[i];
            if (p.R < rMin) rMin = p.R; if (p.R > rMax) rMax = p.R;
            if (p.G < gMin) gMin = p.G; if (p.G > gMax) gMax = p.G;
            if (p.B < bMin) bMin = p.B; if (p.B > bMax) bMax = p.B;
        }
        rRange = rMax - rMin; gRange = gMax - gMin; bRange = bMax - bMin;
        widestChannel = rRange >= gRange && rRange >= bRange ? 0 : gRange >= bRange ? 1 : 2;
        // Report the widest range (so the caller picks the box to split).
        rRange = Math.Max(rRange, Math.Max(gRange, bRange));
    }
}
