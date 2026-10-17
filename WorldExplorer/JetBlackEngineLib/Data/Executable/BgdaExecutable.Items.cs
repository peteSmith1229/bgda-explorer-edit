// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.Items.cs
//
// The item tables of SLES_506.72 (PAL), read and written in place (BgdaItems.cs has the
// types, the property bits and the formulas):
//   item table      VA 0x002A8098 (file 0x1A9098), 81 x 0x30 bytes, then a zero record
//   model table     VA 0x002A79C8 (file 0x1A89C8), 108 x {vif name, tex name, 0, 0}, then zeros;
//                   the last two words are filled in by the game with the loaded model
//   stance weights  VA 0x002A76E8, 6 x f32: how a weapon's stance weighs in its value
//   loot modifiers  VA 0x002A7700 .. 0x002A79B0: five lists of 64-bit values ended by -1,
//                   one of which random loot ORs into an item (FUN_001743A0):
//                     weapons 0x2A7700 (Extreme 0x2A77D0), armour 0x2A78C8 (Extreme 0x2A78F8),
//                     rings and amulets 0x2A7978
//   enchant glow    VA 0x002A79B8: two {R, G, B, alpha, f32 size} layers drawn round a weapon
//                   with +N or a modifier (not a flaming one) (FUN_00174A50 sets alpha and size)
//   materials       the lui immediates of FUN_00178818 (0x00178838 rusty, 0x00178854 fine,
//                   0x00178868 masterwork, 0x00178884 mithril, 0x001788A0 adamantine)
//
// None of these are touched by the three-player patch (checked against ADRIANNA22 to 26).
// The eight spell-scroll records the game never uses (55-62) can become new items.
//
// ADDING ITEMS. The game finds the item table through ten instructions (the parser
// FUN_00174B48, random loot FUN_001743A0, the start-up price pass FUN_00178308 and the
// save code FUN_00178370 / 00178408 / 001784A0 / 001784C0), and every loop over it
// stops at the first record whose name is zero: there is no item count to change. A save
// stores the items of players 1 and 2 and the items on the ground by their index, and the
// three-player patch saves player 3 with pointers into the table, so the table stays where
// it is and grows at its end. The stock game keeps the HUD's panel data right after it
// (VA 0x002A8FF8); the three-player patch moves that data to 0x000D8FF8 (ten lui 0x002B ->
// 0x000E), which frees VA 0x002A8FC8-0x002AA700 (the three-player menu text starts at
// 0x002AA700). Added records go there, then a zero record, then their names, 8-aligned like
// the game's own. ItemRoomProblem checks that the room is freed, empty and referred to by
// nothing (none of the tested three-player builds refers to it).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The five lists random loot takes a modifier from.</summary>
public enum LootModifierList
{
    Weapons,
    WeaponsExtreme,
    Armour,
    ArmourExtreme,
    RingsAndAmulets
}

/// <summary>The glow drawn round a weapon with +N or a modifier: a large faint halo and a smaller core.</summary>
public readonly record struct EnchantGlow(Rgb Halo, Rgb Core);

/// <summary>Changes to the item tables, applied together by <see cref="BgdaExecutable.ApplyItemEdits"/>.</summary>
public sealed class ItemEdits
{
    /// <summary>Item records to write (only those that changed are needed).</summary>
    public List<GameItem> Items { get; } = new();

    /// <summary>
    /// Every item after the game's own 81, in order, when they change (added, removed, edited); null leaves
    /// them as they are. Written before <see cref="Items"/> and the shops, so a shop can sell one.
    /// </summary>
    public List<GameItem>? AddedItems { get; set; }

    /// <summary>New files of models, by model number.</summary>
    public Dictionary<int, (string Vif, string Tex)> Models { get; } = new();

    /// <summary>New item text of shop entries, by entry number.</summary>
    public Dictionary<int, string> ShopTexts { get; } = new();

    public float[]? MaterialMultipliers { get; set; }
    public float[]? StanceWeights { get; set; }
    public EnchantGlow? Glow { get; set; }
    public Dictionary<LootModifierList, ulong[]> Loot { get; } = new();

    public bool IsEmpty => Items.Count == 0 && AddedItems == null && Models.Count == 0 && ShopTexts.Count == 0 &&
                           MaterialMultipliers == null && StanceWeights == null && Glow == null && Loot.Count == 0;
}

public partial class BgdaExecutable
{
    private const int VaToFile = 0xFF000;

    public const int ItemTableVa = 0x002A8098;
    public const int ItemRecordSize = 0x30;

    /// <summary>The records of the game's own table; any more are added items.</summary>
    public const int StockItemCount = 81;

    /// <summary>Where added items go: the room the three-player patch frees after the game's records.</summary>
    public const int AddedItemsVa = ItemTableVa + StockItemCount * ItemRecordSize;

    /// <summary>The end of that room: the three-player menu text starts here.</summary>
    public const int AddedItemsEndVa = 0x002AA700;

    /// <summary>The longest name an added item can have (the longest the game's naming code is known to handle).</summary>
    public const int MaxAddedItemName = 31;

    /// <summary>The ten instructions that point at the HUD's panel data: lui 0x002B in the stock game, 0x000E once moved.</summary>
    private static readonly int[] HudPanelLuiVas =
        { 0x0016F4C4, 0x00175C94, 0x00175CC0, 0x00178D74, 0x00178E44, 0x00179214, 0x0017B094, 0x0017B150, 0x0017B154, 0x0017B160 };

    private const uint HudPanelStockHigh = 0x002B, HudPanelMovedHigh = 0x000E;

    public const int ModelTableVa = 0x002A79C8;
    public const int ModelRecordSize = 0x10;
    public const int ModelCount = 108;
    public const int ArmorYakEntryCount = 59;

