// ═══════════════════════════════════════════════════════════════════════════════
// GENERATED FILE — do not edit by hand. Made by make_worldexplorer_classes.py (three-player tools)
// from the stock SLES_506.72 (PAL, md5 34874fe1aaed80fe52ad3911a7f304aa).
// Drop into WorldExplorer/JetBlackEngineLib/Data/World/BgdaObjectClasses.cs
//
// Classes: the 186 names the object factory FUN_00121648 creates (186 pool calls), with the
//   object size it asks the pool allocator for and the archive (.lmp) the model comes from.
// Levels: the per-level work-buffer entry count from the level table at 0x002AB2D8.
// Disc archives: the 213 separate .LMP files on the disc and their sizes (the disc directory the
//   game keeps in memory, read from a save state).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

namespace JetBlackEngineLib.Data.World;

/// <summary>What an object class is, for grouping in the level budget.</summary>
public enum BgdaObjectKind
{
    /// <summary>A creature that fights the players (bosses included).</summary>
    Monster,
    /// <summary>A harmless creature (ambient rats, cats).</summary>
    Ambient,
    /// <summary>A townsperson or story character.</summary>
    Npc,
    /// <summary>A player start position.</summary>
    PlayerStart,
    /// <summary>A light the factory creates (character lights).</summary>
    Light,
    /// <summary>Props, doors, traps, triggers and other level objects.</summary>
    Object
}

/// <summary>One class the game's object factory creates.</summary>
/// <param name="Name">The class name as objects.ob spells it (the game compares it exactly).</param>
/// <param name="Size">Bytes the factory asks the object pools for.</param>
/// <param name="Archive">The archive the model comes from, or null when the class has none of its own.</param>
public sealed record BgdaObjectClass(string Name, int Size, string? Archive, BgdaObjectKind Kind);

