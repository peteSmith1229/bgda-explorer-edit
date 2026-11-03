using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Pnach;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The Dark Alliance II three-player mod as a PCSX2 cheat file: build 51 is embedded and must
/// come out byte for byte with the default settings; edited settings change only their own
/// words and read back as they were written. Needs no game files.
/// </summary>
[TestFixture]
public class Da2ThreePlayerModTests
{
    private const string Tuning = "WorldExplorer - executable tuning";

    private static PnachGroup Group(PnachFile file, string name) =>
        file.Find(name) ?? throw new AssertionException($"[{name}] is missing.");

    [Test]
    public void Pnach_ParseAndToText_GivesBackTheSameText()
    {
        var text = Da2ThreePlayerMod.StockText;
        Assert.That(PnachFile.Parse(text).ToText(), Is.EqualTo(text));

        const string crlf = "gametitle=x\r\n\r\n[a]\r\npatch=0,EE,00100000,word,00000001\r\n[b]\r\nauthor=me";
        var file = PnachFile.Parse(crlf);
        Assert.That(file.ToText(), Is.EqualTo(crlf));
        Assert.That(file.Groups.Select(g => g.Name), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(file.Groups[0].Patches.Single(), Is.EqualTo(PnachPatch.Word(0x100000, 1)));
        Assert.That(file.Groups[1].Author, Is.EqualTo("me"));
    }

    [Test]
    public void DefaultSettings_WriteBuild51ByteForByte()
    {
        var written = Da2ThreePlayerMod.Write(Da2ThreePlayerMod.DefaultSettings()).ToText();
        Assert.That(written, Is.EqualTo(Da2ThreePlayerMod.StockText));
        Assert.That(written.Length, Is.EqualTo(807_681), "build 51 as deployed is 807,681 bytes");
    }

    [Test]
    public void Build51_IsRecognisedAndReadsBackAsTheDefaults()
    {
        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(Da2ThreePlayerMod.StockText), Tuning);

        Assert.That(read.MainState, Is.EqualTo(Da2ModMainState.Current));
        Assert.That(read.MainBuilds, Is.EqualTo(new[] { 51 }));
        Assert.That(read.OtherGroups, Is.Empty);
        Assert.That(read.Notes, Is.Empty);
        Assert.That(read.Tuning, Is.Null);
        Assert.That(Da2ThreePlayerMod.Write(read.Settings).ToText(), Is.EqualTo(Da2ThreePlayerMod.StockText));

        var s = read.Settings;
        Assert.That(s.HudSizes, Is.EqualTo(new[] { 100, 85, 60, 50 }));
        Assert.That(s.FeatPoints, Is.EqualTo(200));
        Assert.That(s.Gold, Is.EqualTo(500000));
        Assert.That(s.Shops.Counts, Is.EqualTo(new[] { 20, 20, 10 }));
        Assert.That(s.Shops.ExtremeFloor, Is.EqualTo(new[] { 5000f, 3000f, 1000f }));
        Assert.That(s.Shops.Low[0], Is.EqualTo(10f));
        Assert.That(s.Shops.High[11], Is.EqualTo(200000f));
        Assert.That(s.FeatPrices.Count, Is.EqualTo(58), "H's list prices 58 feats");
        Assert.That(s.FeatPrices[32], Is.EqualTo(new[] { 2, 4, 6, 8, 10 }), "Power Attack");
        Assert.That(s.FeatPrices[44], Is.EqualTo(new[] { 30 }), "Hero's Arm");
        Assert.That(s.FeatPrices[85], Is.EqualTo(new[] { 12, 22, 35 }), "Animate Dead (3 ranks)");
    }

