using JetBlackEngineLib.Data.Executable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Checks the item tables, the shop lists and the item formulas against the stock PAL executable in
/// Test_files (skipped when that file is not present). The expected values of the formulas were made by
/// running the game's own functions on this executable in an emulator.
/// </summary>
[TestFixture]
public class BgdaItemTests
{
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
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaItemTests-" + Guid.NewGuid().ToString("N"));
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

    private static int[] Differences(byte[] a, byte[] b) =>
        Enumerable.Range(0, Math.Min(a.Length, b.Length)).Where(i => a[i] != b[i]).ToArray();

    // ── the item table ──

    [Test]
    public void StockItems_AreReadAsTheGameHasThem()
    {
        var exe = Stock();
        Assert.That(exe.ItemsProblem(), Is.Null);
        var items = exe.GetItems();
        Assert.That(items, Has.Count.EqualTo(81));

        var longsword = items[12];
        Assert.That(longsword.Name, Is.EqualTo("longsword"));
        Assert.That(longsword.Weight, Is.EqualTo(4f));
        Assert.That((longsword.DamageMin, longsword.DamageMax), Is.EqualTo((2, 8)));
        Assert.That(longsword.Price, Is.EqualTo(150));
        Assert.That(longsword.Slot, Is.EqualTo(ItemSlot.OneHanded));
        Assert.That(longsword.Category, Is.EqualTo(ItemCategory.Weapon));
        Assert.That(longsword.DamageType, Is.EqualTo(8));
        Assert.That((longsword.Model, longsword.PickupModel), Is.EqualTo((30, 30)));
        Assert.That(ItemBits.Looks(longsword.Flags), Is.EqualTo(5), "long_sword1 to long_sword5");

        var arrows = items[1];
        Assert.That((arrows.StackSize, arrows.Slot, arrows.Stance, arrows.DamageType),
            Is.EqualTo((20, ItemSlot.Ammunition, ItemStance.Ammunition, 4)));

        var plate = items.Single(i => i.Name == "platemail");
        Assert.That((plate.Armour, plate.Price, plate.Category, plate.Slot, plate.Model),
            Is.EqualTo((39, 25000, ItemCategory.Armour, ItemSlot.Body, 19)));
        Assert.That(plate.WornFromArmorYak, Is.True);

        var icing = items[20];
        Assert.That(ItemBits.Has(icing.Flags, ItemBits.UniqueName), Is.True);
        Assert.That(ItemBits.Describe(icing.Flags), Is.EqualTo("Frost, +1"));

        Assert.That(items.Where(i => i.IsSpare).Select(i => i.Name),
            Is.EqualTo(new[] { "burninghands", "snowblind", "circleofprotection", "meteorswarm", "curelightwounds", "fireball", "acidarrow", "magicmissile" }));
        Assert.That(items.Where(i => i.IsSpare).All(i => i.NameCapacity >= 15), Is.True);
        Assert.That(items.Where(i => !i.IsSpare).All(i => i.NameCapacity == 0), Is.True);
        Assert.That(items.All(i => BgdaExecutable.CheckItem(i, i, items) == null), Is.True, "every stock record passes the checks");
    }

    [Test]
    public void WritingTheItemsBack_ChangesNoByte()
    {
        var exe = Stock();
        foreach (var item in exe.GetItems()) exe.SetItem(item);
        var (_, bytes) = SaveAndReopen(exe, "same.elf");
        Assert.That(bytes, Is.EqualTo(_stockBytes));
    }

    [Test]
    public void EditedItem_IsWrittenInItsRecordOnly()
    {
        var exe = Stock();
        var sword = exe.GetItems()[12];
        sword.DamageMin = 20;
        sword.DamageMax = 40;
        sword.Weight = 2.5f;
        sword.DamageType = 0x20;
        sword.Flags = ItemBits.WithEnchantment(sword.Flags, 3) | (1UL << ItemBits.Fire);
        exe.SetItem(sword);
        var (back, bytes) = SaveAndReopen(exe, "sword.elf");

        var read = back.GetItems()[12];
        Assert.That((read.DamageMin, read.DamageMax, read.Weight, read.DamageType), Is.EqualTo((20, 40, 2.5f, 0x20)));
        Assert.That(read.Flags, Is.EqualTo(sword.Flags));
        var record = 0x1A9098 + 12 * 0x30;
        Assert.That(Differences(_stockBytes, bytes).All(o => o >= record && o < record + 0x30), Is.True);
    }