    private const int StanceWeightsVa = 0x002A76E8;
    private const int GlowVa = 0x002A79B8;

    /// <summary>The spell-scroll records (burninghands ... magicmissile): never used by the game.</summary>
    public const int FirstSpareItem = 55, LastSpareItem = 62;

    private static readonly (LootModifierList List, int Va, int Count)[] LootLists =
    {
        (LootModifierList.Weapons, 0x002A7700, 25),
        (LootModifierList.WeaponsExtreme, 0x002A77D0, 30),
        (LootModifierList.Armour, 0x002A78C8, 5),
        (LootModifierList.ArmourExtreme, 0x002A78F8, 15),
        (LootModifierList.RingsAndAmulets, 0x002A7978, 7)
    };

    private static readonly int[] MaterialLuiVas = { 0x00178838, 0x00178854, 0x00178868, 0x00178884, 0x001788A0 };

    // field offsets of an item record
    private const int ItName = 0x00, ItWeight = 0x04, ItStack = 0x08, ItMin = 0x0A, ItMax = 0x0C, ItPrice = 0x10,
        ItArmour = 0x14, ItSlot = 0x18, ItCategory = 0x19, ItStance = 0x1A, ItDamageType = 0x1C, ItFlags = 0x20,
        ItModel = 0x28, ItPickup = 0x2A;

    public const float MaxItemWeight = 1000f;
    public const int MaxItemStack = 999, MaxItemDamage = 9999, MaxItemPrice = 9_999_999, MaxItemArmour = 9999;

    private static int FileOffset(int va) => va - VaToFile;

    // ═══════════════════════════════ checks ═══════════════════════════════

    /// <summary>
    /// Whether the item tables are where this build keeps them: 81 named records (and any added after them)
    /// and a zero record, 108 models with .vif and .tex names and a zero record, the parser's reference to the
    /// item table, the five loot lists ended by -1 and the five material instructions.
    /// </summary>
    public bool ItemsSupported => ItemsProblem() == null;

    /// <summary>How many records the item table has: the game's 81 and any added after them.</summary>
    public int ItemCount => CountItems();

    /// <summary>How many items were added after the game's 81.</summary>
    public int AddedItemCount => Math.Max(0, CountItems() - StockItemCount);

    /// <summary>The records before the first one whose name is zero (as the game counts them).</summary>
    private int CountItems()
    {
        var table = FileOffset(ItemTableVa);
        var max = (AddedItemsEndVa - ItemTableVa) / ItemRecordSize;
        var n = 0;
        while (n < max && table + (n + 1) * ItemRecordSize <= _data.Length && ReadU32(table + n * ItemRecordSize) != 0) n++;
        return n;
    }

    /// <summary>Why the item tables can't be edited, or null when they can.</summary>
    public string? ItemsProblem()
    {
        var table = FileOffset(ItemTableVa);
        if (table + (StockItemCount + 1) * ItemRecordSize > _data.Length) return "The file is too short.";
        var records = CountItems();
        for (var i = 0; i < StockItemCount; i++)
        {
            if (StringAt(ReadU32(table + i * ItemRecordSize)) is not { Length: > 0 })
                return $"Item {i} has no name where this build keeps it.";
        }

        if (records > StockItemCount)
        {
            var added = records - StockItemCount;
            if (ItemTableVa + (records + 1) * ItemRecordSize > AddedItemsEndVa)
                return "The added items don't end where WorldExplorer ends them.";
            var namesVa = ItemTableVa + (records + 1) * ItemRecordSize;
            for (var i = StockItemCount; i < records; i++)
            {
                var nameVa = ReadU32(table + i * ItemRecordSize);
                if (nameVa < namesVa || nameVa >= AddedItemsEndVa || StringAt(nameVa) is not { Length: > 0 and <= MaxAddedItemName } name ||
                    !IsItemName(name))
                    return $"Added item {i} has no name where WorldExplorer puts them.";
            }

            if (RoomReason(out _) is { } reason)
                return $"The item table has {added} item{(added == 1 ? "" : "s")} after the game's {StockItemCount}, but the space " +
                       $"after the table can't hold them ({reason}).";
        }

        if (_data.Skip(table + records * ItemRecordSize).Take(ItemRecordSize).Any(b => b != 0))
            return "The item table doesn't end where this build ends it.";

        var models = FileOffset(ModelTableVa);
        for (var m = 0; m < ModelCount; m++)
        {
            var vif = StringAt(ReadU32(models + m * ModelRecordSize));
            var tex = StringAt(ReadU32(models + m * ModelRecordSize + 4));
            if (vif == null || tex == null || !vif.EndsWith(".vif", StringComparison.Ordinal) ||
                !tex.EndsWith(".tex", StringComparison.Ordinal))
                return $"Model {m} isn't a .vif and .tex pair.";
        }

        if (ReadU32(models + ModelCount * ModelRecordSize) != 0)
            return "The model table doesn't end where this build ends it.";

        // the parser FUN_00174B48: lui s6, 0x002B / addiu s4, s6, -32616 (0x002A8098)
        if (ReadU32(FileOffset(0x00174BA8)) != 0x3C16002B || ReadU32(FileOffset(0x00174BB0)) != 0x26D48098)
            return "The item code isn't the code of this build.";

        foreach (var (list, va, count) in LootLists)
        {
            if (ReadU64(FileOffset(va) + count * 8) != ulong.MaxValue)
                return $"The {list} loot list doesn't end where this build ends it.";
        }

        if (MaterialLuiVas.Any(va => (ReadU32(FileOffset(va)) & 0xFFFF0000) != 0x3C010000))
            return "The material multipliers aren't the instructions of this build.";

        return null;
    }

