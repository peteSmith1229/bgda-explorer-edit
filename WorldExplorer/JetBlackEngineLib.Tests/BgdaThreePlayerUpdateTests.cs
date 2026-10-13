using JetBlackEngineLib.Data.Executable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Older three-player builds (ADRIANNA21 and ADRIANNA19, which the tab used to write, and
/// ADRIANNA20) are recognised and updated to the current build. The older files are rebuilt here from the stock
/// executable in Test_files plus the differences the patch data stores (skipped when the stock
/// executable is not present).
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
        Assert.That(older.Select(o => o.Name), Is.EqualTo(new[] { "ADRIANNA21", "ADRIANNA20", "ADRIANNA19" }));

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

    [Test]
    public void Adrianna21_IsUpdated_ForTheMemoryCountersFix()
    {
        Assert.That(ThreePlayerPatch.CurrentName, Is.EqualTo("ADRIANNA22"));
        var current = Current();
        var (_, delta) = OlderBuilds().First(o => o.Name == "ADRIANNA21");
        var bytes = (byte[])current.Clone();
        foreach (var (off, old) in delta) old.CopyTo(bytes, off);
        var exe = BgdaExecutable.Open(Write("a21.72", bytes));

        Assert.That(exe.GetThreePlayerState(out var detail), Is.EqualTo(ThreePlayerState.Outdated), detail);
        Assert.That(detail, Does.Contain("record for each level"), "says what the update changes");
        // ADRIANNA21 hooked the level load at the texture-cache call (file 0x103A88); ADRIANNA22 has
        // the stock call there and hooks the level loader call (file 0x103B74) instead
        Assert.That(delta.Any(d => d.Offset <= 0x103A88 && 0x103A88 < d.Offset + d.Old.Length), Is.True);
        Assert.That(delta.Any(d => d.Offset <= 0x103B74 && 0x103B74 < d.Offset + d.Old.Length), Is.True);
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
