// ═══════════════════════════════════════════════════════════════════════════════
// The model of an ARMOR.YAK child: a skinned VIF model, sent to VIF1 as it is by DMA REF
// (FUN_0013F5F8 → FUN_0013C920) with the wearer's bone matrices.
//
// Header: +0x00 u64 hidden body parts (bit n hides mesh n of the wearer's model), +0x10
// u16 flags (0x01; 0x10 the metal sets; 0x20 would ask for the enchant glow), +0x12 mesh
// count, +0x13 attach bone (-1), +0x18 ALPHA_1, +0x20 ALPHA_2's FIX, +0x28 mesh offsets
// (relocated on load), +0x30 s8 bounding box (min x, y, z, max x, y, z in units), +0x40
// eight s8 sample points (purpose unknown, kept following their vertices).
//
// Each chunk: ITOP n; UNPACK v3-16 positions (s16, 1/16 unit, model space, bind pose, Z up);
// v3-8 normals; v3-16U strip slots (n + 2); v4-8U bone weights; v4-16U extra strip slots;
// v4-32U GIF tag; MSCAL 66 or 68; then v2-16 UVs (s16, 1/16 texel) per strip slot.
//
// Positions, normals, UVs and the mask can change in place; strips, weights and the VU
// layout stay. Vertices are in model space, so moved vertices follow the skinning.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Armour;

public sealed class ArmorModel
{
    /// <summary>One VIF chunk: where its arrays are in <see cref="Data"/>.</summary>
    public sealed class Chunk
    {
        public int VertexCount;
        public int Program;
        public int Positions = -1, PositionCount;
        public int Normals = -1, NormalCount;
        public int Slots = -1, SlotCount;
        public int Weights = -1, WeightCount;
        public int ExtraSlots = -1, ExtraSlotCount;
        public int GifTag = -1;
        public int Uvs = -1, UvCount;
    }

    public byte[] Data { get; }
    public IReadOnlyList<Chunk> Chunks { get; }

    /// <param name="data">The model (copied).</param>
    public ArmorModel(byte[] data)
    {
        Data = (byte[])data.Clone();
        Chunks = Parse(Data);
    }

    public ulong HiddenBodyParts
    {
        get => BitConverter.ToUInt64(Data, 0);
        set => BitConverter.TryWriteBytes(Data.AsSpan(0, 8), value);
    }

    public ushort Flags => BitConverter.ToUInt16(Data, 0x10);
    public int VertexCount => Chunks.Sum(c => c.VertexCount);

    /// <summary>The VU program of the first chunk: 66 (cloth, leather) or 68 (the metal sets).</summary>
    public int Program => Chunks.Count > 0 ? Chunks[0].Program : 0;

    private static List<Chunk> Parse(byte[] d)
    {
        int meshes = d[0x12];
        if (meshes < 1) throw new ArgumentException("The model has no mesh.");
        var start = BitConverter.ToInt32(d, 0x28);
        var end = BitConverter.ToInt32(d, 0x28 + 4 * meshes);
        if (start < 0x30 || end > d.Length || start > end) throw new ArgumentException("The model's mesh offsets are out of range.");
        var chunks = new List<Chunk>();
        var cur = new Chunk();
        Chunk? prev = null;
        var o = start;
        while (o < end)
        {
            int imm = BitConverter.ToUInt16(d, o);
            int num = d[o + 2];
            var cmd = d[o + 3] & 0x7F;
            switch (cmd)
            {
                case 0x00 or 0x01 or 0x05 or 0x10 or 0x11 or 0x13:
                    o += 4;
                    break;
                case 0x04:
                    cur.VertexCount = imm;
                    o += 4;
                    break;
                case 0x14:
                    cur.Program = imm;
                    chunks.Add(cur);
                    prev = cur;
                    cur = new Chunk();
                    o += 4;
                    break;
                case 0x20:
                    o += 8;
                    break;
                case 0x30 or 0x31:
                    o += 20;
                    break;
                default:
                    if ((cmd & 0x60) != 0x60) throw new ArgumentException($"VIF command {cmd:X2} at {o:X} isn't handled.");
                    var vn = (cmd >> 2) & 3;
                    var vl = cmd & 3;
                    var unsigned = (imm & 0x4000) != 0;
                    var size = new[] { 4, 2, 1, 2 }[vl] * (vn + 1);
                    var p = o + 4;
                    switch (vn, vl)
                    {
                        case (1, 1):
                            var target = prev ?? cur;
                            target.Uvs = p;
                            target.UvCount = num;
                            break;
                        case (2, 1) when !unsigned:
                            cur.Positions = p;
                            cur.PositionCount = num;
                            break;
                        case (2, 1):
                            cur.Slots = p;
                            cur.SlotCount = num;
                            break;
                        case (2, 2):
                            if (cur.Normals < 0)
                            {
                                cur.Normals = p;
                                cur.NormalCount = num;
                            }

                            break;
                        case (3, 2):
                            cur.Weights = p;
                            cur.WeightCount = num;
                            break;
                        case (3, 1):
                            cur.ExtraSlots = p;
                            cur.ExtraSlotCount = num;
                            break;
                        case (3, 0):
                            cur.GifTag = p;
                            break;
                        default:
                            throw new ArgumentException($"Unpack v{vn + 1}-{new[] { 32, 16, 8, 5 }[vl]} at {o:X} isn't handled.");
                    }

                    o = p + ((num * size + 3) & ~3);
                    if (o > end) throw new ArgumentException("An unpack runs past the end of the mesh.");
                    break;
            }
        }

        return chunks;
    }

