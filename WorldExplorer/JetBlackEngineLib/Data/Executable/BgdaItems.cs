// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaItems.cs
//
// Weapons, armour and other items of Baldur's Gate: Dark Alliance (PAL SLES-506.72):
// the types, the 64 property bits and the game's own item formulas. Reading and
// writing the tables is in BgdaExecutable.Items.cs and BgdaExecutable.Shops.cs.
//
// HOW THE GAME DESCRIBES AN ITEM
//   Item table (VA 0x002A8098, 81 records of 0x30 bytes, a zero record ends it; on a
//   three-player executable more can be added after them, see BgdaExecutable.Items.cs):
//     +0x00 name pointer ("longsword")    +0x04 f32 weight (pounds)
//     +0x08 u16 stack size (arrows: 20)   +0x0A s16 damage min   +0x0C s16 damage max
//     +0x10 s32 price                     +0x14 s16 armour
//     +0x18 s8 equipment slot             +0x19 s8 category (weapon / armour / other)
//     +0x1A s8 fighting stance            +0x1C s32 damage type (1 blunt, 4 ranged, 8 bladed)
//     +0x20 u64 property bits             +0x28 s16 model   +0x2A s16 pickup model
//     +0x2C u8 padding: the item's glow setting with glow styles (BgdaExecutable.Glow.cs)
//   Everything else in the game names items as text: a base name and modifiers,
//   "longsword+masterwork+1+fireburst" (FUN_00174B48 turns it into an item). Shops,
//   level objects (item=...), scripts (givePlayerItem) and starting kits all use it.
//   An item in an inventory or a save is {item table index, count, property bits}.
//
// THE FORMULAS (checked against the game's own code run in an emulator, 3000 random
// items: the value differs by 1 in one case, everything else is exact)
//   Material (FUN_00178818): rusty x0.5, fine x1.5, masterwork x2, mithril x3,
//     adamantine x4 (the last of these set wins).
//   Damage range (FUN_001789B8, used by attacks and the item card):
//     (min + N) x material .. (max + N) x material, rounded; Serrated/Spiked: min = max.
//   Armour of a piece (FUN_00178AA0): armour + N, -1 if rusty (0 stays 0).
//   Value (FUN_00173BF8) is the shop's full price. At start-up the game replaces every
//     item's price field with the value of the plain item (FUN_00178308), so a weapon's
//     price comes from its damage, not from the field, and potions and gems end up at a
//     quarter of it (ItemFormulas.ShopValue). Shops charge value x (1 - bonus) and pay
//     value x 0.45 x (1 + bonus), where bonus = 0.1 for the elf and the drow + 0.05 per
//     Charisma point above the rolled score.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>Which inventory list an item goes in (+0x19).</summary>
public enum ItemCategory
{
    Weapon = 0,
    Armour = 1,
    Other = 2
}

/// <summary>The equipment slot an item is worn in (+0x18).</summary>
public enum ItemSlot
{
    OneHanded = 0,
    TwoHanded = 1,
    Shield = 2,
    Body = 3,
    Ring = 4,
    Amulet = 6,
    Ammunition = 7,
    Gloves = 8,
    Boots = 9,
    Helmet = 10,
    Usable = 11
}

/// <summary>How a weapon is held and swung (+0x1A); 0 for everything that isn't a weapon.</summary>
public enum ItemStance
{
    OneHanded = 0,
    TwoHandedSword = 1,
    TwoHandedAxe = 2,
    Staff = 3,
    Bow = 4,
    Ammunition = 5
}

/// <summary>What a property bit is.</summary>
public enum ItemBitKind
{
    /// <summary>Bits 0-3: the +N enchantment, read as a number.</summary>
    Enchantment,
    /// <summary>A modifier the game names on the item ("Flaming", "of Speed").</summary>
    Modifier,
    /// <summary>What the item is made of or what kind it is; never named.</summary>
    Kind,
    /// <summary>Bits 26-28: how many looks the item has (an item's own copy: which look it got).</summary>
    Looks,
    /// <summary>Not seen used.</summary>
    Unknown
}

