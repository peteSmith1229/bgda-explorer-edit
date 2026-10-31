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
/// Corrosive and Corrosive Burst (BgdaExecutable.Corrosive.cs): the Corrosive patch on the stock PAL executable in
/// Test_files (skipped when it is not present), the acid icon in INVENTRY.LMP (archives built here, and
/// Test_files/INVENTRY.LMP when it is there), the names in GTEXT.LMP and the acid glow. The patch is venompatch.py
/// version 3: the checksums here are of the files venompatch.py, inventry_acid.py and make_tinyacid.py write, and what
/// the patch does in the game was checked by emulation (test_venom.py, 46 checks; 39 broken copies all caught) and in
/// the game.
/// </summary>
[TestFixture]
public class BgdaCorrosiveTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>venompatch.py (version 3) with its default settings on the stock executable.</summary>
    private const string PatchedMd5 = "c92917b37f5a852e340da4b2181fd643";

    /// <summary>The game's INVENTRY.LMP, and with tinyacid.tex added by inventry_acid.py.</summary>
    private const string InventryMd5 = "a15bded3799b3157d1aab04a5a30e786", InventryWithIconMd5 = "4064969176e75c2a234c8e3414749e7b";

    /// <summary>The 16 places the patch changes in the game's code: their addresses and how many words (venompatch.py's HOOKS).</summary>
    private static readonly (string Name, int Va, int Words)[] Hooks =
    {
        ("arrow", 0x00110AA4, 1), ("arrow2", 0x001A9930, 1), ("arrow3", 0x001A9958, 1), ("bow", 0x0015EE50, 2),
        ("melee", 0x00167B04, 1), ("burst", 0x001203B4, 1), ("hit", 0x00120588, 1), ("name", 0x00173B9C, 2),
        ("value1", 0x00173D0C, 6), ("value2", 0x00173E14, 5), ("power", 0x00174098, 2), ("parse", 0x00174F80, 1),
        ("card", 0x001770F0, 5), ("stats", 0x00178910, 4), ("icon_load", 0x0016DF80, 1), ("icon_draw", 0x00177344, 1)
    };

    private string _stockPath = "";
    private byte[] _stockBytes = Array.Empty<byte>();
    private string _testFiles = "";
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

        _testFiles = Path.Combine(dir!.FullName, "Test_files");
        _stockPath = Path.Combine(_testFiles, "SLES_506.72");
        _stockBytes = File.ReadAllBytes(_stockPath);
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaCorrosiveTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    private BgdaExecutable Stock() => BgdaExecutable.Open(_stockPath);

    private (BgdaExecutable Exe, byte[] Bytes) SaveAndReopen(BgdaExecutable exe, string name)
    {
        var path = Path.Combine(_workDir, name);
        exe.Save(path);
        return (BgdaExecutable.Open(path), File.ReadAllBytes(path));
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

    private byte[] Patched()
    {
        var exe = Stock();
        exe.SetCorrosive(CorrosiveSettings.Default);
        return SaveAndReopen(exe, "patched").Bytes;
    }

    // ═══════════════════════════════ the patch ═══════════════════════════════

    [Test]
    public void TheStockExecutable_TakesThePatch_AsVenompatchWritesIt()
    {
        RequireStock();
        var exe = Stock();
        Assert.That(exe.GetCorrosiveState(out var detail), Is.EqualTo(CorrosiveState.Off), detail);
        Assert.That(exe.CorrosiveProblem(), Is.Null);

        exe.SetCorrosive(CorrosiveSettings.Default);
        var (on, bytes) = SaveAndReopen(exe, "on");
        Assert.That(Md5(bytes), Is.EqualTo(PatchedMd5), "the same bytes as venompatch.py");
        Assert.That(on.GetCorrosiveState(out detail), Is.EqualTo(CorrosiveState.On), detail);
        Assert.That(on.HasCorrosive, Is.True);
        Assert.That(on.GetCorrosiveSettings(), Is.EqualTo(CorrosiveSettings.Default));
        Assert.That(on.ItemsProblem(), Is.Null, "the item tables can still be edited");
        Assert.That(on.ShopsProblem(), Is.Null);
        Assert.That(on.GlowStylesProblem(), Is.Null);
        Assert.That(bytes.Length, Is.EqualTo(_stockBytes.Length), "the file doesn't grow");
    }

    [Test]
    public void ThePatch_ChangesOnlyTheElfHeadersPaddingAndTheHooks()
    {
        var bytes = Patched();
        var changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).ToList();
        bool Allowed(int i) =>
            i is 0x2C or >= 0x54 and < 0x1000 || Hooks.Any(h => i >= h.Va - VaToFile && i < h.Va - VaToFile + 4 * h.Words);
        Assert.That(changed.Where(i => !Allowed(i)), Is.Empty);
        Assert.That(BitConverter.ToUInt16(bytes, 0x2C), Is.EqualTo(2), "two program headers");
        Assert.That(Enumerable.Range(0, 8).Select(i => BitConverter.ToUInt32(bytes, 0x54 + 4 * i)),
                    Is.EqualTo(new uint[] { 1, 0x100, 0x000FF100, 0x000FF100, 0xF00, 0xF00, 7, 0x1000 }), "PT_LOAD file 0x100 -> 0x000FF100");
        Assert.That(Encoding.ASCII.GetString(bytes, 0x100, 4), Is.EqualTo("VENM"));
        Assert.That(BitConverter.ToInt32(bytes, 0x104), Is.EqualTo(3), "version 3");
        Assert.That(BitConverter.ToUInt64(bytes, 0x108), Is.EqualTo(1UL << 59));
        Assert.That(BitConverter.ToUInt64(bytes, 0x110), Is.EqualTo(1UL << 60));
        Assert.That(Encoding.ASCII.GetString(bytes, 0x140, 4), Is.EqualTo("acid"));
        Assert.That(Encoding.ASCII.GetString(bytes, 0x1D0, 12), Is.EqualTo(CorrosiveIcon.EntryName));
        foreach (var (name, va, _) in Hooks)
        {
            var word = BitConverter.ToUInt32(bytes, va - VaToFile);
            Assert.That(word >> 26 is 2 or 3, Is.True, $"{name}: a jump");
            Assert.That((word & 0x03FFFFFF) << 2 is >= 0x000FF1E0 and < 0x00100000, Is.True, $"{name}: into the patch's code");
        }
    }

    [Test]
    public void RemovingThePatch_GivesTheFileBack()
    {
        var on = OpenBytes(Patched(), "to-remove");
        on.RemoveCorrosive();
        var (off, bytes) = SaveAndReopen(on, "removed");
        Assert.That(bytes, Is.EqualTo(_stockBytes));
        Assert.That(off.GetCorrosiveState(), Is.EqualTo(CorrosiveState.Off));
        off.RemoveCorrosive(); // nothing to take out: nothing happens
    }

    [Test]
    public void Settings_AreWhereTheCodeReadsThem_AndOnlyTheyChange()
    {
        RequireStock();
        var patched = Patched();
        var exe = OpenBytes(patched, "settings");
        var custom = new CorrosiveSettings(8, 12, 7.5f, 2f);
        exe.SetCorrosive(custom);
        var (reopened, bytes) = SaveAndReopen(exe, "settings2");
        Assert.That(BitConverter.ToInt32(bytes, 0x118), Is.EqualTo(8));
        Assert.That(BitConverter.ToInt32(bytes, 0x11C), Is.EqualTo(12));
        Assert.That(BitConverter.ToSingle(bytes, 0x120), Is.EqualTo(7.5f));
        Assert.That(BitConverter.ToSingle(bytes, 0x124), Is.EqualTo(2f));
        Assert.That(reopened.GetCorrosiveSettings(), Is.EqualTo(custom));
        Assert.That(Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != patched[i]).All(i => i is >= 0x118 and < 0x128), Is.True,
                    "only the settings changed");

        reopened.SetCorrosive(CorrosiveSettings.Default);
        Assert.That(Md5(SaveAndReopen(reopened, "settings3").Bytes), Is.EqualTo(PatchedMd5));
    }

    [Test]
    public void BadSettings_AreRefused()
    {
        var bad = new[]
        {
            new CorrosiveSettings(0, 10, 5f, 1f), new CorrosiveSettings(6, 0, 5f, 1f), new CorrosiveSettings(1001, 10, 5f, 1f),
            new CorrosiveSettings(6, 10, -1f, 0f), new CorrosiveSettings(6, 10, 61f, 1f), new CorrosiveSettings(6, 10, float.NaN, 1f),
            new CorrosiveSettings(6, 10, 5f, 6f), new CorrosiveSettings(6, 10, 5f, -0.5f), new CorrosiveSettings(6, 10, 5f, float.PositiveInfinity)
        };
        foreach (var settings in bad)
        {
            Assert.That(settings.Problem(), Is.Not.Null, settings.ToString());
            var exe = Stock();
            Assert.Throws<ArgumentException>(() => exe.SetCorrosive(settings));
            Assert.That(exe.GetCorrosiveState(), Is.EqualTo(CorrosiveState.Off), "nothing changed");
        }
    }

    [Test]
    public void TheDefaults_AreWhatWasTriedInTheGame()
    {
        Assert.That(CorrosiveSettings.Default, Is.EqualTo(new CorrosiveSettings(6, 10, 5f, 1f)));
        Assert.That(CorrosiveSettings.Default.Problem(), Is.Null);
        Assert.That(new CorrosiveSettings(1, 1, 0f, 0f).Problem(), Is.Null);
        Assert.That(new CorrosiveSettings(1000, 1000, 60f, 60f).Problem(), Is.Null);
    }

    [Test]
    public void WhatIsntTheGamesCode_IsLeftAlone()
    {
        void Expect(byte[] bytes, string name, string because)
        {
            var exe = OpenBytes(bytes, name);
            Assert.That(exe.GetCorrosiveState(out var detail), Is.EqualTo(CorrosiveState.Unknown), because);
            Assert.That(exe.CorrosiveProblem(), Is.EqualTo(detail));
            Assert.Throws<InvalidOperationException>(() => exe.SetCorrosive(CorrosiveSettings.Default));
            Assert.Throws<InvalidOperationException>(() => exe.RemoveCorrosive());
        }

        var padding = (byte[])_stockBytes.Clone();
        padding[0x800] = 1;
        Expect(padding, "padding", "something else uses the ELF header's padding");

        var hook = (byte[])_stockBytes.Clone();
        hook[0x00173B9C - VaToFile] ^= 1;
        Expect(hook, "hook", "a hook isn't the game's code");

        var check = (byte[])_stockBytes.Clone();
        check[0x0011F844 - VaToFile] ^= 1;
        Expect(check, "check", "an instruction the code relies on isn't the game's");

        var text = (byte[])_stockBytes.Clone();
        text[0x00311188 - VaToFile] = (byte)'x';
        Expect(text, "text", "the acid arrow's effect isn't where the code looks");

        var code = Patched();
        code[0x1E0 + 40] ^= 4;
        Expect(code, "code", "the patch's code has been changed");

        var bonus = Patched();
        bonus[0x13C] = 1;
        Expect(bonus, "bonus", "Corrosive Burst's power isn't the one the formulas use");

        var hookAfter = Patched();
        hookAfter[0x00174098 - VaToFile + 4] ^= 1;
        Expect(hookAfter, "hook-after", "a hook has been changed since the patch went in");

        var version = Patched();
        version[0x104] = 4;
        Expect(version, "version4", "a version WorldExplorer doesn't know");
    }

    [Test]
    public void AnEarlierVersion_IsRecognised_AndReplaced()
    {
        RequireStock();
        // version 2 had the same data up to 0xBF and no icon: version 3 with its number and without the icon hooks
        var v2 = Patched();
        BitConverter.GetBytes(2).CopyTo(v2, 0x104);
        BitConverter.GetBytes(0xDFBF0140u).CopyTo(v2, 0x0016DF80 - VaToFile);
        BitConverter.GetBytes(0x3C03002Au).CopyTo(v2, 0x00177344 - VaToFile);
        BitConverter.GetBytes(7).CopyTo(v2, 0x118);

        var exe = OpenBytes(v2, "v2");
        Assert.That(exe.GetCorrosiveState(out var detail), Is.EqualTo(CorrosiveState.Outdated), detail);
        Assert.That(detail, Does.Contain("Version 2"));
        Assert.That(exe.CorrosiveProblem(), Is.Null);
        Assert.That(exe.GetCorrosiveSettings(), Is.EqualTo(CorrosiveSettings.Default with { Die = 7 }), "version 2's settings are kept");

        exe.SetCorrosive(exe.GetCorrosiveSettings() with { Die = 6 });
        Assert.That(Md5(SaveAndReopen(exe, "v2-upgraded").Bytes), Is.EqualTo(PatchedMd5));

        var removed = OpenBytes(v2, "v2-removed");
        removed.RemoveCorrosive();
        Assert.That(SaveAndReopen(removed, "v2-removed2").Bytes, Is.EqualTo(_stockBytes));
    }

    // ═══════════════════════════════ items, shops and loot ═══════════════════════════════

    [Test]
    public void TheBits_AreModifiers_WithTheirWords()
    {
        var c = ItemBits.Get(ItemBits.Corrosive);
        var b = ItemBits.Get(ItemBits.CorrosiveBurst);
        Assert.That((c.Kind, c.Name, c.Keyword, c.TextKey), Is.EqualTo((ItemBitKind.Modifier, "Corrosive", "acid", "it_corrosive")));
        Assert.That((b.Kind, b.Name, b.Keyword, b.TextKey), Is.EqualTo((ItemBitKind.Modifier, "Corrosive Burst", "acidburst", "it_corrosiveburst")));
        Assert.That(Corrosive.Bits, Is.EqualTo(1UL << 59 | 1UL << 60));
        Assert.That(ItemBits.Keywords["acid"], Is.EqualTo(59));
        Assert.That(ItemBits.Keywords["acidburst"], Is.EqualTo(60));
    }

    [Test]
    public void ItemText_ReadsAcidAndAcidburst()
    {
        var items = Stock().GetItems();
        var parsed = ItemText.Parse("longsword+2+acidburst", items);
        Assert.That(parsed.Problems, Is.Empty);
        Assert.That(ItemBits.Has(parsed.Flags, ItemBits.CorrosiveBurst), Is.True);
        Assert.That(ItemBits.Enchantment(parsed.Flags), Is.EqualTo(2));
        parsed = ItemText.Parse("dagger+acid+fire", items);
        Assert.That(parsed.Problems, Is.Empty);
        Assert.That(Corrosive.Uses(parsed.Flags) && ItemBits.Has(parsed.Flags, ItemBits.Corrosive) && ItemBits.Has(parsed.Flags, ItemBits.Fire), Is.True);
    }

    [Test]
    public void ValueAndPower_AreFlamingsAndFlamingBursts()
    {
        var items = Stock().GetItems();
        var bases = new ulong[] { 0, 1, 2, 5, 1UL << ItemBits.Keen, 1UL << ItemBits.Masterwork | 3, 1UL << ItemBits.Mithril, 1UL << ItemBits.Ice | 1 };
        foreach (var item in items)
        {
            foreach (var f in bases)
            {
                Assert.That(ItemFormulas.Value(item, 1, f | 1UL << ItemBits.Corrosive), Is.EqualTo(ItemFormulas.Value(item, 1, f | 1UL << ItemBits.Fire)), item.Name);
                Assert.That(ItemFormulas.Value(item, 1, f | 1UL << ItemBits.CorrosiveBurst), Is.EqualTo(ItemFormulas.Value(item, 1, f | 1UL << ItemBits.FireBurst)), item.Name);
            }
        }

        foreach (var f in bases)
        {
            Assert.That(ItemFormulas.Power(f | 1UL << ItemBits.Corrosive), Is.EqualTo(ItemFormulas.Power(f | 1UL << ItemBits.Fire)));
            Assert.That(ItemFormulas.Power(f | 1UL << ItemBits.CorrosiveBurst), Is.EqualTo(ItemFormulas.Power(f | 1UL << ItemBits.FireBurst)));
        }
    }

    [Test]
    public void AnItemWithTheModifier_NeedsThePatch()
    {
        var exe = Stock();
        var sword = exe.GetItems().First(i => i.Name == "longsword");
        sword.Flags |= 1UL << ItemBits.CorrosiveBurst;
        var edits = new ItemEdits();
        edits.Items.Add(sword);
        var e = Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(edits));
        Assert.That(e!.Message, Does.Contain("longsword"));
        Assert.That(e.Message, Does.Contain("Corrosive patch"));
        Assert.That(SaveAndReopen(exe, "refused").Bytes, Is.EqualTo(_stockBytes), "nothing changed");

        edits.Corrosive = CorrosiveSettings.Default;
        exe.ApplyItemEdits(edits);
        Assert.That(exe.GetCorrosiveState(), Is.EqualTo(CorrosiveState.On));
        Assert.That(exe.CorrosiveUses()[0], Is.EqualTo($"item {sword.Index} (longsword)"));
        Assert.That(exe.CorrosiveUses().Skip(1).All(u => u.Contains("(longsword")), Is.True, "and the shops selling longswords");

        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(new ItemEdits { RemoveCorrosive = true })); // the longsword still uses it
        Assert.That(exe.GetCorrosiveState(), Is.EqualTo(CorrosiveState.On));
        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(new ItemEdits { RemoveCorrosive = true, Corrosive = CorrosiveSettings.Default }));

        sword.Flags &= ~Corrosive.Bits;
        var off = new ItemEdits { RemoveCorrosive = true };
        off.Items.Add(sword);
        exe.ApplyItemEdits(off);
        Assert.That(SaveAndReopen(exe, "both-out").Bytes, Is.EqualTo(_stockBytes), "the modifier off and the patch out in one go");
    }

    [Test]
    public void ShopsAndLootLists_CanUseTheBits()
    {
        var exe = Stock();
        var entry = exe.GetShopEntries().First(s => s.List == ItemCategory.Weapon && s.AnyText);
        var shop = new ItemEdits();
        shop.ShopTexts[entry.Number] = "longsword+2+acidburst";
        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(shop));

        var loot = exe.GetLootModifiers(LootModifierList.WeaponsExtreme);
        loot[0] = 1UL << ItemBits.Corrosive;
        var lootEdits = new ItemEdits();
        lootEdits.Loot[LootModifierList.WeaponsExtreme] = loot;
        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(lootEdits));

        lootEdits.ShopTexts[entry.Number] = "longsword+2+acidburst";
        lootEdits.Corrosive = CorrosiveSettings.Default;
        exe.ApplyItemEdits(lootEdits);
        var (reopened, _) = SaveAndReopen(exe, "shop-loot");
        Assert.That(reopened.HasCorrosive, Is.True);
        Assert.That(reopened.GetLootModifiers(LootModifierList.WeaponsExtreme)[0], Is.EqualTo(1UL << ItemBits.Corrosive));
        Assert.That(reopened.CorrosiveUses(), Has.Count.EqualTo(2));
        Assert.That(reopened.CorrosiveUses()[1], Does.Contain("WeaponsExtreme"));
    }

    // ═══════════════════════════════ with the other patches ═══════════════════════════════

    [Test]
    public void GlowStyles_AndThePatch_GoInAndOutInEitherOrder()
    {
        var settings = Corrosive.WithAcidGlow(GlowSettings.Plain(Stock().GetEnchantGlow()), out _);
        var first = Stock();
        first.SetCorrosive(CorrosiveSettings.Default);
        first.SetGlowSettings(settings);
        var second = Stock();
        second.SetGlowSettings(settings);
        second.SetCorrosive(CorrosiveSettings.Default);
        var (a, aBytes) = SaveAndReopen(first, "corrosive-glow");
        var (b, bBytes) = SaveAndReopen(second, "glow-corrosive");
        Assert.That(aBytes, Is.EqualTo(bBytes));
        Assert.That((a.GetCorrosiveState(), a.GetGlowStylesState()), Is.EqualTo((CorrosiveState.On, GlowStylesState.On)));

        a.RemoveGlowStyles();
        Assert.That(a.GetCorrosiveState(), Is.EqualTo(CorrosiveState.On));
        a.RemoveCorrosive();
        Assert.That(SaveAndReopen(a, "both-out").Bytes, Is.EqualTo(_stockBytes));

        b.RemoveCorrosive();
        Assert.That(b.GetGlowStylesState(), Is.EqualTo(GlowStylesState.On));
        Assert.That(b.GetGlowSettings(), Is.EqualTo(settings));
    }

    [Test]
    public void ThreePlayers_AndThePatch_GoInAndOutInEitherOrder()
    {
        var first = Stock();
        first.SetCorrosive(CorrosiveSettings.Default);
        Assert.That(first.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off));
        first.EnableThreePlayers();
        var second = Stock();
        second.EnableThreePlayers();
        var (threeOnly, threeBytes) = SaveAndReopen(second, "three");
        second.SetCorrosive(CorrosiveSettings.Default);
        var (a, aBytes) = SaveAndReopen(first, "corrosive-three");
        Assert.That(SaveAndReopen(second, "three-corrosive").Bytes, Is.EqualTo(aBytes));
        Assert.That((a.GetCorrosiveState(), a.GetThreePlayerState()), Is.EqualTo((CorrosiveState.On, ThreePlayerState.On)));
        Assert.That(threeOnly.GetCorrosiveState(), Is.EqualTo(CorrosiveState.Off));

        a.RemoveCorrosive();
        Assert.That(SaveAndReopen(a, "three-again").Bytes, Is.EqualTo(threeBytes));
        second.DisableThreePlayers();
        Assert.That(second.GetCorrosiveState(), Is.EqualTo(CorrosiveState.On));
        Assert.That(Md5(SaveAndReopen(second, "corrosive-again").Bytes), Is.EqualTo(Md5(Patched())));
    }

    // ═══════════════════════════════ the acid glow ═══════════════════════════════

    [Test]
    public void TheAcidGlow_GoesFirst_InTheFirstFreeStyle()
    {
        var plain = GlowSettings.Plain(new EnchantGlow(new Rgb(10, 10, 68), new Rgb(60, 60, 68)));
        var withAcid = Corrosive.WithAcidGlow(plain, out var style);
        Assert.That(style, Is.EqualTo(1));
        Assert.That(withAcid.Styles[1], Is.EqualTo(Corrosive.AcidGlow));
        Assert.That(withAcid.Rules, Is.EqualTo(new[] { new GlowRule(Corrosive.Bits, 1) }));
        Assert.That(withAcid.Problem(), Is.Null);
        Assert.That(Corrosive.AcidGlow, Is.EqualTo(new GlowStyle(new GlowLayer(new Rgb(60, 110, 0), 64), new GlowLayer(new Rgb(190, 255, 40), 72))));

        // styles 1 and 2 in use: the acid glow takes 3 and its rule goes before theirs
        var styles = plain.Styles.ToArray();
        styles[1] = Corrosive.AcidGlow with { Core = new GlowLayer(new Rgb(255, 0, 0), 72) };
        styles[2] = Corrosive.AcidGlow with { Core = new GlowLayer(new Rgb(0, 0, 255), 72) };
        var rules = new[] { new GlowRule(1UL << ItemBits.Fire, 1), new GlowRule(1UL << ItemBits.Ice, 2), new GlowRule(1UL << ItemBits.Keen, null) };
        var busy = Corrosive.WithAcidGlow(new GlowSettings(styles, rules), out style);
        Assert.That(style, Is.EqualTo(3));
        Assert.That(busy.Rules, Is.EqualTo(rules.Prepend(new GlowRule(Corrosive.Bits, 3))));
        Assert.That(busy.Styles.Where((_, i) => i != 3), Is.EqualTo(styles.Where((_, i) => i != 3)));

        Assert.That(Corrosive.WithAcidGlow(plain, out style, 9).Styles[9], Is.EqualTo(Corrosive.AcidGlow));
        Assert.That(style, Is.EqualTo(9));

        Assert.Throws<InvalidOperationException>(() => Corrosive.WithAcidGlow(withAcid, out _)); // a rule already looks for it
        var full = new GlowSettings(plain.Styles, Enumerable.Range(0, 16).Select(i => new GlowRule(1UL << (i + 4), null)));
        Assert.Throws<InvalidOperationException>(() => Corrosive.WithAcidGlow(full, out _)); // all 16 rules in use
        var allUsed = new GlowSettings(plain.Styles.Select((s, i) => i == 15 ? Corrosive.AcidGlow with { Halo = new GlowLayer(new Rgb(1, 2, 3), 64) } : s),
                                       Array.Empty<GlowRule>());
        Assert.Throws<InvalidOperationException>(() => Corrosive.WithAcidGlow(allUsed, out _)); // no style left in style 0's colours
    }

    // ═══════════════════════════════ the names ═══════════════════════════════

    private static byte[] SmallGameText()
    {
        var texts = Enumerable.Range(0, 5).Select(i => new List<(string, string)> { ("it_flaming", $"Flaming {i}"), ("odd", "x") }).ToArray();
        return LmpWriter.PackEntries(EngineVersion.DarkAlliance,
            GameText.Files.Select((f, i) => (f.File, new UniTextFile(texts[i]).ToArray())).ToList());
    }

    [Test]
    public void TheNames_GoInEveryLanguage_AndComeOutAgain()
    {
        var original = SmallGameText();
        var text = GameText.Read(original);
        Assert.That(Corrosive.HasNames(text), Is.False);
        Corrosive.AddNames(text);
        Assert.That(Corrosive.HasNames(text), Is.True);

        var saved = GameText.Read(text.ToArray());
        Assert.That(Corrosive.HasNames(saved), Is.True);
        Assert.That(saved.Get("it_corrosive"), Is.EqualTo("Corrosive"));
        Assert.That(saved.Get("it_corrosiveburst"), Is.EqualTo("Corrosive Burst"));
        Assert.That(saved.Get("it_corrosive_F", 1), Is.EqualTo("corrosive"));
        Assert.That(saved.Get("it_corrosiveburst", 2), Is.EqualTo("des Säureinfernos"));
        Assert.That(saved.Get("it_corrosiveburst_f", 3), Is.EqualTo("Esplosiva Corrosiva"));
        Assert.That(saved.Get("it_corrosive_m", 4), Is.EqualTo("corrosivo"));
        for (var language = 0; language < 5; language++)
        {
            var keys = saved.Entries(language).Select(e => e.Key).ToList();
            Assert.That(keys.Take(2), Is.EqualTo(new[] { "it_flaming", "odd" }), "the names go after the file's own entries");
            Assert.That(keys.Skip(2), Is.EqualTo(Corrosive.Names.Where(n => n.Language == language).Select(n => n.Key)));
        }

        Assert.That(Corrosive.Names.All(n => n.Text.All(c => GameText.TextCharacters.Contains(c))), Is.True, "letters the game's font draws");
        Assert.That(Corrosive.Names.Count, Is.EqualTo(20));

        Corrosive.AddNames(saved);
        Assert.That(saved.Changed, Is.False, "adding them again changes nothing");
        Corrosive.RemoveNames(saved);
        Assert.That(saved.ToArray(), Is.EqualTo(original));
    }

    // ═══════════════════════════════ the icon and INVENTRY.LMP ═══════════════════════════════

    [Test]
    public void TheIcon_IsMakeTinyacidsTexture()
    {
        var tex = CorrosiveIcon.Texture();
        Assert.That(tex.Length, Is.EqualTo(1760));
        Assert.That(Md5(tex), Is.EqualTo("0a6078e0355e1ee92d2a119375d02f22"));
    }

    /// <summary>An archive: the entries in this order, each file at the next multiple of 0x80, room for <paramref name="spare"/> more entries.</summary>
    private static byte[] Archive(IReadOnlyList<(string Name, byte[]? Data)> entries, int spare = 2)
    {
        var directoryEnd = 4 + 64 * (entries.Count + spare);
        var at = (directoryEnd + 0x7F) & ~0x7F;
        var offsets = new List<(int Offset, int Length)>();
        var files = new List<byte>();
        foreach (var (_, data) in entries)
        {
            if (data == null)
            {
                offsets.Add(offsets[^1]); // an alias of the entry before
                continue;
            }

            while ((at + files.Count) % 0x80 != 0) files.Add(0);
            offsets.Add((at + files.Count, data.Length));
            files.AddRange(data);
        }

        var output = new byte[at + files.Count];
        BitConverter.GetBytes(entries.Count).CopyTo(output, 0);
        for (var i = 0; i < entries.Count; i++)
        {
            Encoding.ASCII.GetBytes(entries[i].Name).CopyTo(output, 4 + 64 * i);
            BitConverter.GetBytes(offsets[i].Offset).CopyTo(output, 4 + 64 * i + 56);
            BitConverter.GetBytes(offsets[i].Length).CopyTo(output, 4 + 64 * i + 60);
        }

        files.ToArray().CopyTo(output, at);
        return output;
    }

    private static byte[] Bytes(int n, byte fill) => Enumerable.Repeat(fill, n).ToArray();

    [Test]
    public void AnAddedEntry_GoesWhereTheGamesSearchLooks()
    {
        var archive = Archive(new (string, byte[]?)[]
        {
            ("ntsc_arrow.tex", Bytes(200, 1)), ("pal_arrow.tex", Bytes(300, 2)), ("tinyab.tex", Bytes(100, 3)), ("tinyabalias.tex", null),
            ("tinycritical.tex", Bytes(129, 4)), ("tinyfire.tex", Bytes(10, 5))
        });
        Assert.That(GameArchive.Problem(archive), Is.Null);
        Assert.That(CorrosiveIcon.Problem(archive), Is.Null);
        var icon = CorrosiveIcon.Texture();
        var added = CorrosiveIcon.AddTo(archive);

        var entries = GameArchive.ReadDirectory(added);
        Assert.That(entries.Select(e => e.Name), Is.EqualTo(new[]
        {
            "ntsc_arrow.tex", "pal_arrow.tex", "tinyab.tex", "tinyabalias.tex", "tinyacid.tex", "tinycritical.tex", "tinyfire.tex"
        }));
        var acid = entries.Single(e => e.Name == "tinyacid.tex");
        var critical = entries.Single(e => e.Name == "tinycritical.tex");
        Assert.That(acid.Offset % 0x80, Is.EqualTo(0));
        Assert.That(acid.Offset, Is.LessThan(critical.Offset), "its file goes before the next entry's file");
        Assert.That(added.AsSpan(acid.Offset, acid.Length).ToArray(), Is.EqualTo(icon));
        Assert.That(GameArchive.Problem(added), Is.Null);
        Assert.That(GameArchive.Contains(added, "tinyacid.tex"), Is.True);
        Assert.That(CorrosiveIcon.IsIn(added), Is.True);

        // every other entry loads as before, in both regions
        foreach (var pal in new[] { true, false })
        {
            var before = GameArchive.AsLoaded(archive, pal);
            var after = GameArchive.AsLoaded(added, pal).Where(f => f.Name != "tinyacid.tex").ToList();
            Assert.That(after.Select(f => f.Name), Is.EqualTo(before.Select(f => f.Name)));
            Assert.That(after.Select(f => f.Data), Is.EqualTo(before.Select(f => f.Data)));
        }

        Assert.That(CorrosiveIcon.AddTo(added), Is.EqualTo(added), "it is there already");
        Assert.That(CorrosiveIcon.RemoveFrom(added), Is.EqualTo(archive), "taken out: the archive as it was");
        Assert.That(CorrosiveIcon.RemoveFrom(archive), Is.EqualTo(archive));
    }

    [Test]
    public void AnEntryNamedLast_GoesAtTheEnd()
    {
        var archive = Archive(new (string, byte[]?)[] { ("a.tex", Bytes(5, 1)), ("b.tex", Bytes(0x80, 2)) });
        var added = GameArchive.Insert(archive, "c.tex", Bytes(7, 3));
        var c = GameArchive.ReadDirectory(added).Last();
        Assert.That((c.Name, c.Offset, c.Length), Is.EqualTo(("c.tex", archive.Length, 7)));
        Assert.That(GameArchive.Remove(added, "c.tex"), Is.EqualTo(archive));
    }

    [Test]
    public void ArchivesThatCantTakeAnEntry_AreRefused()
    {
        var full = Archive(new (string, byte[]?)[] { ("tinyab.tex", Bytes(5, 1)), ("tinycritical.tex", Bytes(5, 2)) }, spare: 0);
        if (64 * 3 + 4 <= GameArchive.ReadDirectory(full).Min(e => e.Offset))
        {
            // the directory's rounding left room for one more: use it up
            full = Archive(new (string, byte[]?)[] { ("tinyab.tex", Bytes(5, 1)), ("tinyb.tex", Bytes(5, 1)), ("tinycritical.tex", Bytes(5, 2)) }, spare: 0);
        }

        Assert.Throws<InvalidDataException>(() => CorrosiveIcon.AddTo(full)); // no room in the directory

        var unsorted = Archive(new (string, byte[]?)[] { ("tinycritical.tex", Bytes(5, 2)), ("tinyab.tex", Bytes(5, 1)) });
        Assert.That(GameArchive.Problem(unsorted), Does.Contain("order"));
        Assert.Throws<InvalidDataException>(() => CorrosiveIcon.AddTo(unsorted));

        var notInventry = Archive(new (string, byte[]?)[] { ("a.tex", Bytes(5, 1)) });
        Assert.That(CorrosiveIcon.Problem(notInventry), Does.Contain("INVENTRY.LMP"));
        Assert.Throws<InvalidDataException>(() => CorrosiveIcon.AddTo(notInventry));
        Assert.That(CorrosiveIcon.Problem(new byte[] { 1, 2 }), Is.Not.Null);

        var twice = Archive(new (string, byte[]?)[] { ("a.tex", Bytes(5, 1)) });
        Assert.Throws<InvalidDataException>(() => GameArchive.Insert(twice, "a.tex", Bytes(1, 1)));
        Assert.Throws<ArgumentException>(() => GameArchive.Insert(twice, "with space.tex", Bytes(1, 1)));
    }

    [Test]
    public void TheGamesSearch_FindsEveryName()
    {
        var names = new[] { "a", "b", "c", "d", "e", "f", "g" };
        for (var n = 0; n <= names.Length; n++)
        {
            var list = names.Take(n).ToList();
            for (var i = 0; i < n; i++) Assert.That(GameArchive.Find(list, list[i]), Is.EqualTo(i));
            Assert.That(GameArchive.Find(list, "0"), Is.EqualTo(-1));
            Assert.That(GameArchive.Find(list, "bb"), Is.EqualTo(-1));
            Assert.That(GameArchive.Find(list, "z"), Is.EqualTo(-1));
        }
    }

    [Test]
    public void TheGamesInventry_TakesTheIcon_AsInventryAcidAddsIt()
    {
        var path = Path.Combine(_testFiles, "INVENTRY.LMP");
        if (!File.Exists(path)) Assert.Ignore("Test_files/INVENTRY.LMP (from the game's disc) was not found.");
        var inventry = File.ReadAllBytes(path);
        if (Md5(inventry) != InventryMd5) Assert.Ignore("Test_files/INVENTRY.LMP isn't the PAL game's.");

        Assert.That(CorrosiveIcon.Problem(inventry), Is.Null);
        Assert.That(CorrosiveIcon.IsIn(inventry), Is.False);
        var added = CorrosiveIcon.AddTo(inventry);
        Assert.That(Md5(added), Is.EqualTo(InventryWithIconMd5), "the same bytes as inventry_acid.py");
        Assert.That(GameArchive.Contains(added, "tinyacid.tex"), Is.True);
        var before = GameArchive.AsLoaded(inventry);
        var after = GameArchive.AsLoaded(added).Where(f => f.Name != "tinyacid.tex").ToList();
        Assert.That(after.Select(f => f.Name), Is.EqualTo(before.Select(f => f.Name)), "every other texture is still found");
        Assert.That(after.Select(f => f.Data), Is.EqualTo(before.Select(f => f.Data)), "and loads the same bytes");
        Assert.That(CorrosiveIcon.RemoveFrom(added), Is.EqualTo(inventry));
    }

    // ═══════════════════════════════ archive order ═══════════════════════════════

    [Test]
    public void ArchiveOrder_IsTheGames_WithoutTheRegionPrefixes()
    {
        Assert.That(LmpFile.GameSortKey("ntsc_shop.tex"), Is.EqualTo("shop.tex"));
        Assert.That(LmpFile.GameSortKey("pal_shop.tex"), Is.EqualTo("shop.tex"));
        Assert.That(LmpFile.GameSortKey("palace.tex"), Is.EqualTo("palace.tex"));
        Assert.That(LmpFile.InGameOrder(new[] { "tinycritical.tex", "pal_b.tex", "tinyacid.tex", "ntsc_b.tex", "a.tex", "tinyab.tex" }),
                    Is.EqualTo(new[] { "a.tex", "pal_b.tex", "ntsc_b.tex", "tinyab.tex", "tinyacid.tex", "tinycritical.tex" }));
    }
}
