using JetBlackEngineLib.Data.Executable;
using System;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The three-player HUD layout (BgdaExecutable.HudLayout.cs): it needs three players, goes in and comes
/// out byte for byte, keeps three players On, round-trips its settings, refuses bad values, and is taken
/// out when three players are turned off. Built from the stock executable in Test_files (skipped when
/// it is not present).
/// </summary>
[TestFixture]
public class BgdaHudLayoutTests
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
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaHudLayoutTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    private BgdaExecutable ThreePlayers()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        exe.EnableThreePlayers();
        return exe;
    }

    private byte[] Bytes(BgdaExecutable exe)
    {
        var path = Path.Combine(_workDir, Guid.NewGuid().ToString("N") + ".72");
        exe.Save(path);
        var bytes = File.ReadAllBytes(path);
        File.Delete(path);
        return bytes;
    }

    private BgdaExecutable Reopen(BgdaExecutable exe)
    {
        var path = Path.Combine(_workDir, Guid.NewGuid().ToString("N") + ".72");
        exe.Save(path);
        return BgdaExecutable.Open(path);
    }

    private static HudLayout Sample()
    {
        var layout = new HudLayout();
        layout[0, HudPart.Panel] = new HudPlacement(120, 60, 60);
        layout[1, HudPart.Skills] = new HudPlacement(-30, -80, 150);
        layout[2, HudPart.Icons] = new HudPlacement(0, 200, 100);
        layout[2, HudPart.Gold] = new HudPlacement(-640, 448, 25);
        layout[1, HudPart.Gold] = new HudPlacement(640, -448, 300);
        return layout;
    }

    [Test]
    public void StockExecutableHasNoHudLayout()
    {
        var exe = BgdaExecutable.Open(_stockPath);
        Assert.That(exe.GetHudLayoutState(), Is.EqualTo(HudLayoutState.Unavailable));
        Assert.Throws<InvalidOperationException>(() => exe.SetHudLayout(HudLayout.Default));
    }

    [Test]
    public void ThreePlayersStartWithTheGamesLayout()
    {
        var exe = ThreePlayers();
        Assert.That(exe.GetHudLayoutState(), Is.EqualTo(HudLayoutState.Off));
        Assert.That(exe.GetHudLayout().IsDefault, Is.True);
    }

    [Test]
    public void InAndOutByteForByte()
    {
        var exe = ThreePlayers();
        var before = Bytes(exe);

        exe.SetHudLayout(Sample());
        var with = Bytes(exe);
        Assert.That(with.Length, Is.EqualTo(before.Length));
        Assert.That(exe.GetHudLayoutState(), Is.EqualTo(HudLayoutState.On));
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On), "three players stay On");

        // only the block and the hook words changed
        var changed = Enumerable.Range(0, before.Length).Where(i => before[i] != with[i]).ToList();
        var hookBytes = HudLayoutPatch.Hooks.SelectMany(h => Enumerable.Range(h.Va - 0xFF000, 4)).ToHashSet();
        Assert.That(changed.All(i => hookBytes.Contains(i) ||
                                     i >= HudLayoutPatch.FileOffset && i < HudLayoutPatch.FileOffset + HudLayoutPatch.Size),
            Is.True, "only the block and the hooks change");
        foreach (var h in HudLayoutPatch.Hooks)
        {
            Assert.That(BitConverter.ToUInt32(before, h.Va - 0xFF000), Is.EqualTo(h.Stock), h.What + " (game's word)");
            Assert.That(BitConverter.ToUInt32(with, h.Va - 0xFF000), Is.EqualTo(h.Patched), h.What + " (patch's word)");
        }

        exe.RemoveHudLayout();
        Assert.That(Bytes(exe), Is.EqualTo(before), "removal gives the three-player file back exactly");
        Assert.That(exe.GetHudLayoutState(), Is.EqualTo(HudLayoutState.Off));
    }

    [Test]
    public void SettingsRoundTrip()
    {
        var exe = ThreePlayers();
        var sample = Sample();
        exe.SetHudLayout(sample);
        var back = Reopen(exe).GetHudLayout();
        for (var p = 0; p < HudLayout.Players; p++)
        {
            foreach (var part in HudLayout.Parts)
            {
                Assert.That(back[p, part], Is.EqualTo(sample[p, part]), $"player {p + 1} {part}");
            }
        }

        // every size from 25 to 300 % reads back as written
        for (var pct = BgdaExecutable.MinHudSize; pct <= BgdaExecutable.MaxHudSize; pct++)
        {
            var l = new HudLayout { [0, HudPart.Panel] = new HudPlacement(0, 0, pct) };
            exe.SetHudLayout(l);
            Assert.That(exe.GetHudLayout()[0, HudPart.Panel].SizePercent, Is.EqualTo(pct));
        }

        // the game's own layout, with the patch in, reads back as the default
        exe.SetHudLayout(HudLayout.Default);
        Assert.That(exe.GetHudLayoutState(), Is.EqualTo(HudLayoutState.On));
        Assert.That(exe.GetHudLayout().IsDefault, Is.True);
    }

    [Test]
    public void TableEntries()
    {
        var exe = ThreePlayers();
        exe.SetHudLayout(Sample());
        var data = Bytes(exe);
        short S(int player, int part, int k) => BitConverter.ToInt16(data,
            HudLayoutPatch.FileOffset + HudLayoutPatch.TableOffset +
            (player * HudLayoutPatch.PartsPerPlayer + part) * HudLayoutPatch.EntrySize + k);

        // player 1's panel scales about the top-left corner: anchor (0, 0) -> (120, 60), 60 % = 154 / 256
        Assert.That(new[] { S(0, 0, 0), S(0, 0, 2), S(0, 0, 4), S(0, 0, 6), S(0, 0, 8) },
            Is.EqualTo(new short[] { 0, 0, 120, 60, 154 }));
        // player 2's skill bar from the bottom-right corner
        Assert.That(new[] { S(1, 3, 0), S(1, 3, 2), S(1, 3, 4), S(1, 3, 6), S(1, 3, 8) },
            Is.EqualTo(new short[] { 640, 448, 610, 368, 384 }));
        // player 3's icons and status icons: drawn off screen, both brought to the top centre and moved
        Assert.That(new[] { S(2, 1, 0), S(2, 1, 4), S(2, 1, 6) }, Is.EqualTo(new short[] { 1103, 320, 200 }));
        Assert.That(new[] { S(2, 4, 0), S(2, 4, 4), S(2, 4, 6) }, Is.EqualTo(new short[] { 939, 320, 200 }));
        // the current part starts unmarked
        Assert.That(BitConverter.ToInt32(data, HudLayoutPatch.FileOffset + 8), Is.EqualTo(-1));
    }

    [Test]
    public void BadValuesAreRefusedAndNothingIsWritten()
    {
        var exe = ThreePlayers();
        var before = Bytes(exe);
        foreach (var bad in new[]
                 {
                     new HudPlacement(641, 0, 100), new HudPlacement(-641, 0, 100), new HudPlacement(0, 449, 100),
                     new HudPlacement(0, -449, 100), new HudPlacement(0, 0, 24), new HudPlacement(0, 0, 301)
                 })
        {
            var l = new HudLayout { [2, HudPart.Skills] = bad };
            Assert.Throws<ArgumentException>(() => exe.SetHudLayout(l));
        }

        Assert.That(Bytes(exe), Is.EqualTo(before));
    }

    [Test]
    public void ThreePlayersOffTakesTheLayoutOut()
    {
        var stock = File.ReadAllBytes(_stockPath);
        var exe = ThreePlayers();
        exe.SetHudLayout(Sample());
        exe.DisableThreePlayers();
        Assert.That(Bytes(exe), Is.EqualTo(stock), "the stock executable comes back exactly");
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off));
    }

    [Test]
    public void ADamagedLayoutIsLeftAlone()
    {
        var exe = ThreePlayers();
        exe.SetHudLayout(Sample());
        var path = Path.Combine(_workDir, "damaged.72");
        exe.Save(path);
        var bytes = File.ReadAllBytes(path);
        var h = HudLayoutPatch.Hooks[3];
        BitConverter.GetBytes(h.Stock).CopyTo(bytes, h.Va - 0xFF000);   // one hook back to the game's word
        File.WriteAllBytes(path, bytes);

        var damaged = BgdaExecutable.Open(path);
        Assert.That(damaged.GetHudLayoutState(), Is.EqualTo(HudLayoutState.Unrecognized));
        Assert.Throws<InvalidOperationException>(() => damaged.SetHudLayout(HudLayout.Default));
        Assert.Throws<InvalidOperationException>(() => damaged.RemoveHudLayout());
        Assert.Throws<InvalidOperationException>(() => damaged.DisableThreePlayers());
        Assert.That(damaged.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On), "nothing was changed");
    }

    [Test]
    public void BlockAreaIsEmptyInStockAndThreePlayerFiles()
    {
        var stock = File.ReadAllBytes(_stockPath);
        Assert.That(stock.Skip(HudLayoutPatch.FileOffset).Take(HudLayoutPatch.Size).All(b => b == 0), Is.True);
        var three = Bytes(ThreePlayers());
        Assert.That(three.Skip(HudLayoutPatch.FileOffset).Take(HudLayoutPatch.Size).All(b => b == 0), Is.True);
    }
}
