// ═══════════════════════════════════════════════════════════════════════════════
// ARMOR.YAK (Baldur's Gate: Dark Alliance, PAL SLES_506.72): what the game expects.
//
// The file holds the armour characters wear: entries of four children (one per character
// class: 0 human, 1 elf, 2 dwarf, 3 class 3), each a skinned VIF model followed by its
// 256-colour texture. An item's record +0x28 picks the entry; +1 is its enchanted look.
//
// The game reads the 0x1000-byte header once (FUN_0015BA60) and streams one child from
// disc into a fixed buffer per player and slot whenever equipment changes (FUN_0015BA80).
// Players 1 and 2 have no size check, so a child bigger than its buffer would overwrite
// the next one; the three-player hook refuses entry >= 85 and children that don't fit.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Armour;

/// <summary>Which equipment slot (and so which buffer) an ARMOR.YAK entry is worn in.</summary>
public enum ArmorSlot
{
    /// <summary>An entry added after the game's 59: the slot depends on the item pointing at it.</summary>
    Unknown,
    Body,
    Boots,
    Gloves,
    Helmet
}

public static class ArmorYakInfo
{
    /// <summary>Bytes the game reads for the header (0x0053D180).</summary>
    public const int HeaderSize = 0x1000;

    /// <summary>Four children of {u32 VIF length, u32 file offset, u32 total length}.</summary>
    public const int EntrySize = 0x30;

    /// <summary>85 × 0x30 fits in the header; the three-player hook refuses entry 85 and above.</summary>
    public const int MaxEntries = 85;

    /// <summary>Children are read in whole CD sectors.</summary>
    public const int Sector = 0x800;

    /// <summary>The entries of the shipped file.</summary>
    public const int ShippedEntries = 59;

    public static readonly IReadOnlyList<string> Characters = new[] { "Human", "Elf", "Dwarf", "Class 3" };

    /// <summary>The buffer a child of this slot is read into (the same for all three players).</summary>
    public static int Capacity(ArmorSlot slot) => slot switch
    {
        ArmorSlot.Body => 0x34800,
        ArmorSlot.Boots => 0xA000,
        ArmorSlot.Gloves => 0xA000,
        ArmorSlot.Helmet => 0x6800,
        _ => 0x34800
    };

    /// <summary>Bytes the game reads for a child of <paramref name="length"/> bytes.</summary>
    public static int ReadSize(int length) => (length + Sector - 1) & ~(Sector - 1);

    private static readonly (int First, string Set)[] Sets =
    {
        (3, "Leather"), (11, "Chain"), (19, "Plate"), (27, "Padded"), (35, "Studded leather"), (43, "Scale"),
        (51, "Half-plate")
    };

    private static readonly ArmorSlot[] SetSlots = { ArmorSlot.Body, ArmorSlot.Boots, ArmorSlot.Gloves, ArmorSlot.Helmet };

    /// <summary>What a shipped entry is, or null for an added one.</summary>
    public static (string Set, ArmorSlot Slot, bool Enchanted)? ShippedEntry(int entry)
    {
        switch (entry)
        {
            case 0: return ("Clothes", ArmorSlot.Body, false);
            case 1: return ("Clothes", ArmorSlot.Boots, false);
            case 2: return ("Clothes", ArmorSlot.Gloves, false);
        }

        foreach (var (first, set) in Sets)
        {
            var k = entry - first;
            if (k >= 0 && k < 8) return (set, SetSlots[k / 2], (k & 1) == 1);
        }

        return null;
    }

    public static ArmorSlot SlotOf(int entry) => ShippedEntry(entry)?.Slot ?? ArmorSlot.Unknown;

    // The enchant glow (ArmorModel.GlowFlag). FUN_00174A50 picks a glow for an item when its power (FUN_00174098,
    // ItemFormulas.Power: the +N, 7 more for an odd +N, and points for most modifiers) is above 0 and it isn't Flaming
    // or Flaming Burst, at strength min(1, 0.6 + 0.025 x power). Worn, a piece shows it only when its model has the flag.

    /// <summary>The enchant glow line of a piece's Details.</summary>
    public static string GlowText(bool glows, int entry)
    {
        if (!glows) return "off, as on every shipped piece (right-click › Glow When Enchanted)";
        return "on: worn, it glows as an enchanted weapon does when the item has power (a +N or most modifiers; not " +
               "Flaming or Flaming Burst), brighter with more" + (GlowNote(entry) is { } note ? ". " + note : "");
    }

    /// <summary>When a glow on this entry shows less than on an enchanted look; null for an enchanted look.</summary>
    public static string? GlowNote(int entry) => ShippedEntry(entry) switch
    {
        { Enchanted: true } => null,
        null => "An added entry glows for whichever item points at it, when that item has power",
        _ => "Items show this look only without a +N, so it glows only for one given another modifier with power " +
             "(Frost, Shock, of Speed…)"
    };

    /// <summary>"Plate body", "Plate body (enchanted)" or "Added entry".</summary>
    public static string EntryName(int entry)
    {
        if (ShippedEntry(entry) is not { } e) return "Added entry";
        return $"{e.Set} {e.Slot.ToString().ToLowerInvariant()}" + (e.Enchanted ? " (enchanted)" : "");
    }

    // Hidden body parts: bit n of a model's u64 at +0x00 hides mesh n of the wearer's own model
    // (human.lmp man.vif, elf.lmp elf.vif, dwarf.lmp dwarf.vif). FUN_0015BEB8 clears those bits
    // from the character's +0x148 every frame while the piece is worn. Names read off renders of
    // each mesh; bits 6 and 10 of the human were confirmed in the game.
    private static readonly string[] HumanParts =
    {
        "eyes", "mouth", "scalp", "ears", "shoulders and chest", "upper arms", "forearms and hands", "torso and legs",
        "feet", "neck", "face"
    };

    private static readonly string[] ElfParts =
    {
        "face", "scalp", "chest and hips", "forearms and hands", "shins", "body (torso, arms, thighs)",
        "neck and head", "upper arms", "feet", "elbows"
    };

    private static readonly string[] DwarfParts = HumanParts.Append("beard").ToArray();

    /// <summary>The meshes of character <paramref name="child"/>'s own model, by bit.</summary>
    public static IReadOnlyList<string> BodyParts(int child) => child switch
    {
        0 => HumanParts,
        1 => ElfParts,
        _ => DwarfParts
    };

    /// <summary>"mouth, shoulders and chest, torso and legs" for a mask, or "none".</summary>
    public static string MaskText(ulong mask, int child)
    {
        var names = BodyParts(child);
        var parts = new List<string>();
        for (var bit = 0; bit < 64; bit++)
        {
            if ((mask >> bit & 1) == 0) continue;
            parts.Add(bit < names.Count ? names[bit] : $"bit {bit}");
        }

        return parts.Count == 0 ? "none" : string.Join(", ", parts);
    }
}