/// <summary>One of the 64 property bits (+0x20 of an item, +0x08 of an item in an inventory).</summary>
public sealed record ItemBit(int Bit, ItemBitKind Kind, string Name, string? Keyword, string? TextKey, string Effect);

/// <summary>The property bits, the modifier words of the item text and their effects.</summary>
public static class ItemBits
{
    public const ulong EnchantmentMask = 0xF;
    public const int LooksShift = 26;
    public const ulong LooksMask = 7UL << LooksShift;

    public const int Fire = 4, Defence = 5, Ice = 6, Shock = 7, Wounding = 8, Speed = 9, Keen = 10,
        FireBurst = 11, IceBurst = 12, ShockBurst = 13, Disruption = 14, Elven = 15,
        Blunt = 16, Masterwork = 21, Stackable = 22, SuperHaste = 23, UniqueName = 24,
        NoRandomLoot = 29, Protection = 31, Dexterity = 32, Constitution = 33, Strength = 34,
        Intelligence = 35, Regeneration = 36, Rejuvenation = 37, Jeweled = 38, Fine = 39,
        Serrated = 40, Adamantine = 41, Mithril = 42, Metal = 43, Rusty = 44;

    /// <summary>Every bit, in bit order (bits 0-3 and 26-28 are listed once each).</summary>
    public static readonly IReadOnlyList<ItemBit> All = new[]
    {
        new ItemBit(0, ItemBitKind.Enchantment, "+N", "1 to 15", null,
            "Bits 0-3 hold N. Adds N to damage, armour, stat bonuses, protection and regeneration; shown as \"+N\"."),
        new ItemBit(Fire, ItemBitKind.Modifier, "Flaming", "fire", "it_flaming", "Item card: +1-6 fire damage."),
        new ItemBit(Defence, ItemBitKind.Modifier, "of Defence", "defense", "it_defense",
            "A weapon of Defence raises armour class by half of N (rounded up); value x1.5."),
        new ItemBit(Ice, ItemBitKind.Modifier, "Frost", "ice", "it_frost", "Item card: +1-12 cold damage."),
        new ItemBit(Shock, ItemBitKind.Modifier, "Shock", "shock", "it_shock", "Item card: +1-24 electric damage."),
        new ItemBit(Wounding, ItemBitKind.Modifier, "Wounding", "wound", "it_wounding", "Raises the value as +6 damage would."),
        new ItemBit(Speed, ItemBitKind.Modifier, "of Speed", "speed", "it_speed", "Value x1.5."),
        new ItemBit(Keen, ItemBitKind.Modifier, "Keen", "keen", "it_keen",
            "Shown with the critical-hit icon; value x1.5. Random loot only puts it on bladed weapons."),
        new ItemBit(FireBurst, ItemBitKind.Modifier, "Flaming Burst", "fireburst", "it_flameburst",
            "Item card: +1-6 fire damage; sets enemies on fire (confirmed in game)."),
        new ItemBit(IceBurst, ItemBitKind.Modifier, "Icy Burst", "iceburst", "it_frostburst", "Item card: +1-12 cold damage."),
        new ItemBit(ShockBurst, ItemBitKind.Modifier, "Shocking Burst", "shockburst", "it_shockburst",
            "Item card: +1-24 electric damage (no lightning damage seen in game)."),
        new ItemBit(Disruption, ItemBitKind.Modifier, "of Disruption", "disruption", "it_disruption",
            "Critical hits on undead do about 4x damage (confirmed in game); value x1.2. Random loot only puts it on blunt weapons."),
        new ItemBit(Elven, ItemBitKind.Modifier, "Elven", "elven", "it_elven", "Shown at half the weight; armour value x1.5."),
        new ItemBit(Blunt, ItemBitKind.Kind, "Blunt weapon", null, null, "Names bit 40 \"Spiked\" instead of \"Serrated\"."),
        new ItemBit(17, ItemBitKind.Kind, "Bladed weapon", null, null, "Swords, daggers, the halberd and the spear."),
        new ItemBit(18, ItemBitKind.Kind, "Scimitar", null, null, "Scimitars, the Onyx Sword, Icingdeath and Twinkle."),
        new ItemBit(19, ItemBitKind.Kind, "Axe, hammer or bow", null, null, "Axes, the warhammer, the shortbow and arrows."),
        new ItemBit(20, ItemBitKind.Unknown, "Bit 20", null, null, "Not set on any item."),
        new ItemBit(Masterwork, ItemBitKind.Modifier, "Masterwork", "masterwork", "it_masterwork", "Damage x2 (material)."),
        new ItemBit(Stackable, ItemBitKind.Kind, "Stacks", null, null, "Potions, gems and arrows: adding one to the inventory adds to the line already there."),
        new ItemBit(SuperHaste, ItemBitKind.Modifier, "of Super Haste", "superhaste", "it_superhaste", "Value x3 (weapons)."),
        new ItemBit(UniqueName, ItemBitKind.Kind, "Own name", null, null,
            "Shown by its own name only, without modifiers or +N (Icingdeath, Twinkle, arrows)."),
        new ItemBit(25, ItemBitKind.Kind, "Gem", null, null, "Set on the four gems."),
        new ItemBit(LooksShift, ItemBitKind.Looks, "Looks", null, null,
            "Bits 26-28: how many models in a row the item can use (1-7). Each item made gets one at random (weapons of a better material pick later ones)."),
        new ItemBit(NoRandomLoot, ItemBitKind.Kind, "Never random loot", null, null,
            "Never picked for random drops or random shop stock (potions, clothes, unique items)."),
        new ItemBit(30, ItemBitKind.Kind, "Long", null, null, "Body armour, clothes, the staff and the spear."),
        new ItemBit(Protection, ItemBitKind.Modifier, "of Protection", "protection", "it_protection",
            "Rings and amulets: armour class +N."),
        new ItemBit(Dexterity, ItemBitKind.Modifier, "of Dexterity", "dexterity", "it_dexterity", "Dexterity +N while worn."),
        new ItemBit(Constitution, ItemBitKind.Modifier, "of Constitution", "constitution", "it_constitution", "Constitution +N while worn."),
        new ItemBit(Strength, ItemBitKind.Modifier, "of Strength", "strength", "it_strength", "Strength +N while worn."),
        new ItemBit(Intelligence, ItemBitKind.Modifier, "of Intellect", "intelligence", "it_intellect", "Intelligence +N while worn."),
        new ItemBit(Regeneration, ItemBitKind.Modifier, "of Regeneration", "healthregeneration", "it_health",
            "Health regenerates faster while worn (by the item's power)."),
        new ItemBit(Rejuvenation, ItemBitKind.Modifier, "of Rejuvenation", "magicregeneration", "it_mana",
            "Energy regenerates faster while worn (by the item's power)."),
        new ItemBit(Jeweled, ItemBitKind.Modifier, "Jeweled", null, "it_jeweled",
            "Value x2. Nothing in the game sets it: no item text word and no random loot."),
        new ItemBit(Fine, ItemBitKind.Modifier, "Fine", "fine", "it_fine", "Damage x1.5 (material)."),
        new ItemBit(Serrated, ItemBitKind.Modifier, "Serrated / Spiked", null, "it_serrated",
            "Every hit does the maximum damage. No item text word sets it; random loot can."),
        new ItemBit(Adamantine, ItemBitKind.Modifier, "Adamantine / Oak", "adamantine", "it_adamantine",
            "Damage x4 (material). \"Oak\" on items that aren't metal."),
        new ItemBit(Mithril, ItemBitKind.Modifier, "Mithral / Darkwood", "mithril", "it_mithral",
            "Damage x3 (material). \"Darkwood\" on items that aren't metal."),
        new ItemBit(Metal, ItemBitKind.Kind, "Metal", null, null, "Chooses Adamantine/Mithral/Rusty rather than Oak/Darkwood/Worn."),
        new ItemBit(Rusty, ItemBitKind.Modifier, "Rusty / Worn", "rusty", "it_rusty",
            "Damage x0.5, armour -1, armour value x0.5. \"Worn\" on items that aren't metal."),
        new ItemBit(45, ItemBitKind.Kind, "Cloth", null, null, "Clothes, padded armour and arrows."),
        new ItemBit(46, ItemBitKind.Kind, "Leather", null, null, "Leather and studded leather."),
        new ItemBit(47, ItemBitKind.Kind, "Mail", null, null, "Scale mail and chainmail."),
        new ItemBit(48, ItemBitKind.Kind, "Plate", null, null, "Half plate and full plate."),
        new ItemBit(49, ItemBitKind.Kind, "Wood", null, null, "The shortbow, club, staff and wooden shield."),
        new ItemBit(50, ItemBitKind.Kind, "Potion", null, null, "The potions."),
        new ItemBit(51, ItemBitKind.Kind, "Amulet", null, null, "The amulets."),
        new ItemBit(52, ItemBitKind.Kind, "Ring", null, null, "The rings."),
        new ItemBit(53, ItemBitKind.Unknown, "Bit 53", null, null, "Not set on any item."),
        new ItemBit(54, ItemBitKind.Kind, "Gem", null, null, "The four gems."),
        new ItemBit(55, ItemBitKind.Kind, "Dagger", null, null, "The dagger."),
        new ItemBit(56, ItemBitKind.Kind, "One-handed melee", null, null, "One-handed melee weapons."),
        new ItemBit(57, ItemBitKind.Kind, "Two-handed melee", null, null, "Two-handed melee weapons."),
        new ItemBit(58, ItemBitKind.Kind, "Warhammer", null, null, "Only the warhammer: what Clangeddin's Fist needs."),
        new ItemBit(59, ItemBitKind.Unknown, "Bit 59", null, null, "Not set on any item."),
        new ItemBit(60, ItemBitKind.Unknown, "Bit 60", null, null, "Not set on any item."),
        new ItemBit(61, ItemBitKind.Unknown, "Bit 61", null, null, "Not set on any item."),
        new ItemBit(62, ItemBitKind.Unknown, "Bit 62", null, null, "Not set on any item."),
        new ItemBit(63, ItemBitKind.Unknown, "Bit 63", null, null, "Not set on any item.")
    };