public static class BgdaObjectClasses
{
    /// <summary>Every class the object factory creates, in the factory's order.</summary>
    public static readonly IReadOnlyList<BgdaObjectClass> All = new BgdaObjectClass[]
    {
        new("charAmbient_Light", 60, null, BgdaObjectKind.Light),
        new("charDirectional_Light", 60, null, BgdaObjectKind.Light),
        new("charDirectional_LightD", 60, null, BgdaObjectKind.Light),
        new("Bartender", 688, "bartend.lmp", BgdaObjectKind.Npc),
        new("ShopKeep", 688, "bartley.lmp", BgdaObjectKind.Npc),
        new("Ethon", 688, "ethon.lmp", BgdaObjectKind.Npc),
        new("Fayed", 688, "fayed.lmp", BgdaObjectKind.Npc),
        new("Garik", 688, "garik.lmp", BgdaObjectKind.Npc),
        new("Bartley", 688, "bartley.lmp", BgdaObjectKind.Npc),
        new("Drunk", 688, "drunk.lmp", BgdaObjectKind.Npc),
        new("Lecher", 688, "nebbish.lmp", BgdaObjectKind.Npc),
        new("Uncle", 688, "uncle.lmp", BgdaObjectKind.Npc),
        new("CaravanGuard", 688, "cguard.lmp", BgdaObjectKind.Npc),
        new("Hoochie", 688, "hoochie.lmp", BgdaObjectKind.Npc),
        new("Peyton", 688, "bartend.lmp", BgdaObjectKind.Npc),
        new("Branoch", 688, "branoch.lmp", BgdaObjectKind.Npc),
        new("Brogan", 688, "brogan.lmp", BgdaObjectKind.Npc),
        new("Torrgeir", 688, "torrgeir.lmp", BgdaObjectKind.Npc),
        new("Kolgrim", 688, "kolgrim.lmp", BgdaObjectKind.Npc),
        new("Thief1", 688, "thief1.lmp", BgdaObjectKind.Npc),
        new("Thief2", 688, "thief2.lmp", BgdaObjectKind.Npc),
        new("Thief3", 688, "thief3.lmp", BgdaObjectKind.Npc),
        new("NPCGuard1", 688, "watchmen.lmp", BgdaObjectKind.Npc),
        new("NPCGuard2", 688, "watchmen.lmp", BgdaObjectKind.Npc),
        new("Sleyvas", 688, "sleyvas.lmp", BgdaObjectKind.Npc),
        new("Keledon", 688, "keledon.lmp", BgdaObjectKind.Npc),
        new("Guard", 688, "watchmen.lmp", BgdaObjectKind.Npc),
        new("Player1", 60, null, BgdaObjectKind.PlayerStart),
        new("AmbientRat", 656, "ratsmall.lmp", BgdaObjectKind.Ambient),
        new("Beholder", 1008, "beholder.lmp", BgdaObjectKind.Monster),
        new("BlackSteelGolem", 672, "g_bsteel.lmp", BgdaObjectKind.Monster),
        new("BrassGolem", 664, "g_brass.lmp", BgdaObjectKind.Monster),
        new("BronzeGolem", 664, "g_bronze.lmp", BgdaObjectKind.Monster),
        new("BugBear", 664, "bugbear.lmp", BgdaObjectKind.Monster),
        new("Bulette", 680, "bulette.lmp", BgdaObjectKind.Monster),
        new("Cat", 688, "cat.lmp", BgdaObjectKind.Ambient),
        new("DisplacerBeast", 672, "dbeast.lmp", BgdaObjectKind.Monster),
        new("Dragon", 744, "dragon.lmp", BgdaObjectKind.Monster),
        new("DrowFemale", 680, "drow_f.lmp", BgdaObjectKind.Monster),
        new("DrowMale", 664, "drow_m.lmp", BgdaObjectKind.Monster),
        new("DrowRider", 672, "drow_r.lmp", BgdaObjectKind.Monster),
        new("Eldrith", 1000, "eldrith.lmp", BgdaObjectKind.Monster),
        new("FireElemental", 720, "fireele.lmp", BgdaObjectKind.Monster),
        new("FrostGiant", 664, "fgiant.lmp", BgdaObjectKind.Monster),
        new("DeadDwarf", 688, "kolgrim.lmp", BgdaObjectKind.Npc),
        new("Gargoyle", 664, "gargoyle.lmp", BgdaObjectKind.Monster),
        new("GasSpore", 704, "spore.lmp", BgdaObjectKind.Monster),
        new("GelCube", 664, "cube.lmp", BgdaObjectKind.Monster),
        new("GiantRat", 696, "ratgiant.lmp", BgdaObjectKind.Monster),
        new("GhostMale", 656, "gtman1.lmp", BgdaObjectKind.Monster),
        new("Gnoll", 672, "gnoll.lmp", BgdaObjectKind.Monster),
        new("Karne", 672, "karne.lmp", BgdaObjectKind.Monster),
        new("Kobold", 664, "kobold.lmp", BgdaObjectKind.Monster),
        new("KoboldShaman", 664, "kshaman.lmp", BgdaObjectKind.Monster),
        new("Lilivarra", 744, "lilvarra.lmp", BgdaObjectKind.Monster),
        new("Lizardman", 688, "lizardm.lmp", BgdaObjectKind.Monster),
        new("Minotaur", 664, "minotaur.lmp", BgdaObjectKind.Monster),
        new("Ogre", 664, "ogre.lmp", BgdaObjectKind.Monster),
        new("OnyxGolem", 728, "g_onyx.lmp", BgdaObjectKind.Monster),
        new("Sessth", 664, "sessth.lmp", BgdaObjectKind.Monster),
        new("Skeleton", 672, "skeleton.lmp", BgdaObjectKind.Monster),
        new("Slime", 664, "slime.lmp", BgdaObjectKind.Monster),
        new("SmallSpider", 664, "spider.lmp", BgdaObjectKind.Monster),
        new("Spider", 664, "spider.lmp", BgdaObjectKind.Monster),
        new("SteelGolem", 664, "g_steel.lmp", BgdaObjectKind.Monster),
        new("TownDwarf", 680, "dwarf_m.lmp", BgdaObjectKind.Npc),
        new("TownFemale1", 672, "twoman1.lmp", BgdaObjectKind.Npc),
        new("TownFemale2", 680, "twoman2.lmp", BgdaObjectKind.Npc),
        new("TownMale1", 680, "tman1.lmp", BgdaObjectKind.Npc),
        new("Thief", 672, "thief.lmp", BgdaObjectKind.Monster),
        new("Umberhulk", 664, "uhulk.lmp", BgdaObjectKind.Monster),
        new("Wisp", 1024, "wisp.lmp", BgdaObjectKind.Monster),
        new("Wolf", 680, "wolf.lmp", BgdaObjectKind.Monster),
        new("Yeti", 672, "yeti.lmp", BgdaObjectKind.Monster),
        new("Zombie", 680, "zombie.lmp", BgdaObjectKind.Monster),
        new("Torch", 120, null, BgdaObjectKind.Object),
        new("Campfire", 120, null, BgdaObjectKind.Object),
        new("Firepot", 120, null, BgdaObjectKind.Object),
        new("Firepot2", 120, null, BgdaObjectKind.Object),
        new("OilLamp", 120, null, BgdaObjectKind.Object),
        new("Candle", 128, null, BgdaObjectKind.Object),
        new("Candle2", 128, null, BgdaObjectKind.Object),
        new("Candle3", 64, null, BgdaObjectKind.Object),
        new("Lantern", 132, null, BgdaObjectKind.Object),
        new("WaterSpout", 76, null, BgdaObjectKind.Object),
        new("Steam", 100, null, BgdaObjectKind.Object),
        new("MissileTrap", 64, null, BgdaObjectKind.Object),
        new("arrowShooter", 72, null, BgdaObjectKind.Object),
        new("fireballShooter", 72, null, BgdaObjectKind.Object),
        new("Shooter", 72, null, BgdaObjectKind.Object),
        new("UserParamProp", 352, null, BgdaObjectKind.Object),
        new("SackSpill", 80, "sackburl.lmp", BgdaObjectKind.Object),
        new("Chest", 120, "chest.lmp", BgdaObjectKind.Object),
        new("Chest2", 120, "chest2.lmp", BgdaObjectKind.Object),
        new("Chest3", 120, "chest3.lmp", BgdaObjectKind.Object),
        new("WickBasketA", 120, "baskets.lmp", BgdaObjectKind.Object),
        new("WickBasketB", 120, "baskets.lmp", BgdaObjectKind.Object),
        new("OnyxPotA", 116, "onyxl1.lmp", BgdaObjectKind.Object),
        new("OnyxPotB", 116, "onyxl1.lmp", BgdaObjectKind.Object),
        new("OnyxPotC", 116, "onyxl1.lmp", BgdaObjectKind.Object),
        new("OnyxPotD", 116, "onyxl1.lmp", BgdaObjectKind.Object),
        new("OnyxChest", 120, "ox_chest.lmp", BgdaObjectKind.Object),
        new("OnyxPalletEmpty", 116, "ox_plt.lmp", BgdaObjectKind.Object),
        new("OnyxPalletLeaned", 116, "ox_plt.lmp", BgdaObjectKind.Object),
        new("OnyxCrateLarge", 116, "ox_plt.lmp", BgdaObjectKind.Object),
        new("OnyxCrateSmall", 116, "ox_plt.lmp", BgdaObjectKind.Object),
        new("OnyxBarrel", 116, "ox_plt.lmp", BgdaObjectKind.Object),
        new("Barrel", 116, "barrel.lmp", BgdaObjectKind.Object),
        new("SnowBarrel", 116, "snobarel.lmp", BgdaObjectKind.Object),
        new("Basket", 116, "baskets2.lmp", BgdaObjectKind.Object),
        new("IceBarrel", 116, "icebarel.lmp", BgdaObjectKind.Object),
        new("IceChestLarge", 120, "icechst1.lmp", BgdaObjectKind.Object),
        new("IceCrate", 116, "icecrate.lmp", BgdaObjectKind.Object),
        new("SnowCrate", 116, "crateb.lmp", BgdaObjectKind.Object),
        new("HRBarrel", 116, "onyxhr.lmp", BgdaObjectKind.Object),
        new("PowderKeg", 116, "powderk.lmp", BgdaObjectKind.Object),
        new("PowderKegSnow", 116, "snowpkeg.lmp", BgdaObjectKind.Object),
        new("PotA1", 116, "pot.lmp", BgdaObjectKind.Object),
        new("PotA2", 116, "pot.lmp", BgdaObjectKind.Object),
        new("PotB1", 116, "pot.lmp", BgdaObjectKind.Object),
        new("PotB2", 116, "pot.lmp", BgdaObjectKind.Object),
        new("PotC", 116, "pot.lmp", BgdaObjectKind.Object),
        new("Crate", 116, "cratea.lmp", BgdaObjectKind.Object),
        new("HRCratea", 116, "onyxhr.lmp", BgdaObjectKind.Object),
        new("HRCrateb", 116, "onyxhr.lmp", BgdaObjectKind.Object),
        new("Stonejar", 116, "stonejar.lmp", BgdaObjectKind.Object),
        new("Stonejar2", 116, "stonejar.lmp", BgdaObjectKind.Object),
        new("UrnBronze", 116, "urns.lmp", BgdaObjectKind.Object),
        new("UrnSilver", 116, "urns.lmp", BgdaObjectKind.Object),
        new("UrnGold", 116, "urns.lmp", BgdaObjectKind.Object),
        new("Ice", 88, "icesheet.lmp", BgdaObjectKind.Object),
        new("Trigger", 92, null, BgdaObjectKind.Object),
        new("TriggerParams", 64, null, BgdaObjectKind.Object),
        new("PushTrigger", 60, null, BgdaObjectKind.Object),
        new("Prop", 176, null, BgdaObjectKind.Object),
        new("ItemProp", 192, null, BgdaObjectKind.Object),
        new("DoorSwing", 76, null, BgdaObjectKind.Object),
        new("DoorSecret", 76, null, BgdaObjectKind.Object),
        new("RollDoor", 76, "rolldoor.lmp", BgdaObjectKind.Object),
        new("Switch", 64, null, BgdaObjectKind.Object),
        new("FloorSwitch", 72, null, BgdaObjectKind.Object),
        new("FallPlatform", 96, null, BgdaObjectKind.Object),
        new("FallPlatformMaze", 96, null, BgdaObjectKind.Object),
        new("SaveCrystal", 184, "crystal.lmp", BgdaObjectKind.Object),
        new("Watcher", 224, "watcher.lmp", BgdaObjectKind.Object),
        new("PokeReflector", 60, null, BgdaObjectKind.Object),
        new("InitScriptLauncher", 60, null, BgdaObjectKind.Object),
        new("Flag", 136, "flag.lmp", BgdaObjectKind.Object),
        new("OnyxFlag1a", 136, "onyxl1.lmp", BgdaObjectKind.Object),
        new("OnyxFlag2a", 136, "onyxl2.lmp", BgdaObjectKind.Object),
        new("OnyxFlag3a", 136, "onyxhr.lmp", BgdaObjectKind.Object),
        new("OnyxFlag3b", 136, "onyxhr.lmp", BgdaObjectKind.Object),
        new("Lever", 68, "lever.lmp", BgdaObjectKind.Object),
        new("WeaponRack", 80, null, BgdaObjectKind.Object),
        new("MetalCrateLrg", 264, "mtlcrate.lmp", BgdaObjectKind.Object),
        new("MetalCrateSml", 264, "mtlcrate.lmp", BgdaObjectKind.Object),
        new("PushPhysicsProp", 264, null, BgdaObjectKind.Object),
        new("CryptPall", 72, "cryptpal.lmp", BgdaObjectKind.Object),
        new("Counter", 64, null, BgdaObjectKind.Object),
        new("Timer", 68, null, BgdaObjectKind.Object),
        new("Orb", 232, "orb.lmp", BgdaObjectKind.Object),
        new("ForceField", 76, null, BgdaObjectKind.Object),
        new("Lamp", 120, null, BgdaObjectKind.Object),
        new("SignalFire", 124, null, BgdaObjectKind.Object),
        new("Gold", 176, null, BgdaObjectKind.Object),
        new("Container", 116, null, BgdaObjectKind.Object),
        new("CosmeticProp", 80, null, BgdaObjectKind.Object),
        new("CosmeticPropAnim", 136, null, BgdaObjectKind.Object),
        new("NPC", 688, null, BgdaObjectKind.Npc),
        new("Teleporter", 656, null, BgdaObjectKind.Object),
        new("Blocker", 60, null, BgdaObjectKind.Object),
        new("BTube", 140, "btube.lmp", BgdaObjectKind.Object),
        new("GTube", 140, "gtube.lmp", BgdaObjectKind.Object),
        new("Bogtree", 136, "bogtree.lmp", BgdaObjectKind.Object),
        new("Tree", 136, "trees.lmp", BgdaObjectKind.Object),
        new("Hardwood", 136, "hardwood.lmp", BgdaObjectKind.Object),
        new("EverSnw", 136, "treepine.lmp", BgdaObjectKind.Object),
        new("EverGrn", 136, "treepine.lmp", BgdaObjectKind.Object),
        new("CemetaryBlock", 80, "cemetary.lmp", BgdaObjectKind.Object),
        new("CemetaryCorner", 80, "cemetary.lmp", BgdaObjectKind.Object),
        new("CemetaryGate", 80, "cemetary.lmp", BgdaObjectKind.Object),
        new("CemetaryWall", 80, "cemetary.lmp", BgdaObjectKind.Object),
        new("Bush", 136, "bush.lmp", BgdaObjectKind.Object),
        new("HutCloth", 136, "hutcloth.lmp", BgdaObjectKind.Object),
        new("WaterStair", 68, null, BgdaObjectKind.Object),
        new("VineDoor", 68, null, BgdaObjectKind.Object)
    };

