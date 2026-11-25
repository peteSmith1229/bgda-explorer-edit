// ═══════════════════════════════════════════════════════════════════════════════
// The texture of an ARMOR.YAK child: header, a 16x16 PSMCT32 CLUT transfer (256 colours,
// swizzled 8-15 <-> 16-23) and the PSMT8 pixels written as PSMCT32 rectangles.
//
// Header: +0 u16 width, +2 height, +4 u16 GS blocks (CLUT + pixels), +6 u16 quadwords of
// GIF data, +8 flags (0x14), +0x0C TEX0 bits 14 and up (TBW, PSM, TW, TH), +0x10 offset of
// the GIF data (relocated on load), +0x1C 0 on disc (the loader's "relocated" mark). The
// upload (FUN_00135388) patches the DBP of the first two BITBLTBUFs (GIF +0x14 and +0x474),
// so the packet layout is kept exactly: only palette and pixel bytes change here.
//
// 30 of the 236 shipped textures store only the rectangles their model samples. Pixels
// outside them read as colour 0 and can't be changed without new transfers.
//
// Alpha is 0..0x80. With the models' blend (ALPHA_1 = 1) 0x80 is invisible and small
// values are solid; most pixels are 1. As a picture (ToRgba / FromRgba) it becomes PNG
// alpha 255 (solid) .. 0 (see-through), which comes back exactly.
// ═══════════════════════════════════════════════════════════════════════════════
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Armour;

public sealed class ArmorTexture
{
    private sealed record Transfer(int Dbp, int Dbw, int Dpsm, int X, int Y, int Rrw, int Rrh,
        List<(int Offset, int Length)> Chunks);

    private readonly List<Transfer> _transfers;

    /// <summary>The texture's bytes, with any edits.</summary>
    public byte[] Data { get; }

    public int Width { get; }
    public int Height { get; }

    /// <param name="data">The texture (copied).</param>
    public ArmorTexture(byte[] data)
    {
        Data = (byte[])data.Clone();
        if (Data.Length < 0x80) throw new ArgumentException("The texture is too short.");
        Width = BitConverter.ToUInt16(Data, 0);
        Height = BitConverter.ToUInt16(Data, 2);
        _transfers = Parse(Data);
        if (_transfers.Count < 2) throw new ArgumentException("The texture has no pixel transfer.");
        var clut = _transfers[0];
        if (clut.Rrw != 16 || clut.Rrh != 16)
            throw new ArgumentException("The texture's first transfer is not a 16x16 colour table.");
        if (Width == 0 || Height == 0) throw new ArgumentException("The texture has no size.");
    }

    /// <summary>Why the game couldn't use <paramref name="data"/> as a texture, or null.</summary>
    public static string? Problem(byte[] data)
    {
        try
        {
            _ = new ArmorTexture(data);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or KeyNotFoundException)
        {
            return ex.Message;
        }

        if (BitConverter.ToUInt32(data, 0x1C) != 0) return "+0x1C must be 0 in the file.";
        var gif = BitConverter.ToInt32(data, 0x10);
        if (gif + BitConverter.ToUInt16(data, 6) * 16 != data.Length) return "The length doesn't match +0x06.";
        return null;
    }

    private static List<Transfer> Parse(byte[] d)
    {
        var gif = BitConverter.ToInt32(d, 0x10);
        var end = gif + BitConverter.ToUInt16(d, 6) * 16;
        if (gif < 0x20 || end > d.Length) throw new ArgumentException("The texture's GIF data is out of range.");
        var regs = new Dictionary<int, ulong>();
        var list = new List<Transfer>();
        Transfer? pending = null;
        var remaining = 0;
        var o = gif;
        while (o < end)
        {
            var lo = BitConverter.ToUInt64(d, o);
            var hi = BitConverter.ToUInt64(d, o + 8);
            var nloop = (int)(lo & 0x7FFF);
            var flg = (int)(lo >> 58) & 3;
            var nreg = (int)(lo >> 60) & 0xF;
            if (nreg == 0) nreg = 16;
            if (flg == 0)
            {
                if (hi != 0xE || nreg != 1) throw new ArgumentException($"Unexpected GIF tag at {o:X}.");
                for (var k = 0; k < nloop; k++)
                {
                    regs[d[o + 16 + 16 * k + 8]] = BitConverter.ToUInt64(d, o + 16 + 16 * k);
                }

                o += 16 + 16 * nloop;
            }
            else if (flg == 2)
            {
                if (pending == null || remaining <= 0)
                {
                    var b = regs[0x50];
                    var p = regs.TryGetValue(0x51, out var pos) ? pos : 0;
                    var r = regs[0x52];
                    pending = new Transfer((int)(b >> 32) & 0x3FFF, (int)(b >> 48) & 0x3F, (int)(b >> 56) & 0x3F,
                        (int)(p >> 32) & 0x7FF, (int)(p >> 48) & 0x7FF, (int)r & 0xFFF, (int)(r >> 32) & 0xFFF,
                        new List<(int, int)>());
                    if (pending.Dpsm != 0) throw new ArgumentException("Only PSMCT32 transfers are handled.");
                    remaining = pending.Rrw * pending.Rrh * 4;
                    list.Add(pending);
                }

                var n = nloop * 16;
                if (o + 16 + n > end) throw new ArgumentException("An image runs past the end of the texture.");
                pending.Chunks.Add((o + 16, n));
                remaining -= n;
                o += 16 + n;
            }
            else
            {
                throw new ArgumentException($"GIF mode {flg} at {o:X} isn't handled.");
            }
        }

        return list;
    }