    private void RequireItems()
    {
        if (ItemsProblem() is { } problem) throw new InvalidOperationException(problem);
    }

    // ═══════════════════════════════ all at once ═══════════════════════════════

    /// <summary>
    /// Applies item edits together: the added items and then the other items first (so shops can sell a new
    /// or renamed record), then models, shops, materials, stance weights, the glow and the loot lists. When
    /// any of them is refused nothing changes, and the exception (<see cref="ArgumentException"/> for a bad
    /// value, <see cref="InvalidOperationException"/> when there is no room for added items) says which.
    /// </summary>
    public void ApplyItemEdits(ItemEdits edits)
    {
        RequireItems();
        if (edits.IsEmpty) return;
        var before = (byte[])_data.Clone();
        try
        {
            if (edits.AddedItems != null) SetAddedItems(edits.AddedItems);
            foreach (var item in edits.Items) SetItem(item);
            foreach (var (index, (vif, tex)) in edits.Models) SetItemModel(index, vif, tex);
            if (edits.ShopTexts.Count > 0) SetShopTexts(edits.ShopTexts);
            if (edits.MaterialMultipliers != null) SetMaterialMultipliers(edits.MaterialMultipliers);
            if (edits.StanceWeights != null) SetStanceWeights(edits.StanceWeights);
            if (edits.Glow is { } glow) SetEnchantGlow(glow);
            foreach (var (list, values) in edits.Loot) SetLootModifiers(list, values);
            CheckShopsSellItems();
        }
        catch
        {
            _data = before;
            ForgetReferences();
            throw;
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when a shop sells an item that no longer exists or is now of
    /// another kind than its list (an item renamed, removed or changed): the game would sell a shortbow.
    /// </summary>
    private void CheckShopsSellItems()
    {
        if (ShopsProblem() != null) return;
        var items = GetItems();
        foreach (var e in GetShopEntries())
        {
            var parsed = ItemText.Parse(e.Text, items);
            var where = $"{ShopStockName(e.Stock)}, {e.List} item {e.Number}";
            if (parsed.ItemIndex < 0)
                throw new ArgumentException($"{where} sells \"{e.Text}\", but no item is called \"{e.Text.Split('+')[0]}\" any more.");
            if (items[parsed.ItemIndex].Category != e.List)
                throw new ArgumentException($"{where} sells \"{e.Text}\", which isn't a {e.List.ToString().ToLowerInvariant()} any more.");
        }
    }

    // ═══════════════════════════════ items ═══════════════════════════════

    /// <summary>Every item record, as it is in the file: the game's 81, then any added after them.</summary>
    public List<GameItem> GetItems()
    {
        RequireItems();
        var count = CountItems();
        var list = new List<GameItem>(count);
        for (var i = 0; i < count; i++)
        {
            var r = FileOffset(ItemTableVa) + i * ItemRecordSize;
            var nameVa = (int)ReadU32(r + ItName);
            var spare = i >= FirstSpareItem && i <= LastSpareItem;
            var added = i >= StockItemCount;
            list.Add(new GameItem
            {
                Index = i,
                Name = StringAt((uint)nameVa) ?? "",
                IsSpare = spare,
                IsAdded = added,
                NameCapacity = added ? MaxAddedItemName : spare && CountReferences(nameVa) == 1 ? SlotCapacity(nameVa) : 0,
                Weight = BitConverter.ToSingle(_data, r + ItWeight),
                StackSize = ReadU16(r + ItStack),
                DamageMin = (short)ReadU16(r + ItMin),
                DamageMax = (short)ReadU16(r + ItMax),
                Price = ReadInt(r + ItPrice),
                Armour = (short)ReadU16(r + ItArmour),
                Slot = (ItemSlot)(sbyte)_data[r + ItSlot],
                Category = (ItemCategory)(sbyte)_data[r + ItCategory],
                Stance = (ItemStance)(sbyte)_data[r + ItStance],
                DamageType = ReadInt(r + ItDamageType),
                Flags = ReadU64(r + ItFlags),
                Model = (short)ReadU16(r + ItModel),
                PickupModel = (short)ReadU16(r + ItPickup)
            });
        }

        return list;
    }

    /// <summary>
    /// Writes one item record (and renames a spare or added record). Throws <see cref="ArgumentException"/>
    /// with a message naming the item when a value is out of range or doesn't fit the record.
    /// </summary>
    public void SetItem(GameItem item)
    {
        RequireItems();
        var count = CountItems();
        if (item.Index < 0 || item.Index >= count) throw new ArgumentOutOfRangeException(nameof(item));
        if (item.Index >= StockItemCount)
        {
            var added = GetAddedItems();
            added[item.Index - StockItemCount] = item;
            SetAddedItems(added);
            return;
        }

        var current = GetItems();
        var old = current[item.Index];
        var problem = CheckItem(item, old, current);
        if (problem != null) throw new ArgumentException($"{old.Name}: {problem}");

        var r = FileOffset(ItemTableVa) + item.Index * ItemRecordSize;
        if (item.Name != old.Name)
        {
            var nameAt = FileOffset((int)ReadU32(r + ItName));
            Array.Clear(_data, nameAt, old.Name.Length);
            Encoding.ASCII.GetBytes(item.Name).CopyTo(_data, nameAt);
        }

        WriteItemFields(r, item);
    }

    /// <summary>Writes everything of a record but its name pointer.</summary>
    private void WriteItemFields(int r, GameItem item)
    {
        BitConverter.GetBytes(item.Weight).CopyTo(_data, r + ItWeight);
        WriteU16(r + ItStack, item.StackSize);
        WriteU16(r + ItMin, item.DamageMin);
        WriteU16(r + ItMax, item.DamageMax);
        WriteInt(r + ItPrice, item.Price);
        WriteU16(r + ItArmour, item.Armour);
        _data[r + ItSlot] = (byte)(sbyte)item.Slot;
        _data[r + ItCategory] = (byte)(sbyte)item.Category;
        _data[r + ItStance] = (byte)(sbyte)item.Stance;
        WriteInt(r + ItDamageType, item.DamageType);
        BitConverter.GetBytes(item.Flags).CopyTo(_data, r + ItFlags);
        WriteU16(r + ItModel, item.Model);
        WriteU16(r + ItPickup, item.PickupModel);
    }

    /// <summary>Why <paramref name="item"/> can't replace <paramref name="old"/>, or null when it can.</summary>
    public static string? CheckItem(GameItem item, GameItem old, IReadOnlyList<GameItem> all)
    {
        if (!float.IsFinite(item.Weight) || item.Weight < 0f || item.Weight > MaxItemWeight)
            return $"Weight must be 0 to {MaxItemWeight:0}.";
        if (item.StackSize < 1 || item.StackSize > MaxItemStack) return $"Stack size must be 1 to {MaxItemStack}.";
        if (item.DamageMin < 0 || item.DamageMax > MaxItemDamage || item.DamageMax < 0 || item.DamageMin > MaxItemDamage)
            return $"Damage must be 0 to {MaxItemDamage}.";
        if (item.DamageMin > item.DamageMax) return "The minimum damage is more than the maximum.";
        if (item.Price < 0 || item.Price > MaxItemPrice) return $"Price must be 0 to {MaxItemPrice:N0}.";
        if (item.Armour < 0 || item.Armour > MaxItemArmour) return $"Armour must be 0 to {MaxItemArmour}.";
        if (item.DamageType < 0) return "The damage type can't be negative.";

        // the unused scroll records and added items can become any item; an added item's name is always checked
        var open = old.IsSpare || old.IsAdded;
        if (item.Name != old.Name || old.IsAdded)
        {
            if (!old.IsAdded && (!old.IsSpare || old.NameCapacity == 0))
                return "Only the unused scroll records and added items can be renamed.";
            var capacity = old.IsAdded ? MaxAddedItemName : old.NameCapacity;
            if (item.Name.Length == 0 || item.Name.Length > capacity)
                return $"The name must be 1 to {capacity} characters.";
            if (!IsItemName(item.Name))
                return "Use letters, digits and _ in the name.";
            if (all.Any(o => o.Index != item.Index && o.Name == item.Name))
                return $"Another item is already called \"{item.Name}\".";
        }

        if (!open && item.Category != old.Category)
            return "Only the unused scroll records and added items can change between weapon, armour and other.";
        var slots = item.Category switch
        {
            ItemCategory.Weapon => new[] { ItemSlot.OneHanded, ItemSlot.TwoHanded, ItemSlot.Ammunition },
            ItemCategory.Armour => new[] { ItemSlot.Shield, ItemSlot.Body, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Helmet },
            ItemCategory.Other => new[] { ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Usable },
            _ => Array.Empty<ItemSlot>()
        };
        if (!slots.Contains(item.Slot)) return $"A {item.Category.ToString().ToLowerInvariant()} can't go in the {item.Slot} slot.";
        if (!open && item.Category != ItemCategory.Weapon && item.Slot != old.Slot)
            return "Only weapons, the unused scroll records and added items can change slot.";
        if (item.Category == ItemCategory.Weapon)
        {
            if (item.Stance < ItemStance.OneHanded || item.Stance > ItemStance.Ammunition) return "Unknown stance.";
            if (item.DamageType == 0) return "A weapon needs a damage type: every weapon of the game is blunt, ranged or bladed.";
            if ((item.Slot == ItemSlot.Ammunition) != (item.Stance == ItemStance.Ammunition))
                return "Ammunition goes in the ammunition slot with the ammunition stance.";
            var bowName = item.Name.Contains("bow") || item.Name.Contains("Bow");
            if (item.Stance == ItemStance.Bow && !bowName)
                return "A bow's name must contain \"bow\": the game picks the ranged weapon set by name.";
            if (item.Stance != ItemStance.Bow && bowName)
                return "Only bows can have \"bow\" in the name: the game would treat it as a bow.";
        }
        else if (item.Stance != ItemStance.OneHanded)
        {
            return "Only weapons have a stance.";
        }

        var looks = ItemBits.Looks(item.Flags);
        if (item.WornFromArmorYak)
        {
            if (item.Model < 0 || item.Model >= ArmorYakEntryCount)
                return $"Worn armour uses ARMOR.YAK entries 0 to {ArmorYakEntryCount - 1}.";
        }
        else if (item.Model < 0 || item.Model + looks - 1 >= ModelCount)
        {
            return $"The model and its {looks} look(s) must be models 0 to {ModelCount - 1}.";
        }

        var pickupLooks = item.PickupModel == item.Model && !item.WornFromArmorYak ? looks : 1;
        if (item.PickupModel < 0 || item.PickupModel + pickupLooks - 1 >= ModelCount)
            return $"The pickup model must be model 0 to {ModelCount - 1}.";
        return null;
    }

    /// <summary>Letters, digits and _ only: what an item's name can be made of.</summary>
    private static bool IsItemName(string name) =>
        name.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');

    // ═══════════════════════════════ added items ═══════════════════════════════

    /// <summary>The bytes after the game's records that added items can use (5,944).</summary>
    public const int AddedItemsRoom = AddedItemsEndVa - AddedItemsVa;

    /// <summary>
    /// Why items can't be added after the game's 81, or null when they can: the room after the table must have
    /// been freed by the three-player patch (the stock game keeps the HUD's data there), nothing in the file may
    /// refer to it, and it must hold nothing but the items added before.
    /// </summary>
    public string? ItemRoomProblem()
    {
        var reason = RoomReason(out var stock);
        if (reason == null) return null;
        return stock
            ? "There is no room for more items in the stock executable: the game keeps the HUD's data right after the item table. " +
              "Turn on three players (Three Players tab) and save: the three-player patch moves that data and frees the room."
            : $"There is no room for more items: the space after the item table can't be used ({reason}).";
    }

    /// <summary>Why the room after the item table can't hold added items, in a few words, or null.</summary>
    private string? RoomReason(out bool stock)
    {
        stock = false;
        if (_data.Length < FileOffset(AddedItemsEndVa)) return "the file is too short";
        foreach (var va in HudPanelLuiVas)
        {
            var w = ReadU32(FileOffset(va));
            if (w >> 26 == 0x0F && (w & 0xFFFF) == HudPanelStockHigh)
            {
                stock = true;
                return "the game keeps the HUD's data there";
            }

            if (w >> 26 != 0x0F || (w & 0xFFFF) != HudPanelMovedHigh)
                return $"the HUD instruction at 0x{va:X8} isn't the one the three-player patch writes";
        }

        if (RoomReferences() is { Count: > 0 } refs) return $"something at 0x{refs[0]:X8} refers to it";
        for (var va = AddedItemsUsedEnd(); va < AddedItemsEndVa; va++)
        {
            if (_data[FileOffset(va)] != 0) return $"it isn't empty at 0x{va:X8}";
        }

        return null;
    }

    /// <summary>The bytes added items with these names take: a record each, the zero record after them and the names, 8-aligned.</summary>
    public static int AddedItemsSize(IEnumerable<string> names)
    {
        var records = 1;
        var text = 0;
        foreach (var name in names)
        {
            records++;
            text += Align8(name.Length + 1);
        }

        return records * ItemRecordSize + text;
    }

    private static int Align8(int n) => (n + 7) & ~7;

    /// <summary>The items after the game's 81 (empty when there are none).</summary>
    public List<GameItem> GetAddedItems() => GetItems().Skip(StockItemCount).ToList();

    /// <summary>
    /// Replaces the items after the game's 81 with <paramref name="items"/>, in order (their Index is ignored:
    /// the first becomes item 81). Each is checked like a renamed record (a name of 1 to 31 letters, digits and
    /// _ that no other item has), and together they must fit in <see cref="AddedItemsRoom"/>. An empty list
    /// removes the added items. Throws <see cref="InvalidOperationException"/> when the executable has no room
    /// and <see cref="ArgumentException"/> naming the item for a bad value; nothing changes then.
    /// </summary>
    public void SetAddedItems(IReadOnlyList<GameItem> items)
    {
        RequireItems();
        var current = GetItems();
        if (items.Count == 0 && current.Count == StockItemCount) return;
        if (items.Count > 0 && ItemRoomProblem() is { } room) throw new InvalidOperationException(room);

        var all = current.Take(StockItemCount).ToList();
        all.AddRange(items.Select((item, k) => item.WithIndex(StockItemCount + k)));
        for (var i = StockItemCount; i < all.Count; i++)
        {
            var item = all[i];
            var blank = new GameItem { Index = i, Name = item.Name, IsAdded = true, NameCapacity = MaxAddedItemName };
            if (CheckItem(item, blank, all) is { } problem) throw new ArgumentException($"Added item {i} ({item.Name}): {problem}");
        }

        var size = AddedItemsSize(items.Select(i => i.Name));
        if (size > AddedItemsRoom)
        {
            throw new ArgumentException($"The added items don't fit: they take {size:N0} bytes and there are {AddedItemsRoom:N0} after the " +
                                        $"item table ({ItemRecordSize} for each item and its name rounded up to 8, and {ItemRecordSize} to end the table).");
        }

        // the records, the zero record that ends the table, then the names
        Array.Clear(_data, FileOffset(AddedItemsVa), AddedItemsRoom);
        var nameVa = AddedItemsVa + (items.Count + 1) * ItemRecordSize;
        for (var i = StockItemCount; i < all.Count; i++)
        {
            var item = all[i];
            var r = FileOffset(ItemTableVa) + i * ItemRecordSize;
            WriteInt(r + ItName, nameVa);
            WriteItemFields(r, item);
            Encoding.ASCII.GetBytes(item.Name).CopyTo(_data, FileOffset(nameVa));
            nameVa += Align8(item.Name.Length + 1);
        }

        ForgetReferences();
    }

    /// <summary>Adds an item after the last one and returns its index (see <see cref="SetAddedItems"/>).</summary>
    public int AddItem(GameItem item)
    {
        var added = GetAddedItems();
        added.Add(item);
        SetAddedItems(added);
        return StockItemCount + added.Count - 1;
    }

    /// <summary>
    /// Removes an added item; the ones after it move down a place. A save keeps items by their place, so a save
    /// that holds this item or one after it gets another item (or none) instead.
    /// </summary>
    public void RemoveAddedItem(int index)
    {
        var added = GetAddedItems();
        if (index < StockItemCount || index >= StockItemCount + added.Count) throw new ArgumentOutOfRangeException(nameof(index));
        added.RemoveAt(index - StockItemCount);
        SetAddedItems(added);
    }

    /// <summary>Where the records, the zero record and the names of the added items end (at least the zero record at 81).</summary>
    private int AddedItemsUsedEnd()
    {
        var count = CountItems();
        var end = ItemTableVa + (count + 1) * ItemRecordSize;
        var stop = FileOffset(AddedItemsEndVa);
        for (var i = StockItemCount; i < count; i++)
        {
            var nameVa = (int)ReadU32(FileOffset(ItemTableVa) + i * ItemRecordSize);
            if (nameVa < end || nameVa >= AddedItemsEndVa) continue;
            var at = FileOffset(nameVa);
            while (at < stop && _data[at] != 0) at++;
            end = Math.Max(end, Math.Min(at + 1, stop) + VaToFile);
        }

        return Math.Min(end, AddedItemsEndVa);
    }

    private List<int>? _roomReferences;

    /// <summary>
    /// The places outside the room after the item table that refer to it: lui-formed addresses anywhere in the
    /// file (every word is read as an instruction, so the code the three-player patch adds is included) and
    /// aligned words that hold an address in it. The stock game's ten HUD instructions refer to it; nothing
    /// in any tested three-player build does.
    /// </summary>
    private List<int> RoomReferences()
    {
        CheckReferenceCaches();
        if (_roomReferences != null) return _roomReferences;
        var list = new List<int>();
        var lastLui = new int[32];
        var luiAt = new int[32];
        Array.Fill(luiAt, int.MinValue / 2);
        var roomStart = FileOffset(AddedItemsVa);
        var roomEnd = FileOffset(AddedItemsEndVa);
        for (var at = 0x1000; at + 4 <= _data.Length; at += 4)
        {
            if (at >= roomStart && at < roomEnd) continue;
            var w = ReadU32(at);
            if (w is >= AddedItemsVa and < AddedItemsEndVa)
            {
                list.Add(at + VaToFile);
                continue;
            }

            var op = (int)(w >> 26);
            var rs = (int)((w >> 21) & 31);
            if (op == 0x0F)
            {
                var rt = (int)((w >> 16) & 31);
                lastLui[rt] = (int)(w & 0xFFFF) << 16;
                luiAt[rt] = at;
                continue;
            }

            if (op is not (0x09 or 0x19 or 0x1E or 0x1F or 0x20 or 0x21 or 0x23 or 0x24 or 0x25 or 0x27 or 0x28 or 0x29 or 0x2B
                or 0x31 or 0x37 or 0x39 or 0x3F)) continue;
            if (at - luiAt[rs] > LuiWindow * 4) continue;
            var target = lastLui[rs] + (short)(w & 0xFFFF);
            if (target is >= AddedItemsVa and < AddedItemsEndVa) list.Add(at + VaToFile);
        }

        return _roomReferences = list;
    }

    // ═══════════════════════════════ models ═══════════════════════════════

    public List<GameItemModel> GetItemModels()
    {
        RequireItems();
        var list = new List<GameItemModel>(ModelCount);
        for (var m = 0; m < ModelCount; m++)
        {
            var r = FileOffset(ModelTableVa) + m * ModelRecordSize;
            var vifVa = (int)ReadU32(r);
            var texVa = (int)ReadU32(r + 4);
            list.Add(new GameItemModel
            {
                Index = m,
                Vif = StringAt((uint)vifVa) ?? "",
                Tex = StringAt((uint)texVa) ?? "",
                VifCapacity = CountReferences(vifVa) == 1 ? SlotCapacity(vifVa) : 0,
                TexCapacity = CountReferences(texVa) == 1 ? SlotCapacity(texVa) : 0
            });
        }

        return list;
    }

    /// <summary>
    /// Sets the files of a model. A name another model already has is shared with it; a new name is
    /// written in place of this model's own name when it fits and no other model shares that name. Both
    /// names are checked before either is written.
    /// </summary>
    public void SetItemModel(int index, string vif, string tex)
    {
        RequireItems();
        if (index < 0 || index >= ModelCount) throw new ArgumentOutOfRangeException(nameof(index));
        CheckFileName(vif, ".vif");
        CheckFileName(tex, ".tex");
        var r = FileOffset(ModelTableVa) + index * ModelRecordSize;
        var vifPlan = PlanModelName(r, vif, index, "model");
        var texPlan = PlanModelName(r + 4, tex, index, "texture");
        if (vifPlan.SameSlot && texPlan.SameSlot && vifPlan.InPlace && texPlan.InPlace)
            throw new InvalidOperationException("The model and the texture share one name.");
        ApplyModelName(r, vif, vifPlan);
        ApplyModelName(r + 4, tex, texPlan);
        ForgetReferences();
    }

    /// <summary>Why <paramref name="vif"/> and <paramref name="tex"/> can't be model <paramref name="index"/>'s files, or null.</summary>
    public string? CheckItemModel(int index, string vif, string tex)
    {
        try
        {
            RequireItems();
            CheckFileName(vif, ".vif");
            CheckFileName(tex, ".tex");
            var r = FileOffset(ModelTableVa) + index * ModelRecordSize;
            PlanModelName(r, vif, index, "model");
            PlanModelName(r + 4, tex, index, "texture");
            return null;
        }
        catch (ArgumentException e)
        {
            return e.Message;
        }
    }

    private static void CheckFileName(string name, string suffix)
    {
        if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) || name.Length <= suffix.Length || name.Length > 55)
            throw new ArgumentException($"\"{name}\" must be a {suffix} file name of 55 characters or fewer.");
        if (name.Any(c => c <= ' ' || c > '~'))
            throw new ArgumentException($"\"{name}\": use letters, digits and punctuation, without spaces.");
    }

