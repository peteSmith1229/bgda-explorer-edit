// ═══════════════════════════════════════════════════════════════════════════════
// The parts of an ARMOR.YAK piece that are its wearer's own body: skin, face and hair.
//
// A few pieces are the character's own body with clothes painted on. The elf's clothes
// body (entry 0) shares 87% of its vertices with elf.vif (face, scalp, chest, torso, neck,
// upper arms) and its texture holds a copy of elf.tex; her clothes gloves (entry 2) are
// her forearms and hands. Recolouring or reshaping such a piece would change her face and
// skin too, in the game as well. Every other piece meets the body only along its seams.
//
// A triangle of a piece is the wearer's own when its three corners lie on a triangle of
// the wearer's body (the same 1/16-unit positions) and the piece's texture shows the
// body's own colours there: sampled at four points of the triangle through each model's
// UVs, no colour channel differs by more than OwnColourTolerance on average. Its texels and
// vertices (with their welded copies) are then the wearer's own.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Armour;

public sealed class ArmorOwnParts
{
    /// <summary>The largest average colour difference (0-255, per channel) between piece and body for "own".</summary>
    public const double OwnColourTolerance = 24;

    /// <summary>Per triangle of the piece (<see cref="ArmorModel.GetTriangles"/> order): the wearer's own.</summary>
    public bool[] Triangles { get; }

    /// <summary>Per VIF vertex: part of an own triangle (itself or a welded copy).</summary>
    public bool[] Vertices { get; }

    /// <summary>Width × Height of the piece's texture: drawn by an own triangle.</summary>
    public bool[] Texels { get; }

    /// <summary>
    /// Width × Height: drawn by the piece's other triangles (with the texel around each, which filtering can blend in),
    /// and not own. The texels in neither are drawn by nothing.
    /// </summary>
    public bool[] Others { get; }

    public int TriangleCount => Triangles.Count(t => t);
    public int VertexCount => Vertices.Count(v => v);
    public int TexelCount => Texels.Count(t => t);

    /// <summary>Whether any of the piece is the wearer's own.</summary>
    public bool Any => TriangleCount > 0;

    private ArmorOwnParts(bool[] triangles, bool[] vertices, bool[] texels, bool[] others)
    {
        Triangles = triangles;
        Vertices = vertices;
        Texels = texels;
        Others = others;
    }

    /// <summary>Nothing of the piece is the wearer's own.</summary>
    public static ArmorOwnParts None(ArmorModel piece, ArmorTexture texture) =>
        new(new bool[piece.GetTriangles().Count], new bool[piece.VertexCount], new bool[texture.Width * texture.Height],
            Enumerable.Repeat(true, texture.Width * texture.Height).ToArray());

