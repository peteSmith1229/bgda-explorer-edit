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
/// feat), Xantam's Orbs and Dimension Door (Ilivarra's teleport) for Adrianna; Power Attack (the game's own feat), Blade Barrier,
/// Antimagic Field and Righteous Might for Kromlech; Inferno Arrow (the game's own cut feat), Entangle (the game's own unused vines),
/// Insect Plague (the kobold shaman's swarm) and Poison Gas Arrow (the monsters' poison gas, on an arrow) for Vahn. On the stock PAL
/// executable in Test_files (skipped when it is not present), with the Three Players tab switching three players underneath them,
/// and versions 13, 12, 11, 10, 9, 8, 7, 6 and 5 (the first test of Poison Gas Arrow, the second of Insect Plague, the first of
/// Entangle and of Inferno Arrow, the second and first of Xantam's Orbs, the first of Sorcerer's Blood, of Power Attack and of
/// Kromlech's spells) brought up to date; their names in GTEXT.LMP and their icons in ICONS.LMP (archives built here). The patch is
/// featpatch.py version 14 (Dimension Door; its fifth data block after the fourth, in the pool's stretch 47): the checksums are of
/// the files featpatch.py writes, and what it does in the game was checked by emulation (test_feats14.py on forty-eight
/// files: 398 checks on each with all twelve; 437 broken copies on two bases).
/// </summary>
[TestFixture]
public class BgdaBorrowedSpellsTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>
    /// featpatch.py version 14 on the stock executable: all twelve at the tested settings, all but Dimension Door (the eleven of
    /// version 13), all but Poison Gas Arrow and Dimension Door (the ten of version 12), all but Insect Plague, Poison Gas Arrow and
    /// Dimension Door (the nine of version 11), all but Entangle, Insect Plague, Poison Gas Arrow and Dimension Door (the eight of
    /// version 10), all but Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door (the seven of version 9), all
    /// twelve at the test files' settings (price 0, 4 mana), Kromlech's four alone, Power Attack alone, Sorcerer's Blood alone,
    /// Sorcerer's Blood alone with 3 ranks at price 4, Xantam's Orbs alone, Xantam's Orbs alone with its own settings (OrbsCustom),
    /// Inferno Arrow alone, Inferno Arrow alone with its own settings (InfernoCustom), Entangle alone, Entangle alone with its own
    /// settings (EntangleCustom), Insect Plague alone, Insect Plague alone with its own settings (InsectCustom), Poison Gas Arrow
    /// alone, Poison Gas Arrow alone with its own settings (GasCustom), Dimension Door alone, and Dimension Door alone with its own
    /// settings (DoorCustom).
    /// </summary>
    private const string AllTwelveMd5 = "992fba67c99f14fe3267b384fc74f73f", ElevenMd5 = "e6d62e0c5cde1adec8c4537f85d51547",
                         TenMd5 = "0f04bd9a52e1bfd1cb72e825094e3bfc", NineMd5 = "9a9fbae5856d29940818af2702747771",
                         EightMd5 = "5d36c5aa6c73451254ffc3b0e4b781c0", SevenMd5 = "2c7f48de795ad494e1b1896fea3c0045",
                         TestSettingsMd5 = "90a4e35396914252c4b13d0b642cb1d0", KromlechMd5 = "cb21325403e7cd8fe45a7c42211c6611",
                         PowerAttackMd5 = "0c884f80233ffe756ccda02339cb7743", SorcerersMd5 = "fa536bc6d552c6476b1aee2f657fabbd",
                         SorcerersThreeRanksMd5 = "d2ab4372fc357033ce97bb1c39ffdb4e", OrbsMd5 = "3b54a6451d600ff6f51b89e6c242ab85",
                         OrbsCustomMd5 = "d626e33ec3027b61955350b4d21b3b28", InfernoMd5 = "02d9fe8d4b4f9f9fd1193ec1992bc10c",
                         InfernoCustomMd5 = "32e5535e1fdf7aba7539097e3e2d1421", EntangleMd5 = "512d881a975dee081f358ac37521013f",
                         EntangleCustomMd5 = "9fd6a7313901cf4707e7a0344421d64e", InsectMd5 = "c3de17178f3d73e0911eb3f9547a1590",
                         InsectCustomMd5 = "04bda7082441c884948ee896e509efab", GasMd5 = "d4b36d7a600be6489cb33598e89b6c7c",
                         GasCustomMd5 = "750b65628f7555b8e1c59b9c333d0f62", DoorMd5 = "7783cea1281aa252cc8aab3a147ed8a8",
                         DoorCustomMd5 = "5acebd379a99de06b79bbbdf87ce6fbc";

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

    /// <summary>
    /// featpatch.py --spells insectplague --insectplague-price 4 --insectplague-energy 6.5 --insectplague-swarms 1,3,5,7,10
    /// --insectplague-time 0.1,1,2.5,10,25.5 --insectplague-dmin 0,1,9,20,0 --insectplague-dmax 0,2,30,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings InsectCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 }, Third: new[] { 1, 3, 5, 7, 10 },
            More: new IReadOnlyList<int>[] { new[] { 1, 10, 25, 100, 255 } });

    /// <summary>
    /// featpatch.py --spells gasarrow --gasarrow-price 4 --gasarrow-energy 6.5 --gasarrow-dmin 0,1,9,20,0 --gasarrow-dmax
    /// 0,2,30,20,255 --gasarrow-reach 10,30,64,100,255 --gasarrow-time 0.1,1,2.5,10,25.5 --gasarrow-hmin 0,1,5,20,0
    /// --gasarrow-hmax 0,2,9,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings GasCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 10, 30, 64, 100, 255 }, new[] { 1, 10, 25, 100, 255 }, new[] { 0, 1, 5, 20, 0 }, new[] { 0, 2, 9, 20, 255 }
            });

    /// <summary>featpatch.py --spells dimensiondoor --dimensiondoor-price 4 --dimensiondoor-energy 6.5 --dimensiondoor-dist 20,50,400.</summary>
    private static readonly BorrowedSpellSettings DoorCustom = new(4, 6.5f, new[] { 20, 50, 400 }, Array.Empty<int>(), RankCount: 3);

    /// <summary>featpatch.py version 14 (all twelve) on ADRIANNA26 (the stock file with three players turned on).</summary>
    private const string ThreePlayersMd5 = "3699b7ba405c6749cf21c7d7847922ef";

    /// <summary>
    /// Version 13 on the stock executable (featpatch.py version 13, all eleven at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version13Md5 = "b7edca53a290183aca5eb7d5e799a054";

    private const string Version13RunsBase64 =
        "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"
        + "OW/lVFBw1MnVJQvtx+H+uf8H/Njmcg==";

    /// <summary>
    /// Version 12 on the stock executable (featpatch.py version 12, all ten at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version12Md5 = "fcf340f0fbcaf9c680ac8ac178cf2108";

    private const string Version12RunsBase64 =
        "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";

    /// <summary>
    /// Version 11 on the stock executable (featpatch.py version 11, all nine at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version11Md5 = "35b1b1ba5fcd4508a1f6b5577fba8acf";

    private const string Version11RunsBase64 =
        "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";

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

    /// <summary>The words that send the game to it (all twelve on: Dimension Door needs none of its own), and the stretches of the pool it takes.</summary>
    private static readonly int[] Hooks =
    {
        0x0010F4AC, 0x0010F648, 0x001109C0, 0x0011B758, 0x0015E220, 0x0015E424, 0x0015E43C, 0x0015E50C, 0x00161EF8, 0x00161F00,
        0x00161F08, 0x001676E4, 0x00167AFC, 0x0016DD2C, 0x0016DD44, 0x0017A4BC, 0x0017B594, 0x0017E52C, 0x0017EA44, 0x0017EAB4,
        0x0018331C, 0x001B177C, 0x001B1784, 0x001BED7C, 0x001BED94, 0x001BEFD0, 0x001BF004, 0x0030A104, 0x0030A11C
    };

    /// <summary>Xantam's Fire's two (its maker's trail vtable); none of Kromlech's spells needs them.</summary>
    private static readonly int[] XantamOnly = { 0x001B177C, 0x001B1784 };

    /// <summary>Sorcerer's Blood's two: a level gained (FUN_00167390's call in the level-up) and the maximum mana (Willpower's rank there).</summary>
    private static readonly int[] SorcerersOnly = { 0x001676E4, 0x0017B594 };

    /// <summary>
    /// Xantam's Orbs alone: the six every spell needs (the level-up lists, the grant, the icons' loading, the cast table's three) and the
    /// HUD's three it shares with Kromlech's spells; nothing of its own. Dimension Door alone takes the same nine.
    /// </summary>
    private static readonly int[] OrbsHooks =
    {
        0x00161EF8, 0x00161F00, 0x00161F08, 0x0016DD44, 0x0017A4BC, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C
    };

    /// <summary>
    /// Inferno Arrow's seven: the spell start's mana and the arrow types and kinds, the arrow's constructor, update and touch, and
    /// icons[9]'s lookup. With the six every spell needs and the HUD's three, the sixteen it takes alone. Four of them (ArrowShared)
    /// Poison Gas Arrow needs too.
    /// </summary>
    private static readonly int[] InfernoOnly = { 0x0010F4AC, 0x0010F648, 0x001109C0, 0x0015E220, 0x0015E424, 0x0015E50C, 0x0016DD2C };

    /// <summary>
    /// Entangle's four, in the game's VineSeek update (nothing else runs it): its step (cos and sin, scaled to 200 a second), the
    /// vine's allocation (0x60) and the call of the VineSpell constructor (EN_VINE). With the six every spell needs and the HUD's three,
    /// the thirteen it takes alone.
    /// </summary>
    private static readonly int[] EntangleOnly = { 0x001BED7C, 0x001BED94, 0x001BEFD0, 0x001BF004 };

    /// <summary>
    /// Insect Plague's one: the poison's bite in the status effects (FUN_0011B3C8), so a bite of a monster its swarm poisoned is
    /// his. With the six every spell needs and the HUD's three, the ten it takes alone.
    /// </summary>
    private static readonly int[] InsectOnly = { 0x0011B758 };

    /// <summary>The four arrow hooks Inferno Arrow and Poison Gas Arrow share: the spell start's slot, the kinds, the arrow's constructor and update.</summary>
    private static readonly int[] ArrowShared = { 0x0010F4AC, 0x0010F648, 0x0015E220, 0x0015E50C };

    /// <summary>
    /// Poison Gas Arrow's own one: the arrow types' last test (type 38 an arrow). With the four arrow hooks it shares with Inferno
    /// Arrow, the poison's bite it shares with Insect Plague, the six every spell needs and the HUD's three, the fifteen it takes
    /// alone.
    /// </summary>
    private static readonly int[] GasOnly = { 0x0015E43C };

    private static readonly (int Start, int End)[] Used =
    {
        (0x00102910, 0x00102B5C), (0x00102D80, 0x00103000), (0x0010C338, 0x0010C6D0), (0x0010CD50, 0x0010CF30), (0x00118978, 0x00118D70),
        (0x00129318, 0x00129520), (0x00129C48, 0x00129D74), (0x0012A710, 0x0012A8A0), (0x0012BAE8, 0x0012BF84), (0x0012CD30, 0x0012D078),
        (0x0012F790, 0x0012FA00), (0x00130540, 0x00130708), (0x00131170, 0x001312BC), (0x00132018, 0x00132160), (0x00132D90, 0x00133348),
        (0x00133350, 0x001333EC), (0x00134CE0, 0x00134E0C), (0x00134F20, 0x001350D0), (0x0013CDF8, 0x0013CFFC), (0x0013D218, 0x0013D42C),
        (0x00166E28, 0x00166FB8), (0x0016ACB8, 0x0016AE7C), (0x00191A20, 0x00191BD8), (0x00191BE0, 0x00191D20), (0x00196AC0, 0x00196C0C),
        (0x00198970, 0x00198AB0), (0x00199A60, 0x00199B88), (0x001A28B0, 0x001A29F8), (0x0020B890, 0x0020BCB0), (0x00221B88, 0x002222C0),
        (0x00224B68, 0x00224C58)
    };

    /// <summary>The address is in one of the stretches of the pool the patch takes.</summary>
    private static bool InUsed(uint va) => Used.Any(u => u.Start <= va && va < u.End);

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

    /// <summary>The five version 6 had (all but Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version6Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.SorcerersBlood or BorrowedSpell.XantamOrbs or BorrowedSpell.InfernoArrow or
                                                BorrowedSpell.Entangle or BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or
                                                BorrowedSpell.DimensionDoor)).ToArray();

    /// <summary>The six version 7 had (all but Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version7Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.XantamOrbs or BorrowedSpell.InfernoArrow or BorrowedSpell.Entangle or
                                                BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor)).ToArray();

    /// <summary>The seven versions 9 and 8 had (all but Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version9Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.InfernoArrow or BorrowedSpell.Entangle or BorrowedSpell.InsectPlague or
                                                BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor)).ToArray();

    /// <summary>The eight version 10 had (all but Entangle, Insect Plague, Poison Gas Arrow and Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version10Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.Entangle or BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or
                                                BorrowedSpell.DimensionDoor)).ToArray();

    /// <summary>The nine version 11 had (all but Insect Plague, Poison Gas Arrow and Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version11Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor)).ToArray();

    /// <summary>The ten version 12 had (all but Poison Gas Arrow and Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version12Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor)).ToArray();

    /// <summary>The eleven version 13 had (all but Dimension Door).</summary>
    private static readonly BorrowedSpell[] Version13Spells = BorrowedSpells.All.Where(s => s != BorrowedSpell.DimensionDoor).ToArray();

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

    private byte[] Version11() => Earlier(Version11RunsBase64, Version11Md5);

    private byte[] Version12() => Earlier(Version12RunsBase64, Version12Md5);

    private byte[] Version13() => Earlier(Version13RunsBase64, Version13Md5);

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
        Assert.That(Md5(on), Is.EqualTo(AllTwelveMd5), "the same bytes as featpatch.py");
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
        Assert.That(Md5(Bytes(exe, "seven")), Is.EqualTo(SevenMd5),
                    "all but Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version10Spells));
        Assert.That(Md5(Bytes(exe, "eight")), Is.EqualTo(EightMd5), "all but Entangle, Insect Plague, Poison Gas Arrow and Dimension Door");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version11Spells));
        Assert.That(Md5(Bytes(exe, "nine")), Is.EqualTo(NineMd5), "all but Insect Plague, Poison Gas Arrow and Dimension Door");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version12Spells));
        Assert.That(Md5(Bytes(exe, "ten")), Is.EqualTo(TenMd5), "all but Poison Gas Arrow and Dimension Door");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version13Spells));
        Assert.That(Md5(Bytes(exe, "eleven")), Is.EqualTo(ElevenMd5), "all but Dimension Door");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InfernoArrow }));
        Assert.That(Md5(Bytes(exe, "inferno")), Is.EqualTo(InfernoMd5), "Inferno Arrow alone: featpatch.py --spells infernoarrow");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.Entangle }));
        Assert.That(Md5(Bytes(exe, "entangle")), Is.EqualTo(EntangleMd5), "Entangle alone: featpatch.py --spells entangle");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InsectPlague }));
        Assert.That(Md5(Bytes(exe, "insect")), Is.EqualTo(InsectMd5), "Insect Plague alone: featpatch.py --spells insectplague");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.PoisonGasArrow }));
        Assert.That(Md5(Bytes(exe, "gas")), Is.EqualTo(GasMd5), "Poison Gas Arrow alone: featpatch.py --spells gasarrow");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.DimensionDoor }));
        Assert.That(Md5(Bytes(exe, "door")), Is.EqualTo(DoorMd5), "Dimension Door alone: featpatch.py --spells dimensiondoor");
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
        Assert.That(BitConverter.ToInt32(bytes, data2 + 4), Is.EqualTo(FeatPatch.Version));
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
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVine + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameVine, 0x80).SequenceEqual(_stockBytes.AsSpan(gameVine, 0x80)), Is.True, "the game's vtable is never written");

        // the cast table: type 36 is Entangle's case, in the pool
        var table = FeatPatch.CastTableVa - VaToFile;
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(39));
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 35);
        Assert.That(InUsed(cast), Is.True, "type 36: its own cast");
        Assert.That(Enumerable.Range(23, 12).Concat(new[] { 36, 37, 38 }).Select(t => BitConverter.ToUInt32(bytes, table + 4 * t)).Distinct(),
                    Is.EqualTo(new[] { 0x001628F0u }),
                    "types 24-35, 37 (Insect Plague's, off), 38 (Poison Gas Arrow's: an arrow, never cast) and 39 (Dimension Door's, off): " +
                    "the default");

        // a second block that isn't version 14's: not WorldExplorer's
        var unmarked = (byte[])bytes.Clone();
        unmarked[data2 + 4] = 11;
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
    public void InsectPlague_IsTheShamansSwarm_WithItsSettingsInAThirdBlock()
    {
        RequireStock();
        var ip = BorrowedSpell.InsectPlague;
        var info = BorrowedSpells.Info(ip);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((48, 37, false, false, false, true, true, true, true, "Vahn", 1)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(ip)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That((info.ThirdMin, info.ThirdMax, info.FirstMax, info.SecondMax), Is.EqualTo((1, 10, 255, 255)), "1 to 10 swarms, 0 to 255 a sting");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)), Is.EqualTo(new[] { (1, 255, true, -1, 1) }),
                    "how long a swarm lasts, in tenths of a second");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 15f, new[] { 1, 1, 1, 1, 1 }, new[] { 3, 3, 3, 3, 3 }, Third: new[] { 1, 2, 3, 4, 5 },
            More: new IReadOnlyList<int>[] { new[] { 80, 80, 80, 80, 80 } })), Is.True,
                    "price 12, 15 mana; one swarm at rank 1 up to five at rank 5; stings of 1-3 for 8 s");
        Assert.That(BorrowedSpellTexts.NameKey(ip), Is.EqualTo("insectplague_name"));
        Assert.That(BorrowedSpellTexts.Of(ip).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "insectplague_name"), (l, "insectplague_desc") })));
        Assert.That(BorrowedSpellTexts.Of(ip).Where(t => t.Key == "insectplague_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Insect Plague", "Fléau d'insectes", "Insektenplage", "Piaga di Insetti", "Plaga de insectos" }),
                    "the D&D spell's name in each language");
        Assert.That(BorrowedSpellIcons.EntryName(ip), Is.EqualTo("icon_insectplague.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(ip), Is.EqualTo("icon_magicmissile.tex (Magic Missile's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [ip] = InsectCustom });
        var bytes = Bytes(exe, "insect-custom");
        Assert.That(Md5(bytes), Is.EqualTo(InsectCustomMd5));
        var reopened = OpenBytes(bytes, "insect-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { ip }));
        Assert.That(read[ip].Equals(InsectCustom), Is.True, "read back: 1-10 swarms, 0.1-25.5 s, 0-255 stings");

        // its hooks: the six every spell needs, the HUD's three and its own one, the poison's bite (a jal into the pool for the game's jalr)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(10));
        Assert.That(InsectOnly.All(hooks.Contains), Is.True);
        var bite = BitConverter.ToUInt32(bytes, 0x0011B758 - VaToFile);
        var biteTo = (int)((bite & 0x03FFFFFF) << 2);
        Assert.That((bite >> 26, Used.Any(u => u.Start <= biteTo && biteTo < u.End)), Is.EqualTo((3u, true)), "jal into the pool");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x0011B758 - VaToFile), Is.EqualTo(0x0100F809u), "(the game's jalr t0)");

        // the main block names it (its bit in the mask) but has no entry for it; the third block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x200u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        var data3 = FeatPatch.Data3Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data3, 4), Is.EqualTo("FEA3"));
        Assert.That(BitConverter.ToInt32(bytes, data3 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That((BitConverter.ToInt32(bytes, data3 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data3 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record");
        Assert.That(bytes[data3 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], Is.EqualTo(4));
        Assert.That(BitConverter.ToSingle(bytes, data3 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data3 + 0xC0, 22), Is.EqualTo("icon_insectplague.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data3 + 0xD8), Is.EqualTo(0x0030A928u), "its fallback: the game's icon_magicmissile.tex");
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectSwarmsAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 3, 5, 7, 10, 0, 0 }));
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectStingMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 9, 20, 0, 0, 0 }));
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectStingMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 30, 20, 255, 0, 0 }));
        Assert.That((BitConverter.ToSingle(bytes, data3 + 0x100), BitConverter.ToSingle(bytes, data3 + 0x104), BitConverter.ToInt32(bytes, data3 + 0x108),
                     BitConverter.ToInt32(bytes, data3 + 0x10C), BitConverter.ToSingle(bytes, data3 + 0x110), BitConverter.ToInt32(bytes, data3 + 0x114)),
                    Is.EqualTo((FeatPatch.InsectRange, FeatPatch.InsectSpeed, FeatPatch.InsectGather, FeatPatch.InsectPeriod, FeatPatch.InsectPoison,
                                FeatPatch.InsectKeep)));
        Assert.That((FeatPatch.InsectRange, FeatPatch.InsectSpeed, FeatPatch.InsectGather, FeatPatch.InsectPeriod, FeatPatch.InsectPoison,
                     FeatPatch.InsectKeep), Is.EqualTo((240f, 150f, 25, 50, 5f, 260)),
                    "within 240, half as fast again as the shaman's swarm, half a second gathering, a sting a second, 5 s of poison");
        Assert.That(bytes.Skip(data3 + 0x120).Take(0xC0).All(b => b == 0), Is.True, "its poison records (the game's run time) zero");

        // off: the third block is there with its defaults, and no entry
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.Entangle }));
        var entangle = Bytes(off, "insect-off");
        Assert.That(Encoding.ASCII.GetString(entangle, data3, 4), Is.EqualTo("FEA3"));
        Assert.That(BitConverter.ToInt32(entangle, data3 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(entangle.Skip(data3 + FeatPatch.InsectSwarmsAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(entangle, 0x0011B758 - VaToFile), Is.EqualTo(0x0100F809u), "the poison's bite the game's");

        // the InsectPlagueSpell vtable copy, all 0xA0 bytes of it (a swarm is a creature: the game's walk calls through +0x8C; a copy
        // of 0x80 crashed the first test): the game's (0x0031B530) but for its destructor (+0x0C) and update (+0x24); the game's draw
        const int gameSwarm = 0x0031B530 - VaToFile;
        var copy = FeatPatch.VTables.Single(v => v.GameVa == 0x0031B530).Va - VaToFile;
        Assert.That(FeatPatch.VTables.Single(v => v.GameVa == 0x0031B530 + 0x80).Va - VaToFile, Is.EqualTo(copy + 0x80), "its second piece follows");
        for (var at = 0; at < 0xA0; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copy + at);
            if (at is 0x0C or 0x24)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameSwarm + at)), $"+0x{at:X2}: the swarms' own");
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameSwarm + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameSwarm, 0xA0).SequenceEqual(_stockBytes.AsSpan(gameSwarm, 0xA0)), Is.True, "the game's vtable is never written");

        // the cast table: type 37 is Insect Plague's case, in the pool
        var cast = BitConverter.ToUInt32(bytes, FeatPatch.CastTableVa - VaToFile + 4 * 36);
        Assert.That(InUsed(cast), Is.True, "type 37: its own cast");

        // a third block that isn't version 14's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)11), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data3 + at] = value;
            Assert.That(OpenBytes(unmarked, "insect-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the shaman's swarm's code it relies on changed: not the game's
        foreach (var va in new[] { 0x001E0D58, 0x0031B530 + 0x2C, 0x001DFAB0 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "swarm").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x01), (0x20, (byte)37) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "insect-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Insect Plague"));
        }
    }

    [Test]
    public void DimensionDoor_IsIlivarrasTeleport_WithItsSettingsInAFifthBlock()
    {
        RequireStock();
        var dd = BorrowedSpell.DimensionDoor;
        var info = BorrowedSpells.Info(dd);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((50, 39, false, false, false, true, false, false, false, "Adrianna", 0)));
        Assert.That((info.FixedRanks, info.FirstMin, info.FirstMax, info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(dd)),
                    Is.EqualTo((3, 20, 400, true, false, true)), "3 ranks, a distance of 20 to 400 each; its own record, texts and icon");
        Assert.That(BorrowedSpells.All.Where(s => s != dd).All(s => BorrowedSpells.Info(s).FixedRanks == 5), Is.True, "every other spell: 5");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 100, 150, 200 }, Array.Empty<int>(), RankCount: 3)), Is.True,
                    "price 12, 10 mana; 100, 150 and 200 ahead at its 3 ranks");
        Assert.That(BorrowedSpellTexts.NameKey(dd), Is.EqualTo("dimensiondoor_name"));
        Assert.That(BorrowedSpellTexts.Of(dd).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "dimensiondoor_name"), (l, "dimensiondoor_desc"), (l, "dimensiondoor_hud") })));
        Assert.That(BorrowedSpellTexts.Of(dd).Where(t => t.Key == "dimensiondoor_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Dimension Door", "Porte dimensionnelle", "Dimensionstür", "Porta Dimensionale", "Puerta dimensional" }));
        Assert.That(BorrowedSpellTexts.Of(dd).Where(t => t.Key == "dimensiondoor_hud").Select(t => t.Text),
                    Is.EqualTo(new[] { "Distance: %d units", "Distance : %d unités", "Entfernung: %d Einheiten", "Distanza: %d unità", "Distancia: %d unidades" }),
                    "its own HUD line: the rank's distance");
        Assert.That(BorrowedSpellIcons.EntryName(dd), Is.EqualTo("icon_dimensiondoor.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(dd), Is.EqualTo("icon_sphere.tex (Otiluke's Resilient Sphere's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [dd] = DoorCustom });
        var bytes = Bytes(exe, "door-custom");
        Assert.That(Md5(bytes), Is.EqualTo(DoorCustomMd5));
        var reopened = OpenBytes(bytes, "door-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { dd }));
        Assert.That(read[dd].Equals(DoorCustom), Is.True, "read back: 20, 50 and 400 ahead");

        // its hooks: the six every spell needs and the HUD's three, nothing of its own (the same nine as Xantam's Orbs alone)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks, Is.EqualTo(OrbsHooks));

        // the cast table: type 39 its case, in the pool; type 38 (Poison Gas Arrow's, an arrow) the default
        var table = FeatPatch.CastTableVa - VaToFile;
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 38);
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(39));
        Assert.That(InUsed(cast) && cast == BorrowedSpells.Data(dd).Cast, Is.True, "type 39: its cast case, in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 37), Is.EqualTo(FeatPatch.CastDefault));

        // the main block names it (its bit in the mask) but has no entry for it; the fifth block has its entry, record and distances
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x800u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data5Va, Is.EqualTo(FeatPatch.Data4Va + FeatPatch.Data4Size), "right after the fourth block");
        var data5 = FeatPatch.Data5Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data5, 4), Is.EqualTo("FEA5"));
        Assert.That(BitConverter.ToInt32(bytes, data5 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That((BitConverter.ToInt32(bytes, data5 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data5 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That((bytes[data5 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], bytes[data5 + FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt]),
                    Is.EqualTo(((byte)4, (byte)3)), "price 4; its entry offers 3 ranks");
        var record = data5 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 14), Is.EqualTo("dimensiondoor\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)39, 1UL << 50)),
                    "type 39, bit 50");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data5 + 0xC0, 23), Is.EqualTo("icon_dimensiondoor.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data5 + 0xD8), Is.EqualTo(0x0030AAD0u), "its fallback: the game's icon_sphere.tex");
        Assert.That(Enumerable.Range(0, 8).Select(i => (int)BitConverter.ToUInt16(bytes, data5 + FeatPatch.DoorDistanceAt + 2 * i)),
                    Is.EqualTo(new[] { 0, 20, 50, 400, 400, 400, 0, 0 }), "the distances by rank (4 and 5, which no entry offers, as 3)");
        Assert.That(Encoding.ASCII.GetString(bytes, data5 + FeatPatch.DoorHudKeyAt, 18), Is.EqualTo("dimensiondoor_hud\0"));
        Assert.That(Enumerable.Range(0, 5).Select(i => BitConverter.ToSingle(bytes, data5 + FeatPatch.DoorMinAt + 4 * i)),
                    Is.EqualTo(new[] { 16f, 8f, 12f, 32f, 2f }), "the least way worth going, a try's step back, a step up, a drop, short of a wall");
        Assert.That(BitConverter.ToInt32(bytes, data5 + FeatPatch.DoorSparksAt), Is.EqualTo(50), "Ilivarra's 50 sparks a burst");
        Assert.That((FeatPatch.DoorMin, FeatPatch.DoorStep, FeatPatch.DoorUp, FeatPatch.DoorDown, FeatPatch.DoorBack, FeatPatch.DoorSparks),
                    Is.EqualTo((16f, 8f, 12f, 32f, 2f, 50)));

        // off: the fifth block is there with its defaults and no entry; type 39 the default
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        var orbs = Bytes(off, "door-off");
        Assert.That(Encoding.ASCII.GetString(orbs, data5, 4), Is.EqualTo("FEA5"));
        Assert.That(BitConverter.ToInt32(orbs, data5 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(Enumerable.Range(0, 8).Select(i => (int)BitConverter.ToUInt16(orbs, data5 + FeatPatch.DoorDistanceAt + 2 * i)),
                    Is.EqualTo(new[] { 0, 100, 150, 200, 200, 200, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(orbs, table + 4 * 38), Is.EqualTo(FeatPatch.CastDefault));

        // a fifth block that isn't version 14's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)13), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data5 + at] = value;
            Assert.That(OpenBytes(unmarked, "door-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the game code it relies on changed (Ilivarra's sparkle's 50, the screen's edges' margins, the walls' sweep, the floor, a
        // model's vertex count): not the game's
        foreach (var va in new[] { 0x001DEE44, 0x00183474, 0x00148C20, 0x00147800, 0x0013FA58 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "door").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x04), (0x20, (byte)39) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "door-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Dimension Door"));
        }
    }

    [Test]
    public void PoisonGasArrow_IsTheMonstersGasOnAnArrow_WithItsSettingsInAFourthBlock()
    {
        RequireStock();
        var pg = BorrowedSpell.PoisonGasArrow;
        var info = BorrowedSpells.Info(pg);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((49, 38, false, false, false, true, true, false, true, "Vahn", 4)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(pg)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)),
                    Is.EqualTo(new[] { (10, 255, false, -1, 1), (1, 255, true, -1, 1), (0, 255, false, 3, 1), (0, 255, false, -1, 1) }),
                    "the cloud's reach (at least 10, the game's constructor's least), its time in tenths of a second, its hits' least and most");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 8, 9, 10, 11, 12 }, new[] { 12, 14, 15, 17, 18 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 40, 45, 50, 55, 60 }, new[] { 40, 45, 50, 55, 60 }, new[] { 2, 2, 3, 3, 4 }, new[] { 3, 4, 4, 5, 6 }
            })), Is.True, "price 12, 10 mana; arrows of 8-12 up to 12-18; clouds of 40 for 4 s up to 60 for 6 s; gas of 2-3 up to 4-6");
        Assert.That(BorrowedSpellTexts.NameKey(pg), Is.EqualTo("poisongasarrow_name"));
        Assert.That(BorrowedSpellTexts.Of(pg).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "poisongasarrow_name"), (l, "poisongasarrow_desc") })));
        Assert.That(BorrowedSpellTexts.Of(pg).Where(t => t.Key == "poisongasarrow_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Poison Gas Arrow", "Flèche de gaz toxique", "Giftgaspfeil", "Freccia di Gas Velenoso", "Flecha de gas venenoso" }));
        Assert.That(BorrowedSpellIcons.EntryName(pg), Is.EqualTo("icon_poisongasarrow.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(pg), Is.EqualTo("icon_melfsacidarrow.tex (Melf's Acid Arrow's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [pg] = GasCustom });
        var bytes = Bytes(exe, "gas-custom");
        Assert.That(Md5(bytes), Is.EqualTo(GasCustomMd5));
        var reopened = OpenBytes(bytes, "gas-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { pg }));
        Assert.That(read[pg].Equals(GasCustom), Is.True, "read back: arrows of 0-255, clouds of 10-255 for 0.1-25.5 s, gas of 0-255");

        // its hooks: the six every spell needs, the HUD's three, the four arrow hooks it shares with Inferno Arrow, the poison's bite
        // it shares with Insect Plague, and its own one, the arrow types' last test (a j into the pool for the game's bnel)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(15));
        Assert.That(GasOnly.Concat(ArrowShared).Concat(InsectOnly).All(hooks.Contains), Is.True);
        Assert.That(new[] { 0x001109C0, 0x0015E424, 0x0016DD2C }.Any(hooks.Contains), Is.False,
                    "Inferno Arrow's own three (its touch, type 9 an arrow, icons[9]'s lookup) stay the game's");
        var types = BitConverter.ToUInt32(bytes, 0x0015E43C - VaToFile);
        var typesTo = (int)((types & 0x03FFFFFF) << 2);
        Assert.That((types >> 26, Used.Any(u => u.Start <= typesTo && typesTo < u.End)), Is.EqualTo((2u, true)), "j into the pool");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x0015E43C - VaToFile), Is.EqualTo(0x54620078u), "(the game's bnel v1, v0)");

        // the main block names it (its bit in the mask) but has no entry for it; the fourth block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x400u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data4Va, Is.EqualTo(FeatPatch.Data3Va + FeatPatch.Data3Size), "right after the third block");
        var data4 = FeatPatch.Data4Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data4, 4), Is.EqualTo("FEA4"));
        Assert.That(BitConverter.ToInt32(bytes, data4 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That((BitConverter.ToInt32(bytes, data4 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data4 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That(bytes[data4 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], Is.EqualTo(4));
        var record = data4 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 15), Is.EqualTo("poisongasarrow\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)38, 1UL << 49)),
                    "type 38 (an arrow's, as the spell start sees it), bit 49");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data4 + 0xC0, 24), Is.EqualTo("icon_poisongasarrow.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data4 + 0xD8), Is.EqualTo(0x0030A940u), "its fallback: the game's icon_melfsacidarrow.tex");
        Assert.That(bytes.Skip(data4 + FeatPatch.GasArrowMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 9, 20, 0, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasArrowMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 30, 20, 255, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasReachAt).Take(8), Is.EqualTo(new byte[] { 0, 10, 30, 64, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 5, 20, 0, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 9, 20, 255, 0, 0 }));
        Assert.That((BitConverter.ToInt32(bytes, data4 + FeatPatch.GasPeriodAt), FeatPatch.GasPeriod, FeatPatch.GasKind),
                    Is.EqualTo((50, 50, 0x40040)), "a hit a second, the game's cloud's kind of hit (the hit function's poison)");

        // off: the fourth block is there with its defaults, and no entry; the arrow hooks as Inferno Arrow alone has them
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InfernoArrow }));
        var inferno = Bytes(off, "gas-off");
        Assert.That(Encoding.ASCII.GetString(inferno, data4, 4), Is.EqualTo("FEA4"));
        Assert.That(BitConverter.ToInt32(inferno, data4 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(inferno.Skip(data4 + FeatPatch.GasReachAt).Take(8), Is.EqualTo(new byte[] { 0, 40, 45, 50, 55, 60, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(inferno, 0x0015E43C - VaToFile), Is.EqualTo(0x54620078u), "the arrow types' last test the game's");
        Assert.That(ArrowShared.All(va => BitConverter.ToUInt32(inferno, va - VaToFile) == BitConverter.ToUInt32(bytes, va - VaToFile)), Is.True,
                    "the shared arrow hooks: the same words (the code checks which of the two is on)");

        // the poisonGasSpell vtable copy: the game's (0x00313140) but for its update (+0x24); the game's constructor, draw and
        // destructor (a cloud is not a creature: 0x80 bytes, the next class's vtable follows)
        const int gameGas = 0x00313140 - VaToFile;
        var copy = FeatPatch.VTables.Single(v => v.GameVa == 0x00313140).Va - VaToFile;
        for (var at = 0; at < 0x80; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copy + at);
            if (at == 0x24)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameGas + at)), "+0x24: the clouds' own update");
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameGas + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameGas, 0x80).SequenceEqual(_stockBytes.AsSpan(gameGas, 0x80)), Is.True, "the game's vtable is never written");

        // a fourth block that isn't version 14's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)12), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data4 + at] = value;
            Assert.That(OpenBytes(unmarked, "gas-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the monsters' gas code it relies on changed (its constructor, its update's rhythm, its destructor, its vtable's update): not
        // the game's
        foreach (var va in new[] { 0x001A22C4, 0x001A1DE8, 0x001A26C0, 0x00313164 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "gas").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x02), (0x20, (byte)38) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "gas-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Poison Gas Arrow"));
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
        Assert.That(InUsed(update), Is.True, "in the pool");
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
                Assert.That(InUsed(word), Is.True, "in the pool");
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

        // saved with Power Attack, Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and
        // Dimension Door added: featpatch.py version 14's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PowerAttack] = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default,
            [BorrowedSpell.SorcerersBlood] = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default,
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v5-updated")), Is.EqualTo(AllTwelveMd5));

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

        // saved with Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door
        // added: featpatch.py version 14's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.SorcerersBlood] = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default,
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v6-updated")), Is.EqualTo(AllTwelveMd5));

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
    public void Version13_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v13 = Version13();
        var exe = OpenBytes(v13, "v13");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 13"));
        Assert.That(detail, Does.Contain("Dimension Door for Adrianna"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version13Spells));

        // saved as it is, or with Dimension Door added: featpatch.py version 14's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v13-updated")), Is.EqualTo(ElevenMd5));
        exe = OpenBytes(v13, "v13b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        });
        Assert.That(Md5(Bytes(exe, "v13-updated-all")), Is.EqualTo(AllTwelveMd5));

        // its own settings (in its four data blocks, where version 14 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v13.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data4 = FeatPatch.Data4Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data4 + FeatPatch.GasReachAt + 2] = 99;                      // Poison Gas Arrow: a cloud of 99 at rank 2
        custom[data4 + FeatPatch.GasTimeAt + 5] = 7;                        // ... for 0.7 s at rank 5
        custom[data4 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = 9;   // ... price 9
        BitConverter.GetBytes(3.5f).CopyTo(custom, data4 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v13-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PoisonGasArrow].MoreValues[0], Is.EqualTo(new[] { 40, 99, 50, 55, 60 }));
        Assert.That(read[BorrowedSpell.PoisonGasArrow].MoreValues[1], Is.EqualTo(new[] { 40, 45, 50, 55, 7 }));
        Assert.That((read[BorrowedSpell.PoisonGasArrow].Price, read[BorrowedSpell.PoisonGasArrow].Mana), Is.EqualTo((9, 3.5f)));
        var updated = OpenBytes(custom, "v13-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v13-custom-saved"), "v13-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v13, "v13-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v13-out-saved"), Is.EqualTo(_stockBytes));

        // version 13 with its code changed, or its fourth block marked 14: not recognised
        var broken = (byte[])v13.Clone();
        broken[FeatPatch.V13Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v13-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v13.Clone();
        marked[data4 + 4] = 14;
        Assert.That(OpenBytes(marked, "v13-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version12_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v12 = Version12();
        var exe = OpenBytes(v12, "v12");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 12"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version12Spells));

        // saved as it is, or with Poison Gas Arrow and Dimension Door added: featpatch.py version 14's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v12-updated")), Is.EqualTo(TenMd5));
        exe = OpenBytes(v12, "v12b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        });
        Assert.That(Md5(Bytes(exe, "v12-updated-all")), Is.EqualTo(AllTwelveMd5));

        // its own settings (in its three data blocks, where version 14 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v12.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data2 = FeatPatch.Data2Va - VaToFile;
        var data3 = FeatPatch.Data3Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data2 + FeatPatch.EntangleVinesAt + 2] = 9;                  // Entangle: 9 vines at rank 2
        custom[data3 + FeatPatch.InsectSwarmsAt + 3] = 7;                   // Insect Plague: 7 swarms at rank 3
        custom[data3 + FeatPatch.InsectTimeAt + 5] = 99;                    // ... for 9.9 s at rank 5
        BitConverter.GetBytes(3.5f).CopyTo(custom, data3 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v12-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.Entangle].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That(read[BorrowedSpell.InsectPlague].ThirdValues, Is.EqualTo(new[] { 1, 2, 7, 4, 5 }));
        Assert.That(read[BorrowedSpell.InsectPlague].MoreValues[0], Is.EqualTo(new[] { 80, 80, 80, 80, 99 }));
        Assert.That(read[BorrowedSpell.InsectPlague].Mana, Is.EqualTo(3.5f));
        var updated = OpenBytes(custom, "v12-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v12-custom-saved"), "v12-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v12, "v12-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v12-out-saved"), Is.EqualTo(_stockBytes));

        // version 12 with its code changed, or its third block marked 13: not recognised
        var broken = (byte[])v12.Clone();
        broken[FeatPatch.V12Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v12-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v12.Clone();
        marked[data3 + 4] = 13;
        Assert.That(OpenBytes(marked, "v12-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version11_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v11 = Version11();
        var exe = OpenBytes(v11, "v11");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 11"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version11Spells));

        // saved as it is, or with Insect Plague, Poison Gas Arrow and Dimension Door added: featpatch.py version 14's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v11-updated")), Is.EqualTo(NineMd5));
        exe = OpenBytes(v11, "v11b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        });
        Assert.That(Md5(Bytes(exe, "v11-updated-all")), Is.EqualTo(AllTwelveMd5));

        // its own settings (in its two data blocks, where version 14 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v11.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data2 = FeatPatch.Data2Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data2 + FeatPatch.EntangleVinesAt + 2] = 9;                  // Entangle: 9 vines at rank 2
        custom[data2 + FeatPatch.EntangleTimeAt + 5] = 99;                  // ... held 9.9 s at rank 5
        var read = OpenBytes(custom, "v11-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.Entangle].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That(read[BorrowedSpell.Entangle].MoreValues[1], Is.EqualTo(new[] { 40, 45, 50, 55, 99 }));
        var updated = OpenBytes(custom, "v11-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v11-custom-saved"), "v11-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v11, "v11-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v11-out-saved"), Is.EqualTo(_stockBytes));

        // version 11 with its code changed, or its second block marked 12: not recognised
        var broken = (byte[])v11.Clone();
        broken[FeatPatch.V11Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v11-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v11.Clone();
        marked[data2 + 4] = 12;
        Assert.That(OpenBytes(marked, "v11-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
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

        // saved as it is, or with Entangle, Insect Plague, Poison Gas Arrow and Dimension Door added: featpatch.py version 14's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v10-updated")), Is.EqualTo(EightMd5));
        exe = OpenBytes(v10, "v10b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        });
        Assert.That(Md5(Bytes(exe, "v10-updated-all")), Is.EqualTo(AllTwelveMd5));

        // its own settings (in its data block at 0x00221B88, where version 14's is too, in version 10's layout) are read and kept
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

        // saved as it is (its data block moves to version 14's place), or with Inferno Arrow, Entangle, Insect Plague, Poison Gas
        // Arrow and Dimension Door added: featpatch.py version 14's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v9-updated")), Is.EqualTo(SevenMd5));
        exe = OpenBytes(v9, "v9b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        });
        Assert.That(Md5(Bytes(exe, "v9-updated-all")), Is.EqualTo(AllTwelveMd5));

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

        // saved as it is: featpatch.py version 14's file with its seven (the orbs in a fan now)
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

        // saved with Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Dimension Door added: featpatch.py
        // version 14's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v7-updated")), Is.EqualTo(AllTwelveMd5));

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
                    "all twenty-nine hooks");
        Assert.That(bytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750).SequenceEqual(_stockBytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750)),
                    Is.True, "the game's ability table (Power Attack's, Sorcerer's Blood's and Inferno Arrow's records among them) is untouched");
        Assert.That(bytes.Take(0x100), Is.EqualTo(_stockBytes.Take(0x100)), "the ELF header and program headers are the game's");
        Assert.That(BgdaExecutable.BorrowedSpellsBytes, Is.EqualTo(Used.Sum(u => u.End - u.Start)));
        Assert.That(BgdaExecutable.FeatPoolBytes, Is.EqualTo(32724));
        Assert.That(BgdaExecutable.FeatPoolStretches, Is.EqualTo(61));

        // Kromlech's four alone: Xantam's Fire's two hooks, Sorcerer's Blood's two, Inferno Arrow's seven, Entangle's four, Insect
        // Plague's one and Poison Gas Arrow's one stay the game's
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Kromlech));
        bytes = Bytes(exe, "where-krom");
        Assert.That(XantamOnly.Concat(SorcerersOnly).Concat(InfernoOnly).Concat(EntangleOnly).Concat(InsectOnly).Concat(GasOnly)
                        .All(va => BitConverter.ToUInt32(bytes, va - VaToFile) == BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.True);

        // Xantam's Orbs alone: nine hooks, and the same stretches of the pool as all twelve
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
            [BorrowedSpell.Entangle] = EntangleCustom,
            [BorrowedSpell.InsectPlague] = InsectCustom,
            [BorrowedSpell.PoisonGasArrow] = GasCustom,
            [BorrowedSpell.DimensionDoor] = DoorCustom
        };
        var exe = Stock();
        exe.SetBorrowedSpells(custom);
        var reopened = OpenBytes(Bytes(exe, "custom"), "custom2");
        AssertSame(reopened.GetBorrowedSpells(), custom);

        // a spell added and two taken out in place, the orbs, the flames, the vines, the swarms, the gas and the door's distances
        // changed, then back to all twelve at the tested settings
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
        changed[BorrowedSpell.InsectPlague] = InsectCustom with
        {
            Third = new[] { 2, 2, 9, 9, 10 }, More = new IReadOnlyList<int>[] { new[] { 7, 8, 9, 10, 11 } }
        };
        changed[BorrowedSpell.PoisonGasArrow] = GasCustom with
        {
            First = new[] { 0, 1, 5, 5, 5 },
            More = new IReadOnlyList<int>[] { new[] { 255, 10, 11, 12, 13 }, new[] { 5, 6, 7, 8, 9 }, new[] { 9, 9, 9, 9, 9 }, new[] { 9, 10, 11, 12, 13 } }
        };
        changed[BorrowedSpell.DimensionDoor] = DoorCustom with { First = new[] { 400, 20, 123 }, Price = 30 };
        reopened.SetBorrowedSpells(changed);
        AssertSame(OpenBytes(Bytes(reopened, "changed"), "changed2").GetBorrowedSpells(), changed);
        reopened.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Md5(Bytes(reopened, "custom-back")), Is.EqualTo(AllTwelveMd5));

        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(16, h)), Is.EqualTo(new[] { 16, 17, 19, 21, 25 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(10, h)), Is.EqualTo(new[] { 10, 11, 13, 15, 19 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(12, h)), Is.EqualTo(new[] { 12, 13, 15, 17, 21 }),
                    "Xantam's Orbs' (Dimension Door's first three)");
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
        var ip = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default;
        var pg = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default;
        var dd = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default;
        IReadOnlyList<int>[] PgRow(int k, int[] row) => pg.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        IReadOnlyList<int>[] Row(int k, int[] row) => ia.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        IReadOnlyList<int>[] EnRow(int k, int[] row) => en.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        var bad = new (BorrowedSpell, BorrowedSpellSettings)[]
        {
            (BorrowedSpell.DimensionDoor, dd with { First = new[] { 19, 150, 200 } }), (BorrowedSpell.DimensionDoor, dd with { First = new[] { 100, 150, 401 } }),
            (BorrowedSpell.DimensionDoor, dd with { First = new[] { 100, 150, 200, 200, 200 } }), (BorrowedSpell.DimensionDoor, dd with { First = new[] { 100, 150 } }),
            (BorrowedSpell.DimensionDoor, dd with { RankCount = 5 }), (BorrowedSpell.DimensionDoor, dd with { RankCount = 1 }),
            (BorrowedSpell.DimensionDoor, dd with { Second = new[] { 1, 2, 3 } }), (BorrowedSpell.DimensionDoor, dd with { Third = new[] { 1, 2, 3 } }),
            (BorrowedSpell.DimensionDoor, dd with { More = ip.More }), (BorrowedSpell.DimensionDoor, dd with { Mana = 0f }),
            (BorrowedSpell.DimensionDoor, dd with { Price = 31 }), (BorrowedSpell.XantamOrbs, xo with { RankCount = 3 }),
            (BorrowedSpell.AntimagicField, am with { First = new[] { 4, 5, 6 }, RankCount = 3 }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(0, new[] { 9, 45, 50, 55, 60 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(0, new[] { 40, 45, 50, 55, 256 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(1, new[] { 40, 0, 50, 55, 60 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(1, new[] { 40, 45, 50, 55, 256 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(2, new[] { 2, 2, 5, 3, 4 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(3, new[] { 3, 4, 4, 5, 256 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = pg.MoreValues.Take(3).ToArray() }), (BorrowedSpell.PoisonGasArrow, pg with { More = null }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = ip.More }), (BorrowedSpell.PoisonGasArrow, pg with { Third = new[] { 1, 1, 1, 1, 1 } }),
            (BorrowedSpell.PoisonGasArrow, pg with { First = new[] { 8, 9, 16, 11, 12 } }), (BorrowedSpell.PoisonGasArrow, pg with { Second = new[] { 12, 14, 15, 17, 256 } }),
            (BorrowedSpell.PoisonGasArrow, pg with { Mana = 0f }), (BorrowedSpell.PoisonGasArrow, pg with { Price = 31 }),
            (BorrowedSpell.InsectPlague, ip with { More = pg.More }), (BorrowedSpell.InfernoArrow, ia with { More = pg.More }),
            (BorrowedSpell.InsectPlague, ip with { Third = null }), (BorrowedSpell.InsectPlague, ip with { Third = new[] { 0, 2, 3, 4, 5 } }),
            (BorrowedSpell.InsectPlague, ip with { Third = new[] { 1, 2, 3, 4, 11 } }), (BorrowedSpell.InsectPlague, ip with { More = null }),
            (BorrowedSpell.InsectPlague, ip with { More = en.More }), (BorrowedSpell.InsectPlague, ip with { More = new IReadOnlyList<int>[] { new[] { 80, 0, 80, 80, 80 } } }),
            (BorrowedSpell.InsectPlague, ip with { More = new IReadOnlyList<int>[] { new[] { 80, 80, 80, 80, 256 } } }),
            (BorrowedSpell.InsectPlague, ip with { First = new[] { 1, 1, 4, 1, 1 } }), (BorrowedSpell.InsectPlague, ip with { Second = new[] { 3, 3, 3, 3, 256 } }),
            (BorrowedSpell.InsectPlague, ip with { Mana = 0f }), (BorrowedSpell.InsectPlague, ip with { Price = 31 }),
            (BorrowedSpell.Entangle, en with { More = ip.More }), (BorrowedSpell.XantamOrbs, xo with { More = ip.More }),
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
        Assert.That(saved.Get("insectplague_name"), Is.EqualTo("Insect Plague"));
        Assert.That(saved.Get("insectplague_name", 2), Is.EqualTo("Insektenplage"));
        Assert.That(saved.Get("insectplague_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        Assert.That(saved.Get("poisongasarrow_name"), Is.EqualTo("Poison Gas Arrow"));
        Assert.That(saved.Get("poisongasarrow_name", 1), Is.EqualTo("Flèche de gaz toxique"));
        Assert.That(saved.Get("poisongasarrow_name", 4), Is.EqualTo("Flecha de gas venenoso"));
        Assert.That(saved.Get("poisongasarrow_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        Assert.That(saved.Get("dimensiondoor_name"), Is.EqualTo("Dimension Door"));
        Assert.That(saved.Get("dimensiondoor_name", 2), Is.EqualTo("Dimensionstür"));
        Assert.That(saved.Get("dimensiondoor_hud"), Is.EqualTo("Distance: %d units"), "its own HUD line: %d the rank's distance");
        for (var language = 0; language < 5; language++)
        {
            var keys = saved.Entries(language).Select(e => e.Key).ToList();
            Assert.That(keys, Is.EqualTo(new[]
            {
                "burninghands_name", "odd", "powerattack_name", "xantamfire_name", "xantamfire_desc", "bladebarrier_name", "bladebarrier_desc",
                "antimagicfield_name", "antimagicfield_desc", "antimagicfield_hud", "righteousmight_name", "righteousmight_desc", "righteousmight_hud",
                "xantamorbs_name", "xantamorbs_desc", "entangle_name", "entangle_desc", "insectplague_name", "insectplague_desc",
                "poisongasarrow_name", "poisongasarrow_desc", "dimensiondoor_name", "dimensiondoor_desc", "dimensiondoor_hud"
            }), "after the file's own entries, spell by spell");
            foreach (var hud in new[] { "antimagicfield_hud", "righteousmight_hud", "dimensiondoor_hud" })
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
            [BorrowedSpell.Entangle] = "a27ef3840af3753c6772028cd928aa66", [BorrowedSpell.InsectPlague] = "6e2878b6609482d7201c7480555c811a",
            [BorrowedSpell.PoisonGasArrow] = "f3c2e780029d954b996989b49541b84b", [BorrowedSpell.DimensionDoor] = "672b2d3ceb1db3c7d1325390e9ad6882"
        };
        var own = BorrowedSpells.All.Where(BorrowedSpellIcons.HasOwnIcon).ToList();
        Assert.That(own, Is.EqualTo(md5.Keys.OrderBy(s => s)),
                    "every spell but Power Attack (the game's icon_powerattack.tex) and Sorcerer's Blood (a passive: no HUD slot)");
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.PowerAttack), Is.EqualTo("icon_powerattack.tex"));
        foreach (var spell in own)
        {
            Assert.That(BorrowedSpellIcons.Texture(spell).Length, Is.EqualTo(3568));
            Assert.That(Md5(BorrowedSpellIcons.Texture(spell)), Is.EqualTo(md5[spell]),
                        "make_xantam_icon.py's, make_kromlech_icons.py's, make_xantam_orbs_icon.py's, make_inferno_arrow_icon.py's, " +
                        "make_entangle_icon.py's, make_insect_plague_icon.py's, make_poison_gas_arrow_icon.py's and " +
                        "make_dimension_door_icon.py's textures");
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
            "exclaim.tex", "icon_antimagicfield.tex", "icon_balllightning.tex", "icon_bladebarrier.tex", "icon_dimensiondoor.tex",
            "icon_entangle.tex", "icon_healthpotion.tex",
            "icon_infernoarrow.tex", "icon_insectplague.tex", "icon_poisongasarrow.tex",
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