    private static readonly Dictionary<string, BgdaObjectClass> ByName = Build();

    private static Dictionary<string, BgdaObjectClass> Build()
    {
        var map = new Dictionary<string, BgdaObjectClass>(StringComparer.Ordinal);
        foreach (var c in All) map.TryAdd(c.Name, c);
        return map;
    }

    /// <summary>The class the factory creates for <paramref name="name"/> (exact, case-sensitive), or null.</summary>
    public static BgdaObjectClass? Find(string name) => ByName.TryGetValue(name, out var c) ? c : null;

    /// <summary>
    /// Level table: (name, entries). The game uses the first entry whose name occurs in the
    /// level's name; the work buffer is entries x 4,784 bytes.
    /// </summary>
    public static readonly (string Name, int Entries)[] LevelTable =
    {
        ("tutor", 48),
        ("cuttown", 20),
        ("town", 20),
        ("tavern", 48),
        ("cellar", 48),
        ("sewer1", 48),
        ("sewer2", 48),
        ("crypt1", 1),
        ("crypt2", 1),
        ("thieves1", 1),
        ("thieves2", 48),
        ("thieves3", 48),
        ("bthrone", 48),
        ("windwalk", 48),
        ("minecamp", 48),
        ("burneye1", 48),
        ("burneye2", 48),
        ("burneye3", 48),
        ("burneye4", 48),
        ("smlcave", 48),
        ("sigtower", 48),
        ("snowpeak", 48),
        ("mine1", 48),
        ("mine2", 48),
        ("mine3", 48),
        ("burnfrst", 48),
        ("icecave1", 48),
        ("icecave2", 48),
        ("icecave3", 48),
        ("chapel", 48),
        ("swamp1", 48),
        ("swamp2", 56),
        ("chelimb", 78),
        ("chekeep1", 78),
        ("chekeep2", 78),
        ("bog", 78),
        ("temple", 48),
        ("gauntlet", 78),
        ("towerb", 48),
        ("tower1", 48),
        ("tower2", 48),
        ("tower3", 48),
        ("tower4", 48),
        ("towerh", 48),
        ("towert", 48)
    };

