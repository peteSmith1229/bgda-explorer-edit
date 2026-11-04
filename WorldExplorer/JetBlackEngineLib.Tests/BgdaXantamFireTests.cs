using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Xantam's Fire, the Enhancements tab (BgdaExecutable.XantamFire.cs): on the stock PAL executable in Test_files
/// (skipped when it is not present), with the Three Players tab switching three players underneath it; its names in
/// GTEXT.LMP and its icon in ICONS.LMP (archives built here). The patch is featpatch.py version 4: the checksums are of
/// the files featpatch.py writes, and what it does in the game was checked by emulation (test_feats.py, 117 checks on
/// each of six files; 32 broken copies all caught).
/// </summary>
[TestFixture]
public class BgdaXantamFireTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>featpatch.py version 4 on the stock executable: the tested settings, and price 0 with 4 mana (the test files').</summary>
    private const string PatchedMd5 = "4f51329f3b6e7f5840e2aef4381a3a2f", TestSettingsMd5 = "328253ea217e8488b356b40b6debb452";

    /// <summary>featpatch.py version 4 on ADRIANNA26 (the stock file with three players turned on).</summary>
    private const string ThreePlayersMd5 = "b3ac7347d839ceccd3eb02dae394f721";

    /// <summary>The six words that send the game to it, and its two stretches of the pool (data, then code and vtables).</summary>
    private static readonly int[] Hooks = { 0x0018331C, 0x0017A4BC, 0x00307BE4, 0x001B177C, 0x001B1784, 0x0016DD44 };

    private const int DataVa = 0x00132D90, DataEnd = 0x00133390, CodeVa = 0x00221B88, CodeEnd = 0x002222F0;

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
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaXantamFireTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    private BgdaExecutable Stock() => BgdaExecutable.Open(_stockPath);

    private byte[] Bytes(BgdaExecutable exe, string name)
    {
        var path = Path.Combine(_workDir, name);
        exe.Save(path);
        return File.ReadAllBytes(path);
    }

    private BgdaExecutable OpenBytes(byte[] bytes, string name)
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllBytes(path, bytes);
        return BgdaExecutable.Open(path);
    }

    private static string Md5(byte[] data) => Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();

    private void RequireStock()
    {
        if (Md5(_stockBytes) != StockMd5) Assert.Ignore("Test_files/SLES_506.72 isn't the stock PAL executable.");
    }

    private static readonly XantamFireSettings TestSettings = new(0, 4f, XantamFireSettings.Default.Damage);

    // ═══════════════════════════════ the patch ═══════════════════════════════

    [Test]
    public void TheStockExecutable_TakesXantamsFire_AsFeatpatchWritesIt()
    {
        RequireStock();
        var exe = Stock();
        Assert.That(exe.GetXantamFireState(out var detail), Is.EqualTo(XantamFireState.Off), detail);
        exe.SetXantamFire(XantamFireSettings.Default);
        var on = Bytes(exe, "on");
        Assert.That(Md5(on), Is.EqualTo(PatchedMd5), "the same bytes as featpatch.py");
        Assert.That(on.Length, Is.EqualTo(_stockBytes.Length), "the file doesn't grow");
        var reopened = OpenBytes(on, "on2");
        Assert.That(reopened.GetXantamFireState(out detail), Is.EqualTo(XantamFireState.On), detail);
        Assert.That(reopened.GetXantamFireSettings(), Is.EqualTo(XantamFireSettings.Default));
        Assert.That(reopened.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off), "the Three Players tab still sees the stock file");

        reopened.RemoveXantamFire();
        Assert.That(Bytes(reopened, "off"), Is.EqualTo(_stockBytes), "taken out: the stock file, byte for byte");

        exe = Stock();
        exe.SetXantamFire(TestSettings);
        Assert.That(Md5(Bytes(exe, "test")), Is.EqualTo(TestSettingsMd5), "price 0 and 4 mana: featpatch.py --price 0 --energy 4");
    }

    [Test]
    public void ItChangesOnlyItsHooksAndTwoStretchesOfUnusedCode()
    {
        RequireStock();
        var exe = Stock();
        exe.SetXantamFire(XantamFireSettings.Default);
        var bytes = Bytes(exe, "where");
        var changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => i + VaToFile).ToList();
        bool Hook(int va) => Hooks.Any(h => h <= va && va < h + 4);
        Assert.That(changed.All(va => Hook(va) || DataVa <= va && va < DataEnd || CodeVa <= va && va < CodeEnd), Is.True);
        Assert.That(Hooks.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True,
                    "all six hooks");
        Assert.That(bytes.Take(0x100), Is.EqualTo(_stockBytes.Take(0x100)), "the ELF header and program headers are the game's");
        Assert.That(BgdaExecutable.XantamFireBytes, Is.EqualTo(DataEnd - DataVa + CodeEnd - CodeVa));
        Assert.That(BgdaExecutable.FeatPoolBytes, Is.EqualTo(32724));
        Assert.That(BgdaExecutable.FeatPoolStretches, Is.EqualTo(61));
    }

    [Test]
    public void ThreePlayers_CanBeTurnedOnAndOffUnderneathIt()
    {
        RequireStock();
        var exe = Stock();
        exe.SetXantamFire(XantamFireSettings.Default);
        var withFire = Bytes(exe, "fire");
        exe.EnableThreePlayers();
        var three = Bytes(exe, "fire-three");
        Assert.That(Md5(three), Is.EqualTo(ThreePlayersMd5), "featpatch.py's file on ADRIANNA26");
        Assert.That(exe.GetXantamFireState(), Is.EqualTo(XantamFireState.On));
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));

        var other = Stock();
        other.EnableThreePlayers();
        other.SetXantamFire(XantamFireSettings.Default);
        Assert.That(Bytes(other, "three-fire"), Is.EqualTo(three), "in the other order: the same file");

        exe.DisableThreePlayers();
        Assert.That(Bytes(exe, "fire-again"), Is.EqualTo(withFire), "turned off again: Xantam's Fire on the stock file");
    }

    [Test]
    public void TheThreePlayerPatch_NeverWritesInThePool()
    {
        RequireStock();
        var exe = Stock();
        exe.EnableThreePlayers();
        var three = Bytes(exe, "three-pool");
        var pool = typeof(BgdaExecutable).Assembly.GetType("JetBlackEngineLib.Data.Executable.FeatPatch")!
            .GetField("Pool")!.GetValue(null) as (int Start, int End)[];
        Assert.That(pool, Is.Not.Null);
        foreach (var (start, end) in pool!)
        {
            Assert.That(three.AsSpan(start - VaToFile, end - start).SequenceEqual(_stockBytes.AsSpan(start - VaToFile, end - start)), Is.True,
                $"0x{start:X8}-0x{end:X8}");
        }

        foreach (var va in Hooks)
            Assert.That(BitConverter.ToUInt32(three, va - VaToFile), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, va - VaToFile)), $"hook 0x{va:X8}");
    }

    [Test]
    public void ItsSettings_AreReadBack_AndChangedInPlace()
    {
        RequireStock();
        var custom = new XantamFireSettings(7, 9.5f, new XantamFireDamage[] { new(4, 9), new(0, 0), new(10, 200), new(255, 255), new(1, 2) });
        var exe = Stock();
        exe.SetXantamFire(custom);
        var reopened = OpenBytes(Bytes(exe, "custom"), "custom2");
        Assert.That(reopened.GetXantamFireSettings(), Is.EqualTo(custom));
        reopened.SetXantamFire(XantamFireSettings.Default);
        Assert.That(Md5(Bytes(reopened, "custom-back")), Is.EqualTo(PatchedMd5));

        Assert.That(XantamFireSettings.RankCost(16, 0), Is.EqualTo(16));
        Assert.That(Enumerable.Range(0, 5).Select(h => XantamFireSettings.RankCost(16, h)), Is.EqualTo(new[] { 16, 17, 19, 21, 25 }));
        Assert.That(XantamFireSettings.RankCost(3, 2), Is.EqualTo(9), "below 5: (held + 1) x price");
    }

    [Test]
    public void BadSettings_AreRefused_AndNothingIsWritten()
    {
        RequireStock();
        var exe = Stock();
        var d = XantamFireSettings.Default;
        var five = new XantamFireDamage[] { new(1, 2), new(1, 3), new(2, 4), new(2, 5), new(3, 6) };
        var bad = new[]
        {
            d with { Price = -1 }, d with { Price = 31 }, d with { Mana = 0f }, d with { Mana = 100.5f }, d with { Mana = float.PositiveInfinity },
            d with { Damage = five.Select((x, i) => i == 2 ? new XantamFireDamage(5, 4) : x).ToArray() },
            d with { Damage = five.Select((x, i) => i == 4 ? new XantamFireDamage(0, 256) : x).ToArray() },
            d with { Damage = five.Take(4).ToArray() }
        };
        foreach (var s in bad)
        {
            Assert.That(s.Problem(), Is.Not.Null);
            Assert.Throws<ArgumentException>(() => exe.SetXantamFire(s));
        }

        Assert.That(Bytes(exe, "bad"), Is.EqualTo(_stockBytes));
        Assert.That(XantamFireSettings.Default.Problem(), Is.Null);
    }

    [Test]
    public void AFileItDoesntKnow_IsLeftAlone()
    {
        RequireStock();
        var hooked = (byte[])_stockBytes.Clone();
        hooked[0x0017A4BC - VaToFile] ^= 1;
        var exe = OpenBytes(hooked, "hooked");
        Assert.That(exe.GetXantamFireState(out var detail), Is.EqualTo(XantamFireState.Unrecognized));
        Assert.That(detail, Does.Contain("0x0017A4BC"));
        Assert.Throws<InvalidOperationException>(() => exe.SetXantamFire(XantamFireSettings.Default));
        Assert.Throws<InvalidOperationException>(() => exe.RemoveXantamFire());
        Assert.That(Bytes(exe, "hooked-after"), Is.EqualTo(hooked));

        var used = (byte[])_stockBytes.Clone();
        used[CodeVa + 0x100 - VaToFile] ^= 0x10;
        Assert.That(OpenBytes(used, "used").GetXantamFireState(out detail), Is.EqualTo(XantamFireState.Unrecognized));
        Assert.That(detail, Does.Contain("another patch"));

        var off = Stock();
        off.RemoveXantamFire();
        Assert.That(Bytes(off, "off-twice"), Is.EqualTo(_stockBytes), "taking it out of a file without it changes nothing");
    }

    // ═══════════════════════════════ the names ═══════════════════════════════

    private static byte[] SmallGameText()
    {
        var texts = Enumerable.Range(0, 5).Select(i => new List<(string, string)> { ("burninghands_name", $"Burning Hands {i}"), ("odd", "x") }).ToArray();
        return LmpWriter.PackEntries(EngineVersion.DarkAlliance,
            GameText.Files.Select((f, i) => (f.File, new UniTextFile(texts[i]).ToArray())).ToList());
    }

    [Test]
    public void TheNames_GoInEveryLanguage_AndComeOutAgain()
    {
        var original = SmallGameText();
        var text = GameText.Read(original);
        Assert.That(XantamFire.HasNames(text) || XantamFire.HasAnyName(text), Is.False);
        XantamFire.AddNames(text);
        var saved = GameText.Read(text.ToArray());
        Assert.That(XantamFire.HasNames(saved), Is.True);
        Assert.That(saved.Get("xantamfire_name"), Is.EqualTo("Xantam's Fire"));
        Assert.That(saved.Get("xantamfire_name", 2), Is.EqualTo("Xantams Feuer"));
        for (var language = 0; language < 5; language++)
        {
            var keys = saved.Entries(language).Select(e => e.Key).ToList();
            Assert.That(keys, Is.EqualTo(new[] { "burninghands_name", "odd", "xantamfire_name", "xantamfire_desc" }), "after the file's own entries");
        }

        Assert.That(XantamFire.Names.All(n => n.Text.All(c => GameText.TextCharacters.Contains(c))), Is.True, "letters the game's font draws");
        XantamFire.AddNames(saved);
        Assert.That(saved.Changed, Is.False, "adding them again changes nothing");
        XantamFire.RemoveNames(saved);
        Assert.That(saved.ToArray(), Is.EqualTo(original));
    }

    // ═══════════════════════════════ the icon and ICONS.LMP ═══════════════════════════════

    /// <summary>An archive: the entries in this order, each file at the next multiple of 0x80, the directory with room for <paramref name="spare"/> more.</summary>
    private static byte[] Archive(IReadOnlyList<(string Name, byte[] Data)> entries, int spare)
    {
        var at = (4 + 64 * (entries.Count + spare) + 0x7F) & ~0x7F;
        var output = new List<byte>(new byte[at]);
        var dir = new List<(int, int)>();
        foreach (var (_, data) in entries)
        {
            while (output.Count % 0x80 != 0) output.Add(0);
            dir.Add((output.Count, data.Length));
            output.AddRange(data);
        }

        var bytes = output.ToArray();
        BitConverter.GetBytes(entries.Count).CopyTo(bytes, 0);
        for (var i = 0; i < entries.Count; i++)
        {
            Encoding.ASCII.GetBytes(entries[i].Name).CopyTo(bytes, 4 + 64 * i);
            BitConverter.GetBytes(dir[i].Item1).CopyTo(bytes, 4 + 64 * i + 56);
            BitConverter.GetBytes(dir[i].Item2).CopyTo(bytes, 4 + 64 * i + 60);
        }

        return bytes;
    }

    private static byte[] Fill(int n, byte b) => Enumerable.Repeat(b, n).ToArray();

    [Test]
    public void TheIcon_GoesIntoAFullIconsLmp_AndComesOutAgain()
    {
        var icon = XantamFireIcon.Texture();
        Assert.That(icon.Length, Is.EqualTo(3568));
        Assert.That(Md5(icon), Is.EqualTo("10628d23903f20cfe1fb1ab5e2989450"), "make_xantam_icon.py's texture");

        // like the game's ICONS.LMP: sorted, the directory full (no room before the first file)
        var icons = Archive(new[]
        {
            ("icon_healthpotion.tex", Fill(300, 1)), ("icon_protection.tex", Fill(200, 2)), ("icon_whirlwind.tex", Fill(129, 3)),
            ("levelup.tex", Fill(70, 4)), ("nomana.tex", Fill(10, 5))
        }, spare: 0);
        Assert.That(GameArchive.Problem(icons), Is.Null);
        Assert.Throws<InvalidDataException>(() => GameArchive.Insert(icons, XantamFireIcon.EntryName, icon)); // no room without growing the directory

        var added = XantamFireIcon.AddTo(icons);
        Assert.That(GameArchive.Problem(added), Is.Null, "the game reads it as it should");
        var entries = GameArchive.ReadDirectory(added);
        Assert.That(entries.Select(e => e.Name), Is.EqualTo(new[]
        {
            "icon_healthpotion.tex", "icon_protection.tex", "icon_whirlwind.tex", "icon_xantamfire.tex", "levelup.tex", "nomana.tex"
        }));
        Assert.That(GameArchive.ReadDirectory(icons).Min(e => e.Offset) + 0x80, Is.EqualTo(entries.Min(e => e.Offset)), "every file moved on by 0x80");
        Assert.That(GameArchive.AsLoaded(added).Where(f => f.Name != XantamFireIcon.EntryName).Select(f => f.Data),
                    Is.EqualTo(GameArchive.AsLoaded(icons).Select(f => f.Data)), "every other file as it was");
        Assert.That(GameArchive.AsLoaded(added).Single(f => f.Name == XantamFireIcon.EntryName).Data, Is.EqualTo(icon));
        Assert.That(XantamFireIcon.IsIn(added) && !XantamFireIcon.IsIn(icons), Is.True);
        Assert.That(XantamFireIcon.AddTo(added), Is.EqualTo(added), "adding it again changes nothing");

        Assert.That(XantamFireIcon.RemoveFrom(added), Is.EqualTo(icons), "taken out: the archive as it was, byte for byte");
        Assert.That(XantamFireIcon.RemoveFrom(icons), Is.EqualTo(icons));
        Assert.That(XantamFireIcon.Problem(Archive(new[] { ("tinyab.tex", Fill(10, 1)) }, spare: 1)), Does.Contain("isn't ICONS.LMP"));
    }
}