    /// <summary>The modifiers (the bits the game names on an item), in the order the game names them.</summary>
    public static IEnumerable<ItemBit> Modifiers => All.Where(b => b.Kind == ItemBitKind.Modifier);

    /// <summary>The bit with this number (bits 1-3 give the +N entry, 27-28 the looks entry).</summary>
    public static ItemBit Get(int bit) =>
        All.Last(b => b.Bit <= bit);

    /// <summary>The modifier words of the item text, as the game's parser (FUN_00174B48) has them.</summary>
    public static readonly IReadOnlyDictionary<string, int> Keywords =
        All.Where(b => b.Keyword != null && b.Kind == ItemBitKind.Modifier)
           .ToDictionary(b => b.Keyword!, b => b.Bit, StringComparer.Ordinal);

    public static int Enchantment(ulong flags) => (int)(flags & EnchantmentMask);

    public static bool Has(ulong flags, int bit) => ((flags >> bit) & 1) != 0;

    /// <summary>How many models in a row the item uses (bits 26-28; 0 counts as 1).</summary>
    public static int Looks(ulong flags)
    {
        var n = (int)((flags & LooksMask) >> LooksShift);
        return n == 0 ? 1 : n;
    }

    public static ulong WithLooks(ulong flags, int looks)
    {
        if (looks < 1 || looks > 7) throw new ArgumentException("An item has 1 to 7 looks.");
        return (flags & ~LooksMask) | ((ulong)(looks == 1 ? 0 : looks) << LooksShift);
    }

