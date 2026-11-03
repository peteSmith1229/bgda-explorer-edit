// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Names.cs
//
// English names for Dark Alliance II's characters and feats, as the game's text shows them.
// The executable only holds text keys ("AB_PowerAttackName"); the names come from the game's
// gtext.uni (matched feat by feat against the class feat tables of SLUS_206.75, and the
// DA1 heroes' lists of the three-player mod). Feat ids are the game's own numbers (the ones
// the class tables, the ability templates and the mod's price table use).
// ═══════════════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using System.Globalization;

namespace JetBlackEngineLib.Data.Executable;

public static class Da2Names
{
    /// <summary>Character class ids 0-9 (the lump / feat / stats tables are indexed by them).</summary>
    public static readonly string[] Classes =
    {
        "Vahn", "Adrianna", "Kromlech", "Drizzt", "Dorn", "Vhaidra", "Ysuran", "Borador", "Allessia", "Artemis"
    };

    /// <summary>What each class is.</summary>
    public static readonly string[] ClassTitles =
    {
        "Human Arcane Archer (DA1)", "Elven Sorceress (DA1)", "Dwarven Fighter (DA1)", "Drow Ranger",
        "Barbarian", "Monk", "Necromancer", "Rogue", "Cleric", "Assassin"
    };

    public static string ClassName(int id) => id >= 0 && id < Classes.Length ? Classes[id] : "class " + id;

    private static readonly Dictionary<int, string> FeatNames = new()
    {
        [0] = "Targetting", [1] = "Accuracy", [2] = "Bull Rush", [3] = "Great Fortitude", [4] = "Flame Arrow",
        [5] = "Exploding Arrows", [6] = "Hail of Arrows", [7] = "Inferno Arrow", [8] = "Magic Missile",
        [9] = "Burning Hands", [10] = "Melf's Acid Arrow", [11] = "Fireball", [12] = "Cure Light Wounds",
        [13] = "Snowblind", [14] = "Fire Shield", [15] = "Meteor Swarm", [16] = "Dodge", [17] = "Endurance",
        [18] = "Toughness", [19] = "Willpower", [20] = "Iron Will", [21] = "Combat Reflexes", [22] = "Death Blow",
        [23] = "Deflect Missiles", [24] = "Enchant Arrows", [25] = "Improved Critical", [26] = "Sword and Fist",
        [27] = "Improved Block", [28] = "Shield Expertise", [29] = "Whirlwind Attack", [30] = "Sorcerer's Blood",
        [31] = "Clangeddin's Fist", [32] = "Power Attack", [33] = "Lightning Bolt", [34] = "Ball Lightning",
        [35] = "Mordenkainen's Sword", [36] = "Shock Arrows", [37] = "Ice Arrows", [38] = "Otiluke's Icy Sphere",
        [39] = "Repulsion", [40] = "Arcane Quiver", [41] = "Two-Weapon Fighting", [42] = "Cleave", [43] = "Sunder",
        [44] = "Hero's Arm", [45] = "Barbarian Rage", [46] = "Shield Bash", [52] = "Heart of the Bear",
        [53] = "Sprint", [54] = "Heart of the Wolf", [55] = "Resist Elements", [56] = "Barkskin",
        [57] = "Hero's Mantle", [58] = "Animal Friendship", [59] = "Bale Arm", [60] = "Stealth", [61] = "Evasion",
        [62] = "Crushing Blow", [63] = "Diamond Body", [64] = "Piercing Strike", [65] = "Spinning Strike",
        [66] = "Stunning Blow", [67] = "Sweep Attack", [68] = "Unarmed Combat", [70] = "Arterial Strike",
        [71] = "Crippling Blow", [72] = "Poison", [73] = "Hail of Knives", [74] = "Contagion",
        [75] = "Dark Possession", [76] = "Enervation", [77] = "Fear", [78] = "Life Drain",
        [79] = "Ray of Enfeeblement", [80] = "Chill Touch", [81] = "Vampiric Touch", [82] = "Ghoul Touch",
        [83] = "Claws of Darkness", [84] = "Shadow Conjure", [85] = "Animate Dead", [86] = "Shadow Spray",
        [87] = "Shadow Shield", [88] = "Charm Humanoid", [89] = "Flame Arrow (Ysuran)", [90] = "Hold",
        [91] = "Slow", [92] = "Haste", [93] = "Ice Storm", [94] = "Shield", [95] = "Shocking Grasp",
        [96] = "Long Weapon Focus", [97] = "Great Weapon Focus", [98] = "Hand Weapon Focus",
        [99] = "Armor Proficiency", [100] = "Hail of Bolts", [101] = "Precise Shot", [102] = "Ransack",
        [103] = "Smokepowder Bolt", [104] = "Smokepowder Bomb", [105] = "Smokepowder Satchel",
        [107] = "Dwarven Warsong", [108] = "Bless", [109] = "Cure Wounds", [110] = "Divine Resilience",
        [111] = "Divine Strength", [112] = "Flame Strike", [113] = "Protective Ward", [114] = "Purity",
        [115] = "Sanctuary", [116] = "Spiritual Weapon", [117] = "Turn Undead", [119] = "Smite Evil",
        [120] = "Empower", [121] = "Clarity", [122] = "Alchemical Lore", [123] = "Enchant Item",
        [124] = "Forge Lore", [125] = "Ash Blade",
    };

    /// <summary>The feat's name, or "feat N" when the game has no feat with that id.</summary>
    public static string Feat(int id) =>
        FeatNames.TryGetValue(id, out var name) ? name : "feat " + id.ToString(CultureInfo.InvariantCulture);

    public static bool IsFeat(int id) => FeatNames.ContainsKey(id);

    /// <summary>Every named feat id, in order.</summary>
    public static IEnumerable<int> FeatIds => FeatNames.Keys;
}
