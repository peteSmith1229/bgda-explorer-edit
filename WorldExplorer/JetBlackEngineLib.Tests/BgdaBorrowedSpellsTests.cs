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
/// The borrowed spells, the Enhancements tab (BgdaExecutable.BorrowedSpells.cs): Xantam's Fire for Adrianna; Blade Barrier,
/// Antimagic Field and Righteous Might for Kromlech. On the stock PAL executable in Test_files (skipped when it is not
/// present), with the Three Players tab switching three players underneath them; their names in GTEXT.LMP and their icons
/// in ICONS.LMP (archives built here). The patch is featpatch.py version 5: the checksums are of the files featpatch.py
/// writes, and what it does in the game was checked by emulation (test_feats5.py on seven files: 168 checks on each with
/// all four; 40 broken copies, all caught on two bases).
/// </summary>
[TestFixture]
public class BgdaBorrowedSpellsTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>featpatch.py version 5 on the stock executable: all four at the tested settings, at the test files' (price 0, 4 mana), and Kromlech's three alone.</summary>
    private const string AllFourMd5 = "64c66cca454d8dbf6e3883b7db97dd76", TestSettingsMd5 = "c4233a5036b8bbf5db3fb0e00d801b0b",
                         KromlechMd5 = "184d444b91e73ba512aedb018ddb2f90";

    /// <summary>featpatch.py version 5 (all four) on ADRIANNA26 (the stock file with three players turned on).</summary>
    private const string ThreePlayersMd5 = "49311d2f8b215dffbe71e88bd54d8b9d";

    /// <summary>The words that send the game to it (all four on), and the stretches of the pool it takes.</summary>
    private static readonly int[] Hooks =
    {
        0x00161EF8, 0x00161F00, 0x00161F08, 0x00167AFC, 0x0016DD44, 0x0017A4BC, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C,
        0x001B177C, 0x001B1784, 0x0030A104, 0x0030A11C
    };

    /// <summary>Xantam's Fire's two (its maker's trail vtable); none of Kromlech's spells needs them.</summary>
    private static readonly int[] XantamOnly = { 0x001B177C, 0x001B1784 };

    private static readonly (int Start, int End)[] Used =
    {
        (0x00102910, 0x00102B70), (0x00102D80, 0x00103000), (0x0010C338, 0x0010C6C8), (0x0010CD50, 0x0010CF3C), (0x00118978, 0x00118BA4),
        (0x00118BB0, 0x00118D5C), (0x00129318, 0x0012951C), (0x00129C48, 0x00129D60), (0x0012A710, 0x0012A86C), (0x0012BAE8, 0x0012BC44),
        (0x0012BC50, 0x0012BDA8), (0x00132D90, 0x001332B0)
    };

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
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaBorrowedSpellsTests-" + Guid.NewGuid().ToString("N"));
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

    private static readonly BorrowedSpell[] Kromlech = { BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField, BorrowedSpell.RighteousMight };

    private static Dictionary<BorrowedSpell, BorrowedSpellSettings> Defaults(IEnumerable<BorrowedSpell> spells) =>
        spells.ToDictionary(s => s, s => BorrowedSpells.Info(s).Default);

    private static void AssertSame(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> got, IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> want)
    {
        Assert.That(got.Keys.OrderBy(k => k), Is.EqualTo(want.Keys.OrderBy(k => k)));
        foreach (var (spell, settings) in want) Assert.That(got[spell].Equals(settings), Is.True, spell.ToString());
    }

    // ═══════════════════════════════ the patch ═══════════════════════════════

    [Test]
    public void TheStockExecutable_TakesTheSpells_AsFeatpatchWritesThem()
    {
        RequireStock();
        var exe = Stock();
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Off), detail);
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var on = Bytes(exe, "on");
        Assert.That(Md5(on), Is.EqualTo(AllFourMd5), "the same bytes as featpatch.py");
        Assert.That(on.Length, Is.EqualTo(_stockBytes.Length), "the file doesn't grow");
        var reopened = OpenBytes(on, "on2");
        Assert.That(reopened.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        AssertSame(reopened.GetBorrowedSpells(), Defaults(BorrowedSpells.All));
        Assert.That(reopened.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off), "the Three Players tab still sees the stock file");

        reopened.RemoveBorrowedSpells();
        Assert.That(Bytes(reopened, "off"), Is.EqualTo(_stockBytes), "taken out: the stock file, byte for byte");

        exe = Stock();
        exe.SetBorrowedSpells(BorrowedSpells.All.ToDictionary(s => s, BorrowedSpells.TestSettings));
        Assert.That(Md5(Bytes(exe, "test")), Is.EqualTo(TestSettingsMd5), "price 0 and 4 mana: featpatch.py's test settings (without the tripwire)");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Kromlech));
        Assert.That(Md5(Bytes(exe, "krom")), Is.EqualTo(KromlechMd5), "Kromlech's three alone: featpatch.py --spells bladebarrier,antimagic,righteousmight");
    }

    [Test]
    public void ItChangesOnlyItsHooksAndUnusedCode()
    {
        RequireStock();
        var exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var bytes = Bytes(exe, "where");
        var changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => i + VaToFile).ToList();
        bool Hook(int va) => Hooks.Any(h => h <= va && va < h + 4);
        Assert.That(changed.All(va => Hook(va) || Used.Any(u => u.Start <= va && va < u.End)), Is.True);
        Assert.That(Hooks.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True,
                    "all fourteen hooks");
        Assert.That(bytes.Take(0x100), Is.EqualTo(_stockBytes.Take(0x100)), "the ELF header and program headers are the game's");
        Assert.That(BgdaExecutable.BorrowedSpellsBytes, Is.EqualTo(Used.Sum(u => u.End - u.Start)));
        Assert.That(BgdaExecutable.FeatPoolBytes, Is.EqualTo(32724));
        Assert.That(BgdaExecutable.FeatPoolStretches, Is.EqualTo(61));

        // Kromlech's three alone: Xantam's Fire's two hooks stay the game's
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Kromlech));
        bytes = Bytes(exe, "where-krom");
        Assert.That(XantamOnly.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) == BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True);
    }

    [Test]
    public void ThreePlayers_CanBeTurnedOnAndOffUnderneathThem()
    {
        RequireStock();
        var exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var withSpells = Bytes(exe, "spells");
        exe.EnableThreePlayers();
        var three = Bytes(exe, "spells-three");
        Assert.That(Md5(three), Is.EqualTo(ThreePlayersMd5), "featpatch.py's file on ADRIANNA26");
        Assert.That(exe.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));

        var other = Stock();
        other.EnableThreePlayers();
        other.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Bytes(other, "three-spells"), Is.EqualTo(three), "in the other order: the same file");

        exe.DisableThreePlayers();
        Assert.That(Bytes(exe, "spells-again"), Is.EqualTo(withSpells), "turned off again: the spells on the stock file");
    }

    [Test]
    public void TheThreePlayerPatch_NeverWritesInThePoolOrTheHooks()
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
    public void TheirSettings_AreReadBack_AndChangedInPlace()
    {
        RequireStock();
        var custom = new Dictionary<BorrowedSpell, BorrowedSpellSettings>
        {
            [BorrowedSpell.XantamFire] = new(7, 9.5f, new[] { 4, 0, 10, 255, 1 }, new[] { 9, 0, 200, 255, 2 }),
            [BorrowedSpell.AntimagicField] = new(30, 100f, new[] { 1, 60, 2, 3, 4 }, Array.Empty<int>()),
            [BorrowedSpell.RighteousMight] = new(0, 0.5f, new[] { 0, 200, 5, 6, 7 }, new[] { 90, 0, 1, 2, 3 })
        };
        var exe = Stock();
        exe.SetBorrowedSpells(custom);
        var reopened = OpenBytes(Bytes(exe, "custom"), "custom2");
        AssertSame(reopened.GetBorrowedSpells(), custom);

        // a spell added and one taken out in place, then back to all four at the tested settings
        var changed = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(custom) { [BorrowedSpell.BladeBarrier] = BorrowedSpells.Info(BorrowedSpell.BladeBarrier).Default };
        changed.Remove(BorrowedSpell.XantamFire);
        reopened.SetBorrowedSpells(changed);
        AssertSame(OpenBytes(Bytes(reopened, "changed"), "changed2").GetBorrowedSpells(), changed);
        reopened.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Md5(Bytes(reopened, "custom-back")), Is.EqualTo(AllFourMd5));

        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(16, h)), Is.EqualTo(new[] { 16, 17, 19, 21, 25 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(10, h)), Is.EqualTo(new[] { 10, 11, 13, 15, 19 }));
        Assert.That(BorrowedSpellSettings.RankCost(3, 2), Is.EqualTo(9), "below 5: (held + 1) x price");
    }

    [Test]
    public void BadSettings_AreRefused_AndNothingIsWritten()
    {
        RequireStock();
        var exe = Stock();
        var x = BorrowedSpells.Info(BorrowedSpell.XantamFire).Default;
        var am = BorrowedSpells.Info(BorrowedSpell.AntimagicField).Default;
        var rm = BorrowedSpells.Info(BorrowedSpell.RighteousMight).Default;
        var bad = new (BorrowedSpell, BorrowedSpellSettings)[]
        {
            (BorrowedSpell.XantamFire, x with { Price = -1 }), (BorrowedSpell.XantamFire, x with { Price = 31 }),
            (BorrowedSpell.XantamFire, x with { Mana = 0f }), (BorrowedSpell.XantamFire, x with { Mana = 100.5f }),
            (BorrowedSpell.XantamFire, x with { Mana = float.PositiveInfinity }),
            (BorrowedSpell.BladeBarrier, x with { First = new[] { 1, 1, 5, 2, 3 } }), (BorrowedSpell.BladeBarrier, x with { Second = new[] { 2, 3, 4, 5, 256 } }),
            (BorrowedSpell.BladeBarrier, x with { First = new[] { 1, 1, 2, 2 } }),
            (BorrowedSpell.AntimagicField, am with { First = new[] { 4, 5, 0, 7, 8 } }), (BorrowedSpell.AntimagicField, am with { Second = new[] { 1, 2, 3, 4, 5 } }),
            (BorrowedSpell.RighteousMight, rm with { First = new[] { 10, 20, 30, 40, 201 } }), (BorrowedSpell.RighteousMight, rm with { Second = new[] { 91, 20, 30, 40, 50 } })
        };
        foreach (var (spell, s) in bad)
        {
            Assert.That(s.Problem(spell), Is.Not.Null);
            Assert.Throws<ArgumentException>(() => exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [spell] = s }));
        }

        Assert.That(Bytes(exe, "bad"), Is.EqualTo(_stockBytes));
        Assert.That(BorrowedSpells.All.All(s => BorrowedSpells.Info(s).Default.Problem(s) == null), Is.True);
    }

    [Test]
    public void AFileItDoesntKnow_IsLeftAlone()
    {
        RequireStock();
        var hooked = (byte[])_stockBytes.Clone();
        hooked[0x0017A4BC - VaToFile] ^= 1;
        var exe = OpenBytes(hooked, "hooked");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        Assert.That(detail, Does.Contain("0x0017A4BC"));
        Assert.Throws<InvalidOperationException>(() => exe.SetBorrowedSpells(Defaults(BorrowedSpells.All)));
        Assert.Throws<InvalidOperationException>(() => exe.RemoveBorrowedSpells());
        Assert.That(Bytes(exe, "hooked-after"), Is.EqualTo(hooked));

        var used = (byte[])_stockBytes.Clone();
        used[0x0012BAE8 + 0x20 - VaToFile] ^= 0x10;
        Assert.That(OpenBytes(used, "used").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        Assert.That(detail, Does.Contain("another patch"));

        var off = Stock();
        off.RemoveBorrowedSpells();
        Assert.That(Bytes(off, "off-twice"), Is.EqualTo(_stockBytes), "taking them out of a file without them changes nothing");
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
        Assert.That(BorrowedSpells.All.Any(s => BorrowedSpellTexts.HasNames(text, s) || BorrowedSpellTexts.HasAnyName(text, s)), Is.False);
        BorrowedSpellTexts.AddNames(text, BorrowedSpells.All);
        var saved = GameText.Read(text.ToArray());
        Assert.That(BorrowedSpells.All.All(s => BorrowedSpellTexts.HasNames(saved, s)), Is.True);
        Assert.That(saved.Get("xantamfire_name"), Is.EqualTo("Xantam's Fire"));
        Assert.That(saved.Get("bladebarrier_name"), Is.EqualTo("Blade Barrier"));
        Assert.That(saved.Get("antimagicfield_name", 2), Is.EqualTo("Antimagisches Feld"));
        Assert.That(saved.Get("righteousmight_hud"), Is.EqualTo("+%d%% damage, -%d%% damage taken"), "the HUD's line: %d the rank's values, %% a percent sign");
        for (var language = 0; language < 5; language++)
        {
            var keys = saved.Entries(language).Select(e => e.Key).ToList();
            Assert.That(keys, Is.EqualTo(new[]
            {
                "burninghands_name", "odd", "xantamfire_name", "xantamfire_desc", "bladebarrier_name", "bladebarrier_desc",
                "antimagicfield_name", "antimagicfield_desc", "antimagicfield_hud", "righteousmight_name", "righteousmight_desc", "righteousmight_hud"
            }), "after the file's own entries, spell by spell");
            foreach (var hud in new[] { "antimagicfield_hud", "righteousmight_hud" })
                Assert.That(saved.Get(hud, language)!.TrimEnd().EndsWith("%"), Is.False, "a HUD line ends in plain text (the game's formatter reads on past a line ending in a %-code)");
        }

        Assert.That(BorrowedSpells.All.SelectMany(BorrowedSpellTexts.Of).All(n => n.Text.All(c => GameText.TextCharacters.Contains(c))), Is.True,
                    "letters the game's font draws");
        BorrowedSpellTexts.AddNames(saved, BorrowedSpells.All);
        Assert.That(saved.Changed, Is.False, "adding them again changes nothing");
        BorrowedSpellTexts.RemoveNames(saved, BorrowedSpells.All);
        Assert.That(saved.ToArray(), Is.EqualTo(original));

        text = GameText.Read(original);
        BorrowedSpellTexts.AddNames(text, new[] { BorrowedSpell.RighteousMight, BorrowedSpell.XantamFire });
        Assert.That(text.Entries(0).Select(e => e.Key).Skip(2).First(), Is.EqualTo("xantamfire_name"), "in the spells' order, whatever order they are asked in");
        Assert.That(BorrowedSpellTexts.HasAnyName(text, BorrowedSpell.BladeBarrier), Is.False);
    }

    // ═══════════════════════════════ the icons and ICONS.LMP ═══════════════════════════════

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
    public void TheIcons_GoIntoAFullIconsLmp_AndComeOutAgain()
    {
        var md5 = new Dictionary<BorrowedSpell, string>
        {
            [BorrowedSpell.XantamFire] = "10628d23903f20cfe1fb1ab5e2989450", [BorrowedSpell.BladeBarrier] = "f586c2f47c445397ff313207706965db",
            [BorrowedSpell.AntimagicField] = "c6d39e2cd0b3573358519a201bd9a6d9", [BorrowedSpell.RighteousMight] = "bd6f5fbeff4c9015adc061080c9912a1"
        };
        foreach (var spell in BorrowedSpells.All)
        {
            Assert.That(BorrowedSpellIcons.Texture(spell).Length, Is.EqualTo(3568));
            Assert.That(Md5(BorrowedSpellIcons.Texture(spell)), Is.EqualTo(md5[spell]), "make_xantam_icon.py's and make_kromlech_icons.py's textures");
        }

        // like the game's ICONS.LMP: sorted, the directory full (no room before the first file)
        var icons = Archive(new[]
        {
            ("exclaim.tex", Fill(90, 9)), ("icon_balllightning.tex", Fill(60, 8)), ("icon_healthpotion.tex", Fill(300, 1)),
            ("icon_protection.tex", Fill(200, 2)), ("icon_quake.tex", Fill(40, 6)), ("icon_whirlwind.tex", Fill(129, 3)),
            ("levelup.tex", Fill(70, 4)), ("nomana.tex", Fill(10, 5))
        }, spare: 0);
        Assert.That(GameArchive.Problem(icons), Is.Null);

        var added = BorrowedSpellIcons.AddTo(icons, BorrowedSpells.All);
        Assert.That(GameArchive.Problem(added), Is.Null, "the game reads it as it should");
        Assert.That(GameArchive.ReadDirectory(added).Select(e => e.Name), Is.EqualTo(new[]
        {
            "exclaim.tex", "icon_antimagicfield.tex", "icon_balllightning.tex", "icon_bladebarrier.tex", "icon_healthpotion.tex", "icon_protection.tex",
            "icon_quake.tex", "icon_righteousmight.tex", "icon_whirlwind.tex", "icon_xantamfire.tex", "levelup.tex", "nomana.tex"
        }), "each where its name sorts");
        Assert.That(GameArchive.AsLoaded(added).Where(f => !f.Name.StartsWith("icon_") || f.Name is "icon_healthpotion.tex" or "icon_protection.tex" or
                                                        "icon_whirlwind.tex" or "icon_balllightning.tex" or "icon_quake.tex").Select(f => f.Data),
                    Is.EqualTo(GameArchive.AsLoaded(icons).Select(f => f.Data)), "every other file as it was");
        foreach (var spell in BorrowedSpells.All)
        {
            Assert.That(GameArchive.AsLoaded(added).Single(f => f.Name == BorrowedSpellIcons.EntryName(spell)).Data, Is.EqualTo(BorrowedSpellIcons.Texture(spell)));
            Assert.That(BorrowedSpellIcons.IsIn(added, spell) && !BorrowedSpellIcons.IsIn(icons, spell), Is.True);
        }

        Assert.That(BorrowedSpellIcons.AddTo(added, BorrowedSpells.All), Is.EqualTo(added), "adding them again changes nothing");
        Assert.That(BorrowedSpellIcons.RemoveFrom(added, BorrowedSpells.All), Is.EqualTo(icons), "taken out: the archive as it was, byte for byte");
        Assert.That(BorrowedSpellIcons.RemoveFrom(added, new[] { BorrowedSpell.BladeBarrier }),
                    Is.EqualTo(BorrowedSpellIcons.AddTo(icons, BorrowedSpells.All.Where(s => s != BorrowedSpell.BladeBarrier))),
                    "one taken out: as if it had never gone in");
        Assert.That(BorrowedSpellIcons.RemoveFrom(icons, BorrowedSpells.All), Is.EqualTo(icons));
        Assert.That(BorrowedSpellIcons.Problem(Archive(new[] { ("tinyab.tex", Fill(10, 1)) }, spare: 1)), Does.Contain("isn't ICONS.LMP"));
    }
}
