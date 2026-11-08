using JetBlackEngineLib.Data.Executable;
using System;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Checks the three-player patch and the difficulty scaling against the stock
/// PAL executable in Test_files (skipped when that file is not present).
/// </summary>
[TestFixture]
public class BgdaExecutableTests
{
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
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaExecutableTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    /// <summary>Saves the executable and returns the bytes written.</summary>
    private byte[] SaveAndRead(BgdaExecutable exe, string name)
    {
        var path = Path.Combine(_workDir, name);
        exe.Save(path);
        return File.ReadAllBytes(path);
    }

    private BgdaExecutable ReopenSaved(BgdaExecutable exe, string name)
    {
        var path = Path.Combine(_workDir, name);
        exe.Save(path);
        return BgdaExecutable.Open(path);
    }

    [Test]
    public void StockExecutable_IsRecognised()
    {
        var exe = BgdaExecutable.Open(_stockPath);

        Assert.That(_stockBytes.Length, Is.EqualTo(BgdaExecutable.StockFileLength));
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off));
        Assert.That(exe.GetDifficultyState(), Is.EqualTo(DifficultyCodeState.Stock));
        Assert.That(exe.GetDifficultyScaling().SameAs(BgdaExecutable.StockDifficulty()), Is.True);
    }

    [Test]
    public void ThreePlayers_EnableEditDisable_RestoresTheStockFile()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        exe.EnableThreePlayers();

        var patched = ReopenSaved(exe, "three-player.72");
        Assert.That(new FileInfo(Path.Combine(_workDir, "three-player.72")).Length,
            Is.EqualTo(BgdaExecutable.ThreePlayerFileLength));
        Assert.That(patched.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));

        var settings = patched.GetThreePlayerSettings();
        var defaults = BgdaExecutable.DefaultThreePlayerSettings();
        Assert.That(settings.MonsterHp, Is.EqualTo(defaults.MonsterHp));
        Assert.That(settings.OrbDivisor, Is.EqualTo(defaults.OrbDivisor));

        settings.MonsterHp = 2.25f;
        settings.OrbDivisor = 3;
        settings.Markers[2] = new Rgb(10, 200, 30);
        settings.SmallMapX = -40f;
        patched.SetThreePlayerSettings(settings);

        var edited = ReopenSaved(patched, "three-player-edited.72");
        Assert.That(edited.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On),
            "editing the settings must not break the patch check");
        var readBack = edited.GetThreePlayerSettings();
        Assert.That(readBack.MonsterHp, Is.EqualTo(2.25f));
        Assert.That(readBack.OrbDivisor, Is.EqualTo(3));
        Assert.That(readBack.Markers[2], Is.EqualTo(new Rgb(10, 200, 30)));
        Assert.That(readBack.SmallMapX, Is.EqualTo(-40f));

        edited.DisableThreePlayers();
        Assert.That(SaveAndRead(edited, "three-player-off.72"), Is.EqualTo(_stockBytes));
    }

    [Test]
    public void ThreePlayers_KeepEditsMadeOutsideThePatch()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        var xp = exe.GetXpTable();
        xp[1] = 900;
        exe.SetXpTable(xp);

        exe.EnableThreePlayers();
        var patched = ReopenSaved(exe, "three-player-xp.72");
        Assert.That(patched.GetXpTable(), Is.EqualTo(xp));
        Assert.That(BgdaMonsters.Open(Path.Combine(_workDir, "three-player-xp.72")).GetMonsters().Count,
            Is.EqualTo(BgdaMonsters.Open(_stockPath).GetMonsters().Count),
            "the monster table must still be found in the patched file");

        patched.DisableThreePlayers();
        Assert.That(patched.GetXpTable(), Is.EqualTo(xp));
    }

    [Test]
    public void XpTable_HasTheGamesFortyLevels()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        var xp = exe.GetXpTable();
        Assert.That(xp.Length, Is.EqualTo(40));
        Assert.That(xp[1], Is.EqualTo(1000), "level 2");
        Assert.That(xp[15], Is.EqualTo(120000), "level 16, the last the tab used to show");
        Assert.That(xp[16], Is.EqualTo(136000), "level 17");
        Assert.That(xp[39], Is.EqualTo(795000), "level 40");
        Assert.That(exe.GetXpCap(), Is.EqualTo(99999999), "after level 40: what no one reaches");

        xp[20] = 230000;
        xp[39] = 900000;
        exe.SetXpTable(xp);
        var again = ReopenSaved(exe, "xp-forty.72");
        Assert.That(again.GetXpTable(), Is.EqualTo(xp));
        Assert.That(again.GetXpCap(), Is.EqualTo(99999999), "the cap stays");
        var bytes = File.ReadAllBytes(Path.Combine(_workDir, "xp-forty.72"));
        var changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).ToList();
        Assert.That(changed.All(i => i >= BgdaExecutable.XpTableOffset + 20 * 4 && i < BgdaExecutable.XpTableOffset + 40 * 4), Is.True,
                    "only levels 21 and 40 changed");

        var bad = again.GetXpTable();
        bad[17] = bad[16];
        var e = Assert.Throws<ArgumentException>(() => again.SetXpTable(bad));
        Assert.That(e!.Message, Does.Contain("Level 18"));
        var over = again.GetXpTable();
        over[39] = 99999999;
        e = Assert.Throws<ArgumentException>(() => again.SetXpTable(over));
        Assert.That(e!.Message, Does.Contain("Level 40"));
        Assert.That(again.GetXpTable(), Is.EqualTo(xp), "nothing written");
    }

    [Test]
    public void XpTable_AFileEditedWhenTheTabShowedSixteenLevels_StillOpens()
    {
        // level 16 raised above the game's level 17, as the 16-level tab allowed: the file still opens, the whole table is read,
        // and saving it as it is says which level is wrong (in the game levels 17 on would come one a kill)
        var bytes = (byte[])_stockBytes.Clone();
        BitConverter.GetBytes(150000).CopyTo(bytes, BgdaExecutable.XpTableOffset + 15 * 4);
        var path = Path.Combine(_workDir, "xp-sixteen.72");
        File.WriteAllBytes(path, bytes);
        var exe = BgdaExecutable.Open(path);
        var xp = exe.GetXpTable();
        Assert.That(xp[15], Is.EqualTo(150000));
        Assert.That(xp[16], Is.EqualTo(136000));
        var e = Assert.Throws<ArgumentException>(() => exe.SetXpTable(xp));
        Assert.That(e!.Message, Does.Contain("Level 17"));
        xp[16] = 160000;
        xp[17] = 170000;
        exe.SetXpTable(xp);
        Assert.That(ReopenSaved(exe, "xp-sixteen-fixed.72").GetXpTable(), Is.EqualTo(xp));
    }

    [Test]
    public void Difficulty_EditThenStock_RestoresTheStockFile()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        var edited = BgdaExecutable.StockDifficulty();
        edited.MonsterHp[0] = 0.5f;
        edited.MonsterDamage[2] = 1.75f;
        edited.LevelBonus[3] = 45f;
        edited.BossHp[3] = 3f;

        exe.SetDifficultyScaling(edited);
        Assert.That(exe.GetDifficultyState(), Is.EqualTo(DifficultyCodeState.Table));
        Assert.That(exe.GetDifficultyScaling().SameAs(edited), Is.True);

        exe.SetDifficultyScaling(BgdaExecutable.StockDifficulty());
        Assert.That(exe.GetDifficultyState(), Is.EqualTo(DifficultyCodeState.Stock));
        Assert.That(SaveAndRead(exe, "difficulty.72"), Is.EqualTo(_stockBytes));
    }

    [Test]
    public void InvalidValues_AreRejectedWithoutWriting()
    {
        var exe = BgdaExecutable.Open(_stockPath);

        var bad = BgdaExecutable.StockDifficulty();
        bad.MonsterHp[1] = 0f;
        Assert.Throws<ArgumentException>(() => exe.SetDifficultyScaling(bad));
        Assert.Throws<InvalidOperationException>(() => exe.GetThreePlayerSettings());
        Assert.Throws<InvalidOperationException>(() => exe.DisableThreePlayers());

        Assert.That(SaveAndRead(exe, "rejected.72").SequenceEqual(_stockBytes), Is.True);
    }
}