    /// <summary>Why the game couldn't draw <paramref name="data"/> as an armour model, or null.</summary>
    public static string? Problem(byte[] data)
    {
        ArmorModel m;
        try
        {
            m = new ArmorModel(data);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            return ex.Message;
        }

        for (var k = 0; k < m.Chunks.Count; k++)
        {
            var c = m.Chunks[k];
            if (c.Positions < 0 || c.Normals < 0 || c.Slots < 0 || c.GifTag < 0 || c.Uvs < 0)
                return $"Chunk {k} lacks part of its data.";
            if (c.PositionCount != c.VertexCount || c.NormalCount != c.VertexCount || c.SlotCount != c.VertexCount + 2)
                return $"Chunk {k}'s counts don't agree.";
        }

        return null;
    }

    // ───────────────────────────── arrays (one row per VIF vertex, chunk after chunk) ─────────────────────────────

    /// <summary>x, y, z per vertex in game units (Z up).</summary>
    public double[] GetPositions()
    {
        var p = new double[VertexCount * 3];
        var i = 0;
        foreach (var c in Chunks)
        {
            for (var k = 0; k < c.VertexCount * 3; k++) p[i++] = BitConverter.ToInt16(Data, c.Positions + 2 * k) / 16.0;
        }

        return p;
    }

    public void SetPositions(double[] positions)
    {
        if (positions.Length != VertexCount * 3) throw new ArgumentException($"{positions.Length / 3} positions for {VertexCount} vertices.");
        var q = new short[positions.Length];
        for (var k = 0; k < q.Length; k++)
        {
            var v = Math.Round(positions[k] * 16.0);
            if (v < short.MinValue || v > short.MaxValue) throw new ArgumentException("A position is outside ±2047.9 units.");
            q[k] = (short)v;
        }

        var i = 0;
        foreach (var c in Chunks)
        {
            for (var k = 0; k < c.VertexCount * 3; k++) BitConverter.TryWriteBytes(Data.AsSpan(c.Positions + 2 * k, 2), q[i++]);
        }
    }

    /// <summary>x, y, z per vertex, length about 1.</summary>
    public double[] GetNormals()
    {
        var n = new double[VertexCount * 3];
        var i = 0;
        foreach (var c in Chunks)
        {
            for (var k = 0; k < c.VertexCount * 3; k++) n[i++] = (sbyte)Data[c.Normals + k] / 127.0;
        }

        return n;
    }

    /// <summary>Writes normals (normalised); <paramref name="rows"/> limits it to some vertices.</summary>
    public void SetNormals(double[] normals, bool[]? rows = null)
    {
        var i = 0;
        foreach (var c in Chunks)
        {
            for (var k = 0; k < c.VertexCount; k++, i++)
            {
                if (rows != null && !rows[i]) continue;
                double x = normals[3 * i], y = normals[3 * i + 1], z = normals[3 * i + 2];
                var len = Math.Sqrt(x * x + y * y + z * z);
                if (len == 0) len = 1;
                Data[c.Normals + 3 * k] = (byte)(sbyte)Math.Clamp(Math.Round(x / len * 127.0), -127, 127);
                Data[c.Normals + 3 * k + 1] = (byte)(sbyte)Math.Clamp(Math.Round(y / len * 127.0), -127, 127);
                Data[c.Normals + 3 * k + 2] = (byte)(sbyte)Math.Clamp(Math.Round(z / len * 127.0), -127, 127);
            }
        }
    }

    /// <summary>Per chunk, per strip slot: u, v in texels.</summary>
    public List<double[]> GetUvs() => Chunks.Select(c =>
        Enumerable.Range(0, c.UvCount * 2).Select(k => BitConverter.ToInt16(Data, c.Uvs + 2 * k) / 16.0).ToArray()).ToList();