    /// <summary>How a model name is set: keep it, point at another model's name, or write it in place.</summary>
    private readonly record struct NamePlan(bool Keep, int PointTo, bool InPlace, bool SameSlot);

    private NamePlan PlanModelName(int field, string name, int index, string what)
    {
        var va = (int)ReadU32(field);
        if (StringAt((uint)va) == name) return new NamePlan(true, 0, false, false);

        var models = FileOffset(ModelTableVa);
        for (var k = 0; k < ModelCount * 2; k++)
        {
            var other = (int)ReadU32(models + (k / 2) * ModelRecordSize + (k % 2) * 4);
            if (StringAt((uint)other) == name) return new NamePlan(false, other, false, false);
        }

        var capacity = CountReferences(va) == 1 ? SlotCapacity(va) : 0;
        if (name.Length > capacity)
        {
            throw new ArgumentException(capacity == 0
                ? $"Model {index}: its {what} name is shared with another model, so it can only become a name another model already has."
                : $"Model {index}: \"{name}\" is too long; this {what} name has room for {capacity} characters (or use a name another model already has).");
        }

        var otherField = field == models + index * ModelRecordSize ? field + 4 : field - 4;
        return new NamePlan(false, 0, true, (int)ReadU32(otherField) == va);
    }

    private void ApplyModelName(int field, string name, NamePlan plan)
    {
        if (plan.Keep) return;
        if (!plan.InPlace)
        {
            WriteInt(field, plan.PointTo);
            return;
        }

        var va = (int)ReadU32(field);
        var at = FileOffset(va);
        Array.Clear(_data, at, SlotCapacity(va));
        Encoding.ASCII.GetBytes(name).CopyTo(_data, at);
    }

