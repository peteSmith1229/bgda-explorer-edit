using JetBlackEngineLib.Data.Executable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Glow styles (BgdaExecutable.Glow.cs) on the stock PAL executable in Test_files (skipped when it is not present):
/// putting GLOW in and taking it out, the styles, rules and item settings, what is refused, and the three-player patch.
/// What GLOW does in the game was checked by running it in an emulator (glowpatch.py, test_glow.py); the expected
/// glows here are the ones that run gave.
/// </summary>
[TestFixture]
public class BgdaGlowTests
{
    private const int VaToFile = 0xFF000;
    private string _stockPath = "";
    private byte[] _stockBytes = Array.Empty<byte>();
    private string _workDir = "";

    [OneTimeSetUp]
    public void FindStockExecutable()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "SLES_506.72")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Ignore("Test_files/SLES_506.72 was not found above the test directory.");
        }

        _stockPath = Path.Combine(dir!.FullName, "Test_files", "SLES_506.72");
        _stockBytes = File.ReadAllBytes(_stockPath);
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaGlowTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    private BgdaExecutable Stock() => BgdaExecutable.Open(_stockPath);

    private (BgdaExecutable Exe, byte[] Bytes) SaveAndReopen(BgdaExecutable exe, string name)
    {
        var path = Path.Combine(_workDir, name);
        exe.Save(path);
        return (BgdaExecutable.Open(path), File.ReadAllBytes(path));
    }

    private static int[] Differences(byte[] a, byte[] b) =>
        Enumerable.Range(0, Math.Min(a.Length, b.Length)).Where(i => a[i] != b[i]).ToArray();

    private static bool InGlowCode(int fileOffset) =>
        fileOffset + VaToFile is >= BgdaExecutable.GlowCodeVa and < BgdaExecutable.GlowCodeEndVa ||
        fileOffset + VaToFile is >= BgdaExecutable.GlowFunctionVa and < BgdaExecutable.GlowFunctionVa + 8;

    private static bool IsItemGlowByte(int fileOffset)
    {
        var va = fileOffset + VaToFile;
        return va >= BgdaExecutable.ItemTableVa && (va - BgdaExecutable.ItemTableVa) % BgdaExecutable.ItemRecordSize == BgdaExecutable.ItemGlowOffset;
    }

    private static ulong Bits(params int[] bits) => bits.Aggregate(0UL, (m, b) => m | 1UL << b);

    private static GlowStyle Style(int hr, int hg, int hb, int ha, int cr, int cg, int cb, int ca) =>
        new(new GlowLayer(new Rgb(hr, hg, hb), ha), new GlowLayer(new Rgb(cr, cg, cb), ca));

    /// <summary>The set tried in the game: styles 1-8 and eight rules (glowpatch.py's test styles).</summary>
    private static GlowSettings Tested()
    {
        var styles = Enumerable.Repeat(GlowStyle.Game, 16).ToArray();
        styles[1] = Style(0, 70, 110, 64, 120, 220, 255, 72);
        styles[2] = Style(110, 100, 0, 64, 255, 255, 120, 72);
        styles[3] = Style(110, 40, 0, 64, 255, 140, 20, 72);
        styles[4] = Style(110, 0, 0, 64, 255, 30, 30, 72);
        styles[5] = Style(60, 60, 70, 64, 220, 220, 235, 72);
        styles[6] = Style(70, 0, 110, 64, 200, 90, 255, 72);
        styles[7] = Style(110, 90, 30, 64, 255, 240, 170, 72);
        styles[8] = Style(0, 110, 20, 64, 120, 255, 140, 72);
        return new GlowSettings(styles, new[]
        {
            new GlowRule(Bits(14), 7), new GlowRule(Bits(6, 12), 1), new GlowRule(Bits(7, 13), 2), new GlowRule(Bits(4, 11), 3),
            new GlowRule(Bits(8), 4), new GlowRule(Bits(9, 23), 8), new GlowRule(Bits(10), 5), new GlowRule(Bits(5), null)
        });
    }

    private static void SetGlow(BgdaExecutable exe, string name, ItemGlow glow)
    {
        var item = exe.GetItems().Single(i => i.Name == name);
        item.Glow = glow;
        exe.SetItem(item);
    }

    // ── on and off ──

    [Test]
    public void StockExecutable_HasTheGamesOwnGlow()
    {
        var exe = Stock();
        Assert.That(exe.GetGlowStylesState(), Is.EqualTo(GlowStylesState.Off));
        Assert.That(exe.HasGlowStyles, Is.False);
        Assert.That(exe.GlowStylesProblem(), Is.Null, "glow styles can be put in");
        var settings = exe.GetGlowSettings();
        Assert.That(settings.Styles, Has.Count.EqualTo(16));
        Assert.That(settings.Styles.All(s => s == GlowStyle.Game), "every style the game's colours at 64 and 72");
        Assert.That(settings.Rules, Is.Empty);
        Assert.That(exe.GetItems().All(i => i.Glow.IsAutomatic), "the padding byte is zero in every record");
    }

    [Test]
    public void GlowStyles_WithNothingSet_ChangeOnlyTheGlowFunctionAndTheDebugKit()
    {
        var exe = Stock();
        exe.SetGlowSettings(exe.GetGlowSettings());
        var (on, bytes) = SaveAndReopen(exe, "plain.elf");
        Assert.That(on.GetGlowStylesState(), Is.EqualTo(GlowStylesState.On));
        Assert.That(on.GetGlowSettings(), Is.EqualTo(GlowSettings.Plain(on.GetEnchantGlow())));
        var diff = Differences(_stockBytes, bytes);
        Assert.That(diff.Length, Is.GreaterThan(0));
        Assert.That(diff.All(InGlowCode), "only FUN_00174A50's first two words and FUN_001633F8 change");
        Assert.That(BitConverter.ToUInt32(bytes, BgdaExecutable.GlowFunctionVa - VaToFile), Is.EqualTo(0x08058CFEu), "j 0x001633F8");
        Assert.That(BitConverter.ToUInt32(bytes, BgdaExecutable.GlowFunctionVa - VaToFile + 4), Is.EqualTo(0u), "nop");
        Assert.That(BitConverter.ToUInt32(bytes, BgdaExecutable.GlowCodeVa - VaToFile), Is.EqualTo(0x27BDFFD0u), "GLOW starts like the game's function");
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, BgdaExecutable.GlowMarkerVa - VaToFile, 4), Is.EqualTo("GLOW"));
        Assert.That(BitConverter.ToUInt32(bytes, BgdaExecutable.GlowMarkerVa - VaToFile + 4), Is.EqualTo(1u));
        Assert.That(bytes.Skip(BgdaExecutable.GlowStylesVa - VaToFile).Take(8), Is.EqualTo(new byte[] { 10, 10, 68, 64, 60, 60, 68, 72 }),
            "style 0: the table's colours at the game's strengths");
    }

    [Test]
    public void RemovingGlowStyles_GivesTheStockFileBack()
    {
        var exe = Stock();
        exe.SetGlowSettings(Tested());
        SetGlow(exe, "shortsword", ItemGlow.InStyle(6));
        SetGlow(exe, "warhammer", ItemGlow.Never);
        var (on, _) = SaveAndReopen(exe, "remove-on.elf");
        Assert.That(on.HasGlowStyles);
        on.RemoveGlowStyles();
        var (off, bytes) = SaveAndReopen(on, "remove-off.elf");
        Assert.That(off.GetGlowStylesState(), Is.EqualTo(GlowStylesState.Off));
        Assert.That(bytes, Is.EqualTo(_stockBytes), "the game's function, the debug kit and every record's padding byte back");
        off.RemoveGlowStyles();
        var (_, again) = SaveAndReopen(off, "remove-again.elf");
        Assert.That(again, Is.EqualTo(_stockBytes), "removing them again does nothing");
    }

    // ── styles, rules and items ──

    [Test]
    public void GlowSettings_AreWrittenWhereGlowReadsThem()
    {
        var exe = Stock();
        exe.SetGlowSettings(Tested());
        var (on, bytes) = SaveAndReopen(exe, "tested.elf");
        Assert.That(on.GetGlowSettings(), Is.EqualTo(Tested()));
        int At(int va) => va - VaToFile;
        Assert.That(bytes.Skip(At(BgdaExecutable.GlowStylesVa) + 8).Take(8), Is.EqualTo(new byte[] { 0, 70, 110, 64, 120, 220, 255, 72 }), "style 1, Frost");
        Assert.That(BitConverter.ToUInt64(bytes, At(BgdaExecutable.GlowRuleBitsVa)), Is.EqualTo(1UL << 14), "rule 1 looks for Disruption");
        Assert.That(BitConverter.ToUInt64(bytes, At(BgdaExecutable.GlowRuleBitsVa) + 8 * 7), Is.EqualTo(1UL << 5), "rule 8 for of Defence");
        Assert.That(bytes.Skip(At(BgdaExecutable.GlowRuleStylesVa)).Take(16),
            Is.EqualTo(new byte[] { 7, 1, 2, 3, 4, 8, 5, 255, 0, 0, 0, 0, 0, 0, 0, 0 }), "their styles (255 = no glow); unused ones 0");
        Assert.That(BitConverter.ToUInt64(bytes, At(BgdaExecutable.GlowRuleBitsVa) + 8 * 8), Is.EqualTo(0UL), "unused rules look for nothing");
    }

    [Test]
    public void GlowSettings_RoundTrip()
    {
        var styles = Enumerable.Range(0, 16).Select(i => Style(i, 2 * i, 255 - i, 10 + i, 100 + i, 3 * i, 7 * i, 255 - 2 * i)).ToArray();
        var rules = Enumerable.Range(0, 16).Select(i => new GlowRule(1UL << (4 + i), i % 5 == 4 ? null : (15 - i) % 16)).ToArray();
        var settings = new GlowSettings(styles, rules);
        var exe = Stock();
        exe.SetGlowSettings(settings);
        var (on, _) = SaveAndReopen(exe, "roundtrip.elf");
        Assert.That(on.GetGlowSettings(), Is.EqualTo(settings));
        Assert.That(on.GetEnchantGlow(), Is.EqualTo(new EnchantGlow(styles[0].Halo.Colour, styles[0].Core.Colour)),
            "the ENCHANT GLOW table shows style 0");

        on.SetGlowSettings(new GlowSettings(styles, rules.Take(3)));
        var (fewer, _) = SaveAndReopen(on, "roundtrip-fewer.elf");
        Assert.That(fewer.GetGlowSettings().Rules, Is.EqualTo(rules.Take(3)), "fewer rules: the others are cleared");
    }

    [Test]
    public void ItemGlowSettings_AreKeptInTheRecordsPaddingByte()
    {
        var exe = Stock();
        exe.SetGlowSettings(Tested());
        var (on, before) = SaveAndReopen(exe, "items-before.elf");
        SetGlow(on, "shortsword", ItemGlow.InStyle(6));
        SetGlow(on, "warhammer", ItemGlow.Never);
        SetGlow(on, "dagger", ItemGlow.InStyle(15));
        var (back, after) = SaveAndReopen(on, "items-after.elf");
        var diff = Differences(before, after);
        Assert.That(diff, Has.Length.EqualTo(3));
        Assert.That(diff.All(IsItemGlowByte), "only the three records' byte +0x2C");
        var items = back.GetItems();
        Assert.That(items.Single(i => i.Name == "shortsword").Glow, Is.EqualTo(ItemGlow.InStyle(6)));
        Assert.That(items.Single(i => i.Name == "warhammer").Glow.IsNever);
        Assert.That(items.Single(i => i.Name == "dagger").Glow.Style, Is.EqualTo(15));
        Assert.That(after[BgdaExecutable.ItemTableVa - VaToFile + 7 * BgdaExecutable.ItemRecordSize + BgdaExecutable.ItemGlowOffset], Is.EqualTo(255));

        // writing the items back (as the Items tab does) keeps the settings
        foreach (var item in back.GetItems()) back.SetItem(item);
        var (_, again) = SaveAndReopen(back, "items-again.elf");
        Assert.That(again, Is.EqualTo(after));
    }

    [Test]
    public void ItemGlow_Values()
    {
        Assert.That(ItemGlow.Automatic.Value, Is.EqualTo(0));
        Assert.That(ItemGlow.InStyle(0).Value, Is.EqualTo(1));
        Assert.That(ItemGlow.InStyle(15).Value, Is.EqualTo(16));
        Assert.That(ItemGlow.Never.Value, Is.EqualTo(255));
        Assert.That(new ItemGlow(17).Style, Is.EqualTo(0), "GLOW masks a style with 15");
        Assert.That(new ItemGlow(17).IsValid, Is.False, "WorldExplorer never writes 17-254");
        Assert.That(default(ItemGlow).IsAutomatic);
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemGlow.InStyle(16));
        Assert.That(ItemGlow.InStyle(4).ToString(), Is.EqualTo("Style 4"));
    }

    [Test]
    public void EnchantGlow_WithGlowStyles_IsStyleZero()
    {
        var exe = Stock();
        var settings = Tested();
        var styles = settings.Styles.ToArray();
        styles[0] = Style(10, 10, 68, 100, 60, 60, 68, 110);
        exe.SetGlowSettings(new GlowSettings(styles, settings.Rules));
        exe.SetEnchantGlow(new EnchantGlow(new Rgb(1, 2, 3), new Rgb(4, 5, 6)));
        var (on, _) = SaveAndReopen(exe, "enchant.elf");
        Assert.That(on.GetEnchantGlow(), Is.EqualTo(new EnchantGlow(new Rgb(1, 2, 3), new Rgb(4, 5, 6))));
        Assert.That(on.GetGlowSettings().Styles[0], Is.EqualTo(Style(1, 2, 3, 100, 4, 5, 6, 110)), "style 0 takes the colours, keeps its strengths");
        Assert.That(on.GetGlowSettings().Styles.Skip(1), Is.EqualTo(settings.Styles.Skip(1)), "the other styles stay");
    }

    // ── refused ──

    [Test]
    public void BadGlowSettings_AreRefusedAndChangeNothing()
    {
        var exe = Stock();
        var good = Tested();
        var bad = new List<GlowSettings>
        {
            new(good.Styles.Take(15), good.Rules),
            new(good.Styles.Select((s, i) => i == 3 ? s with { Halo = s.Halo with { Strength = 256 } } : s), good.Rules),
            new(good.Styles.Select((s, i) => i == 9 ? s with { Core = s.Core with { Colour = new Rgb(0, -1, 0) } } : s), good.Rules),
            new(good.Styles, Enumerable.Repeat(new GlowRule(1, 0), 17)),
            new(good.Styles, new[] { new GlowRule(0, 1) }),
            new(good.Styles, new[] { new GlowRule(1UL << 6, 16) })
        };
        foreach (var settings in bad)
        {
            Assert.That(settings.Problem(), Is.Not.Null);
            Assert.Throws<ArgumentException>(() => exe.SetGlowSettings(settings));
        }

        var (same, bytes) = SaveAndReopen(exe, "bad.elf");
        Assert.That(bytes, Is.EqualTo(_stockBytes), "nothing written");
        Assert.That(same.HasGlowStyles, Is.False);
    }

    [Test]
    public void ItemGlowSetting_WithoutGlowStyles_IsRefused()
    {
        var exe = Stock();
        var dagger = exe.GetItems().Single(i => i.Name == "dagger");
        dagger.Glow = ItemGlow.InStyle(3);
        var edits = new ItemEdits();
        edits.Items.Add(dagger);
        var e = Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(edits));
        Assert.That(e!.Message, Does.Contain("glow styles are off"));
        var (_, bytes) = SaveAndReopen(exe, "refused.elf");
        Assert.That(bytes, Is.EqualTo(_stockBytes));

        dagger.Glow = new ItemGlow(40);
        Assert.Throws<ArgumentException>(() => exe.SetItem(dagger));

        // with glow styles in the same edits it goes through
        var together = new ItemEdits { GlowStyles = Tested() };
        dagger.Glow = ItemGlow.InStyle(3);
        together.Items.Add(dagger);
        exe.ApplyItemEdits(together);
        Assert.That(exe.GetItems().Single(i => i.Name == "dagger").Glow, Is.EqualTo(ItemGlow.InStyle(3)));
    }

    [Test]
    public void GlowEdits_AreAllOrNothing()
    {
        var exe = Stock();
        var edits = new ItemEdits { GlowStyles = Tested() };
        var sword = exe.GetItems()[12];
        sword.DamageMax = -5;
        edits.Items.Add(sword);
        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(edits));
        Assert.That(exe.HasGlowStyles, Is.False, "the bad item stops the glow styles too");

        var both = new ItemEdits { GlowStyles = Tested(), RemoveGlowStyles = true };
        Assert.That(both.IsEmpty, Is.False);
        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(both));
        var (_, bytes) = SaveAndReopen(exe, "atomic.elf");
        Assert.That(bytes, Is.EqualTo(_stockBytes));

        var remove = new ItemEdits { RemoveGlowStyles = true };
        Assert.That(remove.IsEmpty, Is.False);
        exe.ApplyItemEdits(remove);
        Assert.That(exe.GetGlowStylesState(), Is.EqualTo(GlowStylesState.Off), "removing from an executable without them does nothing");
    }

    [Test]
    public void AnotherPatchOfTheGlowFunction_StopsGlowStyles()
    {
        void Expect(byte[] bytes, string name, string because)
        {
            var path = Path.Combine(_workDir, name);
            File.WriteAllBytes(path, bytes);
            var exe = BgdaExecutable.Open(path);
            Assert.That(exe.GetGlowStylesState(out var detail), Is.EqualTo(GlowStylesState.Unknown), name);
            Assert.That(detail, Does.Contain(because));
            Assert.That(exe.GlowStylesProblem(), Is.EqualTo(detail));
            Assert.Throws<InvalidOperationException>(() => exe.SetGlowSettings(Tested()));
            Assert.Throws<InvalidOperationException>(() => exe.GetGlowSettings());
            Assert.Throws<InvalidOperationException>(() => exe.RemoveGlowStyles());
            Assert.That(exe.GetItems(), Has.Count.EqualTo(81), "the items can still be edited");
        }

        var function = (byte[])_stockBytes.Clone();
        function[BgdaExecutable.GlowFunctionVa - VaToFile] ^= 1;
        Expect(function, "function.elf", "isn't the game's");

        var kit = (byte[])_stockBytes.Clone();
        kit[BgdaExecutable.GlowCodeVa - VaToFile + 0x100] ^= 1;
        Expect(kit, "kit.elf", "debug starting kit");

        var exe = Stock();
        exe.SetGlowSettings(Tested());
        var (_, on) = SaveAndReopen(exe, "on.elf");
        var code = (byte[])on.Clone();
        code[BgdaExecutable.GlowCodeVa - VaToFile + 40] ^= 1;
        Expect(code, "code.elf", "isn't the glow styles code");
        var mark = (byte[])on.Clone();
        mark[BgdaExecutable.GlowMarkerVa - VaToFile + 4] = 2;
        Expect(mark, "mark.elf", "version 1 mark");
    }

    // ── with the three-player patch ──

    [Test]
    public void GlowStyles_SurviveTheThreePlayerPatch()
    {
        var exe = Stock();
        exe.SetGlowSettings(Tested());
        SetGlow(exe, "shortsword", ItemGlow.InStyle(6));
        exe.EnableThreePlayers();
        var (on, _) = SaveAndReopen(exe, "glow-three.elf");
        Assert.That(on.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));
        Assert.That(on.GetGlowSettings(), Is.EqualTo(Tested()));
        Assert.That(on.GetItems().Single(i => i.Name == "shortsword").Glow, Is.EqualTo(ItemGlow.InStyle(6)));

        var added = on.GetItems()[12].AsAdded(81);
        added.Name = "longsword2";
        added.Glow = ItemGlow.InStyle(4);
        on.AddItem(added);
        var second = on.GetItems()[3].AsAdded(82);
        second.Name = "dagger2";
        second.Glow = ItemGlow.Never;
        on.AddItem(second);
        var (withAdded, _) = SaveAndReopen(on, "glow-added.elf");
        Assert.That(withAdded.GetItems()[81].Glow, Is.EqualTo(ItemGlow.InStyle(4)));
        Assert.That(withAdded.GetItems()[82].Glow.IsNever);
        withAdded.RemoveAddedItem(81);
        Assert.That(withAdded.GetItems()[81].Glow.IsNever, "the setting moves down with its item");
        withAdded.RemoveAddedItem(81);
        withAdded.DisableThreePlayers();
        var (off, _) = SaveAndReopen(withAdded, "glow-three-off.elf");
        Assert.That(off.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off));
        Assert.That(off.GetGlowSettings(), Is.EqualTo(Tested()), "glow styles stay when three players are turned off");
        off.RemoveGlowStyles();
        var (_, bytes) = SaveAndReopen(off, "glow-three-stock.elf");
        Assert.That(bytes, Is.EqualTo(_stockBytes));
    }

    [Test]
    public void ThreePlayerPatchData_TouchesNoGlowByte()
    {
        var patch = typeof(BgdaExecutable).GetField("Patch", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var value = patch.GetType().GetProperty("Value")!.GetValue(patch)!;
        var offsets = new List<(int Offset, int Length, string What)>();
        void Add(object run, string bytesName, string what)
        {
            var offset = (int)run.GetType().GetProperty("Offset")!.GetValue(run)!;
            var length = ((byte[])run.GetType().GetProperty(bytesName)!.GetValue(run)!).Length;
            offsets.Add((offset, length, what));
        }

        foreach (var run in (Array)value.GetType().GetField("Item1")!.GetValue(value)!) Add(run, "Patched", "a patch run");
        foreach (var tail in (Array)value.GetType().GetField("Item2")!.GetValue(value)!) Add(tail, "Bytes", "the added tail");
        foreach (var older in (Array)value.GetType().GetField("Item3")!.GetValue(value)!)
        {
            var name = (string)older.GetType().GetProperty("Name")!.GetValue(older)!;
            foreach (var d in (Array)older.GetType().GetProperty("Delta")!.GetValue(older)!) Add(d, "New", name);
        }

        Assert.That(offsets.Count, Is.GreaterThan(10));
        var touching = offsets.Where(o => Enumerable.Range(o.Offset, o.Length).Any(i => InGlowCode(i) ||
                                                                                       IsItemGlowByte(i) && i + VaToFile < BgdaExecutable.AddedItemsEndVa))
            .Select(o => $"{o.What} at 0x{o.Offset:X}").ToList();
        Assert.That(touching, Is.Empty, "no tested three-player build (ADRIANNA19-26) changes a byte glow styles use");
    }

    // ── what GLOW does (the emulator's results) ──

    // the cases test_glow.py ran in the emulator with the tested set: the property bits, the item's byte, the style (-1: none)
    [TestCase("1", 0, 0)]          // +2, automatic: the game's own style 0
    [TestCase("1 5", 0, -1)]       // +2 of Defence: the "no glow" rule
    [TestCase("1 5", 7, 6)]        // ... set to style 6: its own style still glows
    [TestCase("1 6", 0, 1)]        // +2 Frost
    [TestCase("1 12", 0, 1)]       // +2 Icy Burst
    [TestCase("1 7", 0, 2)]        // +2 Shock
    [TestCase("1 4", 0, 3)]        // +2 Flaming: the Fire rule makes it glow (the game wouldn't)
    [TestCase("1 8", 0, 4)]        // +2 Wounding
    [TestCase("10", 0, 5)]         // Keen alone (power 0): the Silver rule
    [TestCase("9", 0, 8)]          // of Speed
    [TestCase("14 6", 0, 7)]       // Disruption and Frost: Disruption's rule comes first
    [TestCase("17 43", 0, -1)]     // plain, no rule: no glow
    [TestCase("0 1", 7, 6)]        // +3, style 6: its own style
    [TestCase("0 1 6", 7, 6)]      // +3 Frost, style 6: its own style beats the rule
    [TestCase("17", 7, -1)]        // plain, style 6: no power and no rule
    [TestCase("10", 7, 6)]         // Keen alone, style 6: the rule matches, so it glows in its own style
    [TestCase("0 1 4", 1, 0)]      // +3 Flaming, style 0: glows in it
    [TestCase("0 2 6", 255, -1)]   // +5 Frost, never
    [TestCase("0 1 2 3", 17, 0)]   // +15, byte 17: style 16 & 15 = 0
    public void GlowFunction_DecidesAsGlowDidInTheEmulator(string bits, int itemByte, int style)
    {
        var mask = bits.Split(' ').Select(int.Parse).Aggregate(0UL, (m, b) => m | 1UL << b);
        var result = GlowFunction.Decide(Tested(), new ItemGlow((byte)itemByte), mask);
        Assert.That(result?.Style ?? -1, Is.EqualTo(style));
    }

    [Test]
    public void GlowFunction_Strengths()
    {
        // from the emulator: Bartley's test list
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.Automatic, 2), Is.EqualTo(new GlowResult(0, 41, 46, null)), "Long Sword +2");
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.Automatic, 2 | 1UL << 6), Is.EqualTo(new GlowResult(1, 43, 48, 1)), "Long Sword Frost +2");
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.Automatic, 1UL << 10), Is.EqualTo(new GlowResult(5, 38, 43, 6)), "Long Sword Keen");
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.Automatic, 15), Is.EqualTo(new GlowResult(0, 64, 72, null)), "Long Sword +15");
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.Automatic, 1 | 1UL << 6)?.HaloAlpha, Is.EqualTo(52), "Bastard Sword Frost +1: power 9");
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.InStyle(6), 2 | 1UL << 6), Is.EqualTo(new GlowResult(6, 43, 48, 1)), "Short Sword Frost +2");
        Assert.That(GlowFunction.Decide(Tested(), ItemGlow.Never, 3), Is.Null, "Warhammer +3");
        Assert.That(GlowFunction.GameDecide(2), Is.EqualTo(new GlowResult(0, 41, 46, null)));
        Assert.That(GlowFunction.GameDecide(2 | 1UL << 4), Is.Null, "the game: no glow for Flaming");
        Assert.That(GlowFunction.GameDecide(1UL << 10), Is.Null, "the game: none for Keen alone");
        Assert.That(GlowFunction.Factor(0), Is.EqualTo(0.6f));
        Assert.That(GlowFunction.Factor(40), Is.EqualTo(1f));
    }
}
