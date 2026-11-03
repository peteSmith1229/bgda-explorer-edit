using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Dark Alliance II's level scripts with the script tools Dark Alliance 1 has (Edit Script Rewards, Insert
/// Reward at Call Site): the same .scr format, the same reward commands. Reads every level file put in
/// Test_files/Da2Data/ (the game's BG/DATA/*.gob, USA; none are in the repository) and is skipped without
/// them; checked on the 12 levels of 2026-11-02 (bgate, bginn, caravan, goblair, guild1, inn, lyranc,
/// monaster, redcamp, redlair, trollb, village).
/// </summary>
[TestFixture]
public class Da2ScriptTests
{
    private string[] _gobs = Array.Empty<string>();

    [OneTimeSetUp]
    public void FindLevels()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Test_files", "Da2Data"))) dir = dir.Parent;
        if (dir == null) Assert.Ignore("Test_files/Da2Data (Dark Alliance II level files) was not found above the test directory.");
        _gobs = Directory.GetFiles(Path.Combine(dir!.FullName, "Test_files", "Da2Data"), "*.gob").OrderBy(p => p).ToArray();
        if (_gobs.Length == 0) Assert.Ignore("Test_files/Da2Data holds no level files.");
    }

    private static (GobFile Gob, LmpFile Lmp, byte[] Script) Open(string path)
    {
        var gob = new GobFile(EngineVersion.DarkAlliance, path);
        var lmp = gob.Directory[Path.GetFileNameWithoutExtension(path).ToLowerInvariant() + ".lmp"];
        lmp.ReadDirectory();
        var e = lmp.Directory["script.scr"];
        return (gob, lmp, lmp.FileData.AsSpan(e.StartOffset, e.Length).ToArray());
    }

    private static byte[] Entry(LmpFile lmp, string name)
    {
        var e = lmp.Directory[name];
        return lmp.FileData.AsSpan(e.StartOffset, e.Length).ToArray();
    }

    [Test]
    public void EveryScript_IsRead_AndItsRewardsFound()
    {
        var rewards = new Dictionary<string, string>();
        foreach (var path in _gobs)
        {
            var (_, _, scr) = Open(path);
            var level = Path.GetFileNameWithoutExtension(path);
            var script = EditableScript.Load(scr);
            var found = ScriptRewardScanner.Scan(scr);
            var sites = ScriptCallSiteScanner.Scan(scr);
            Assert.That(found.Count, Is.EqualTo(script.Calls.Count(c => c.Name is "givePlayerGold" or "givePlayerExp" or "givePlayerItem")), level);
            Assert.That(sites.Count, Is.EqualTo(script.Calls.Count), level);
            rewards[level] = string.Join(", ", found.Select(r => $"{r.FunctionName}:{r.ExternalName}={r.IntValue}"));
        }

        // values read from the game's scripts (quest rewards and the inn's purchases)
        if (rewards.TryGetValue("bgate", out var bgate))
            Assert.That(bgate, Is.EqualTo("QRedFangR:givePlayerGold=3000, QRedFangR:givePlayerExp=3000, QSewersR:givePlayerGold=1000, QSewersR:givePlayerExp=1000"));
        if (rewards.TryGetValue("village", out var village))
            Assert.That(village, Is.EqualTo("RescueVillager:givePlayerGold=250, RescueVillager:givePlayerExp=750"));
        if (rewards.TryGetValue("bginn", out var bginn))
            Assert.That(bginn, Does.Contain("QGandamR:givePlayerExp=8000").And.Contain("BuyKey:givePlayerGold=-4000"));
    }

    [Test]
    public void RewardEdits_AreSavedIntoTheLevelFile_AndNothingElseChanges()
    {
        foreach (var path in _gobs)
        {
            var level = Path.GetFileNameWithoutExtension(path);
            var (gob, lmp, scr) = Open(path);
            var found = ScriptRewardScanner.Scan(scr);
            if (found.Count == 0) continue;

            // a value edited in place: the level file keeps its size, four bytes at most change
            var edited = (byte[])scr.Clone();
            ScriptRewardScanner.ApplyIntValue(edited, found[0], 4321);
            Assert.That(ScriptRewardScanner.Scan(edited)[0].IntValue, Is.EqualTo(4321), level);
            lmp.ReplaceEntry("script.scr", edited);
            var inPlace = GobWriter.TryPatchInPlace(gob);
            Assert.That(inPlace, Is.Not.Null, level);
            var original = File.ReadAllBytes(path);
            Assert.That(inPlace!.Length, Is.EqualTo(original.Length), level);
            Assert.That(original.Zip(inPlace).Count(p => p.First != p.Second), Is.InRange(1, 4), level);

            // rewards inserted before a call: the script grows, the level file is packed again
            var (gob2, lmp2, scr2) = Open(path);
            var anchor = ScriptCallSiteScanner.Scan(scr2).First(s => s.Insertable);
            var grown = ScriptDetourPatcher.InsertRewardsBeforeCall(scr2, anchor, new[]
            {
                new NewRewardCall { ExternalName = "givePlayerExp", IntValue = 777 },
                new NewRewardCall { ExternalName = "givePlayerItem", ItemName = "dagger" }
            });
            Assert.That(EditableScript.Load(grown).Calls.Count, Is.EqualTo(EditableScript.Load(scr2).Calls.Count + 2), level);
            Assert.That(ScriptRewardScanner.Scan(grown).Count(r => r.IntValue == 777), Is.EqualTo(1), level);
            lmp2.ReplaceEntry("script.scr", grown);
            var packed = GobWriter.TryPatchInPlace(gob2) ?? GobWriter.Pack(gob2);
            var saved = new GobFile(EngineVersion.DarkAlliance, "saved.gob", packed);
            Assert.That(saved.Directory.Keys, Is.EqualTo(gob2.Directory.Keys), level);
            var savedLmp = saved.Directory[level + ".lmp"];
            savedLmp.ReadDirectory();
            Assert.That(Entry(savedLmp, "script.scr"), Is.EqualTo(grown), level);
            foreach (var name in lmp2.Directory.Keys.Where(n => n != "script.scr"))
                Assert.That(Entry(savedLmp, name), Is.EqualTo(Entry(lmp2, name)), $"{level}: {name} is unchanged");
        }
    }
}
