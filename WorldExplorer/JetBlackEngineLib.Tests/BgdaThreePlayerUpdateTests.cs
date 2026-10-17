using JetBlackEngineLib.Data.Executable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Older three-player builds (ADRIANNA22, 21 and 19, which the tab used to write, and ADRIANNA25, 24, 23
/// and 20, which were handed out) are recognised and updated to the current build. The older files are
/// rebuilt here from the stock executable in Test_files plus the differences the patch data stores
/// (skipped when the stock executable is not present).
/// </summary>
[TestFixture]
public class BgdaThreePlayerUpdateTests
{
    private string _stockPath = "";
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
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaThreePlayerUpdateTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    /// <summary>The current three-player file: the stock executable with the patch turned on.</summary>
    private byte[] Current()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        exe.EnableThreePlayers();
        var path = Path.Combine(_workDir, "current.72");
        exe.Save(path);
        return File.ReadAllBytes(path);
    }

    /// <summary>The older builds as (name, [(offset, old bytes)]), read from the patch data.</summary>
    private static List<(string Name, List<(int Offset, byte[] Old)> Delta)> OlderBuilds()
    {
        var blob = Convert.FromBase64String(ThreePlayerPatch.BlobBase64);
        var pos = 0;
        int U32() { var v = BitConverter.ToInt32(blob, pos); pos += 4; return v; }
        byte[] Bytes(int n) { var b = blob[pos..(pos + n)]; pos += n; return b; }

        Assert.That(Encoding.ASCII.GetString(blob, 0, 4), Is.EqualTo("3PL2"));
        pos = 12;
        var runs = U32();
        for (var i = 0; i < runs; i++) { U32(); var n = U32(); pos += 2 * n; }
        var tail = U32();
        for (var i = 0; i < tail; i++) { U32(); var len = U32(); pos += len; }
        var result = new List<(string, List<(int, byte[])>)>();
        var count = U32();
        for (var i = 0; i < count; i++)
        {
            var name = Encoding.ASCII.GetString(Bytes(U32()));
            var delta = new List<(int, byte[])>();
            var k = U32();
            for (var j = 0; j < k; j++)
            {
                var off = U32();
                var n = U32();
                delta.Add((off, Bytes(n)));
                pos += n;                                  // the current bytes
            }

            result.Add((name, delta));
        }

        Assert.That(pos, Is.EqualTo(blob.Length));
        return result;
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Test]
    public void OlderBuilds_AreRecognised_AndUpdatedByteForByte()
    {
        var current = Current();
        var older = OlderBuilds();
        Assert.That(older.Select(o => o.Name), Is.EqualTo(new[]
        {
            "ADRIANNA25", "ADRIANNA24", "ADRIANNA23", "ADRIANNA22", "ADRIANNA21", "ADRIANNA20", "ADRIANNA19"
        }));

        foreach (var (name, delta) in older)
        {
            var bytes = (byte[])current.Clone();
            foreach (var (off, old) in delta) old.CopyTo(bytes, off);
            var exe = BgdaExecutable.Open(Write(name + ".72", bytes));

            Assert.That(exe.GetThreePlayerState(out var detail, out var build), Is.EqualTo(ThreePlayerState.Outdated), detail);
            Assert.That(build, Is.EqualTo(name));
            Assert.That(detail, Does.Contain("Saving updates it to " + ThreePlayerPatch.CurrentName));
            Assert.That(exe.GetThreePlayerSettings().MonsterHp, Is.EqualTo(BgdaExecutable.DefaultThreePlayerSettings().MonsterHp));

            exe.UpgradeThreePlayers();
            Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));
            var up = Path.Combine(_workDir, name + "-updated.72");
            exe.Save(up);
            Assert.That(File.ReadAllBytes(up), Is.EqualTo(current), $"{name} updated = the current build");

            var off2 = BgdaExecutable.Open(Write(name + "-2.72", bytes));
            off2.DisableThreePlayers();
            var stock = Path.Combine(_workDir, name + "-off.72");
            off2.Save(stock);
            Assert.That(File.ReadAllBytes(stock), Is.EqualTo(File.ReadAllBytes(_stockPath)), $"{name} turned off = stock");
        }
    }

    /// <summary>The older build as a file (the current one with the build's own bytes put back).</summary>
    private (byte[] Bytes, List<(int Offset, byte[] Old)> Delta) Older(string name)
    {
        var (_, delta) = OlderBuilds().First(o => o.Name == name);
        var bytes = Current();
        foreach (var (off, old) in delta) old.CopyTo(bytes, off);
        return (bytes, delta);
    }

    private static bool Covers(List<(int Offset, byte[] Old)> delta, int fileOffset) =>
        delta.Any(d => d.Offset <= fileOffset && fileOffset < d.Offset + d.Old.Length);

    private const int VaToFile = 0xFF000;
    private const int StagedToFile = 0x540000 - VaToFile;   // the code the game copies to low memory at start-up

    [Test]
    public void Adrianna21_IsUpdated_ForTheMemoryCountersFix()
    {
        Assert.That(ThreePlayerPatch.CurrentName, Is.EqualTo("ADRIANNA26"));
        var (bytes, delta) = Older("ADRIANNA21");
        var exe = BgdaExecutable.Open(Write("a21.72", bytes));

        Assert.That(exe.GetThreePlayerState(out var detail), Is.EqualTo(ThreePlayerState.Outdated), detail);
        Assert.That(detail, Does.Contain("record for each level"), "says what the update changes");
        // ADRIANNA21 hooked the level load at the texture-cache call (file 0x103A88); ADRIANNA22 and later have
        // the stock call there and hook the level loader call (file 0x103B74) instead
        Assert.That(Covers(delta, 0x103A88), Is.True);
        Assert.That(Covers(delta, 0x103B74), Is.True);
    }

    [Test]
    public void Adrianna22_IsUpdated_ForPlayer3InCutscenes()
    {
        var (bytes, delta) = Older("ADRIANNA22");
        var exe = BgdaExecutable.Open(Write("a22.72", bytes));

        Assert.That(exe.GetThreePlayerState(out var detail), Is.EqualTo(ThreePlayerState.Outdated), detail);
        Assert.That(detail, Does.Contain("Player3"), "says what the update changes");
        // the cutscene set-up's factory call and the end-of-scene "Player2" compare (ADRIANNA23), and the
        // main loop's camera-cut texture wait (ADRIANNA24)
        Assert.That(Covers(delta, 0x00158E5C - VaToFile), Is.True);
        Assert.That(Covers(delta, 0x001586B8 - VaToFile), Is.True);
        Assert.That(Covers(delta, 0x00203150 - VaToFile), Is.True);
    }

    [Test]
    public void Adrianna24_IsUpdated_ForTheMemoryCardFix()
    {
        var (bytes, delta) = Older("ADRIANNA24");
        var exe = BgdaExecutable.Open(Write("a24.72", bytes));

        Assert.That(exe.GetThreePlayerState(out var detail), Is.EqualTo(ThreePlayerState.Outdated), detail);
        Assert.That(detail, Does.Contain("memory card"), "says what the update changes");
        // ADRIANNA23 and 24 had PE3's first word in the delay slot of the jump at 0x000B9B4C
        Assert.That(Covers(delta, 0x000B9B50 + StagedToFile), Is.True);
        Assert.That(Covers(delta, 0x001586B8 - VaToFile), Is.True);
    }

    [Test]
    public void Adrianna25_IsUpdated_ForFaceAndLookAt()
    {
        var (bytes, delta) = Older("ADRIANNA25");
        var exe = BgdaExecutable.Open(Write("a25.72", bytes));

        Assert.That(exe.GetThreePlayerState(out var detail), Is.EqualTo(ThreePlayerState.Outdated), detail);
        Assert.That(detail, Does.Contain("\"Face\" or \"Look at\""), "says what the update changes");
        Assert.That(Covers(delta, 0x001557B4 - VaToFile), Is.True, "the Face call");
        Assert.That(Covers(delta, 0x00157974 - VaToFile), Is.True, "the current Look at");
        Assert.That(Covers(delta, 0x00157A7C - VaToFile), Is.True, "the next Look at");
        var current = Current();
        var differing = delta.Sum(d => d.Old.Where((b, i) => b != current[d.Offset + i]).Count());
        Assert.That(differing, Is.LessThan(100), "only those and the new code");
    }

    [Test]
    public void CurrentBuild_LeavesTheMemoryCardHooksDelaySlotEmpty()
    {
        var current = Current();
        uint Word(int fileOffset) => BitConverter.ToUInt32(current, fileOffset);
        // the players-line hook ends with 'j 0x001361E0' at 0x000B9B4C; the word after it runs too
        Assert.That(Word(0x000B9B4C + StagedToFile), Is.EqualTo(0x0804D878u));
        Assert.That(Word(0x000B9B50 + StagedToFile), Is.EqualTo(0u));
    }

    [Test]
    public void OutdatedBuild_KeepsItsAddedItemsWhenUpdated()
    {
        var (bytes, _) = Older("ADRIANNA25");
        var exe = BgdaExecutable.Open(Write("a25-items.72", bytes));
        var items = exe.GetItems();
        var added = items[12].Clone();
        added.Name = "orcbane";
        exe.AddItem(added);
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Outdated), "added items keep the build recognised");

        exe.UpgradeThreePlayers();
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));
        Assert.That(exe.ItemCount, Is.EqualTo(82));
        Assert.That(exe.GetItems()[81].Name, Is.EqualTo("orcbane"));
    }

    [Test]
    public void OlderBuild_KeepsItsSettingsWhenUpdated()
    {
        var current = Current();
        var (_, delta) = OlderBuilds().First(o => o.Name == "ADRIANNA19");
        var bytes = (byte[])current.Clone();
        foreach (var (off, old) in delta) old.CopyTo(bytes, off);
        var exe = BgdaExecutable.Open(Write("a19-settings.72", bytes));

        var settings = exe.GetThreePlayerSettings();
        settings.MonsterHp = 3.25f;
        settings.Markers[2] = new Rgb(1, 2, 3);
        exe.SetThreePlayerSettings(settings);
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Outdated), "editing settings keeps the build recognised");

        exe.UpgradeThreePlayers();
        var read = exe.GetThreePlayerSettings();
        Assert.That(read.MonsterHp, Is.EqualTo(3.25f));
        Assert.That(read.Markers[2], Is.EqualTo(new Rgb(1, 2, 3)));
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));
    }

    [Test]
    public void ChangedOlderBuild_IsNotRecognised()
    {
        var current = Current();
        var (_, delta) = OlderBuilds().First(o => o.Name == "ADRIANNA19");
        var bytes = (byte[])current.Clone();
        foreach (var (off, old) in delta) old.CopyTo(bytes, off);
        var (first, firstOld) = delta[0];
        bytes[first] ^= 0x01;                               // one byte that is neither ADRIANNA19's nor current
        var exe = BgdaExecutable.Open(Write("a19-poked.72", bytes));

        Assert.That(exe.GetThreePlayerState(out var detail), Is.EqualTo(ThreePlayerState.Unrecognized), detail);
        Assert.That(detail, Does.Contain("closest to ADRIANNA19"));
        Assert.Throws<InvalidOperationException>(() => exe.UpgradeThreePlayers());
        Assert.That(firstOld.Length, Is.GreaterThan(0));
    }
}