    // ═══════════════════════════════ stance weights, materials, glow, loot ═══════════════════════════════

    /// <summary>How each stance (0-5) weighs in a weapon's value. Stock 1.1, 0.85, 0.85, 1.1, 0.95, 1.</summary>
    public float[] GetStanceWeights()
    {
        RequireItems();
        return Enumerable.Range(0, 6).Select(k => BitConverter.ToSingle(_data, FileOffset(StanceWeightsVa) + 4 * k)).ToArray();
    }

    public void SetStanceWeights(IReadOnlyList<float> weights)
    {
        RequireItems();
        if (weights.Count != 6) throw new ArgumentException("There are six stance weights.");
        if (weights.Any(w => !float.IsFinite(w) || w < 0.01f || w > 100f))
            throw new ArgumentException("Stance weights must be 0.01 to 100.");
        for (var k = 0; k < 6; k++) BitConverter.GetBytes(weights[k]).CopyTo(_data, FileOffset(StanceWeightsVa) + 4 * k);
    }

    /// <summary>The damage multipliers of rusty, fine, masterwork, mithril and adamantine (stock 0.5, 1.5, 2, 3, 4).</summary>
    public float[] GetMaterialMultipliers()
    {
        RequireItems();
        return MaterialLuiVas.Select(va => UpperHalfToFloat(ReadU32(FileOffset(va)) & 0xFFFF)).ToArray();
    }

