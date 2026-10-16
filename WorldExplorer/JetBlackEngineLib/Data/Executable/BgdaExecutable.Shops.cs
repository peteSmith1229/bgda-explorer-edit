// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.Shops.cs
//
// What the shops sell (SLES_506.72, PAL). FUN_0016FF50 fills a shop's three lists by calling
// the item-text parser FUN_00174B48 once per item, so a list is code: each item is an
// "addiu a0, rX, low" after a "lui rX, high" that points a0 at its item text.
//   Extreme difficulty: every shop sells arrows, 17 random weapons, 17 random armour pieces,
//     extra healing / extra rejuvenation / recall potions and 8 random other items.
//   Otherwise the level picks the list: "tavern" (Bartley), "minecamp" (Branoch), any other
//     level (Sleyvas). The shopkeeper is Branoch in levels whose name contains "mine",
//     Sleyvas in swamp1 and temple1, Bartley elsewhere.
//
// Changing an item rewrites the instruction's low half (and, where the lui is the item's own, its
// high half). New item text goes in the strings only the shop lists use, VA 0x0030B160-0x0030BB10,
// which are packed again on every change; strings anything else uses stay where they are. Text
// the game already has nearby (item names such as "longsword") is pointed at instead of copied.
// The four arrows entries share their lui, so they can only use text near them (arrows, dagger,
// club, staff ...). The three-player patch changes bytes next to these lists but none of them.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>A shop's stock list.</summary>
public enum ShopStock
{
    /// <summary>The fixed items every shop sells on Extreme (the rest is random).</summary>
    Extreme,
    /// <summary>Bartley, level "tavern".</summary>
    Bartley,
    /// <summary>Branoch, level "minecamp".</summary>
    Branoch,
    /// <summary>Sleyvas, any other level.</summary>
    Sleyvas
}

/// <summary>One item a shop sells.</summary>
public sealed class ShopEntry
{
    /// <summary>Position in <see cref="BgdaExecutable.GetShopEntries"/> (0-141).</summary>
    public int Number { get; init; }

    public ShopStock Stock { get; init; }

    /// <summary>The list it is in (weapons, armour or other); its item must be of that kind.</summary>
    public ItemCategory List { get; init; }

    /// <summary>The item text, e.g. "longsword+1+fire".</summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// False for the arrows entries: their text can only be text the game already has next to "arrows"
    /// (<see cref="BgdaExecutable.NearbyShopTexts"/>).
    /// </summary>
    public bool AnyText { get; init; }
}

public partial class BgdaExecutable
{
    private sealed record ShopSlot(ShopStock Stock, ItemCategory List, int AddiuVa, int LuiVa, bool OwnLui, string StockText);

    private const int ShopPoolStart = 0x0030B160, ShopPoolEnd = 0x0030BB10;
    private const uint NearHigh = 0x0031, FarHigh = 0x0032;