    private byte[] Read(Transfer t)
    {
        var want = t.Rrw * t.Rrh * 4;
        var bytes = new byte[want];
        var pos = 0;
        foreach (var (offset, length) in t.Chunks)
        {
            var take = Math.Min(length, want - pos);
            if (take <= 0) break;
            Buffer.BlockCopy(Data, offset, bytes, pos, take);
            pos += take;
        }

        return bytes;
    }

    private void Write(Transfer t, byte[] payload)
    {
        var pos = 0;
        foreach (var (offset, length) in t.Chunks)
        {
            var take = Math.Min(length, payload.Length - pos);
            if (take <= 0) break;
            Buffer.BlockCopy(payload, pos, Data, offset, take);
            pos += take;
        }
    }

    // ───────────────────────────── palette ─────────────────────────────

    /// <summary>CLUT storage position → colour index (entries 8-15 and 16-23 of every 32 swap).</summary>
    private static int Unswizzle(int i) => (i & ~0x18) | ((i & 0x08) << 1) | ((i & 0x10) >> 1);

    /// <summary>256 × RGBA in colour-index order; alpha 0..0x80.</summary>
    public byte[] GetPalette()
    {
        var raw = Read(_transfers[0]);
        var pal = new byte[1024];
        for (var i = 0; i < 256; i++) Buffer.BlockCopy(raw, 4 * i, pal, 4 * Unswizzle(i), 4);
        return pal;
    }

    public void SetPalette(byte[] palette)
    {
        if (palette.Length != 1024) throw new ArgumentException("A palette is 256 × 4 bytes.");
        var raw = new byte[1024];
        for (var i = 0; i < 256; i++) Buffer.BlockCopy(palette, 4 * Unswizzle(i), raw, 4 * i, 4);
        Write(_transfers[0], raw);
    }

    // ───────────────────────────── pixels ─────────────────────────────

    private int Dbw8 => ((Width + 0x7F) & ~0x7F) / 0x40;

    private GsMemory LoadGs()
    {
        var gs = new GsMemory();
        foreach (var t in _transfers.Skip(1))
        {
            gs.WriteTexPSMCT32(t.Dbp, t.Dbw, t.X, t.Y, t.Rrw, t.Rrh, Read(t), 0);
        }

        return gs;
    }

    /// <summary>Width × Height colour indices, row by row.</summary>
    public byte[] GetIndices() => LoadGs().ReadTexPSMT8(_transfers[1].Dbp, Dbw8, 0, 0, Width, Height);

    /// <summary>True where the file stores the pixel (see the class notes).</summary>
    public bool[] Coverage()
    {
        var gs = new GsMemory();
        foreach (var t in _transfers.Skip(1))
        {
            var ones = new byte[t.Rrw * t.Rrh * 4];
            Array.Fill(ones, (byte)1);
            gs.WriteTexPSMCT32(t.Dbp, t.Dbw, t.X, t.Y, t.Rrw, t.Rrh, ones, 0);
        }

        return gs.ReadTexPSMT8(_transfers[1].Dbp, Dbw8, 0, 0, Width, Height).Select(b => b != 0).ToArray();
    }