    public static ulong WithEnchantment(ulong flags, int n)
    {
        if (n < 0 || n > 15) throw new ArgumentException("The enchantment is 0 to 15.");
        return (flags & ~EnchantmentMask) | (uint)n;
    }

    /// <summary>The names of the modifier bits set (and "+N"), as the item would show them in English.</summary>
    public static string Describe(ulong flags, bool metal = true)
    {
        var parts = new List<string>();
        foreach (var b in Modifiers)
        {
            if (!Has(flags, b.Bit)) continue;
            parts.Add(b.Bit switch
            {
                Adamantine => metal ? "Adamantine" : "Oak",
                Mithril => metal ? "Mithral" : "Darkwood",
                Rusty => metal ? "Rusty" : "Worn",
                Serrated => Has(flags, Blunt) ? "Spiked" : "Serrated",
                _ => b.Name
            });
        }

        var n = Enchantment(flags);
        if (n > 0) parts.Add("+" + n.ToString(CultureInfo.InvariantCulture));
        return string.Join(", ", parts);
    }
}

/// <summary>One record of the item table.</summary>
public sealed class GameItem
{
    /// <summary>Position in the table: what saves and inventories store.</summary>
    public int Index { get; init; }

    /// <summary>The name the item text uses ("longsword"); "_name" added gives its text key in GTEXT.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// True for the eight spell-scroll records the game never uses (burninghands ... magicmissile):
    /// they can be renamed and turned into any kind of item.
    /// </summary>
    public bool IsSpare { get; init; }