    private static readonly ShopSlot[] ShopSlots =
    {
        new(ShopStock.Extreme, ItemCategory.Weapon, 0x001702F0, 0x001702E8, false, "arrows"),
        new(ShopStock.Extreme, ItemCategory.Other, 0x001703F4, 0x00170308, false, "exhealingpotion"),
        new(ShopStock.Extreme, ItemCategory.Other, 0x00170418, 0x00170310, false, "exrejuvpotion"),
        new(ShopStock.Extreme, ItemCategory.Other, 0x00170434, 0x00170314, false, "recallpotion"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001704D0, 0x001704C8, false, "arrows"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001704F4, 0x001704EC, true, "club+fine"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170518, 0x00170514, true, "dagger"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170538, 0x00170534, true, "handaxe"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170558, 0x00170554, true, "halberd"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170578, 0x00170574, true, "spear+1"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170598, 0x00170594, true, "warhammer"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001705B8, 0x001705B4, true, "shortbow"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001705D8, 0x001705D4, true, "shortbow+rusty"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001705F8, 0x001705F4, true, "shortsword+2"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170618, 0x00170614, true, "battleaxe"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170638, 0x00170634, true, "ltMace+fine"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170658, 0x00170654, true, "staff+masterwork"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170678, 0x00170674, true, "longsword+1+fire"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x00170698, 0x00170694, true, "battleaxe+fireburst+1"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001706B8, 0x001706B4, true, "bastardsword+1+ice"),
        new(ShopStock.Bartley, ItemCategory.Weapon, 0x001706D8, 0x001706D4, true, "shortbow+masterwork+1"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x0017070C, 0x001706F4, false, "steelshield"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170718, 0x00170714, true, "paddedarmor"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170738, 0x00170734, true, "paddedboots"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170758, 0x00170754, true, "paddedgloves"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170778, 0x00170774, true, "paddedhelmet"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170798, 0x00170794, true, "leatherarmor"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x001707B8, 0x001707B4, true, "leatherboots"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x001707D8, 0x001707D4, true, "leathergloves"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x001707F8, 0x001707F4, true, "leatherhelmet"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170818, 0x00170814, true, "sleatherarmor"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170838, 0x00170834, true, "sleatherboots"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170858, 0x00170854, true, "sleathergloves"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170878, 0x00170874, true, "sleatherhelmet"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x00170898, 0x00170894, false, "scalemail"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x001708B8, 0x001708B4, false, "scaleboots"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x001708D8, 0x001708D4, true, "scalegloves"),
        new(ShopStock.Bartley, ItemCategory.Armour, 0x001708F8, 0x001708F4, true, "scalehelmet"),
        new(ShopStock.Bartley, ItemCategory.Other, 0x00170920, 0x00170914, false, "healingpotion"),
        new(ShopStock.Bartley, ItemCategory.Other, 0x00170938, 0x00170934, false, "recallpotion"),
        new(ShopStock.Bartley, ItemCategory.Other, 0x00170958, 0x00170954, true, "rejuvpotion"),
        new(ShopStock.Bartley, ItemCategory.Other, 0x00170978, 0x00170974, true, "ring+protection+1"),
        new(ShopStock.Bartley, ItemCategory.Other, 0x0017099C, 0x00170994, true, "amulet+dexterity+1"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x001709C0, 0x001709B8, false, "arrows"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x001709E4, 0x001709DC, true, "dagger+mithril+iceburst"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170A08, 0x00170A04, true, "morningstar+fine"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170A28, 0x00170A24, true, "handaxe+mithril"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170A48, 0x00170A44, true, "battleaxe+fine"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170A68, 0x00170A64, true, "longsword+fine"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170A88, 0x00170A84, true, "ltMace+masterwork+fire"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170AA8, 0x00170AA4, true, "shortsword+ice"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170AC8, 0x00170AC4, true, "warhammer+masterwork+ice+2"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170AE8, 0x00170AE4, true, "morningstar+masterwork+1+disruption"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170B08, 0x00170B04, true, "shortbow+fine+2"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170B28, 0x00170B24, true, "spear+fine+3"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170B48, 0x00170B44, true, "longsword+masterwork+1+fireburst"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170B68, 0x00170B64, true, "staff+mithril+fireburst"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170B88, 0x00170B84, true, "battleaxe+masterwork+iceburst+2"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170BA8, 0x00170BA4, true, "halberd+mithral+defense+4"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170BC8, 0x00170BC4, true, "shortbow+masterwork+2+ice"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170BE8, 0x00170BE4, true, "scimitar+mithril+2"),
        new(ShopStock.Branoch, ItemCategory.Weapon, 0x00170C08, 0x00170C04, true, "greatsword+masterwork+speed"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170C3C, 0x00170C24, false, "steelshield+2"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170C48, 0x00170C44, true, "paddedarmor+3"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170C68, 0x00170C64, true, "paddedboots+2"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170C88, 0x00170C84, true, "paddedgloves+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170CA8, 0x00170CA4, true, "paddedhelmet+2"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170CC8, 0x00170CC4, true, "leatherarmor+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170CE8, 0x00170CE4, true, "leatherboots+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170D08, 0x00170D04, true, "leathergloves+2"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170D28, 0x00170D24, true, "leatherhelmet+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170D48, 0x00170D44, true, "sleatherarmor+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170D68, 0x00170D64, true, "sleatherboots+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170D88, 0x00170D84, true, "sleathergloves+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170DA8, 0x00170DA4, true, "sleatherhelmet+1"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170DC8, 0x00170DC4, false, "scalemail"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170DE8, 0x00170DE4, false, "scaleboots"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170E08, 0x00170E04, true, "scalegloves"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170E28, 0x00170E24, true, "scalehelmet"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170E48, 0x00170E44, true, "chainmail"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170E68, 0x00170E64, true, "chainboots"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170E88, 0x00170E84, true, "chainhelmet"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170EA8, 0x00170EA4, true, "chaingloves"),
        new(ShopStock.Branoch, ItemCategory.Armour, 0x00170EC8, 0x00170EC4, true, "scalemail+elven+2"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170EF0, 0x00170EE4, false, "healingpotion"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170F08, 0x00170F04, false, "recallpotion"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170F28, 0x00170F24, true, "rejuvpotion"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170F48, 0x00170F44, true, "amulet+strength+1"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170F68, 0x00170F64, true, "ring+constitution+1"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170F88, 0x00170F84, true, "amulet+dexterity+1"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170FA8, 0x00170FA4, true, "amulet+dexterity+2"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170FC8, 0x00170FC4, true, "amulet+intelligence+2"),
        new(ShopStock.Branoch, ItemCategory.Other, 0x00170FEC, 0x00170FE4, true, "ring+protection+3"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x00171024, 0x0017101C, false, "arrows"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x00171050, 0x00171044, false, "shortsword+adamantine+2"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017106C, 0x00171068, true, "morningstar+masterwork+1"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017108C, 0x00171088, true, "greataxe+mithril+1"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x001710AC, 0x001710A8, true, "battleaxe+masterwork+2+iceburst"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x001710CC, 0x001710C8, true, "scimitar+masterwork+2+speed"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x001710EC, 0x001710E8, true, "longsword+masterwork+1"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017110C, 0x00171108, true, "ltMace+mithril+fireburst+2"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017112C, 0x00171128, true, "shortsword+ice+3"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017114C, 0x00171148, true, "warhammer+masterwork+shock+3"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017116C, 0x00171168, true, "morningstar+mithril+1+fireburst"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017118C, 0x00171188, true, "shortbow+masterwork+4"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x001711AC, 0x001711A8, true, "spear+masterwork+3+speed"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x001711CC, 0x001711C8, true, "longsword+mithril+1+iceburst"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x001711EC, 0x001711E8, true, "staff+mithril+shockburst+1"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017120C, 0x00171208, true, "battleaxe+mithril+shockburst+3"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017122C, 0x00171228, true, "halberd+adamantine+speed+3"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017124C, 0x00171248, true, "shortbow+mithril+3+iceburst"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017126C, 0x00171268, true, "bastardsword+adamantine+2"),
        new(ShopStock.Sleyvas, ItemCategory.Weapon, 0x0017128C, 0x00171288, true, "morningstar+mithril+disruption+3"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001712C0, 0x001712A8, false, "scalemail+1"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001712CC, 0x001712C8, true, "scaleboots+1"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001712EC, 0x001712E8, true, "scalegloves+1"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017130C, 0x00171308, true, "scalehelmet+1"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017132C, 0x00171328, true, "chainmail"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017134C, 0x00171348, true, "chainboots"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017136C, 0x00171368, true, "chainhelmet"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017138C, 0x00171388, true, "chaingloves"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001713AC, 0x001713A8, true, "chainmail+2"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001713CC, 0x001713C8, true, "chainboots+2"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001713EC, 0x001713E8, true, "chainhelmet+1"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017140C, 0x00171408, true, "chaingloves+1"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017142C, 0x00171428, true, "hplatemail"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017144C, 0x00171448, true, "hplateboots"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017146C, 0x00171468, true, "hplategloves"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017148C, 0x00171488, true, "hplatehelmet"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001714AC, 0x001714A8, true, "platemail"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001714CC, 0x001714C8, true, "plateboots"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x001714EC, 0x001714E8, true, "plategloves"),
        new(ShopStock.Sleyvas, ItemCategory.Armour, 0x0017150C, 0x00171508, true, "platehelmet"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x00171534, 0x00171528, false, "exhealingpotion"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x0017154C, 0x00171548, false, "recallpotion"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x0017156C, 0x00171568, true, "exrejuvpotion"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x0017158C, 0x00171588, true, "ring+strength+3"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x001715AC, 0x001715A8, true, "amulet+constitution+3"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x001715CC, 0x001715C8, true, "amulet+dexterity+2"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x001715EC, 0x001715E8, true, "ring+dexterity+4"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x0017160C, 0x00171608, true, "amulet+intelligence+4"),
        new(ShopStock.Sleyvas, ItemCategory.Other, 0x0017162C, 0x00171628, true, "amulet+protection+4"),
    };

    public static string ShopStockName(ShopStock stock) => stock switch
    {
        ShopStock.Extreme => "Every shop on Extreme (with random stock)",
        ShopStock.Bartley => "Bartley (level tavern)",
        ShopStock.Branoch => "Branoch (level minecamp)",
        _ => "Sleyvas (any other level)"
    };

    public bool ShopsSupported => ShopsProblem() == null;

    /// <summary>Why the shop lists can't be edited, or null when they can.</summary>
    public string? ShopsProblem()
    {
        if (ItemsProblem() is { } items) return items;
        foreach (var slot in ShopSlots)
        {
            if (Decode(slot) is null) return $"The shop code at 0x{slot.AddiuVa:X8} isn't the code of this build.";
        }

        for (var va = ShopPoolStart; va < ShopPoolEnd; va++)
        {
            var b = _data[FileOffset(va)];
            if (b != 0 && (b < 0x20 || b > 0x7E)) return "The shop item text isn't where this build keeps it.";
        }

        return null;
    }

    /// <summary>The high half (0x31 or 0x32) and the item text of a shop slot, or null when the code isn't as expected.</summary>
    private (uint High, int TextVa, string Text)? Decode(ShopSlot slot)
    {
        var addiu = ReadU32(FileOffset(slot.AddiuVa));
        var lui = ReadU32(FileOffset(slot.LuiVa));
        var rs = (addiu >> 21) & 31;
        if (addiu >> 26 != 0x09 || ((addiu >> 16) & 31) != 4) return null;          // addiu a0, rs, low
        if (lui >> 26 != 0x0F || ((lui >> 16) & 31) != rs) return null;              // lui rs, high
        var high = lui & 0xFFFF;
        if (high != NearHigh && high != FarHigh) return null;
        var va = (int)(high << 16) + (short)(addiu & 0xFFFF);
        return StringAt((uint)va) is { } text ? (high, va, text) : null;
    }

    private void RequireShops()
    {
        if (ShopsProblem() is { } problem) throw new InvalidOperationException(problem);
    }

    /// <summary>Every item every shop sells, in the order the game adds them.</summary>
    public List<ShopEntry> GetShopEntries()
    {
        RequireShops();
        return ShopSlots.Select((s, i) => new ShopEntry
        {
            Number = i,
            Stock = s.Stock,
            List = s.List,
            Text = Decode(s)!.Value.Text,
            AnyText = s.OwnLui || Decode(s)!.Value.High == NearHigh
        }).ToList();
    }

    /// <summary>The item texts an entry that isn't <see cref="ShopEntry.AnyText"/> can use.</summary>
    public List<string> NearbyShopTexts(IReadOnlyList<GameItem> items)
    {
        var list = new List<string>();
        var lo = FileOffset((int)(FarHigh << 16) - 0x8000);
        var hi = Math.Min(_data.Length, FileOffset((int)(FarHigh << 16) + 0x8000));
        for (var at = lo + 1; at < hi; at++)
        {
            if (_data[at - 1] != 0 || _data[at] == 0) continue;
            var text = StringAt((uint)(at + VaToFile));
            if (text != null && ItemText.Parse(text, items).IsValid && !list.Contains(text)) list.Add(text);
        }

        return list;
    }

    /// <summary>
    /// Changes what shops sell: <paramref name="texts"/> maps entry numbers to new item text. Each text
    /// must be item text the game reads without changes, of the kind of its list. Throws
    /// <see cref="ArgumentException"/> (and changes nothing) when a text is wrong or the texts don't fit.
    /// </summary>
    public void SetShopTexts(IReadOnlyDictionary<int, string> texts)
    {
        RequireShops();
        var entries = GetShopEntries();
        var items = GetItems();
        var wanted = entries.Select(e => texts.TryGetValue(e.Number, out var t) ? t : e.Text).ToArray();
        if (wanted.SequenceEqual(entries.Select(e => e.Text))) return;

        foreach (var e in entries)
        {
            var text = wanted[e.Number];
            if (text == e.Text) continue;
            var parsed = ItemText.Parse(text, items);
            var where = $"{ShopStockName(e.Stock)}, {e.List} item {e.Number}";
            if (!parsed.IsValid) throw new ArgumentException($"{where}: {parsed.Problems[0]}");
            if (items[parsed.ItemIndex].Category != e.List)
                throw new ArgumentException($"{where}: \"{text}\" is a {items[parsed.ItemIndex].Category.ToString().ToLowerInvariant()}, not a {e.List.ToString().ToLowerInvariant()}.");
        }

        // the strings in the pool something other than a shop entry uses stay where they are
        var entryVas = new HashSet<int>(ShopSlots.Select(s => s.AddiuVa));
        var fixedRanges = new List<(int Start, int End)>();
        for (var va = ShopPoolStart; va < ShopPoolEnd;)
        {
            if (_data[FileOffset(va)] == 0) { va++; continue; }
            var text = StringAt((uint)va) ?? "";
            var others = DataReferences(va) + CodeReferences(va).Count(r => !entryVas.Contains(r));
            if (others > 0) fixedRanges.Add((va, va + text.Length + 1));
            va += text.Length + 1;
        }

        var free = new List<(int Start, int End)>();
        var cursor = ShopPoolStart;
        foreach (var (start, end) in fixedRanges)
        {
            if (start > cursor) free.Add((cursor, start));
            cursor = end;
        }

        if (cursor < ShopPoolEnd) free.Add((cursor, ShopPoolEnd));
        bool InFree(int va) => free.Any(f => va >= f.Start && va < f.End);

        // where each entry's text goes: existing text nearby, or a new string in the pool
        var target = new int[entries.Count];
        var high = new uint[entries.Count];
        var needed = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            var slot = ShopSlots[i];
            var current = Decode(slot)!.Value;
            var text = wanted[i];
            if (current.High == FarHigh)
            {
                var far = FindString(text, FarHigh, _ => true);
                if (far >= 0)
                {
                    target[i] = far;
                    high[i] = FarHigh;
                    continue;
                }

                if (!slot.OwnLui)
                    throw new ArgumentException($"{ShopStockName(slot.Stock)}, {slot.List} item {i}: this entry can only sell " +
                                                string.Join(", ", NearbyShopTexts(items)) + ".");
            }

            high[i] = NearHigh;
            var near = FindString(text, NearHigh, va => !InFree(va));
            if (near >= 0) target[i] = near;
            else if (!needed.Contains(text)) needed.Add(text);
        }

        // pack the new strings (8-aligned when they fit, else byte by byte)
        var placed = Pack(needed, free, 8) ?? Pack(needed, free, 1);
        if (placed == null)
        {
            var room = free.Sum(f => f.End - f.Start);
            var need = needed.Sum(t => t.Length + 1);
            throw new ArgumentException($"The shops' item texts need {need - room} more bytes than there is room for; " +
                                        "shorten some, or use item names on their own (they cost nothing).");
        }

        foreach (var (start, end) in free) Array.Clear(_data, FileOffset(start), end - start);
        foreach (var (text, va) in placed) Encoding.ASCII.GetBytes(text).CopyTo(_data, FileOffset(va));
        for (var i = 0; i < entries.Count; i++)
        {
            var slot = ShopSlots[i];
            if (target[i] == 0) target[i] = placed[wanted[i]];
            var low = target[i] - (int)(high[i] << 16);
            if (low is < short.MinValue or > short.MaxValue) throw new InvalidOperationException("Shop text out of reach.");
            var addiu = FileOffset(slot.AddiuVa);
            WriteInt(addiu, (int)((ReadU32(addiu) & 0xFFFF0000) | (uint)(ushort)low));
            var lui = FileOffset(slot.LuiVa);
            if ((ReadU32(lui) & 0xFFFF) != high[i])
            {
                if (!slot.OwnLui) throw new InvalidOperationException("A shared lui can't be changed.");
                WriteInt(lui, (int)((ReadU32(lui) & 0xFFFF0000) | high[i]));
            }
        }

        ForgetReferences();
    }

