// ═══════════════════════════════════════════════════════════════════════════════
// OBJ files for reshaping ARMOR.YAK models in Blender or any modeller.
//
// The OBJ has one vertex per welded vertex (copies of a point that the game repeats in
// several strips become one, so seams can't open), Y up (as Blender and most tools read
// OBJ), in game units, with the faces, per-slot UVs and normals for viewing. Only the
// positions come back: the vertex count and order must stay as exported, which is checked.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Data.Armour;

public static class ArmorObj
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>OBJ text for <paramref name="model"/>; <paramref name="mtlName"/> (optional) names a material file.</summary>
    public static string Write(ArmorModel model, int textureWidth, int textureHeight, string name, string? mtlName = null)
    {
        var groups = model.WeldGroups(out var count);
        var first = new int[count];
        Array.Fill(first, -1);
        for (var i = 0; i < groups.Length; i++)
        {
            if (first[groups[i]] < 0) first[groups[i]] = i;
        }

        var p = model.GetPositions();
        var n = model.GetNormals();
        var sb = new StringBuilder();
        sb.Append($"# ARMOR.YAK {name}: {count} vertices (welded from {model.VertexCount}), Y up, game units.\n");
        sb.Append("# Move vertices freely but keep their number and order (Blender: don't add, delete or merge\n");
        sb.Append("# vertices). Only the positions are read back.\n");
        if (mtlName != null) sb.Append($"mtllib {mtlName}\n");
        foreach (var i in first)
        {
            // game (x, y, z), Z up → OBJ (x, z, -y), Y up
            sb.Append(string.Format(Inv, "v {0:0.####} {1:0.####} {2:0.####}\n", p[3 * i], p[3 * i + 2], -p[3 * i + 1]));
        }

        foreach (var i in first)
        {
            sb.Append(string.Format(Inv, "vn {0:0.####} {1:0.####} {2:0.####}\n", n[3 * i], n[3 * i + 2], -n[3 * i + 1]));
        }

        var uvs = model.GetUvs();
        var vtBase = new int[uvs.Count];
        var k = 0;
        for (var c = 0; c < uvs.Count; c++)
        {
            vtBase[c] = k;
            for (var s = 0; s < uvs[c].Length / 2; s++)
            {
                sb.Append(string.Format(Inv, "vt {0:0.#####} {1:0.#####}\n", uvs[c][2 * s] / textureWidth,
                    1.0 - uvs[c][2 * s + 1] / textureHeight));
                k++;
            }
        }

        sb.Append($"o {name}\n");
        if (mtlName != null) sb.Append("usemtl armour\n");
        sb.Append("s 1\n");
        foreach (var t in model.GetTriangles())
        {
            // counter-clockwise for viewers (the game draws both sides)
            var corners = new[] { (t.C, t.SC), (t.B, t.SB), (t.A, t.SA) };
            sb.Append("f " + string.Join(" ", corners.Select(x =>
                $"{groups[x.Item1] + 1}/{vtBase[t.Chunk] + x.Item2 + 1}/{groups[x.Item1] + 1}")) + "\n");
        }

        return sb.ToString();
    }

    /// <summary>A material file pointing at the texture's PNG.</summary>
    public static string WriteMtl(string pngName) => $"newmtl armour\nKd 1 1 1\nmap_Kd {pngName}\n";

    /// <summary>
    /// The positions (VIF order, game units) from an OBJ written by <see cref="Write"/> and edited.
    /// Throws when the vertex count changed or the vertices were reordered.
    /// </summary>
    public static double[] ReadPositions(ArmorModel model, string objText)
    {
        var vertices = new List<double[]>();
        using (var reader = new StringReader(objText))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4 || parts[0] != "v") continue;
                vertices.Add(new[]
                {
                    double.Parse(parts[1], NumberStyles.Float, Inv), double.Parse(parts[2], NumberStyles.Float, Inv),
                    double.Parse(parts[3], NumberStyles.Float, Inv)
                });
            }
        }

        var groups = model.WeldGroups(out var count);
        if (vertices.Count != count)
            throw new InvalidDataException($"The OBJ has {vertices.Count} vertices; it must keep the {count} it was " +
                                           "exported with, in the same order.");

        // back to game space: OBJ (x, y, z), Y up → game (x, -z, y)
        var welded = vertices.Select(v => new[] { v[0], -v[2], v[1] }).ToList();

        // a reordered mesh shows as many vertices sitting exactly on another vertex's original place
        var original = new double[count][];
        var p = model.GetPositions();
        for (var i = 0; i < groups.Length; i++) original[groups[i]] ??= new[] { p[3 * i], p[3 * i + 1], p[3 * i + 2] };
        var place = new Dictionary<(long, long, long), int>();
        for (var g = 0; g < count; g++) place.TryAdd(Key(original[g]), g);
        int same = 0, other = 0;
        for (var g = 0; g < count; g++)
        {
            if (!place.TryGetValue(Key(welded[g]), out var at)) continue;
            if (at == g) same++;
            else other++;
        }

        if (other > Math.Max(same, count / 10))
            throw new InvalidDataException($"The vertex order changed ({other} vertices sit on another vertex's " +
                                           "original place). Export again keeping the vertex order.");

        var positions = new double[model.VertexCount * 3];
        for (var i = 0; i < groups.Length; i++)
        {
            positions[3 * i] = welded[groups[i]][0];
            positions[3 * i + 1] = welded[groups[i]][1];
            positions[3 * i + 2] = welded[groups[i]][2];
        }

        return positions;
    }

    private static (long, long, long) Key(double[] v) =>
        ((long)Math.Round(v[0] * 16), (long)Math.Round(v[1] * 16), (long)Math.Round(v[2] * 16));
}