    /// <summary>
    /// Writes the five material multipliers. The game loads each as the upper half of a float (one
    /// instruction), so a value is rounded to the nearest that fits (1.5, 2, 2.25, 3 ... are exact);
    /// the values written are returned.
    /// </summary>
    public float[] SetMaterialMultipliers(IReadOnlyList<float> values)
    {
        RequireItems();
        if (values.Count != MaterialLuiVas.Length) throw new ArgumentException("There are five material multipliers.");
        if (values.Any(v => !float.IsFinite(v) || v < 0f || v > 100f))
            throw new ArgumentException("Material multipliers must be 0 to 100.");
        var written = new float[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            var upper = FloatToUpperHalf(values[i]);
            var at = FileOffset(MaterialLuiVas[i]);
            WriteInt(at, (int)((ReadU32(at) & 0xFFFF0000) | upper));
            written[i] = UpperHalfToFloat(upper);
        }

        return written;
    }

    /// <summary>The float a lui immediate makes: the value's upper 16 bits.</summary>
    public static float UpperHalfToFloat(uint upper) => BitConverter.Int32BitsToSingle((int)(upper << 16));

    /// <summary>The upper 16 bits nearest to <paramref name="value"/> (round half to even on the dropped bits).</summary>
    public static uint FloatToUpperHalf(float value)
    {
        var bits = (uint)BitConverter.SingleToInt32Bits(value);
        var rounded = bits + 0x7FFF + ((bits >> 16) & 1);
        return rounded >> 16;
    }

