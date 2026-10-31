using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Adding entries to archives, archives to GOBs and objects to levels, checked
/// against Test_files/TAVERN.GOB and TEST.GOB (skipped when they are not
/// present). When Test_files/TMAN2.LMP is there too, the bar patrons it holds
/// are added to the tavern.
/// </summary>
[TestFixture]
public class ArchiveImportTests
{
    private const int Sector = 2048;

    private string _testFiles = "";

    [OneTimeSetUp]
    public void FindTestFiles()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "TAVERN.GOB")))
        {
            dir = dir.Parent;
        }

        if (dir == null || !File.Exists(Path.Combine(dir.FullName, "Test_files", "TEST.GOB")))
        {
            Assert.Ignore("Test_files/TAVERN.GOB and TEST.GOB were not found above the test directory.");
        }

        _testFiles = Path.Combine(dir!.FullName, "Test_files");
    }

    private string PathOf(string name) => Path.Combine(_testFiles, name);

    private GobFile Open(string name) => new(EngineVersion.DarkAlliance, PathOf(name));

    private static GobFile Saved(GobFile gob) =>
        new(EngineVersion.DarkAlliance, "saved.gob", GobWriter.TryPatchInPlace(gob) ?? GobWriter.Pack(gob));

    private static LmpFile Archive(GobFile gob, string name)
    {
        var lmp = gob.Directory[name];
        lmp.EnsureDirectory();
        return lmp;
    }

    private static int ArchiveOffset(byte[] gob, int index) => BitConverter.ToInt32(gob, index * 0x28 + 0x20);

    [Test]
    public void GameArchives_AreInTheGamesNameOrder()
    {
        var checkedArchives = 0;
        foreach (var gobName in new[] { "TAVERN.GOB", "TEST.GOB", "CUTTOWN.GOB" })
        {
            if (!File.Exists(PathOf(gobName))) continue;
            foreach (var (name, lmp) in Open(gobName).Directory)
            {
                lmp.ReadDirectory();
                // The shop screen's pictures (ntsc_back.tex, pal_back.tex) sort without their region prefix, as the game reads them.
                Assert.That(lmp.IsDirectorySorted(), Is.True, $"{gobName}/{name}");
                checkedArchives++;
            }
        }

        Assert.That(checkedArchives, Is.GreaterThanOrEqualTo(18));
    }

    [Test]
    public void TheShopPictures_KeepTheirPlace_WhenAnEntryIsAdded()
    {
        var gob = Open("TAVERN.GOB");
        var shop = Archive(gob, "shop.lmp");
        var stock = shop.Directory.Keys.ToList();

        shop.AddEntry("bag.tex", new byte[] { 1 });
        shop.AddEntry("aaa.tex", new byte[] { 2 });
        var saved = Archive(Saved(gob), "shop.lmp");

        var expected = stock.ToList();
        expected.Insert(0, "aaa.tex");
        expected.Insert(expected.IndexOf("backpack.tex") + 1, "bag.tex");
        Assert.That(saved.Directory.Keys, Is.EqualTo(expected), "where the game's search looks for them");
        Assert.That(saved.IsDirectorySorted(), Is.True);
        foreach (var name in stock)
        {
            Assert.That(saved.GetEntryBytes(name), Is.EqualTo(shop.GetEntryBytes(name)), name);
        }
    }

    [Test]
    public void AddedEntries_GoWhereTheirNamesSort()
    {
        var gob = Open("TAVERN.GOB");
        var uncle = Archive(gob, "uncle.lmp");
        var stock = uncle.Directory.Keys.ToDictionary(n => n, n => uncle.GetEntryBytes(n)!);

        uncle.AddEntry("zzz.bin", new byte[] { 5, 6 });
        uncle.AddEntry("aaa.bin", new byte[] { 1, 2, 3 });
        uncle.AddEntry("mmm.bin", new byte[] { 4 });
        Assert.That(uncle.EffectiveEntryNames(), Is.EqualTo(new[]
        {
            "aaa.bin", "iamjuan.anm", "lowuncle.tex", "lowuncle.vif", "mmm.bin", "uncle.tex", "uncle.vif", "zzz.bin"
        }));
        Assert.That(GobWriter.TryPatchInPlace(gob), Is.Null, "new entries move the others");

        var saved = Saved(gob);
        var savedUncle = Archive(saved, "uncle.lmp");
        Assert.That(savedUncle.Directory.Keys, Is.EqualTo(uncle.EffectiveEntryNames()));
        Assert.That(savedUncle.IsDirectorySorted(), Is.True, "the game finds entries by binary search");
        Assert.That(savedUncle.GetEntryBytes("aaa.bin"), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(savedUncle.GetEntryBytes("mmm.bin"), Is.EqualTo(new byte[] { 4 }));
        foreach (var (name, bytes) in stock)
        {
            Assert.That(savedUncle.GetEntryBytes(name), Is.EqualTo(bytes), name);
        }

        foreach (var name in gob.Directory.Keys.Where(n => n != "uncle.lmp"))
        {
            Assert.That(saved.Directory[name].GetRawData(), Is.EqualTo(gob.Directory[name].GetRawData()), name);
        }
    }

    [Test]
    public void AnArchiveOutOfNameOrder_KeepsItsOrder()
    {
        var stock = new[] { "zeta.tex", "alpha.tex", "mid.tex" };
        var bytes = LmpWriter.PackEntries(EngineVersion.DarkAlliance, stock.Select((n, i) => (n, new byte[] { (byte)i, 9 })).ToList());
        var lmp = new LmpFile(EngineVersion.DarkAlliance, "odd.lmp", bytes, 0, bytes.Length);
        lmp.ReadDirectory();
        Assert.That(lmp.IsDirectorySorted(), Is.False);

        lmp.AddEntry("aaa.tex", new byte[] { 1 });
        var packed = LmpWriter.Pack(lmp);
        var saved = new LmpFile(EngineVersion.DarkAlliance, "odd.lmp", packed, 0, packed.Length);
        saved.ReadDirectory();

        Assert.That(saved.Directory.Keys, Is.EqualTo(stock.Append("aaa.tex")), "new entries go last");
        foreach (var name in stock)
        {
            Assert.That(saved.GetEntryBytes(name), Is.EqualTo(lmp.GetEntryBytes(name)), name);
        }
    }

    [Test]
    public void AddedEntry_CanBeReplacedAndTakenBack()
    {
        var gob = Open("TEST.GOB");
        var lmp = gob.Directory["test.lmp"]; // its directory hasn't been read

        lmp.AddEntry("new.bin", new byte[] { 1 });
        Assert.That(lmp.Directory, Is.Not.Empty, "adding reads the directory first, so no entry is lost");
        Assert.That(lmp.IsAdded("new.bin"), Is.True);
        Assert.That(lmp.IsAdded("objects.ob"), Is.False);

        lmp.ReplaceEntry("new.bin", new byte[] { 2, 3 });
        Assert.That(lmp.GetEntryBytes("new.bin"), Is.EqualTo(new byte[] { 2, 3 }));
        Assert.That(lmp.PendingEdits, Is.Empty, "replacing a new entry changes the addition");

        lmp.DeleteEntry("new.bin");
        Assert.That(lmp.IsDirty, Is.False);
        Assert.That(lmp.GetEntryBytes("new.bin"), Is.Null);
        Assert.That(GobWriter.TryPatchInPlace(gob), Is.EqualTo(File.ReadAllBytes(PathOf("TEST.GOB"))));

        // A name differing only in case replaces the entry: the archive never holds both.
        Assert.That(lmp.HasEntry("OBJECTS.OB"), Is.True);
        Assert.That(lmp.AddedEntryName("OBJECTS.OB"), Is.EqualTo("objects.ob"));
        lmp.AddEntry("OBJECTS.OB", new byte[] { 4 });
        Assert.That(lmp.PendingAdditions, Is.Empty);
        Assert.That(lmp.EffectiveEntryNames(), Is.EqualTo(new[] { "dialog.bin", "objects.ob", "script.scr", "test.world" }));
        Assert.That(lmp.GetEntryBytes("objects.ob"), Is.EqualTo(new byte[] { 4 }));

        lmp.AddEntry("New.bin", new byte[] { 5 });
        lmp.AddEntry("NEW.BIN", new byte[] { 6 });
        Assert.That(lmp.PendingAdditions.Select(a => a.Name), Is.EqualTo(new[] { "New.bin" }), "keeps the name it was added as");
        Assert.That(lmp.AddedEntryName("new.bin"), Is.EqualTo("New.bin"));
        Assert.That(lmp.GetEntryBytes("New.bin"), Is.EqualTo(new byte[] { 6 }));

        Assert.That(LmpFile.CheckEntryName(new string('a', LmpFile.MaxEntryNameLength)), Is.Null);
        Assert.That(LmpFile.CheckEntryName(new string('a', LmpFile.MaxEntryNameLength + 1)), Does.Contain("55"));
        Assert.That(LmpFile.CheckEntryName("dir/file.tex"), Is.Not.Null);
        Assert.That(LmpFile.CheckEntryName(" "), Is.Not.Null);
    }

    [Test]
    public void AddedArchive_IsSavedWithTheArchivesObjectsUse()
    {
        var uncleBytes = Open("TAVERN.GOB").Directory["uncle.lmp"].GetRawData();
        var original = File.ReadAllBytes(PathOf("TEST.GOB"));
        var gob = Open("TEST.GOB");

        var added = gob.AddArchive(" UNCLE.LMP", uncleBytes);
        Assert.That(added.Name, Is.EqualTo("uncle.lmp"), "named in lower case, as the game looks it up");
        Assert.That(added.Directory, Has.Count.EqualTo(5));
        Assert.That(gob.AddedArchives, Is.EqualTo(new[] { "uncle.lmp" }));
        Assert.That(gob.Directory.Keys,
            Is.EqualTo(new[] { "crystal.lmp", "uncle.lmp", "env.lmp", "waterenv.lmp", "test.lmp" }),
            "before env.lmp, like the archives the game's GOBs hold for their objects");
        Assert.That(gob.IsDirty, Is.True);
        Assert.That(GobWriter.TryPatchInPlace(gob), Is.Null);

        var packed = GobWriter.Pack(gob);
        var saved = new GobFile(EngineVersion.DarkAlliance, "saved.gob", packed);
        var stock = new GobFile(EngineVersion.DarkAlliance, "stock.gob", original);
        Assert.That(saved.Directory.Keys, Is.EqualTo(gob.Directory.Keys));
        Assert.That(saved.Directory["uncle.lmp"].GetRawData(), Is.EqualTo(uncleBytes));
        foreach (var name in stock.Directory.Keys)
        {
            Assert.That(saved.Directory[name].GetRawData(), Is.EqualTo(stock.Directory[name].GetRawData()), name);
        }

        Assert.That(ArchiveOffset(packed, 0), Is.EqualTo(Sector), "the directory is one sector");
        for (var i = 0; i < saved.Directory.Count; i++)
        {
            Assert.That(ArchiveOffset(packed, i) % Sector, Is.Zero, "every archive starts a sector");
        }

        gob.RemoveAddedArchive("uncle.lmp");
        Assert.That(gob.IsDirty, Is.False);
        Assert.That(gob.Directory.Keys, Is.EqualTo(stock.Directory.Keys));
        Assert.That(GobWriter.TryPatchInPlace(gob), Is.EqualTo(original));
    }

    [Test]
    public void ArchivesThatCantBeAdded_AreRefused()
    {
        var gob = Open("TEST.GOB");
        var crystal = gob.Directory["crystal.lmp"].GetRawData();

        Assert.That(gob.CheckNewArchiveName("CRYSTAL.LMP"), Does.Contain("already has"));
        Assert.That(gob.CheckNewArchiveName("crystal2"), Does.Contain(".lmp"));
        Assert.That(gob.CheckNewArchiveName(new string('a', 28) + ".lmp"), Does.Contain("31"));
        Assert.That(gob.CheckNewArchiveName("my file.lmp"), Is.Not.Null);
        Assert.That(gob.CheckNewArchiveName("tman2.lmp"), Is.Null);
        Assert.Throws<ArgumentException>(() => gob.AddArchive("crystal.lmp", crystal));
        Assert.Throws<InvalidDataException>(() => gob.AddArchive("junk.lmp", new byte[] { 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => gob.AddArchive("junk.lmp", BitConverter.GetBytes(5000)));
        Assert.Throws<ArgumentException>(() => gob.RemoveAddedArchive("crystal.lmp"));
        Assert.That(gob.IsDirty, Is.False, "nothing was added");

        // The game reads the directory as one sector: 51 archives fit.
        for (var i = gob.Directory.Count; i < GobFile.MaxArchives; i++) gob.AddArchive($"copy{i}.lmp", crystal);
        Assert.That(gob.CheckNewArchiveName("onemore.lmp"), Does.Contain("51"));
        Assert.Throws<ArgumentException>(() => gob.AddArchive("onemore.lmp", crystal));

        var packed = GobWriter.Pack(gob);
        Assert.That(ArchiveOffset(packed, 0), Is.EqualTo(Sector), "51 archives still fit the first sector");
        Assert.That(new GobFile(EngineVersion.DarkAlliance, "full.gob", packed).Directory,
            Has.Count.EqualTo(GobFile.MaxArchives));
    }

    [TestCase(0, 0x0000)]
    [TestCase(22.5, 0x1000)]
    [TestCase(90, 0x4000)]
    [TestCase(100, 0x4000)]
    [TestCase(270, 0xC000)]
    [TestCase(-90, 0xC000)]
    [TestCase(337.5, 0xF000)]
    [TestCase(360, 0x0000)]
    [TestCase(720 + 45, 0x2000)]
    public void Facing_IsOneOfSixteenDirections(double degrees, int i6)
    {
        Assert.That((ushort)LevelObjects.FacingToI6(degrees), Is.EqualTo(i6));
        Assert.That(LevelObjects.FacingToI6(LevelObjects.FacingOf((short)i6)), Is.EqualTo((short)i6));
    }

    [Test]
    public void NewObjects_AreSavedInTheLevelsObjectList()
    {
        var gob = Open("TAVERN.GOB");
        var tavern = Archive(gob, "tavern.lmp");
        var stock = tavern.GetEntryBytes("objects.ob")!;
        var objects = ObDecoder.Decode(stock, 0, stock.Length);
        var names = objects.Select(o => o.Name).ToList();

        var uncle = Archive(gob, "uncle.lmp");
        Assert.That(LevelObjects.CheckModelProp(uncle, "uncle.vif", "uncle.tex", "iamjuan.anm"), Is.Null);
        Assert.That(LevelObjects.CheckModelProp(uncle, "Uncle.vif", "uncle.tex", "iamjuan.anm"),
            Does.Contain("no Uncle.vif"), "names are compared exactly");
        Assert.That(LevelObjects.CheckModelProp(uncle, "uncle.tex", "uncle.tex", "iamjuan.anm"), Does.Contain(".vif"));
        Assert.That(LevelObjects.CheckModelProp(uncle, "uncle.vif", "uncle.tex", ""), Does.Contain(".anm"));

        objects.Add(LevelObjects.Create(LevelObjects.ModelPropClass, 100, 120.5f, 0, 270,
            LevelObjects.ModelPropProperties("uncle.lmp", "uncle.vif", "uncle.tex", "iamjuan.anm", noCollide: true)));
        objects.Add(LevelObjects.Create("Chest", -50, 60, 0, 45, Array.Empty<string>()));
        tavern.ReplaceEntry("objects.ob", ObEncoder.Encode(objects, BitConverter.ToInt16(stock, 2)));

        var savedBytes = Archive(Saved(gob), "tavern.lmp").GetEntryBytes("objects.ob")!;
        var saved = ObDecoder.Decode(savedBytes, 0, savedBytes.Length);
        Assert.That(saved.Select(o => o.Name), Is.EqualTo(names.Concat(new[] { "UserParamProp", "Chest" })));

        var prop = saved[^2];
        Assert.That(prop.Properties, Is.EqualTo(new[]
        {
            "noCollide=1", "lump=uncle.lmp", "model=uncle.vif", "texture=uncle.tex", "anim=iamjuan.anm"
        }));
        Assert.That(prop.Floats, Is.EqualTo(new[] { 100f, 120.5f, 0f }));
        Assert.That((ushort)prop.I6, Is.EqualTo(0xC000));
        Assert.That((ushort)saved[^1].I6, Is.EqualTo(0x2000));
        Assert.That(saved[^1].Properties, Is.Empty);
    }

    [Test]
    public void BadObjects_AreRefused()
    {
        Assert.Throws<ArgumentException>(() => LevelObjects.Create("", 0, 0, 0, 0, Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => LevelObjects.Create("Two Words", 0, 0, 0, 0, Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => LevelObjects.Create("Chest", 0, 0, 0, 0, new[] { "noValue" }));
        Assert.Throws<ArgumentException>(() => LevelObjects.Create("Chest", 0, 0, 0, 0, new[] { "=value" }));
        Assert.Throws<ArgumentException>(() => LevelObjects.Create("Chest", 0, 0, 0, 0, new[] { "name=café" }));
        Assert.Throws<ArgumentException>(() => LevelObjects.Create("Chest", float.NaN, 0, 0, 0, Array.Empty<string>()));

        Assert.That(LevelObjects.ClassesUsing("watchmen.lmp").Select(c => c.Name),
            Is.EqualTo(new[] { "NPCGuard1", "NPCGuard2", "Guard" }));
        Assert.That(LevelObjects.ClassesUsing("CHEST.LMP").Select(c => c.Name), Is.EqualTo(new[] { "Chest" }));
        Assert.That(LevelObjects.ClassesUsing("tman2.lmp"), Is.Empty, "no class loads the bar patrons' archive");
    }

    [Test]
    public void BarPatrons_CanBeAddedToTheTavern()
    {
        if (!File.Exists(PathOf("TMAN2.LMP"))) Assert.Ignore("Test_files/TMAN2.LMP was not found.");

        var gob = Open("TAVERN.GOB");
        var tman2 = gob.AddArchive("TMAN2.LMP", File.ReadAllBytes(PathOf("TMAN2.LMP")));
        var order = gob.Directory.Keys.ToList();
        Assert.That(order.IndexOf("tman2.lmp"), Is.EqualTo(order.IndexOf("env.lmp") - 1));
        Assert.That(LevelObjects.CheckModelProp(tman2, "barpatron.vif", "bp_costume3.tex", "bp_drinking1.anm"), Is.Null);

        var tavern = Archive(gob, "tavern.lmp");
        var stock = tavern.GetEntryBytes("objects.ob")!;
        var objects = ObDecoder.Decode(stock, 0, stock.Length);
        objects.Add(LevelObjects.Create(LevelObjects.ModelPropClass, -120, 150, 0, 90,
            LevelObjects.ModelPropProperties("tman2.lmp", "barpatron.vif", "bp_costume3.tex", "bp_drinking1.anm", false)));
        tavern.ReplaceEntry("objects.ob", ObEncoder.Encode(objects, BitConverter.ToInt16(stock, 2)));

        var saved = Saved(gob);
        Assert.That(saved.Directory.Keys, Is.EqualTo(order));
        Assert.That(saved.Directory["tman2.lmp"].GetRawData(), Is.EqualTo(File.ReadAllBytes(PathOf("TMAN2.LMP"))));
        var savedObjects = Archive(saved, "tavern.lmp").GetEntryBytes("objects.ob")!;
        Assert.That(ObDecoder.Decode(savedObjects, 0, savedObjects.Length)[^1].GetProperty("lump"), Is.EqualTo("tman2.lmp"));
    }
}