    /// <summary>Per vertex: (bone, weight 0..255) pairs, read the way WorldExplorer's VifDecoder does.</summary>
    public List<(int Bone, int Weight)[]> GetWeights()
    {
        var all = new List<(int, int)[]>();
        foreach (var c in Chunks)
        {
            var got = new List<(int, int)[]>();
            var k = 0;
            while (k < c.WeightCount)
            {
                var o = c.Weights + 4 * k;
                int b1 = Data[o], w1 = Data[o + 1], b2 = Data[o + 2], x = Data[o + 3];
                if (b2 == 0xFF)
                {
                    for (var r = 0; r < x; r++) got.Add(new[] { (b1 / 4, w1) });
                    k++;
                }
                else
                {
                    var infl = new List<(int, int)> { (b1 / 4, w1), (b2 / 4, x) };
                    if (w1 + x < 255)
                    {
                        int b3 = Data[o + 4], w3 = Data[o + 5], b4 = Data[o + 6], w4 = Data[o + 7];
                        infl.Add((b3 / 4, w3));
                        if (b4 != 0xFF) infl.Add((b4 / 4, w4));
                        k++;
                    }

                    got.Add(infl.ToArray());
                    k++;
                }
            }

            for (var v = 0; v < c.VertexCount; v++) all.Add(v < got.Count ? got[v] : Array.Empty<(int, int)>());
        }

        return all;
    }

    /// <summary>
    /// Per chunk, per strip slot: the chunk's vertex it shows and whether the slot draws a triangle
    /// (rebuilt the way the VU program does, as WorldExplorer's VifDecoder.ChunksToMesh).
    /// </summary>
    public List<(int Vertex, bool Draws)[]> GetStrips()
    {
        var all = new List<(int, bool)[]>();
        foreach (var c in Chunks)
        {
            var lo = BitConverter.ToUInt64(Data, c.GifTag);
            var n = (int)(lo & 0x7FFF);
            var nreg = (int)(lo >> 60) & 0xF;
            if (nreg == 0) nreg = 16;
            var vs = new int[n];
            for (var i = 2; i < c.SlotCount; i++)
            {
                var v = i - 2;
                int a = BitConverter.ToUInt16(Data, c.Slots + 6 * i);
                int b = BitConverter.ToUInt16(Data, c.Slots + 6 * i + 2);
                int d = BitConverter.ToUInt16(Data, c.Slots + 6 * i + 4);
                int s2 = (b & 0x1FF) / nreg, s3 = (d & 0x1FF) / nreg;
                if (s3 < n && s2 < n) vs[s3] = (vs[s2] & 0x1FF) | (d & 0x8000);
                var s = (a & 0x1FF) / nreg;
                if (v < c.VertexCount && s < n) vs[s] = v | (a & 0x8000);
            }

            if (c.ExtraSlots >= 0)
            {
                int count = BitConverter.ToUInt16(Data, c.ExtraSlots);
                for (var e = 0; e < count; e++)
                {
                    var o = c.ExtraSlots + 8 * (1 + e);
                    int q0 = BitConverter.ToUInt16(Data, o), q1 = BitConverter.ToUInt16(Data, o + 2),
                        q2 = BitConverter.ToUInt16(Data, o + 4), q3 = BitConverter.ToUInt16(Data, o + 6);
                    vs[(q1 & 0x1FF) / nreg] = (q1 & 0x8000) | (vs[(q0 & 0x1FF) / nreg] & 0x1FF);
                    vs[(q3 & 0x1FF) / nreg] = (q3 & 0x8000) | (vs[(q2 & 0x1FF) / nreg] & 0x1FF);
                }
            }

            all.Add(vs.Select(s => (s & 0xFF, (s & 0x8000) == 0)).ToArray());
        }

        return all;
    }

    /// <summary>Triangles: three global vertex numbers, and the chunk and its three strip slots (WorldExplorer's winding).</summary>
    public List<(int A, int B, int C, int Chunk, int SA, int SB, int SC)> GetTriangles()
    {
        var tris = new List<(int, int, int, int, int, int, int)>();
        var strips = GetStrips();
        var b = 0;
        for (var ci = 0; ci < Chunks.Count; ci++)
        {
            var st = strips[ci];
            for (var i = 2; i < st.Length; i++)
            {
                if (!st[i].Draws) continue;
                var (s0, s1, s2) = ((i - 2) & 1) == 0 ? (i - 2, i - 1, i) : (i - 1, i - 2, i);
                tris.Add((b + st[s0].Vertex, b + st[s1].Vertex, b + st[s2].Vertex, ci, s0, s1, s2));
            }

            b += Chunks[ci].VertexCount;
        }

        return tris;
    }