    public EnchantGlow GetEnchantGlow()
    {
        RequireItems();
        var at = FileOffset(GlowVa);
        return new EnchantGlow(new Rgb(_data[at], _data[at + 1], _data[at + 2]),
                               new Rgb(_data[at + 8], _data[at + 9], _data[at + 10]));
    }

    public void SetEnchantGlow(EnchantGlow glow)
    {
        RequireItems();
        foreach (var c in new[] { glow.Halo, glow.Core })
        {
            if (c.R is < 0 or > 255 || c.G is < 0 or > 255 || c.B is < 0 or > 255)
                throw new ArgumentException("Glow colours are 0 to 255 per channel.");
        }

        var at = FileOffset(GlowVa);
        _data[at] = (byte)glow.Halo.R;
        _data[at + 1] = (byte)glow.Halo.G;
        _data[at + 2] = (byte)glow.Halo.B;
        _data[at + 8] = (byte)glow.Core.R;
        _data[at + 9] = (byte)glow.Core.G;
        _data[at + 10] = (byte)glow.Core.B;
    }

    /// <summary>
    /// A loot list: each value is either a number 1-15 (the +N; on Extreme the game then raises +N while the
    /// item stays within the drop's budget) or one property bit.
    /// </summary>
    public ulong[] GetLootModifiers(LootModifierList list)
    {
        RequireItems();
        var (_, va, count) = LootLists.Single(l => l.List == list);
        return Enumerable.Range(0, count).Select(k => ReadU64(FileOffset(va) + 8 * k)).ToArray();
    }

