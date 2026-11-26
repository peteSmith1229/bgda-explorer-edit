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
        Assert.That(exe.GetHudLayout().IsGameLayout, Is.True);
        Assert.That(exe.GetHudLayout().IsDefault, Is.False);
    }

    [Test]
    public void DefaultLayoutSwapsPlayers2And3()
    {
        var d = HudLayout.Default;
        foreach (var part in HudLayout.Parts)
        {
            var dx = part == HudPart.Gold ? 266 : 219;
            Assert.That(d[0, part], Is.EqualTo(new HudPlacement(0, 0, 100)), $"player 1 {part}");
            Assert.That(d[1, part], Is.EqualTo(new HudPlacement(-dx, 0, 100)), $"player 2 {part}");
            Assert.That(d[2, part], Is.EqualTo(new HudPlacement(dx, 0, 100)), $"player 3 {part}");
        }

        var exe = ThreePlayers();
        exe.SetHudLayout(d);
        Assert.That(exe.GetHudLayoutState(out var detail), Is.EqualTo(HudLayoutState.On));
        Assert.That(detail, Does.Contain("default layout"));
        Assert.That(Reopen(exe).GetHudLayout().IsDefault, Is.True);

        // where each player's anchor lands: player 1 the left edge, player 2 the centre, player 3 the right edge
        var data = Bytes(exe);
        short S(int player, int part, int k) => BitConverter.ToInt16(data,
            HudLayoutPatch.FileOffset + HudLayoutPatch.TableOffset +
            (player * HudLayoutPatch.PartsPerPlayer + part) * HudLayoutPatch.EntrySize + k);
        for (var p = 0; p < 3; p++)
        {
            for (var part = 0; part <= HudLayoutPatch.Effects; part++)
            {
                Assert.That(S(p, part, 4), Is.EqualTo(new short[] { 0, 320, 640 }[p]), $"player {p + 1} part {part} lands");
                Assert.That(S(p, part, 8), Is.EqualTo(256));
            }
        }

        // player 2's panel is drawn where player 3's was, and the other way round: the game's x base is 435 for
        // player 2 and 216 for player 3 (frame at base + 14); with size 100 % a point moves by N - A
        Assert.That(435 + 14 + (S(1, 0, 4) - S(1, 0, 0)), Is.EqualTo(216 + 14));
        Assert.That(216 + 14 + (S(2, 0, 4) - S(2, 0, 0)), Is.EqualTo(435 + 14));
        // the gold totals (game: 602 for player 2, 336 for player 3) trade places too
        Assert.That(602 + (S(1, 2, 4) - S(1, 2, 0)), Is.EqualTo(336));
        Assert.That(336 + (S(2, 2, 4) - S(2, 2, 0)), Is.EqualTo(602));
        // player 3's potion symbols (drawn at 1170) go where player 2's are drawn (606), his status icons (851)
        // where hers are (451)
        Assert.That(1170 + (S(2, 1, 4) - S(2, 1, 0)), Is.EqualTo(606));
        Assert.That(851 + (S(2, 4, 4) - S(2, 4, 0)), Is.EqualTo(451));
    }

    [Test]
    public void TheGamesPlacesWithThePatchIn()
    {
        var exe = ThreePlayers();
        exe.SetHudLayout(HudLayout.GameLayout);
        Assert.That(exe.GetHudLayoutState(out var detail), Is.EqualTo(HudLayoutState.On));
        Assert.That(detail, Does.Contain("game's own places"));
        Assert.That(Reopen(exe).GetHudLayout().IsGameLayout, Is.True);
    }

    [Test]
    public void FirstVersionFilesReadTheSame()
    {
        // the first version of the tab wrote every anchor where the game draws the part (the built-in table's)
        // and moved the target by the offset; such a file is still On and reads back its offsets
        var exe = ThreePlayers();
        exe.SetHudLayout(Sample());
        var path = Path.Combine(_workDir, "first.72");
        exe.Save(path);
        var bytes = File.ReadAllBytes(path);
        var block = Convert.FromBase64String(HudLayoutPatch.BlockBase64);
        var sample = Sample();
        for (var p = 0; p < 3; p++)
        {
            for (var part = 0; part <= HudLayoutPatch.Effects; part++)
            {
                var e = HudLayoutPatch.TableOffset + (p * HudLayoutPatch.PartsPerPlayer + part) * HudLayoutPatch.EntrySize;
                var v = sample[p, part == HudLayoutPatch.Effects ? HudPart.Icons : (HudPart)part];
                var at = HudLayoutPatch.FileOffset + e;
                Array.Copy(block, e, bytes, at, 4);
                BitConverter.GetBytes((short)(BitConverter.ToInt16(block, e + 4) + v.OffsetX)).CopyTo(bytes, at + 4);
                BitConverter.GetBytes((short)(BitConverter.ToInt16(block, e + 6) + v.OffsetY)).CopyTo(bytes, at + 6);
            }
        }

        File.WriteAllBytes(path, bytes);
        var first = BgdaExecutable.Open(path);
        Assert.That(first.GetHudLayoutState(), Is.EqualTo(HudLayoutState.On));
        var back = first.GetHudLayout();
        for (var p = 0; p < HudLayout.Players; p++)
        {
            foreach (var part in HudLayout.Parts)
            {
                Assert.That(back[p, part], Is.EqualTo(sample[p, part]), $"player {p + 1} {part}");
            }
        }
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

        // the default layout reads back as the default
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
        // player 2's skill bar about the bottom centre in the default layout: the game draws it 219 further right,
        // so the anchor is (539, 448) in the game's coordinates; -30 from the default (-219) puts it at 509
        Assert.That(new[] { S(1, 3, 0), S(1, 3, 2), S(1, 3, 4), S(1, 3, 6), S(1, 3, 8) },
            Is.EqualTo(new short[] { 539, 448, 509, 368, 384 }));
        // player 3's icons and status icons, drawn off screen by the game: at offset 0 under his panel in the
        // middle (the game's 1103 and 939 land on 320), anchored at the top-right corner of the default layout
        Assert.That(new[] { S(2, 1, 0), S(2, 1, 4), S(2, 1, 6) }, Is.EqualTo(new short[] { 1204, 421, 200 }));
        Assert.That(new[] { S(2, 4, 0), S(2, 4, 4), S(2, 4, 6) }, Is.EqualTo(new short[] { 1040, 421, 200 }));
        Assert.That(421 + (1103 - 1204), Is.EqualTo(320));
        Assert.That(421 + (939 - 1040), Is.EqualTo(320));
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