    /// <summary>
    /// True for a record added after the game's own 81 (index 81 and up, three-player executables only):
    /// it can be renamed and turned into any kind of item.
    /// </summary>
    public bool IsAdded { get; init; }

    /// <summary>The longest name that fits where this record's name is stored (0 when it can't be renamed).</summary>
    public int NameCapacity { get; init; }

    public float Weight { get; set; }
    public int StackSize { get; set; }
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }

    /// <summary>The price field. For weapons the game overwrites it with the value worked out from the damage.</summary>
    public int Price { get; set; }

    public int Armour { get; set; }
    public ItemSlot Slot { get; set; }
    public ItemCategory Category { get; set; }
    public ItemStance Stance { get; set; }

    /// <summary>The attack type of its hits: 1 blunt, 4 ranged, 8 bladed (other types: fire 0x20, lightning 0x80, cold 0x400).</summary>
    public int DamageType { get; set; }

    public ulong Flags { get; set; }

    /// <summary>
    /// Weapons, shields and the rest: the model in the hand (model table index; the looks follow it).
    /// Body armour, boots, gloves and helmets: the ARMOR.YAK entry worn (the next entry is the enchanted look).
    /// </summary>
    public int Model { get; set; }

    /// <summary>The model on the ground and in the inventory (model table index).</summary>
    public int PickupModel { get; set; }

    /// <summary>Its own glow setting (+0x2C, used with glow styles): automatic, a style, or never.</summary>
    public ItemGlow Glow { get; set; }

    public bool WornFromArmorYak => Category == ItemCategory.Armour && Slot != ItemSlot.Shield;

    public GameItem Clone() => (GameItem)MemberwiseClone();

    /// <summary>A copy at another place in the table (an added item that moves up when one before it is removed).</summary>
    public GameItem WithIndex(int index) => CopyAt(index, IsSpare, IsAdded, NameCapacity);

    /// <summary>A copy as an item added after the game's own, at <paramref name="index"/>.</summary>
    public GameItem AsAdded(int index) => CopyAt(index, false, true, BgdaExecutable.MaxAddedItemName);

    private GameItem CopyAt(int index, bool spare, bool added, int nameCapacity) => new()
    {
        Index = index,
        Name = Name,
        IsSpare = spare,
        IsAdded = added,
        NameCapacity = nameCapacity,
        Weight = Weight,
        StackSize = StackSize,
        DamageMin = DamageMin,
        DamageMax = DamageMax,
        Price = Price,
        Armour = Armour,
        Slot = Slot,
        Category = Category,
        Stance = Stance,
        DamageType = DamageType,
        Flags = Flags,
        Model = Model,
        PickupModel = PickupModel,
        Glow = Glow
    };
}