    /// <summary>Sets every pixel's colour index. Returns how many changed pixels fall outside the stored rectangles.</summary>
    public int SetIndices(byte[] indices)
    {
        if (indices.Length != Width * Height) throw new ArgumentException($"A {Width}x{Height} texture needs {Width * Height} indices.");
        var old = GetIndices();
        var covered = Coverage();
        var lost = 0;
        for (var i = 0; i < indices.Length; i++)
        {
            if (indices[i] != old[i] && !covered[i]) lost++;
        }

        var gs = LoadGs();
        gs.WriteTexPSMT8(_transfers[1].Dbp, Dbw8, 0, 0, Width, Height, indices);
        foreach (var t in _transfers.Skip(1))
        {
            Write(t, gs.ReadTexPSMCT32(t.Dbp, t.Dbw, t.X, t.Y, t.Rrw, t.Rrh));
        }

        return lost;
    }

    // ───────────────────────────── as a picture ─────────────────────────────

    /// <summary>PS2 alpha (0 solid .. 0x80 see-through) → PNG alpha (255 .. 0).</summary>
    public static byte PngAlpha(int ps2) => (byte)(255 - (int)Math.Round(Math.Min(ps2, 128) * 255 / 128.0));

    /// <summary>PNG alpha → PS2 alpha (exact inverse of <see cref="PngAlpha"/> on 0..0x80).</summary>
    public static byte Ps2Alpha(int png) => (byte)Math.Round((255 - (double)png) * 128 / 255);

    /// <summary>Width × Height × RGBA for editing (alpha as in <see cref="PngAlpha"/>).</summary>
    public byte[] ToRgba()
    {
        var pal = GetPalette();
        var idx = GetIndices();
        var rgba = new byte[idx.Length * 4];
        for (var i = 0; i < idx.Length; i++)
        {
            var p = 4 * idx[i];
            rgba[4 * i] = pal[p];
            rgba[4 * i + 1] = pal[p + 1];
            rgba[4 * i + 2] = pal[p + 2];
            rgba[4 * i + 3] = PngAlpha(pal[p + 3]);
        }

        return rgba;
    }

    /// <summary>
    /// Puts a Width × Height × RGBA picture into the texture. A picture that only uses colours of the
    /// current palette is mapped exactly and the palette stays; otherwise a new 256-colour palette is made
    /// (or, with <paramref name="keepPalette"/>, the nearest current colours are used). Pixels are grouped
    /// by alpha so cut-outs and the metal sets' translucency stay exact.
    /// Returns the changed pixels and how many of them fall outside the stored rectangles.
    /// </summary>
    public (int Changed, int Lost) FromRgba(byte[] rgba, bool keepPalette = false)
    {
        if (rgba.Length != Width * Height * 4)
            throw new ArgumentException($"The picture must be {Width}x{Height}, the size of the texture: " +
                                        "the game's layout and the model's UVs depend on it.");
        var count = Width * Height;
        var want = new int[count * 4];
        for (var i = 0; i < count; i++)
        {
            want[4 * i] = rgba[4 * i];
            want[4 * i + 1] = rgba[4 * i + 1];
            want[4 * i + 2] = rgba[4 * i + 2];
            want[4 * i + 3] = Ps2Alpha(rgba[4 * i + 3]);
        }

        var pal = GetPalette();
        var oldIdx = GetIndices();

        // exact: every wanted colour is in the palette (unchanged pixels keep their own index)
        var lookup = new Dictionary<int, int>();
        for (var i = 255; i >= 0; i--) lookup[Key(pal[4 * i], pal[4 * i + 1], pal[4 * i + 2], pal[4 * i + 3])] = i;
        var idx = (byte[])oldIdx.Clone();
        var exact = true;
        for (var j = 0; j < count && exact; j++)
        {
            var p = 4 * idx[j];
            if (pal[p] == want[4 * j] && pal[p + 1] == want[4 * j + 1] && pal[p + 2] == want[4 * j + 2] &&
                pal[p + 3] == want[4 * j + 3]) continue;
            if (lookup.TryGetValue(Key(want[4 * j], want[4 * j + 1], want[4 * j + 2], want[4 * j + 3]), out var k))
                idx[j] = (byte)k;
            else
                exact = false;
        }

        if (exact)
        {
            var lostExact = SetIndices(idx);
            return (CountDifferent(idx, oldIdx), lostExact);
        }

        var (newPal, newIdx) = Quantize(want, keepPalette ? pal : null);
        if (!keepPalette)
        {
            for (var i = 0; i < 256; i++) newPal[4 * i + 3] = Math.Min(newPal[4 * i + 3], (byte)0x80);
        }

        SetPalette(newPal);
        var lost = SetIndices(newIdx);
        return (CountDifferent(newIdx, oldIdx), lost);
    }

    private static int Key(int r, int g, int b, int a) => r | g << 8 | b << 16 | a << 24;

