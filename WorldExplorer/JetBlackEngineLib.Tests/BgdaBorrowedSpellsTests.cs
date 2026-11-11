using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The borrowed spells, the Enhancements tab (BgdaExecutable.BorrowedSpells.cs): Xantam's Fire, Sorcerer's Blood (the game's own
/// feat) and Xantam's Orbs for Adrianna; Power Attack (the game's own feat), Blade Barrier, Antimagic Field and Righteous Might for
/// Kromlech; Inferno Arrow (the game's own cut feat) and Entangle (the game's own unused vines) for Vahn. On the stock PAL
/// executable in Test_files (skipped when it is not present), with the Three Players tab switching three players underneath them,
/// and versions 10, 9, 8, 7, 6 and 5 (the first test of Inferno Arrow, the second and first of Xantam's Orbs, the first of Sorcerer's
/// Blood, of Power Attack and of Kromlech's spells) brought up to date; their names in GTEXT.LMP and their icons in ICONS.LMP
/// (archives built here). The patch is featpatch.py version 11 (Entangle; its second data block at the start of the pool's stretch
/// 60): the checksums are of the files featpatch.py writes, and what it does in the game was checked by emulation (test_feats11.py
/// on twenty-six files: 297 checks on each with all nine; 234 broken copies on two bases).
/// </summary>
[TestFixture]
public class BgdaBorrowedSpellsTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>
    /// featpatch.py version 11 on the stock executable: all nine at the tested settings, all but Entangle (the eight of version 10),
    /// all but Inferno Arrow and Entangle (the seven of version 9), all nine at the test files' settings (price 0, 4 mana), Kromlech's
    /// four alone, Power Attack alone, Sorcerer's Blood alone, Sorcerer's Blood alone with 3 ranks at price 4, Xantam's Orbs alone,
    /// Xantam's Orbs alone with its own settings (OrbsCustom), Inferno Arrow alone, Inferno Arrow alone with its own settings
    /// (InfernoCustom), Entangle alone, and Entangle alone with its own settings (EntangleCustom).
    /// </summary>
    private const string AllNineMd5 = "35b1b1ba5fcd4508a1f6b5577fba8acf", EightMd5 = "874f878d4561994e632c7ccce1aeeae1",
                         SevenMd5 = "13fee48ded1953e8d96c1156806da92f", TestSettingsMd5 = "455aa80df41cf167164b446adb6d55a0",
                         KromlechMd5 = "9140e0a1e42f2407cb3636fcf02485f5", PowerAttackMd5 = "9c43b9f8fe2a8eac7d55ad7bae5d91c4",
                         SorcerersMd5 = "e4d14a17db1520a441a66a8bf059f103", SorcerersThreeRanksMd5 = "b83ea36c71ab1c2812e5d5e7ff4eb463",
                         OrbsMd5 = "be475908ebf9234c22d113bda22a4db8", OrbsCustomMd5 = "cfafd4033ec2a38d98c10574b3f3362b",
                         InfernoMd5 = "7106413d5afd942e4ee40d75f2967f12", InfernoCustomMd5 = "f8a7a88a448bcaefa8316679b3984893",
                         EntangleMd5 = "6f6a3a2a2b9ca5df6332455cb962e659", EntangleCustomMd5 = "a3b8d16b35c8084e829eae30e7a53dd3";

    /// <summary>
    /// featpatch.py --spells xantamorbs --xantamorbs-price 4 --xantamorbs-energy 6.5 --xantamorbs-orbs 2,3,5,7,10
    /// --xantamorbs-dmin 1,5,9,20,0 --xantamorbs-dmax 3,6,30,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings OrbsCustom =
        new(4, 6.5f, new[] { 1, 5, 9, 20, 0 }, new[] { 3, 6, 30, 20, 255 }, Third: new[] { 2, 3, 5, 7, 10 });

    /// <summary>
    /// featpatch.py --spells infernoarrow --infernoarrow-price 4 --infernoarrow-energy 6.5 --infernoarrow-dmin 1,5,9,20,0
    /// --infernoarrow-dmax 3,6,30,20,255 --infernoarrow-burn 0.1,1,2.5,10,25.5 --infernoarrow-blast 1,40,100,200,255
    /// --infernoarrow-spread 255,1,30,90,120 --infernoarrow-fmin 0,0,1,9,200 --infernoarrow-fmax 0,1,1,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings InfernoCustom =
        new(4, 6.5f, new[] { 1, 5, 9, 20, 0 }, new[] { 3, 6, 30, 20, 255 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 1, 40, 100, 200, 255 }, new[] { 255, 1, 30, 90, 120 }, new[] { 1, 10, 25, 100, 255 }, new[] { 0, 0, 1, 9, 200 },
                new[] { 0, 1, 1, 20, 255 }
            });

    /// <summary>
    /// featpatch.py --spells entangle --entangle-price 4 --entangle-energy 6.5 --entangle-vines 1,3,5,7,10 --entangle-slow 2,20,50,80,98
    /// --entangle-time 0.1,1,2.5,10,25.5 --entangle-dmin 0,1,9,20,0 --entangle-dmax 0,2,30,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings EntangleCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 }, Third: new[] { 1, 3, 5, 7, 10 },
            More: new IReadOnlyList<int>[] { new[] { 2, 20, 50, 80, 98 }, new[] { 1, 10, 25, 100, 255 } });

    /// <summary>featpatch.py version 11 (all nine) on ADRIANNA26 (the stock file with three players turned on).</summary>
    private const string ThreePlayersMd5 = "cfb0330da0fb7197f3e02ce815aa6613";

    /// <summary>
    /// Version 10 on the stock executable (featpatch.py version 10, all eight at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version10Md5 = "944c654a77454ffca092eebabe1ce05f";

    private const string Version10RunsBase64 =
        "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"
        + "+tP6Z1nfN/mszJTvD7a0rF3fsr7l7rpNW7asmym+qf3RmQziXDPWnKk/W1puv/ur9Mfqv4j+ZJ1fn9m9fPMd61s2b6ltadnyY6Kv7o/Oqj++mYefotc0"
        + "68dLeNuCSbacmmtNuoWYu/mSa7UqaVlDhel6Ntt6CtZjb2slZ71xeoPOMBDjVnD3cVPWs/Rwql7NXE+11ptYvfZdesLCjV5O4L6cvp48XY9mrlc4Zb3m"
        + "2lvoM9wYfMKKrTRtPSVbrrPzr8hyDBL5nPOZtBUoyPh3jpqnr8fkLrglLxsseefJAfu/DlfZbr6WzYe7xokquFu5HvuY3sDloasmKRsPzPwucQwI7d+2"
        + "3O3fJLEPi4bZxwojln2x+/6lYu7b1zNctfAyW4gOYlrtU4Vh9VjtU5k62T7JnAncnYYTTfesm2HbVps1f4Ot1I1MqSdW0GstX+D1zZeZc1vgzwuy+CxQ"
        + "cHlRiHVH4yvrWSHqmkhdfIpfsO7Prj5/6y3XzdDLg2E6fyBMrYz2h+mv+xGOKJarDF0pL3IeT73p/3//A80X";

    /// <summary>
    /// Version 9 on the stock executable (featpatch.py version 9, all seven at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version9Md5 = "8c794aa8d5341ab7fa971605fcc03bfe";

    private const string Version9RunsBase64 =
        "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";

    /// <summary>
    /// Version 8 on the stock executable (featpatch.py version 8, all seven at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version8Md5 = "bb70e0ec22b3063c64804fc403778a6d";

    private const string Version8RunsBase64 =
        "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";

    /// <summary>
    /// Version 7 on the stock executable (featpatch.py version 7, all six at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version7Md5 = "5d6952d30c679fdcb8e7b735f854ae93";

    private const string Version7RunsBase64 =
        "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";


    /// <summary>
    /// Version 6 on the stock executable (featpatch.py version 6, all five at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version6Md5 = "1c131fd0e186959692c371b61291b66f";

    private const string Version6RunsBase64 =
        "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";

    /// <summary>
    /// Version 5 on the stock executable (featpatch.py version 5, all four at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version5Md5 = "64c66cca454d8dbf6e3883b7db97dd76";

    private const string Version5RunsBase64 =
        "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";

    /// <summary>The words that send the game to it (all nine on), and the stretches of the pool it takes.</summary>
    private static readonly int[] Hooks =
    {
        0x0010F4AC, 0x0010F648, 0x001109C0, 0x0015E220, 0x0015E424, 0x0015E50C, 0x00161EF8, 0x00161F00, 0x00161F08, 0x001676E4,
        0x00167AFC, 0x0016DD2C, 0x0016DD44, 0x0017A4BC, 0x0017B594, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C, 0x001B177C,
        0x001B1784, 0x001BED7C, 0x001BED94, 0x001BEFD0, 0x001BF004, 0x0030A104, 0x0030A11C
    };

    /// <summary>Xantam's Fire's two (its maker's trail vtable); none of Kromlech's spells needs them.</summary>
    private static readonly int[] XantamOnly = { 0x001B177C, 0x001B1784 };

    /// <summary>Sorcerer's Blood's two: a level gained (FUN_00167390's call in the level-up) and the maximum mana (Willpower's rank there).</summary>
    private static readonly int[] SorcerersOnly = { 0x001676E4, 0x0017B594 };

    /// <summary>
    /// Xantam's Orbs alone: the six every spell needs (the level-up lists, the grant, the icons' loading, the cast table's three) and the
    /// HUD's three it shares with Kromlech's spells; nothing of its own.
    /// </summary>
    private static readonly int[] OrbsHooks =
    {
        0x00161EF8, 0x00161F00, 0x00161F08, 0x0016DD44, 0x0017A4BC, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C
    };

    /// <summary>
    /// Inferno Arrow's seven: the spell start's mana and the arrow types and kinds, the arrow's constructor, update and touch, and
    /// icons[9]'s lookup. With the six every spell needs and the HUD's three, the sixteen it takes alone.
    /// </summary>
    private static readonly int[] InfernoOnly = { 0x0010F4AC, 0x0010F648, 0x001109C0, 0x0015E220, 0x0015E424, 0x0015E50C, 0x0016DD2C };

    /// <summary>
    /// Entangle's four, in the game's VineSeek update (nothing else runs it): its step (cos and sin, scaled to 200 a second), the
    /// vine's allocation (0x60) and the call of the VineSpell constructor (EN_VINE). With the six every spell needs and the HUD's three,
    /// the thirteen it takes alone.
    /// </summary>
    private static readonly int[] EntangleOnly = { 0x001BED7C, 0x001BED94, 0x001BEFD0, 0x001BF004 };

    private static readonly (int Start, int End)[] Used =
    {
        (0x00102910, 0x00102B70), (0x00102D80, 0x00102FF4), (0x0010C338, 0x0010C6C4), (0x0010CD50, 0x0010CF50), (0x00118978, 0x00118D70),
        (0x00129318, 0x0012952C), (0x00129C48, 0x00129D64), (0x0012A710, 0x0012A8A0), (0x0012BAE8, 0x0012BF90), (0x0012CD30, 0x0012CE34),
        (0x0012CE40, 0x0012D06C), (0x0012F790, 0x0012F9FC), (0x00130540, 0x00130704), (0x00131170, 0x0013123C), (0x00131240, 0x001312C0),
        (0x00132018, 0x00132168), (0x00132D90, 0x00132EE8), (0x00132EF0, 0x00133394), (0x00221B88, 0x002222C0), (0x00224B68, 0x00224C50)
    };

    /// <summary>The game's Power Attack record (its mana is the Feats &amp; Spells tab's: 5 in the game).</summary>
    private const int PowerRecordVa = 0x0030E2E0;

    /// <summary>The game's Sorcerer's Blood record (type 24, bit 32).</summary>
    private const int SorcerersRecordVa = 0x0030E3D0;

    /// <summary>The game's Inferno Arrow record (type 9, bit 8: 1 mana, 1-6 damage, never used).</summary>
    private const int InfernoRecordVa = 0x0030DA20;

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

    private static readonly BorrowedSpell[] Kromlech =
        { BorrowedSpell.PowerAttack, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField, BorrowedSpell.RighteousMight };

    /// <summary>The four version 5 had (all but Power Attack and Sorcerer's Blood).</summary>
    private static readonly BorrowedSpell[] Version5Spells =
        { BorrowedSpell.XantamFire, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField, BorrowedSpell.RighteousMight };

    /// <summary>The five version 6 had (all but Sorcerer's Blood, Xantam's Orbs, Inferno Arrow and Entangle).</summary>
    private static readonly BorrowedSpell[] Version6Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.SorcerersBlood or BorrowedSpell.XantamOrbs or BorrowedSpell.InfernoArrow or
                                                BorrowedSpell.Entangle)).ToArray();

    /// <summary>The six version 7 had (all but Xantam's Orbs, Inferno Arrow and Entangle).</summary>
    private static readonly BorrowedSpell[] Version7Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.XantamOrbs or BorrowedSpell.InfernoArrow or BorrowedSpell.Entangle)).ToArray();

    /// <summary>The seven versions 9 and 8 had (all but Inferno Arrow and Entangle).</summary>
    private static readonly BorrowedSpell[] Version9Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.InfernoArrow or BorrowedSpell.Entangle)).ToArray();

    /// <summary>The eight version 10 had (all but Entangle).</summary>
    private static readonly BorrowedSpell[] Version10Spells = BorrowedSpells.All.Where(s => s != BorrowedSpell.Entangle).ToArray();

    /// <summary>The stock executable with an earlier version in, as featpatch.py wrote it (its runs, checked by their MD5).</summary>
    private byte[] Earlier(string runsBase64, string md5)
    {
        using var packed = new MemoryStream(Convert.FromBase64String(runsBase64));
        using var unpack = new ZLibStream(packed, CompressionMode.Decompress);
        using var runs = new MemoryStream();
        unpack.CopyTo(runs);
        var r = runs.ToArray();
        var bytes = (byte[])_stockBytes.Clone();
        for (var at = 0; at < r.Length;)
        {
            var offset = BitConverter.ToInt32(r, at);
            var length = BitConverter.ToInt32(r, at + 4);
            Array.Copy(r, at + 8, bytes, offset, length);
            at += 8 + length;
        }

        Assert.That(Md5(bytes), Is.EqualTo(md5));
        return bytes;
    }

    private byte[] Version5() => Earlier(Version5RunsBase64, Version5Md5);

    private byte[] Version6() => Earlier(Version6RunsBase64, Version6Md5);

    private byte[] Version7() => Earlier(Version7RunsBase64, Version7Md5);

    private byte[] Version8() => Earlier(Version8RunsBase64, Version8Md5);

    private byte[] Version9() => Earlier(Version9RunsBase64, Version9Md5);

    private byte[] Version10() => Earlier(Version10RunsBase64, Version10Md5);

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
        Assert.That(Md5(on), Is.EqualTo(AllNineMd5), "the same bytes as featpatch.py");
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
        Assert.That(Md5(Bytes(exe, "krom")), Is.EqualTo(KromlechMd5),
                    "Kromlech's four alone: featpatch.py --spells powerattack,bladebarrier,antimagic,righteousmight");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.PowerAttack }));
        Assert.That(Md5(Bytes(exe, "power")), Is.EqualTo(PowerAttackMd5), "Power Attack alone: featpatch.py --spells powerattack");
        Assert.That(exe.GetBorrowedSpells()[BorrowedSpell.PowerAttack].Equals(BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default), Is.True);

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.SorcerersBlood }));
        Assert.That(Md5(Bytes(exe, "sorcerer")), Is.EqualTo(SorcerersMd5), "Sorcerer's Blood alone: featpatch.py --spells sorcerersblood");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        Assert.That(Md5(Bytes(exe, "orbs")), Is.EqualTo(OrbsMd5), "Xantam's Orbs alone: featpatch.py --spells xantamorbs");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version9Spells));
        Assert.That(Md5(Bytes(exe, "seven")), Is.EqualTo(SevenMd5), "all but Inferno Arrow and Entangle");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version10Spells));
        Assert.That(Md5(Bytes(exe, "eight")), Is.EqualTo(EightMd5), "all but Entangle");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InfernoArrow }));
        Assert.That(Md5(Bytes(exe, "inferno")), Is.EqualTo(InfernoMd5), "Inferno Arrow alone: featpatch.py --spells infernoarrow");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.Entangle }));
        Assert.That(Md5(Bytes(exe, "entangle")), Is.EqualTo(EntangleMd5), "Entangle alone: featpatch.py --spells entangle");
    }

    [Test]
    public void Entangle_IsTheGamesOwnVines_WithAThornyUpdateOfItsOwn()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.Entangle);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((47, 36, false, false, false, true, true, true, true, "Vahn", 2)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(BorrowedSpell.Entangle)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That((info.ThirdMin, info.ThirdMax, info.FirstMax, info.SecondMax), Is.EqualTo((1, 10, 255, 255)), "1 to 10 vines, 0 to 255 a thorn");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)),
                    Is.EqualTo(new[] { (2, 98, false, -1, 2), (1, 255, true, -1, 1) }), "an even percentage slower; the time in tenths of a second");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 12f, new[] { 2, 3, 4, 5, 6 }, new[] { 4, 5, 6, 7, 8 }, Third: new[] { 1, 2, 3, 4, 5 },
            More: new IReadOnlyList<int>[] { new[] { 30, 34, 40, 44, 50 }, new[] { 40, 45, 50, 55, 60 } })), Is.True,
                    "price 12, 12 mana; one vine at rank 1 up to five at rank 5; thorns of 2-4 up to 6-8; 30 up to 50 percent slower for 4 up to 6 s");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.Entangle), Is.EqualTo("entangle_name"));
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.Entangle).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "entangle_name"), (l, "entangle_desc") })));
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.Entangle).Where(t => t.Key == "entangle_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Entangle", "Enchevêtrement", "Verstricken", "Intralciare", "Enmarañar" }), "the D&D spell's name in each language");
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.Entangle), Is.EqualTo("icon_entangle.tex"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.Entangle] = EntangleCustom });
        var bytes = Bytes(exe, "entangle-custom");
        Assert.That(Md5(bytes), Is.EqualTo(EntangleCustomMd5));
        var reopened = OpenBytes(bytes, "entangle-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { BorrowedSpell.Entangle }));
        Assert.That(read[BorrowedSpell.Entangle].Equals(EntangleCustom), Is.True, "read back: 1-10 vines, 2-98 percent, 0.1-25.5 s, 0-255 thorns");

        // its hooks: the six every spell needs, the HUD's three and its own four, in the game's VineSeek update
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(13));
        Assert.That(EntangleOnly.All(hooks.Contains), Is.True);
        Assert.That(BitConverter.ToUInt32(bytes, 0x001BEFD0 - VaToFile), Is.EqualTo(0x24040060u), "addiu a0, zero, 0x60: room for the vine's own fields");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x001BEFD0 - VaToFile), Is.EqualTo(0x24040058u), "(the game's 0x58)");

        // the second data block: its mark, the icons of types 32-36, Entangle's tables by rank (entry 0 is rank 0's; the slow halved)
        var data2 = FeatPatch.Data2Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data2, 4), Is.EqualTo("FEA2"));
        Assert.That(BitConverter.ToInt32(bytes, data2 + 4), Is.EqualTo(11));
        Assert.That(Encoding.ASCII.GetString(bytes, data2 + 8 + 4 * 0x18, 18), Is.EqualTo("icon_entangle.tex\0"));
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleVinesAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 3, 5, 7, 10, 0, 0 }));
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleSlowAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 40, 49, 0, 0 }),
                    "the game's slow byte: half the percentage (49 of 50 at 98 percent)");
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleThornMaxAt + 1).Take(5), Is.EqualTo(new byte[] { 0, 2, 30, 20, 255 }));
        Assert.That(BitConverter.ToSingle(bytes, data2 + 0xC0), Is.EqualTo(FeatPatch.EntangleRange));
        Assert.That(BitConverter.ToSingle(bytes, data2 + 0xC4) * 2 * 50, Is.EqualTo(FeatPatch.EntangleSpeed), "the step a frame, halved");
        Assert.That(BitConverter.ToInt32(bytes, data2 + 0xC8), Is.EqualTo(FeatPatch.EntanglePeriod));
        Assert.That((FeatPatch.EntangleRange, FeatPatch.EntangleSpeed, FeatPatch.EntanglePeriod), Is.EqualTo((240f, 200f, 50)));

        // the VineSpell vtable copy: the game's (0x00317470) but for its destructor (+0x0C) and update (+0x24)
        const int gameVine = 0x00317470 - VaToFile;
        var copy = FeatPatch.VTables.Single(v => v.GameVa == 0x00317470).Va - VaToFile;
        for (var at = 0; at < 0x80; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copy + at);
            if (at is 0x0C or 0x24)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVine + at)), $"+0x{at:X2}: the vines' own");
                Assert.That(word is >= 0x00100000u and < 0x00140000u, Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVine + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameVine, 0x80).SequenceEqual(_stockBytes.AsSpan(gameVine, 0x80)), Is.True, "the game's vtable is never written");

        // the cast table: type 36 is Entangle's case, in the pool
        var table = FeatPatch.CastTableVa - VaToFile;
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(36));
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 35);
        Assert.That(cast is >= 0x00100000u and < 0x00140000u, Is.True, "type 36: its own cast");
        Assert.That(Enumerable.Range(23, 12).Select(t => BitConverter.ToUInt32(bytes, table + 4 * t)).Distinct(), Is.EqualTo(new[] { 0x001628F0u }),
                    "types 24-35: the default");

        // a second block that isn't version 11's: not WorldExplorer's
        var unmarked = (byte[])bytes.Clone();
        unmarked[data2 + 4] = 10;
        Assert.That(OpenBytes(unmarked, "entangle-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), detail);

        // the vines' code it relies on changed: not the game's
        foreach (var va in new[] { 0x001BF00C, 0x001BF66C, 0x003174F4 + 0x20, 0x00233804 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "vines").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4D, (byte)0x80), (0x20, (byte)36) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "entangle-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Entangle"));
        }
    }

    [Test]
    public void InfernoArrow_UsesTheGamesOwnRecord_AndBurnsWhereItStrikes()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.InfernoArrow);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird, info.Range,
                     info.Hero, info.More.Count),
                    Is.EqualTo((8, 9, false, true, false, true, true, false, true, "Vahn", 5)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(BorrowedSpell.InfernoArrow)), Is.EqualTo((false, true, true)),
                    "the game's record and texts, its own icon");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost)),
                    Is.EqualTo(new[] { (1, 255, false, -1), (1, 255, false, -1), (1, 255, true, -1), (0, 255, false, 4), (0, 255, false, -1) }),
                    "the blast's reach, the spread, the time in tenths of a second, a burn's least (at most its most) and most");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 12f, new[] { 10, 12, 14, 16, 18 }, new[] { 15, 18, 21, 24, 27 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 64, 68, 72, 76, 80 }, new[] { 48, 54, 60, 66, 72 }, new[] { 20, 25, 30, 35, 40 }, new[] { 1, 1, 2, 2, 3 }, new[] { 2, 3, 3, 4, 5 }
            })), Is.True, "price 12, 12 mana; 10-15 up to 18-27; a blast of 64 up to 80; flames spreading 48 up to 72 for 2 up to 4 seconds, 1-2 up to 3-5 a burn");
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.InfernoArrow), Is.Empty, "its texts are the game's own");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.InfernoArrow), Is.EqualTo("InfernoArrow"));
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.InfernoArrow), Is.EqualTo("icon_infernoarrow.tex"));
        Assert.That(Encoding.ASCII.GetString(_stockBytes, InfernoRecordVa - VaToFile, 13), Is.EqualTo("infernoarrow\0"));
        Assert.That(BitConverter.ToInt16(_stockBytes, InfernoRecordVa - VaToFile + 0x20), Is.EqualTo(9));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.InfernoArrow] = InfernoCustom });
        var bytes = Bytes(exe, "inferno-custom");
        Assert.That(Md5(bytes), Is.EqualTo(InfernoCustomMd5));
        var read = OpenBytes(bytes, "inferno-custom2").GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { BorrowedSpell.InfernoArrow }));
        Assert.That(read[BorrowedSpell.InfernoArrow].Equals(InfernoCustom), Is.True, "read back: 0.1 to 25.5 seconds among the rest");
        Assert.That(read[BorrowedSpell.InfernoArrow].MoreValues[2], Is.EqualTo(new[] { 1, 10, 25, 100, 255 }), "the times in tenths");

        // its hooks: the six every spell needs, the HUD's three and its own seven
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(16));
        Assert.That(InfernoOnly.All(hooks.Contains), Is.True);
        Assert.That(BitConverter.ToUInt32(bytes, 0x0015E424 - VaToFile), Is.EqualTo(0x2C420004u), "sltiu v0, v0, 4: type 9 is an arrow");
        Assert.That(bytes.AsSpan(InfernoRecordVa - VaToFile, 0x50).SequenceEqual(_stockBytes.AsSpan(InfernoRecordVa - VaToFile, 0x50)), Is.True,
                    "the game's record is never written");

        // its data block: the mana and every table by rank (entry 0 is rank 0's), its icon's name
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.InfernoManaAt), Is.EqualTo(6.5f));
        Assert.That(bytes.Skip(data + FeatPatch.InfernoBurnAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data + FeatPatch.InfernoDamageMaxAt + 1).Take(5), Is.EqualTo(new byte[] { 3, 6, 30, 20, 255 }));
        Assert.That(Encoding.ASCII.GetString(bytes, data + FeatPatch.InfernoIconAt, 22), Is.EqualTo("icon_infernoarrow.tex\0"));

        // the flames' FireTrail vtable copy (the last of the two copies of the game's FireTrail vtable, 0x00316798; Xantam's Fire's is
        // the other): the game's but for its update (the flames' own burns, then the game's update) and its touch (none)
        const int gameTrail = 0x00316798 - VaToFile;
        var copies = FeatPatch.VTables.Where(v => v.GameVa == 0x00316798).Select(v => v.Va - VaToFile).ToList();
        Assert.That(copies.Count, Is.EqualTo(2), "Xantam's Fire's and Inferno Arrow's");
        var flames = copies[^1];
        for (var at = 0; at < 0x80; at += 4)
        {
            if (at is not (0x24 or 0x3C))
                Assert.That(BitConverter.ToUInt32(bytes, flames + at), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameTrail + at)), $"+0x{at:X2}");
        }

        var update = BitConverter.ToUInt32(bytes, flames + 0x24);
        Assert.That(update, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameTrail + 0x24)), "its own update, not the game's");
        Assert.That(update, Is.Not.EqualTo(BitConverter.ToUInt32(bytes, copies[0] + 0x24)), "nor Xantam's Fire's");
        Assert.That(update is >= 0x00100000u and < 0x00140000u, Is.True, "in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, flames + 0x3C), Is.EqualTo(0x0012E418u), "no touch");
        Assert.That(bytes.AsSpan(gameTrail, 0x80).SequenceEqual(_stockBytes.AsSpan(gameTrail, 0x80)), Is.True, "the game's vtable is never written");

        // the cast table: type 9 is the game's own case (the arrows')
        var table = FeatPatch.CastTableVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 8), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, 0x00307BC0 - VaToFile + 4 * 8)));

        // the game's record changed, or another ability with its bit or cast type: refused
        foreach (var (va, at, value) in new[]
                 {
                     (InfernoRecordVa, 0x00, (byte)'I'), (InfernoRecordVa, 0x20, (byte)10), (InfernoRecordVa, 0x49, (byte)2),
                     (0x0030E1F0, 0x49, (byte)1), (0x0030E1F0, 0x20, (byte)9)
                 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile + at] = at == 0x49 ? (byte)(changed[va - VaToFile + at] | value) : value;
            if (va != InfernoRecordVa && at == 0x20) changed[va - VaToFile + 0x21] = 0;
            var other = OpenBytes(changed, "inferno-changed");
            Assert.That(other.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8} +0x{at:X2}");
            Assert.That(detail, Does.Contain("Inferno Arrow"));
        }

        // the arrow's code it relies on changed: not the game's
        foreach (var va in new[] { 0x0010F650, 0x001109D4, 0x0015E55C })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "arrow").GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }
    }

    [Test]
    public void XantamsOrbs_AreHisDodgeBalls_WithATouchOfTheirOwn()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.XantamOrbs);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.Passive, info.HasMana, info.HasSecond, info.HasThird, info.Range, info.Hero),
                    Is.EqualTo((46, 35, false, false, true, true, true, true, "Adrianna")));
        Assert.That((info.ThirdMin, info.ThirdMax, info.FirstMax, info.SecondMax), Is.EqualTo((1, 10, 255, 255)), "1 to 10 orbs, 0 to 255 damage each");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 8, 8, 8, 8, 8 }, new[] { 12, 12, 12, 12, 12 },
                                                                  Third: new[] { 1, 2, 3, 4, 5 })), Is.True,
                    "price 12, 10 mana, one orb at rank 1 up to five at rank 5, each 8-12");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.XantamOrbs), Is.EqualTo("xantamorbs_name"));
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.XantamOrbs), Is.EqualTo("icon_xantamorbs.tex"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.XantamOrbs] = OrbsCustom });
        var bytes = Bytes(exe, "orbs-custom");
        Assert.That(Md5(bytes), Is.EqualTo(OrbsCustomMd5));
        var read = OpenBytes(bytes, "orbs-custom2").GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { BorrowedSpell.XantamOrbs }));
        Assert.That(read[BorrowedSpell.XantamOrbs].Equals(OrbsCustom), Is.True, "read back: 2, 3, 5, 7 and 10 orbs, 1-3 ... 0-255 damage");
        Assert.That(read[BorrowedSpell.XantamOrbs].Equals(OrbsCustom with { Third = new[] { 2, 3, 5, 7, 9 } }), Is.False, "the orbs count");

        // only the hooks every spell needs and the HUD's
        Assert.That(Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.EqualTo(OrbsHooks));

        // the game's DodgeBall vtable (0x0031C3C8) is never written; the copy's touch (+0x3C) is the orbs' own, every other word the game's
        int gameVt = 0x0031C3C8 - VaToFile, copyVt = FeatPatch.VTables.Single(v => v.GameVa == 0x0031C3C8).Va - VaToFile;
        Assert.That(bytes.AsSpan(gameVt, 0x80).SequenceEqual(_stockBytes.AsSpan(gameVt, 0x80)), Is.True);
        for (var at = 0; at < 0x80; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copyVt + at);
            if (at == 0x3C)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVt + at)), "its touch: the orbs' own");
                Assert.That(word is >= 0x00100000u and < 0x00140000u, Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVt + at)), $"+0x{at:X2}");
        }

        // the cast table (its own piece of the pool): the game's 23 cases, the orbs' case for type 35, the default between
        var table = FeatPatch.CastTableVa - VaToFile;
        for (var t = 0; t < 23; t++)
            Assert.That(BitConverter.ToUInt32(bytes, table + 4 * t), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, 0x00307BC0 - VaToFile + 4 * t)));
        Assert.That(Enumerable.Range(23, 11).Select(t => BitConverter.ToUInt32(bytes, table + 4 * t)).Distinct(), Is.EqualTo(new[] { 0x001628F0u }));
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 34), Is.Not.EqualTo(0x001628F0u), "type 35: the orbs' cast");

        // the fan (version 9): 200 a second, the cos and sin of half of 12 degrees, the fan's point 32 behind her hand
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsSpeedAt), Is.EqualTo(200f));
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsFanAt), Is.EqualTo((float)Math.Cos(Math.PI * 6 / 180)));
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsFanAt + 4), Is.EqualTo((float)Math.Sin(Math.PI * 6 / 180)));
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsApexAt), Is.EqualTo(32f));
        Assert.That((FeatPatch.OrbsSpeed, FeatPatch.OrbsFanDegrees, FeatPatch.OrbsApex), Is.EqualTo((200f, 12f, 32f)));

        // a cast table that isn't the one written, or the game's DodgeBall changed: not WorldExplorer's, or not the game's
        var table2 = (byte[])bytes.Clone();
        table2[table + 4 * 34] ^= 4;
        Assert.That(OpenBytes(table2, "orbs-table").GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), detail);
        foreach (var va in new[] { 0x001E83A8, 0x0031C3EC, 0x001E47B0 })   // its constructor, its vtable's update, its 500 frames
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "dodgeball").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4D, (byte)0x40), (0x20, (byte)35) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "orbs-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Xantam's Orbs"));
        }
    }

    [Test]
    public void SorcerersBlood_UsesTheGamesOwnRecord_AndItsRanksAreASetting()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.Passive, info.HasMana, info.HasValues, info.Hero),
                    Is.EqualTo((32, 24, true, true, false, false, "Adrianna")));
        Assert.That((info.Default.Price, info.Default.RankCount, info.Default.Mana), Is.EqualTo((3, 1, 0f)), "one rank at price 3, no mana");
        Assert.That(Encoding.ASCII.GetString(_stockBytes, SorcerersRecordVa - VaToFile, 15), Is.EqualTo("SorcerersBlood\0"));
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.SorcerersBlood), Is.Empty, "its texts are the game's own");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.SorcerersBlood), Is.EqualTo("SorcerersBlood"));
        Assert.That(BorrowedSpellIcons.HasOwnIcon(BorrowedSpell.SorcerersBlood), Is.False, "a passive feat has no HUD slot");

        // 3 ranks at price 4: featpatch.py --spells sorcerersblood --sorcerersblood-ranks 3 --sorcerersblood-price 4
        var three = new BorrowedSpellSettings(4, 0f, Array.Empty<int>(), Array.Empty<int>(), RankCount: 3);
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.SorcerersBlood] = three });
        var bytes = Bytes(exe, "sorcerer-3");
        Assert.That(Md5(bytes), Is.EqualTo(SorcerersThreeRanksMd5));
        var read = OpenBytes(bytes, "sorcerer-3b").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.SorcerersBlood].Equals(three), Is.True, "read back: price 4, 3 ranks");
        Assert.That(SorcerersOnly.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True,
                    "its two hooks");
        Assert.That(bytes.AsSpan(SorcerersRecordVa - VaToFile, 0x50).SequenceEqual(_stockBytes.AsSpan(SorcerersRecordVa - VaToFile, 0x50)), Is.True,
                    "the game's record is never written");

        // a record that isn't the game's Sorcerer's Blood any more, or another ability with its bit: refused
        foreach (var (va, at, value) in new[]
                 {
                     (SorcerersRecordVa, 0x00, (byte)'s'), (SorcerersRecordVa, 0x20, (byte)23), (SorcerersRecordVa, 0x4C, (byte)2),
                     (0x0030E380, 0x4C, (byte)1)                     // Clangeddin's Fist with bit 32 too
                 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile + at] |= value;
            var other = OpenBytes(changed, "sorcerer-changed");
            Assert.That(other.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8} +0x{at:X2}");
            Assert.That(detail, Does.Contain("Sorcerer's Blood"));
        }
    }

    [Test]
    public void PowerAttack_UsesTheGamesOwnRecord()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.PowerAttack);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.HasMana, info.HasSecond), Is.EqualTo((34, 18, true, false, false)));
        Assert.That(Encoding.ASCII.GetString(_stockBytes, PowerRecordVa - VaToFile, 12), Is.EqualTo("powerattack\0"));
        Assert.That(BitConverter.ToSingle(_stockBytes, PowerRecordVa - VaToFile + 0x2C), Is.EqualTo(5f), "the game's 5 mana");

        // the record is never written, and its mana (the Feats & Spells tab's) can change underneath the spells
        var exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var on = Bytes(exe, "power-record");
        Assert.That(on.AsSpan(PowerRecordVa - VaToFile, 0x50).SequenceEqual(_stockBytes.AsSpan(PowerRecordVa - VaToFile, 0x50)), Is.True);
        var index = exe.GetFeatSpells().Single(f => f.Name == "powerattack").Index;
        exe.SetFeatEnergy(index, 7.5f);
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        exe.RemoveBorrowedSpells();
        var off = Bytes(exe, "power-record-off");
        Assert.That(BitConverter.ToSingle(off, PowerRecordVa - VaToFile + 0x2C), Is.EqualTo(7.5f), "its mana stays the Feats & Spells tab's");

        // a record that isn't the game's Power Attack any more: refused
        foreach (var (at, value) in new[] { (0x00, (byte)'P'), (0x20, (byte)19), (0x4C, (byte)1) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[PowerRecordVa - VaToFile + at] = value;
            var other = OpenBytes(changed, "power-changed");
            Assert.That(other.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Power Attack record"));
        }
    }

    [Test]
    public void Version5_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v5 = Version5();
        var exe = OpenBytes(v5, "v5");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 5"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version5Spells));

        // saved with Power Attack, Sorcerer's Blood, Xantam's Orbs, Inferno Arrow and Entangle added: featpatch.py version 11's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PowerAttack] = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default,
            [BorrowedSpell.SorcerersBlood] = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default,
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v5-updated")), Is.EqualTo(AllNineMd5));

        // its own settings (in its data block, where version 5 keeps them) are read and kept
        var custom = (byte[])v5.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 8 + 0x42] = 3;                                // Xantam's Fire's price
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x200 + 0x50 + 0x2C);   // Blade Barrier's mana (the second record)
        custom[data + 0x340 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x360 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x378 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        var read = OpenBytes(custom, "v5-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.XantamFire].Price, Is.EqualTo(3));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        var updated = OpenBytes(custom, "v5-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v5-custom-saved"), "v5-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v5, "v5-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v5-out-saved"), Is.EqualTo(_stockBytes));

        // version 5 with its code changed: not recognised
        var broken = (byte[])v5.Clone();
        broken[0x00102910 + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v5-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version6_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v6 = Version6();
        var exe = OpenBytes(v6, "v6");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 6"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version6Spells));

        // saved with Sorcerer's Blood, Xantam's Orbs, Inferno Arrow and Entangle added: featpatch.py version 11's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.SorcerersBlood] = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default,
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v6-updated")), Is.EqualTo(AllNineMd5));

        // its own settings (in its data block, where version 6 keeps them) are read and kept
        var custom = (byte[])v6.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x3E8 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x260 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x3A0 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x3C0 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x3D8 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        var read = OpenBytes(custom, "v6-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        var updated = OpenBytes(custom, "v6-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v6-custom-saved"), "v6-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v6, "v6-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v6-out-saved"), Is.EqualTo(_stockBytes));

        // version 6 with its code changed: not recognised
        var broken = (byte[])v6.Clone();
        broken[0x00102910 + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v6-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version10_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v10 = Version10();
        var exe = OpenBytes(v10, "v10");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 10"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version10Spells));

        // saved as it is, or with Entangle added: featpatch.py version 11's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v10-updated")), Is.EqualTo(EightMd5));
        exe = OpenBytes(v10, "v10b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default
        });
        Assert.That(Md5(Bytes(exe, "v10-updated-all")), Is.EqualTo(AllNineMd5));

        // its own settings (in its data block at 0x00221B88, where version 11's is too, in version 10's layout) are read and kept
        var custom = (byte[])v10.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x558 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x380 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x568 + 2] = 9;                                       // Xantam's Orbs: 9 orbs at rank 2
        BitConverter.GetBytes(3.5f).CopyTo(custom, data + 0x630);           // Inferno Arrow's mana
        custom[data + 0x648 + 5] = 99;                                      // its flames: 9.9 s at rank 5
        custom[data + 0x650 + 1] = 33;                                      // its blast: 33 at rank 1
        var read = OpenBytes(custom, "v10-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.XantamOrbs].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That(read[BorrowedSpell.InfernoArrow].Mana, Is.EqualTo(3.5f));
        Assert.That(read[BorrowedSpell.InfernoArrow].MoreValues[2], Is.EqualTo(new[] { 20, 25, 30, 35, 99 }));
        Assert.That(read[BorrowedSpell.InfernoArrow].MoreValues[0], Is.EqualTo(new[] { 33, 68, 72, 76, 80 }));
        var updated = OpenBytes(custom, "v10-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v10-custom-saved"), "v10-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v10, "v10-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v10-out-saved"), Is.EqualTo(_stockBytes));

        // version 10 with its code changed: not recognised
        var broken = (byte[])v10.Clone();
        broken[FeatPatch.V10Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v10-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version9_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v9 = Version9();
        var exe = OpenBytes(v9, "v9");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 9"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version9Spells));

        // saved as it is (its data block moves to version 11's place), or with Inferno Arrow and Entangle added: featpatch.py version
        // 11's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v9-updated")), Is.EqualTo(SevenMd5));
        exe = OpenBytes(v9, "v9b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default
        });
        Assert.That(Md5(Bytes(exe, "v9-updated-all")), Is.EqualTo(AllNineMd5));

        // its own settings (in its data block at 0x00132D90, where every version before 10 keeps them) are read and kept
        var custom = (byte[])v9.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x4F8 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x320 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x508 + 2] = 9;                                       // Xantam's Orbs: 9 orbs at rank 2
        var read = OpenBytes(custom, "v9-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.XantamOrbs].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        var updated = OpenBytes(custom, "v9-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v9-custom-saved"), "v9-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v9, "v9-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v9-out-saved"), Is.EqualTo(_stockBytes));

        // version 9 with its code changed: not recognised
        var broken = (byte[])v9.Clone();
        broken[FeatPatch.V9Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v9-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version8_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v8 = Version8();
        var exe = OpenBytes(v8, "v8");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 8"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version9Spells));

        // saved as it is: featpatch.py version 11's file with its seven (the orbs in a fan now)
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v8-updated")), Is.EqualTo(SevenMd5));

        // its own settings (in its data block, where version 8 keeps them) are read and kept, the orbs' among them
        var custom = (byte[])v8.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x4F8 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x320 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x4B0 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x4D0 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x4E8 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        custom[data + 0x80 + 5 * 0x60 + 8 + 0x41] = 4;                      // Sorcerer's Blood: 4 ranks (the sixth entry)
        BitConverter.GetBytes(7.5f).CopyTo(custom, data + 0x320 + 4 * 0x50 + 0x2C);   // Xantam's Orbs' mana (the fifth own record)
        custom[data + 0x508 + 2] = 9;                                       // Xantam's Orbs: 9 orbs at rank 2
        custom[data + 0x510 + 5] = 3;                                       // ... 3-40 each at rank 5
        custom[data + 0x518 + 5] = 40;
        var read = OpenBytes(custom, "v8-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        Assert.That(read[BorrowedSpell.SorcerersBlood].RankCount, Is.EqualTo(4));
        Assert.That(read[BorrowedSpell.XantamOrbs].Mana, Is.EqualTo(7.5f));
        Assert.That(read[BorrowedSpell.XantamOrbs].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That((read[BorrowedSpell.XantamOrbs].First[4], read[BorrowedSpell.XantamOrbs].Second[4]), Is.EqualTo((3, 40)));
        var updated = OpenBytes(custom, "v8-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v8-custom-saved"), "v8-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v8, "v8-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v8-out-saved"), Is.EqualTo(_stockBytes));

        // version 8 with its code changed: not recognised
        var broken = (byte[])v8.Clone();
        broken[FeatPatch.V8Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v8-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version7_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v7 = Version7();
        var exe = OpenBytes(v7, "v7");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 7"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version7Spells));

        // saved with Xantam's Orbs, Inferno Arrow and Entangle added: featpatch.py version 11's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v7-updated")), Is.EqualTo(AllNineMd5));

        // its own settings (in its data block, where version 7 keeps them) are read and kept
        var custom = (byte[])v7.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x448 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x2C0 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x400 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x420 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x438 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        custom[data + 0x80 + 5 * 0x60 + 8 + 0x41] = 4;                      // Sorcerer's Blood: 4 ranks (the sixth entry)
        var read = OpenBytes(custom, "v7-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        Assert.That(read[BorrowedSpell.SorcerersBlood].RankCount, Is.EqualTo(4));
        var updated = OpenBytes(custom, "v7-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v7-custom-saved"), "v7-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v7, "v7-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v7-out-saved"), Is.EqualTo(_stockBytes));

        // version 7 with its code changed: not recognised
        var broken = (byte[])v7.Clone();
        broken[0x00102910 + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v7-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
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
                    "all twenty-seven hooks");
        Assert.That(bytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750).SequenceEqual(_stockBytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750)),
                    Is.True, "the game's ability table (Power Attack's, Sorcerer's Blood's and Inferno Arrow's records among them) is untouched");
        Assert.That(bytes.Take(0x100), Is.EqualTo(_stockBytes.Take(0x100)), "the ELF header and program headers are the game's");
        Assert.That(BgdaExecutable.BorrowedSpellsBytes, Is.EqualTo(Used.Sum(u => u.End - u.Start)));
        Assert.That(BgdaExecutable.FeatPoolBytes, Is.EqualTo(32724));
        Assert.That(BgdaExecutable.FeatPoolStretches, Is.EqualTo(61));

        // Kromlech's four alone: Xantam's Fire's two hooks, Sorcerer's Blood's two, Inferno Arrow's seven and Entangle's four stay the game's
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Kromlech));
        bytes = Bytes(exe, "where-krom");
        Assert.That(XantamOnly.Concat(SorcerersOnly).Concat(InfernoOnly).Concat(EntangleOnly)
                        .All(va => BitConverter.ToUInt32(bytes, va - VaToFile) == BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.True);

        // Xantam's Orbs alone: nine hooks, and the same stretches of the pool as all nine
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        bytes = Bytes(exe, "where-orbs");
        Assert.That(Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.EqualTo(OrbsHooks));
        changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => i + VaToFile).ToList();
        Assert.That(changed.All(va => Hook(va) || Used.Any(u => u.Start <= va && va < u.End)), Is.True);
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
            [BorrowedSpell.PowerAttack] = new(9, 0f, new[] { 1, 20, 3, 1, 2 }, Array.Empty<int>()),
            [BorrowedSpell.AntimagicField] = new(30, 100f, new[] { 1, 60, 2, 3, 4 }, Array.Empty<int>()),
            [BorrowedSpell.RighteousMight] = new(0, 0.5f, new[] { 0, 200, 5, 6, 7 }, new[] { 90, 0, 1, 2, 3 }),
            [BorrowedSpell.SorcerersBlood] = new(7, 0f, Array.Empty<int>(), Array.Empty<int>(), RankCount: 4),
            [BorrowedSpell.XantamOrbs] = new(30, 0.5f, new[] { 0, 255, 1, 2, 3 }, new[] { 0, 255, 9, 2, 4 }, Third: new[] { 10, 1, 4, 9, 6 }),
            [BorrowedSpell.InfernoArrow] = InfernoCustom,
            [BorrowedSpell.Entangle] = EntangleCustom
        };
        var exe = Stock();
        exe.SetBorrowedSpells(custom);
        var reopened = OpenBytes(Bytes(exe, "custom"), "custom2");
        AssertSame(reopened.GetBorrowedSpells(), custom);

        // a spell added and two taken out in place, the orbs, the flames and the vines changed, then back to all nine at the tested
        // settings
        var changed = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(custom) { [BorrowedSpell.BladeBarrier] = BorrowedSpells.Info(BorrowedSpell.BladeBarrier).Default };
        changed.Remove(BorrowedSpell.XantamFire);
        changed.Remove(BorrowedSpell.PowerAttack);
        changed[BorrowedSpell.SorcerersBlood] = changed[BorrowedSpell.SorcerersBlood] with { RankCount = 5, Price = 0 };
        changed[BorrowedSpell.XantamOrbs] = changed[BorrowedSpell.XantamOrbs] with { Third = new[] { 1, 1, 1, 1, 1 } };
        changed[BorrowedSpell.InfernoArrow] = InfernoCustom with
        {
            More = InfernoCustom.MoreValues.Select((r, k) => k == 2 ? (IReadOnlyList<int>)new[] { 255, 255, 255, 255, 255 } : r).ToArray()
        };
        changed[BorrowedSpell.Entangle] = EntangleCustom with
        {
            Third = new[] { 10, 10, 1, 1, 2 }, More = new IReadOnlyList<int>[] { new[] { 98, 2, 4, 6, 8 }, new[] { 5, 6, 7, 8, 9 } }
        };
        reopened.SetBorrowedSpells(changed);
        AssertSame(OpenBytes(Bytes(reopened, "changed"), "changed2").GetBorrowedSpells(), changed);
        reopened.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Md5(Bytes(reopened, "custom-back")), Is.EqualTo(AllNineMd5));

        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(16, h)), Is.EqualTo(new[] { 16, 17, 19, 21, 25 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(10, h)), Is.EqualTo(new[] { 10, 11, 13, 15, 19 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(12, h)), Is.EqualTo(new[] { 12, 13, 15, 17, 21 }), "Xantam's Orbs'");
        Assert.That(BorrowedSpellSettings.RankCost(3, 2), Is.EqualTo(9), "below 5: (held + 1) x price");
    }

    [Test]
    public void BadSettings_AreRefused_AndNothingIsWritten()
    {
        RequireStock();
        var exe = Stock();
        var x = BorrowedSpells.Info(BorrowedSpell.XantamFire).Default;
        var pa = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default;
        var am = BorrowedSpells.Info(BorrowedSpell.AntimagicField).Default;
        var rm = BorrowedSpells.Info(BorrowedSpell.RighteousMight).Default;
        var sb = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default;
        var xo = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default;
        var ia = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default;
        var en = BorrowedSpells.Info(BorrowedSpell.Entangle).Default;
        IReadOnlyList<int>[] Row(int k, int[] row) => ia.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        IReadOnlyList<int>[] EnRow(int k, int[] row) => en.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        var bad = new (BorrowedSpell, BorrowedSpellSettings)[]
        {
            (BorrowedSpell.Entangle, en with { More = EnRow(0, new[] { 30, 35, 40, 44, 50 }) }), (BorrowedSpell.Entangle, en with { More = EnRow(0, new[] { 0, 34, 40, 44, 50 }) }),
            (BorrowedSpell.Entangle, en with { More = EnRow(0, new[] { 30, 34, 40, 44, 100 }) }), (BorrowedSpell.Entangle, en with { More = EnRow(1, new[] { 40, 0, 50, 55, 60 }) }),
            (BorrowedSpell.Entangle, en with { More = EnRow(1, new[] { 40, 45, 50, 55, 256 }) }), (BorrowedSpell.Entangle, en with { More = en.MoreValues.Take(1).ToArray() }),
            (BorrowedSpell.Entangle, en with { More = null }), (BorrowedSpell.Entangle, en with { Third = null }),
            (BorrowedSpell.Entangle, en with { Third = new[] { 0, 2, 3, 4, 5 } }), (BorrowedSpell.Entangle, en with { Third = new[] { 1, 2, 3, 4, 11 } }),
            (BorrowedSpell.Entangle, en with { First = new[] { 2, 3, 9, 5, 6 } }), (BorrowedSpell.Entangle, en with { Mana = 0f }),
            (BorrowedSpell.Entangle, en with { Price = 31 }), (BorrowedSpell.XantamOrbs, xo with { More = en.More }),
            (BorrowedSpell.InfernoArrow, ia with { More = null }), (BorrowedSpell.InfernoArrow, ia with { More = ia.MoreValues.Take(4).ToArray() }),
            (BorrowedSpell.InfernoArrow, ia with { More = Row(0, new[] { 0, 1, 1, 1, 1 }) }), (BorrowedSpell.InfernoArrow, ia with { More = Row(1, new[] { 1, 1, 1, 1, 256 }) }),
            (BorrowedSpell.InfernoArrow, ia with { More = Row(2, new[] { 1, 0, 1, 1, 1 }) }), (BorrowedSpell.InfernoArrow, ia with { More = Row(3, new[] { 1, 1, 9, 2, 3 }) }),
            (BorrowedSpell.InfernoArrow, ia with { First = new[] { 10, 12, 22, 16, 18 } }), (BorrowedSpell.InfernoArrow, ia with { Mana = 0f }),
            (BorrowedSpell.InfernoArrow, ia with { Third = new[] { 1, 1, 1, 1, 1 } }), (BorrowedSpell.XantamOrbs, xo with { More = ia.More }),
            (BorrowedSpell.XantamOrbs, xo with { Third = null }), (BorrowedSpell.XantamOrbs, xo with { Third = new[] { 1, 2, 3, 4 } }),
            (BorrowedSpell.XantamOrbs, xo with { Third = new[] { 0, 2, 3, 4, 5 } }), (BorrowedSpell.XantamOrbs, xo with { Third = new[] { 1, 2, 3, 4, 11 } }),
            (BorrowedSpell.XantamOrbs, xo with { First = new[] { 8, 8, 13, 8, 8 } }), (BorrowedSpell.XantamOrbs, xo with { Second = new[] { 12, 12, 12, 12, 256 } }),
            (BorrowedSpell.XantamOrbs, xo with { Mana = 0f }), (BorrowedSpell.XantamOrbs, xo with { Price = 31 }),
            (BorrowedSpell.XantamFire, x with { Third = new[] { 1, 2, 3, 4, 5 } }), (BorrowedSpell.SorcerersBlood, sb with { Third = new[] { 1, 1, 1, 1, 1 } }),
            (BorrowedSpell.XantamFire, x with { Price = -1 }), (BorrowedSpell.XantamFire, x with { Price = 31 }),
            (BorrowedSpell.XantamFire, x with { Mana = 0f }), (BorrowedSpell.XantamFire, x with { Mana = 100.5f }),
            (BorrowedSpell.XantamFire, x with { Mana = float.PositiveInfinity }),
            (BorrowedSpell.BladeBarrier, x with { First = new[] { 1, 1, 5, 2, 3 } }), (BorrowedSpell.BladeBarrier, x with { Second = new[] { 2, 3, 4, 5, 256 } }),
            (BorrowedSpell.BladeBarrier, x with { First = new[] { 1, 1, 2, 2 } }),
            (BorrowedSpell.AntimagicField, am with { First = new[] { 4, 5, 0, 7, 8 } }), (BorrowedSpell.AntimagicField, am with { Second = new[] { 1, 2, 3, 4, 5 } }),
            (BorrowedSpell.RighteousMight, rm with { First = new[] { 10, 20, 30, 40, 201 } }), (BorrowedSpell.RighteousMight, rm with { Second = new[] { 91, 20, 30, 40, 50 } }),
            (BorrowedSpell.PowerAttack, pa with { Mana = 5f }), (BorrowedSpell.PowerAttack, pa with { First = new[] { 1, 2, 0, 4, 5 } }),
            (BorrowedSpell.PowerAttack, pa with { First = new[] { 1, 2, 3, 4, 21 } }), (BorrowedSpell.PowerAttack, pa with { Second = new[] { 1, 2, 3, 4, 5 } }),
            (BorrowedSpell.PowerAttack, pa with { Price = 31 }), (BorrowedSpell.PowerAttack, pa with { RankCount = 3 }),
            (BorrowedSpell.XantamFire, x with { RankCount = 1 }),
            (BorrowedSpell.SorcerersBlood, sb with { RankCount = 0 }), (BorrowedSpell.SorcerersBlood, sb with { RankCount = 6 }),
            (BorrowedSpell.SorcerersBlood, sb with { Mana = 1f }), (BorrowedSpell.SorcerersBlood, sb with { First = new[] { 1 } }),
            (BorrowedSpell.SorcerersBlood, sb with { Price = 31 }), (BorrowedSpell.SorcerersBlood, sb with { Price = -1 })
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
        var texts = Enumerable.Range(0, 5).Select(i => new List<(string, string)>
        {
            ("burninghands_name", $"Burning Hands {i}"), ("odd", "x"), ("powerattack_name", $"Power Attack {i}")
        }).ToArray();
        return LmpWriter.PackEntries(EngineVersion.DarkAlliance,
            GameText.Files.Select((f, i) => (f.File, new UniTextFile(texts[i]).ToArray())).ToList());
    }

    [Test]
    public void TheNames_GoInEveryLanguage_AndComeOutAgain()
    {
        var original = SmallGameText();
        var text = GameText.Read(original);
        var own = BorrowedSpells.All.Where(s => BorrowedSpells.Info(s).HasOwnTexts).ToList();
        Assert.That(own.Any(s => BorrowedSpellTexts.HasNames(text, s) || BorrowedSpellTexts.HasAnyName(text, s)), Is.False);
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.PowerAttack), Is.Empty, "Power Attack's texts are the game's own");
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.SorcerersBlood), Is.Empty, "and Sorcerer's Blood's");
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.InfernoArrow), Is.Empty, "and Inferno Arrow's");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.PowerAttack), Is.EqualTo("powerattack_name"));
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.XantamFire), Is.EqualTo("xantamfire_name"));
        BorrowedSpellTexts.AddNames(text, BorrowedSpells.All);
        var saved = GameText.Read(text.ToArray());
        Assert.That(BorrowedSpells.All.All(s => BorrowedSpellTexts.HasNames(saved, s)), Is.True);
        Assert.That(saved.Get("xantamfire_name"), Is.EqualTo("Xantam's Fire"));
        Assert.That(saved.Get("bladebarrier_name"), Is.EqualTo("Blade Barrier"));
        Assert.That(saved.Get("antimagicfield_name", 2), Is.EqualTo("Antimagisches Feld"));
        Assert.That(saved.Get("righteousmight_hud"), Is.EqualTo("+%d%% damage, -%d%% damage taken"), "the HUD's line: %d the rank's values, %% a percent sign");
        Assert.That(saved.Get("xantamorbs_name"), Is.EqualTo("Xantam's Orbs"));
        Assert.That(saved.Get("xantamorbs_name", 2), Is.EqualTo("Xantams Kugeln"));
        Assert.That(saved.Get("xantamorbs_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\"");
        Assert.That(saved.Get("entangle_name"), Is.EqualTo("Entangle"));
        Assert.That(saved.Get("entangle_name", 1), Is.EqualTo("Enchevêtrement"));
        Assert.That(saved.Get("entangle_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        for (var language = 0; language < 5; language++)
        {
            var keys = saved.Entries(language).Select(e => e.Key).ToList();
            Assert.That(keys, Is.EqualTo(new[]
            {
                "burninghands_name", "odd", "powerattack_name", "xantamfire_name", "xantamfire_desc", "bladebarrier_name", "bladebarrier_desc",
                "antimagicfield_name", "antimagicfield_desc", "antimagicfield_hud", "righteousmight_name", "righteousmight_desc", "righteousmight_hud",
                "xantamorbs_name", "xantamorbs_desc", "entangle_name", "entangle_desc"
            }), "after the file's own entries, spell by spell");
            foreach (var hud in new[] { "antimagicfield_hud", "righteousmight_hud" })
                Assert.That(saved.Get(hud, language)!.TrimEnd().EndsWith("%"), Is.False, "a HUD line ends in plain text (the game's formatter reads on past a line ending in a %-code)");
        }

        Assert.That(BorrowedSpells.All.SelectMany(BorrowedSpellTexts.Of).All(n => n.Text.All(c => GameText.TextCharacters.Contains(c))), Is.True,
                    "letters the game's font draws");
        BorrowedSpellTexts.AddNames(saved, BorrowedSpells.All);
        Assert.That(saved.Changed, Is.False, "adding them again changes nothing");
        BorrowedSpellTexts.RemoveNames(saved, BorrowedSpells.All);
        Assert.That(saved.ToArray(), Is.EqualTo(original), "the game's own powerattack_name stays");

        text = GameText.Read(original);
        BorrowedSpellTexts.AddNames(text, new[] { BorrowedSpell.RighteousMight, BorrowedSpell.XantamFire });
        Assert.That(text.Entries(0).Select(e => e.Key).Skip(3).First(), Is.EqualTo("xantamfire_name"), "in the spells' order, whatever order they are asked in");
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
            [BorrowedSpell.AntimagicField] = "c6d39e2cd0b3573358519a201bd9a6d9", [BorrowedSpell.RighteousMight] = "bd6f5fbeff4c9015adc061080c9912a1",
            [BorrowedSpell.XantamOrbs] = "549084289a254b4fd6413f9ff814f308", [BorrowedSpell.InfernoArrow] = "a7200918c0b4af9f16b4ff6ce63a00b5",
            [BorrowedSpell.Entangle] = "a27ef3840af3753c6772028cd928aa66"
        };
        var own = BorrowedSpells.All.Where(BorrowedSpellIcons.HasOwnIcon).ToList();
        Assert.That(own, Is.EqualTo(md5.Keys.OrderBy(s => s)),
                    "every spell but Power Attack (the game's icon_powerattack.tex) and Sorcerer's Blood (a passive: no HUD slot)");
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.PowerAttack), Is.EqualTo("icon_powerattack.tex"));
        foreach (var spell in own)
        {
            Assert.That(BorrowedSpellIcons.Texture(spell).Length, Is.EqualTo(3568));
            Assert.That(Md5(BorrowedSpellIcons.Texture(spell)), Is.EqualTo(md5[spell]),
                        "make_xantam_icon.py's, make_kromlech_icons.py's, make_xantam_orbs_icon.py's, make_inferno_arrow_icon.py's and " +
                        "make_entangle_icon.py's textures");
        }

        // like the game's ICONS.LMP: sorted, the directory full (no room before the first file)
        var icons = Archive(new[]
        {
            ("exclaim.tex", Fill(90, 9)), ("icon_balllightning.tex", Fill(60, 8)), ("icon_healthpotion.tex", Fill(300, 1)),
            ("icon_powerattack.tex", Fill(100, 7)), ("icon_protection.tex", Fill(200, 2)), ("icon_quake.tex", Fill(40, 6)),
            ("icon_whirlwind.tex", Fill(129, 3)), ("levelup.tex", Fill(70, 4)), ("nomana.tex", Fill(10, 5))
        }, spare: 0);
        Assert.That(GameArchive.Problem(icons), Is.Null);

        var added = BorrowedSpellIcons.AddTo(icons, BorrowedSpells.All);
        Assert.That(GameArchive.Problem(added), Is.Null, "the game reads it as it should");
        Assert.That(GameArchive.ReadDirectory(added).Select(e => e.Name), Is.EqualTo(new[]
        {
            "exclaim.tex", "icon_antimagicfield.tex", "icon_balllightning.tex", "icon_bladebarrier.tex", "icon_entangle.tex", "icon_healthpotion.tex",
            "icon_infernoarrow.tex",
            "icon_powerattack.tex", "icon_protection.tex", "icon_quake.tex", "icon_righteousmight.tex", "icon_whirlwind.tex", "icon_xantamfire.tex",
            "icon_xantamorbs.tex", "levelup.tex", "nomana.tex"
        }), "each where its name sorts");
        Assert.That(GameArchive.AsLoaded(added).Where(f => !f.Name.StartsWith("icon_") || f.Name is "icon_healthpotion.tex" or "icon_protection.tex" or
                                                        "icon_whirlwind.tex" or "icon_balllightning.tex" or "icon_quake.tex" or "icon_powerattack.tex").Select(f => f.Data),
                    Is.EqualTo(GameArchive.AsLoaded(icons).Select(f => f.Data)), "every other file as it was (Power Attack's the game's own)");
        Assert.That(BorrowedSpellIcons.IsIn(icons, BorrowedSpell.PowerAttack), Is.True);
        Assert.That(BorrowedSpellIcons.AddTo(icons, new[] { BorrowedSpell.PowerAttack }), Is.EqualTo(icons), "Power Attack brings no icon");
        Assert.That(BorrowedSpellIcons.AddTo(icons, new[] { BorrowedSpell.SorcerersBlood }), Is.EqualTo(icons), "nor Sorcerer's Blood");
        Assert.That(BorrowedSpellIcons.RemoveFrom(icons, new[] { BorrowedSpell.PowerAttack }), Is.EqualTo(icons), "and never takes the game's out");
        foreach (var spell in own)
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