    [Test]
    public void SpareRecord_BecomesANewItem()
    {
        var exe = Stock();
        var items = exe.GetItems();
        var spare = items[55].Clone();
        spare.Name = "flametongue";
        spare.Category = ItemCategory.Weapon;
        spare.Slot = ItemSlot.OneHanded;
        spare.Stance = ItemStance.OneHanded;
        spare.DamageMin = 3;
        spare.DamageMax = 12;
        spare.DamageType = 8;
        spare.Model = 30;
        spare.PickupModel = 30;
        spare.Flags = (1UL << ItemBits.FireBurst) | 2;
        exe.SetItem(spare);
        var (back, _) = SaveAndReopen(exe, "spare.elf");

        var after = back.GetItems();
        Assert.That(after[55].Name, Is.EqualTo("flametongue"));
        Assert.That(after[55].Category, Is.EqualTo(ItemCategory.Weapon));
        Assert.That(after.Where(i => i.Index != 55).Select(i => i.Name), Is.EqualTo(items.Where(i => i.Index != 55).Select(i => i.Name)));
        var parsed = ItemText.Parse("flametongue+masterwork", after);
        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ItemIndex, Is.EqualTo(55));
    }

    [Test]
    public void BadItems_AreRefused()
    {
        var exe = Stock();
        var items = exe.GetItems();
        string Problem(int index, Action<GameItem> edit)
        {
            var copy = items[index].Clone();
            edit(copy);
            return BgdaExecutable.CheckItem(copy, items[index], items) ?? "";
        }

        Assert.That(Problem(12, i => i.DamageMin = 9), Does.Contain("minimum"));
        Assert.That(Problem(12, i => i.Weight = -1), Does.Contain("Weight"));
        Assert.That(Problem(12, i => i.StackSize = 0), Does.Contain("Stack"));
        Assert.That(Problem(12, i => i.Name = "sword"), Does.Contain("Only the unused"));
        Assert.That(Problem(12, i => i.Category = ItemCategory.Armour), Does.Contain("Only the unused"));
        Assert.That(Problem(12, i => i.Stance = ItemStance.Bow), Does.Contain("bow"));
        Assert.That(Problem(0, i => i.Stance = ItemStance.OneHanded), Does.Contain("bow"));
        Assert.That(Problem(12, i => i.Model = 105), Does.Contain("look"));
        Assert.That(Problem(51, i => i.Model = 59), Does.Contain("ARMOR.YAK"));
        Assert.That(Problem(51, i => i.Slot = ItemSlot.Helmet), Does.Contain("change slot"));
        Assert.That(Problem(55, i => i.Name = "fire ball"), Does.Contain("letters"));
        Assert.That(Problem(55, i => i.Name = "longsword"), Does.Contain("already"));
        Assert.That(Problem(55, i => i.Name = new string('x', 16)), Does.Contain("1 to 15"));
        Assert.That(Problem(12, i => i.DamageType = 0), Does.Contain("damage type"));
        Assert.That(Problem(55, i => (i.Category, i.Slot) = (ItemCategory.Weapon, ItemSlot.OneHanded)), Does.Contain("damage type"),
            "an unused record has none");
        Assert.That(Problem(12, i => i.Slot = ItemSlot.TwoHanded), Is.EqualTo(""), "a weapon can become two-handed");

        var bad = items[12].Clone();
        bad.DamageMin = 9;
        Assert.Throws<ArgumentException>(() => exe.SetItem(bad));
    }

    // ── models ──

    [Test]
    public void Models_AreReadAndRenamedInPlace()
    {
        var exe = Stock();
        var models = exe.GetItemModels();
        Assert.That(models, Has.Count.EqualTo(108));
        Assert.That((models[30].Vif, models[30].Tex), Is.EqualTo(("long_sword1.vif", "long_sword1.tex")));
        Assert.That((models[90].Vif, models[90].Tex), Is.EqualTo(("potion.vif", "potionmana.tex")));
        Assert.That(models[87].VifCapacity, Is.EqualTo(0), "potion.vif is shared by three models");
        Assert.That(models[101].VifCapacity, Is.GreaterThan(9), "scroll.vif is the scroll's own");

        exe.SetItemModel(101, "mysword.vif", "mysword.tex");
        exe.SetItemModel(49, "potion.vif", "potiononyx.tex");
        Assert.Throws<ArgumentException>(() => exe.SetItemModel(87, "averyveryverylongname.vif", "potion.tex"));
        Assert.Throws<ArgumentException>(() => exe.SetItemModel(100, "amulet3.tex", "amulet3.tex"));
        var (back, _) = SaveAndReopen(exe, "models.elf");
        var after = back.GetItemModels();
        Assert.That((after[101].Vif, after[101].Tex), Is.EqualTo(("mysword.vif", "mysword.tex")));
        Assert.That((after[49].Vif, after[49].Tex), Is.EqualTo(("potion.vif", "potiononyx.tex")));
        Assert.That(after.Where(m => m.Index is not (101 or 49)).Select(m => m.Vif + m.Tex),
            Is.EqualTo(models.Where(m => m.Index is not (101 or 49)).Select(m => m.Vif + m.Tex)));
    }

    // ── materials, stances, glow, loot lists ──

    [Test]
    public void Tuning_IsReadAndWritten()
    {
        var exe = Stock();
        Assert.That(exe.GetMaterialMultipliers(), Is.EqualTo(new[] { 0.5f, 1.5f, 2f, 3f, 4f }));
        Assert.That(exe.GetStanceWeights(), Is.EqualTo(ItemFormulas.StockStanceWeights.ToArray()));
        var glow = exe.GetEnchantGlow();
        Assert.That(glow, Is.EqualTo(new EnchantGlow(new Rgb(10, 10, 68), new Rgb(60, 60, 68))));
        Assert.That(exe.GetLootModifiers(LootModifierList.Weapons).Take(11), Is.EqualTo(new ulong[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 16 }));
        Assert.That(exe.GetLootModifiers(LootModifierList.RingsAndAmulets), Is.EqualTo(Enumerable.Range(31, 7).Select(b => 1UL << b).ToArray()));

        var written = exe.SetMaterialMultipliers(new[] { 0.5f, 1.3f, 2f, 3f, 6f });
        Assert.That(written, Is.EqualTo(new[] { 0.5f, 1.296875f, 2f, 3f, 6f }), "the upper half of 1.3");
        exe.SetStanceWeights(new[] { 1f, 1f, 1f, 1f, 1f, 1f });
        exe.SetEnchantGlow(new EnchantGlow(new Rgb(255, 0, 0), new Rgb(0, 255, 0)));
        var loot = exe.GetLootModifiers(LootModifierList.Armour);
        loot[0] = 1UL << ItemBits.Elven;
        exe.SetLootModifiers(LootModifierList.Armour, loot);
        Assert.Throws<ArgumentException>(() => exe.SetLootModifiers(LootModifierList.Armour, new ulong[] { 1, 2, 3 }));
        Assert.Throws<ArgumentException>(() => exe.SetLootModifiers(LootModifierList.Armour, new ulong[] { 1, 2, 3, 4, 0x30 }));

        var (back, bytes) = SaveAndReopen(exe, "tuning.elf");
        Assert.That(back.GetMaterialMultipliers(), Is.EqualTo(written));
        Assert.That(back.GetStanceWeights(), Is.EqualTo(new[] { 1f, 1f, 1f, 1f, 1f, 1f }));
        Assert.That(back.GetEnchantGlow(), Is.EqualTo(new EnchantGlow(new Rgb(255, 0, 0), new Rgb(0, 255, 0))));
        Assert.That(back.GetLootModifiers(LootModifierList.Armour), Is.EqualTo(loot));
        // 4 instruction bytes (2 multipliers), 24 stance bytes at most, 6 glow bytes, 8 loot bytes
        Assert.That(Differences(_stockBytes, bytes).Length, Is.LessThanOrEqualTo(4 + 24 + 6 + 8));
    }

    [Test]
    public void ItemEdits_SurviveTheThreePlayerPatch()
    {
        var exe = Stock();
        var sword = exe.GetItems()[12];
        sword.DamageMax = 99;
        exe.SetItem(sword);
        exe.SetShopTexts(new Dictionary<int, string> { [5] = "longsword+5+fireburst+keen" });
        exe.EnableThreePlayers();
        var (on, _) = SaveAndReopen(exe, "three.elf");
        Assert.That(on.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));
        Assert.That(on.GetItems()[12].DamageMax, Is.EqualTo(99));
        Assert.That(on.GetShopEntries()[5].Text, Is.EqualTo("longsword+5+fireburst+keen"));
        on.DisableThreePlayers();
        var (off, _) = SaveAndReopen(on, "three-off.elf");
        Assert.That(off.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off));
        Assert.That(off.GetItems()[12].DamageMax, Is.EqualTo(99));
        Assert.That(off.GetShopEntries()[5].Text, Is.EqualTo("longsword+5+fireburst+keen"));
    }

    [Test]
    public void ItemEdits_AreAppliedAllOrNothing()
    {
        var exe = Stock();
        Assert.That(exe.GetShopEntries()[17].Text, Is.EqualTo("longsword+1+fire"), "Bartley's 14th item");
        var spare = exe.GetItems()[60];
        spare.Name = "flametongue";
        spare.Category = ItemCategory.Weapon;
        spare.Slot = ItemSlot.OneHanded;
        (spare.DamageMin, spare.DamageMax, spare.DamageType) = (6, 12, 8);
        (spare.Model, spare.PickupModel) = (30, 30);
        spare.Flags = (spare.Flags & ~(1UL << ItemBits.NoRandomLoot)) | 1UL << ItemBits.Fire;

        var edits = new ItemEdits();
        edits.Items.Add(spare);
        edits.ShopTexts[17] = "flametongue+2";             // sells the record renamed in the same edits
        edits.Models[49] = ("long_sword1.vif", "long_sword1.tex");
        edits.MaterialMultipliers = new[] { 0.5f, 1.5f, 2.5f, 3f, 4f };
        edits.Loot[LootModifierList.Armour] = new ulong[] { 1, 2, 3, 1UL << ItemBits.Elven, 5 };
        exe.ApplyItemEdits(edits);
        var (back, _) = SaveAndReopen(exe, "edits.elf");
        Assert.That(back.GetItems()[60].Name, Is.EqualTo("flametongue"));
        Assert.That(back.GetShopEntries()[17].Text, Is.EqualTo("flametongue+2"));
        Assert.That(ItemText.Parse("flametongue+2", back.GetItems()).ItemIndex, Is.EqualTo(60));
        Assert.That(back.GetItemModels()[49].Vif, Is.EqualTo("long_sword1.vif"));
        Assert.That(back.GetMaterialMultipliers()[2], Is.EqualTo(2.5f));
        Assert.That(back.GetLootModifiers(LootModifierList.Armour)[3], Is.EqualTo(1UL << ItemBits.Elven));

        // the last part is wrong: the parts before it are undone too
        var stock = Stock();
        var bad = new ItemEdits();
        var sword = stock.GetItems()[12];
        sword.DamageMax = 20;
        bad.Items.Add(sword);
        bad.ShopTexts[17] = "longsword+5";
        bad.Loot[LootModifierList.Armour] = new ulong[] { 1, 2, 3 };
        Assert.Throws<ArgumentException>(() => stock.ApplyItemEdits(bad));
        var path = Path.Combine(_workDir, "edits-bad.elf");
        stock.Save(path);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(_stockBytes));
        Assert.That(stock.GetItems()[12].DamageMax, Is.EqualTo(8));
        Assert.That(stock.GetShopEntries()[17].Text, Is.EqualTo("longsword+1+fire"));
    }

    // ── items added after the game's 81 (three-player executables) ──

    /// <summary>The stock executable with three players on (the Three Players tab's patch), saved and opened again.</summary>
    private (BgdaExecutable Exe, byte[] Bytes) ThreePlayers(string name)
    {
        var exe = Stock();
        exe.EnableThreePlayers();
        return SaveAndReopen(exe, name);
    }

    /// <summary>A copy of item <paramref name="from"/> under another name.</summary>
    private static GameItem Copy(IReadOnlyList<GameItem> items, int from, string name, Action<GameItem>? edit = null)
    {
        var item = items[from].Clone();
        item.Name = name;
        edit?.Invoke(item);
        return item;
    }

    private const int VaToFile = 0xFF000;

    [Test]
    public void StockExecutable_HasNoRoomForMoreItems()
    {
        var exe = Stock();
        Assert.That((exe.ItemCount, exe.AddedItemCount), Is.EqualTo((81, 0)));
        Assert.That(exe.ItemRoomProblem(), Does.Contain("Turn on three players"));
        var items = exe.GetItems();
        Assert.Throws<InvalidOperationException>(() => exe.AddItem(Copy(items, 12, "orcbane")));
        exe.SetAddedItems(Array.Empty<GameItem>());
        var path = Path.Combine(_workDir, "stock-room.elf");
        exe.Save(path);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(_stockBytes));
    }

    [Test]
    public void ThreePlayerExecutable_TakesItemsAfterTheGamesOwn()
    {
        var (exe, threeBytes) = ThreePlayers("three-room.elf");
        Assert.That(exe.ItemRoomProblem(), Is.Null);
        Assert.That(BgdaExecutable.AddedItemsVa, Is.EqualTo(0x002A8FC8));
        Assert.That(BgdaExecutable.AddedItemsRoom, Is.EqualTo(5944));
        var items = exe.GetItems();
        var platehelmet = items.Single(i => i.Name == "platehelmet").Index;
        var ring = items.First(i => i.Name == "ring").Index;
        var orcbane = Copy(items, 12, "orcbane", i =>
        {
            i.DamageMax = 14;
            i.Flags = ItemBits.WithEnchantment(i.Flags, 2) & ~(1UL << ItemBits.NoRandomLoot);
        });
        var wizardry = Copy(items, ring, "ringofwizardry", i => i.Flags |= 1UL << ItemBits.Intelligence);
        var helm = Copy(items, platehelmet, "dragonhelm", i => i.Armour = 9);
        exe.SetAddedItems(new[] { orcbane, wizardry, helm });

        var (back, bytes) = SaveAndReopen(exe, "added.elf");
        Assert.That(back.ItemsProblem(), Is.Null);
        Assert.That(back.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On), "the three-player patch is untouched");
        var after = back.GetItems();
        Assert.That((after.Count, back.AddedItemCount), Is.EqualTo((84, 3)));
        Assert.That(after.Take(81).Select(i => i.Name), Is.EqualTo(items.Select(i => i.Name)));
        Assert.That(after.Skip(81).Select(i => i.Name), Is.EqualTo(new[] { "orcbane", "ringofwizardry", "dragonhelm" }));
        Assert.That(after.Skip(81).All(i => i.IsAdded && !i.IsSpare && i.NameCapacity == 31), Is.True);
        Assert.That(after.Take(81).Any(i => i.IsAdded), Is.False);
        Assert.That((after[81].DamageMin, after[81].DamageMax, after[81].Flags), Is.EqualTo((2, 14, orcbane.Flags)));
        Assert.That((after[82].Slot, after[82].Flags), Is.EqualTo((ItemSlot.Ring, wizardry.Flags)));
        Assert.That((after[83].Armour, after[83].Model, after[83].Category), Is.EqualTo((9, items[platehelmet].Model, ItemCategory.Armour)));
        Assert.That(ItemText.Parse("orcbane+1+fire", after).ItemIndex, Is.EqualTo(81));
        Assert.That(after.All(i => BgdaExecutable.CheckItem(i, i, after) == null), Is.True);

        // the records follow the game's, then a zero record ends the table, then the names, 8-aligned
        int Word(int va) => BitConverter.ToInt32(bytes, va - VaToFile);
        Assert.That(new[] { Word(0x2A8FC8), Word(0x2A8FF8), Word(0x2A9028) }, Is.EqualTo(new[] { 0x2A9088, 0x2A9090, 0x2A90A0 }));
        Assert.That(bytes.Skip(0x2A9058 - VaToFile).Take(0x30).All(b => b == 0), Is.True, "the zero record");
        Assert.That(Encoding.ASCII.GetString(bytes, 0x2A9088 - VaToFile, 0x28),
            Is.EqualTo("orcbane\0ringofwizardry\0\0dragonhelm\0\0\0\0\0\0"));
        Assert.That(Differences(threeBytes, bytes).All(o => o + VaToFile is >= 0x2A8FC8 and < 0x2A90AB), Is.True, "nothing else changes");

        // shops sell them (their names are copied into the shops' text area)
        back.SetShopTexts(new Dictionary<int, string> { [5] = "orcbane+1", [25] = "dragonhelm", [41] = "ringofwizardry+2" });
        var (sold, _) = SaveAndReopen(back, "added-sold.elf");
        var shops = sold.GetShopEntries();
        Assert.That(new[] { shops[5].Text, shops[25].Text, shops[41].Text }, Is.EqualTo(new[] { "orcbane+1", "dragonhelm", "ringofwizardry+2" }));
        Assert.That(sold.GetItems().Skip(81).Select(i => i.Name), Is.EqualTo(new[] { "orcbane", "ringofwizardry", "dragonhelm" }));
        Assert.That(sold.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));

        // removing an item a shop sells is refused: the game would sell a shortbow there
        var e = Assert.Throws<ArgumentException>(() => sold.ApplyItemEdits(new ItemEdits { AddedItems = new List<GameItem>() }));
        Assert.That(e!.Message, Does.Contain("no item is called \"orcbane\""));
        Assert.That(sold.ItemCount, Is.EqualTo(84), "nothing changed");
    }

    [Test]
    public void AddedItems_AreRenamedAndRemovedInPlace()
    {
        var (exe, threeBytes) = ThreePlayers("three-remove.elf");
        var items = exe.GetItems();
        var greatsword = items.Single(i => i.Name == "greatsword").Index;
        Assert.That(exe.AddItem(Copy(items, 12, "orcbane")), Is.EqualTo(81));
        Assert.That(exe.AddItem(Copy(items, greatsword, "giantslayer")), Is.EqualTo(82));
        Assert.That(exe.AddItem(Copy(items, 12, "elfblade")), Is.EqualTo(83));

        var renamed = exe.GetItems()[81];
        renamed.Name = "orcbanethegreater";
        renamed.DamageMax = 30;
        exe.SetItem(renamed);
        Assert.That(exe.GetItems().Skip(81).Select(i => i.Name), Is.EqualTo(new[] { "orcbanethegreater", "giantslayer", "elfblade" }));
        Assert.That(exe.GetItems()[81].DamageMax, Is.EqualTo(30));

        exe.RemoveAddedItem(82);
        var left = exe.GetItems();
        Assert.That(left.Skip(81).Select(i => (i.Index, i.Name)), Is.EqualTo(new[] { (81, "orcbanethegreater"), (82, "elfblade") }));
        Assert.That(left[82].DamageMax, Is.EqualTo(items[12].DamageMax));
        Assert.That(exe.ItemsProblem(), Is.Null);
        Assert.Throws<ArgumentOutOfRangeException>(() => exe.RemoveAddedItem(80));
        Assert.Throws<ArgumentOutOfRangeException>(() => exe.RemoveAddedItem(83));

        exe.SetAddedItems(Array.Empty<GameItem>());
        var (back, bytes) = SaveAndReopen(exe, "three-removed.elf");
        Assert.That(bytes, Is.EqualTo(threeBytes), "with them all removed the file is as it was");
        Assert.That(back.ItemCount, Is.EqualTo(81));
    }

    [Test]
    public void AddedItems_FillTheRoomAndNoMore()
    {
        var (exe, _) = ThreePlayers("three-full.elf");
        var items = exe.GetItems();
        // a 12-letter name takes 16 bytes: 64 an item, and 48 end the table
        var many = Enumerable.Range(0, 92).Select(k => Copy(items, 12, $"testsword{k:D3}")).ToList();
        Assert.That(BgdaExecutable.AddedItemsSize(many.Select(i => i.Name)), Is.EqualTo(93 * 48 + 92 * 16));
        exe.SetAddedItems(many);
        Assert.That((exe.ItemCount, exe.ItemsProblem()), Is.EqualTo((173, (string?)null)));

        many.Add(Copy(items, 12, "onetoomany"));
        var e = Assert.Throws<ArgumentException>(() => exe.SetAddedItems(many));
        Assert.That(e!.Message, Does.Contain("don't fit"));
        Assert.That(exe.ItemCount, Is.EqualTo(173), "nothing changed");

        var (back, bytes) = SaveAndReopen(exe, "full.elf");
        Assert.That(back.GetItems()[172].Name, Is.EqualTo("testsword091"));
        Assert.That(ItemText.Parse("testsword091+3", back.GetItems()).ItemIndex, Is.EqualTo(172));
        Assert.That(back.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On), "the three-player menu text after the room is untouched");
        Assert.That(Encoding.ASCII.GetString(bytes, 0x2AA6E8 - VaToFile, 16), Is.EqualTo("testsword091\0\0\0\0"), "the last name ends the room");
    }

    [Test]
    public void BadAddedItems_AreRefusedAndChangeNothing()
    {
        var (exe, threeBytes) = ThreePlayers("three-bad.elf");
        var items = exe.GetItems();
        void Refused(GameItem item, string expected, int index = 82)
        {
            var e = Assert.Throws<ArgumentException>(() => exe.SetAddedItems(new[] { Copy(items, 12, "first"), item }));
            Assert.That(e!.Message, Does.Contain(expected));
            Assert.That(e.Message, Does.Contain($"Added item {index}"));
        }

        Refused(Copy(items, 12, "longsword"), "already");
        Refused(Copy(items, 12, "first"), "already", 81);
        Refused(Copy(items, 12, "orc bane"), "letters");
        Refused(Copy(items, 12, ""), "1 to 31");
        Refused(Copy(items, 12, new string('x', 32)), "1 to 31");
        Refused(Copy(items, 12, "orcbane", i => i.DamageMin = 99), "minimum");
        Refused(Copy(items, 12, "orcbow"), "bow");
        Refused(Copy(items, 12, "orcbane", i => i.Model = 105), "look");

        // an added item can become any kind of item
        var ring = Copy(items, 12, "plainring", i =>
        {
            (i.Category, i.Slot, i.DamageType) = (ItemCategory.Other, ItemSlot.Ring, 0);
            i.Model = i.PickupModel = items.First(r => r.Name == "ring").Model;
            i.Flags = 0;
        });
        var added = ring.WithIndex(81);
        Assert.That(BgdaExecutable.CheckItem(added, new GameItem { Index = 81, Name = "plainring", IsAdded = true, NameCapacity = 31 },
            items.Append(added).ToList()), Is.Null);

        // all or nothing with the other edits
        var edits = new ItemEdits { AddedItems = new List<GameItem> { Copy(items, 12, "orcbane") } };
        edits.ShopTexts[5] = "orcbane+mithral";
        Assert.Throws<ArgumentException>(() => exe.ApplyItemEdits(edits));
        var path = Path.Combine(_workDir, "three-bad-after.elf");
        exe.Save(path);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(threeBytes));
        Assert.That(exe.ItemCount, Is.EqualTo(81));
    }

    [Test]
    public void ThreePlayers_StayOnWhileItemsAreAdded()
    {
        var exe = Stock();
        exe.EnableThreePlayers();
        exe.AddItem(Copy(exe.GetItems(), 12, "orcbane"));
        var e = Assert.Throws<InvalidOperationException>(() => exe.DisableThreePlayers());
        Assert.That(e!.Message, Does.Contain("Items tab"));
        Assert.That((exe.GetThreePlayerState(), exe.ItemCount), Is.EqualTo((ThreePlayerState.On, 82)));

        exe.RemoveAddedItem(81);
        exe.DisableThreePlayers();
        var (_, bytes) = SaveAndReopen(exe, "three-off-again.elf");
        Assert.That(bytes, Is.EqualTo(_stockBytes));
    }

    [Test]
    public void BrokenAddedItems_AreNoticed()
    {
        var exe = Stock();
        exe.EnableThreePlayers();
        exe.AddItem(Copy(exe.GetItems(), 12, "orcbane"));
        var (_, bytes) = SaveAndReopen(exe, "broken-source.elf");
        string? ProblemWith(Action<byte[]> damage)
        {
            var broken = (byte[])bytes.Clone();
            damage(broken);
            var path = Path.Combine(_workDir, "broken.elf");
            File.WriteAllBytes(path, broken);
            return BgdaExecutable.Open(path).ItemsProblem();
        }

        Assert.That(ProblemWith(_ => { }), Is.Null);
        Assert.That(ProblemWith(b => BitConverter.GetBytes(0x0030B1C0).CopyTo(b, 0x2A8FC8 - VaToFile)), Does.Contain("Added item 81"),
            "a name outside the room");
        Assert.That(ProblemWith(b => b[0x2AA6F0 - VaToFile] = 1), Does.Contain("isn't empty"));
        Assert.That(ProblemWith(b => BitConverter.GetBytes(0x002A9000).CopyTo(b, 0x2A8098 + 0x10 - VaToFile)), Does.Contain("refers to it"),
            "the shortbow's price holding an address in the room");
        Assert.That(ProblemWith(b =>
        {
            foreach (var va in new[] { 0x0016F4C4, 0x00175C94, 0x00175CC0, 0x00178D74, 0x00178E44, 0x00179214, 0x0017B094, 0x0017B150, 0x0017B154, 0x0017B160 })
                b[va - VaToFile] = 0x2B;
        }), Does.Contain("HUD"), "the HUD's data back after the table");
    }

    // ── shops ──

    [Test]
    public void StockShops_AreTheGamesLists()
    {
        var exe = Stock();
        Assert.That(exe.ShopsProblem(), Is.Null);
        var shops = exe.GetShopEntries();
        var items = exe.GetItems();
        Assert.That(shops, Has.Count.EqualTo(142));
        Assert.That(shops.Count(e => e.Stock == ShopStock.Bartley), Is.EqualTo(39));
        Assert.That(shops.First(e => e.Stock == ShopStock.Bartley).Text, Is.EqualTo("arrows"));
        Assert.That(shops.Single(e => e.Text.StartsWith("halberd+mithral")).Stock, Is.EqualTo(ShopStock.Branoch));
        Assert.That(shops.Where(e => !e.AnyText).Select(e => e.Text), Is.EqualTo(new[] { "arrows", "arrows", "arrows", "arrows" }));
        foreach (var e in shops)
        {
            var parsed = ItemText.Parse(e.Text, items);
            Assert.That(parsed.ItemIndex, Is.Not.EqualTo(-1), e.Text);
            Assert.That(items[parsed.ItemIndex].Category, Is.EqualTo(e.List), e.Text);
        }

        Assert.That(ItemText.Parse("halberd+mithral+defense+4", items).Problems[0], Does.Contain("mithril"));
        var nearby = exe.NearbyShopTexts(items);
        Assert.That(nearby.Contains("arrows") && nearby.Contains("dagger") && nearby.Contains("club"), Is.True);
    }

    [Test]
    public void ShopText_IsRepackedWithoutTouchingOtherStrings()
    {
        var exe = Stock();
        var before = exe.GetShopEntries();
        var items = exe.GetItems();
        var changes = new Dictionary<int, string>
        {
            [10] = "warhammer+masterwork+shock+5",                      // Bartley's "warhammer" (an item's name) -> new text
            [11] = "longsword",                                         // an item's name: no room needed
            [44] = "dagger+adamantine+5+superhaste+disruption",         // Branoch
            [12] = "greatsword+mithril+5+fireburst+keen+speed",
            [6] = "dagger+3"                                            // Bartley's dagger has its own lui: moves next to the others
        };
        exe.SetShopTexts(changes);
        var (back, bytes) = SaveAndReopen(exe, "shops.elf");
        var after = back.GetShopEntries();
        for (var i = 0; i < before.Count; i++)
        {
            Assert.That(after[i].Text, Is.EqualTo(changes.TryGetValue(i, out var t) ? t : before[i].Text), $"entry {i}");
        }

        Assert.That(back.GetItems().Select(i => i.Name), Is.EqualTo(items.Select(i => i.Name)), "item names stay");
        // only the shop code and the shop-only strings change
        foreach (var o in Differences(_stockBytes, bytes))
        {
            var va = o + 0xFF000;
            Assert.That(va is >= 0x0016FF50 and < 0x00171818 or >= 0x0030B160 and < 0x0030BB10, Is.True, $"0x{va:X}");
        }

        // the unchanged stock texts come back exactly when the old texts are restored
        back.SetShopTexts(before.ToDictionary(e => e.Number, e => e.Text));
        Assert.That(back.GetShopEntries().Select(e => e.Text), Is.EqualTo(before.Select(e => e.Text)));
    }

    [Test]
    public void BadShopTexts_AreRefusedAndChangeNothing()
    {
        var exe = Stock();
        var path = Path.Combine(_workDir, "shops-bad.elf");
        Assert.Throws<ArgumentException>(() => exe.SetShopTexts(new Dictionary<int, string> { [5] = "longsword+mithral" }));
        Assert.Throws<ArgumentException>(() => exe.SetShopTexts(new Dictionary<int, string> { [5] = "leatherarmor" }));
        Assert.Throws<ArgumentException>(() => exe.SetShopTexts(new Dictionary<int, string> { [5] = "nothing" }));
        Assert.Throws<ArgumentException>(() => exe.SetShopTexts(new Dictionary<int, string> { [4] = "longsword" }));   // an arrows entry
        var orders = new[] { "fireburst+keen+speed+superhaste", "keen+fireburst+speed+superhaste",
                             "speed+keen+fireburst+superhaste", "superhaste+speed+keen+fireburst" };
        var tooMuch = exe.GetShopEntries().Where(e => e.AnyText && e.List == ItemCategory.Weapon)
            .ToDictionary(e => e.Number, e => $"greatsword+mithril+{e.Number % 15 + 1}+{orders[e.Number / 15 % 4]}");
        Assert.That(tooMuch.Values.Distinct().Count(), Is.GreaterThan(40), "different texts, so they can't share");
        Assert.Throws<ArgumentException>(() => exe.SetShopTexts(tooMuch));
        exe.Save(path);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(_stockBytes));
    }

    // ── item text and formulas ──

    [Test]
    public void ItemText_IsReadLikeTheGame()
    {
        var items = Stock().GetItems();
        var p = ItemText.Parse("longsword+masterwork+1+fireburst", items);
        Assert.That(p.IsValid, Is.True);
        Assert.That(p.ItemIndex, Is.EqualTo(12));
        Assert.That(p.Flags & 0x0000_0FFF_FFFF_FFFF & ~ItemBits.LooksMask,
            Is.EqualTo((items[12].Flags | (1UL << ItemBits.Masterwork) | 1 | (1UL << ItemBits.FireBurst)) & 0x0000_0FFF_FFFF_FFFF & ~ItemBits.LooksMask));
        Assert.That(ItemText.Parse("arrows", items).Count, Is.EqualTo(20));
        Assert.That(ItemText.Parse("ring+protection+16", items).IsValid, Is.False);
        Assert.That(ItemText.Parse("ring+1+2", items).Flags & 0xF, Is.EqualTo(3UL), "numbers are OR-ed like the game");
        Assert.That(ItemText.Parse("sword", items).ItemIndex, Is.EqualTo(-1));
        Assert.That(ItemText.Parse("longsword+frost", items).Problems[0], Does.Contain("frost"));
    }

    private static readonly (int Item, int Count, ulong Flags, int Value)[] PlainValues =
    {
        (0, 1, 0x000200000C090000UL, 521),
        (1, 1, 0x0000200021490000UL, 3),
        (2, 1, 0x0102000008010000UL, 133),
        (3, 1, 0x0080080010020000UL, 112),
        (4, 1, 0x0102000050010000UL, 205),
        (5, 1, 0x0100080008010000UL, 205),
        (6, 1, 0x0100080000010000UL, 1982),
        (7, 1, 0x0600080008090000UL, 197),
        (8, 1, 0x0100080010090000UL, 205),
        (9, 1, 0x0200080008090000UL, 684),
        (10, 1, 0x0200080008090000UL, 2365),
        (11, 1, 0x0100080008020000UL, 205),
        (12, 1, 0x0100080014020000UL, 433),
        (13, 1, 0x010008000C040000UL, 742),
        (14, 1, 0x010008000C040000UL, 742),
        (15, 1, 0x0200080008020000UL, 1853),
        (16, 1, 0x0200080010020000UL, 3090),
        (17, 1, 0x0100080020040000UL, 198846),
        (18, 1, 0x0200080004020000UL, 2494),
        (19, 1, 0x0100080044020000UL, 293),
        (20, 1, 0x0000080021040041UL, 13216),
        (21, 1, 0x0000080021040001UL, 13418),
        (22, 1, 0x0002000000000000UL, 80),
        (23, 1, 0x0000080014000000UL, 600),
        (24, 1, 0x0000200060000000UL, 5),
        (25, 1, 0x0000200020000000UL, 4),
        (26, 1, 0x0000200020000000UL, 4),
        (27, 1, 0x0000200040000000UL, 400),
        (28, 1, 0x0000200000000000UL, 100),
        (29, 1, 0x0000200000000000UL, 110),
        (30, 1, 0x0000200000000000UL, 120),
        (31, 1, 0x0000400040000000UL, 1200),
        (32, 1, 0x0000400000000000UL, 300),
        (33, 1, 0x0000400000000000UL, 350),
        (34, 1, 0x0000400000000000UL, 350),
        (35, 1, 0x0000400040000000UL, 2200),
        (36, 1, 0x0000400000000000UL, 650),
        (37, 1, 0x0000400000000000UL, 650),
        (38, 1, 0x0000400000000000UL, 650),
        (39, 1, 0x0000880040000000UL, 5000),
        (40, 1, 0x0000880000000000UL, 1450),
        (41, 1, 0x0000880000000000UL, 1350),
        (42, 1, 0x0000880000000000UL, 1450),
        (43, 1, 0x0000880040000000UL, 10000),
        (44, 1, 0x0000880000000000UL, 3000),
        (45, 1, 0x0000880000000000UL, 3450),
        (46, 1, 0x0000880000000000UL, 3000),
        (47, 1, 0x0001080040000000UL, 16000),
        (48, 1, 0x0001080000000000UL, 3500),
        (49, 1, 0x0001080000000000UL, 3800),
        (50, 1, 0x0001080000000000UL, 3300),
        (51, 1, 0x0001080040000000UL, 25000),
        (52, 1, 0x0001080000000000UL, 3300),
        (53, 1, 0x0001080000000000UL, 3700),
        (54, 1, 0x0001080000000000UL, 3000),
        (55, 1, 0x0000000020000000UL, 75),
        (56, 1, 0x0000000020000000UL, 75),
        (57, 1, 0x0000000020000000UL, 75),
        (58, 1, 0x0000000020000000UL, 75),
        (59, 1, 0x0000000020000000UL, 50),
        (60, 1, 0x0000000020000000UL, 63),
        (61, 1, 0x0000000020000000UL, 75),
        (62, 1, 0x0000000020000000UL, 38),
        (63, 1, 0x0004000020400000UL, 40),
        (64, 1, 0x0004000020400000UL, 125),
        (65, 1, 0x0004000020400000UL, 375),
        (66, 1, 0x0004000020400000UL, 40),
        (67, 1, 0x0004000020400000UL, 125),
        (68, 1, 0x0004000020400000UL, 375),
        (69, 1, 0x0010000010000000UL, 315),
        (70, 1, 0x0010000010000000UL, 315),
        (71, 1, 0x0010000010000000UL, 315),
        (72, 1, 0x000800000C000000UL, 300),
        (73, 1, 0x000800000C000000UL, 300),
        (74, 1, 0x000800000C000000UL, 300),
        (75, 1, 0x0004000020400000UL, 225),
        (76, 1, 0x0040000002400000UL, 100),
        (77, 1, 0x0040000002400000UL, 500),
        (78, 1, 0x0040000002400000UL, 1150),
        (79, 1, 0x0040000002400000UL, 3100),
        (80, 1, 0x0000880061000000UL, 10000),
    };

    private static readonly (int Item, int Count, ulong Flags, int Value, int Min, int Max, int Power)[] Vectors =
    {
        (27, 20, 0x0000300140000016UL, 12397, 3, 3, 13),
        (5, 2, 0x0100180208810400UL, 1168, 1, 3, 8),
        (73, 2, 0x000804000C00000CUL, 23487, 36, 36, 12),
        (7, 1, 0x0600084008091227UL, 158498, 8, 15, 21),
        (44, 1, 0x0000881000200007UL, 11466, 14, 14, 21),
        (3, 3, 0x0080080010020001UL, 800, 2, 5, 8),
        (56, 7, 0x000000002000001DUL, 15983, 14, 19, 21),
        (78, 1, 0x00400100024000AEUL, 26203, 14, 14, 16),
        (24, 2, 0x0000220460000401UL, 313, 4, 4, 9),
        (2, 20, 0x010200880801120EUL, 219515, 24, 26, 34),
        (31, 20, 0x000040804000000CUL, 76683, 18, 18, 12),
        (17, 3, 0x0100080020240023UL, 1296631, 186, 246, 11),
        (45, 2, 0x0000889080000007UL, 20426, 11, 11, 28),
        (1, 7, 0x0000200021490029UL, 671, 10, 15, 17),
        (74, 2, 0x000800000C000809UL, 15036, 9, 9, 18),
        (80, 1, 0x00009A006120100EUL, 26841, 56, 56, 16),
        (73, 7, 0x000802400C000015UL, 6309, 20, 20, 13),
        (54, 1, 0x00010C0500000003UL, 6620, 9, 9, 16),
        (24, 1, 0x0000242060008003UL, 2108, 9, 9, 13),
        (4, 1, 0x0102050050010052UL, 42798, 24, 24, 4),
        (72, 1, 0x000800000C00C005UL, 6309, 5, 5, 14),
        (10, 1, 0x0200084808090004UL, 18140, 16, 20, 8),
        (72, 20, 0x000800100C200008UL, 22123, 16, 16, 16),
        (41, 1, 0x000088000000000FUL, 17386, 15, 15, 22),
        (47, 7, 0x000118004000080AUL, 153668, 5, 5, 12),
        (9, 1, 0x0200081008090004UL, 5409, 8, 16, 8),
        (48, 3, 0x0001080000000101UL, 14155, 1, 1, 10),
        (4, 1, 0x010200805081A004UL, 774299, 8, 15, 14),
        (7, 7, 0x0600081008090005UL, 5487, 6, 13, 17),
        (59, 20, 0x000000002020000BUL, 25755, 22, 22, 18),
        (53, 1, 0x0001084000004080UL, 7400, 0, 0, 3),
        (67, 1, 0x0004005020600041UL, 1998, 2, 2, 10),
        (32, 1, 0x00004000002004A6UL, 4339, 12, 12, 8),
        (4, 1, 0x0102002150010002UL, 2058, 3, 8, 6),
        (13, 1, 0x010008000C042103UL, 111933, 8, 10, 14),
        (18, 7, 0x0200090804020A0FUL, 249879, 39, 39, 43),
        (10, 1, 0x0200090108092800UL, 78038, 16, 16, 4),
        (7, 20, 0x060008000809000DUL, 30621, 14, 21, 20),
        (39, 1, 0x0000880140000044UL, 11608, 4, 4, 9),
        (24, 3, 0x000020806000804FUL, 20009, 23, 23, 23),
        (33, 1, 0x000040000020004BUL, 9557, 22, 22, 19),
        (73, 20, 0x000806480C00000DUL, 34632, 52, 52, 33),
        (40, 3, 0x000099000000820EUL, 27743, 7, 7, 18),
        (13, 20, 0x01000C000C040905UL, 73979, 30, 36, 16),
        (10, 1, 0x020008020809000BUL, 33214, 23, 27, 29),
        (41, 1, 0x0000880100000007UL, 7675, 7, 7, 21),
        (39, 3, 0x0000880040004024UL, 30644, 4, 4, 7),
        (23, 1, 0x0000080014200001UL, 1092, 2, 2, 8),
        (1, 20, 0x000030082169040BUL, 2358, 24, 34, 29),
        (48, 1, 0x00010840800000C5UL, 17189, 5, 5, 19),
        (51, 1, 0x0001080040000486UL, 58133, 6, 6, 7),
        (79, 3, 0x004000000240000EUL, 183137, 14, 14, 14),
        (7, 1, 0x0600085008290006UL, 28563, 14, 28, 12),
        (60, 20, 0x0000000A20000447UL, 52942, 8, 13, 29),
        (5, 7, 0x0100084008010000UL, 410, 1, 6, 0),
        (23, 1, 0x0000080814000033UL, 2441, 3, 3, 15),
        (67, 20, 0x0004000420400000UL, 2500, 0, 0, 0),
        (64, 3, 0x00040000A040000FUL, 20752, 15, 15, 37),
        (22, 2, 0x000200010000000BUL, 9044, 11, 11, 29),
        (36, 1, 0x000041000000000DUL, 12748, 13, 13, 20),
        (37, 7, 0x000044000080010BUL, 20905, 33, 33, 28),
        (69, 1, 0x0010010090000001UL, 1023, 1, 1, 9),
        (68, 3, 0x000400002040000AUL, 15978, 10, 10, 10),
        (55, 20, 0x000015002000000CUL, 25452, 54, 54, 12),
        (1, 1, 0x000020042149000EUL, 352, 15, 20, 28),
        (76, 20, 0x0040000002400005UL, 33852, 5, 5, 12),
        (54, 7, 0x0001180000000008UL, 30800, 4, 4, 8),
        (74, 1, 0x000800040C00104EUL, 54519, 14, 14, 31),
        (15, 3, 0x020008000802000CUL, 35973, 20, 30, 12),
        (15, 2, 0x0200080008828096UL, 283781, 14, 24, 16),
        (64, 1, 0x0004000020C00001UL, 3071, 1, 1, 16),
        (61, 1, 0x0000005420800001UL, 2163, 2, 7, 18),
        (67, 1, 0x00040080A040A006UL, 2708, 9, 9, 14),
        (26, 20, 0x0000248020000302UL, 916, 6, 6, 8),
        (65, 1, 0x0004000120400096UL, 7624, 6, 6, 14),
        (80, 1, 0x0000880061000008UL, 29596, 8, 8, 8),
        (57, 1, 0x000000222000090AUL, 3996, 11, 16, 34),
        (11, 1, 0x01000C0008028146UL, 69381, 21, 36, 9),
        (36, 20, 0x0000420000800242UL, 20966, 8, 8, 15),
        (68, 1, 0x0004000020400009UL, 8713, 9, 9, 16),
        (77, 20, 0x004000000240600EUL, 255302, 14, 14, 18),
        (6, 20, 0x01000A0000019000UL, 72703, 32, 48, 2),
        (3, 2, 0x008008001002000DUL, 47428, 14, 17, 20),
        (41, 1, 0x000088040000004EUL, 16058, 14, 14, 29),
        (51, 1, 0x0001080040000001UL, 33288, 1, 1, 8),
        (16, 7, 0x0200088410020001UL, 9469, 17, 38, 9),
        (49, 1, 0x0001080000000007UL, 13304, 7, 7, 14),
        (38, 20, 0x0000428000000406UL, 31999, 24, 24, 6),
        (70, 2, 0x0010020010200815UL, 6625, 20, 20, 15),
        (39, 20, 0x0000988140000011UL, 66275, 2, 2, 10),
        (55, 20, 0x0000008220000849UL, 61166, 15, 23, 28),
        (50, 2, 0x0001080800804209UL, 23081, 9, 9, 39),
        (72, 1, 0x000800080C006044UL, 6016, 4, 4, 13),
        (72, 3, 0x000800010C000016UL, 14923, 6, 6, 13),
        (78, 7, 0x0040104202400003UL, 146986, 2, 2, 13),
        (14, 2, 0x010008000C040201UL, 3363, 6, 8, 12),
        (69, 3, 0x001000001000000AUL, 18564, 10, 10, 10),
        (80, 7, 0x0000880061000007UL, 165391, 7, 7, 14),
        (23, 7, 0x000018001400000EUL, 18274, 7, 7, 14),
        (62, 1, 0x0000006020200208UL, 1318, 18, 28, 20),
        (5, 20, 0x010008808801001AUL, 51622, 17, 24, 21),
        (29, 20, 0x000020000000000BUL, 14556, 11, 11, 18),
        (44, 20, 0x0000880080000307UL, 142417, 7, 7, 27),
        (34, 3, 0x0000400000000008UL, 8042, 8, 8, 8),
        (53, 7, 0x00010C000000025FUL, 91808, 45, 45, 28),
        (48, 1, 0x0001080000000002UL, 6223, 2, 2, 2),
        (51, 1, 0x0001180040000309UL, 37901, 5, 5, 22),
        (53, 20, 0x0001080000000000UL, 74000, 0, 0, 0),
        (49, 20, 0x000108000000004AUL, 205858, 10, 10, 11),
        (6, 20, 0x01000C080001008FUL, 354326, 69, 81, 38),
        (43, 2, 0x0000880840008006UL, 70866, 6, 6, 12),
        (29, 2, 0x0000200000200008UL, 6044, 16, 16, 8),
        (20, 7, 0x0000082221040043UL, 20809, 12, 15, 17),
        (36, 20, 0x000040300080001DUL, 48239, 13, 13, 55),
        (58, 3, 0x000001002020000EUL, 4675, 40, 40, 14),
        (43, 1, 0x000088884000808EUL, 62417, 21, 21, 29),
        (46, 1, 0x00008A0200000006UL, 10220, 24, 24, 12),
        (77, 1, 0x004000000240010AUL, 8651, 10, 10, 12),
        (35, 1, 0x0000400A40000042UL, 4206, 2, 2, 7),
        (70, 7, 0x0010100010800085UL, 6625, 3, 3, 21),
        (46, 1, 0x000088210000084BUL, 16717, 11, 11, 43),
        (4, 7, 0x0102000050010008UL, 19326, 9, 14, 8),
        (43, 3, 0x0000880040008107UL, 110243, 7, 7, 16),
        (4, 2, 0x0102006450010041UL, 13907, 2, 7, 11),
        (40, 7, 0x0000880000000043UL, 19069, 3, 3, 11),
        (31, 2, 0x0000400040000041UL, 3467, 1, 1, 9),
        (33, 1, 0x0000408100000001UL, 762, 2, 2, 9),
        (36, 7, 0x000040000080080DUL, 23956, 13, 13, 30),
        (25, 20, 0x0000200020000006UL, 3860, 6, 6, 6),
        (45, 20, 0x000088050000408CUL, 202225, 12, 12, 39),
        (48, 1, 0x0001080000000003UL, 7491, 3, 3, 10),
        (48, 20, 0x0001080200002009UL, 182334, 9, 9, 27),
        (47, 1, 0x000108084000103BUL, 51842, 11, 11, 33),
        (24, 1, 0x0000200060000008UL, 5526, 8, 8, 8),
        (33, 1, 0x0000412400000083UL, 2006, 3, 3, 17),
        (56, 2, 0x0000000020800002UL, 2347, 3, 8, 10),
        (33, 1, 0x000040208000000DUL, 11886, 13, 13, 46),
        (80, 1, 0x00008A406100C00EUL, 106730, 56, 56, 16),
        (23, 1, 0x0000080014001001UL, 1092, 1, 1, 10),
        (57, 2, 0x0000001820000006UL, 4076, 7, 12, 18),
        (9, 3, 0x0200080208090009UL, 18303, 13, 21, 25),
        (65, 1, 0x0004001020E00080UL, 4971, 0, 0, 9),
        (31, 2, 0x0000400040000009UL, 12531, 9, 9, 16),
        (72, 2, 0x000800000C001005UL, 6309, 5, 5, 14),
        (38, 1, 0x0000440000000008UL, 7079, 24, 24, 8),
        (19, 7, 0x01000A504402080AUL, 317242, 48, 64, 22),
        (43, 3, 0x0000880040000080UL, 30000, 0, 0, 1),
        (73, 1, 0x000800000C800001UL, 975, 1, 1, 16),
        (45, 1, 0x0000881000008207UL, 18749, 7, 7, 25),
        (16, 1, 0x0200090010022007UL, 121411, 31, 31, 16),
        (9, 7, 0x0200080008090007UL, 12145, 11, 19, 14),
        (62, 1, 0x0000000120004009UL, 1715, 10, 15, 27),
        (1, 1, 0x000020842149001BUL, 359, 18, 26, 30),
        (59, 3, 0x00000008A0000116UL, 4745, 6, 6, 21),
        (21, 2, 0x00000806A1040001UL, 13418, 17, 33, 11),
        (54, 3, 0x0001081A00008006UL, 34930, 6, 6, 24),
        (2, 20, 0x0102000108011144UL, 61938, 6, 7, 13),
        (23, 1, 0x00000804140020AEUL, 13842, 14, 14, 32),
        (38, 2, 0x00004410000000CEUL, 15910, 42, 42, 30),
        (67, 3, 0x0004100020601804UL, 4454, 8, 8, 8),
        (21, 1, 0x000008882124004FUL, 301047, 62, 94, 38),
        (61, 1, 0x000000002080000EUL, 2610, 15, 20, 22),
        (58, 3, 0x0000000020000800UL, 1003, 1, 6, 2),
        (49, 3, 0x000108000000100AUL, 37284, 10, 10, 12),
        (52, 1, 0x0001080C00008081UL, 6982, 1, 1, 11),
        (58, 1, 0x000014002000090EUL, 2163, 45, 60, 18),
        (8, 7, 0x0100089010092800UL, 96697, 2, 9, 4),
        (2, 1, 0x0102000008012005UL, 105284, 7, 8, 14),
        (36, 1, 0x0000400000000007UL, 6067, 7, 7, 14),
        (76, 3, 0x004000000240000CUL, 5290, 12, 12, 12),
        (52, 1, 0x0001180000000006UL, 7279, 3, 3, 6),
        (18, 1, 0x0200080904020007UL, 24635, 9, 31, 28),
        (14, 2, 0x010008400C040C81UL, 116171, 6, 8, 11),
        (41, 1, 0x0000880000000009UL, 9893, 9, 9, 16),
        (62, 1, 0x0000010220000103UL, 1042, 9, 9, 15),
        (46, 20, 0x000088000000010FUL, 195180, 15, 15, 24),
        (6, 1, 0x010008800081000EUL, 382683, 33, 39, 22),
        (12, 3, 0x010008801402400AUL, 52382, 18, 27, 12),
        (3, 1, 0x008008101082108AUL, 746456, 11, 14, 31),
        (16, 3, 0x02000C0110021002UL, 74156, 36, 78, 6),
        (61, 20, 0x0000154020000008UL, 17067, 42, 42, 8),
        (63, 1, 0x0004008620C00000UL, 698, 0, 0, 8),
        (39, 7, 0x000088404000480BUL, 197746, 11, 11, 22),
        (53, 3, 0x0001088300000000UL, 11100, 0, 0, 0),
        (1, 1, 0x000020002149000EUL, 299, 15, 20, 14),
        (54, 1, 0x0001080000000207UL, 11466, 7, 7, 18),
        (42, 1, 0x0000880400000007UL, 7905, 7, 7, 21),
        (68, 20, 0x000400002040014CUL, 158591, 12, 12, 15),
        (74, 20, 0x000800000C000002UL, 1942, 2, 2, 2),
        (45, 1, 0x000088000000000DUL, 20795, 13, 13, 20),
        (44, 3, 0x00008C080000080FUL, 40577, 45, 45, 39),
        (4, 7, 0x0102000050010003UL, 3619, 4, 9, 10),
        (2, 20, 0x010200000801100EUL, 110101, 16, 17, 16),
        (1, 3, 0x0000200221690000UL, 8, 2, 12, 0),
        (32, 1, 0x0000400080000001UL, 696, 1, 1, 9),
        (36, 1, 0x0000404000000042UL, 2809, 2, 2, 3),
        (70, 2, 0x001000011000040AUL, 33787, 10, 10, 20),
        (20, 1, 0x0000080221042445UL, 251114, 14, 17, 20),
        (31, 2, 0x0000408040002029UL, 12531, 14, 14, 19),
        (69, 7, 0x0010001110000107UL, 19014, 7, 7, 30),
        (6, 2, 0x010008120001140AUL, 132310, 18, 22, 32),
        (70, 1, 0x0010004010000002UL, 2039, 2, 2, 2),
        (17, 2, 0x0100081120040041UL, 260041, 91, 121, 11),
        (20, 3, 0x0000080021040043UL, 20809, 12, 15, 11),
        (21, 3, 0x0000080021040801UL, 27148, 17, 33, 10),
        (35, 2, 0x000040004000000EUL, 25068, 14, 14, 14),
        (33, 7, 0x000040000000210FUL, 20721, 15, 15, 26),
        (58, 2, 0x000000402000404FUL, 5652, 16, 21, 25),
        (51, 7, 0x0001088240000214UL, 335229, 6, 6, 13),
        (47, 1, 0x0001080040000027UL, 41332, 7, 7, 15),
        (68, 2, 0x0004001020400001UL, 9692, 1, 1, 9),
        (33, 1, 0x000040100000880FUL, 21532, 15, 15, 39),
        (58, 3, 0x000001002020000EUL, 4675, 40, 40, 14),
        (39, 2, 0x0000880040000009UL, 31621, 9, 9, 16),
        (40, 1, 0x0000880000000027UL, 7905, 7, 7, 15),
        (63, 1, 0x000400002040000BUL, 1270, 11, 11, 18),
        (29, 7, 0x0000204800000093UL, 4078, 3, 3, 15),
        (44, 20, 0x00009A1000000012UL, 47347, 8, 8, 5),
        (61, 7, 0x0000000020000007UL, 10574, 8, 13, 14),
        (52, 1, 0x000108140000081AUL, 16147, 10, 10, 33),
        (39, 1, 0x0000880040000007UL, 16061, 7, 7, 14),
        (10, 1, 0x0200180008090004UL, 4280, 8, 10, 4),
        (68, 7, 0x00040440A0400109UL, 102816, 27, 27, 27),
        (55, 20, 0x0000000420000088UL, 36299, 9, 14, 17),
        (9, 2, 0x0200080008090007UL, 12145, 11, 19, 14),
        (46, 1, 0x0000880100800004UL, 7800, 4, 4, 16),
        (27, 1, 0x0000200040000931UL, 828, 1, 1, 14),
        (59, 1, 0x0000002A20000006UL, 1975, 6, 6, 24),
        (59, 1, 0x0000000020000004UL, 544, 4, 4, 4),
        (42, 7, 0x0000880000004310UL, 10150, 0, 0, 9),
        (4, 1, 0x0102000050010021UL, 1389, 2, 7, 9),
        (29, 1, 0x000020000000000FUL, 13627, 15, 15, 22),
        (7, 3, 0x060008000809000DUL, 30621, 14, 21, 20),
        (23, 1, 0x000008001400000CUL, 11401, 12, 12, 12),
        (0, 2, 0x000200000C092102UL, 76541, 6, 10, 6),
        (49, 1, 0x000108040020000EUL, 23296, 28, 28, 28),
        (44, 20, 0x000088000000000BUL, 170727, 11, 11, 18),
        (50, 3, 0x0001080000000002UL, 16155, 2, 2, 2),
        (22, 1, 0x0002050080000008UL, 5706, 24, 24, 16),
        (47, 3, 0x000108004000010EUL, 153870, 14, 14, 16),
        (37, 1, 0x0000420100000003UL, 2528, 12, 12, 13),
        (54, 7, 0x0001080800004007UL, 52819, 7, 7, 23),
        (68, 2, 0x0004008120408023UL, 14999, 5, 5, 14),
        (48, 7, 0x0001080000804000UL, 24500, 0, 0, 10),
        (16, 20, 0x020008001002000AUL, 31065, 20, 34, 10),
        (30, 20, 0x0000200000000001UL, 3467, 1, 1, 8),
        (54, 1, 0x0001080000000008UL, 12738, 8, 8, 8),
        (33, 1, 0x0000400100000406UL, 4448, 6, 6, 12),
        (7, 20, 0x0600080008090008UL, 12363, 9, 16, 8),
        (21, 1, 0x0000080021040003UL, 21061, 19, 35, 10),
        (75, 1, 0x0004042220400020UL, 732, 0, 0, 1),
        (77, 1, 0x0040100002409109UL, 14487, 5, 5, 20),
        (70, 3, 0x0010002110800083UL, 5952, 3, 3, 25),
        (18, 1, 0x0200081004020106UL, 27951, 8, 30, 14),
        (40, 7, 0x0000982000002404UL, 11750, 2, 2, 10),
        (38, 1, 0x0000400100002402UL, 1800, 2, 2, 6),
        (18, 7, 0x0200082204020004UL, 12025, 6, 28, 12),
        (41, 7, 0x0000880100000806UL, 24267, 6, 6, 14),
        (32, 1, 0x000040100000020BUL, 9422, 11, 11, 33),
        (62, 2, 0x0000008020002184UL, 1194, 8, 15, 9),
        (12, 7, 0x0100080014220002UL, 6302, 8, 20, 2),
        (67, 20, 0x0004000020400004UL, 14933, 4, 4, 4),
        (66, 1, 0x000400002040000CUL, 922, 12, 12, 12),
        (6, 1, 0x0100080000018008UL, 28160, 16, 20, 8),
        (23, 20, 0x00000C1414000802UL, 19414, 6, 6, 8),
        (23, 1, 0x000018401400080CUL, 11401, 6, 6, 14),
        (67, 1, 0x000400002040046FUL, 4562, 15, 15, 24),
        (55, 1, 0x0000040020801408UL, 2163, 27, 42, 18),
        (42, 2, 0x00008810000000A1UL, 4127, 1, 1, 11),
        (2, 7, 0x0102042808014001UL, 3996, 9, 12, 12),
        (65, 1, 0x000400002040000BUL, 9814, 11, 11, 18),
        (39, 7, 0x0000880040000008UL, 89802, 8, 8, 8),
        (15, 1, 0x020008000802000CUL, 35973, 20, 30, 12),
        (61, 1, 0x0000010020800007UL, 2610, 13, 13, 22),
        (61, 1, 0x0000000020000009UL, 1943, 10, 15, 16),
        (55, 1, 0x0000100820000405UL, 2052, 3, 6, 17),
        (46, 1, 0x0000882000002400UL, 3000, 0, 0, 2),
        (55, 3, 0x0000000020000005UL, 4030, 6, 11, 12),
        (68, 1, 0x0004000020400003UL, 5493, 3, 3, 10),
        (36, 1, 0x000040800000000CUL, 11540, 18, 18, 12),
        (22, 3, 0x0002000000000008UL, 6092, 8, 8, 8),
        (73, 1, 0x000800000C000000UL, 300, 0, 0, 0),
        (75, 1, 0x0004000020400003UL, 3396, 3, 3, 10),
        (28, 1, 0x0000200000000003UL, 1571, 3, 3, 10),
        (69, 7, 0x001000041000000AUL, 33787, 10, 10, 20),
        (10, 2, 0x0200080008090002UL, 5161, 14, 18, 2),
        (63, 1, 0x0004000020C0014DUL, 2060, 13, 13, 31),
        (4, 20, 0x0102000050010006UL, 11422, 7, 12, 6),
        (20, 3, 0x000008002104C045UL, 36703, 14, 17, 15),
        (64, 1, 0x0004000220400006UL, 2350, 6, 6, 12),
        (25, 1, 0x0000200020000004UL, 2097, 4, 4, 4),
        (12, 1, 0x010008001402000DUL, 51669, 15, 21, 20),
        (50, 7, 0x0001080600000006UL, 53995, 6, 6, 18),
        (17, 1, 0x010008002004000EUL, 435590, 104, 134, 14),
        (77, 1, 0x0040000002400008UL, 5856, 8, 8, 8),
        (44, 1, 0x0000880000200007UL, 11466, 14, 14, 14),
        (53, 1, 0x0001089000800007UL, 13074, 11, 11, 29),
        (16, 3, 0x0200080010820002UL, 38983, 12, 26, 10),
        (43, 3, 0x0000880040000017UL, 73496, 7, 7, 15),
        (31, 1, 0x0000400040004207UL, 7330, 7, 7, 20),
        (75, 7, 0x000402082040020CUL, 64211, 48, 48, 28),
        (67, 1, 0x000400002060440EUL, 3071, 28, 28, 16),
        (23, 1, 0x0000082894000006UL, 4993, 6, 6, 24),
        (42, 2, 0x000088800000000EUL, 20637, 21, 21, 14),
        (7, 1, 0x060008080809004DUL, 46336, 14, 21, 34),
        (28, 1, 0x000020000000000FUL, 13597, 15, 15, 22),
        (2, 20, 0x010200000801102BUL, 119524, 13, 14, 21),
        (78, 1, 0x004001020240000EUL, 46952, 14, 14, 28),
        (34, 1, 0x000040000000002AUL, 8449, 10, 10, 11),
        (15, 2, 0x0200088008220845UL, 56239, 26, 46, 15),
        (65, 1, 0x0004000020400009UL, 8713, 9, 9, 16),
        (57, 3, 0x0000014020000044UL, 1865, 10, 10, 5),
        (19, 2, 0x0100080044020007UL, 15751, 9, 13, 14),
        (35, 20, 0x0000410040000007UL, 105659, 7, 7, 14),
        (45, 20, 0x000088000000000EUL, 215908, 14, 14, 14),
        (30, 3, 0x0000200000000006UL, 4470, 6, 6, 6),
        (50, 1, 0x0001090080800046UL, 10873, 6, 6, 21),
        (61, 2, 0x0000000020000006UL, 1526, 7, 12, 6),
        (4, 1, 0x0102000050014007UL, 18078, 8, 13, 16),
        (50, 20, 0x0001080100804026UL, 147428, 6, 6, 23),
        (27, 7, 0x000020444000200DUL, 26973, 13, 13, 35),
        (13, 1, 0x010008300C04012CUL, 88284, 17, 19, 39),
        (44, 1, 0x00008C9000001008UL, 12738, 24, 24, 18),
        (14, 1, 0x01000A020C04011BUL, 219139, 64, 72, 32),
        (34, 3, 0x000040000000000AUL, 10276, 10, 10, 10),
        (56, 1, 0x000002012000880AUL, 2610, 44, 64, 22),
        (49, 1, 0x000108000000008BUL, 18879, 11, 11, 19),
        (8, 3, 0x0100080010090008UL, 19326, 9, 14, 8),
        (8, 20, 0x0100082010091002UL, 20545, 3, 8, 6),
        (11, 1, 0x0100080008020002UL, 2058, 3, 8, 2),
        (53, 1, 0x00010A800000200CUL, 20049, 48, 48, 14),
        (66, 7, 0x0004040920408003UL, 6569, 9, 9, 16),
        (78, 20, 0x004000240240012EUL, 1551785, 14, 14, 45),
        (24, 1, 0x000020006000000FUL, 13309, 15, 15, 22),
        (64, 20, 0x0004000020400088UL, 31723, 8, 8, 9),
        (57, 1, 0x000000002080301AUL, 2723, 11, 16, 23),
        (21, 1, 0x0000080021041081UL, 100474, 17, 33, 11),
        (52, 3, 0x0001090000000002UL, 16155, 2, 2, 2),
        (49, 1, 0x000118000000000AUL, 12494, 5, 5, 10),
        (8, 20, 0x010008011009A002UL, 79568, 3, 8, 6),
        (66, 1, 0x000400002040000DUL, 1389, 13, 13, 20),
        (46, 2, 0x0000884880000002UL, 19414, 2, 2, 6),
        (66, 1, 0x0004000020400000UL, 40, 0, 0, 0),
        (67, 1, 0x0004000020400004UL, 984, 4, 4, 4),
        (1, 7, 0x0000200121494035UL, 774, 6, 11, 21),
        (54, 20, 0x0001080400000420UL, 60000, 0, 0, 1),
        (22, 7, 0x0002100400001003UL, 1884, 2, 2, 15),
        (47, 1, 0x0001082440000045UL, 35618, 5, 5, 23),
        (38, 7, 0x0000400180800022UL, 7853, 2, 2, 15),
        (23, 20, 0x00000C0814000405UL, 27428, 15, 15, 17),
        (6, 1, 0x0100082000210008UL, 59614, 32, 40, 16),
        (7, 2, 0x0600080008092007UL, 79172, 8, 15, 16),
        (5, 2, 0x01000800088140C6UL, 502143, 7, 12, 18),
        (55, 1, 0x0000001020000007UL, 2498, 8, 13, 21),
        (30, 3, 0x000020000000008FUL, 14385, 15, 15, 23),
        (34, 3, 0x000040000000400DUL, 13898, 13, 13, 22),
        (32, 2, 0x000040000080000CUL, 11401, 12, 12, 20),
        (0, 1, 0x000200000C090002UL, 2459, 6, 10, 2),
        (41, 1, 0x0000890A0020000EUL, 16058, 28, 28, 42),
        (13, 3, 0x0100080E0C04020AUL, 47572, 15, 17, 44),
        (28, 1, 0x000020010000001BUL, 8882, 11, 11, 30),
        (1, 2, 0x0000201021490086UL, 341, 7, 12, 13),
        (75, 2, 0x000400282040220AUL, 24141, 10, 10, 36),
        (63, 1, 0x0004140020400003UL, 809, 9, 9, 10),
        (39, 1, 0x0000980040004002UL, 4671, 1, 1, 4),
        (67, 2, 0x000402202040000EUL, 10403, 56, 56, 28),
        (55, 2, 0x0000000020200482UL, 939, 6, 16, 3),
        (5, 7, 0x0100080008014005UL, 9908, 6, 11, 14),
        (2, 2, 0x0102000008018006UL, 10867, 8, 9, 6),
        (3, 1, 0x0080082010020985UL, 71191, 6, 9, 22),
        (0, 1, 0x000200000C090008UL, 17039, 12, 16, 8),
        (80, 20, 0x0000882061000080UL, 200000, 0, 0, 1),
        (76, 7, 0x0040000082400008UL, 16048, 8, 8, 16),
        (21, 3, 0x00000800210409A9UL, 259979, 25, 41, 22),
        (27, 2, 0x000020004000000EUL, 14433, 14, 14, 14),
        (80, 7, 0x00008800E1800008UL, 174089, 8, 8, 24),
        (39, 1, 0x00008C004000844CUL, 35514, 36, 36, 13),
        (72, 1, 0x000800400C000009UL, 15036, 9, 9, 16),
        (61, 1, 0x0000004520000807UL, 3528, 8, 13, 30),
        (23, 1, 0x000008001400A024UL, 4847, 4, 4, 7),
        (57, 3, 0x0000001220000000UL, 225, 1, 6, 0),
        (14, 7, 0x010008860C040100UL, 5057, 8, 11, 2),
        (78, 2, 0x004004220240400DUL, 166570, 39, 39, 48),
        (34, 2, 0x0000400000000001UL, 1224, 1, 1, 8),
        (28, 3, 0x000020100000000EUL, 12956, 14, 14, 28),
        (80, 20, 0x0000880061000004UL, 382820, 4, 4, 4),
        (77, 20, 0x0040000002400002UL, 33734, 2, 2, 2),
        (64, 20, 0x0004000020400004UL, 14933, 4, 4, 4),
        (16, 1, 0x02000A401002200FUL, 1040617, 100, 156, 24),
        (69, 3, 0x0010000090000006UL, 8609, 6, 6, 12),
        (13, 1, 0x010008000C040051UL, 14876, 6, 8, 10),
        (15, 20, 0x0200080508020004UL, 8066, 12, 22, 12),
        (33, 3, 0x0000400100200000UL, 1050, 0, 0, 0),
        (72, 3, 0x000800000C001444UL, 4628, 4, 4, 7),
        (0, 1, 0x000200000C09000FUL, 53167, 19, 23, 22),
        (66, 1, 0x00040040A040000AUL, 1389, 10, 10, 20),
        (61, 20, 0x0000000120000003UL, 27592, 4, 9, 13),
        (41, 1, 0x0000884080004805UL, 8384, 5, 5, 21),
        (67, 1, 0x0004000320400006UL, 3438, 6, 6, 18),
        (52, 20, 0x000108000000000BUL, 186938, 11, 11, 18),
    };

    /// <summary>Values the game gives after its start-up price pass (FUN_00178308): what shops show.</summary>
    private static readonly (int Item, int Count, ulong Flags, int Value)[] ShopValues =
    {
        (0, 1, 0x000200000C090000UL, 521),
        (0, 20, 0x000200000C09040CUL, 52513),
        (1, 1, 0x0000200021490000UL, 2),
        (1, 1, 0x0000200021490005UL, 275),
        (2, 1, 0x0102000008010000UL, 133),
        (2, 3, 0x0102000008010004UL, 5302),
        (3, 1, 0x0080080010020000UL, 112),
        (3, 20, 0x008008C01002000CUL, 108790),
        (4, 1, 0x0102000050010000UL, 205),
        (4, 3, 0x0102000150010402UL, 2987),
        (5, 1, 0x0100080008010000UL, 205),
        (5, 3, 0x0100180008010006UL, 7824),
        (6, 1, 0x0100080000010000UL, 1982),
        (6, 3, 0x0100080000010006UL, 18433),
        (7, 1, 0x0600080008090000UL, 197),
        (7, 20, 0x0600080008094045UL, 15758),
        (8, 1, 0x0100080010090000UL, 205),
        (8, 3, 0x0100080010094002UL, 2430),
        (9, 1, 0x0200080008090000UL, 684),
        (9, 1, 0x0200080408090001UL, 1549),
        (10, 1, 0x0200080008090000UL, 2365),
        (10, 3, 0x020008000809000DUL, 43217),
        (11, 1, 0x0100080008020000UL, 205),
        (11, 1, 0x0100080208020002UL, 2058),
        (12, 1, 0x0100080014020000UL, 433),
        (12, 3, 0x010008001402000EUL, 59525),
        (13, 1, 0x010008000C040000UL, 742),
        (13, 20, 0x010008000C040004UL, 7696),
        (14, 1, 0x010008000C040000UL, 742),
        (14, 20, 0x01000C008C040005UL, 39041),
        (15, 1, 0x0200080008020000UL, 1853),
        (15, 1, 0x0200084008020205UL, 34132),
        (16, 1, 0x0200080010020000UL, 3090),
        (16, 1, 0x0200080010020003UL, 8132),
        (17, 1, 0x0100080020040000UL, 198846),
        (17, 1, 0x010018002084000FUL, 861642),
        (18, 1, 0x0200080004020000UL, 2494),
        (18, 3, 0x020008000402014DUL, 110518),
        (19, 1, 0x0100080044020000UL, 293),
        (19, 1, 0x0100080044024000UL, 551),
        (20, 1, 0x0000080021040041UL, 13216),
        (20, 1, 0x000008002104004DUL, 92472),
        (21, 1, 0x0000080021040001UL, 13418),
        (21, 20, 0x0000080021041007UL, 92486),
        (22, 1, 0x0002000000000000UL, 80),
        (22, 3, 0x000200000000000CUL, 10396),
        (23, 1, 0x0000080014000000UL, 600),
        (23, 1, 0x000008001400000FUL, 15113),
        (24, 1, 0x0000200060000000UL, 5),
        (24, 1, 0x0000200060800084UL, 2099),
        (25, 1, 0x0000200020000000UL, 4),
        (25, 3, 0x0000200020000003UL, 1418),
        (26, 1, 0x0000200020000000UL, 4),
        (26, 3, 0x0000200020000005UL, 2880),
        (27, 1, 0x0000200040000000UL, 400),
        (27, 20, 0x0000200040000004UL, 17319),
        (28, 1, 0x0000200000000000UL, 100),
        (28, 1, 0x000024000000000AUL, 7797),
        (29, 1, 0x0000200000000000UL, 110),
        (29, 3, 0x0000200000000008UL, 6308),
        (30, 1, 0x0000200000000000UL, 120),
        (30, 3, 0x0000200200000045UL, 3593),
        (31, 1, 0x0000400040000000UL, 1200),
        (31, 3, 0x0000500040000020UL, 1800),
        (32, 1, 0x0000400000000000UL, 300),
        (32, 3, 0x0000400000200006UL, 5646),
        (33, 1, 0x0000400000000000UL, 350),
        (33, 3, 0x000040000080000CUL, 12656),
        (34, 1, 0x0000400000000000UL, 350),
        (34, 1, 0x0000400000000002UL, 1335),
        (35, 1, 0x0000400040000000UL, 2200),
        (35, 3, 0x000042004020000BUL, 26444),
        (36, 1, 0x0000400000000000UL, 650),
        (36, 1, 0x0000402000000007UL, 6067),
        (37, 1, 0x0000400000000000UL, 650),
        (37, 20, 0x0000400000000009UL, 39157),
        (38, 1, 0x0000400000000000UL, 650),
        (38, 20, 0x000040000000000AUL, 41459),
        (39, 1, 0x0000880040000000UL, 5000),
        (39, 3, 0x0000880040002007UL, 39035),
        (40, 1, 0x0000880000000000UL, 1450),
        (40, 3, 0x0000880000800002UL, 7542),
        (41, 1, 0x0000880000000000UL, 1350),
        (41, 3, 0x0000884000002002UL, 13362),
        (42, 1, 0x0000880000000000UL, 1450),
        (42, 3, 0x000088000000110FUL, 26481),
        (43, 1, 0x0000880040000000UL, 10000),
        (43, 20, 0x0000884040000009UL, 1011257),
        (44, 1, 0x0000880000000000UL, 3000),
        (44, 20, 0x0000880000200005UL, 125716),
        (45, 1, 0x0000880000000000UL, 3450),
        (45, 3, 0x000088000000000EUL, 42646),
        (46, 1, 0x0000880000000000UL, 3000),
        (46, 3, 0x000088000000000AUL, 31021),
        (47, 1, 0x0001080040000000UL, 16000),
        (47, 3, 0x0001088040008000UL, 72000),
        (48, 1, 0x0001080000000000UL, 3500),
        (48, 20, 0x0001084000000206UL, 308593),
        (49, 1, 0x0001080000000000UL, 3800),
        (49, 20, 0x0001090000800008UL, 188539),
        (50, 1, 0x0001080000000000UL, 3300),
        (50, 20, 0x0001080000008007UL, 234303),
        (51, 1, 0x0001080040000000UL, 25000),
        (51, 1, 0x000108004000000CUL, 79473),
        (52, 1, 0x0001080000000000UL, 3300),
        (52, 20, 0x0001080000000006UL, 147428),
        (53, 1, 0x0001080000000000UL, 3700),
        (53, 20, 0x0001090000000209UL, 192382),
        (54, 1, 0x0001080000000000UL, 3000),
        (54, 3, 0x000108000000000EUL, 38657),
        (55, 1, 0x0000000020000000UL, 38),
        (55, 20, 0x000000042000000DUL, 36532),
        (56, 1, 0x0000000020000000UL, 38),
        (56, 20, 0x0000000020000009UL, 17176),
        (57, 1, 0x0000000020000000UL, 38),
        (57, 3, 0x0000000020000000UL, 113),
        (58, 1, 0x0000000020000000UL, 38),
        (58, 3, 0x0000000020800022UL, 1981),
        (59, 1, 0x0000000020000000UL, 25),
        (59, 20, 0x000000A02000000BUL, 21327),
        (60, 1, 0x0000000020000000UL, 32),
        (60, 1, 0x0000000020000009UL, 961),
        (61, 1, 0x0000000020000000UL, 38),
        (61, 1, 0x000004002000000CUL, 880),
        (62, 1, 0x0000000020000000UL, 19),
        (62, 1, 0x0000000020004008UL, 516),
        (63, 1, 0x0004000020400000UL, 20),
        (63, 20, 0x0004008020400004UL, 2599),
        (64, 1, 0x0004000020400000UL, 63),
        (64, 20, 0x000400042040000CUL, 43367),
        (65, 1, 0x0004000020400000UL, 188),
        (65, 1, 0x0004000020401005UL, 3937),
        (66, 1, 0x0004000020400000UL, 20),
        (66, 20, 0x0004000820400008UL, 9277),
        (67, 1, 0x0004000020400000UL, 63),
        (67, 3, 0x000400002040000CUL, 3400),
        (68, 1, 0x0004000020400000UL, 188),
        (68, 20, 0x000400002040024FUL, 146773),
        (69, 1, 0x0010000010000000UL, 315),
        (69, 1, 0x0010000210000007UL, 17286),
        (70, 1, 0x0010000010000000UL, 315),
        (70, 3, 0x0010040010000003UL, 3325),
        (71, 1, 0x0010000010000000UL, 315),
        (71, 20, 0x0010002010000002UL, 3650),
        (72, 1, 0x000800000C000000UL, 300),
        (72, 3, 0x000800440C000000UL, 546),
        (73, 1, 0x000800000C000000UL, 300),
        (73, 3, 0x000800000C804000UL, 300),
        (74, 1, 0x000800000C000000UL, 300),
        (74, 20, 0x000800000C000005UL, 6309),
        (75, 1, 0x0004000020400000UL, 113),
        (75, 1, 0x0004000020400021UL, 1666),
        (76, 1, 0x0040000002400000UL, 50),
        (76, 3, 0x004000040240080AUL, 4971),
        (77, 1, 0x0040000002400000UL, 250),
        (77, 20, 0x0040000002400025UL, 91390),
        (78, 1, 0x0040000002400000UL, 575),
        (78, 1, 0x004000000240000DUL, 16622),
        (79, 1, 0x0040000002400000UL, 1550),
        (79, 3, 0x0040000002400203UL, 91694),
        (80, 1, 0x0000880061000000UL, 10000),
        (80, 20, 0x0000880061008048UL, 730738),
    };

    [Test]
    public void ShopValues_MatchTheGameAfterItsStartUpPricePass()
    {
        var items = Stock().GetItems();
        var misses = 0;
        foreach (var (item, count, flags, value) in ShopValues)
        {
            var v = ItemFormulas.ShopValue(items[item], count, flags);
            Assert.That(Math.Abs(v - value), Is.LessThanOrEqualTo(Math.Max(1, value / 100000)), $"{items[item].Name} x{count} 0x{flags:X}");
            if (v != value) misses++;
        }

        Assert.That(misses, Is.LessThanOrEqualTo(2));
        Assert.That(ItemFormulas.ShopValue(items[64], 1, items[64].Flags), Is.EqualTo(63), "a healing potion priced 250");
        Assert.That(ItemFormulas.BuyPrice(63, 0.1f), Is.EqualTo(56));
        Assert.That(ItemFormulas.SellPrice(63), Is.EqualTo(28));
    }

    [Test]
    public void Formulas_MatchTheGamesOwnCode()
    {
        var items = Stock().GetItems();
        foreach (var (item, count, flags, value) in PlainValues)
        {
            Assert.That(ItemFormulas.Value(items[item], count, flags), Is.EqualTo(value), items[item].Name);
        }

        var valueMisses = 0;
        foreach (var (item, count, flags, value, min, max, power) in Vectors)
        {
            Assert.That(ItemFormulas.Damage(items[item], flags), Is.EqualTo((min, max)), $"{items[item].Name} 0x{flags:X}");
            Assert.That(ItemFormulas.Power(flags), Is.EqualTo(power), $"0x{flags:X}");
            var v = ItemFormulas.Value(items[item], count, flags);
            Assert.That(Math.Abs(v - value), Is.LessThanOrEqualTo(Math.Max(1, value / 100000)), $"{items[item].Name} x{count} 0x{flags:X}");
            if (v != value) valueMisses++;
        }

        Assert.That(valueMisses, Is.LessThanOrEqualTo(4), "values off by one (the game's own powf rounds differently)");
    }
}