    /// <summary>
    /// The separate archives on the game disc (lower-case name -> file size), from the disc
    /// directory the game keeps in memory. An object whose archive is not in the level's GOB makes
    /// the game load it from here when the level starts.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> DiscArchives =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["act1tran.lmp"] = 2030712, ["act1trap.lmp"] = 2031024, ["act2tran.lmp"] = 2030512, ["act2trap.lmp"] = 2030952,
        ["act3tran.lmp"] = 2030584, ["act3trap.lmp"] = 2030896, ["barrel.lmp"] = 81344, ["bartend.lmp"] = 808040,
        ["bartley.lmp"] = 1732416, ["baskets.lmp"] = 169728, ["baskets2.lmp"] = 80432, ["bat.lmp"] = 18752,
        ["beholder.lmp"] = 2451804, ["bislogo.lmp"] = 2035911, ["bogtree.lmp"] = 89024, ["branoch.lmp"] = 1059464,
        ["brogan.lmp"] = 1143940, ["btube.lmp"] = 92176, ["bugbear.lmp"] = 974332, ["bulette.lmp"] = 1096288,
        ["bullfrog.lmp"] = 43968, ["bush.lmp"] = 105296, ["cantent1.lmp"] = 120880, ["cantent2.lmp"] = 120880,
        ["canvtent.lmp"] = 120880, ["cat.lmp"] = 105168, ["cemetary.lmp"] = 383008, ["cguard.lmp"] = 909336,
        ["chelobs.lmp"] = 635648, ["chest.lmp"] = 102400, ["chest2.lmp"] = 103616, ["chest3.lmp"] = 101648,
        ["config.lmp"] = 317264, ["cratea.lmp"] = 41920, ["crateb.lmp"] = 36288, ["cryptpal.lmp"] = 83024,
        ["crystal.lmp"] = 18784, ["cube.lmp"] = 442640, ["d_burn.lmp"] = 651600, ["d_night.lmp"] = 526448,
        ["d_swamp.lmp"] = 589776, ["dbeast.lmp"] = 476096, ["demo.lmp"] = 1235232, ["demomark.lmp"] = 42832,
        ["dragon.lmp"] = 2678240, ["drizz.lmp"] = 618703, ["drizz_fr.lmp"] = 614095, ["drizz_ge.lmp"] = 614991,
        ["drizz_it.lmp"] = 616783, ["drizz_sp.lmp"] = 613967, ["drow_f.lmp"] = 325580, ["drow_m.lmp"] = 668628,
        ["drow_r.lmp"] = 852828, ["drunk.lmp"] = 792400, ["dwarf.lmp"] = 645815, ["dwarf_d.lmp"] = 123616,
        ["dwarf_f.lmp"] = 223264, ["dwarf_fr.lmp"] = 639927, ["dwarf_ge.lmp"] = 639927, ["dwarf_it.lmp"] = 641463,
        ["dwarf_m.lmp"] = 470144, ["dwarf_sp.lmp"] = 640823, ["eldrith.lmp"] = 3542532, ["elf.lmp"] = 950480,
        ["elf_fr.lmp"] = 947664, ["elf_ge.lmp"] = 949968, ["elf_it.lmp"] = 944336, ["elf_sp.lmp"] = 951376,
        ["end.lmp"] = 2032280, ["env_def.lmp"] = 65872, ["env_ice.lmp"] = 65872, ["env_shll.lmp"] = 65872,
        ["env_sink.lmp"] = 65872, ["env_thv.lmp"] = 65872, ["env_twr.lmp"] = 65872, ["equipmnt.lmp"] = 1076736,
        ["ethon.lmp"] = 759824, ["fayed.lmp"] = 786280, ["fgiant.lmp"] = 2446752, ["fireele.lmp"] = 140720,
        ["flag.lmp"] = 25740, ["flint.lmp"] = 21024, ["forcedr.lmp"] = 78016, ["forestbe.lmp"] = 136624,
        ["frontend.lmp"] = 656912, ["fx.lmp"] = 1578416, ["g_brass.lmp"] = 502952, ["g_bronze.lmp"] = 591464,
        ["g_bsteel.lmp"] = 583404, ["g_onyx.lmp"] = 527152, ["g_steel.lmp"] = 1065456, ["gargoyle.lmp"] = 208688,
        ["garik.lmp"] = 2094204, ["gears.lmp"] = 29952, ["globsnd.lmp"] = 32800, ["gnoll.lmp"] = 465328,
        ["gtext.lmp"] = 345964, ["gtman1.lmp"] = 444024, ["gtube.lmp"] = 92176, ["hardwood.lmp"] = 50352,
        ["hoochie.lmp"] = 795152, ["horn.lmp"] = 28352, ["hud.lmp"] = 255584, ["human.lmp"] = 659800,
        ["human_fr.lmp"] = 658136, ["human_ge.lmp"] = 658520, ["human_it.lmp"] = 657624, ["human_sp.lmp"] = 658392,
        ["hutcloth.lmp"] = 16384, ["icebarel.lmp"] = 70640, ["icechst1.lmp"] = 99968, ["icecrate.lmp"] = 36800,
        ["icesheet.lmp"] = 162240, ["icon.lmp"] = 105072, ["icons.lmp"] = 114784, ["inventry.lmp"] = 1024416,
        ["karne.lmp"] = 1987376, ["keledon.lmp"] = 757664, ["kobold.lmp"] = 707648, ["kolgrim.lmp"] = 814224,
        ["kshaman.lmp"] = 361892, ["kveg.lmp"] = 55008, ["langmenu.lmp"] = 329248, ["legal.lmp"] = 617632,
        ["lever.lmp"] = 5916, ["lilvarra.lmp"] = 1096880, ["lizardm.lmp"] = 655616, ["load_fr.lmp"] = 113280,
        ["load_gr.lmp"] = 72336, ["load_it.lmp"] = 114480, ["load_sp.lmp"] = 83488, ["loading.lmp"] = 98880,
        ["mainmenu.lmp"] = 7968367, ["mapicons.lmp"] = 22016, ["medalion.lmp"] = 21888, ["mine2.lmp"] = 280912,
        ["minedoor.lmp"] = 58332, ["minotaur.lmp"] = 443232, ["mouth.lmp"] = 88652, ["mpgs.lmp"] = 488,
        ["mtlcrate.lmp"] = 98784, ["nebbish.lmp"] = 817280, ["ogre.lmp"] = 1015904, ["oilcan.lmp"] = 32528,
        ["onyxhr.lmp"] = 194432, ["onyxl1.lmp"] = 283696, ["onyxl2.lmp"] = 30400, ["orb.lmp"] = 307152,
        ["ox_chest.lmp"] = 159936, ["ox_plt.lmp"] = 249360, ["pbislogo.lmp"] = 2031989, ["pend.lmp"] = 2029158,
        ["plegal.lmp"] = 873168, ["pmainmnu.lmp"] = 8545625, ["porko.lmp"] = 2142176, ["pot.lmp"] = 110848,
        ["powderk.lmp"] = 84160, ["pporko.lmp"] = 2138848, ["pwotclog.lmp"] = 2403488, ["ratgiant.lmp"] = 193300,
        ["ratsmall.lmp"] = 26920, ["rolldoor.lmp"] = 82240, ["rottrees.lmp"] = 57664, ["sackburl.lmp"] = 14752,
        ["sessth.lmp"] = 1262832, ["shop.lmp"] = 362272, ["skeleton.lmp"] = 511392, ["sleyvas.lmp"] = 817136,
        ["slime.lmp"] = 261904, ["snobarel.lmp"] = 68992, ["snowflag.lmp"] = 21648, ["snowpkeg.lmp"] = 101376,
        ["soundseq.lmp"] = 35440, ["spicebox.lmp"] = 50192, ["spider.lmp"] = 495160, ["spore.lmp"] = 298624,
        ["start.lmp"] = 1774864, ["stonejar.lmp"] = 281840, ["thief.lmp"] = 392528, ["thief1.lmp"] = 556448,
        ["thief2.lmp"] = 493096, ["thief3.lmp"] = 649240, ["thieves2.lmp"] = 1852352, ["title.lmp"] = 391566,
        ["tkarne.lmp"] = 905976, ["tman1.lmp"] = 533624, ["tman2.lmp"] = 681080, ["torch.lmp"] = 17600,
        ["torrgeir.lmp"] = 1004076, ["treepine.lmp"] = 103808, ["trees.lmp"] = 221536, ["twoman1.lmp"] = 312880,
        ["twoman2.lmp"] = 237696, ["uhulk.lmp"] = 777840, ["uncle.lmp"] = 735776, ["urns.lmp"] = 37376,
        ["vines.lmp"] = 209648, ["w_cellar.lmp"] = 5584, ["w_chapel.lmp"] = 5584, ["w_chelim.lmp"] = 5584,
        ["w_forge.lmp"] = 5584, ["w_green.lmp"] = 5584, ["w_swamp.lmp"] = 5584, ["w_tower.lmp"] = 5584,
        ["watcher.lmp"] = 304308, ["watchmen.lmp"] = 1699256, ["waterst.lmp"] = 35136, ["wine.lmp"] = 14112,
        ["wisp.lmp"] = 27968, ["wolf.lmp"] = 325392, ["wotclogo.lmp"] = 2398880, ["yeti.lmp"] = 518480,
        ["zombie.lmp"] = 863360
    };

    /// <summary>Entries when no level-table name matches.</summary>
    public const int DefaultLevelEntries = 48;

    /// <summary>The level table's entry count for a level (e.g. "burnfrst"), matched as the game does.</summary>
    public static int LevelEntries(string levelName)
    {
        foreach (var (name, entries) in LevelTable)
        {
            if (levelName.Contains(name, StringComparison.Ordinal)) return entries;
        }

        return DefaultLevelEntries;
    }
}