    private static int CountDifferent(byte[] a, byte[] b)
    {
        var n = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) n++;
        }

        return n;
    }

    /// <summary>
    /// RGB + PS2 alpha → 256-colour palette and indices. With a palette to keep: nearest colour. Otherwise the
    /// pixels are grouped by alpha (ascending), each group gets slots in proportion to its size (at least one),
    /// and its colours are its own when they fit, else a median cut.
    /// </summary>
    private static (byte[] Palette, byte[] Indices) Quantize(int[] want, byte[]? keep)
    {
        var count = want.Length / 4;
        var idx = new byte[count];
        if (keep != null)
        {
            for (var j = 0; j < count; j++)
            {
                var best = 0;
                var bestD = long.MaxValue;
                for (var i = 0; i < 256; i++)
                {
                    long dr = want[4 * j] - keep[4 * i], dg = want[4 * j + 1] - keep[4 * i + 1],
                        db = want[4 * j + 2] - keep[4 * i + 2], da = want[4 * j + 3] - keep[4 * i + 3];
                    var d = dr * dr + dg * dg + db * db + 64 * da * da;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }

                idx[j] = (byte)best;
            }

            return ((byte[])keep.Clone(), idx);
        }

        var groups = Enumerable.Range(0, count).GroupBy(j => want[4 * j + 3]).OrderBy(g => g.Key)
            .Select(g => (Alpha: g.Key, Pixels: g.ToArray())).ToList();
        if (groups.Count > 256) throw new ArgumentException("The picture has more than 256 levels of transparency.");

        // slots: one each, the rest by share, the remainder to the largest fractions (ties: lower alpha first)
        var slots = groups.Select(_ => 1).ToArray();
        var spare = 256 - groups.Count;
        var share = groups.Select(g => (double)g.Pixels.Length / count * spare).ToArray();
        for (var k = 0; k < slots.Length; k++) slots[k] += (int)Math.Floor(share[k]);
        var left = 256 - slots.Sum();
        foreach (var k in Enumerable.Range(0, slots.Length).OrderByDescending(k => share[k] - Math.Floor(share[k]))
                     .ThenBy(k => k).Take(left))
        {
            slots[k]++;
        }

        var palette = new byte[1024];
        var start = 0;
        for (var g = 0; g < groups.Count; g++)
        {
            var (alpha, pixels) = groups[g];
            var n = slots[g];
            var colours = pixels.Select(j => (want[4 * j] << 16) | (want[4 * j + 1] << 8) | want[4 * j + 2]).ToArray();
            var unique = colours.Distinct().OrderBy(c => c).ToArray(); // R, then G, then B
            int[] chosen;
            if (unique.Length <= n)
            {
                chosen = unique;
            }
            else
            {
                chosen = MedianCut(colours, n);
            }

            var position = new Dictionary<int, int>();
            for (var k = 0; k < chosen.Length; k++)
            {
                palette[4 * (start + k)] = (byte)(chosen[k] >> 16);
                palette[4 * (start + k) + 1] = (byte)(chosen[k] >> 8);
                palette[4 * (start + k) + 2] = (byte)chosen[k];
                palette[4 * (start + k) + 3] = (byte)alpha;
                position.TryAdd(chosen[k], k);
            }

            var nearest = new Dictionary<int, int>();
            for (var q = 0; q < pixels.Length; q++)
            {
                var c = colours[q];
                if (!position.TryGetValue(c, out var k) && !nearest.TryGetValue(c, out k))
                {
                    k = Nearest(c, chosen);
                    nearest[c] = k;
                }

                idx[pixels[q]] = (byte)(start + k);
            }

            start += n;
        }

        return (palette, idx);
    }

    private static int Nearest(int c, int[] chosen)
    {
        int r = c >> 16, g = (c >> 8) & 0xFF, b = c & 0xFF;
        var best = 0;
        var bestD = int.MaxValue;
        for (var k = 0; k < chosen.Length; k++)
        {
            int dr = r - (chosen[k] >> 16), dg = g - ((chosen[k] >> 8) & 0xFF), db = b - (chosen[k] & 0xFF);
            var d = dr * dr + dg * dg + db * db;
            if (d < bestD)
            {
                bestD = d;
                best = k;
            }
        }

        return best;
    }

    /// <summary>Median cut of 0xRRGGBB colours (with repeats) into <paramref name="n"/> averaged colours.</summary>
    private static int[] MedianCut(int[] colours, int n)
    {
        var counts = colours.GroupBy(c => c).Select(g => (Colour: g.Key, Count: g.Count())).ToList();
        var boxes = new List<List<(int Colour, int Count)>> { counts };
        while (boxes.Count < n)
        {
            // split the box with the widest channel range (that has more than one colour)
            var bestBox = -1;
            var bestRange = -1;
            var bestChannel = 0;
            for (var i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Count < 2) continue;
                for (var ch = 0; ch < 3; ch++)
                {
                    var shift = 16 - 8 * ch;
                    var min = 255;
                    var max = 0;
                    foreach (var (colour, _) in boxes[i])
                    {
                        var v = (colour >> shift) & 0xFF;
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }

                    if (max - min > bestRange)
                    {
                        bestRange = max - min;
                        bestBox = i;
                        bestChannel = ch;
                    }
                }
            }

            if (bestBox < 0) break;
            var s = 16 - 8 * bestChannel;
            var sorted = boxes[bestBox].OrderBy(e => (e.Colour >> s) & 0xFF).ThenBy(e => e.Colour).ToList();
            var total = sorted.Sum(e => e.Count);
            var acc = 0;
            var cut = 1;
            for (var i = 0; i < sorted.Count - 1; i++)
            {
                acc += sorted[i].Count;
                cut = i + 1;
                if (acc * 2 >= total) break;
            }

            boxes[bestBox] = sorted.Take(cut).ToList();
            boxes.Add(sorted.Skip(cut).ToList());
        }

        return boxes.Select(box =>
        {
            double r = 0, g = 0, b = 0, w = 0;
            foreach (var (colour, c) in box)
            {
                r += (colour >> 16) * c;
                g += ((colour >> 8) & 0xFF) * c;
                b += (colour & 0xFF) * c;
                w += c;
            }

            return ((int)Math.Round(r / w) << 16) | ((int)Math.Round(g / w) << 8) | (int)Math.Round(b / w);
        }).ToArray();
    }

    // ───────────────────────────── colours ─────────────────────────────

    /// <summary>
    /// Rotates the hue of the palette colours whose hue is in [<paramref name="hueFrom"/>, <paramref name="hueTo"/>)
    /// (degrees; the range may wrap, e.g. 330..30) and whose saturation is at least <paramref name="minSaturation"/>,
    /// and scales their saturation and value. Alpha is untouched. Same results as Python's colorsys.
    /// </summary>
    public static byte[] HueShift(byte[] palette, double shiftDegrees, double hueFrom = 0, double hueTo = 360,
        double minSaturation = 0.15, double saturationScale = 1, double valueScale = 1)
    {
        var output = (byte[])palette.Clone();
        for (var i = 0; i < 256; i++)
        {
            var (h, s, v) = RgbToHsv(output[4 * i] / 255.0, output[4 * i + 1] / 255.0, output[4 * i + 2] / 255.0);
            var hd = h * 360;
            var inside = hueFrom <= hueTo ? hd >= hueFrom && hd < hueTo : hd >= hueFrom || hd < hueTo;
            if (!inside || s < minSaturation) continue;
            h = PyMod(hd + shiftDegrees, 360) / 360;
            s = Math.Min(1.0, s * saturationScale);
            v = Math.Min(1.0, v * valueScale);
            var (r, g, b) = HsvToRgb(h, s, v);
            output[4 * i] = (byte)Math.Round(r * 255);
            output[4 * i + 1] = (byte)Math.Round(g * 255);
            output[4 * i + 2] = (byte)Math.Round(b * 255);
        }

        return output;
    }

    private static double PyMod(double x, double m)
    {
        var r = x % m;
        if (r != 0 && r < 0 != m < 0) r += m;
        return r;
    }

    private static (double H, double S, double V) RgbToHsv(double r, double g, double b)
    {
        var maxc = Math.Max(r, Math.Max(g, b));
        var minc = Math.Min(r, Math.Min(g, b));
        var rangec = maxc - minc;
        if (minc == maxc) return (0.0, 0.0, maxc);
        var s = rangec / maxc;
        var rc = (maxc - r) / rangec;
        var gc = (maxc - g) / rangec;
        var bc = (maxc - b) / rangec;
        double h;
        if (r == maxc) h = bc - gc;
        else if (g == maxc) h = 2.0 + rc - bc;
        else h = 4.0 + gc - rc;
        h = PyMod(h / 6.0, 1.0);
        return (h, s, maxc);
    }

    private static (double R, double G, double B) HsvToRgb(double h, double s, double v)
    {
        if (s == 0.0) return (v, v, v);
        var i = (int)(h * 6.0);
        var f = h * 6.0 - i;
        var p = v * (1.0 - s);
        var q = v * (1.0 - s * f);
        var t = v * (1.0 - s * (1.0 - f));
        return (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
    }
}
