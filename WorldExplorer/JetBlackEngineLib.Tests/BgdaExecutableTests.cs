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
