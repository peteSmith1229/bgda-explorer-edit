// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Executable.Items.cs
//
// BASE ITEMS, 0x318C00: 83 rows of 48 bytes (weapons, armour, potions, jewellery, gems):
//   +0 name pointer   +4 f32 weight   +16 i32 base value (gold; the shop prices, the random-loot and
//   shop-stock generator 0x2A8D30 choose bases by it)   +20 i32 armour   +24 byte type (1 weapon,
//   2-3 shield / armour pieces, 4 / 6 / 12 jewellery, potions and gems)   +25 byte list (0 weapons,
//   1 armour, 2 other)   +26 byte weapon class (0 one-handed, 1 two-handed sword, 2 axe / hammer,
//   3 polearm, 4 bow, 7 thrown, 8 crossbow)   +32 u64 flags.
//   Items in the game (and in saves) point at their row, so an edit reaches every such item.
//
// LEVELS, 0x311AD8: 108 records of 0x44 bytes: +0 level name pointer, +4 / +8 lump names, +12 f32
//   monster level L (read into 0x35FD7C when the level loads; every creature's HP, damage, armour,
//   attack and XP rating grows with it), +60 flags: 0x100000 / 0x200000 / 0x400000 / 0x800000 = act
//   1-4 (the formula set: 0x400000 -> set 3, 0x200000 -> 2, 0x800000 -> 4, else 1), 0x80000 = basic
//   items only (the opening levels: plain items, no quality or gems).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

public sealed record Da2Item(int Index, uint Address, string Name, float Weight, int Value, int Armour, int Type, int List, int WeaponClass)
{
    public static readonly string[] Lists = { "Weapon", "Armour", "Other" };
    public static readonly string[] WeaponClasses =
        { "One-handed", "Two-handed sword", "Axe / hammer", "Polearm", "Bow", "?", "?", "Thrown", "Crossbow" };

    public string ListName => List >= 0 && List < Lists.Length ? Lists[List] : "?";

    public string Kind => List == 0
        ? (WeaponClass >= 0 && WeaponClass < WeaponClasses.Length ? WeaponClasses[WeaponClass] : "Weapon")
        : Type switch
        {
            2 => "Shield", 3 => "Body armour", 8 => "Gloves", 9 => "Boots", 10 => "Helmet",
            4 => "Ring", 6 => "Amulet", 12 => "Potion / gem", _ => ListName
        };
}

public sealed record Da2Level(int Index, uint Address, string Name, float MonsterLevel, uint Flags)
{
    public int Act => (Flags & 0x800000) != 0 ? 4 : (Flags & 0x400000) != 0 ? 3 : (Flags & 0x200000) != 0 ? 2 : 1;

    /// <summary>The stat formula set of setStats 0x24B250 (1-4).</summary>
    public int FormulaSet => (Flags & 0x400000) != 0 ? 3 : (Flags & 0x200000) != 0 ? 2 : (Flags & 0x800000) != 0 ? 4 : 1;

    public bool BasicItemsOnly => (Flags & 0x80000) != 0;
}

public sealed partial class Da2Executable
{
    public const uint ItemTable = 0x00318C00;
    public const uint ItemSize = 48;
    public const int ItemCount = 83;
    public const int MaxItemValue = 10_000_000, MaxArmour = 255;
    public const float MaxWeight = 1000f;

    public const uint LevelTable = 0x00311AD8;
    public const uint LevelSize = 0x44;
    public const int LevelCount = 108;
    public const float MaxMonsterLevel = 100f;

    public List<Da2Item> GetItems()
    {
        var list = new List<Da2Item>();
        for (var i = 0; i < ItemCount; i++)
        {
            var a = ItemTable + (uint)(i * ItemSize);
            list.Add(new Da2Item(i, a, StringAt(a), F32(a + 4), I32(a + 16), I32(a + 20), U8(a + 24), U8(a + 25), U8(a + 26)));
        }

        return list;
    }

    public void SetItem(int index, float weight, int value, int armour)
    {
        if (index < 0 || index >= ItemCount) throw new ArgumentOutOfRangeException(nameof(index));
        var a = ItemTable + (uint)(index * ItemSize);
        var name = StringAt(a);
        if (float.IsNaN(weight) || weight < 0 || weight > MaxWeight) throw new ArgumentException($"{name}: the weight must be from 0 to {MaxWeight:0}.");
        if (value < 1 || value > MaxItemValue) throw new ArgumentException($"{name}: the value must be from 1 to {MaxItemValue:N0} gold.");
        if (armour < 0 || armour > MaxArmour) throw new ArgumentException($"{name}: the armour must be from 0 to {MaxArmour}.");
        SetF32(a + 4, weight);
        SetI32(a + 16, value);
        SetI32(a + 20, armour);
    }

    public List<Da2Level> GetLevels() =>
        Enumerable.Range(0, LevelCount).Select(i =>
        {
            var a = LevelTable + (uint)(i * LevelSize);
            return new Da2Level(i, a, StringAt(a), F32(a + 12), Word(a + 60));
        }).ToList();

    public void SetMonsterLevel(int index, float level)
    {
        if (index < 0 || index >= LevelCount) throw new ArgumentOutOfRangeException(nameof(index));
        if (float.IsNaN(level) || level < 0 || level > MaxMonsterLevel)
            throw new ArgumentException($"{StringAt(LevelTable + (uint)(index * LevelSize))}: the monster level must be from 0 to {MaxMonsterLevel:0}.");
        SetF32(LevelTable + (uint)(index * LevelSize) + 12, level);
    }
}