/// <summary>One record of the model table: a .vif model and a .tex texture in EQUIPMNT.LMP.</summary>
public sealed class GameItemModel
{
    public int Index { get; init; }
    public string Vif { get; init; } = "";
    public string Tex { get; init; } = "";

    /// <summary>The longest .vif name that fits in place (0: the name is shared and can only point at another name).</summary>
    public int VifCapacity { get; init; }

    public int TexCapacity { get; init; }
}

/// <summary>An item as the game makes it from item text: what <see cref="ItemText.Parse"/> returns.</summary>
public sealed record ParsedItem(int ItemIndex, int Count, ulong Flags, IReadOnlyList<string> Problems)
{
    public bool IsValid => Problems.Count == 0;
}

/// <summary>Item text ("longsword+masterwork+1+fireburst") the way the game reads it (FUN_00174B48).</summary>
public static class ItemText
{
    public const int MaxLength = 63;

    /// <summary>
    /// Reads item text like the game: the base name before the first '+', then each '+' word. A number
    /// is OR-ed into the +N bits (the game keeps its low four bits), a known word sets its bit, anything
    /// else is skipped. An unknown base name gives the first item (the shortbow) in the game; here it is a
    /// problem. Every case the game would quietly change or skip is listed in <see cref="ParsedItem.Problems"/>.
    /// </summary>
    public static ParsedItem Parse(string text, IReadOnlyList<GameItem> items)
    {
        var problems = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return new ParsedItem(-1, 0, 0, new[] { "Enter an item, such as longsword+1." });
        }

        if (text.Length > MaxLength) problems.Add($"Use {MaxLength} characters or fewer.");
        if (text.Any(c => c <= ' ' || c > '~')) problems.Add("Use letters, digits and '+' only, without spaces.");

        var words = text.Split('+');
        var index = -1;
        for (var i = 0; i < items.Count; i++)
        {
            if (string.Equals(items[i].Name, words[0], StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            problems.Add($"There is no item called \"{words[0]}\" (the game would give a shortbow).");
            return new ParsedItem(-1, 0, 0, problems);
        }

        var item = items[index];
        var flags = item.Flags;
        foreach (var word in words.Skip(1))
        {
            if (word.Length > 0 && char.IsDigit(word[0]))
            {
                var digits = new string(word.TakeWhile(char.IsDigit).ToArray());
                var n = int.Parse(digits.Length > 9 ? digits[..9] : digits, CultureInfo.InvariantCulture);
                if (digits.Length != word.Length) problems.Add($"\"{word}\": the game reads only the number {n}.");
                if (n < 1 || n > 15) problems.Add($"+{word}: the enchantment is 1 to 15.");
                flags |= (ulong)n & ItemBits.EnchantmentMask;
                continue;
            }

            if (ItemBits.Keywords.TryGetValue(word, out var bit))
            {
                flags |= 1UL << bit;
                continue;
            }

            problems.Add($"\"{word}\" isn't a word the game knows; it would be skipped" +
                         (word == "mithral" ? " (the game spells it \"mithril\")." : "."));
        }

        return new ParsedItem(index, item.StackSize, flags, problems);
    }
}

/// <summary>The game's item formulas, in single precision like the game.</summary>
public static class ItemFormulas
{
    /// <summary>The material multipliers in the order the game tests them (the last one set wins).</summary>
    public static readonly IReadOnlyList<(int Bit, string Name)> Materials = new[]
    {
        (ItemBits.Rusty, "Rusty / Worn"), (ItemBits.Fine, "Fine"), (ItemBits.Masterwork, "Masterwork"),
        (ItemBits.Mithril, "Mithral / Darkwood"), (ItemBits.Adamantine, "Adamantine / Oak")
    };

