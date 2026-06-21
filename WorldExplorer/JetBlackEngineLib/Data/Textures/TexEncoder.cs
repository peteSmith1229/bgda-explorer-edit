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
/// Encodes an image into a PS2 <c>.tex</c>/<c>.etex</c> by inverting
/// <see cref="TexDecoder"/>. Supports 256-colour (PSMT8), 16-colour (PSMT4) and
/// 32-bit direct (image-mode) textures.
///
/// <para><b>Same dimensions</b> → the original texture is used as a template and
/// only the palette/pixel bytes are swapped in place (byte-faithful).</para>
///
/// <para><b>Different dimensions</b> → the file is <i>synthesised from scratch</i>
/// for 256-colour and 32-bit textures: header, GIF/DMA tags and all
/// size-derived transfer parameters are rebuilt. This synthesis is validated to
/// reproduce the real game textures byte-for-byte <b>except</b> three header
/// fields at +0x04/+0x08/+0x0C whose meaning is unknown (the decoder ignores
/// them; +0x0C looks like a hash). Those three fields are copied from the
/// source texture. Whether the game accepts a resized texture with copied
/// values is unverified and must be confirmed in-game. 16-colour resizing is
/// not offered (no sample was available to validate its synthesis).</para>
/// </summary>
public static class TexEncoder
{
    private const int BITBLTBUF = 0x50;
    private const int TRXPOS    = 0x51;
    private const int TRXREG    = 0x52;
    private const int Image32PixelOffset = 0xD0;
    private const int PalSetupStart = 0x80; // constant palette-setup block for 256-colour
    private const int PalSetupEnd   = 0xE0; // (0x50 setup GIF + 0x10 palette IMAGE tag)