    /// <summary>Finds the parts of <paramref name="piece"/> that are <paramref name="body"/>'s own (see the file notes).</summary>
    public static ArmorOwnParts Find(ArmorModel piece, ArmorTexture texture, ArmorModel body, ArmorTexture bodyTexture)
    {
        var scores = ColourDifferences(piece, texture, body, bodyTexture);
        var own = scores.Select(d => d <= OwnColourTolerance).ToArray();
        var triangles = piece.GetTriangles();
        var uvs = piece.GetUvs();

        // vertices: those of own triangles, with their welded copies in other chunks
        var vertices = new bool[piece.VertexCount];
        var groups = piece.WeldGroups(out var groupCount);
        var ownGroup = new bool[groupCount];
        for (var i = 0; i < triangles.Count; i++)
        {
            if (!own[i]) continue;
            ownGroup[groups[triangles[i].A]] = ownGroup[groups[triangles[i].B]] = ownGroup[groups[triangles[i].C]] = true;
        }

        for (var v = 0; v < vertices.Length; v++) vertices[v] = ownGroup[groups[v]];

        // texels: those an own triangle draws (texel centres inside it in UV space, and its corners' texels); the
        // others' with one texel around them, as bilinear filtering reaches that far
        var texels = new bool[texture.Width * texture.Height];
        var drawn = new bool[texels.Length];
        for (var i = 0; i < triangles.Count; i++)
        {
            var t = triangles[i];
            MarkTexels(own[i] ? texels : drawn, texture.Width, texture.Height, uvs[t.Chunk], t.SA, t.SB, t.SC);
        }

        var others = new bool[texels.Length];
        for (var y = 0; y < texture.Height; y++)
        {
            for (var x = 0; x < texture.Width; x++)
            {
                if (!drawn[y * texture.Width + x]) continue;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var i = (y + dy + texture.Height) % texture.Height * texture.Width + (x + dx + texture.Width) % texture.Width;
                        if (!texels[i]) others[i] = true;
                    }
                }
            }
        }

        return new ArmorOwnParts(own, vertices, texels, others);
    }

    /// <summary>
    /// Per triangle of the piece: how far its colours are from the body's own at the same place (the average over four
    /// points of the largest channel difference, 0-255), or NaN when it doesn't lie on a triangle of the body.
    /// </summary>
    public static double[] ColourDifferences(ArmorModel piece, ArmorTexture texture, ArmorModel body, ArmorTexture bodyTexture)
    {
        var bodyKeys = Keys(body.GetPositions());
        var bodyUvs = body.GetUvs();
        var bodyTriangles = new Dictionary<(long, long, long), (int A, int B, int C, int Chunk, int SA, int SB, int SC)>();
        foreach (var t in body.GetTriangles())
        {
            bodyTriangles.TryAdd(Sorted(bodyKeys[t.A], bodyKeys[t.B], bodyKeys[t.C]), t);
        }

        var onBody = new HashSet<long>(bodyKeys);
        var keys = Keys(piece.GetPositions());
        var uvs = piece.GetUvs();
        var rgba = texture.ToRgba();
        var bodyRgba = bodyTexture.ToRgba();
        var triangles = piece.GetTriangles();
        var scores = new double[triangles.Count];
        var weights = new[] { (1 / 3.0, 1 / 3.0, 1 / 3.0), (0.6, 0.2, 0.2), (0.2, 0.6, 0.2), (0.2, 0.2, 0.6) };

        for (var i = 0; i < triangles.Count; i++)
        {
            scores[i] = double.NaN;
            var t = triangles[i];
            var (ka, kb, kc) = (keys[t.A], keys[t.B], keys[t.C]);
            if (!onBody.Contains(ka) || !onBody.Contains(kb) || !onBody.Contains(kc)) continue;
            if (!bodyTriangles.TryGetValue(Sorted(ka, kb, kc), out var b)) continue;

            // the body triangle's corners in the piece triangle's order, matched by position
            (double U, double V) BodyUv(long key)
            {
                var slot = bodyKeys[b.A] == key ? b.SA : bodyKeys[b.B] == key ? b.SB : b.SC;
                return (bodyUvs[b.Chunk][2 * slot], bodyUvs[b.Chunk][2 * slot + 1]);
            }

            (double U, double V) PieceUv(int slot) => (uvs[t.Chunk][2 * slot], uvs[t.Chunk][2 * slot + 1]);
            var pc = new[] { PieceUv(t.SA), PieceUv(t.SB), PieceUv(t.SC) };
            var bc = new[] { BodyUv(ka), BodyUv(kb), BodyUv(kc) };
            double total = 0;
            foreach (var (wa, wb, wc) in weights)
            {
                var p = Sample(rgba, texture.Width, texture.Height, wa * pc[0].U + wb * pc[1].U + wc * pc[2].U,
                    wa * pc[0].V + wb * pc[1].V + wc * pc[2].V);
                var q = Sample(bodyRgba, bodyTexture.Width, bodyTexture.Height, wa * bc[0].U + wb * bc[1].U + wc * bc[2].U,
                    wa * bc[0].V + wb * bc[1].V + wc * bc[2].V);
                total += Enumerable.Range(0, 3).Max(ch => Math.Abs(rgba[p + ch] - bodyRgba[q + ch]));
            }

            scores[i] = total / weights.Length;
        }

        return scores;
    }

    private static long[] Keys(double[] positions)
    {
        var keys = new long[positions.Length / 3];
        for (var i = 0; i < keys.Length; i++)
        {
            long x = (long)Math.Round(positions[3 * i] * 16) + 32768, y = (long)Math.Round(positions[3 * i + 1] * 16) + 32768,
                z = (long)Math.Round(positions[3 * i + 2] * 16) + 32768;
            keys[i] = (x << 32) | (y << 16) | z;
        }

        return keys;
    }

    private static (long, long, long) Sorted(long a, long b, long c)
    {
        if (a > b) (a, b) = (b, a);
        if (b > c) (b, c) = (c, b);
        if (a > b) (a, b) = (b, a);
        return (a, b, c);
    }

    /// <summary>Offset of the RGBA texel at (u, v) texels, wrapping as the GS does.</summary>
    private static int Sample(byte[] rgba, int width, int height, double u, double v)
    {
        var x = ((int)Math.Floor(u) % width + width) % width;
        var y = ((int)Math.Floor(v) % height + height) % height;
        return 4 * (y * width + x);
    }

    private static void MarkTexels(bool[] texels, int width, int height, double[] uv, int sa, int sb, int sc)
    {
        double ax = uv[2 * sa], ay = uv[2 * sa + 1], bx = uv[2 * sb], by = uv[2 * sb + 1], cx = uv[2 * sc], cy = uv[2 * sc + 1];
        void Mark(double u, double v)
        {
            var x = ((int)Math.Floor(u) % width + width) % width;
            var y = ((int)Math.Floor(v) % height + height) % height;
            texels[y * width + x] = true;
        }

        Mark(ax, ay);
        Mark(bx, by);
        Mark(cx, cy);
        var den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy);
        if (Math.Abs(den) < 1e-9) return;
        int x0 = (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx))), x1 = (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx)));
        int y0 = (int)Math.Floor(Math.Min(ay, Math.Min(by, cy))), y1 = (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy)));
        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                double px = x + 0.5, py = y + 0.5;
                var w0 = ((by - cy) * (px - cx) + (cx - bx) * (py - cy)) / den;
                var w1 = ((cy - ay) * (px - cx) + (ax - cx) * (py - cy)) / den;
                if (w0 >= -1e-9 && w1 >= -1e-9 && 1 - w0 - w1 >= -1e-9) Mark(px, py);
            }
        }
    }
}
