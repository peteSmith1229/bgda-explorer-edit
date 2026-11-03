// ═══════════════════════════════════════════════════════════════════════════════
// GENERATED FILE — do not edit by hand. Made from the game's 107 level files (BG/DATA/*.gob of
// Dark Alliance II, USA) by the level budget scan of the three-player tools (scratch levelbudget.py,
// 2026-10-04, build 43 = the same memory use as build 51): what each level puts in the game's object
// pools and heap when it loads, before any creature is added.
//
// Pools (0x31A170): 1280 objects of up to 0xC0 bytes (props, chests, torches, gold piles ...), 16 of
// 0x300 (dropped items, placed items), 192 of 0x400 and 64 of 0x600 (creatures, the three players,
// summons). Creature-sized = the 0x400 and 0x600 slots in use at load, players included; a full pool
// crashes the game (the drop code and the factories don't check for it), so keep at most ~200 at
// load (56 spare for summons, portal / container spawns and drops beyond the 16).
// Heap: KB (1000 bytes) free after everything the level can load, with the heaviest possible party
// (Adrianna + Ysuran + Dorn, 2719 KB of character lumps). Lumps: loaded at once (the table holds 128).
// ═══════════════════════════════════════════════════════════════════════════════
namespace JetBlackEngineLib.Data.Executable;