    /// <summary>Rewrites a loot list. It keeps its length; each value must be a number 1-15 or a single bit.</summary>
    public void SetLootModifiers(LootModifierList list, IReadOnlyList<ulong> values)
    {
        RequireItems();
        var (_, va, count) = LootLists.Single(l => l.List == list);
        if (values.Count != count) throw new ArgumentException($"The {list} loot list has {count} entries.");
        foreach (var v in values)
        {
            if (!IsLootValue(v)) throw new ArgumentException($"0x{v:X} is neither +1 to +15 nor a single property bit.");
        }

        for (var k = 0; k < count; k++) BitConverter.GetBytes(values[k]).CopyTo(_data, FileOffset(va) + 8 * k);
    }

    public static bool IsLootValue(ulong v) =>
        v is >= 1 and <= 15 || (v > 15 && (v & (v - 1)) == 0);

    // ═══════════════════════════════ strings ═══════════════════════════════

    /// <summary>The NUL-ended printable string at a virtual address, or null.</summary>
    private string? StringAt(uint va)
    {
        var at = (long)va - VaToFile;
        if (at < 0x1000 || at >= _data.Length) return null;
        var end = (int)at;
        while (end < _data.Length && _data[end] != 0)
        {
            if (_data[end] < 0x20 || _data[end] > 0x7E || end - at > 255) return null;
            end++;
        }

        return end >= _data.Length ? null : Encoding.ASCII.GetString(_data, (int)at, end - (int)at);
    }

    /// <summary>
    /// The longest string that fits at <paramref name="va"/>: up to the next string, keeping a NUL, and
    /// never more than 31 characters.
    /// </summary>
    private int SlotCapacity(int va)
    {
        var at = FileOffset(va);
        var end = at;
        while (end < _data.Length && _data[end] != 0) end++;
        while (end < _data.Length && _data[end] == 0 && end - at < 32) end++;
        return end - at - 1;
    }

    /// <summary>
    /// How many places refer to <paramref name="va"/>: aligned 32-bit words equal to it anywhere in the
    /// file, and lui/addiu (or load) pairs in the code that form it.
    /// </summary>
    private int CountReferences(int va) => DataReferences(va) + CodeReferences(va).Count;

    private Dictionary<int, int>? _dataReferences;

    /// <summary>How many aligned 32-bit words in the file hold <paramref name="va"/> (string area 0x300000-0x330000).</summary>
    private int DataReferences(int va)
    {
        CheckReferenceCaches();
        if (_dataReferences == null)
        {
            _dataReferences = new Dictionary<int, int>();
            for (var o = 0; o + 4 <= _data.Length; o += 4)
            {
                var w = BitConverter.ToInt32(_data, o);
                if (w is < 0x00300000 or >= 0x00330000) continue;
                _dataReferences[w] = _dataReferences.TryGetValue(w, out var n) ? n + 1 : 1;
            }
        }

        return _dataReferences.TryGetValue(va, out var count) ? count : 0;
    }

    /// <summary>Drops the cached references after an edit that moves a pointer or a string.</summary>
    private void ForgetReferences()
    {
        _dataReferences = null;
        _codeReferences = null;
        _roomReferences = null;
    }

    private byte[]? _referencesOf;

    /// <summary>Drops the cached references when the file was replaced (three players turned on or off).</summary>
    private void CheckReferenceCaches()
    {
        if (ReferenceEquals(_referencesOf, _data)) return;
        ForgetReferences();
        _referencesOf = _data;
    }

    private Dictionary<int, List<int>>? _codeReferences;

    /// <summary>
    /// The code addresses that form <paramref name="va"/> with a lui and an addiu or a load (the lui up to
    /// 128 instructions before, in the main code 0x00100000-0x0022F530). Cached; edits that move strings
    /// call <see cref="ForgetReferences"/>.
    /// </summary>
    private List<int> CodeReferences(int va)
    {
        CheckReferenceCaches();
        _codeReferences ??= ScanCodeReferences();
        return _codeReferences.TryGetValue(va, out var list) ? list : new List<int>();
    }

    private const int CodeStart = 0x00100000, CodeEnd = 0x0022F530, LuiWindow = 128;

    private Dictionary<int, List<int>> ScanCodeReferences()
    {
        var refs = new Dictionary<int, List<int>>();
        var lastLui = new int[32];
        var luiAt = new int[32];
        for (var r = 0; r < 32; r++) luiAt[r] = int.MinValue;
        var end = Math.Min(CodeEnd, _data.Length + VaToFile);
        for (var va = CodeStart; va + 4 <= end; va += 4)
        {
            var w = ReadU32(FileOffset(va));
            var op = (int)(w >> 26);
            var rs = (int)((w >> 21) & 31);
            var rt = (int)((w >> 16) & 31);
            if (op == 0x0F)
            {
                lastLui[rt] = (int)(w & 0xFFFF) << 16;
                luiAt[rt] = va;
                continue;
            }

            if (op is not (0x09 or 0x19 or 0x20 or 0x21 or 0x23 or 0x24 or 0x25 or 0x27 or 0x28 or 0x29 or 0x2B
                or 0x31 or 0x37 or 0x39 or 0x3F)) continue;
            if (va - luiAt[rs] > LuiWindow * 4) continue;
            var target = lastLui[rs] + (short)(w & 0xFFFF);
            if (!refs.TryGetValue(target, out var list)) refs[target] = list = new List<int>();
            list.Add(va);
        }

        return refs;
    }

    private uint ReadU32(int off) => BitConverter.ToUInt32(_data, off);

    private ulong ReadU64(int off) => BitConverter.ToUInt64(_data, off);
}