    // ───────────────────────────── editing ─────────────────────────────

    /// <summary>
    /// Maps every VIF vertex to a welded vertex: copies of a point (same 1/16-unit position and the same bone
    /// weights) that several chunks repeat become one, so an edit can't open seams between chunks.
    /// </summary>
    public int[] WeldGroups(out int count)
    {
        var p = GetPositions();
        var w = GetWeights();
        var keys = new Dictionary<string, int>();
        var group = new int[VertexCount];
        for (var i = 0; i < group.Length; i++)
        {
            var key = $"{Math.Round(p[3 * i] * 16)},{Math.Round(p[3 * i + 1] * 16)},{Math.Round(p[3 * i + 2] * 16)}|" +
                      string.Join(";", w[i].Select(x => $"{x.Bone}:{x.Weight}"));
            if (!keys.TryGetValue(key, out var g))
            {
                g = keys.Count;
                keys[key] = g;
            }

            group[i] = g;
        }

        count = keys.Count;
        return group;
    }

    /// <summary>
    /// Sets new positions (VIF order; rounded to 1/16 unit), recalculates the normals of the moved vertices
    /// from the welded mesh, and keeps the header's bounding box and sample points in step.
    /// Returns how many vertices moved; nothing changes when none did.
    /// </summary>
    public int EditPositions(double[] positions, bool recalculateAllNormals = false)
    {
        var old = GetPositions();
        var q = positions.Select(x => Math.Round(x * 16) / 16).ToArray();
        var moved = new bool[VertexCount];
        var count = 0;
        for (var i = 0; i < VertexCount; i++)
        {
            moved[i] = q[3 * i] != old[3 * i] || q[3 * i + 1] != old[3 * i + 1] || q[3 * i + 2] != old[3 * i + 2];
            if (moved[i]) count++;
        }

        if (count == 0) return 0;
        SetPositions(q);
        var normals = RecalculateNormals(q, recalculateAllNormals ? null : moved);
        SetNormals(normals, recalculateAllNormals ? null : moved);
        UpdateBounds(old);
        return count;
    }

    /// <summary>Area-weighted normals of the welded mesh at <paramref name="positions"/>; vertices outside <paramref name="only"/> keep theirs.</summary>
    private double[] RecalculateNormals(double[] positions, bool[]? only)
    {
        var g = WeldGroups(out var groups);
        var acc = new double[groups * 3];
        foreach (var t in GetTriangles())
        {
            double ux = positions[3 * t.B] - positions[3 * t.A], uy = positions[3 * t.B + 1] - positions[3 * t.A + 1],
                uz = positions[3 * t.B + 2] - positions[3 * t.A + 2];
            double vx = positions[3 * t.C] - positions[3 * t.A], vy = positions[3 * t.C + 1] - positions[3 * t.A + 1],
                vz = positions[3 * t.C + 2] - positions[3 * t.A + 2];
            double nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
            foreach (var v in new[] { t.A, t.B, t.C })
            {
                acc[3 * g[v]] += nx;
                acc[3 * g[v] + 1] += ny;
                acc[3 * g[v] + 2] += nz;
            }
        }

        var old = GetNormals();
        var result = new double[VertexCount * 3];
        for (var i = 0; i < VertexCount; i++)
        {
            double x = acc[3 * g[i]], y = acc[3 * g[i] + 1], z = acc[3 * g[i] + 2];
            // keep the file's facing: flip where the new normal points against the old one
            if (x * old[3 * i] + y * old[3 * i + 1] + z * old[3 * i + 2] < 0)
            {
                x = -x;
                y = -y;
                z = -z;
            }

            if (only != null && !only[i])
            {
                x = old[3 * i];
                y = old[3 * i + 1];
                z = old[3 * i + 2];
            }

            result[3 * i] = x;
            result[3 * i + 1] = y;
            result[3 * i + 2] = z;
        }

        return result;
    }