    /// <summary>
    /// The address of a NUL-ended string equal to <paramref name="text"/> that a lui of
    /// <paramref name="high"/> reaches, or -1.
    /// </summary>
    private int FindString(string text, uint high, Func<int, bool> allowed)
    {
        var pattern = Encoding.ASCII.GetBytes(text + "\0");
        var lo = Math.Max(1, FileOffset((int)(high << 16) - 0x8000));
        var hi = Math.Min(_data.Length - pattern.Length, FileOffset((int)(high << 16) + 0x7FFF));
        for (var at = lo; at <= hi; at++)
        {
            if (_data[at - 1] != 0 || _data[at] != pattern[0]) continue;
            if (!_data.AsSpan(at, pattern.Length).SequenceEqual(pattern)) continue;
            var va = at + VaToFile;
            if (allowed(va)) return va;
        }

        return -1;
    }

    /// <summary>First fit, longest first, into the free ranges; null when they don't all fit.</summary>
    private static Dictionary<string, int>? Pack(List<string> texts, List<(int Start, int End)> free, int align)
    {
        var cursor = free.Select(f => f.Start).ToList();
        var result = new Dictionary<string, int>();
        foreach (var text in texts.OrderByDescending(t => t.Length).ThenBy(t => t, StringComparer.Ordinal))
        {
            var size = text.Length + 1;
            var done = false;
            for (var k = 0; k < free.Count && !done; k++)
            {
                var at = (cursor[k] + align - 1) / align * align;
                if (at + size > free[k].End) continue;
                result[text] = at;
                cursor[k] = at + size;
                done = true;
            }

            if (!done) return null;
        }

        return result;
    }
}
