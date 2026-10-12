using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The Dark Alliance level budget (object pools and memory) on the GOBs in Test_files
/// (skipped when they are not present). The expected numbers come from a separate Python
/// implementation and from save states of the game (tools/tests in the three-player tools).
/// </summary>
[TestFixture]
public class BgdaLevelBudgetTests
{
    private string _dir = "";

    [OneTimeSetUp]
    public void FindTestFiles()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "TAVERN.GOB")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Ignore("Test_files/TAVERN.GOB was not found above the test directory.");
        }

        _dir = Path.Combine(dir!.FullName, "Test_files");
    }

    private GobFile Gob(string name) => new(EngineVersion.DarkAlliance, Path.Combine(_dir, name));

    [Test]
    public void ClassTable_MatchesTheGame()
    {
        Assert.That(BgdaObjectClasses.All.Count, Is.EqualTo(186));
        var gnoll = BgdaObjectClasses.Find("Gnoll")!;
        Assert.That(gnoll.Size, Is.EqualTo(672));
        Assert.That(gnoll.Archive, Is.EqualTo("gnoll.lmp"));
        Assert.That(BgdaObjectClasses.Find("gnoll"), Is.Null, "the game compares class names exactly");
        Assert.That(BgdaLevelBudget.PoolFor(BgdaObjectClasses.Find("Beholder")!.Size), Is.EqualTo(BgdaPool.Large));
        Assert.That(BgdaLevelBudget.PoolFor(BgdaObjectClasses.Find("Teleporter")!.Size), Is.EqualTo(BgdaPool.Medium));
        Assert.That(BgdaLevelBudget.PoolFor(BgdaObjectClasses.Find("Barrel")!.Size), Is.EqualTo(BgdaPool.Small));
        Assert.That(BgdaObjectClasses.LevelEntries("town"), Is.EqualTo(20));
        Assert.That(BgdaObjectClasses.LevelEntries("crypt1"), Is.EqualTo(1));
        Assert.That(BgdaObjectClasses.LevelEntries("cellar1"), Is.EqualTo(48));
        Assert.That(BgdaObjectClasses.DiscArchives["GNOLL.LMP"], Is.EqualTo(465328));
    }

    [Test]
    public void Tavern_PoolsAndMemory()
    {
        var b = BgdaLevelBudget.Analyze(Gob("TAVERN.GOB"));

        Assert.That(b.LevelName, Is.EqualTo("tavern"));
        Assert.That(b.ObjectCount, Is.EqualTo(36));
        Assert.That(b.Small.Used, Is.EqualTo(21));
        Assert.That(b.Medium.Used, Is.EqualTo(9), "nine story characters");
        Assert.That(b.Large.Used, Is.EqualTo(3), "the three player characters");
        Assert.That(b.NotCreated, Is.EqualTo(0));
        Assert.That(b.WorldFile, Is.EqualTo("pub.world"));
        Assert.That(b.WorldBufferUnits, Is.EqualTo(28));
        Assert.That(b.WorkBufferEntries, Is.EqualTo(48));
        Assert.That(b.Archives.Count, Is.EqualTo(14));
        Assert.That(b.Archives.All(a => a.Source == BgdaArchiveSource.Gob), Is.True);
        Assert.That(b.Archives.First(a => a.Name == "crystal.lmp").RamBytes, Is.EqualTo(19040), "measured in the game");
        Assert.That(b.Archives.First(a => a.Name == "shop.lmp").SoundBytes, Is.EqualTo(19840));
        Assert.That(b.GobArchivesBytes, Is.EqualTo(11819492));
        Assert.That(b.FreeAfterLoadBytes, Is.EqualTo(1958552));
        Assert.That(b.MemoryStatus, Is.EqualTo(BgdaBudgetStatus.Ok));
        Assert.That(b.SlotStatus, Is.EqualTo(BgdaBudgetStatus.Ok));
        Assert.That(b.MonsterRoom, Is.EqualTo(384 - 9 - BgdaLevelBudget.SpellSlots));
        Assert.That(b.Warnings, Is.Empty, "the level lights are known names");
    }

    [Test]
    public void Test_PoolsAndMemory()
    {
        var b = BgdaLevelBudget.Analyze(Gob("TEST.GOB"));

        Assert.That(b.ObjectCount, Is.EqualTo(13));
        Assert.That(b.Classes.First(c => c.Name == "SaveCrystal").Count, Is.EqualTo(4));
        Assert.That(b.Small.Used, Is.EqualTo(7));
        Assert.That(b.Medium.Used, Is.EqualTo(0));
        Assert.That(b.WorldBufferUnits, Is.EqualTo(4));
        Assert.That(b.FreeAfterLoadBytes, Is.EqualTo(16763308));
    }

    [Test]
    public void PendingEdits_AreCounted_AndMissingArchivesComeFromTheDisc()
    {
        var gob = Gob("TEST.GOB");
        var lmp = gob.Directory["test.lmp"];
        lmp.ReadDirectory();
        var info = lmp.FindFile("objects.ob")!;
        var original = lmp.FileData.AsSpan(info.StartOffset, info.Length).ToArray();
        var objects = ObDecoder.Decode(original, 0, original.Length);
        for (var i = 0; i < 20; i++) objects.Add(new ObjectData("Wolf", 0, new[] { 10f * i, 0f, 0f }, new List<string>()));
        lmp.ReplaceEntry("objects.ob", ObEncoder.Encode(objects, BitConverter.ToInt16(original, 2)));

        var b = BgdaLevelBudget.Analyze(gob);
        Assert.That(b.ObjectCount, Is.EqualTo(33));
        Assert.That(b.Medium.Used, Is.EqualTo(20));
        var wolf = b.Archives.First(a => a.Name == "wolf.lmp");
        Assert.That(wolf.Source, Is.EqualTo(BgdaArchiveSource.Disc), "not in TEST.GOB: the game loads it from the disc");
        Assert.That(wolf.FileBytes, Is.EqualTo(325392));
        Assert.That(wolf.UsedBy, Is.EqualTo(new[] { "Wolf" }));
        Assert.That(b.DiscArchivesBytes, Is.EqualTo(325392 + 256 + BgdaLevelBudget.PerBlockBytes));
        Assert.That(b.Warnings.Any(w => w.StartsWith("Wolf needs wolf.lmp, which is not in this GOB")), Is.True);

        var saved = BgdaLevelBudget.Analyze(Gob("TEST.GOB"));
        Assert.That(saved.Medium.Used, Is.EqualTo(0), "the file on disk is unchanged");
    }

    [Test]
    public void FullPools_SpillAndThenRefuse()
    {
        var names = Enumerable.Repeat("Kobold", 400).Concat(Enumerable.Repeat("Barrel", 600)).ToList();
        var slots = BgdaLevelBudget.CountSlots(names);
        Assert.That(slots.Small, Is.EqualTo(512));
        Assert.That(slots.Medium, Is.EqualTo(384));
        Assert.That(slots.Large, Is.EqualTo(32), "16 Kobolds, then 13 Barrels spill into the 1 KB pool (3 slots are the players')");
        Assert.That(slots.NotCreated, Is.EqualTo(600 - 512 - 13), "the other 75 Barrels are not created");
        Assert.That(BgdaLevelBudget.CountSlots(new[] { "Zap", "Ambient_Light", "NoSuchClass" }).NotCreated, Is.EqualTo(0));
    }
}