    /// <summary>The stock multipliers of <see cref="Materials"/>: 0.5, 1.5, 2, 3, 4.</summary>
    public static readonly IReadOnlyList<float> StockMaterialMultipliers = new[] { 0.5f, 1.5f, 2f, 3f, 4f };

    /// <summary>The stock value weights of the six stances (VA 0x002A76E8).</summary>
    public static readonly IReadOnlyList<float> StockStanceWeights = new[] { 1.1f, 0.85f, 0.85f, 1.1f, 0.95f, 1.0f };

    public static float Material(ulong flags, IReadOnlyList<float>? multipliers = null)
    {
        multipliers ??= StockMaterialMultipliers;
        var m = 1f;
        for (var i = 0; i < Materials.Count; i++)
        {
            if (ItemBits.Has(flags, Materials[i].Bit)) m = multipliers[i];
        }

        return m;
    }

    /// <summary>The damage of each hit (FUN_001789B8): the minimum and the maximum.</summary>
    public static (int Min, int Max) Damage(GameItem item, ulong flags, IReadOnlyList<float>? multipliers = null)
    {
        var n = ItemBits.Enchantment(flags);
        var min = ItemBits.Has(flags, ItemBits.Serrated) ? item.DamageMax : item.DamageMin;
        var m = Material(flags, multipliers);
        return ((int)((min + n) * m + 0.5f), (int)((item.DamageMax + n) * m + 0.5f));
    }

    /// <summary>Armour of one piece (FUN_00178AA0).</summary>
    public static int Armour(GameItem item, ulong flags)
    {
        if (item.Armour == 0) return 0;
        return item.Armour + ItemBits.Enchantment(flags) - (ItemBits.Has(flags, ItemBits.Rusty) ? 1 : 0);
    }

    /// <summary>
    /// An item's power (FUN_00174098): drives regeneration and the value of potions and gems.
    /// The game adds 7 when bit 0 is set, so odd enchantments count for much more.
    /// </summary>
    public static int Power(ulong flags)
    {
        var n = ItemBits.Enchantment(flags);
        var v = n;
        if ((flags & 1) != 0) v += 7;
        foreach (var (bit, add) in new[] { (4, 1), (5, 1), (6, 1), (7, 1), (8, 2), (9, 4), (11, 2), (12, 2), (13, 2), (14, 2), (23, 8) })
        {
            if (ItemBits.Has(flags, bit)) v += add;
        }

        for (var bit = 31; bit <= 37; bit++)
        {
            if (ItemBits.Has(flags, bit)) v += n;
        }

        return v;
    }