/// <summary>A level of the stock game at load: creatures, pools in use, free heap.</summary>
public sealed record Da2LevelBudget(string Level, int Act, int Creatures, int Corpses, int Small, int Medium, int Large,
    int Huge, int HeapFreeKb, int Lumps, int PortalSpawns, int ContainerSpawns)
{
    public const int SmallSlots = 1280, ItemSlots = 16, CreatureSlots = 192 + 64, SafeCreatureSlots = 200, LumpSlots = 128;

    /// <summary>The 0x400 and 0x600 slots in use at load (creatures, players and other big objects).</summary>
    public int CreatureSized => Large + Huge;

    /// <summary>How many more creature-sized objects fit at load while keeping 56 slots spare.</summary>
    public int Room => SafeCreatureSlots - CreatureSized;

    public static Da2LevelBudget? Find(string level)
    {
        foreach (var b in All)
        {
            if (string.Equals(b.Level, level, System.StringComparison.OrdinalIgnoreCase)) return b;
        }

        return null;
    }

    public static readonly Da2LevelBudget[] All =
    {
        new("ahome", 3, 0, 0, 104, 1, 1, 3, 6990, 24, 0, 0),
        new("air1", 3, 73, 0, 70, 7, 44, 32, 6247, 27, 0, 0),
        new("air2", 3, 7, 0, 11, 0, 7, 3, 8002, 24, 0, 0),
        new("attic", 1, 34, 0, 37, 0, 34, 3, 10148, 25, 0, 0),
        new("batcave", 1, 26, 0, 10, 0, 26, 3, 11707, 23, 0, 0),
        new("battleb", 4, 158, 0, 57, 6, 158, 3, 2939, 29, 0, 0),
        new("battleb2", 4, 12, 0, 26, 0, 12, 3, 3029, 25, 0, 0),
        new("bgate", 1, 0, 0, 117, 1, 49, 3, 2517, 28, 0, 0),
        new("bgate2", 1, 0, 0, 6, 0, 2, 3, 8852, 22, 0, 0),
        new("bgate3", 1, 0, 0, 4, 0, 15, 3, 6842, 24, 0, 0),
        new("bgchurch", 1, 0, 0, 7, 0, 4, 3, 10154, 24, 0, 0),
        new("bginn", 1, 0, 0, 8, 1, 12, 3, 2108, 30, 0, 0),
        new("bgmanor", 1, 0, 0, 39, 1, 3, 3, 5247, 28, 0, 0),
        new("bhall", 2, 57, 0, 106, 1, 78, 3, 5432, 29, 0, 0),
        new("blair", 3, 1, 0, 34, 0, 0, 4, 10196, 24, 0, 0),
        new("blood1", 1, 87, 0, 122, 5, 92, 3, 5384, 31, 0, 0),
        new("blood2", 1, 50, 0, 67, 1, 50, 3, 7791, 28, 0, 0),
        new("bruins", 2, 95, 0, 42, 5, 95, 3, 7433, 26, 0, 0),
        new("caravan", 1, 49, 4, 300, 2, 60, 3, 3234, 31, 0, 0),
        new("cellar", 1, 17, 0, 23, 0, 17, 3, 8308, 26, 0, 0),
        new("cliffs", 1, 46, 0, 63, 2, 46, 3, 7907, 27, 0, 0),
        new("cliffs2", 1, 38, 0, 248, 1, 38, 3, 6060, 27, 0, 0),
        new("dcastle", 2, 109, 18, 115, 2, 142, 3, 4389, 30, 0, 0),
        new("dcaverns", 2, 77, 3, 44, 12, 99, 3, 8702, 29, 0, 0),
        new("ddung1", 2, 80, 1, 185, 4, 81, 3, 2114, 30, 0, 0),
        new("ddung2", 2, 73, 0, 213, 2, 76, 3, 2200, 30, 13, 0),
        new("dlair", 2, 7, 0, 16, 0, 7, 3, 10439, 23, 0, 0),
        new("earth1", 3, 33, 0, 141, 3, 33, 3, 3289, 29, 0, 0),
        new("earth2", 3, 4, 0, 16, 0, 5, 3, 6329, 24, 1, 0),
        new("fire1", 3, 72, 0, 65, 7, 86, 3, 6136, 28, 0, 6),
        new("fire2", 3, 1, 0, 13, 0, 0, 4, 10914, 22, 0, 0),
        new("fwcat", 1, 83, 0, 99, 2, 83, 3, 2464, 29, 0, 14),
        new("fwcellar", 1, 69, 0, 133, 6, 70, 3, 1122, 32, 0, 0),
        new("fwdung", 1, 42, 0, 149, 3, 49, 3, 1416, 30, 31, 5),
        new("fwmanor1", 1, 0, 0, 83, 1, 0, 4, 6983, 26, 0, 0),
        new("fwmanor2", 1, 1, 0, 160, 1, 8, 3, 8395, 25, 0, 0),
        new("fwyrms", 2, 24, 0, 270, 1, 24, 3, 3175, 29, 0, 0),
        new("ghold", 3, 85, 0, 36, 3, 85, 3, 3512, 28, 0, 0),
        new("gmine", 3, 27, 3, 21, 2, 30, 3, 3980, 28, 0, 0),
        new("goblair", 1, 99, 0, 128, 3, 116, 3, 5611, 33, 0, 0),
        new("gobtres", 1, 10, 0, 28, 1, 11, 3, 7732, 28, 0, 0),
        new("guild1", 1, 71, 0, 95, 3, 76, 3, 3804, 29, 0, 0),
        new("guild2", 1, 7, 1, 31, 0, 9, 3, 4991, 29, 2, 0),
        new("hedge", 2, 33, 0, 108, 2, 33, 3, 3324, 30, 0, 0),
        new("hham1", 2, 83, 0, 65, 3, 83, 3, 4805, 29, 0, 0),
        new("hham2", 2, 71, 0, 92, 4, 71, 3, 3668, 26, 0, 0),
        new("hhamb", 2, 2, 0, 42, 0, 1, 4, 8265, 24, 0, 0),
        new("hmoor", 2, 24, 0, 81, 1, 25, 3, 7478, 28, 0, 0),
        new("inn", 1, 6, 0, 8, 0, 15, 3, 6789, 27, 0, 0),
        new("kpn1", 4, 43, 0, 105, 3, 43, 3, 347, 28, 28, 4),
        new("kpn2", 4, 42, 0, 96, 3, 42, 3, 2455, 27, 22, 5),
        new("kpn3", 4, 7, 0, 25, 0, 6, 4, 7689, 26, 0, 0),
        new("kpnd", 4, 47, 0, 132, 2, 50, 3, 3782, 28, 29, 0),
        new("kpns", 4, 0, 0, 4, 0, 0, 3, 6540, 22, 0, 0),
        new("lab1", 1, 55, 0, 147, 2, 56, 3, 4754, 29, 39, 0),
        new("lab2", 1, 19, 0, 88, 1, 22, 3, 4859, 29, 17, 0),
        new("lyran1", 2, 86, 0, 107, 3, 86, 3, 3696, 30, 19, 0),
        new("lyran2", 2, 125, 0, 108, 3, 125, 3, 1495, 31, 1, 0),
        new("lyranc", 2, 89, 0, 80, 3, 89, 3, 3113, 28, 0, 0),
        new("lyranf", 2, 1, 0, 108, 0, 1, 3, 4089, 25, 0, 0),
        new("lyrano", 2, 0, 0, 12, 0, 0, 3, 7486, 24, 3, 0),
        new("mausoleu", 3, 182, 0, 39, 4, 182, 3, 3818, 27, 1, 0),
        new("mgate", 4, 29, 0, 120, 1, 31, 3, 1990, 26, 0, 0),
        new("mgate2", 4, 1, 0, 99, 0, 1, 3, 8612, 21, 0, 0),
        new("monaster", 3, 27, 0, 104, 3, 26, 4, 3935, 28, 33, 6),
        new("ogrecave", 1, 22, 1, 15, 1, 23, 3, 9710, 25, 0, 0),
        new("onyx1", 4, 22, 0, 96, 2, 22, 3, 6823, 26, 0, 0),
        new("onyx2", 4, 18, 0, 91, 2, 18, 3, 4982, 27, 0, 0),
        new("onyx3", 4, 13, 0, 73, 2, 13, 3, 4311, 26, 0, 0),
        new("onyxtop", 4, 1, 0, 13, 0, 2, 3, 11010, 22, 0, 0),
        new("redbase", 1, 58, 0, 44, 3, 58, 3, 2326, 27, 0, 0),
        new("redcamp", 1, 65, 0, 702, 3, 65, 3, 3672, 29, 0, 0),
        new("redlair", 1, 23, 0, 34, 2, 23, 3, 5907, 28, 0, 0),
        new("redpass", 1, 36, 0, 35, 2, 36, 3, 4925, 29, 0, 0),
        new("rwoods", 3, 55, 0, 452, 3, 55, 3, 3544, 28, 0, 0),
        new("scave", 2, 53, 0, 81, 3, 53, 3, 6712, 28, 0, 0),
        new("sewers", 1, 107, 0, 206, 8, 107, 3, 4075, 34, 0, 0),
        new("sgcave1", 2, 46, 1, 22, 3, 47, 3, 6384, 27, 0, 0),
        new("sgcave2", 2, 47, 0, 26, 7, 47, 3, 4174, 27, 0, 0),
        new("sgcave21", 3, 50, 0, 39, 3, 50, 3, 3926, 30, 0, 0),
        new("sgcave22", 3, 50, 0, 25, 4, 50, 3, 4426, 27, 0, 0),
        new("sgcave23", 3, 23, 0, 16, 3, 23, 3, 4830, 26, 0, 0),
        new("sgcave24", 3, 27, 0, 13, 3, 27, 3, 6259, 27, 0, 0),
        new("sgcave25", 3, 18, 0, 17, 13, 18, 3, 3698, 25, 0, 0),
        new("sgcave3", 2, 34, 0, 12, 3, 34, 3, 4881, 25, 0, 0),
        new("sgcave4", 2, 21, 0, 11, 3, 21, 3, 6933, 25, 0, 0),
        new("sgcave5", 2, 19, 0, 18, 7, 19, 3, 3545, 27, 0, 0),
        new("sgorge", 2, 59, 2, 137, 5, 61, 3, 4006, 28, 0, 0),
        new("sgorge2", 3, 67, 0, 128, 11, 67, 3, 1860, 29, 0, 0),
        new("shadow", 4, 3, 0, 7, 0, 2, 4, 7699, 25, 0, 0),
        new("spidlair", 1, 15, 0, 14, 2, 15, 3, 11638, 21, 0, 0),
        new("stemp1", 2, 89, 0, 35, 3, 89, 3, 5967, 23, 0, 0),
        new("stemp2", 2, 62, 0, 91, 4, 62, 3, 4148, 25, 0, 0),
        new("tclaws", 2, 12, 0, 69, 2, 12, 3, 9970, 26, 9, 0),
        new("teeth", 1, 34, 0, 54, 3, 34, 3, 5608, 29, 0, 0),
        new("teeth2", 2, 46, 0, 61, 3, 46, 3, 4911, 29, 0, 0),
        new("teeth3", 3, 32, 0, 52, 3, 32, 3, 4305, 30, 0, 0),
        new("trollb", 1, 59, 0, 1057, 3, 61, 3, 7420, 37, 0, 0),
        new("village", 1, 45, 0, 80, 2, 71, 3, 1734, 28, 0, 0),
        new("water1", 3, 67, 0, 43, 3, 67, 3, 7376, 28, 0, 0),
        new("water2", 3, 6, 0, 14, 0, 6, 3, 10348, 25, 0, 0),
        new("whouse", 1, 53, 0, 97, 6, 56, 3, 4744, 30, 0, 0),
        new("whouse2", 2, 0, 0, 9, 1, 1, 3, 7289, 23, 0, 0),
        new("zattack", 2, 23, 0, 106, 1, 23, 3, 6560, 27, 0, 0),
        new("zbtower", 3, 9, 0, 78, 1, 9, 3, 7688, 30, 24, 0),
        new("zctower", 3, 50, 0, 745, 5, 49, 4, 4599, 33, 5, 0),
        new("zstudy", 3, 1, 0, 120, 1, 0, 4, 9575, 27, 0, 0),
    };
}
