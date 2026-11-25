using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// ARMOR.YAK editing: the game's own file from Test_files (skipped when it is not there), read, rewritten,
/// and edited the ways the Armour menu does.
/// </summary>
[TestFixture]
public class ArmorYakTests
{
    private const string StockMd5 = "7c82d8ae0d07b535ef623ce34b045ca8";

    /// <summary>The first in-game test file (armour-yak/ARMOR.YAK, made by armoryak.py), confirmed by Pete.</summary>
    private const string TestFileMd5 = "ee1ea99b235597814fb23acb50751734";

    private byte[]? _stock;

    [OneTimeSetUp]
    public void FindArmorYak()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "ARMOR.YAK")))
        {
            dir = dir.Parent;
        }

        if (dir != null) _stock = File.ReadAllBytes(Path.Combine(dir.FullName, "Test_files", "ARMOR.YAK"));
    }

    private byte[] Stock()
    {
        if (_stock == null) Assert.Ignore("Test_files/ARMOR.YAK was not found above the test directory.");
        Assert.That(Md5(_stock!), Is.EqualTo(StockMd5), "Test_files/ARMOR.YAK is not the game's own file.");
        return _stock!;
    }

    private YakFile Open()
    {
        var yak = new YakFile(EngineVersion.DarkAlliance, "ARMOR.YAK", Stock());
        yak.ReadEntries();
        return yak;
    }

    private static string Md5(byte[] data) => Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();

    [Test]
    public void TheGamesFile_HasItsEntriesAndNoProblems()
    {
        var yak = Open();
        Assert.That(yak.EntryCount, Is.EqualTo(ArmorYakInfo.ShippedEntries));
        Assert.That(YakFile.Problems(Stock()), Is.Empty);
        Assert.That(ArmorYakInfo.EntryName(0), Is.EqualTo("Clothes body"));
        Assert.That(ArmorYakInfo.EntryName(20), Is.EqualTo("Plate body (enchanted)"));
        Assert.That(ArmorYakInfo.EntryName(58), Is.EqualTo("Half-plate helmet (enchanted)"));
        Assert.That(ArmorYakInfo.EntryName(59), Is.EqualTo("Added entry"));
        Assert.That(ArmorYakInfo.SlotOf(25), Is.EqualTo(ArmorSlot.Helmet));
        // the largest shipped child: the elf's clothes, 0x33800 bytes read into the 0x34800 body buffer
        Assert.That(ArmorYakInfo.ReadSize(yak.GetChild(0, 1).Length), Is.EqualTo(0x33800));
    }

    [Test]
    public void AnUneditedFile_IsWrittenByteForByte()
    {
        var yak = Open();
        Assert.That(yak.IsDirty, Is.False);
        Assert.That(yak.Write(), Is.EqualTo(Stock()));
    }

    [Test]
    public void EveryTexture_ComesBackByteForByte()
    {
        var yak = Open();
        var partial = 0;
        for (var e = 0; e < yak.EntryCount; e++)
        {
            for (var c = 0; c < 4; c++)
            {
                var tex = yak.GetChild(e, c).Tex;
                var t = new ArmorTexture(tex);
                if (!t.Coverage().All(x => x)) partial++;
                var again = new ArmorTexture(tex);
                again.SetPalette(t.GetPalette());
                Assert.That(again.SetIndices(t.GetIndices()), Is.EqualTo(0));
                Assert.That(again.Data, Is.EqualTo(tex), $"entry {e} child {c}");
                var picture = new ArmorTexture(tex);
                Assert.That(picture.FromRgba(t.ToRgba()), Is.EqualTo((0, 0)), $"entry {e} child {c}");
                Assert.That(picture.Data, Is.EqualTo(tex), $"entry {e} child {c} as a picture");
            }
        }

        Assert.That(partial, Is.EqualTo(30));
    }

    [Test]
    public void EveryModel_ComesBackThroughObj()
    {
        var yak = Open();
        for (var e = 0; e < yak.EntryCount; e++)
        {
            for (var c = 0; c < 4; c++)
            {
                var child = yak.GetChild(e, c);
                Assert.That(ArmorModel.Problem(child.Vif), Is.Null, $"entry {e} child {c}");
                var m = new ArmorModel(child.Vif);
                var t = new ArmorTexture(child.Tex);
                var obj = ArmorObj.Write(m, t.Width, t.Height, $"{e}_{c}");
                var positions = ArmorObj.ReadPositions(m, obj);
                Assert.That(positions, Is.EqualTo(m.GetPositions()), $"entry {e} child {c}");
                Assert.That(m.EditPositions(positions), Is.EqualTo(0));
                Assert.That(m.Data, Is.EqualTo(child.Vif));
            }
        }
    }

    [Test]
    public void AnObjWithOtherVerticesOrOrder_IsRefused()
    {
        var yak = Open();
        var child = yak.GetChild(2, 1);
        var m = new ArmorModel(child.Vif);
        var obj = ArmorObj.Write(m, 64, 64, "gloves");
        var lines = obj.Split('\n').ToList();
        var vertices = lines.Where(l => l.StartsWith("v ")).ToList();
        var others = lines.Where(l => !l.StartsWith("v ")).ToList();

        var reversed = string.Join("\n", vertices.AsEnumerable().Reverse().Concat(others));
        Assert.That(() => ArmorObj.ReadPositions(m, reversed), Throws.TypeOf<InvalidDataException>());
        var oneLess = string.Join("\n", vertices.Skip(1).Concat(others));
        Assert.That(() => ArmorObj.ReadPositions(m, oneLess), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void AMovedObj_ChangesTheShapeNormalsAndBounds()
    {
        var yak = Open();
        var child = yak.GetChild(25, 0); // plate helmet, human
        var m = new ArmorModel(child.Vif);
        var obj = ArmorObj.Write(m, 128, 64, "helmet");
        // scale the OBJ by 1.2 about its centroid, as a modeller would
        var lines = obj.Split('\n');
        var v = lines.Where(l => l.StartsWith("v ")).Select(l => l.Split(' ').Skip(1).Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray()).ToList();
        var centre = Enumerable.Range(0, 3).Select(a => v.Average(x => x[a])).ToArray();
        var k = 0;
        var edited = string.Join("\n", lines.Select(l => l.StartsWith("v ")
            ? FormattableString.Invariant($"v {centre[0] + (v[k][0] - centre[0]) * 1.2} {centre[1] + (v[k][1] - centre[1]) * 1.2} {centre[2] + (v[k++][2] - centre[2]) * 1.2}")
            : l));
        var moved = m.EditPositions(ArmorObj.ReadPositions(m, edited));
        Assert.That(moved, Is.EqualTo(597));
        var before = new ArmorModel(child.Vif).GetPositions();
        var after = m.GetPositions();
        double Extent(double[] p, int axis) => Enumerable.Range(0, p.Length / 3).Max(i => p[3 * i + axis]) -
                                              Enumerable.Range(0, p.Length / 3).Min(i => p[3 * i + axis]);
        for (var axis = 0; axis < 3; axis++)
        {
            Assert.That(Extent(after, axis) / Extent(before, axis), Is.EqualTo(1.2).Within(0.02));
        }

        // the header's box grows with it: -5,-7,64 .. 5,6,78 becomes -6,-8,63 .. 6,8,80
        Assert.That(m.Data.Skip(0x30).Take(6).Select(b => (int)(sbyte)b).ToArray(), Is.EqualTo(new[] { -6, -8, 63, 6, 8, 80 }));
        Assert.That(ArmorModel.Problem(m.Data), Is.Null);
    }

    [Test]
    public void MovedVertices_KeepTheHeadersMarginAndSamplePoints()
    {
        // the human's clothes body stores y from -10 although its vertices start at -9.0: that margin stays
        var child = Open().GetChild(0, 0);
        var m = new ArmorModel(child.Vif);
        Assert.That(m.Data.Skip(0x30).Take(6).Select(b => (int)(sbyte)b).ToArray(), Is.EqualTo(new[] { -18, -10, 4, 18, 8, 68 }));
        var p = m.GetPositions();
        for (var i = 0; i < p.Length / 3; i++) p[3 * i + 1] += 2;
        Assert.That(m.EditPositions(p), Is.EqualTo(m.VertexCount));
        Assert.That(m.Data.Skip(0x30).Take(6).Select(b => (int)(sbyte)b).ToArray(), Is.EqualTo(new[] { -18, -8, 4, 18, 10, 68 }));
        var before = child.Vif.Skip(0x40).Take(24).Select(b => (int)(sbyte)b).ToArray();
        var after = m.Data.Skip(0x40).Take(24).Select(b => (int)(sbyte)b).ToArray();
        for (var k = 0; k < 8; k++)
        {
            Assert.That(after[3 * k], Is.EqualTo(before[3 * k]));
            Assert.That(after[3 * k + 1], Is.EqualTo(before[3 * k + 1] + 2));
            Assert.That(after[3 * k + 2], Is.EqualTo(before[3 * k + 2]));
        }
    }

    [Test]
    public void Reshaped_IsWhatTheStepsWrite_AndLeavesTheModelAlone()
    {
        var yak = Open();
        foreach (var (entry, c) in new[] { (2, 0), (19, 1), (25, 2) }) // clothes gloves, plate body, plate helmet
        {
            var vif = yak.GetChild(entry, c).Vif;
            var m = new ArmorModel(vif);
            Assert.That(m.Reshaped(1, 1, 1, 0), Is.EqualTo(vif));

            // the steps Reshape ran before Reshaped existed (and make_test.py's gloves)
            var scaled = new ArmorModel(vif);
            scaled.EditPositions(m.ScaledPairs(1.2, 0.9, 1.1));
            scaled.EditPositions(scaled.Inflated(0.5));
            Assert.That(m.Reshaped(1.2, 0.9, 1.1, 0.5), Is.EqualTo(scaled.Data));
            Assert.That(m.Data, Is.EqualTo(vif));
            Assert.That(ArmorModel.Problem(scaled.Data), Is.Null);
        }

        // nothing within Reshape's sliders (50-250 %, -1..4 units) leaves the header's ±127-unit box; past them the
        // human's clothes body at 250 % pushed out 12 units does, and is refused
        var body = new ArmorModel(yak.GetChild(0, 0).Vif);
        Assert.That(body.Reshaped(2.5, 2.5, 2.5, 4), Is.Not.EqualTo(body.Data));
        Assert.Throws<ArgumentException>(() => body.Reshaped(2.5, 2.5, 2.5, 12));
    }

    [Test]
    public void TheFirstTestFile_IsMadeByteForByte()
    {
        // armoryak.py's make_test.py: entry 0 palette, entry 1 pixels, entry 2 shape, for children 0-2
        var yak = Open();
        var body = new (double Shift, double From, double To, double Sat, double Val)[]
        {
            (-105, 65, 135, 1.7, 1.15), (200, 45, 100, 1.1, 1.0), (-170, 180, 270, 1.2, 1.15)
        };
        for (var c = 0; c < 3; c++)
        {
            var b = yak.GetChild(0, c);
            var t = new ArmorTexture(b.Tex);
            var (shift, from, to, sat, val) = body[c];
            t.SetPalette(ArmorTexture.HueShift(t.GetPalette(), shift, from, to, 0.15, sat, val));
            yak.SetChild(0, c, b.Vif, t.Data);

            var boots = yak.GetChild(1, c);
            var bt = new ArmorTexture(boots.Tex);
            Assert.That(bt.FromRgba(Harlequin(bt.ToRgba(), bt.Width, bt.Height)).Lost, Is.EqualTo(0));
            yak.SetChild(1, c, boots.Vif, bt.Data);

            var gloves = yak.GetChild(2, c);
            var m = new ArmorModel(gloves.Vif);
            m.EditPositions(m.ScaledPairs(1.0, 1.8, 1.8));
            yak.SetChild(2, c, m.Data, gloves.Tex);
        }

        var written = yak.Write();
        Assert.That(written.Length, Is.EqualTo(Stock().Length));
        Assert.That(YakFile.Problems(written), Is.Empty);
        Assert.That(Md5(written), Is.EqualTo(TestFileMd5));
    }

    /// <summary>make_test.py's harlequin: red and white diamonds over the boots' own shading.</summary>
    private static byte[] Harlequin(byte[] rgba, int w, int h, int cell = 12)
    {
        var output = (byte[])rgba.Clone();
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var i = 4 * (y * w + x);
                var lum = (0.30 * rgba[i] + 0.59 * rgba[i + 1] + 0.11 * rgba[i + 2]) / 255.0;
                lum = 0.35 + 0.9 * lum;
                var diamond = ((x + y) / cell + (x - y + 4096) / cell) % 2;
                var colour = diamond == 1 ? new[] { 200.0, 30, 40 } : new[] { 235.0, 230, 220 };
                for (var k = 0; k < 3; k++) output[i + k] = (byte)Math.Clamp(colour[k] * lum, 0, 255);
            }
        }

        return output;
    }

    [Test]
    public void AddedEntries_ShareTheirBytesUntilEdited()
    {
        var yak = Open();
        var first = yak.AddEntries(19);
        Assert.That(first, Is.EqualTo(59));
        Assert.That(yak.EntryCount, Is.EqualTo(61));
        Assert.That(yak.IsDirty, Is.True);
        var shared = yak.Write();
        Assert.That(shared.Length, Is.EqualTo(Stock().Length), "copies point at the bytes they copy");

        var t = new ArmorTexture(yak.GetChild(59, 0).Tex);
        t.SetPalette(ArmorTexture.HueShift(t.GetPalette(), 0, minSaturation: 0, saturationScale: 0, valueScale: 0.6));
        yak.SetChild(59, 0, yak.GetChild(59, 0).Vif, t.Data);
        var written = yak.Write();
        Assert.That(written.Length, Is.EqualTo(ArmorYakInfo.ReadSize(Stock().Length) + yak.GetChild(59, 0).Length));
        Assert.That(YakFile.Problems(written), Is.Empty);

        var back = new YakFile(EngineVersion.DarkAlliance, "ARMOR.YAK", written);
        back.ReadEntries();
        Assert.That(back.EntryCount, Is.EqualTo(61));
        Assert.That(back.GetChild(59, 0).Tex, Is.EqualTo(t.Data));
        Assert.That(back.GetChild(60, 1).Vif, Is.EqualTo(yak.GetChild(20, 1).Vif));
        var stock = Open();
        for (var e = 0; e < 59; e++)
        {
            for (var c = 0; c < 4; c++) Assert.That(back.Entries[e].Children[c]!.VifOffset, Is.EqualTo(stock.Entries[e].Children[c]!.VifOffset));
        }

        yak.RemoveLastAddedEntry();
        yak.RemoveLastAddedEntry();
        Assert.That(yak.IsDirty, Is.False);
        Assert.That(yak.Write(), Is.EqualTo(Stock()));
    }

    [Test]
    public void AChildThatGrows_MovesToTheEnd_AndOneTooBigIsReported()
    {
        var yak = Open();
        var helmet = yak.GetChild(25, 0);
        // a texture padded past the helmet's 0x6800 buffer
        var big = helmet.Tex.Concat(new byte[0x8000]).ToArray();
        yak.SetChild(25, 0, helmet.Vif, big);
        var written = yak.Write();
        Assert.That(written.Length, Is.GreaterThan(Stock().Length));
        var problems = YakFile.Problems(written);
        Assert.That(problems.Any(p => p.Contains("Entry 25") && p.Contains("helmet buffer")), Is.True);
    }

    [Test]
    public void SavedState_FollowsEditsAndSaves()
    {
        var yak = Open();
        Assert.That(yak.HasUnsavedChanges, Is.False);
        var child = yak.GetChild(9, 2);
        var m = new ArmorModel(child.Vif);
        m.HiddenBodyParts |= 1UL << 10;
        yak.SetChild(9, 2, m.Data, child.Tex);
        Assert.That(yak.HasUnsavedChanges, Is.True);
        Assert.That(yak.IsChildEdited(9, 2), Is.True);
        yak.MarkSaved();
        Assert.That(yak.HasUnsavedChanges, Is.False);
        Assert.That(yak.IsDirty, Is.True);
        yak.RevertChild(9, 2);
        Assert.That(yak.HasUnsavedChanges, Is.True);
        Assert.That(yak.IsDirty, Is.False);
        Assert.That(ArmorYakInfo.MaskText(new ArmorModel(yak.GetChild(25, 0).Vif).HiddenBodyParts, 0),
            Is.EqualTo("eyes, mouth, scalp, ears, face"));
    }

    [Test]
    public void HueShift_OnlyTouchesItsRange()
    {
        var palette = new byte[1024];
        palette[0] = 40; palette[1] = 160; palette[2] = 50; palette[3] = 1;   // green
        palette[4] = 200; palette[5] = 150; palette[6] = 120; palette[7] = 1; // skin
        var shifted = ArmorTexture.HueShift(palette, 180, 60, 180);
        Assert.That(shifted.Skip(4).Take(4).ToArray(), Is.EqualTo(new byte[] { 200, 150, 120, 1 }));
        Assert.That(shifted.Take(4).ToArray(), Is.Not.EqualTo(palette.Take(4).ToArray()));
        Assert.That(shifted[3], Is.EqualTo(1));
    }
}