    /// <summary>
    /// The shop value of <paramref name="count"/> of an item with these bits (FUN_00173BF8). Weapons
    /// are valued by their damage; armour and other items by the price field; rings and amulets by
    /// their modifier and +N alone.
    /// </summary>
    public static int Value(GameItem item, int count, ulong flags,
        IReadOnlyList<float>? stanceWeights = null, IReadOnlyList<float>? multipliers = null)
    {
        stanceWeights ??= StockStanceWeights;
        const float half = 0.5f;
        var n = ItemBits.Enchantment(flags);
        bool Has(int bit) => ItemBits.Has(flags, bit);

        if (item.Category == ItemCategory.Weapon && item.Stance != ItemStance.Ammunition)
        {
            var (lo, hi) = Damage(item, flags, multipliers);
            float low = lo, high = hi;
            void Both(float add) { high += add; low += add; }
            if (Has(ItemBits.Fire)) Both(6);
            if (Has(ItemBits.Ice)) Both(12);
            if (Has(ItemBits.Shock)) Both(24);
            if (Has(ItemBits.FireBurst)) { low += 8; high += 16; }
            if (Has(ItemBits.IceBurst)) { low += 16; high += 32; }
            if (Has(ItemBits.ShockBurst)) { low += 48; high += 64; }
            if (Has(ItemBits.Wounding)) Both(6);
            if (Has(ItemBits.SuperHaste)) { high *= 3; low *= 3; }
            if (Has(ItemBits.Speed)) { high += high * half; low += low * half; }
            var p = Pow(n, 1.1f);
            var p2 = p + p;
            high += p2;
            low += p2;
            var weight = (int)item.Stance >= 0 && (int)item.Stance < stanceWeights.Count ? stanceWeights[(int)item.Stance] : 1f;
            high *= weight;
            low *= weight;
            var spread = (high - low) * 0.1f;
            var v = Pow((low + high) * half - spread, 1.95f);
            if (Has(ItemBits.Disruption)) v *= 1.2f;
            if (Has(ItemBits.Keen)) v *= 1.5f;
            if (Has(ItemBits.Defence)) v *= 1.5f;
            v *= 40f;
            if ((flags & 0x807BDF) != 0) v += 400f;
            if (Has(ItemBits.Jeweled)) v += v;
            return (int)(v * half + half);
        }

        if (item.Category == ItemCategory.Armour)
        {
            float v = item.Price * count;
            if (Has(ItemBits.Jeweled)) v += v;
            if (Has(ItemBits.Rusty)) v *= half;
            if (n != 0)
            {
                v *= Pow(n + 1, 0.4f);
                v += Pow(n, 1.4f) * 300f;
            }

            if (Has(ItemBits.Elven)) v *= 1.5f;
            v += v;
            return (int)(v * half + half);
        }

        if (item.Slot is ItemSlot.Ring or ItemSlot.Amulet)
        {
            var v = 1f;
            foreach (var (bit, k) in new[] { (32, 1.82f), (33, 1.6f), (34, 1.82f), (35, 1.3f), (36, 1.76f), (37, 1.79f) })
            {
                if (Has(bit)) v = k;
            }

            v *= Pow(n + 1, 1.7f);
            if (item.Slot == ItemSlot.Ring) v *= 1.05f;
            return (int)(v * 300f + half);
        }

        {
            float v = item.Price * count;
            var power = Power(flags);
            v *= Pow(power + 1, 1.1f);
            if (power > 0) v += 500f;
            return (int)(v * half + half);
        }
    }

    /// <summary>
    /// The value the game shows in a shop. At start-up (FUN_00178308) the game replaces each item's price
    /// field with <see cref="Value"/> of the plain item and values everything from then on with that: the
    /// same for armour, unused for weapons, rings and amulets, but potions, gems and the like get halved
    /// twice (a healing potion priced 250 sells for 63).
    /// </summary>
    public static int ShopValue(GameItem item, int count, ulong flags,
        IReadOnlyList<float>? stanceWeights = null, IReadOnlyList<float>? multipliers = null)
    {
        var running = item.Clone();
        running.Price = Value(item, 1, item.Flags, stanceWeights, multipliers);
        return Value(running, count, flags, stanceWeights, multipliers);
    }

    /// <summary>What a shop charges (FUN_00175AD8): <paramref name="bonus"/> is 0.1 for the elf and the drow plus 0.05 per Charisma point above the rolled score.</summary>
    public static int BuyPrice(int value, float bonus = 0f) => (int)(value * (1f - bonus));

    /// <summary>What a shop pays for one (FUN_00175AD8); arrows are priced per 20.</summary>
    public static int SellPrice(int valuePerOne, bool ammunition = false, float bonus = 0f)
    {
        float v = valuePerOne;
        if (ammunition) v *= 20f;
        var f = v * 0.45f;
        return (int)(bonus * f + f);
    }

    private static float Pow(float x, float y) => x <= 0f ? 0f : MathF.Pow(x, y);
}