    [Test]
    public void HudSizes_AnySizeFrom25To100_GetsItsOwnGroup()
    {
        var s = Da2ThreePlayerMod.DefaultSettings();
        s.HudSizes = new List<int> { 100, 75, 50, 25 };
        var file = Da2ThreePlayerMod.Write(s);

        Assert.That(Group(file, "HUD size 75%").BootWords()[Da2ThreePlayerMod.HudOption], Is.EqualTo(0x485300C0u));
        Assert.That(Group(file, "HUD size 25%").BootWords()[Da2ThreePlayerMod.HudOption], Is.EqualTo(0x48530040u));
        Assert.That(file.Find("HUD size 85%"), Is.Null);
        Assert.That(Group(file, "HUD size 50%").Lines, Is.EqualTo(Group(PnachFile.Parse(Da2ThreePlayerMod.StockText), "HUD size 50%").Lines));

        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(file.ToText()), Tuning);
        Assert.That(read.Settings.HudSizes, Is.EqualTo(new[] { 100, 75, 50, 25 }));

        for (var pct = 25; pct <= 100; pct++)
        {
            var word = Da2ThreePlayerMod.HudWord(pct) & 0xFFFF;
            Assert.That((int)Math.Round(word * 100 / 256.0, MidpointRounding.AwayFromZero), Is.EqualTo(pct));
            Assert.That(word, Is.InRange(64u, 256u), "the HUD code scales only 64-255 (256 = full size)");
        }
    }

    [Test]
    public void TestAmounts_ChangeTheGroupNameAndItsWords()
    {
        var s = Da2ThreePlayerMod.DefaultSettings();
        s.FeatPoints = 999;
        s.Gold = 1_234_567;
        var file = Da2ThreePlayerMod.Write(s);

        var points = Group(file, "TEST - all players always have 999 feat points");
        Assert.That(points.Patches.Select(p => (p.Place, p.Address, p.Value)),
            Is.EqualTo(new[] { (1, 0xB00E0u, 999u), (1, 0xB6410u, 999u), (1, 0xBC740u, 999u) }));
        var gold = Group(file, "TEST - party always has 1234567 gold");
        Assert.That(gold.Patches.Single(), Is.EqualTo(PnachPatch.EveryFrame(0xB00DC, 1_234_567)));

        var read = Da2ThreePlayerMod.Read(file, Tuning);
        Assert.That(read.Settings.FeatPoints, Is.EqualTo(999));
        Assert.That(read.Settings.Gold, Is.EqualTo(1_234_567));
    }

    [Test]
    public void Shops_EditedSettings_ChangeOnlyTheDataWords()
    {
        var stock = Group(PnachFile.Parse(Da2ThreePlayerMod.StockText), Da2ThreePlayerMod.ShopsGroup);
        var s = Da2ThreePlayerMod.DefaultSettings();
        s.Shops.Counts[0] = 30;
        s.Shops.High[3] = 150000f;   // act 2, weapons
        s.Shops.ExtremeFloor[2] = 2500f;
        var group = Group(Da2ThreePlayerMod.Write(s), Da2ThreePlayerMod.ShopsGroup);

        var before = stock.BootWords();
        var after = group.BootWords();
        Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
        var changed = after.Where(p => before[p.Key] != p.Value).Select(p => p.Key).OrderBy(a => a).ToList();
        Assert.That(changed, Is.EqualTo(new[] { 0xAF01Cu, 0xAF060u, 0xAF074u }));
        Assert.That(after[0xAF060], Is.EqualTo(30u));
        Assert.That(BitConverter.Int32BitsToSingle((int)after[0xAF01C]), Is.EqualTo(150000f));
        Assert.That(group.Description, Does.Contain("the weapons tab is 30 random weapons").And.Contain("2500 (other)")
            .And.EndsWith("edited in WorldExplorer."));

        var read = Da2ThreePlayerMod.Read(Da2ThreePlayerMod.Write(s), Tuning);
        Assert.That(read.Settings.Shops.SameAs(s.Shops), Is.True);
    }

    [Test]
    public void ShopsDescription_OfTheStockSettings_IsBuild51s()
    {
        var stock = Group(PnachFile.Parse(Da2ThreePlayerMod.StockText), Da2ThreePlayerMod.ShopsGroup);
        Assert.That(Da2ThreePlayerMod.ShopsDescription(Da2ThreePlayerMod.StockShops()),
            Is.EqualTo("description=" + stock.Description));
    }

    [Test]
    public void FeatPrices_TableIsRebuiltAndTheCodeStaysPut()
    {
        var stockFile = PnachFile.Parse(Da2ThreePlayerMod.StockText);
        var stock = Group(stockFile, Da2ThreePlayerMod.FeatsGroup).BootWords();
        var s = Da2ThreePlayerMod.DefaultSettings();
        s.FeatPrices[32] = new[] { 1, 1, 1, 1, 1 };   // Power Attack
        s.FeatPrices.Remove(117);                      // Turn Undead goes back to the game's own prices
        s.FeatPrices[16] = new[] { 3, 3 };             // Dodge, new in the table
        s.FeatPrices[33] = new[] { 20 };               // Lightning Bolt, new
        var file = Da2ThreePlayerMod.Write(s);
        var words = Group(file, Da2ThreePlayerMod.FeatsGroup).BootWords();

        const uint table = 0xE05FC;
        foreach (var (address, value) in stock.Where(p => p.Key < table || p.Key >= 0xE1000))
            Assert.That(words[address], Is.EqualTo(value), $"word at 0x{address:X6} must not change");

        var rows = new List<(uint Id, uint[] Prices)>();
        for (var at = table; words[at] != 0; at += 24)
            rows.Add((words[at], Enumerable.Range(1, 5).Select(i => words[at + (uint)(4 * i)]).ToArray()));
        Assert.That(rows.Select(r => r.Id), Is.Ordered.And.Unique);
        Assert.That(rows.Count, Is.EqualTo(59));
        Assert.That(rows.Single(r => r.Id == 32).Prices, Is.EqualTo(new uint[] { 1, 1, 1, 1, 1 }));
        Assert.That(rows.Single(r => r.Id == 16).Prices, Is.EqualTo(new uint[] { 3, 3, 3, 3, 3 }));
        Assert.That(rows.Any(r => r.Id == 117), Is.False);
        Assert.That(words.Keys.Where(a => a >= table && a < 0xE1000).Max() % 16, Is.EqualTo(12u),
            "the table's words end on a 16-byte boundary");

        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(file.ToText()), Tuning);
        Assert.That(read.Settings.FeatPrices[16], Is.EqualTo(new[] { 3 }));
        Assert.That(read.Settings.FeatPrices[33], Is.EqualTo(new[] { 20 }));
        Assert.That(read.Settings.FeatPrices.ContainsKey(117), Is.False);
        Assert.That(read.Settings.FeatPrices.Count, Is.EqualTo(59));
    }

    [Test]
    public void OtherGroupsAndTheTuningGroup_AreKept()
    {
        var file = PnachFile.Parse(Da2ThreePlayerMod.StockText);
        file.Groups.Insert(3, new PnachGroup("My own cheat", new[] { "author=me", "patch=1,EE,00400000,word,00000001", "" }));
        file.Groups.Add(new PnachGroup(Tuning, new[] { "patch=0,EE,00313924,word,00000384" }));
        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(file.ToText()), Tuning);

        Assert.That(read.OtherGroups.Select(g => g.Name), Is.EqualTo(new[] { "My own cheat" }));
        Assert.That(read.Tuning?.Patches.Single().Value, Is.EqualTo(900u));

        var written = Da2ThreePlayerMod.Write(read.Settings, read.Tuning, read.OtherGroups);
        Assert.That(written.Groups.Select(g => g.Name).TakeLast(2), Is.EqualTo(new[] { Tuning, "My own cheat" }));
        Assert.That(written.ToText(), Does.StartWith(Da2ThreePlayerMod.StockText.TrimEnd('\n')));
        Assert.That(written.ToText(), Does.EndWith("[My own cheat]\nauthor=me\npatch=1,EE,00400000,word,00000001\n"));
    }

    [Test]
    public void GroupsOfOlderBuilds_AreMapped()
    {
        var stock = PnachFile.Parse(Da2ThreePlayerMod.StockText);
        var file = new PnachFile(stock.Header, stock.Groups.Select(g => g.Clone()));
        Group(file, Da2ThreePlayerMod.FeatsGroup).Name = "Feats - new rank costs, Vahn + Borador extras";
        file.Groups.RemoveAll(g => g.Name.StartsWith("TEST - party", StringComparison.Ordinal));
        file.Groups.Add(new PnachGroup("TEST - party always has 50000 gold", new[] { "patch=1,EE,000B00DC,word,0000C350" }));
        file.Groups.Add(new PnachGroup("DA1 heroes - shouts from the disc", new[] { "patch=0,EE,000A1FA4,word,53440001" }));

        var read = Da2ThreePlayerMod.Read(file, Tuning);
        Assert.That(read.Settings.IncludeFeatPrices, Is.True);
        Assert.That(read.Settings.FeatPrices.Count, Is.EqualTo(58));
        Assert.That(read.Settings.Gold, Is.EqualTo(50000));
        Assert.That(read.OtherGroups, Is.Empty);
        Assert.That(read.Notes, Has.Count.EqualTo(2));
    }

    [Test]
    public void AChangedMainGroup_IsReportedAsUnknown()
    {
        var file = PnachFile.Parse(Da2ThreePlayerMod.StockText);
        var main = Group(file, Da2ThreePlayerMod.MainGroup);
        var i = main.Lines.FindIndex(l => l.StartsWith("patch=", StringComparison.Ordinal));
        main.Lines[i] = PnachPatch.Word(0xA0000, 0).ToLine();

        var read = Da2ThreePlayerMod.Read(file, Tuning);
        Assert.That(read.MainState, Is.EqualTo(Da2ModMainState.Unknown));
        Assert.That(read.MainBuilds, Is.Empty);
    }

    [Test]
    public void BadSettings_AreRejected()
    {
        void Bad(Action<Da2ModSettings> change)
        {
            var s = Da2ThreePlayerMod.DefaultSettings();
            change(s);
            Assert.Throws<ArgumentException>(() => Da2ThreePlayerMod.Write(s));
        }

        Bad(s => s.HudSizes.Add(20));
        Bad(s => s.HudSizes.Add(100));
        Bad(s => s.FeatPoints = 0);
        Bad(s => s.Gold = -5);
        Bad(s => s.Shops.Low[4] = s.Shops.High[4] + 1);
        Bad(s => s.Shops.Counts[2] = 65);
        Bad(s => s.FeatPrices[32] = new[] { 1, 2, 3, 4, 5, 6 });
        Bad(s => s.FeatPrices[32] = new[] { 0 });
        Bad(s => s.FeatPrices[42] = new[] { 256 });
    }

    /// <summary>
    /// When the workspace's builds are copied to Test_files/Da2Builds (da2_3p_build16.pnach ...), every
    /// one of them must be recognised as its own build. Skipped otherwise.
    /// </summary>
    [Test]
    public void OlderBuilds_AreRecognised()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Test_files", "Da2Builds"))) dir = dir.Parent;
        if (dir == null) Assert.Ignore("Test_files/Da2Builds was not found above the test directory.");

        var files = Directory.GetFiles(Path.Combine(dir!.FullName, "Test_files", "Da2Builds"), "da2_3p_build*.pnach");
        Assert.That(files, Is.Not.Empty);
        foreach (var path in files)
        {
            var build = int.Parse(Path.GetFileNameWithoutExtension(path).Substring("da2_3p_build".Length));
            var read = Da2ThreePlayerMod.Read(PnachFile.Load(path), Tuning);
            Assert.That(read.MainBuilds, Does.Contain(build), path);
            Assert.That(read.MainState, Is.EqualTo(build == 51 ? Da2ModMainState.Current : Da2ModMainState.Older), path);
            Assert.That(read.OtherGroups, Is.Empty, $"{path}: every group of a build must be known");
            Assert.DoesNotThrow(() => Da2ThreePlayerMod.Write(read.Settings), path);
        }
    }
}
