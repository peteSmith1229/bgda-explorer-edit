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
/// The borrowed spells, the Enhancements tab (BgdaExecutable.BorrowedSpells.cs): Xantam's Fire for Adrianna; Power Attack
/// (the game's own feat), Blade Barrier, Antimagic Field and Righteous Might for Kromlech. On the stock PAL executable in
/// Test_files (skipped when it is not present), with the Three Players tab switching three players underneath them, and
/// version 5 (the first test of Kromlech's spells) brought up to date; their names in GTEXT.LMP and their icons in
/// ICONS.LMP (archives built here). The patch is featpatch.py version 6: the checksums are of the files featpatch.py
/// writes, and what it does in the game was checked by emulation (test_feats6.py on eleven files: 176 checks on each
/// with all five; 63 broken copies, all caught on two bases).
/// </summary>
[TestFixture]
public class BgdaBorrowedSpellsTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>
    /// featpatch.py version 6 on the stock executable: all five at the tested settings, at the test files' (price 0, 4 mana), Kromlech's
    /// four alone and Power Attack alone.
    /// </summary>
    private const string AllFiveMd5 = "1c131fd0e186959692c371b61291b66f", TestSettingsMd5 = "253b05b6fbc499290ccfc5b763f93dc5",
                         KromlechMd5 = "9440349e3ff95df8815c66e8d88d968a", PowerAttackMd5 = "e958f42a2d8ae785988bafc386f0b073";

    /// <summary>featpatch.py version 6 (all five) on ADRIANNA26 (the stock file with three players turned on).</summary>
    private const string ThreePlayersMd5 = "0b779cd88a544c7707b791aa2da7a86c";

    /// <summary>
    /// Version 5 on the stock executable (featpatch.py version 5, all four at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version5Md5 = "64c66cca454d8dbf6e3883b7db97dd76";

    private const string Version5RunsBase64 =
        "eNq1WW1wXNV5fu7HyndXK7hayWJlnORee1dayXJYORKVyxKv5GVnMU7RuCajYRxYf0aubaIaMqNMPZ1rcKht9OUEGGj5saWSjRl92YhESUhQElsW"
        + "GULcZiZxp6GrtgymCWmUNk0cAtw+5+5dabWSM/xw78yd9+45577nnOd93ue8V9I3AmkZiOF4zwpgJXi14ESPhvov+7Gu1zROQkNaUuAJRe1v12Yw"
        + "br+Ac/Y4ztuTeMmewoQ9g1dt07LgR3mvhGDofhgB04Cso65Ph3e0kraSNkgbpK1AIDawflvNahjTO5TWaUBKrgbe/tOXVb94z4wDZjvkFO5+6gO7"
        + "NKzgwHoZ6fJ3lLpw9FpdmO1DErSwF6V1v7XTK/3YEs77nBk83r9dubuvGUa52Mut8Ep6O9CIuLM3zjPd4c6pQ5o+qiEJPH/VotVpK9021W2rpA26"
        + "bZrbFqQ1jYyiY6jWwOnaf1+j+g2sCE0g8JWXUfEVAxO1wDHu2dJVPFnzvn1KF3P7iXETtEZiEvqd7XXWY0YM2YzGZbMlLXNPsczjWrOZcvYPGaWx"
        + "m/5a9Ws40PwLG46POLS+mcET/fVTXklgxXhkGY8s45FlPLKMR5bxyPbZuOUyvl2rYVaBez1T/nHH9rzhuEL6rYBjO1w7c1Ou//JvP+ZYw20PulZ3"
        + "reba2f/I2SuuvezaGddOuXaS1roTsMizDDlUgSrG6ic1TWOqf0A+1teCwwNxPDyQQudAO/YPrPa92F+D1FCEcTYnARVpx9ELu1S/g9lkHEE8OZ1v"
        + "xwtWopTLi+In0yL+CjFW4F3vi3thxTWsgzd2+dd1YTMVkYDmizpuv2haQH08ILcCDk/iKR22vaVm7TP1UrykNBTsOCA3oedpM/2EhD1+KLxTGB57"
        + "/fuCo5YUwXenZYdDj04zMsl2ZK6eZl8Kg/35vkq3bzv7OjBxVf2W6k9DDa1BXN+K58egfzWR0a2k9WkpBu1o4lFdShp4fbqtYmXS2sS2rqOJEH40"
        + "LetVyRAET70JbPVC4W0acbSTfymc7jdbZpkzlvzkhRK/4A199wfIpfEntHALUsM7cHePx80HP+p6ZT5bEH2Rke+QVxL0mjS5cudJjyZDDl1mjMCc"
        + "1jBok+N2kDlvMOcroDNueo15nHi1lsOMazLHZJ/+BeOoeMI+xCuteK79gurpFe2iX2W7wxt3LH1mZWIgeN7CteXfmVS9IxIaaxRE6q/acPNV62Ne"
        + "NPoYp/z7aXhqNPoUvxvhiYm2ubNDIcVty8/BPWS59izXnhV5EOUeW34MDDAjprjHKPeoc29B7s2gngkNM49nYA5MMWyRPhJrrJJWpQ3SarSOPkUy"
        + "ihkFPttALrQAFcS6/RWBpzYaA0b9uTU301LrqonZxtqc1pmX8loneGrGAxLzedS2tVDCeVetEXpgHPT4zaghnw8X6GFqXhOHmtr8MYXaV4K0g9Fc"
        + "qz9Wwt+y+7uLv7di68hzg0/0V6MsVrZvpDnYcUh6KFYWe/B/RppNPrNvTD5YSg59Zui+++rCeT296uppFGqs60vHQqkzXEMEMrHKEivieY54nnfw"
        + "jBNPsQ8f9WyV0FmucwvPkHac7Ekh7vLtnt67qN2O3hDzAWKeJuadxLyLmHfzDLF4huSwT0s5TQj1pagJ7dQEge3Aei0sdPuj6nUTcPGjaLZOzV5O"
        + "qwt1+prQ6bg3Zsknhv+YViuuVqcLtFrEjvvNcr9Z7jfL/Wa53yz3m80Qu7e/X6I5ujmfb+Pk4zny8Tz5+JL9ryc9jt5Vca4j67y9T67z9co7mP8x"
        + "f6gBB1fKO3z4i5A/VMdnoQvY7oPC26SG5XOVZ3KfhUCNwTM5dwqkyGfv+mpqgNmelrUqoXWlktl+j5SAP3bXn/U0m13AJpRRcu7tLcG7TuwkvBH+"
        + "g/3DlR3kBnNRz+VzR00Vqjnu5n7b3sg93DuiIlj7B9u4ReU4KtGIbTfWzBKPNLbViHzkHsmjc+TRefLopfm8bH8TeFfKnQ8GcYgQhyhxaKFuxvDy"
        + "1Ti+djXl1hkUy747XN0WNUeLU29kkOeq0OgmhB43WnO6Knuk5Omitja2RRAIPUf+VvN8KNtX1xyMe5kjXuZIXbPJZ/aNKSENwt9qDI6tJqdkt24Q"
        + "eWx5Kh2/7OvP91W6fUwD3l+7qlL7g+TuXFtuXtGnsU1HhtqSoa5kxtYx147/17GQ0BWd3PynTYx72krY9raan9rxKucsJm7EJEtMssTkEjG5REwu"
        + "pRxMZxWh37MOj8bJo3Pk0avzeqaTP3P1OX0SnBSauf1Tqv+yPhQSWPrnsfT2riAvEvD1eVgvxVj/fWD7Y7bd0xzqOMgNrBvZR20SOiXuHkP1y6wL"
        + "hc/b1zvYkyeRDeI318F1nWOsX81SbssNrm/VN1Qtx5tcTdL9rIQGjvTiMd2AFPLiqG7SBmjX0NbTinH3/d7jvPcZ15rGlAzN07SZWhGLeJDXCDMu"
        + "S3ElcCqllJ/6Kcc2Etc5YuKM15eOj6dk6qpXSit1T1WjLrwBvtCtqJcCKTHL2bEKvr/tXdVvNYrYxEVbP9InEsZuK4lOK6FzT//IeYSuzM9jLL+u"
        + "LsV7SvjsplULfGeW8a2K584TifQBK1k4R5xzjDOuDZiiFh5dRguPLlO7Hv1ItSswyHpqaLoB09OPNpYkLd6mLnj3DXIq0s/KsFzFxtr37cp5rjSh"
        + "jhrpYy1b75w7HH5RrEty8kC+6KyHNc33oCbbdE9yKsi80KzE7bQ/t6W7xDsMf/JRXeY+5GQTSpJv2XDaH5eQ/DfqBtcgjbscnyIW438nYVjKKVnX"
        + "C7fm6kzXFtez6ZW59pT7+0bWs8530tmc/+Nnl5/fuqD/v82fr6FF/VzI7XicnI7PyGkl8NSAfNzR+o+Jeo91sDk5I8+q/9lXNl8LRnup5Y7TwnpE"
        + "5G/U1YiWvxoKlbl6L+rXdtavXlG7WXGnBjUjlmwPMvd5Ls6qb9UI/c3VL5Dz9WTwH2SoMubzgedqL2RfuMRdRwr3vOnBnnLbrgu/x5i38+zo+2Uu"
        + "12db/KFgehj1rGOsEyNhs3NYikqjb4a6L0hrMLty8lOT4Zt5BnHJf2PbSsiDzbo4i8QZ1M6zKK81gstmsI3M2d1jajLb1g6raAtFsDnyezvt1Kd8"
        + "/nIEypkGrnm5sYmCsSlsfjMFxQ53XRDr+SA/z42sKwvryML4FOq5qDdj9CHzDAu1e3mG+0bDPLvYNipqzvx5l687r1tjFsX8evVePaTYlc+ZTWu1"
        + "Vq7PbFBgVC5oeqKnsF9oXGoD9c046vYf60kNywgS2QG5p0dF7/pGZMqttJ9jy8gNxrdrWJpTf9R7M2bLy3AtSu5VSJiM8PvuFniu9Gqovk2DUWFO"
        + "TUrdSvCrexj7tVMtSnBqEhsY71WoVgI8h2bpYxV9ZDzXojx/K2YGn+nXPf/cu3amWvFh420VMG7x41e95Ryje66MLP3mOc3YnbFNY1bK4xK9YyiU"
        + "q2EGs+L7gmOIzZmsqF8+fMbl67yfIZu5wTWpscDPVX9wi8CB0eN7zrdd+mji8msr/Dkly+mbOCP1r8vYIWFxfhecdWb8e3LujKs49YpXfCdv43fy"
        + "nw/lc31NQa5fZq7rBbleVpDr7cprfaLGEXyaQOD5l1HxfHHNf3NB3k8x70uL8v6NKPVAme7Lv3/5Ovl/vbqk8LspV5eoMeuBhflFbaK4tcmNrDve"
        + "+cEyGBec2+ZAHHk8awvwhOe/Ry3lx31zxLW8ANebCjU0VcEc9I6yhJhOuTWfqAtz3+++xNp2HzH3jRZralkRtvWPrSCGaXmuCFMRo8h8jKx5jPP5"
        + "9Inr5FM1eZ5b7696q9znFGYvHlP+/qJYn8NJVydmF32PTFwV8eqUKlnfViZf6fL4OyW8zd9vL9TzE5fy/GW/g/W212V81jmn33H/6vSzM4FlT0m9"
        + "LndKzv36xv/Vac7dybYXWVGrnphEBS/Vd6OE1sMK06evJVIKkdpNxd81n4mi6lr0rvMFtPTdwvGs4uQBeWufiMLKP6JqeRXzuyrW5ahYsFYoWbDj"
        + "ChXjQHgVviQFeBIEO64t/G5ZYPDAnQp+R3STd7Vu97h/3VPd22DDBdZhzF787bPPbnL+aIipVmBiM6w7RHXVBkXxePIviihJkizOOciK6inB/LV/"
        + "9xceerB750OP7Dy0b//hvZ98ZG83Ci+3EMPCkAcf2nloL4qvgv49ex/evaQfHh0F25i/ZNfuOrhzz95dOw8f3r/38HIzLOpfbgaPb+FZW+qfy9t/"
        + "aOfn9+/et3/vwT1LZyjqXzqDp8CpvtT/4f2f73xk7xe++PAh8bDUf1H/Mv79C88GBjYpIqYFsGLZy4c9UBik3DK6Wm3cD5GMH0oijBn7X+jnhOxi"
        + "Xojhcs4MOpM4VHOdzeCHoARgi7RFOiJl7EWDtWLMlvozHX/z3lt/hqchnAxKqvRhsT+9GKOl/tYs8tfVej/e43YJEj12o8ifIZhWovkErJqvTGcN"
        + "8xu+vMHpzLSK9VBR6KFBekAayLn1VX48smHBivHMr08vypVCEIuyxelfDMqiEU7/4k0u6i/iYOcXizAt4tCS/j1GFeYiVXiktgrbI7nn4ttarzt2"
        + "ILS0r8FtW/2JhbYja6vwG2N5X4W3Tu0f7ynHO98MLA7Di1HMnY2im1ZcB9sUJHBjrmufLIHX4cVjDUfktF7hva3EIUjj+ydLHMrfy/M+8csSJz1e"
        + "K31lYgWEBE4Pqg3/u8IZ8MAuVUu8l3v+kyHVP1HwvHqz5gz/y0b1SG0pfM7/zdb23SS1NVlQzxlOZ+q5wGr3ceJbgf8DJ6YW9Q==";

    /// <summary>The words that send the game to it (all five on), and the stretches of the pool it takes.</summary>
    private static readonly int[] Hooks =
    {
        0x00161EF8, 0x00161F00, 0x00161F08, 0x00167AFC, 0x0016DD44, 0x0017A4BC, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C,
        0x001B177C, 0x001B1784, 0x0030A104, 0x0030A11C
    };

    /// <summary>Xantam's Fire's two (its maker's trail vtable); none of Kromlech's spells needs them.</summary>
    private static readonly int[] XantamOnly = { 0x001B177C, 0x001B1784 };

    private static readonly (int Start, int End)[] Used =
    {
        (0x00102910, 0x00102B70), (0x00102D80, 0x00103000), (0x0010C338, 0x0010C6C8), (0x0010CD50, 0x0010CF3C), (0x00118978, 0x00118D6C),
        (0x00129320, 0x00129504), (0x00129C48, 0x00129D68), (0x0012A710, 0x0012A878), (0x0012BAE8, 0x0012BE9C), (0x00132D90, 0x00133340)
    };

    /// <summary>The game's Power Attack record (its mana is the Feats &amp; Spells tab's: 5 in the game).</summary>
    private const int PowerRecordVa = 0x0030E2E0;

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

    /// <summary>The four version 5 had (all but Power Attack).</summary>
    private static readonly BorrowedSpell[] Version5Spells =
        { BorrowedSpell.XantamFire, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField, BorrowedSpell.RighteousMight };

    /// <summary>The stock executable with version 5 in, as featpatch.py version 5 wrote it.</summary>
    private byte[] Version5()
    {
        using var packed = new MemoryStream(Convert.FromBase64String(Version5RunsBase64));
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

        Assert.That(Md5(bytes), Is.EqualTo(Version5Md5));
        return bytes;
    }

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
        Assert.That(Md5(on), Is.EqualTo(AllFiveMd5), "the same bytes as featpatch.py");
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

        // saved with Power Attack added: featpatch.py version 6's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PowerAttack] = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v5-updated")), Is.EqualTo(AllFiveMd5));

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
        Assert.That(bytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750).SequenceEqual(_stockBytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750)),
                    Is.True, "the game's ability table (Power Attack's record among them) is untouched");
        Assert.That(bytes.Take(0x100), Is.EqualTo(_stockBytes.Take(0x100)), "the ELF header and program headers are the game's");
        Assert.That(BgdaExecutable.BorrowedSpellsBytes, Is.EqualTo(Used.Sum(u => u.End - u.Start)));
        Assert.That(BgdaExecutable.FeatPoolBytes, Is.EqualTo(32724));
        Assert.That(BgdaExecutable.FeatPoolStretches, Is.EqualTo(61));

        // Kromlech's four alone: Xantam's Fire's two hooks stay the game's
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
            [BorrowedSpell.PowerAttack] = new(9, 0f, new[] { 1, 20, 3, 1, 2 }, Array.Empty<int>()),
            [BorrowedSpell.AntimagicField] = new(30, 100f, new[] { 1, 60, 2, 3, 4 }, Array.Empty<int>()),
            [BorrowedSpell.RighteousMight] = new(0, 0.5f, new[] { 0, 200, 5, 6, 7 }, new[] { 90, 0, 1, 2, 3 })
        };
        var exe = Stock();
        exe.SetBorrowedSpells(custom);
        var reopened = OpenBytes(Bytes(exe, "custom"), "custom2");
        AssertSame(reopened.GetBorrowedSpells(), custom);

        // a spell added and two taken out in place, then back to all five at the tested settings
        var changed = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(custom) { [BorrowedSpell.BladeBarrier] = BorrowedSpells.Info(BorrowedSpell.BladeBarrier).Default };
        changed.Remove(BorrowedSpell.XantamFire);
        changed.Remove(BorrowedSpell.PowerAttack);
        reopened.SetBorrowedSpells(changed);
        AssertSame(OpenBytes(Bytes(reopened, "changed"), "changed2").GetBorrowedSpells(), changed);
        reopened.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Md5(Bytes(reopened, "custom-back")), Is.EqualTo(AllFiveMd5));

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
        var pa = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default;
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
            (BorrowedSpell.RighteousMight, rm with { First = new[] { 10, 20, 30, 40, 201 } }), (BorrowedSpell.RighteousMight, rm with { Second = new[] { 91, 20, 30, 40, 50 } }),
            (BorrowedSpell.PowerAttack, pa with { Mana = 5f }), (BorrowedSpell.PowerAttack, pa with { First = new[] { 1, 2, 0, 4, 5 } }),
            (BorrowedSpell.PowerAttack, pa with { First = new[] { 1, 2, 3, 4, 21 } }), (BorrowedSpell.PowerAttack, pa with { Second = new[] { 1, 2, 3, 4, 5 } }),
            (BorrowedSpell.PowerAttack, pa with { Price = 31 })
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
        var own = BorrowedSpells.All.Where(s => !BorrowedSpells.Info(s).GameFeat).ToList();
        Assert.That(own.Any(s => BorrowedSpellTexts.HasNames(text, s) || BorrowedSpellTexts.HasAnyName(text, s)), Is.False);
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.PowerAttack), Is.Empty, "Power Attack's texts are the game's own");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.PowerAttack), Is.EqualTo("powerattack_name"));
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
                "burninghands_name", "odd", "powerattack_name", "xantamfire_name", "xantamfire_desc", "bladebarrier_name", "bladebarrier_desc",
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
            [BorrowedSpell.AntimagicField] = "c6d39e2cd0b3573358519a201bd9a6d9", [BorrowedSpell.RighteousMight] = "bd6f5fbeff4c9015adc061080c9912a1"
        };
        var own = BorrowedSpells.All.Where(BorrowedSpellIcons.HasOwnIcon).ToList();
        Assert.That(own, Is.EqualTo(md5.Keys.OrderBy(s => s)), "every spell but Power Attack (the game's icon_powerattack.tex)");
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.PowerAttack), Is.EqualTo("icon_powerattack.tex"));
        foreach (var spell in own)
        {
            Assert.That(BorrowedSpellIcons.Texture(spell).Length, Is.EqualTo(3568));
            Assert.That(Md5(BorrowedSpellIcons.Texture(spell)), Is.EqualTo(md5[spell]), "make_xantam_icon.py's and make_kromlech_icons.py's textures");
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
            "exclaim.tex", "icon_antimagicfield.tex", "icon_balllightning.tex", "icon_bladebarrier.tex", "icon_healthpotion.tex", "icon_powerattack.tex",
            "icon_protection.tex", "icon_quake.tex", "icon_righteousmight.tex", "icon_whirlwind.tex", "icon_xantamfire.tex", "levelup.tex", "nomana.tex"
        }), "each where its name sorts");
        Assert.That(GameArchive.AsLoaded(added).Where(f => !f.Name.StartsWith("icon_") || f.Name is "icon_healthpotion.tex" or "icon_protection.tex" or
                                                        "icon_whirlwind.tex" or "icon_balllightning.tex" or "icon_quake.tex" or "icon_powerattack.tex").Select(f => f.Data),
                    Is.EqualTo(GameArchive.AsLoaded(icons).Select(f => f.Data)), "every other file as it was (Power Attack's the game's own)");
        Assert.That(BorrowedSpellIcons.IsIn(icons, BorrowedSpell.PowerAttack), Is.True);
        Assert.That(BorrowedSpellIcons.AddTo(icons, new[] { BorrowedSpell.PowerAttack }), Is.EqualTo(icons), "Power Attack brings no icon");
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
