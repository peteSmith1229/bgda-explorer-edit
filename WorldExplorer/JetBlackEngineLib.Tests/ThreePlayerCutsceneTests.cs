using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.DataContainers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The cutscenes with player 3 that the Three Players tab puts in the game's GOB files. The tests that change a GOB
/// file use Test_files/CUTTOWN.GOB (the game's own; skipped when it isn't there) on a copy in a temporary folder.
/// </summary>
[TestFixture]
public class ThreePlayerCutsceneTests
{
    private const ThreePlayerCutsceneState Original = ThreePlayerCutsceneState.Original;
    private const ThreePlayerCutsceneState WithPlayer3 = ThreePlayerCutsceneState.WithPlayer3;
    private const ThreePlayerCutsceneState Changed = ThreePlayerCutsceneState.Changed;
    private const ThreePlayerCutsceneState NotFound = ThreePlayerCutsceneState.NotFound;

    private string? _cutTown;
    private string _work = "";

    [OneTimeSetUp]
    public void FindFiles()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "CUTTOWN.GOB")))
        {
            dir = dir.Parent;
        }

        _cutTown = dir == null ? null : Path.Combine(dir.FullName, "Test_files", "CUTTOWN.GOB");
        _work = Path.Combine(Path.GetTempPath(), "WorldExplorerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_work);
    }

    [OneTimeTearDown]
    public void RemoveFiles()
    {
        try
        {
            Directory.Delete(_work, true);
        }
        catch (IOException)
        {
            // left for the system to clean up
        }
    }

    private byte[] StockCutTown()
    {
        if (_cutTown == null) Assert.Ignore("Test_files/CUTTOWN.GOB was not found above the test directory.");
        return File.ReadAllBytes(_cutTown!);
    }

    private static ThreePlayerCutscene Intro => ThreePlayerCutscenes.All.Single(c => c.Entry == "intro.cut");

    /// <summary>Every entry of every archive, as "archive/entry" → bytes, in file order.</summary>
    private static List<(string Name, byte[] Bytes)> Entries(byte[] gob)
    {
        var file = new GobFile(EngineVersion.DarkAlliance, "test.gob", gob);
        var all = new List<(string, byte[])>();
        foreach (var (archive, lmp) in file.Directory)
        {
            lmp.ReadDirectory();
            foreach (var (name, entry) in lmp.Directory)
            {
                all.Add(($"{archive}/{name}", lmp.FileData.AsSpan(entry.StartOffset, entry.Length).ToArray()));
            }
        }

        return all;
    }

    private static byte[] Entry(byte[] gob, string name) => Entries(gob).Single(e => e.Name == name).Bytes;

    /// <summary>A folder holding the given files, and a finder like the Three Players tab's for it.</summary>
    private (string Folder, Func<string, string?> Find) Folder(string name, params (string File, byte[] Bytes)[] files)
    {
        var folder = Path.Combine(_work, name);
        Directory.CreateDirectory(folder);
        foreach (var (file, bytes) in files) File.WriteAllBytes(Path.Combine(folder, file), bytes);
        return (folder, gob => File.Exists(Path.Combine(folder, gob)) ? Path.Combine(folder, gob) : null);
    }

    [Test]
    public void TheData_IsTheEightCutscenesWithPlayer3()
    {
        Assert.That(ThreePlayerCutscenes.All.Select(c => c.ToString()).ToArray(), Is.EqualTo(new[]
        {
            "garik.cut (BTHRONE.GOB)", "orbroom.cut (CRYPT2.GOB)", "orbroom2.cut (CRYPT2.GOB)", "intro.cut (CUTTOWN.GOB)",
            "dbeast.cut (ICECAVE1.GOB)", "dragintro.cut (ICECAVE3.GOB)", "ethonfree.cut (SEWER1.GOB)",
            "eldrithend.cut (TOWERT.GOB)"
        }));
        Assert.That(ThreePlayerCutscenes.GobNames.ToArray(), Is.EqualTo(new[]
        {
            "BTHRONE.GOB", "CRYPT2.GOB", "CUTTOWN.GOB", "ICECAVE1.GOB", "ICECAVE3.GOB", "SEWER1.GOB", "TOWERT.GOB"
        }));

        foreach (var cutscene in ThreePlayerCutscenes.All)
        {
            var before = EditableCutscene.Load(cutscene.GetOriginal());
            var after = EditableCutscene.Load(cutscene.GetWithPlayer3());
            Assert.That(before.Cast.Any(a => a.Name == "Player3"), Is.False, $"{cutscene}: the game's own has no Player3");
            Assert.That(after.Cast.Single(a => a.Name == "Player3").IsCreated, Is.True, $"{cutscene}: Player3 is made by the scene");
            Assert.That(after.ToArray(), Is.EqualTo(cutscene.GetWithPlayer3()), $"{cutscene}: read and written back unchanged");
        }
    }

    [Test]
    public void OnlyTheIntro_NeedsAdrianna26()
    {
        // its camera "Look at" and a "Face" aim at Player3, who isn't there in one- and two-player games
        Assert.That(ThreePlayerCutscenes.All.Where(c => c.NeedsAdrianna26).Select(c => c.ToString()).ToArray(),
            Is.EqualTo(new[] { "intro.cut (CUTTOWN.GOB)" }));
    }

    [Test]
    public void TheIntro_IsMadeFromTheGamesOwn()
    {
        Assert.That(Intro.GetOriginal(), Is.EqualTo(Entry(StockCutTown(), "cuttown.lmp/intro.cut")));
    }

    [Test]
    public void Update_ChangesOnlyTheCutscene_AndPutsTheGamesOwnBack()
    {
        var stock = StockCutTown();
        var found = new List<ThreePlayerCutsceneStatus>();
        var updated = ThreePlayerCutscenes.Update("CUTTOWN.GOB", stock, withPlayer3: true, found);
        Assert.That(updated, Is.Not.Null);
        Assert.That(found.Select(s => s.State).ToArray(), Is.EqualTo(new[] { Original }));

        var before = Entries(stock);
        var after = Entries(updated!);
        Assert.That(after.Select(e => e.Name).ToArray(), Is.EqualTo(before.Select(e => e.Name).ToArray()), "the same entries, in order");
        for (var i = 0; i < before.Count; i++)
        {
            var want = before[i].Name == "cuttown.lmp/intro.cut" ? Intro.GetWithPlayer3() : before[i].Bytes;
            Assert.That(after[i].Bytes, Is.EqualTo(want), before[i].Name);
        }

        Assert.That(ThreePlayerCutscenes.Update("CUTTOWN.GOB", updated!, withPlayer3: true), Is.Null, "nothing left to do");

        var back = ThreePlayerCutscenes.Update("CUTTOWN.GOB", updated!, withPlayer3: false);
        Assert.That(back, Is.Not.Null);
        var restored = Entries(back!);
        Assert.That(restored.Select(e => e.Name).ToArray(), Is.EqualTo(before.Select(e => e.Name).ToArray()));
        for (var i = 0; i < before.Count; i++) Assert.That(restored[i].Bytes, Is.EqualTo(before[i].Bytes), before[i].Name);
        Assert.That(ThreePlayerCutscenes.Update("CUTTOWN.GOB", back!, withPlayer3: false), Is.Null, "already the game's own");
    }

    [Test]
    public void ACutsceneChangedSomeOtherWay_IsLeftAsItIs()
    {
        // the intro with its last keyframe a second later: neither the game's own nor the version with player 3
        var gob = new GobFile(EngineVersion.DarkAlliance, "CUTTOWN.GOB", StockCutTown());
        var lmp = gob.Directory["cuttown.lmp"];
        lmp.ReadDirectory();
        var scene = EditableCutscene.Load(lmp.GetEntryBytes("intro.cut")!);
        scene.SetTime(scene.Keyframes[^1], scene.Duration + 1);
        lmp.ReplaceEntry("intro.cut", scene.ToArray());
        var changed = GobWriter.TryPatchInPlace(gob) ?? GobWriter.Pack(gob);

        var found = new List<ThreePlayerCutsceneStatus>();
        Assert.That(ThreePlayerCutscenes.Update("CUTTOWN.GOB", changed, withPlayer3: true, found), Is.Null);
        Assert.That(found.Single().State, Is.EqualTo(Changed));
        Assert.That(ThreePlayerCutscenes.Update("CUTTOWN.GOB", changed, withPlayer3: false), Is.Null);

        var (_, find) = Folder("changed", ("CUTTOWN.GOB", changed));
        var plan = ThreePlayerCutscenes.Inspect(find);
        Assert.That(ThreePlayerCutscenes.DescribePlan(plan),
            Does.Contain("Left as they are, because they have been changed: intro.cut (CUTTOWN.GOB)."));
        var result = ThreePlayerCutscenes.Apply(find, withPlayer3: true);
        Assert.That(result.LeftAsTheyAre.Select(s => s.Cutscene.Entry).ToArray(), Is.EqualTo(new[] { "intro.cut" }));
        Assert.That(result.Changed, Is.Empty);
    }

    [Test]
    public void Apply_UpdatesTheFilesItFinds_AndSaysWhatItDid()
    {
        var stock = StockCutTown();
        var (folder, find) = Folder("apply", ("CUTTOWN.GOB", stock));
        var path = Path.Combine(folder, "CUTTOWN.GOB");

        var plan = ThreePlayerCutscenes.Inspect(find);
        Assert.That(plan.Count, Is.EqualTo(8));
        Assert.That(plan.Single(s => s.Cutscene.Entry == "intro.cut").State, Is.EqualTo(Original));
        Assert.That(plan.Count(s => s.State == NotFound), Is.EqualTo(7));
        Assert.That(ThreePlayerCutscenes.DescribePlan(plan), Is.EqualTo(
            $"When you save, player 3 is added to 1 cutscene in 1 GOB file: CUTTOWN (in {folder}). Only those cutscenes " +
            "change. Not found: BTHRONE.GOB, CRYPT2.GOB, ICECAVE1.GOB, ICECAVE3.GOB, SEWER1.GOB and TOWERT.GOB."));
        Assert.That(ThreePlayerCutscenes.DescribeTurnOff(plan), Is.Null, "nothing with player 3 yet");

        var result = ThreePlayerCutscenes.Apply(find, withPlayer3: true);
        Assert.That(result.Changed.Select(s => s.Cutscene.Entry).ToArray(), Is.EqualTo(new[] { "intro.cut" }));
        Assert.That(result.NotFound.Count, Is.EqualTo(7));
        Assert.That(result.Failed, Is.Empty);
        Assert.That(result.Describe(), Is.EqualTo(
            $"Player 3 is now in 1 cutscene in 1 GOB file: CUTTOWN (in {folder}). Not found: BTHRONE.GOB, CRYPT2.GOB, " +
            "ICECAVE1.GOB, ICECAVE3.GOB, SEWER1.GOB and TOWERT.GOB."));
        Assert.That(Entry(File.ReadAllBytes(path), "cuttown.lmp/intro.cut"), Is.EqualTo(Intro.GetWithPlayer3()));
        Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName).ToArray(), Is.EqualTo(new[] { "CUTTOWN.GOB" }),
            "no temporary file is left");

        var again = ThreePlayerCutscenes.Apply(find, withPlayer3: true);
        Assert.That(again.Changed, Is.Empty);
        Assert.That(again.AlreadyDone.Count, Is.EqualTo(1));
        Assert.That(again.Describe(), Is.EqualTo($"The cutscenes already had player 3: 1 cutscene in 1 GOB file (in {folder}). Not found: " +
                                             "BTHRONE.GOB, CRYPT2.GOB, ICECAVE1.GOB, ICECAVE3.GOB, SEWER1.GOB and TOWERT.GOB."));

        var withIntro = ThreePlayerCutscenes.Inspect(find);
        Assert.That(ThreePlayerCutscenes.DescribePlan(withIntro), Does.Contain(
            $"The game's cutscenes already have player 3: 1 cutscene in 1 GOB file (in {folder})."));
        Assert.That(ThreePlayerCutscenes.ShouldOfferPuttingBack(withIntro), Is.True);
        Assert.That(ThreePlayerCutscenes.DescribeTurnOff(withIntro), Is.EqualTo(
            $"1 cutscene in 1 GOB file has player 3 (in {folder}). intro.cut (CUTTOWN.GOB) has a Face or Look at aimed at " +
            "Player3, which only ADRIANNA26 and later play safely, so when you save you'll be asked whether to put the " +
            "game's own cutscenes back."));
        Assert.That(ThreePlayerCutscenes.PuttingBackQuestion(withIntro), Does.Contain("Put the game's own cutscenes back?"));

        var back = ThreePlayerCutscenes.Apply(find, withPlayer3: false);
        Assert.That(back.Changed.Count, Is.EqualTo(1));
        Assert.That(back.Describe(), Does.Contain($"The game's own cutscenes are back in 1 cutscene in 1 GOB file: CUTTOWN (in {folder})."));
        var restored = Entries(File.ReadAllBytes(path));
        var original = Entries(stock);
        Assert.That(restored.Count, Is.EqualTo(original.Count));
        for (var i = 0; i < original.Count; i++) Assert.That(restored[i].Bytes, Is.EqualTo(original[i].Bytes), original[i].Name);
        Assert.That(ThreePlayerCutscenes.ShouldOfferPuttingBack(ThreePlayerCutscenes.Inspect(find)), Is.False);
    }

    [Test]
    public void NoGobFiles_NothingChanges()
    {
        var none = ThreePlayerCutscenes.Inspect(_ => null);
        Assert.That(none.All(s => s.State == NotFound && s.Path == null), Is.True);
        Assert.That(ThreePlayerCutscenes.DescribePlan(none), Is.EqualTo(
            "The game's GOB files (BTHRONE, CRYPT2, CUTTOWN, ICECAVE1, ICECAVE3, SEWER1 and TOWERT) weren't found, so no " +
            "cutscenes will change."));
        Assert.That(ThreePlayerCutscenes.Apply(_ => null, withPlayer3: true).Describe(),
            Is.EqualTo("The game's GOB files weren't found, so no cutscenes changed."));
    }

    [Test]
    public void AFileThatIsNotAGob_IsLeftAlone()
    {
        var notAGob = new byte[100];
        var (folder, find) = Folder("not-a-gob", ("TOWERT.GOB", notAGob));
        var towert = ThreePlayerCutscenes.Inspect(find).Single(s => s.Cutscene.Gob == "TOWERT.GOB");
        Assert.That(towert.State, Is.EqualTo(NotFound));
        Assert.That(ThreePlayerCutscenes.DescribePlan(ThreePlayerCutscenes.Inspect(find)), Is.EqualTo(
            "No cutscene will change. Not found: BTHRONE.GOB, CRYPT2.GOB, CUTTOWN.GOB, ICECAVE1.GOB, ICECAVE3.GOB, " +
            "SEWER1.GOB and eldrithend.cut in TOWERT.GOB."));
        var result = ThreePlayerCutscenes.Apply(find, withPlayer3: true);
        Assert.That(result.NotFound.Count, Is.EqualTo(8));
        Assert.That(result.Describe(), Does.Contain("No cutscene changed. Not found: "));
        Assert.That(File.ReadAllBytes(Path.Combine(folder, "TOWERT.GOB")), Is.EqualTo(notAGob), "not written");
    }

    [Test]
    public void ADamagedGob_IsReportedAndLeftAsItWas()
    {
        // the game's own CUTTOWN.GOB cut short: its directories point past the end
        var stock = StockCutTown();
        var damaged = stock.AsSpan(0, stock.Length / 2).ToArray();
        var (folder, find) = Folder("damaged", ("CUTTOWN.GOB", damaged));
        var path = Path.Combine(folder, "CUTTOWN.GOB");
        var intro = ThreePlayerCutscenes.Inspect(find).Single(s => s.Cutscene.Gob == "CUTTOWN.GOB");
        Assert.That(intro.State, Is.EqualTo(NotFound), "can't be read: as good as not there");
        Assert.That(intro.Path, Is.EqualTo(path));
        var result = ThreePlayerCutscenes.Apply(find, withPlayer3: true);
        Assert.That(result.Failed.Select(f => f.Path).ToArray(), Is.EqualTo(new[] { path }));
        Assert.That(result.Describe(), Does.Contain("CUTTOWN.GOB couldn't be updated and is unchanged"));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(damaged), "not written");
        Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName).ToArray(), Is.EqualTo(new[] { "CUTTOWN.GOB" }));
    }

    [Test]
    public void AFileThatCantBeRead_IsReported()
    {
        // a folder where the GOB file should be: it can't be read, and nothing is written
        var (folder, _) = Folder("unreadable");
        var path = Path.Combine(folder, "CUTTOWN.GOB");
        Directory.CreateDirectory(path);
        var result = ThreePlayerCutscenes.Apply(gob => gob == "CUTTOWN.GOB" ? path : null, withPlayer3: true);
        Assert.That(result.Failed.Select(f => f.Path).ToArray(), Is.EqualTo(new[] { path }));
        Assert.That(result.Describe(), Does.Contain("CUTTOWN.GOB couldn't be updated and is unchanged"));
        Assert.That(Directory.GetFiles(folder), Is.Empty, "no temporary file is left");
    }
}