    /// <summary>
    /// The header's bounding box (+0x30) after vertices moved from <paramref name="oldPositions"/>: floor/ceil of
    /// the geometry with the margin the shipped file had kept; each sample point (+0x40) follows its nearest vertex.
    /// </summary>
    private void UpdateBounds(double[] oldPositions)
    {
        var p = GetPositions();
        var n = VertexCount;
        for (var axis = 0; axis < 3; axis++)
        {
            double oldMin = double.MaxValue, oldMax = double.MinValue, min = double.MaxValue, max = double.MinValue;
            for (var i = 0; i < n; i++)
            {
                oldMin = Math.Min(oldMin, oldPositions[3 * i + axis]);
                oldMax = Math.Max(oldMax, oldPositions[3 * i + axis]);
                min = Math.Min(min, p[3 * i + axis]);
                max = Math.Max(max, p[3 * i + axis]);
            }

            var storedLo = (sbyte)Data[0x30 + axis];
            var storedHi = (sbyte)Data[0x33 + axis];
            var marginLo = Math.Max(0, Math.Floor(oldMin) - storedLo);
            var marginHi = Math.Max(0, storedHi - Math.Ceiling(oldMax));
            var lo = Math.Floor(min) - marginLo;
            var hi = Math.Ceiling(max) + marginHi;
            if (lo < -128 || hi > 127) throw new ArgumentException("The model reaches beyond the header's bounding box range (±127 units).");
            Data[0x30 + axis] = (byte)(sbyte)lo;
            Data[0x33 + axis] = (byte)(sbyte)hi;
        }

        for (var k = 0; k < 8; k++)
        {
            var o = 0x40 + 3 * k;
            double px = (sbyte)Data[o], py = (sbyte)Data[o + 1], pz = (sbyte)Data[o + 2];
            var best = 0;
            var bestD = double.MaxValue;
            for (var i = 0; i < n; i++)
            {
                double dx = oldPositions[3 * i] - px, dy = oldPositions[3 * i + 1] - py, dz = oldPositions[3 * i + 2] - pz;
                var d = dx * dx + dy * dy + dz * dz;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            for (var axis = 0; axis < 3; axis++)
            {
                var v = Math.Round(new[] { px, py, pz }[axis] + (p[3 * best + axis] - oldPositions[3 * best + axis]));
                Data[o + axis] = (byte)(sbyte)Math.Clamp(v, -128, 127);
            }
        }
    }

    /// <summary>
    /// New positions with each side of a mirrored pair (x &lt; 0 and x ≥ 0: gloves, boots) scaled about its own
    /// centre; a body or helmet has one side. <paramref name="factor"/> is x, y, z (x left-right, z up).
    /// </summary>
    public double[] ScaledPairs(double fx, double fy, double fz)
    {
        var p = GetPositions();
        var output = (double[])p.Clone();
        var f = new[] { fx, fy, fz };
        foreach (var negative in new[] { true, false })
        {
            var rows = Enumerable.Range(0, VertexCount).Where(i => negative ? p[3 * i] < 0 : p[3 * i] >= 0).ToList();
            if (rows.Count == 0) continue;
            for (var axis = 0; axis < 3; axis++)
            {
                var min = rows.Min(i => p[3 * i + axis]);
                var max = rows.Max(i => p[3 * i + axis]);
                var c = (min + max) / 2;
                foreach (var i in rows) output[3 * i + axis] = c + (p[3 * i + axis] - c) * f[axis];
            }
        }

        return output;
    }

    /// <summary>New positions pushed out along the (welded, averaged) normals by <paramref name="distance"/> units.</summary>
    public double[] Inflated(double distance)
    {
        var g = WeldGroups(out var groups);
        var normals = GetNormals();
        var acc = new double[groups * 3];
        for (var i = 0; i < VertexCount; i++)
        {
            for (var a = 0; a < 3; a++) acc[3 * g[i] + a] += normals[3 * i + a];
        }

        var p = GetPositions();
        for (var i = 0; i < VertexCount; i++)
        {
            double x = acc[3 * g[i]], y = acc[3 * g[i] + 1], z = acc[3 * g[i] + 2];
            var len = Math.Max(Math.Sqrt(x * x + y * y + z * z), 1e-9);
            p[3 * i] += x / len * distance;
            p[3 * i + 1] += y / len * distance;
            p[3 * i + 2] += z / len * distance;
        }

        return p;
    }

    /// <summary>
    /// The model's bytes scaled (<see cref="ScaledPairs"/>), then pushed out along the scaled model's normals
    /// (<see cref="Inflated"/>): what WorldExplorer's Reshape writes, and what its preview shows. This model is
    /// left as it is. Throws <see cref="ArgumentException"/> when the result doesn't fit the format.
    /// </summary>
    public byte[] Reshaped(double scaleX, double scaleY, double scaleZ, double inflate)
    {
        var result = new ArmorModel(Data);
        result.EditPositions(ScaledPairs(scaleX, scaleY, scaleZ));
        if (inflate != 0) result.EditPositions(result.Inflated(inflate));
        return result.Data;
    }
}