    /// <summary>True if this texture is a format the encoder can target.</summary>
    public static bool CanEncodeInto(ReadOnlySpan<byte> templateTex)
    {
        try
        {
            if (IsImage32(templateTex)) return true;
            var tp = ParseTemplate(templateTex);
            return tp.PaletteLen == 256 * 4 || tp.PaletteLen == 16 * 4;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encodes <paramref name="image"/> using <paramref name="templateTex"/>.
    /// Same dimensions → in-place swap; different dimensions → synthesised
    /// (256-colour / 32-bit only).
    /// </summary>
    public static byte[] Encode(byte[] templateTex, BitmapSource image)
    {
        if (IsImage32(templateTex))
        {
            int w0 = DataUtil.GetLeShort(templateTex, 0);
            int h0 = DataUtil.GetLeShort(templateTex, 2);
            return image.PixelWidth == w0 && image.PixelHeight == h0
                ? EncodeImage32(templateTex, image)
                : EncodeImage32Synth(templateTex, image);
        }

        var tp = ParseTemplate(templateTex);
        var sameDims = image.PixelWidth == tp.FinalW && image.PixelHeight == tp.FinalH;
        var bgra = ToBgra32(image);

        if (tp.PaletteLen == 256 * 4)
        {
            if (!sameDims) return EncodePsmt8Synth(templateTex, image);
            EncodePsmt8(tp.FinalW, tp.FinalH, tp.Dbp, tp.Dbw, tp.Sx, tp.Sy, tp.Rrw, tp.Rrh,
                bgra, out var pal, out var img);
            return Splice(templateTex, tp, pal, img);
        }

        if (tp.PaletteLen == 16 * 4)
        {
            if (!sameDims)
                throw new NotSupportedException(
                    "Dimension changes for 16-colour (PSMT4) textures aren't supported — no " +
                    "16-colour sample was available to validate the synthesis. Keep the same " +
                    "dimensions for this texture.");
            EncodePsmt4(tp.FinalW, tp.FinalH, tp.Dbp, tp.Dbw, tp.Sx, tp.Sy, tp.Rrw, tp.Rrh,
                bgra, out var pal, out var img);
            return Splice(templateTex, tp, pal, img);
        }

        throw new NotSupportedException(
            "Only 256-colour (PSMT8), 16-colour (PSMT4) and 32-bit textures can be replaced.");
    }

    // ── Splice (same-dimensions in-place swap) ─────────────────────────────────

    private static byte[] Splice(byte[] templateTex, TemplateInfo tp, byte[] palBytes, byte[] imgBytes)
    {
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

    // ── 256-colour (PSMT8) palette + pixel encoding ────────────────────────────

    private static void EncodePsmt8(int w, int h, int dbp, int dbw, int sx, int sy, int rrw, int rrh,
        byte[] bgra, out byte[] palBytes, out byte[] imgBytes)
    {
        Quantize(bgra, w, h, 256, out var palette, out var indices);

        var palEntries = new (byte R, byte G, byte B, byte A)[256];
        for (var i = 0; i < 256; i++)
        {
            var (r, g, b, a) = palette[i];
            palEntries[i] = (r, g, b, ToPs2Alpha(a));
        }
        SwizzlePalette256(palEntries);
        palBytes = PaletteToBytes(palEntries, 256);

        var destWBytes = (w + 0x7f) & ~0x7f;
        var linear = new byte[destWBytes * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
            linear[y * destWBytes + x] = indices[y * w + x];

        var gs = new GsMemory();
        gs.WriteTexPSMT8(dbp, destWBytes / 0x40, 0, 0, destWBytes, h, linear);
        imgBytes = gs.ReadTexPSMCT32(dbp, dbw, sx, sy, rrw, rrh);
    }

    // ── 16-colour (PSMT4) palette + pixel encoding ─────────────────────────────

    private static void EncodePsmt4(int w, int h, int dbp, int dbw, int sx, int sy, int rrw, int rrh,
        byte[] bgra, out byte[] palBytes, out byte[] imgBytes)
    {
        Quantize(bgra, w, h, 16, out var palette, out var indices);

        // 16-entry palettes are stored LINEARLY (no swizzle).
        var palEntries = new (byte R, byte G, byte B, byte A)[16];
        for (var i = 0; i < 16; i++)
        {
            var (r, g, b, a) = palette[i];
            palEntries[i] = (r, g, b, ToPs2Alpha(a));
        }
        palBytes = PaletteToBytes(palEntries, 16);

        var destWBytes = (w + 0x3f) & ~0x3f;
        var destHBytes = (h + 0x0f) & ~0x0f;
        var expanded = new byte[destWBytes * destHBytes];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
            expanded[y * destWBytes + x] = indices[y * w + x];

        var gs = new GsMemory();
        gs.WriteTexPSMT4(dbp, destWBytes / 0x40, sx, sy, destWBytes, destHBytes, expanded);
        imgBytes = gs.ReadTexPSMCT32(dbp, dbw, sx, sy, rrw, rrh);
    }

    // ── 256-colour SYNTHESIS (new dimensions) ──────────────────────────────────

    private static byte[] EncodePsmt8Synth(byte[] template, BitmapSource image)
    {
        var w = image.PixelWidth;
        var h = image.PixelHeight;

        // Size-derived transfer params (validated byte-exact against book/greenwater).
        var destW16 = (w + 0x0f) & ~0x0f;
        var destH16 = (h + 0x0f) & ~0x0f;
        var rrw = destW16 / 2;
        var rrh = destH16 / 2;
        var destWBytes = (w + 0x7f) & ~0x7f;
        var dbwBitblt = destWBytes / 0x80;

        EncodePsmt8(w, h, 0, dbwBitblt, 0, 0, rrw, rrh, ToBgra32(image),
            out var palBytes, out var imgBytes);
        var imgNloop = (rrw * rrh * 4) / 16;

        var palSetup = template[PalSetupStart..PalSetupEnd];   // constant 0x60-byte block
        var dest = BuildAdTag(nloop: 1, regs: new[] { BitbltbufEntry(0, dbwBitblt, 0) });
        var xfer = BuildAdTag(nloop: 3, regs: new[] { TrxregEntry(rrw, rrh), AdEntry(TRXPOS), AdEntry(0x53) });
        var imgTag = BuildImageTag(imgNloop);

        var body = Concat(palSetup, palBytes, dest, xfer, imgTag, imgBytes);
        return WithHeader(template, w, h, body);
    }

    // ── 32-bit SYNTHESIS (new dimensions) ──────────────────────────────────────

    private static byte[] EncodeImage32Synth(byte[] template, BitmapSource image)
    {
        var w = image.PixelWidth;
        var h = image.PixelHeight;
        var bgra = ToBgra32(image);

        // Reuse the template's transfer GIF tag (nloop==3 @0x80) and patch the
        // TRXREG (w,h) and BITBLTBUF dbw for the new size.
        var setup = template[0x80..0xC0];
        var dbw = Math.Max(1, (w + 0x3f) / 0x40);
        for (var i = 0; i < 3; i++)
        {
            var b = 0x10 + (i * 0x10);
            var reg = DataUtil.GetLeInt(setup, b + 8);
            if (reg == BITBLTBUF) setup[b + 6] = (byte)dbw;
            else if (reg == TRXREG) { PutU16(setup, b + 0, w); PutU16(setup, b + 4, h); }
        }

        var imgNloop = (w * h * 4) / 16;
        var imgTag = BuildImageTag(imgNloop);

        var px = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            px[i * 4 + 0] = bgra[i * 4 + 2]; // R
            px[i * 4 + 1] = bgra[i * 4 + 1]; // G
            px[i * 4 + 2] = bgra[i * 4 + 0]; // B
            px[i * 4 + 3] = ToPs2Alpha32(bgra[i * 4 + 3]);
        }

        var body = Concat(setup, imgTag, px);
        return WithHeader(template, w, h, body);
    }

    /// <summary>
    /// Builds the 0x80-byte header for a synthesised texture: dimensions and
    /// length are computed; the three unknown fields at +0x04/+0x08/+0x0C are
    /// COPIED from the source template (see class remarks). offsetToGIF = 0x80.
    /// </summary>
    private static byte[] WithHeader(byte[] template, int w, int h, byte[] body)
    {
        var hdr = new byte[0x80];
        PutU16(hdr, 0, w);
        PutU16(hdr, 2, h);
        // @4 (u16) and @8/@12 (two u32) — meaning unknown; copy from source.
        Array.Copy(template, 4, hdr, 4, 2);
        Array.Copy(template, 8, hdr, 8, 8);
        PutU32(hdr, 16, 0x80);
        PutU16(hdr, 6, body.Length / 16);
        return Concat(hdr, body);
    }

    // ── GIF tag builders ───────────────────────────────────────────────────────

    /// <summary>A 16-byte A+D register entry: 8 data bytes + register id @+8.</summary>
    private static byte[] AdEntry(int regId, long data = 0)
    {
        var e = new byte[0x10];
        PutU32(e, 0, (int)(data & 0xFFFFFFFF));
        PutU32(e, 4, (int)((data >> 32) & 0xFFFFFFFF));
        PutU32(e, 8, regId);
        return e;
    }

    private static byte[] BitbltbufEntry(int dbp, int dbw, int dpsm)
    {
        var e = AdEntry(BITBLTBUF);
        PutU16(e, 4, dbp & 0x3FFF);   // DBP (bits 32-45)
        e[6] = (byte)(dbw & 0x3F);    // DBW (bits 48-53)
        e[7] = (byte)(dpsm & 0x3F);   // DPSM (bits 56-61)
        return e;
    }

    private static byte[] TrxregEntry(int rrw, int rrh)
    {
        var e = AdEntry(TRXREG);
        PutU16(e, 0, rrw);
        PutU16(e, 4, rrh);
        return e;
    }

    /// <summary>PACKED A+D GIF tag (EOP set, nreg=1, REGS=A+D) followed by its entries.</summary>
    private static byte[] BuildAdTag(int nloop, byte[][] regs)
    {
        var tag = new byte[0x10];
        PutU16(tag, 0, (nloop & 0x7FFF) | 0x8000); // nloop + EOP
        tag[7] = 0x10;                              // nreg = 1, flg = 0 (PACKED)
        tag[8] = 0x0e;                              // REGS[0] = A+D
        return Concat(new[] { tag }.Concat(regs).ToArray());
    }

    /// <summary>IMAGE-mode GIF tag (EOP set, flg=2).</summary>
    private static byte[] BuildImageTag(int nloop)
    {
        var tag = new byte[0x10];
        PutU16(tag, 0, (nloop & 0x7FFF) | 0x8000); // nloop + EOP
        tag[7] = 0x08;                              // flg = 2 (IMAGE)
        return tag;
    }

    // ── Template parsing (palettised) ─────────────────────────────────────────

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
        g.Parse(t[off..]);
        off += g.Length;

        GIFTag g2 = new();
        g2.Parse(t[off..]);
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

    // ── 32-bit detection / encode ──────────────────────────────────────────────

    private static bool IsImage32(ReadOnlySpan<byte> t)
    {
        if (t.Length < Image32PixelOffset) return false;
        var off = DataUtil.GetLeInt(t, 16);
        if (off < 0 || off + GIFTag.Size > t.Length) return false;
        GIFTag g = new();
        g.Parse(t[off..]);
        if (g.nloop != 3) return false;
        GIFTag g2 = new();
        g2.Parse(t[0xC0..]);
        return g2.flg == 2;
    }

    private static byte[] EncodeImage32(byte[] templateTex, BitmapSource image)
    {
        int finalW = DataUtil.GetLeShort(templateTex, 0);
        int finalH = DataUtil.GetLeShort(templateTex, 2);

        var pixelCount = finalW * finalH;
        if (Image32PixelOffset + (pixelCount * 4) > templateTex.Length)
            throw new InvalidOperationException("Template too small for its declared dimensions.");

        var bgra = ToBgra32(image);
        var outBytes = (byte[])templateTex.Clone();
        for (var i = 0; i < pixelCount; i++)
        {
            var o = Image32PixelOffset + (i * 4);
            outBytes[o + 0] = bgra[i * 4 + 2];
            outBytes[o + 1] = bgra[i * 4 + 1];
            outBytes[o + 2] = bgra[i * 4 + 0];
            outBytes[o + 3] = ToPs2Alpha32(bgra[i * 4 + 3]);
        }
        return outBytes;
    }

    // ── Pixel / palette helpers ────────────────────────────────────────────────

    private static byte[] ToBgra32(BitmapSource image)
    {
        var src = image.Format == PixelFormats.Bgra32
            ? image
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var stride = src.PixelWidth * 4;
        var px = new byte[stride * src.PixelHeight];
        src.CopyPixels(px, stride, 0);
        return px;
    }

    private static byte[] PaletteToBytes((byte R, byte G, byte B, byte A)[] pal, int count)
    {
        var b = new byte[count * 4];
        for (var i = 0; i < count; i++)
        {
            b[i * 4 + 0] = pal[i].R;
            b[i * 4 + 1] = pal[i].G;
            b[i * 4 + 2] = pal[i].B;
            b[i * 4 + 3] = pal[i].A;
        }
        return b;
    }

    /// <summary>Straight alpha → PS2 alpha for PALETTISED textures (0x00 opaque, 0x80 transparent).</summary>
    private static byte ToPs2Alpha(byte a)
    {
        if (a >= 255) return 0x00;
        if (a == 0) return 0x80;
        return (byte)Math.Clamp((int)Math.Round((255 - a) / 2.0), 1, 127);
    }

    /// <summary>Straight alpha → PS2 alpha for 32-bit IMAGE-MODE textures (inverse of PalEntry.Argb).</summary>
    private static byte ToPs2Alpha32(byte a)
    {
        if (a >= 255) return 0xFF;
        if (a == 0) return 0x80;
        return (byte)Math.Clamp((int)Math.Round(a / 2.0), 1, 0x7F);
    }

    private static void SwizzlePalette256((byte R, byte G, byte B, byte A)[] pal)
    {
        var tmp = new (byte, byte, byte, byte)[256];
        Array.Copy(pal, tmp, 256);
        for (var i = 0; i < 256; i += 32)
        for (var k = 0; k < 8; k++)
        {
            pal[i + k]      = tmp[i + k];
            pal[i + 16 + k] = tmp[i + 8 + k];
            pal[i + 8 + k]  = tmp[i + 16 + k];
            pal[i + 24 + k] = tmp[i + 24 + k];
        }
    }

    // ── Little-endian writers / concat ─────────────────────────────────────────

    private static void PutU16(byte[] a, int o, int v)
    {
        a[o] = (byte)(v & 0xFF);
        a[o + 1] = (byte)((v >> 8) & 0xFF);
    }

    private static void PutU32(byte[] a, int o, int v)
    {
        a[o] = (byte)(v & 0xFF);
        a[o + 1] = (byte)((v >> 8) & 0xFF);
        a[o + 2] = (byte)((v >> 16) & 0xFF);
        a[o + 3] = (byte)((v >> 24) & 0xFF);
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var total = parts.Sum(p => p.Length);
        var outBytes = new byte[total];
        var pos = 0;
        foreach (var p in parts)
        {
            Array.Copy(p, 0, outBytes, pos, p.Length);
            pos += p.Length;
        }
        return outBytes;
    }

    // ── Quantisation (median cut) ──────────────────────────────────────────────

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
            foreach (var (key, slot) in distinctMap)
                palette[slot] = ((byte)((key >> 16) & 0xFF), (byte)((key >> 8) & 0xFF),
                                 (byte)(key & 0xFF), (byte)((key >> 24) & 0xFF));
            for (var i = 0; i < n; i++)
                indices[i] = (byte)distinctMap[pixelKeys[i]];
            return;
        }

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
