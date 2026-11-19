// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.BorrowedSpells.cs
//
// BORROWED SPELLS (the Enhancements tab): eight spells the heroes learn from the game's monsters, the game's own
// vines that nothing in it casts, three feats the game has but never offers and three new ones of the patch's own,
// each a feat on a hero's level-up screen, each turned on or off by itself:
//   Xantam's Fire for Adrianna    the beholder Xantam's fire trails, which burn the monsters in their path;
//   Xantam's Orbs for Adrianna    the beholder's third attack: the rank's number of his glowing orbs (one at rank
//                                 1, up to five at rank 5) in a fan whose middle goes at the nearest monster in
//                                 front of her, each 8-12 damage to the first monster it touches, or breaking the
//                                 barrel, crate or pot it touches;
//   Dimension Door for Adrianna   Ilivarra's teleport: she vanishes in a burst of Ilivarra's purple and gold
//                                 sparks and reappears the rank's distance ahead (100, 150 or 200 at ranks
//                                 1-3), stopping short at walls, closed doors, ledges and the edge of the screen;
//   Sorcerer's Blood for Adrianna the game's own cut feat, passive: each level she gains once she has it gives
//                                 her 1 more mana a rank ("+1 Mana Point gain per level. This is not
//                                 retroactive."); 1 rank by default, up to 5;
//   Power Attack for Kromlech     the game's own cut feat: casting it readies his weapon, and his next hits
//                                 (one at rank 1, up to five at rank 5) within 10 seconds do double damage;
//   Blade Barrier for Kromlech    Ilivarra's nine whirling axes: once a second every monster in reach is hit;
//   Antimagic Field for Kromlech  Ilivarra's field: fire, cold, electricity and magic do him no harm;
//   Righteous Might for Kromlech  Ilivarra's might: he grows, his weapon hits do more damage and take less;
//   Inferno Arrow for Vahn        the game's own cut feat, made to work: an enchanted arrow that bursts into
//                                 flames where it strikes, burning every monster round it, then sets the
//                                 ground alight for a few seconds;
//   Entangle for Vahn             the game's own vines (VineSeek and VineSpell), which nothing in it ever casts:
//                                 the rank's number of thorny vines (one at rank 1, up to five at rank 5) crawl
//                                 along the ground from his feet, each to a different monster near him, the
//                                 nearest first, and wrap it: it walks slower, and the thorns hurt it every
//                                 second, from him;
//   Insect Plague for Vahn        the kobold shaman's insect swarm: the rank's number of swarms (one at rank 1, up
//                                 to five at rank 5) gather in front of him, each flies to a different monster
//                                 near him, the nearest first, and follows it, stinging it every second from him
//                                 and poisoning it with the game's own poison; when its monster dies it moves on
//                                 to the next;
//   Poison Gas Arrow for Vahn     the monsters' poison gas (the kobold shamans', the lizardmen's, the zombies'),
//                                 on an arrow: it bursts into a cloud of the game's own dark green gas where it
//                                 strikes, and once a second every monster in the cloud is hurt from him and
//                                 poisoned with the game's own poison; a thin wisp of the gas trails the arrow;
//   Expert Focus for Vahn         a new feat, Power Attack for his bow as a timed boost: for the rank's time (5 to
//                                 15 seconds at ranks 1-5) every arrow he shoots does 50% more damage and trails
//                                 Ilivarra's purple and gold sparks;
//   Phase Axe for Kromlech        a new feat: a magic axe thrown at the nearest monster in front of him, spinning
//                                 flat, that flies through every monster in its path, hitting each once, until a
//                                 wall or its range (400) stops it; more damage by rank;
//   Shatter for Adrianna          a new feat: a ring of frost bursts out all round her; every monster within its
//                                 reach takes a little cold damage and is frozen solid for a moment (1 to 3
//                                 seconds at ranks 1-5, as far as 60 to 100), barrels, crates and pots break,
//                                 and her spells wait a little longer than the freeze.
// It is featpatch.py version 17, written from the data in BgdaFeatPatch.cs (generated from that script), so
// the bytes are the ones tested in the emulator.
//
// POWER ATTACK is the game's: its ability record (0x0030E2E0, bit 34, cast type 18, 5 mana), its grant, its
// HUD icon and line ("Damage: Double Normal Melee Damage") and its texts in five languages were all left in,
// but no hero's list offers it and its cast case does nothing. The patch adds its list entry to Kromlech's
// list and its cast case, and doubles his hits in the melee hit (where Righteous Might raises them). Its
// mana is its record's, which the Feats & Spells tab edits; it needs nothing in GTEXT.LMP or ICONS.LMP.
//
// SORCERER'S BLOOD is the game's too: its record (0x0030E3D0, bit 32, type 24, the passives') and its texts in
// five languages are in, but no list offers it and nothing reads its rank. The game works a hero's maximum
// mana out afresh (FUN_00167390: class, INT, level, Willpower), so a bonus that isn't retroactive needs a
// total kept from level to level: each level she gains with the feat adds her rank to the last rank byte of
// her record (+0x123, list entry 63's, which no list reaches; the game saves the whole record), and the
// maximum mana is the game's plus that total. Its list entry goes after Xantam's Fire in her list. It needs
// nothing in GTEXT.LMP or ICONS.LMP (a passive has no HUD slot).
//
// INFERNO ARROW is the game's too, cut before it worked: its record (0x0030DA20, bit 8, type 9: 1 mana and 1-6
// damage, never used), its HUD name and its texts in five languages ("Inferno Arrows": "This feat will enchant an
// arrow so that it erupts into flames on impact.") are in, but no list offers it, type 9 isn't one of the bow's
// arrow types and no arrow does anything on impact for it. The patch adds its list entry to Vahn's list, makes
// type 9 an arrow (kind 4, the one kind no arrow uses), puts the setting's mana and the rank's damage into its slot
// at every shot (the HUD shows the rank's too), and gives kind 4 Flaming Arrow's look and sound in flight. Where it
// strikes: Exploding Arrow's burst; every monster and breakable thing within the rank's reach (measured to its edge)
// takes the arrow's most damage within a third of the reach and less beyond it (at least 1), fire, from Vahn
// (resistances count, his kills give him the experience), and a monster it sets alight burns for the rank's time;
// then flames run out along the floor all round (the game's own fire trails, as far as the rank's spread) and
// burn there for that time, burning every monster in them five times a second. Never a player. Its icon,
// icon_infernoarrow.tex, goes in ICONS.LMP as the borrowed spells' do; it needs nothing in GTEXT.LMP.
//
// ENTANGLE uses the game's own vines, which nothing builds: a VineSeek (vtable 0x003174F0, constructor
// FUN_001BF690) crawls along the floor towards its monster trailing green sparks, and within 10 of it builds a
// VineSpell (FUN_001BF5A0), which follows the monster spiralling green sparks round it, slowed (the game's slow byte,
// which the chill uses too) and poisoned, until its time is up. Its cast case (type 36) sends the rank's number of
// seekers from his feet, each at a different monster within 240 of him (never a player, nothing flagged 4, nothing
// dead), the nearest first. The patch hooks the seeker's update (nothing else runs it): it crawls at 200 a second (the
// game's 100), and its VineSpell is made 0x60 bytes and given a vtable copy whose update keeps the monster at least the
// rank's percentage slower and, every second from a second after the wrap, hurts it with the rank's thorns from Vahn
// (piercing that a shield can't block; resistances count, his kills give him the experience), and whose destructor
// checks the monster is still there (the game's writes through a link that may be gone). Its poison is left as it was
// (the thorns are his instead), and the vine falls away when its monster dies. Its name, description and icon go in
// GTEXT.LMP and ICONS.LMP as the other borrowed spells'; its HUD line is the game's "Damage: %d-%d" (a thorn's). Its
// list entry goes after Inferno Arrow's in his list.
//
// INSECT PLAGUE is the kobold shaman's swarm (InsectPlagueSpell: vtable 0x0031B530, constructor FUN_001E0D58), built
// by the patch as the game's constructor builds it but without the shaman's sounds (his archive isn't loaded
// everywhere). Its cast case (type 37) sends the rank's number of swarms, each to a different monster within 240 of
// him (never a player, nothing flagged 4, nothing dead), the nearest one no swarm is on first. A swarm is a creature
// the game draws itself (black motes, then the insects): it gathers 24 in front of him for half a second, then follows
// its monster with the game's own walk, and every second it is on it, it stings it for the rank's damage from Vahn
// (piercing that a shield can't block; resistances count, his kills give him the experience) and poisons it with the
// game's own poison (5 seconds; 1 for a monster that resists poison, none for one immune to it). The poison's bites
// count as his too: the patch hooks the poison's bite (0x0011B758), so a monster his swarm poisoned is bitten with him
// as the attacker (alone that changes nothing, as every hero gets a kill's experience; with two or three players the
// killer gets the larger share). When its monster dies the swarm moves on to the nearest other monster within 240 of
// it; it disperses when its time is up or none is left. Its list entry, ability record and settings are in a third
// data block (the main one has no room for them); its name, description and icon go in GTEXT.LMP and ICONS.LMP as the
// other borrowed spells'; its HUD line is the game's "Damage: %d-%d" (a sting's). Its list entry goes after Entangle's
// in his list.
//
// POISON GAS ARROW is an arrow feat (type 38, kind 7: no arrow of the game's uses either), shot from the bow as the
// game's arrow feats are, with arrows. Its slot gets the rank's damage at every shot (its mana is its record's); the
// arrow sounds like Melf's Acid Arrow and trails a thin wisp of the monsters' poison gas (their dark green puffs). Where
// it strikes (a monster, a wall or the floor) the game's own poison gas cloud bursts (poisonGasSpell: vtable 0x00313140,
// constructor FUN_001A2260, as the kobold shamans, the lizardmen and the zombies make theirs), with the rank's reach
// and time and Melf's Acid Arrow's hit sound; its puffs, its drift and its end are the game's. Only its update is the
// patch's (a vtable copy): once a second, from the moment it bursts, every monster whose edge is within its reach on the
// ground (never a player, nothing flagged 4, nothing dead) takes the rank's gas damage from Vahn through the game's hit
// function, with the game's cloud's kind of hit, which poisons it with the game's own poison (5 seconds; 1 for a monster
// that resists poison, none for one immune to it); the monsters' clouds hit everything in them for 5, players too, and
// that hit is left out. The arrow's own hit does the rank's arrow damage, and the cloud's first hit poisons what it struck.
// The poison's bites count as his, as Insect Plague's do (the same hook and records). Its list entry, ability record and
// settings are in a fourth data block, laid out as the third; its name, description and icon go in GTEXT.LMP and
// ICONS.LMP as the other borrowed spells'; its HUD line is the game's "Damage: %d-%d" (the arrow's). Its list entry goes
// after Insect Plague's in his list.
//
// DIMENSION DOOR is Ilivarra's teleport (her FUN_001DEDE8 sparkle and FUN_0010CF50 step), given to Adrianna as a spell
// of 3 ranks (type 39). Its cast case works out where she lands: the rank's distance along the way she faces, cut short
// at the first of the screen's four edges she would cross (the game's own FUN_00183450 edges round her height, with
// the game's margins, from where the camera is), then by the game's own sweep of her circle (FUN_00148C20 with her
// walk's mask: walls, closed doors and steps higher than her walk climbs) 2 short of what would stop her walking, then
// 8 nearer a try until the floor there (the game's FUN_00147800 round her circle) is at most 12 above her feet and 32
// below (a ledge, a pit she can't land in, the level's end). With less than 16 of the way left she stays. Ilivarra's
// sparkle (50 of her purple and gold sparks on the vertices of the hero's body) bursts where she was and where she is,
// with Otiluke's Resilient Sphere's shimmer, and the game's own grid relink puts her in the cell she has come to. Its
// list entry, ability record and distances are in a fifth data block, laid out as the third (the distances 20-400 by
// rank); its name, description and icon go in GTEXT.LMP and ICONS.LMP as the other borrowed spells'; its HUD line is its
// own ("Distance: %d units"). Its list entry goes after Xantam's Orbs in her list.
//
// EXPERT FOCUS is the patch's own: Power Attack for Vahn's bow, as a timed boost (type 40, bit 51). Its cast case
// keeps, for his player, the frame his focus ends (now and the rank's time; casting it again starts the time again) and
// the rank's percentage, in the sixth data block, with Magic Missile's cast sound. The shot's three calls of the arrow
// constructor (0x0015F160, 0x0015F278 and 0x0015F354: a single arrow, and a multishot's first and further arrows) go
// through the patch first: while the shooter's focus lasts, each arrow's least and most damage are raised by the
// percentage, rounded ((x x (100 + p) + 50) / 100); then on to the constructor with every argument as it was. An arrow
// in flight whose shooter's focus lasts leaves one of Ilivarra's purple and gold sparks behind it each frame (in the
// arrows' flight hook, which Inferno Arrow and Poison Gas Arrow use too). A focus that would end more than 25.5 seconds
// off (the longest the settings allow) is from before a frame-count reset and is over. Its list entry, ability record
// and settings are in a sixth data block, laid out as the third; its name, description and icon go in GTEXT.LMP and
// ICONS.LMP as the other borrowed spells'; its HUD line is its own ("Arrows +%d%% damage for %d.%d seconds"). Its list
// entry goes after Poison Gas Arrow's in his list.
//
// PHASE AXE is the patch's own too: a magic axe for Kromlech (type 41, bit 52), thrown as the thieves throw their knives.
// Its cast case builds it with the game's own arrow constructor (kind 0, no sounds), with the model and texture of the
// game's axe4 (the one-handed double-bitted axe, from the weapons every level loads) and the rank's least and most damage,
// from his hand at the middle of the nearest monster in front of him (straight ahead and level when there is none), at
// 300 a second, his heading, him as its owner; then a vtable copy of the knives' (0x00319CE8) whose update, draw and
// touch are the patch's. Its update turns it (it spins flat) and leaves one of Ilivarra's lilac sparks where it is each
// frame, and moves it with the game's own FUN_0010E200; a wall or the floor ends it with the knives' dust and clang, and
// it vanishes in a ring of lilac sparks (with Mordenkainen's Sword's end sound) when its 67 frames (400 units) are up.
// Its touch hurts every creature and breakable thing it meets, once each (it remembers the last eight it hit), never a
// player: the game's hit function from him (magic that a shield can't block; his kills, his experience), with
// Mordenkainen's Sword's hit sound; from its first hit on it flies level. Its draw is the knives' with the matrix moved
// so that the middle of the axe's model is on the axe's place. Its cast sound is Mordenkainen's Sword's. Its list entry,
// ability record and settings are in a seventh data block, laid out as the third; its name, description and icon go in
// GTEXT.LMP and ICONS.LMP as the other borrowed spells'; its HUD line is the game's own "Damage: %d-%d" (each hit's).
// Its list entry goes after Righteous Might's in his list.
//
// SHATTER is the patch's own too: a ring of frost for Adrianna (type 42, bit 53). Its cast case sets her cooldown (the
// hero's own, +0x348, which all her spells wait for) to the rank's from the cast on, plays the game's ice-burst sound
// (expl_ice1-3, the one a frozen monster makes as it breaks) following her, sets up a particle type of its own the first
// time (the game's FUN_00153F10, as Vahn's Knockback sets up his ring's: icy blue to white, fading) and sends 128 of its
// particles out in a ring from her, at her waist, to the rank's reach in half a second. Then every creature and breakable
// thing whose edge is within the reach of her on the ground (never her, a player or anything flagged 4; a creature alive)
// takes the rank's damage from her through the game's hit function (cold magic that a shield can't block; her kills, her
// experience; a barrel, crate, pot, basket or pallet breaks), and every such creature that cold, magic and damage can
// touch is held for the rank's time (half for one that resists cold or magic): a record of the eighth data block's 32
// says until when. The hold itself is a hook at the end of the game's status effects (FUN_0011B3C8, which every
// creature's update calls every frame, at 0x0011B7D8): a held creature that is alive gets the slow byte 50, the game's
// own hold (as its vines hold: it doesn't walk, and its animation and clock stand still), and a chill of at least 2,
// which the game draws as blue; once its hold is over the game's own chill thaws it. A monster that dies frozen breaks
// into ice, as the game's chilled ones do. Its list entry, ability record, settings and run-time records are in an eighth
// data block, at the start of stretch 52; its name, description, HUD line ("Reach %d, frozen for %d.%d seconds") and
// icon go in GTEXT.LMP and ICONS.LMP as the other borrowed spells'. Its list entry goes after Dimension Door's in her list.
//
// XANTAM'S ORBS are Xantam's DodgeBalls (his third attack; vtable 0x0031C3C8, built by the game's own constructor
// FUN_001E8320), flying and bursting as his do, but with a vtable copy whose touch hurts monsters and breakable
// things only (barrels, crates, pots, baskets and pallets: flag 8, which the game's hit function breaks), once an
// orb, through the game's hit function from her (resistances count, her kills give her the experience); never a
// player. His hurt anyone for 10, players too, with no experience. Its cast case (type 35) sends them in a fan, 12
// degrees apart, whose middle goes at the middle of the nearest monster in front of her (straight ahead when there
// is none), from a point 32 behind her hand, so they are apart from the moment they appear. Its name, description
// and icon go in GTEXT.LMP and ICONS.LMP as the other borrowed spells'; its HUD line is the game's own
// "Damage: %d-%d" (each orb's). Its list entry goes after Sorcerer's Blood in her list.
//
// WHERE IT GOES. The pool: 61 stretches of the game's own code that nothing ever runs (unused library
// functions and leftover routines, 32,724 bytes; see featpool.py). They are the same addresses and bytes
// in the stock executable and in every three-player build, the game's own segment loads them, and no
// other patch WorldExplorer knows writes there. The patch takes 23,032 bytes of it: its data block at
// 0x00221B88 (every earlier version's was at 0x00132D90), its second block at 0x00224B68 (the icons of types 32-42
// and Entangle's settings, since version 11), its third at 0x0020B890 (Insect Plague's list entry, ability record
// and settings, since version 12), its fourth at 0x0020BA70 (Poison Gas Arrow's, since version 13), its fifth at
// 0x0020BB90 (Dimension Door's, since version 14), its sixth at 0x0020E6E8 (Expert Focus's, since version 15), its
// seventh at 0x0020E810 (Phase Axe's, since version 16), its eighth at 0x0020F6A8 (Shatter's, since version 17), its
// code in thirty-eight pieces, its vtable copies (the swarm's a creature's whole 0xA0 bytes) and its cast table
// (42 types). The code is the same whichever spells are on; only the hooks of the spells that are on are written
// (33 words when all fifteen are), and the data block says which ones are. Nothing else changes: no program
// header, no start-up copy, the same file length.
//
// SO IT NEEDS NO THREE-PLAYER BUILD, and the Three Players tab can turn three players on or off
// underneath it: the three-player patch never touches these bytes, and the code finds the class lists
// (0x002A0478 in the stock game, 0x000D0100 with three players) from FUN_00162B78's own instructions while
// the game runs. (Checked: installing and switching three players commute.) Corrosive and Glow can be in
// as well: Righteous Might and Power Attack hook the instruction before the melee hit's call, which
// Corrosive changes.
//
// STATES. Off: the hooks and the pool are the game's. On: version 17 is in, with the spells its data
// block names. Outdated: version 16 (phase-axe-test1: all fourteen, Phase Axe the newest), version 15
// (expert-focus-test1: all thirteen, Expert Focus the newest), version 14
// (dimension-door-test1: all twelve, Dimension Door the newest), version 13
// (poison-gas-arrow-test1: all eleven, Poison Gas Arrow the newest), version 12 (insect-plague-test2: all ten, Insect
// Plague the newest), version 11
// (entangle-test1: all nine, Entangle the newest), version 10
// (inferno-arrow-test1: all eight, Inferno Arrow the newest), version 9
// (xantam-orbs-test2: all seven, the orbs in a fan), version 8
// (xantam-orbs-test1: all seven, the orbs aimed with a random scatter), version 7 (sorcerers-blood-test1: the
// six without Xantam's Orbs), version 6 (power-attack-test1: the five without Sorcerer's Blood either),
// version 5 (kromlech-spells-test1: the four borrowed spells of then), or Xantam's Fire alone as the fourth or
// third test version (test4: in the pool; test3: a 12 KB spell area copied into low memory at start-up);
// saving takes it out and puts version 17 in, every spell it had with its settings. Unrecognized: anything
// else (earlier test versions, or another patch in those places).
//
// SETTINGS (the data block; the code reads them while the game runs): each spell's level-up price
// (0-30, priced as the game prices its own feats: from 5 on a rank costs price + rank^1.6, so 16 gives
// 16, 17, 19, 21 and 25; below 5, (rank + 1) x price; 0 makes every rank free), the mana a cast takes (not
// Power Attack's: its record's; Sorcerer's Blood has none), and up to three values by rank: Xantam's Fire
// and Blade Barrier the least and most damage of a burn or a hit, Xantam's Orbs the least and most damage
// of each orb and the number of orbs (1-10), Power Attack the hits it doubles, Antimagic Field its seconds,
// Righteous Might the percentage more damage his weapon hits do and the percentage less weapon hits on him
// do. Sorcerer's Blood has the number of ranks its entry offers (1-5; each adds 1 mana a level) in their
// place. Inferno Arrow has seven by rank: the arrow's least and most damage (the blast's most is the arrow's
// most), the blast's reach, how far the flames spread, how long they burn (tenths of a second: the monsters
// the blast sets alight burn as long), and the least and most damage of each of their burns. Entangle has
// five: a thorn's least and most damage, the number of vines (1-10), how much slower a wrapped monster walks
// (a percentage, an even number from 2 to 98: the game counts in fiftieths) and how long a vine holds it (tenths
// of a second). Insect Plague has four: a sting's least and most damage, the number of swarms (1-10) and how long
// a swarm lasts (tenths of a second); its settings are in the third block. Poison Gas Arrow has six: the arrow's least
// and most damage, the cloud's reach (10-255, measured to a monster's edge), how long it lasts (tenths of a second) and
// the least and most damage of its hits (once a second); its settings are in the fourth block. Dimension Door has 3
// ranks (its entry offers no more), each with how far ahead she goes (20-400); its settings are in the fifth block.
// Expert Focus has two by rank: the percentage more damage his arrows do while it lasts (1-200) and how long it lasts
// (tenths of a second, up to 25.5 seconds); its settings are in the sixth block. Phase Axe has two by rank: the least
// and most damage of each of its hits; its settings are in the seventh block. Shatter has five by rank: the least and
// most damage, the reach (10-255, measured to a monster's edge), how long a monster stays frozen (tenths of a second,
// 0.1-25.5 seconds) and her cooldown after it (tenths of a second, 0-25.5 seconds); its settings are in the eighth block.
//
// THE GAME'S FILES. GTEXT.LMP needs each borrowed spell's name and description (and two HUD lines;
// BorrowedSpellTexts, in the five languages), or the level-up screen and the HUD show NF:<name>_name.
// ICONS.LMP can take their HUD icons (BorrowedSpellIcons); without one the HUD shows a game icon in its
// place. Power Attack's, Sorcerer's Blood's and Inferno Arrow's texts (and Power Attack's icon) are the game's
// own: GTEXT.LMP doesn't change for them, nor ICONS.LMP for Power Attack and Sorcerer's Blood.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Text;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The spells the patch can give the heroes, in the order the data block lists them.</summary>
public enum BorrowedSpell
{
    XantamFire,
    PowerAttack,
    BladeBarrier,
    AntimagicField,
    RighteousMight,
    SorcerersBlood,
    XantamOrbs,
    InfernoArrow,
    Entangle,
    InsectPlague,
    PoisonGasArrow,
    DimensionDoor,
    ExpertFocus,
    PhaseAxe,
    Shatter
}

public enum BorrowedSpellsState
{
    /// <summary>The game's code and the free pool: the spells can be put in.</summary>
    Off,
    /// <summary>Version 17 is in: which spells, and their settings, can be read and changed.</summary>
    On,
    /// <summary>Version 16 (all fourteen, Phase Axe the newest), 15 (all thirteen, Expert Focus the newest), 14 (all twelve, Dimension Door the newest), 13 (all eleven, Poison Gas Arrow the newest), 12 (all ten, Insect Plague the newest), 11 (all nine, Entangle the newest), 10 (all eight, Inferno Arrow the newest), 9 (all seven, the orbs in a fan), 8 (all seven, the orbs aimed with a random scatter), 7 (the six without Xantam's Orbs), 6 (the five without Sorcerer's Blood either) or 5 (the four borrowed spells of then), or Xantam's Fire alone as the fourth or third test version: saving brings it to version 17.</summary>
    Outdated,
    /// <summary>Neither: an earlier test version, or another patch where it goes.</summary>
    Unrecognized
}

/// <summary>
/// What a spell is (its name, its hero, where his or her level-up list has it, its feat bit and cast type), what its settings
/// by rank mean and the values they may take, and its defaults (what featpatch.py writes). <paramref name="GameFeat"/>: the
/// game's own feat (Power Attack, Sorcerer's Blood), whose ability record (and so its mana), icon and texts are the game's.
/// <paramref name="Passive"/>: a passive feat (Sorcerer's Blood), no cast and no values by rank; the number of ranks its entry
/// offers is its setting instead. <paramref name="ThirdName"/>: a third value by rank (Xantam's Orbs: the number of orbs).
/// <paramref name="GameRecord"/>: the game's own ability record and texts (Inferno Arrow), though the patch sets its mana and
/// brings its icon. <paramref name="MoreValues"/>: further values by rank (Inferno Arrow's blast, flames and burns;
/// Entangle's slow and time; Insect Plague's time; Poison Gas Arrow's cloud: its reach, time and damage; Expert Focus's time;
/// Shatter's reach, freeze and cooldown).
/// <paramref name="FixedRanks"/>: the ranks its list entry offers when that isn't a setting (5; Dimension Door 3), and so
/// how many of each value by rank it has.
/// </summary>
public sealed record BorrowedSpellInfo(
    BorrowedSpell Spell, string Title, string Hero, string After, int Bit, int CastType,
    string Values, string FirstName, int FirstMin, int FirstMax, string? SecondName, int SecondMin, int SecondMax,
    bool Range, BorrowedSpellSettings Default, bool GameFeat = false, bool Passive = false,
    string? ThirdName = null, int ThirdMin = 0, int ThirdMax = 0, bool GameRecord = false,
    IReadOnlyList<BorrowedSpellValue>? MoreValues = null, int FixedRanks = BorrowedSpellSettings.Ranks)
{
    /// <summary>The spell has two values by rank (damage least and most, or more and less).</summary>
    public bool HasSecond => SecondName != null;

    /// <summary>The spell has a third value by rank (Xantam's Orbs: how many orbs).</summary>
    public bool HasThird => ThirdName != null;

    /// <summary>The spell has values by rank (every one but Sorcerer's Blood).</summary>
    public bool HasValues => !Passive;

    /// <summary>The patch writes the mana a cast takes (not for Power Attack, whose record's mana the Feats &amp; Spells tab edits, nor a passive).</summary>
    public bool HasMana => !GameFeat;

    /// <summary>The further values by rank (none for every spell but Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Expert Focus).</summary>
    public IReadOnlyList<BorrowedSpellValue> More => MoreValues ?? Array.Empty<BorrowedSpellValue>();

    /// <summary>The spell's names and descriptions go into GTEXT.LMP with it (not the game's own feats').</summary>
    public bool HasOwnTexts => !GameFeat && !GameRecord;

    /// <summary>The spell's own ability record is the patch's (not the game's own feats', nor Inferno Arrow's).</summary>
    public bool UsesGameRecord => GameFeat || GameRecord;
}

/// <summary>
/// A further value by rank (Inferno Arrow's, Entangle's, Insect Plague's, Poison Gas Arrow's, Expert Focus's, Shatter's): what it is, the values it may take, and whether it is kept in tenths
/// (a time in seconds with one decimal: <paramref name="Tenths"/>). <paramref name="AtMost"/>: the index of the further value it
/// may not be more than (a least damage's most), or -1. <paramref name="Step"/>: the values go in steps of it (Entangle's slow:
/// 2, an even percentage, as the game counts in fiftieths).
/// </summary>
public sealed record BorrowedSpellValue(string Name, int Min, int Max, bool Tenths = false, int AtMost = -1, int Step = 1);

/// <summary>
/// One spell's settings: the level-up price byte, the mana a cast takes (0 for Power Attack: its record's; 0 for Sorcerer's
/// Blood: a passive), and its values by rank (ranks 1-5): Xantam's Fire and Blade Barrier the least (<see cref="First"/>) and
/// most (<see cref="Second"/>) damage, Power Attack the hits it doubles and Antimagic Field the seconds (<see cref="First"/>;
/// <see cref="Second"/> empty), Righteous Might the percentage more damage (<see cref="First"/>) and less damage taken
/// (<see cref="Second"/>); Xantam's Orbs the least (<see cref="First"/>) and most (<see cref="Second"/>) damage of each orb and
/// how many orbs (<see cref="Third"/>); Inferno Arrow the arrow's least (<see cref="First"/>) and most (<see cref="Second"/>)
/// damage and five more (<see cref="More"/>: the blast's reach, how far the flames spread, how long they burn in tenths of a
/// second, the least and most damage of a burn); Entangle a thorn's least (<see cref="First"/>) and most (<see cref="Second"/>)
/// damage, how many vines (<see cref="Third"/>) and two more (<see cref="More"/>: the percentage slower a wrapped monster walks,
/// how long a vine holds it in tenths of a second); Insect Plague a sting's least (<see cref="First"/>) and most
/// (<see cref="Second"/>) damage, how many swarms (<see cref="Third"/>) and one more (<see cref="More"/>: how long a swarm
/// lasts in tenths of a second); Poison Gas Arrow the arrow's least (<see cref="First"/>) and most (<see cref="Second"/>)
/// damage and four more (<see cref="More"/>: the cloud's reach, how long it lasts in tenths of a second, the least and most
/// damage of its hits); Dimension Door how far ahead she goes (<see cref="First"/>; <see cref="Second"/> empty) at its 3
/// ranks; Expert Focus the percentage more damage his arrows do (<see cref="First"/>; <see cref="Second"/> empty) and one
/// more (<see cref="More"/>: how long the focus lasts in tenths of a second); Phase Axe the least (<see cref="First"/>) and
/// most (<see cref="Second"/>) damage of each of its hits; Shatter the least (<see cref="First"/>) and most
/// (<see cref="Second"/>) damage and three more (<see cref="More"/>: its reach, how long a monster stays frozen and her
/// cooldown after it, both in tenths of a second); Sorcerer's Blood none (all empty). <see cref="RankCount"/>: the ranks its list entry offers, 5 for every spell
/// but Sorcerer's Blood, whose setting it is (1-5; each rank adds 1 mana a level), and Dimension Door (3: its values are
/// by those 3 ranks). <see cref="Third"/> is
/// null (none) for every spell but Xantam's Orbs, Entangle and Insect Plague, <see cref="More"/> for every one but Inferno
/// Arrow, Entangle, Insect Plague, Poison Gas Arrow and Expert Focus.
/// </summary>
public sealed record BorrowedSpellSettings(int Price, float Mana, IReadOnlyList<int> First, IReadOnlyList<int> Second,
                                           int RankCount = 5, IReadOnlyList<int>? Third = null,
                                           IReadOnlyList<IReadOnlyList<int>>? More = null)
{
    /// <summary>The third values (empty when the spell has none).</summary>
    public IReadOnlyList<int> ThirdValues => Third ?? Array.Empty<int>();

    /// <summary>The further values, each by rank (empty when the spell has none).</summary>
    public IReadOnlyList<IReadOnlyList<int>> MoreValues => More ?? Array.Empty<IReadOnlyList<int>>();

    public const int Ranks = 5, MaxPrice = 30, MaxRankCount = 5;
    public const float MaxMana = 100f;

    /// <summary>
    /// What the next rank costs on the level-up screen when the hero holds <paramref name="held"/> ranks (0-4), as the game prices
    /// its feats (FUN_0017C620): a price below 5 costs (held + 1) x price, from 5 on price + held^1.6.
    /// </summary>
    public static int RankCost(int price, int held) => price < 5 ? (held + 1) * price : price + (int)MathF.Pow(held, 1.6f);

    /// <summary>Why these can't be written for <paramref name="spell"/>, or null.</summary>
    public string? Problem(BorrowedSpell spell)
    {
        var info = BorrowedSpells.Info(spell);
        if (Price is < 0 or > MaxPrice) return $"{info.Title}: the price is 0 to {MaxPrice} (0 makes every rank free).";
        if (info.HasMana && (!float.IsFinite(Mana) || Mana <= 0f || Mana > MaxMana))
            return $"{info.Title}: the mana a cast takes is above 0 and at most {MaxMana:0}.";
        if (!info.HasMana && Mana != 0f)
            return info.Passive
                ? $"{info.Title}: a passive feat takes no mana (0)."
                : $"{info.Title}: its mana is its own ability record's (the Feats & Spells tab), so it has none here (0).";
        if (info.Passive)
        {
            if (RankCount is < 1 or > MaxRankCount) return $"{info.Title}: its level-up entry offers 1 to {MaxRankCount} ranks.";
            if (First is null || First.Count != 0 || Second is null || Second.Count != 0 || ThirdValues.Count != 0)
                return $"{info.Title}: it has no values by rank.";
            return null;
        }

        var ranks = info.FixedRanks;
        if (RankCount != ranks) return $"{info.Title}: its level-up entry offers {ranks} ranks.";
        if (First is null || First.Count != ranks || Second is null || Second.Count != (info.HasSecond ? ranks : 0) ||
            ThirdValues.Count != (info.HasThird ? ranks : 0))
            return $"{info.Title}: {info.Values} is given for each of the {ranks} ranks.";
        for (var r = 0; r < ranks; r++)
        {
            if (First[r] < info.FirstMin || First[r] > info.FirstMax)
                return $"{info.Title}, rank {r + 1}: {info.FirstName} is {info.FirstMin} to {info.FirstMax}.";
            if (!info.HasSecond) continue;
            if (Second[r] < info.SecondMin || Second[r] > info.SecondMax)
                return $"{info.Title}, rank {r + 1}: {info.SecondName} is {info.SecondMin} to {info.SecondMax}.";
            if (info.Range && First[r] > Second[r]) return $"{info.Title}, rank {r + 1}: the least damage is more than the most.";
            if (info.HasThird && (ThirdValues[r] < info.ThirdMin || ThirdValues[r] > info.ThirdMax))
                return $"{info.Title}, rank {r + 1}: {info.ThirdName} is {info.ThirdMin} to {info.ThirdMax}.";
        }

        if (MoreValues.Count != info.More.Count || MoreValues.Any(v => v is null || v.Count != ranks))
            return $"{info.Title}: {info.Values} is given for each of the {ranks} ranks.";
        for (var r = 0; r < ranks; r++)
        {
            for (var k = 0; k < info.More.Count; k++)
            {
                var v = info.More[k];
                if (MoreValues[k][r] < v.Min || MoreValues[k][r] > v.Max || (MoreValues[k][r] - v.Min) % v.Step != 0)
                    return $"{info.Title}, rank {r + 1}: {v.Name} is " +
                           (v.Tenths ? $"{v.Min / 10.0:0.#} to {v.Max / 10.0:0.#} seconds." :
                            v.Step == 2 ? $"an even number from {v.Min} to {v.Max}." : $"{v.Min} to {v.Max}.");
                if (v.AtMost >= 0 && MoreValues[k][r] > MoreValues[v.AtMost][r])
                    return $"{info.Title}, rank {r + 1}: {v.Name} is more than {info.More[v.AtMost].Name}.";
            }
        }

        return null;
    }

    public bool Equals(BorrowedSpellSettings? other) =>
        other is not null && Price == other.Price && Mana.Equals(other.Mana) && RankCount == other.RankCount &&
        First.SequenceEqual(other.First) && Second.SequenceEqual(other.Second) && ThirdValues.SequenceEqual(other.ThirdValues) &&
        MoreValues.Count == other.MoreValues.Count && MoreValues.Zip(other.MoreValues).All(p => p.First.SequenceEqual(p.Second));

    public override int GetHashCode() =>
        HashCode.Combine(Price, Mana, First.Count, Second.Count, RankCount, ThirdValues.Count, MoreValues.Count);
}

/// <summary>The fifteen spells: what each is, its defaults (what featpatch.py writes) and the test files' settings.</summary>
public static class BorrowedSpells
{
    /// <summary>Every spell, in the order of <see cref="BorrowedSpell"/> (the data block's).</summary>
    public static IReadOnlyList<BorrowedSpell> All { get; } =
        new[]
        {
            BorrowedSpell.XantamFire, BorrowedSpell.PowerAttack, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField,
            BorrowedSpell.RighteousMight, BorrowedSpell.SorcerersBlood, BorrowedSpell.XantamOrbs, BorrowedSpell.InfernoArrow,
            BorrowedSpell.Entangle, BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor,
            BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter
        };

    /// <summary>Inferno Arrow's further values by rank, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly BorrowedSpellValue[] InfernoValues =
    {
        new("the blast's reach", 1, 255), new("how far the flames spread", 1, 255),
        new("how long the flames burn", 1, 255, Tenths: true), new("the least damage of a burn", 0, 255, AtMost: 4),
        new("the most damage of a burn", 0, 255)
    };

    /// <summary>Entangle's further values by rank, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly BorrowedSpellValue[] EntangleValues =
    {
        new("the percentage slower a wrapped monster walks", 2, 98, Step: 2), new("how long a vine holds it", 1, 255, Tenths: true)
    };

    /// <summary>Insect Plague's further value by rank, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly BorrowedSpellValue[] InsectValues = { new("how long a swarm lasts", 1, 255, Tenths: true) };

    /// <summary>Poison Gas Arrow's further values by rank, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly BorrowedSpellValue[] GasValues =
    {
        new("the cloud's reach", 10, 255), new("how long the cloud lasts", 1, 255, Tenths: true),
        new("the least damage of the gas", 0, 255, AtMost: 3), new("the most damage of the gas", 0, 255)
    };

    /// <summary>Expert Focus's further value by rank, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly BorrowedSpellValue[] FocusValues = { new("how long the focus lasts", 1, 255, Tenths: true) };

    /// <summary>Shatter's further values by rank, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly BorrowedSpellValue[] ShatterValues =
    {
        new("its reach", FeatPatch.ShatterReachMin, FeatPatch.ShatterReachMax),
        new("how long a monster stays frozen", FeatPatch.ShatterFreezeMin, FeatPatch.ShatterFreezeMax, Tenths: true),
        new("her cooldown after it", FeatPatch.ShatterCooldownMin, FeatPatch.ShatterCooldownMax, Tenths: true)
    };

    private static readonly BorrowedSpellInfo[] Infos =
    {
        new(BorrowedSpell.XantamFire, "Xantam's Fire", "Adrianna", "Targetting", 26, 10,
            "the damage of each burn", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(16, 15f, new[] { 1, 1, 2, 2, 3 }, new[] { 2, 3, 4, 5, 6 })),
        new(BorrowedSpell.PowerAttack, "Power Attack", "Kromlech", "his own feats", 34, 18,
            "the number of hits it doubles", "the number of hits doubled", 1, 20, null, 0, 0, false,
            new BorrowedSpellSettings(5, 0f, new[] { 1, 2, 3, 4, 5 }, Array.Empty<int>()), GameFeat: true),
        new(BorrowedSpell.BladeBarrier, "Blade Barrier", "Kromlech", "his own feats", 43, 32,
            "the damage of each hit", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(10, 15f, new[] { 4, 6, 8, 10, 12 }, new[] { 8, 10, 13, 16, 20 })),
        new(BorrowedSpell.AntimagicField, "Antimagic Field", "Kromlech", "his own feats", 44, 33,
            "how long it lasts", "the time in seconds", 1, 60, null, 0, 0, false,
            new BorrowedSpellSettings(8, 10f, new[] { 4, 5, 6, 7, 8 }, Array.Empty<int>())),
        new(BorrowedSpell.RighteousMight, "Righteous Might", "Kromlech", "his own feats", 45, 34,
            "the percentages", "the percentage more damage", 0, 200, "the percentage less damage taken", 0, 90, false,
            new BorrowedSpellSettings(12, 15f, new[] { 10, 20, 30, 40, 50 }, new[] { 10, 20, 30, 40, 50 })),
        new(BorrowedSpell.SorcerersBlood, "Sorcerer's Blood", "Adrianna", "her own feats and Xantam's Fire", 32, 24,
            "nothing", "", 0, 0, null, 0, 0, false,
            new BorrowedSpellSettings(3, 0f, Array.Empty<int>(), Array.Empty<int>(), RankCount: 1), GameFeat: true, Passive: true),
        new(BorrowedSpell.XantamOrbs, "Xantam's Orbs", "Adrianna", "her own feats, Xantam's Fire and Sorcerer's Blood", 46, 35,
            "the number of orbs and the damage of each", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(12, 10f, new[] { 8, 8, 8, 8, 8 }, new[] { 12, 12, 12, 12, 12 }, Third: new[] { 1, 2, 3, 4, 5 }),
            ThirdName: "the number of orbs", ThirdMin: 1, ThirdMax: 10),
        new(BorrowedSpell.InfernoArrow, "Inferno Arrow", "Vahn", "his own feats", 8, 9,
            "the arrow's damage, the blast's reach and the flames", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(12, 12f, new[] { 10, 12, 14, 16, 18 }, new[] { 15, 18, 21, 24, 27 },
                More: new IReadOnlyList<int>[]
                {
                    new[] { 64, 68, 72, 76, 80 }, new[] { 48, 54, 60, 66, 72 }, new[] { 20, 25, 30, 35, 40 }, new[] { 1, 1, 2, 2, 3 },
                    new[] { 2, 3, 3, 4, 5 }
                }),
            GameRecord: true, MoreValues: InfernoValues),
        new(BorrowedSpell.Entangle, "Entangle", "Vahn", "his own feats and Inferno Arrow", 47, 36,
            "the thorns' damage, the number of vines, the slow and the time", "the least damage", 0, 255, "the most damage", 0,
            255, true,
            new BorrowedSpellSettings(12, 12f, new[] { 2, 3, 4, 5, 6 }, new[] { 4, 5, 6, 7, 8 }, Third: new[] { 1, 2, 3, 4, 5 },
                More: new IReadOnlyList<int>[] { new[] { 30, 34, 40, 44, 50 }, new[] { 40, 45, 50, 55, 60 } }),
            ThirdName: "the number of vines", ThirdMin: 1, ThirdMax: 10, MoreValues: EntangleValues),
        new(BorrowedSpell.InsectPlague, "Insect Plague", "Vahn", "his own feats, Inferno Arrow and Entangle", 48, 37,
            "the stings' damage, the number of swarms and the time", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(12, 15f, new[] { 1, 1, 1, 1, 1 }, new[] { 3, 3, 3, 3, 3 }, Third: new[] { 1, 2, 3, 4, 5 },
                More: new IReadOnlyList<int>[] { new[] { 80, 80, 80, 80, 80 } }),
            ThirdName: "the number of swarms", ThirdMin: 1, ThirdMax: 10, MoreValues: InsectValues),
        new(BorrowedSpell.PoisonGasArrow, "Poison Gas Arrow", "Vahn", "his own feats, Inferno Arrow, Entangle and Insect Plague", 49,
            38, "the arrow's damage and the cloud's reach, time and damage", "the least damage", 0, 255, "the most damage", 0, 255,
            true,
            new BorrowedSpellSettings(12, 10f, new[] { 8, 9, 10, 11, 12 }, new[] { 12, 14, 15, 17, 18 },
                More: new IReadOnlyList<int>[]
                {
                    new[] { 40, 45, 50, 55, 60 }, new[] { 40, 45, 50, 55, 60 }, new[] { 2, 2, 3, 3, 4 }, new[] { 3, 4, 4, 5, 6 }
                }),
            MoreValues: GasValues),
        new(BorrowedSpell.DimensionDoor, "Dimension Door", "Adrianna", "her own feats, Xantam's Fire, Sorcerer's Blood and Xantam's Orbs",
            50, 39, "how far she goes", "the distance", FeatPatch.DoorDistanceMin, FeatPatch.DoorDistanceMax, null, 0, 0, false,
            new BorrowedSpellSettings(12, 10f, new[] { 100, 150, 200 }, Array.Empty<int>(), RankCount: FeatPatch.DoorRanks),
            FixedRanks: FeatPatch.DoorRanks),
        new(BorrowedSpell.ExpertFocus, "Expert Focus", "Vahn", "his own feats, Inferno Arrow, Entangle, Insect Plague and Poison Gas Arrow",
            51, 40, "the percentage more damage and the time", "the percentage more damage", FeatPatch.FocusPercentMin,
            FeatPatch.FocusPercentMax, null, 0, 0, false,
            new BorrowedSpellSettings(12, 10f, new[] { 50, 50, 50, 50, 50 }, Array.Empty<int>(),
                More: new IReadOnlyList<int>[] { new[] { 50, 75, 100, 125, 150 } }),
            MoreValues: FocusValues),
        new(BorrowedSpell.PhaseAxe, "Phase Axe", "Kromlech",
            "his own feats, Power Attack, Blade Barrier, Antimagic Field and Righteous Might", 52, 41,
            "the damage of each hit", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(12, 10f, new[] { 8, 10, 12, 14, 16 }, new[] { 12, 15, 18, 21, 24 })),
        new(BorrowedSpell.Shatter, "Shatter", "Adrianna",
            "her own feats, Xantam's Fire, Sorcerer's Blood, Xantam's Orbs and Dimension Door", 53, 42,
            "the damage, the reach, the freeze and the cooldown", "the least damage", 0, 255, "the most damage", 0, 255, true,
            new BorrowedSpellSettings(12, 10f, new[] { 1, 2, 3, 4, 5 }, new[] { 3, 4, 5, 6, 8 },
                More: new IReadOnlyList<int>[]
                {
                    new[] { 60, 70, 80, 90, 100 }, new[] { 10, 15, 20, 25, 30 }, new[] { 20, 25, 30, 35, 40 }
                }),
            MoreValues: ShatterValues)
    };

    public static BorrowedSpellInfo Info(BorrowedSpell spell) => Infos[(int)spell];

    /// <summary>The test files' settings: every rank free, 4 mana a cast (Power Attack: its record's; Sorcerer's Blood: none), the default values.</summary>
    public static BorrowedSpellSettings TestSettings(BorrowedSpell spell) =>
        Info(spell).Default with { Price = 0, Mana = Info(spell).HasMana ? 4f : 0f };

    internal static SpellPatchData Data(BorrowedSpell spell) => FeatPatch.Spells[(int)spell];

    internal static int Mask(BorrowedSpell spell) => Data(spell).Mask;
}

/// <summary>
/// The spells' texts in GTEXT.LMP: the level-up screen's names and descriptions, the HUD's names and two of its lines. Power
/// Attack, Sorcerer's Blood and Inferno Arrow have none here: their texts (powerattack_name, powerattack_desc,
/// PowerSpellDamageWeapon; SorcerersBlood, SorcerersBloodDesc; infernoarrow_name, InfernoArrow, InfernoArrowDesc) are the
/// game's own, never added or taken out.
/// </summary>
public static class BorrowedSpellTexts
{
    /// <summary>The keys and texts of <paramref name="spell"/> in each language (0 English ... 4 Spanish): what gtext_feats17.py writes (none for Power Attack, Sorcerer's Blood and Inferno Arrow).</summary>
    public static IReadOnlyList<(int Language, string Key, string Text)> Of(BorrowedSpell spell)
    {
        var key = BorrowedSpells.Data(spell).Key;
        return FeatPatch.Texts.Where(t => t.Spell == key).Select(t => (t.Language, t.Key, t.Text)).ToList();
    }

    /// <summary>The key of the spell's name ("bladebarrier_name"; "SorcerersBlood"): its list entry's, what the game shows when it is missing, after "NF:".</summary>
    public static string NameKey(BorrowedSpell spell) =>
        Encoding.ASCII.GetString(Convert.FromBase64String(BorrowedSpells.Data(spell).EntryBase64), 8, 0x20).TrimEnd('\0');

    /// <summary>True when <paramref name="text"/> has every one of the spell's texts as written here.</summary>
    public static bool HasNames(GameText text, BorrowedSpell spell) => Of(spell).All(n => text.Get(n.Key, n.Language) == n.Text);

    /// <summary>True when <paramref name="text"/> has any of the spell's keys.</summary>
    public static bool HasAnyName(GameText text, BorrowedSpell spell) => Of(spell).Any(n => text.Get(n.Key, n.Language) != null);

    /// <summary>
    /// Puts the texts of <paramref name="spells"/> in <paramref name="text"/>, spell by spell in the order of
    /// <see cref="BorrowedSpells.All"/> (a key it has already is changed in place, a new one goes at the end).
    /// </summary>
    public static void AddNames(GameText text, IEnumerable<BorrowedSpell> spells)
    {
        var set = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Where(set.Contains))
        foreach (var (language, key, name) in Of(spell))
            text.SetLanguageText(language, key, name);
    }

    /// <summary>Takes the keys of <paramref name="spells"/> out of <paramref name="text"/>.</summary>
    public static void RemoveNames(GameText text, IEnumerable<BorrowedSpell> spells)
    {
        foreach (var spell in spells.Distinct())
        foreach (var (language, key, _) in Of(spell))
            text.RemoveLanguageText(language, key);
    }
}

/// <summary>
/// The spells' HUD icons in ICONS.LMP (icon_xantamfire.tex, icon_bladebarrier.tex, icon_antimagicfield.tex,
/// icon_righteousmight.tex, icon_xantamorbs.tex, icon_infernoarrow.tex, icon_entangle.tex, icon_insectplague.tex,
/// icon_poisongasarrow.tex, icon_dimensiondoor.tex, icon_expertfocus.tex, icon_phaseaxe.tex, icon_shatter.tex). Power Attack's, icon_powerattack.tex, is the game's
/// own: never added or taken out. Sorcerer's Blood, a passive feat, has no HUD slot and no icon.
/// </summary>
public static class BorrowedSpellIcons
{
    public static string EntryName(BorrowedSpell spell) => BorrowedSpells.Data(spell).IconName;

    /// <summary>The spell brings an icon of its own (every one but Power Attack, whose icon the game has, and Sorcerer's Blood, which needs none).</summary>
    public static bool HasOwnIcon(BorrowedSpell spell) => !BorrowedSpells.Info(spell).GameFeat;

    /// <summary>The texture: 44 x 44, 8-bit with 256 colours, in the layout of the game's spell icons (empty for Power Attack and Sorcerer's Blood).</summary>
    public static byte[] Texture(BorrowedSpell spell) => Convert.FromBase64String(BorrowedSpells.Data(spell).IconBase64);

    /// <summary>The game's icon the HUD shows when the spell's own isn't in ICONS.LMP.</summary>
    public static string FallbackName(BorrowedSpell spell) => spell switch
    {
        BorrowedSpell.XantamFire => "icon_protection.tex (Fire Shield's)",
        BorrowedSpell.PowerAttack => "icon_powerattack.tex (its own, the game's)",
        BorrowedSpell.BladeBarrier => "icon_whirlwind.tex (Whirlwind Attack's)",
        BorrowedSpell.AntimagicField => "icon_protection.tex (Fire Shield's)",
        BorrowedSpell.SorcerersBlood => "none (a passive feat has no HUD slot)",
        BorrowedSpell.XantamOrbs => "icon_fireball.tex (Fireball's)",
        BorrowedSpell.InfernoArrow => "icon_flamingarrow.tex (Flaming Arrow's)",
        BorrowedSpell.Entangle => "icon_melfsacidarrow.tex (Melf's Acid Arrow's)",
        BorrowedSpell.InsectPlague => "icon_magicmissile.tex (Magic Missile's)",
        BorrowedSpell.PoisonGasArrow => "icon_melfsacidarrow.tex (Melf's Acid Arrow's)",
        BorrowedSpell.DimensionDoor => "icon_sphere.tex (Otiluke's Resilient Sphere's)",
        BorrowedSpell.ExpertFocus => "icon_powerattack.tex (Power Attack's)",
        BorrowedSpell.PhaseAxe => "icon_morden.tex (Mordenkainen's Sword's)",
        BorrowedSpell.Shatter => "icon_coneofcold.tex (Cone of Cold's)",
        _ => "icon_powerattack.tex (Power Attack's)"
    };

    /// <summary>Why <paramref name="icons"/> isn't an ICONS.LMP the icons can go in or come out of, or null.</summary>
    public static string? Problem(byte[] icons)
    {
        List<GameArchive.Entry> entries;
        try
        {
            entries = GameArchive.ReadDirectory(icons);
        }
        catch (InvalidDataException e)
        {
            return e.Message;
        }

        if (!entries.Any(e => e.Name == "icon_protection.tex") || !entries.Any(e => e.Name == "icon_healthpotion.tex"))
            return "It isn't ICONS.LMP: it has no icon_protection.tex and icon_healthpotion.tex (the spell icons).";
        return GameArchive.Problem(icons);
    }

    /// <summary>True when <paramref name="icons"/> has the spell's icon.</summary>
    public static bool IsIn(byte[] icons, BorrowedSpell spell) => GameArchive.ReadDirectory(icons).Any(e => e.Name == EntryName(spell));

    /// <summary>
    /// ICONS.LMP with the icons of <paramref name="spells"/> added where the game finds them (where each name sorts; when the
    /// directory is full every file first moves on by 0x80): icons_lmp17.py's result, byte for byte. An icon that is in already
    /// is left as it is, and Power Attack and Sorcerer's Blood bring none (the game's is used; a passive needs none).
    /// </summary>
    public static byte[] AddTo(byte[] icons, IEnumerable<BorrowedSpell> spells)
    {
        if (Problem(icons) is { } problem) throw new InvalidDataException(problem);
        var set = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Where(s => set.Contains(s) && HasOwnIcon(s)))
        {
            if (!IsIn(icons, spell)) icons = GameArchive.Insert(icons, EntryName(spell), Texture(spell), growDirectory: true);
        }

        return icons;
    }

    /// <summary>
    /// ICONS.LMP without the icons of <paramref name="spells"/>, its directory back to its smallest size: as it was before they went
    /// in. The game's own icon_powerattack.tex always stays.
    /// </summary>
    public static byte[] RemoveFrom(byte[] icons, IEnumerable<BorrowedSpell> spells)
    {
        if (Problem(icons) is { } problem) throw new InvalidDataException(problem);
        var set = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Reverse().Where(s => set.Contains(s) && HasOwnIcon(s)))
        {
            if (IsIn(icons, spell)) icons = GameArchive.Remove(icons, EntryName(spell), shrinkDirectory: true);
        }

        return icons;
    }
}

public partial class BgdaExecutable
{
    private static readonly byte[] FeatMark = "FEAT"u8.ToArray();

    private static readonly Lazy<(byte[] Pool, (int Va, byte[] Bytes)[] Code, byte[] Data, byte[] V4Code, byte[] Data2, byte[] Data3,
        byte[] Data4, byte[] Data5, byte[] Data6, byte[] Data7, byte[] Data8)> FeatPatchData = new(LoadFeatPatchData);

    private static (byte[] Pool, (int Va, byte[] Bytes)[] Code, byte[] Data, byte[] V4Code, byte[] Data2, byte[] Data3, byte[] Data4,
        byte[] Data5, byte[] Data6, byte[] Data7, byte[] Data8) LoadFeatPatchData()
    {
        using var packed = new MemoryStream(Convert.FromBase64String(FeatPatch.PoolOriginalBase64));
        using var unpack = new ZLibStream(packed, CompressionMode.Decompress);
        using var pool = new MemoryStream();
        unpack.CopyTo(pool);
        var bytes = pool.ToArray();
        if (bytes.Length != FeatPatch.PoolBytes ||
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != FeatPatch.PoolOriginalSha256)
            throw new InvalidDataException("The built-in feats patch data is damaged.");
        var code = FeatPatch.Code.Select(c => (c.Va, Convert.FromBase64String(c.Base64))).ToArray();
        var data = Convert.FromBase64String(FeatPatch.DataCommonBase64);
        var v4 = Convert.FromBase64String(FeatPatch.V4CodeBase64);
        var data2 = Convert.FromBase64String(FeatPatch.Data2CommonBase64);
        var data3 = Convert.FromBase64String(FeatPatch.Data3CommonBase64);
        var data4 = Convert.FromBase64String(FeatPatch.Data4CommonBase64);
        var data5 = Convert.FromBase64String(FeatPatch.Data5CommonBase64);
        var data6 = Convert.FromBase64String(FeatPatch.Data6CommonBase64);
        var data7 = Convert.FromBase64String(FeatPatch.Data7CommonBase64);
        var data8 = Convert.FromBase64String(FeatPatch.Data8CommonBase64);
        if (data.Length != FeatPatch.DataSize || v4.Length != FeatPatch.V4CodeEndVa - FeatPatch.V4CodeVa ||
            data2.Length != FeatPatch.Data2Size || !data2.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data2Mark)) ||
            BitConverter.ToInt32(data2, 4) != FeatPatch.Version ||
            data3.Length != FeatPatch.Data3Size || !data3.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data3Mark)) ||
            BitConverter.ToInt32(data3, 4) != FeatPatch.Version ||
            data4.Length != FeatPatch.Data4Size || !data4.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data4Mark)) ||
            BitConverter.ToInt32(data4, 4) != FeatPatch.Version ||
            data5.Length != FeatPatch.Data5Size || !data5.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data5Mark)) ||
            BitConverter.ToInt32(data5, 4) != FeatPatch.Version ||
            data6.Length != FeatPatch.Data6Size || !data6.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data6Mark)) ||
            BitConverter.ToInt32(data6, 4) != FeatPatch.Version ||
            data7.Length != FeatPatch.Data7Size || !data7.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data7Mark)) ||
            BitConverter.ToInt32(data7, 4) != FeatPatch.Version ||
            data8.Length != FeatPatch.Data8Size || !data8.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes(FeatPatch.Data8Mark)) ||
            BitConverter.ToInt32(data8, 4) != FeatPatch.Version)
            throw new InvalidDataException("The built-in feats patch data is damaged.");
        return (bytes, code, data, v4, data2, data3, data4, data5, data6, data7, data8);
    }

    /// <summary>The end of everything the patch reads or writes in the game's part of the file (a shorter file isn't the game's).</summary>
    private static readonly Lazy<int> FeatEndVa = new(() =>
        new[] { FeatPatch.Pool[^1].End, FeatPatch.AbilityTableEndVa + 4, FeatPatch.GameCastTableVa + 4 * FeatPatch.GameCastTypes }
            .Concat(FeatPatch.Checks.Select(c => c.Va + 4))
            .Concat(FeatPatch.Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V16Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V16Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V16VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V15Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V15Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V15VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V14Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V14Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V14VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V13Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V13Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V13VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V12Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V12Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V12VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V11Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V11Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V11VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V10Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V10Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V10VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V9Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V9Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V9VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V8Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V8Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V8VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V7Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V7Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V7VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V6Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V6Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V6VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Concat(FeatPatch.V5Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.V5Code.Select(c => c.Va + c.Length))
            .Concat(FeatPatch.V5VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Append(FeatPatch.SorcerersRecordVa + 0x50)
            .Append(FeatPatch.InfernoRecordVa + 0x50)
            .Concat(FeatPatch.V4Hooks.Select(h => h.Va + 4))
            .Concat(FeatPatch.Strings.Select(s => s.Va + s.Text.Length + 1))
            .Concat(FeatPatch.VTables.Select(v => v.GameVa + FeatPatch.VTableSize))
            .Max());

    /// <summary>How much game code nothing runs there is for new feats (the pool), and in how many stretches.</summary>
    public static int FeatPoolBytes => FeatPatch.PoolBytes;

    public static int FeatPoolStretches => FeatPatch.Pool.Length;

    /// <summary>How much of the pool the borrowed spells take (any of them: the code is the same), and so how much is left.</summary>
    public static int BorrowedSpellsBytes => FeatPatch.PoolUsedBytes;

    /// <summary>The game's bytes at <paramref name="start"/>-<paramref name="end"/>, inside one stretch of the pool.</summary>
    private static ReadOnlySpan<byte> PoolOriginal(int start, int end)
    {
        var at = 0;
        foreach (var (a, b) in FeatPatch.Pool)
        {
            if (a <= start && end <= b) return FeatPatchData.Value.Pool.AsSpan(at + start - a, end - start);
            at += b - a;
        }

        throw new ArgumentOutOfRangeException(nameof(start), "Not inside one stretch of the pool.");
    }

    private bool BytesAre(int va, ReadOnlySpan<byte> bytes) =>
        _data.Length >= FileOffset(va) + bytes.Length && _data.AsSpan(FileOffset(va), bytes.Length).SequenceEqual(bytes);

    /// <summary>The file offset of <paramref name="offset"/> in the data block at <paramref name="dataVa"/> (version 17's, 16's, 15's, 14's, 13's, 12's, 11's and 10's, or every earlier version's at <see cref="FeatPatch.LegacyDataVa"/>).</summary>
    private int FeatDataAt(int offset, int dataVa = FeatPatch.DataVa) => FileOffset(dataVa) + offset;

    /// <summary>Where a version's data block is: version 17's, 16's, 15's, 14's, 13's, 12's, 11's and 10's in the pool's stretch 59, every earlier one's at 0x00132D90.</summary>
    private static int DataVaOf(int version) => version >= FeatPatch.V10Version ? FeatPatch.DataVa : FeatPatch.LegacyDataVa;

    /// <summary>The second data block's mark ("FEA2" and the version: 17, or 16, 15, 14, 13, 12 or 11 for <paramref name="version"/> 16, 15, 14, 13, 12 or 11), which version 11 on writes with its data block.</summary>
    private bool Data2Marked(int version = FeatPatch.Version) =>
        BytesAre(FeatPatch.Data2Va, Encoding.ASCII.GetBytes(FeatPatch.Data2Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data2Va) + 4) == version;

    /// <summary>The third data block's mark ("FEA3" and the version: 17, or 16, 15, 14, 13 or 12 for <paramref name="version"/> 16, 15, 14, 13 or 12), which version 12 on writes with its data block.</summary>
    private bool Data3Marked(int version = FeatPatch.Version) =>
        BytesAre(FeatPatch.Data3Va, Encoding.ASCII.GetBytes(FeatPatch.Data3Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data3Va) + 4) == version;

    /// <summary>The fourth data block's mark ("FEA4" and the version: 17, or 16, 15, 14 or 13 for <paramref name="version"/> 16, 15, 14 or 13), which version 13 on writes with its data block.</summary>
    private bool Data4Marked(int version = FeatPatch.Version) =>
        BytesAre(FeatPatch.Data4Va, Encoding.ASCII.GetBytes(FeatPatch.Data4Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data4Va) + 4) == version;

    /// <summary>The fifth data block's mark ("FEA5" and the version: 17, or 16, 15 or 14 for <paramref name="version"/> 16, 15 or 14), which version 14 on writes with its data block.</summary>
    private bool Data5Marked(int version = FeatPatch.Version) =>
        BytesAre(FeatPatch.Data5Va, Encoding.ASCII.GetBytes(FeatPatch.Data5Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data5Va) + 4) == version;

    /// <summary>The sixth data block's mark ("FEA6" and the version: 17, or 16 or 15 for <paramref name="version"/> 16 or 15), which version 15 on writes with its data block.</summary>
    private bool Data6Marked(int version = FeatPatch.Version) =>
        BytesAre(FeatPatch.Data6Va, Encoding.ASCII.GetBytes(FeatPatch.Data6Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data6Va) + 4) == version;

    /// <summary>The seventh data block's mark ("FEA7" and the version: 17, or 16 for <paramref name="version"/> 16), which version 16 on writes with its data block.</summary>
    private bool Data7Marked(int version = FeatPatch.Version) =>
        BytesAre(FeatPatch.Data7Va, Encoding.ASCII.GetBytes(FeatPatch.Data7Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data7Va) + 4) == version;

    /// <summary>The eighth data block's mark ("FEA8" and the version, 17), which version 17 writes with its data block.</summary>
    private bool Data8Marked() =>
        BytesAre(FeatPatch.Data8Va, Encoding.ASCII.GetBytes(FeatPatch.Data8Mark)) &&
        ReadU32(FileOffset(FeatPatch.Data8Va) + 4) == FeatPatch.Version;

    /// <summary>The spell's list entry and ability record are in the main data block (every one but Insect Plague, whose are the third block's, Poison Gas Arrow, whose are the fourth's, Dimension Door, whose are the fifth's, Expert Focus, whose are the sixth's, Phase Axe, whose are the seventh's, and Shatter, whose are the eighth's).</summary>
    private static bool InMainBlock(BorrowedSpell spell) =>
        spell is not (BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor or BorrowedSpell.ExpertFocus
            or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter);

    private bool PoolIsGame() => FeatPatch.Pool.All(s => BytesAre(s.Start, PoolOriginal(s.Start, s.End)));

    /// <summary>A vtable copy as the patch writes it: the game's vtable as this file has it, with the patch's words.</summary>
    private byte[] FeatVTable((int Va, int GameVa, (int At, uint Word)[] Words) vt)
    {
        var image = _data.AsSpan(FileOffset(vt.GameVa), FeatPatch.VTableSize).ToArray();
        foreach (var (at, word) in vt.Words) BitConverter.GetBytes(word).CopyTo(image, at);
        return image;
    }

    private static int FeatMask(IEnumerable<BorrowedSpell> spells) => spells.Aggregate(0, (m, s) => m | BorrowedSpells.Mask(s));

    private static List<BorrowedSpell> SpellsOf(int mask) => BorrowedSpells.All.Where(s => (mask & BorrowedSpells.Mask(s)) != 0).ToList();

    private static string Names(IReadOnlyList<BorrowedSpell> spells)
    {
        var names = spells.Select(s => BorrowedSpells.Info(s).Title).ToList();
        return names.Count < 2 ? string.Join("", names) : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];
    }

    /// <summary>The spells the data block says are in when it has the mark of <paramref name="version"/>, or null.</summary>
    private List<BorrowedSpell>? MarkedSpells(int version)
    {
        var at = DataVaOf(version);
        if (!BytesAre(at, FeatMark) || ReadU32(FeatDataAt(FeatPatch.VersionAt, at)) != version) return null;
        return SpellsOf((int)ReadU32(FeatDataAt(FeatPatch.SpellsAt, at)));
    }

    /// <summary>The spells version 17 says it has (its data block's mask), or null when the file doesn't have version 17's mark.</summary>
    private List<BorrowedSpell>? CurrentSpells() => MarkedSpells(FeatPatch.Version);

    /// <summary>
    /// An earlier version as WorldExplorer or featpatch.py wrote it: its mark, a mask of its own spells (none of
    /// <paramref name="without"/>, which it didn't have), their hooks (and only theirs), its code (the SHA-256 of its pieces) and
    /// its vtable copies. Null when it isn't.
    /// </summary>
    private List<BorrowedSpell>? EarlierSpells(int version, BorrowedSpell[] without, (int Va, uint Game, uint Patched, int Spells)[] hooks,
                                               (int Va, int Length)[] code, string sha256,
                                               (int Va, int GameVa, (int At, uint Word)[] Words)[] vtables)
    {
        if (MarkedSpells(version) is not { } spells) return null;
        var mask = (int)ReadU32(FeatDataAt(FeatPatch.SpellsAt, DataVaOf(version)));
        if (spells.Count == 0 || spells.Any(without.Contains) || mask != FeatMask(spells)) return null;
        if (!hooks.All(h => ReadU32(FileOffset(h.Va)) == ((h.Spells & mask) != 0 ? h.Patched : h.Game))) return null;
        if (!vtables.All(v => BytesAre(v.Va, FeatVTable(v)))) return null;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (va, length) in code)
        {
            if (_data.Length < FileOffset(va) + length) return null;
            sha.AppendData(_data, FileOffset(va), length);
        }

        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant() == sha256 ? spells : null;
    }

    /// <summary>Version 16 (all fourteen, Phase Axe the newest; its second to seventh blocks marked 16): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V16Spells() =>
        Data2Marked(FeatPatch.V16Version) && Data3Marked(FeatPatch.V16Version) && Data4Marked(FeatPatch.V16Version) &&
        Data5Marked(FeatPatch.V16Version) && Data6Marked(FeatPatch.V16Version) && Data7Marked(FeatPatch.V16Version)
            ? EarlierSpells(FeatPatch.V16Version, new[] { BorrowedSpell.Shatter }, FeatPatch.V16Hooks, FeatPatch.V16Code,
                            FeatPatch.V16CodeSha256, FeatPatch.V16VTables)
            : null;

    /// <summary>Version 15 (all thirteen, Expert Focus the newest; its second to sixth blocks marked 15): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V15Spells() =>
        Data2Marked(FeatPatch.V15Version) && Data3Marked(FeatPatch.V15Version) && Data4Marked(FeatPatch.V15Version) &&
        Data5Marked(FeatPatch.V15Version) && Data6Marked(FeatPatch.V15Version)
            ? EarlierSpells(FeatPatch.V15Version, new[] { BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter }, FeatPatch.V15Hooks,
                            FeatPatch.V15Code,
                            FeatPatch.V15CodeSha256, FeatPatch.V15VTables)
            : null;

    /// <summary>Version 14 (all twelve, Dimension Door the newest; its second, third, fourth and fifth blocks marked 14): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V14Spells() =>
        Data2Marked(FeatPatch.V14Version) && Data3Marked(FeatPatch.V14Version) && Data4Marked(FeatPatch.V14Version) &&
        Data5Marked(FeatPatch.V14Version)
            ? EarlierSpells(FeatPatch.V14Version, new[] { BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter }, FeatPatch.V14Hooks,
                            FeatPatch.V14Code,
                            FeatPatch.V14CodeSha256, FeatPatch.V14VTables)
            : null;

    /// <summary>Version 13 (all eleven, Poison Gas Arrow the newest; its second, third and fourth blocks marked 13): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V13Spells() =>
        Data2Marked(FeatPatch.V13Version) && Data3Marked(FeatPatch.V13Version) && Data4Marked(FeatPatch.V13Version)
            ? EarlierSpells(FeatPatch.V13Version, new[] { BorrowedSpell.DimensionDoor, BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter }, FeatPatch.V13Hooks, FeatPatch.V13Code,
                            FeatPatch.V13CodeSha256, FeatPatch.V13VTables)
            : null;

    /// <summary>Version 12 (all ten, Insect Plague the newest; its second and third blocks marked 12): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V12Spells() =>
        Data2Marked(FeatPatch.V12Version) && Data3Marked(FeatPatch.V12Version)
            ? EarlierSpells(FeatPatch.V12Version, new[] { BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor, BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter }, FeatPatch.V12Hooks,
                            FeatPatch.V12Code, FeatPatch.V12CodeSha256, FeatPatch.V12VTables)
            : null;

    /// <summary>Version 11 (all nine, Entangle the newest; its second block marked 11): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V11Spells() =>
        Data2Marked(FeatPatch.V11Version)
            ? EarlierSpells(FeatPatch.V11Version,
                            new[] { BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor, BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter },
                            FeatPatch.V11Hooks, FeatPatch.V11Code, FeatPatch.V11CodeSha256, FeatPatch.V11VTables)
            : null;

    /// <summary>Version 10 (all eight, Inferno Arrow the newest): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V10Spells() =>
        EarlierSpells(FeatPatch.V10Version,
                      new[] { BorrowedSpell.Entangle, BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor, BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter },
                      FeatPatch.V10Hooks, FeatPatch.V10Code, FeatPatch.V10CodeSha256, FeatPatch.V10VTables);

    /// <summary>Version 9 (all seven, the orbs in a fan): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V9Spells() =>
        EarlierSpells(FeatPatch.V9Version,
                      new[]
                      {
                          BorrowedSpell.InfernoArrow, BorrowedSpell.Entangle, BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow,
                          BorrowedSpell.DimensionDoor,
                          BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter
                      },
                      FeatPatch.V9Hooks, FeatPatch.V9Code, FeatPatch.V9CodeSha256, FeatPatch.V9VTables);

    /// <summary>Version 8 (all seven, the orbs aimed with a random scatter): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V8Spells() =>
        EarlierSpells(FeatPatch.V8Version,
                      new[]
                      {
                          BorrowedSpell.InfernoArrow, BorrowedSpell.Entangle, BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow,
                          BorrowedSpell.DimensionDoor,
                          BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter
                      },
                      FeatPatch.V8Hooks, FeatPatch.V8Code, FeatPatch.V8CodeSha256, FeatPatch.V8VTables);

    /// <summary>Version 7 (the six, without Xantam's Orbs): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V7Spells() =>
        EarlierSpells(FeatPatch.V7Version,
                      new[]
                      {
                          BorrowedSpell.XantamOrbs, BorrowedSpell.InfernoArrow, BorrowedSpell.Entangle, BorrowedSpell.InsectPlague,
                          BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor,
                          BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter
                      },
                      FeatPatch.V7Hooks, FeatPatch.V7Code, FeatPatch.V7CodeSha256, FeatPatch.V7VTables);

    /// <summary>Version 6 (the five spells, without Sorcerer's Blood): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V6Spells() =>
        EarlierSpells(FeatPatch.V6Version,
                      new[]
                      {
                          BorrowedSpell.SorcerersBlood, BorrowedSpell.XantamOrbs, BorrowedSpell.InfernoArrow, BorrowedSpell.Entangle,
                          BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor,
                          BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter
                      },
                      FeatPatch.V6Hooks, FeatPatch.V6Code, FeatPatch.V6CodeSha256, FeatPatch.V6VTables);

    /// <summary>Version 5 (the four borrowed spells, without Power Attack): see <see cref="EarlierSpells"/>.</summary>
    private List<BorrowedSpell>? V5Spells() =>
        EarlierSpells(FeatPatch.V5Version,
                      new[]
                      {
                          BorrowedSpell.PowerAttack, BorrowedSpell.SorcerersBlood, BorrowedSpell.XantamOrbs, BorrowedSpell.InfernoArrow,
                          BorrowedSpell.Entangle, BorrowedSpell.InsectPlague, BorrowedSpell.PoisonGasArrow, BorrowedSpell.DimensionDoor,
                          BorrowedSpell.ExpertFocus, BorrowedSpell.PhaseAxe, BorrowedSpell.Shatter
                      },
                      FeatPatch.V5Hooks, FeatPatch.V5Code, FeatPatch.V5CodeSha256, FeatPatch.V5VTables);

    /// <summary>
    /// Off, On, Outdated (version 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6 or 5, or Xantam's Fire alone as the fourth or third test version: updated on save) or
    /// Unrecognized; <paramref name="detail"/> says what was found, in a sentence.
    /// </summary>
    public BorrowedSpellsState GetBorrowedSpellsState(out string detail)
    {
        if (_data.Length < FileOffset(FeatEndVa.Value))
        {
            detail = "The file is too short to be this game's executable.";
            return BorrowedSpellsState.Unrecognized;
        }

        var (_, code, _, v4Code, _, _, _, _, _, _, _) = FeatPatchData.Value;
        if (CurrentSpells() is { } spells)
        {
            var mask = FeatMask(spells);
            var hooks = FeatPatch.Hooks.All(h => ReadU32(FileOffset(h.Va)) == ((h.Spells & mask) != 0 ? h.Patched : h.Game));
            if (spells.Count > 0 && hooks && code.All(c => BytesAre(c.Va, c.Bytes)) && FeatPatch.VTables.All(v => BytesAre(v.Va, FeatVTable(v))) &&
                BytesAre(FeatPatch.CastTableVa, FeatCastTable(spells)) && Data2Marked() && Data3Marked() && Data4Marked() &&
                Data5Marked() && Data6Marked() && Data7Marked() && Data8Marked() &&
                ReadU32(Data3At(FeatPatch.Data3CountAt)) == (spells.Contains(BorrowedSpell.InsectPlague) ? 1u : 0u) &&
                ReadU32(Data4At(FeatPatch.Data3CountAt)) == (spells.Contains(BorrowedSpell.PoisonGasArrow) ? 1u : 0u) &&
                ReadU32(Data5At(FeatPatch.Data3CountAt)) == (spells.Contains(BorrowedSpell.DimensionDoor) ? 1u : 0u) &&
                ReadU32(Data6At(FeatPatch.Data3CountAt)) == (spells.Contains(BorrowedSpell.ExpertFocus) ? 1u : 0u) &&
                ReadU32(Data7At(FeatPatch.Data3CountAt)) == (spells.Contains(BorrowedSpell.PhaseAxe) ? 1u : 0u) &&
                ReadU32(Data8At(FeatPatch.Data3CountAt)) == (spells.Contains(BorrowedSpell.Shatter) ? 1u : 0u))
            {
                detail = $"In (version 17, in game code nothing runs): {Names(spells)}. It works with or without three players.";
                return BorrowedSpellsState.On;
            }

            detail = "The file has the borrowed spells' data block, but its code or hooks aren't what WorldExplorer writes.";
            return BorrowedSpellsState.Unrecognized;
        }

        if (V16Spells() is { } sixteen)
        {
            detail = $"In as version 16 (the first test of Phase Axe): {Names(sixteen)}. Saving brings it to version 17, " +
                     "which adds Shatter for Adrianna; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V15Spells() is { } fifteen)
        {
            detail = $"In as version 15 (the first test of Expert Focus): {Names(fifteen)}. Saving brings it to version 17, " +
                     "which adds Phase Axe for Kromlech and Shatter for Adrianna; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V14Spells() is { } fourteen)
        {
            detail = $"In as version 14 (the first test of Dimension Door): {Names(fourteen)}. Saving brings it to version 17, " +
                     "which adds Expert Focus for Vahn, Phase Axe for Kromlech and Shatter for Adrianna; every spell keeps its " +
                     "settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V13Spells() is { } thirteen)
        {
            detail = $"In as version 13 (the first test of Poison Gas Arrow): {Names(thirteen)}. Saving brings it to version 17, " +
                     "which adds Dimension Door and Shatter for Adrianna, Expert Focus for Vahn and Phase Axe for Kromlech; every spell keeps its " +
                     "settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V12Spells() is { } twelve)
        {
            detail = $"In as version 12 (the second test of Insect Plague): {Names(twelve)}. Saving brings it to version 17, which " +
                     "adds Poison Gas Arrow and Expert Focus for Vahn, Dimension Door and Shatter for Adrianna and Phase Axe for Kromlech; every " +
                     "spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V11Spells() is { } eleven)
        {
            detail = $"In as version 11 (the first test of Entangle): {Names(eleven)}. Saving brings it to version 17, which " +
                     "adds Insect Plague, Poison Gas Arrow and Expert Focus for Vahn, Dimension Door and Shatter for Adrianna and Phase Axe for " +
                     "Kromlech; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V10Spells() is { } ten)
        {
            detail = $"In as version 10 (the first test of Inferno Arrow): {Names(ten)}. Saving brings it to version 17, which " +
                     "adds Entangle, Insect Plague, Poison Gas Arrow and Expert Focus for Vahn, Dimension Door and Shatter for Adrianna and Phase " +
                     "Axe for Kromlech; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V9Spells() is { } nine)
        {
            detail = $"In as version 9 (the second test of Xantam's Orbs): {Names(nine)}. Saving brings it to version 17, which " +
                     "adds Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Expert Focus for Vahn, Dimension Door and " +
                     "Shatter for Adrianna and Phase Axe for Kromlech; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V8Spells() is { } eight)
        {
            detail = $"In as version 8 (the first test of Xantam's Orbs): {Names(eight)}. Saving brings it to version 17, which " +
                     "fans the orbs out, lets them break barrels and crates, and adds Inferno Arrow, Entangle, Insect Plague and " +
                     "Poison Gas Arrow and Expert Focus for Vahn, Dimension Door and Shatter for Adrianna and Phase Axe for Kromlech; every spell " +
                     "keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V7Spells() is { } seven)
        {
            detail = $"In as version 7 (the first test of Sorcerer's Blood): {Names(seven)}. Saving brings it to version 17, which " +
                     "adds Xantam's Orbs, Dimension Door and Shatter for Adrianna, Inferno Arrow, Entangle, Insect Plague, Poison Gas " +
                     "Arrow and Expert Focus for Vahn and Phase Axe for Kromlech; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V6Spells() is { } six)
        {
            detail = $"In as version 6 (the first test of Power Attack): {Names(six)}. Saving brings it to version 17, which adds " +
                     "Sorcerer's Blood, Xantam's Orbs, Dimension Door and Shatter for Adrianna, Inferno Arrow, Entangle, Insect Plague, " +
                     "Poison Gas Arrow and Expert Focus for Vahn and Phase Axe for Kromlech; every spell keeps its settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V5Spells() is { } old)
        {
            detail = $"In as version 5 (the first test of Kromlech's spells): {Names(old)}. Saving brings it to version 17, which adds " +
                     "Power Attack and Phase Axe for Kromlech, Sorcerer's Blood, Xantam's Orbs, Dimension Door and Shatter for Adrianna, and " +
                     "Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow and Expert Focus for Vahn; every spell keeps its " +
                     "settings.";
            return BorrowedSpellsState.Outdated;
        }

        if (V4Present(v4Code))
        {
            detail = "Xantam's Fire is in as the fourth test version (Adrianna's spell alone). Saving brings it to version 17, which " +
                     "also has Sorcerer's Blood, Xantam's Orbs, Dimension Door, Shatter, Kromlech's spells and Vahn's Inferno Arrow, Entangle, " +
                     "Insect Plague, Poison Gas Arrow and Expert Focus; its price, mana and damage are kept.";
            return BorrowedSpellsState.Outdated;
        }

        if (V3Present())
        {
            detail = "Xantam's Fire is in as the third test version (its spell area, which only a three-player build loads). " +
                     "Saving brings it to version 17, which works with or without three players; its price, mana and damage are kept.";
            return BorrowedSpellsState.Outdated;
        }

        var other = FeatPatch.Hooks.Select(h => (h.Va, h.Game)).Concat(FeatPatch.V4Hooks.Select(h => (h.Va, h.Game)))
            .FirstOrDefault(h => ReadU32(FileOffset(h.Va)) != h.Game);
        if (other.Va != 0)
        {
            detail = $"0x{other.Va:X8}, where the borrowed spells hook in, isn't the game's code: an earlier test version (take it " +
                     "out with featpatch.py --remove) or another patch.";
            return BorrowedSpellsState.Unrecognized;
        }

        if (BorrowedSpellsProblem() is { } problem)
        {
            detail = problem;
            return BorrowedSpellsState.Unrecognized;
        }

        detail = "No borrowed spells in: they can be put in, with or without three players.";
        return BorrowedSpellsState.Off;
    }

    public BorrowedSpellsState GetBorrowedSpellsState() => GetBorrowedSpellsState(out _);

    /// <summary>Why the patch can't go in this file (its hooks being the game's), or null.</summary>
    private string? BorrowedSpellsProblem()
    {
        foreach (var (va, word) in FeatPatch.Checks)
        {
            if (ReadU32(FileOffset(va)) != word) return $"0x{va:X8} isn't the game's code, which the borrowed spells rely on.";
        }

        if (ReadU32(FileOffset(FeatPatch.MeleeCallVa)) >> 26 != 3)
            return $"0x{FeatPatch.MeleeCallVa:X8} (the hero's melee hit) isn't a call, the game's or Corrosive's.";
        foreach (var (va, text) in FeatPatch.Strings)
        {
            if (!BytesAre(va, Encoding.ASCII.GetBytes(text + "\0"))) return $"\"{text}\" isn't where the game keeps it.";
        }

        if (!BytesAre(FeatPatch.AbilityTableEndVa, "END\0"u8))
            return "The ability table doesn't end where the game's does.";

        // the game's Power Attack, Sorcerer's Blood and Inferno Arrow: their names, cast types and bits (Power Attack's mana,
        // damage and colours are the Feats & Spells tab's; Inferno Arrow's mana and damage are the patch's, put in its slot)
        foreach (var (spell, recordVa, name) in new[]
                 {
                     (BorrowedSpell.PowerAttack, FeatPatch.PowerRecordVa, FeatPatch.PowerRecordName),
                     (BorrowedSpell.SorcerersBlood, FeatPatch.SorcerersRecordVa, FeatPatch.SorcerersRecordName),
                     (BorrowedSpell.InfernoArrow, FeatPatch.InfernoRecordVa, FeatPatch.InfernoRecordName)
                 })
        {
            var record = FileOffset(recordVa);
            var info = BorrowedSpells.Info(spell);
            if (!BytesAre(recordVa, Encoding.ASCII.GetBytes(name + "\0")) ||
                BitConverter.ToInt16(_data, record + 0x20) != info.CastType || ReadU64(record + 0x48) != 1UL << info.Bit)
                return $"The game's {info.Title} record (0x{recordVa:X8}) isn't the game's any more.";
        }

        for (var va = FeatPatch.AbilityTableVa; va < FeatPatch.AbilityTableEndVa; va += 0x50)
        {
            var at = FileOffset(va);
            foreach (var spell in BorrowedSpells.All)
            {
                var info = BorrowedSpells.Info(spell);
                if (info.UsesGameRecord) continue;      // Power Attack, Sorcerer's Blood, Inferno Arrow: the game's records are theirs
                if ((ReadU64(at + 0x48) & 1UL << info.Bit) != 0 || BitConverter.ToInt16(_data, at + 0x20) == info.CastType)
                    return $"An ability already uses bit {info.Bit} or cast type {info.CastType}, which {info.Title} needs.";
            }

            var ia = BorrowedSpells.Info(BorrowedSpell.InfernoArrow);
            if (va != FeatPatch.InfernoRecordVa &&
                ((ReadU64(at + 0x48) & 1UL << ia.Bit) != 0 || BitConverter.ToInt16(_data, at + 0x20) == ia.CastType))
                return $"Another ability (0x{va:X8}) has bit {ia.Bit} or cast type {ia.CastType}, Inferno Arrow's.";

            var sb = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood);
            if (va != FeatPatch.SorcerersRecordVa && (ReadU64(at + 0x48) & 1UL << sb.Bit) != 0)
                return $"Another ability (0x{va:X8}) has bit {sb.Bit}, Sorcerer's Blood's.";
        }

        // the game's own segment loads the pool (the first program header: type 1, file 0x1000, address 0x00100000)
        var ph = (int)ReadU32(0x1C);
        if (ph <= 0 || ph + 32 > _data.Length || ReadU32(ph) != 1 || ReadU32(ph + 4) != 0x1000 || ReadU32(ph + 8) != 0x00100000 ||
            ReadU32(ph + 4) + ReadU32(ph + 16) < (uint)FileOffset(FeatPatch.Pool[^1].End))
            return "The executable's first program header isn't the game's.";
        var lui = ReadU32(FileOffset(FeatPatch.ListsLuiVa));
        var addiu = ReadU32(FileOffset(FeatPatch.ListsAddiuVa));
        var lists = (int)((lui & 0xFFFF) << 16) + (short)(addiu & 0xFFFF);
        if (lui >> 16 != 0x3C03 || addiu >> 16 != 0x246A || lists is not (0x002A0478 or 0x000D0100))
            return "FUN_00162B78, which builds the level-up lists, isn't the game's or the three-player builds'.";
        if (!PoolIsGame())
            return "The unused game code where the borrowed spells go holds something else: another patch uses it.";
        return null;
    }

    /// <summary>The fourth test version: its "FEAT" version 4 mark in the data block, its six hooks and its code.</summary>
    private bool V4Present(byte[] v4Code) =>
        BytesAre(FeatPatch.LegacyDataVa, FeatMark) && ReadU32(FeatDataAt(FeatPatch.VersionAt, FeatPatch.LegacyDataVa)) == 4 &&
        FeatPatch.V4Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Patched) && BytesAre(FeatPatch.V4CodeVa, v4Code);

    /// <summary>The third test version: its "FEAT" version 3 mark in the spell area's bytes and its seven hooks.</summary>
    private bool V3Present() =>
        _data.Length >= FeatPatch.V3AreaFile + FeatPatch.V3AreaSize &&
        _data.AsSpan(FeatPatch.V3AreaFile, 4).SequenceEqual(FeatMark) && ReadU32(FeatPatch.V3AreaFile + 4) == 3 &&
        FeatPatch.V3Hooks.All(h => ReadU32(FileOffset(h.Va)) == h.Patched);

    /// <summary>
    /// Where a version's data block is and keeps the settings (version 17's, 16's, 15's, 14's, 13's, 12's, 11's and 10's at
    /// <see cref="FeatPatch.DataVa"/>, every earlier one's at <see cref="FeatPatch.LegacyDataVa"/>; the entries are where the
    /// others' are; the orbs' tables are versions 17's to 8's alone, Inferno Arrow's (<see cref="Inferno"/>: its mana, its least
    /// and most damage, then its five further tables) versions 17's to 10's, Entangle's versions 17's to 11's, in their second
    /// block, Insect Plague's versions 17's to 12's, in their third, Poison Gas Arrow's versions 17's to 13's, in their fourth,
    /// Dimension Door's versions 17's to 14's, in their fifth, Expert Focus's versions 17's to 15's, in their sixth, Phase
    /// Axe's versions 17's and 16's, in their seventh, Shatter's version 17's, in its eighth). Version 16's, 15's, 14's, 13's,
    /// 12's and 11's layouts are version 17's (the second block shorter: its run-time icons alone).
    /// </summary>
    private sealed record FeatLayout(int DataVa, int Entries, int Abilities, int BarrierMin, int BarrierMax, int FieldFrames,
                                     int MightDamage, int MightDefence, int PowerHits, int OrbsCount = -1, int OrbsMin = -1,
                                     int OrbsMax = -1, int[]? Inferno = null);

    private static readonly FeatLayout CurrentLayout = new(FeatPatch.DataVa, FeatPatch.EntriesAt, FeatPatch.AbilitiesAt,
        FeatPatch.BarrierDamageMinAt, FeatPatch.BarrierDamageMaxAt, FeatPatch.FieldFramesAt, FeatPatch.MightDamageAt,
        FeatPatch.MightDefenceAt, FeatPatch.PowerHitsAt, FeatPatch.OrbsCountAt, FeatPatch.OrbsDamageMinAt, FeatPatch.OrbsDamageMaxAt,
        new[]
        {
            FeatPatch.InfernoManaAt, FeatPatch.InfernoDamageMinAt, FeatPatch.InfernoDamageMaxAt, FeatPatch.InfernoBlastAt,
            FeatPatch.InfernoSpreadAt, FeatPatch.InfernoBurnAt, FeatPatch.InfernoFlameMinAt, FeatPatch.InfernoFlameMaxAt
        });

    private static readonly FeatLayout V10Layout = new(FeatPatch.DataVa, FeatPatch.V10EntriesAt, FeatPatch.V10AbilitiesAt,
        FeatPatch.V10BarrierDamageMinAt, FeatPatch.V10BarrierDamageMaxAt, FeatPatch.V10FieldFramesAt, FeatPatch.V10MightDamageAt,
        FeatPatch.V10MightDefenceAt, FeatPatch.V10PowerHitsAt, FeatPatch.V10OrbsCountAt, FeatPatch.V10OrbsDamageMinAt,
        FeatPatch.V10OrbsDamageMaxAt,
        new[]
        {
            FeatPatch.V10InfernoManaAt, FeatPatch.V10InfernoDamageMinAt, FeatPatch.V10InfernoDamageMaxAt, FeatPatch.V10InfernoBlastAt,
            FeatPatch.V10InfernoSpreadAt, FeatPatch.V10InfernoBurnAt, FeatPatch.V10InfernoFlameMinAt, FeatPatch.V10InfernoFlameMaxAt
        });

    private static readonly FeatLayout V9Layout = new(FeatPatch.LegacyDataVa, FeatPatch.V9EntriesAt, FeatPatch.V9AbilitiesAt,
        FeatPatch.V9BarrierDamageMinAt, FeatPatch.V9BarrierDamageMaxAt, FeatPatch.V9FieldFramesAt, FeatPatch.V9MightDamageAt,
        FeatPatch.V9MightDefenceAt, FeatPatch.V9PowerHitsAt, FeatPatch.V9OrbsCountAt, FeatPatch.V9OrbsDamageMinAt,
        FeatPatch.V9OrbsDamageMaxAt);

    private static readonly FeatLayout V8Layout = new(FeatPatch.LegacyDataVa, FeatPatch.V8EntriesAt, FeatPatch.V8AbilitiesAt, FeatPatch.V8BarrierDamageMinAt,
        FeatPatch.V8BarrierDamageMaxAt, FeatPatch.V8FieldFramesAt, FeatPatch.V8MightDamageAt, FeatPatch.V8MightDefenceAt,
        FeatPatch.V8PowerHitsAt, FeatPatch.V8OrbsCountAt, FeatPatch.V8OrbsDamageMinAt, FeatPatch.V8OrbsDamageMaxAt);

    private static readonly FeatLayout V7Layout = new(FeatPatch.LegacyDataVa, FeatPatch.V7EntriesAt, FeatPatch.V7AbilitiesAt, FeatPatch.V7BarrierDamageMinAt,
        FeatPatch.V7BarrierDamageMaxAt, FeatPatch.V7FieldFramesAt, FeatPatch.V7MightDamageAt, FeatPatch.V7MightDefenceAt,
        FeatPatch.V7PowerHitsAt);

    private static readonly FeatLayout V6Layout = new(FeatPatch.LegacyDataVa, FeatPatch.V6EntriesAt, FeatPatch.V6AbilitiesAt, FeatPatch.V6BarrierDamageMinAt,
        FeatPatch.V6BarrierDamageMaxAt, FeatPatch.V6FieldFramesAt, FeatPatch.V6MightDamageAt, FeatPatch.V6MightDefenceAt,
        FeatPatch.V6PowerHitsAt);

    private static readonly FeatLayout V5Layout = new(FeatPatch.LegacyDataVa, FeatPatch.V5EntriesAt, FeatPatch.V5AbilitiesAt, FeatPatch.V5BarrierDamageMinAt,
        FeatPatch.V5BarrierDamageMaxAt, FeatPatch.V5FieldFramesAt, FeatPatch.V5MightDamageAt, FeatPatch.V5MightDefenceAt, -1);

    /// <summary>
    /// The spells in the file and their settings (On; Outdated: version 16's, 15's, 14's, 13's, 12's, 11's, 10's, 9's, 8's, 7's, 6's or 5's spells, or Xantam's Fire's), or none (Off,
    /// Unrecognized). Power Attack's mana is 0: its record's is the Feats &amp; Spells tab's. Sorcerer's Blood's is 0 too: it
    /// is passive.
    /// </summary>
    public IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> GetBorrowedSpells()
    {
        var found = new Dictionary<BorrowedSpell, BorrowedSpellSettings>();
        var state = GetBorrowedSpellsState(out _);
        if (state == BorrowedSpellsState.Outdated && V16Spells() is { } sixteen)
            return ReadAll(sixteen, CurrentLayout);
        if (state == BorrowedSpellsState.Outdated && V15Spells() is { } fifteen)
            return ReadAll(fifteen, CurrentLayout);
        if (state == BorrowedSpellsState.Outdated && V14Spells() is { } fourteen)
            return ReadAll(fourteen, CurrentLayout);
        if (state == BorrowedSpellsState.Outdated && V13Spells() is { } thirteen)
            return ReadAll(thirteen, CurrentLayout);
        if (state == BorrowedSpellsState.Outdated && V12Spells() is { } twelve)
            return ReadAll(twelve, CurrentLayout);
        if (state == BorrowedSpellsState.Outdated && V11Spells() is { } eleven)
            return ReadAll(eleven, CurrentLayout);
        if (state == BorrowedSpellsState.Outdated && V10Spells() is { } ten)
            return ReadAll(ten, V10Layout);
        if (state == BorrowedSpellsState.Outdated && V9Spells() is { } nine)
            return ReadAll(nine, V9Layout);
        if (state == BorrowedSpellsState.Outdated && V8Spells() is { } eight)
            return ReadAll(eight, V8Layout);
        if (state == BorrowedSpellsState.Outdated && V7Spells() is { } seven)
            return ReadAll(seven, V7Layout);
        if (state == BorrowedSpellsState.Outdated && V6Spells() is { } six)
            return ReadAll(six, V6Layout);
        if (state == BorrowedSpellsState.Outdated && V5Spells() is { } old)
            return ReadAll(old, V5Layout);       // every spell had its own ability record, in the order of the entries

        if (state == BorrowedSpellsState.Outdated)
        {
            var block = V3Present() ? FeatPatch.V3AreaFile : FeatDataAt(0, FeatPatch.LegacyDataVa);   // the same layout in both
            found[BorrowedSpell.XantamFire] = new BorrowedSpellSettings(
                _data[block + FeatPatch.V4PriceAt], BitConverter.ToSingle(_data, block + FeatPatch.V4ManaAt),
                ByRank(block + FeatPatch.V4DamageMinAt), ByRank(block + FeatPatch.V4DamageMaxAt));
            return found;
        }

        return state == BorrowedSpellsState.On ? ReadAll(CurrentSpells()!, CurrentLayout) : found;
    }

    /// <summary>
    /// The settings of <paramref name="spells"/> (in the data block's order) where <paramref name="layout"/> keeps them; Insect
    /// Plague's from the third block, Poison Gas Arrow's from the fourth, Dimension Door's from the fifth, Expert Focus's from
    /// the sixth, Phase Axe's from the seventh and Shatter's from the eighth (their entries and records are there, not among
    /// the main block's).
    /// </summary>
    private Dictionary<BorrowedSpell, BorrowedSpellSettings> ReadAll(List<BorrowedSpell> spells, FeatLayout layout)
    {
        var found = new Dictionary<BorrowedSpell, BorrowedSpellSettings>();
        var own = 0;
        var i = 0;
        foreach (var spell in spells)
        {
            if (!InMainBlock(spell))
            {
                found[spell] = spell switch
                {
                    BorrowedSpell.InsectPlague => ReadInsectPlague(),
                    BorrowedSpell.PoisonGasArrow => ReadPoisonGasArrow(),
                    BorrowedSpell.DimensionDoor => ReadDimensionDoor(),
                    BorrowedSpell.ExpertFocus => ReadExpertFocus(),
                    BorrowedSpell.PhaseAxe => ReadPhaseAxe(),
                    _ => ReadShatter()
                };
                continue;
            }

            var record = BorrowedSpells.Data(spell).OwnRecord ? own++ : -1;
            found[spell] = ReadSettings(spell, layout.Entries + FeatPatch.EntryStride * i++,
                                        record < 0 ? -1 : layout.Abilities + FeatPatch.AbilityStride * record, layout);
        }

        return found;
    }

    /// <summary>Insect Plague's settings from the third block: the price in its entry, the mana in its record, its tables.</summary>
    private BorrowedSpellSettings ReadInsectPlague() =>
        new(_data[Data3At(FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt)],
            BitConverter.ToSingle(_data, Data3At(FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt)),
            ByRank(Data3At(FeatPatch.InsectStingMinAt)), ByRank(Data3At(FeatPatch.InsectStingMaxAt)),
            Third: ByRank(Data3At(FeatPatch.InsectSwarmsAt)), More: new IReadOnlyList<int>[] { ByRank(Data3At(FeatPatch.InsectTimeAt)) });

    /// <summary>Poison Gas Arrow's settings from the fourth block: the price in its entry, the mana in its record, its tables.</summary>
    private BorrowedSpellSettings ReadPoisonGasArrow() =>
        new(_data[Data4At(FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt)],
            BitConverter.ToSingle(_data, Data4At(FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt)),
            ByRank(Data4At(FeatPatch.GasArrowMinAt)), ByRank(Data4At(FeatPatch.GasArrowMaxAt)),
            More: GasTables.Select(t => (IReadOnlyList<int>)ByRank(Data4At(t))).ToArray());

    /// <summary>Poison Gas Arrow's further tables by rank in the fourth block, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly int[] GasTables = { FeatPatch.GasReachAt, FeatPatch.GasTimeAt, FeatPatch.GasMinAt, FeatPatch.GasMaxAt };

    /// <summary>
    /// Dimension Door's settings from the fifth block: the price and the ranks in its entry, the mana in its record, its distances
    /// at ranks 1-3 (u16s).
    /// </summary>
    private BorrowedSpellSettings ReadDimensionDoor() =>
        new(_data[Data5At(FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt)],
            BitConverter.ToSingle(_data, Data5At(FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt)),
            Enumerable.Range(1, FeatPatch.DoorRanks).Select(r => (int)ReadU16(Data5At(FeatPatch.DoorDistanceAt + 2 * r))).ToArray(),
            Array.Empty<int>(), _data[Data5At(FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt)]);

    /// <summary>
    /// Expert Focus's settings from the sixth block: the price in its entry, the mana in its record, its percentages and its times
    /// (tenths of a second) by rank.
    /// </summary>
    private BorrowedSpellSettings ReadExpertFocus() =>
        new(_data[Data6At(FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt)],
            BitConverter.ToSingle(_data, Data6At(FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt)),
            ByRank(Data6At(FeatPatch.FocusPercentAt)), Array.Empty<int>(),
            More: new IReadOnlyList<int>[] { ByRank(Data6At(FeatPatch.FocusTimeAt)) });

    /// <summary>Phase Axe's settings from the seventh block: the price in its entry, the mana in its record, its damage by rank.</summary>
    private BorrowedSpellSettings ReadPhaseAxe() =>
        new(_data[Data7At(FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt)],
            BitConverter.ToSingle(_data, Data7At(FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt)),
            ByRank(Data7At(FeatPatch.AxeDamageMinAt)), ByRank(Data7At(FeatPatch.AxeDamageMaxAt)));

    /// <summary>Shatter's settings from the eighth block: the price in its entry, the mana in its record, its damage, reach, freeze and cooldown by rank.</summary>
    private BorrowedSpellSettings ReadShatter() =>
        new(_data[Data8At(FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt)],
            BitConverter.ToSingle(_data, Data8At(FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt)),
            ByRank(Data8At(FeatPatch.ShatterDamageMinAt)), ByRank(Data8At(FeatPatch.ShatterDamageMaxAt)),
            More: ShatterTables.Select(t => (IReadOnlyList<int>)ByRank(Data8At(t))).ToArray());

    /// <summary>Shatter's further tables in the eighth block, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly int[] ShatterTables = { FeatPatch.ShatterReachAt, FeatPatch.ShatterFreezeAt, FeatPatch.ShatterCooldownAt };

    /// <summary>
    /// One spell's settings from the data block: the price (and Sorcerer's Blood's ranks) from its entry (at
    /// <paramref name="entry"/>), the mana from its ability record (at <paramref name="ability"/>; -1: none, Power Attack and
    /// Sorcerer's Blood; Inferno Arrow's is the data block's own), its values by rank where <paramref name="layout"/>'s version
    /// keeps them (Entangle's in the second block).
    /// </summary>
    private BorrowedSpellSettings ReadSettings(BorrowedSpell spell, int entry, int ability, FeatLayout layout)
    {
        int At(int offset) => FeatDataAt(offset, layout.DataVa);
        var price = _data[At(entry + FeatPatch.EntryPriceAt)];
        var mana = ability < 0 ? 0f : BitConverter.ToSingle(_data, At(ability + FeatPatch.AbilityManaAt));
        return spell switch
        {
            BorrowedSpell.XantamFire => new BorrowedSpellSettings(price, mana, ByRank(At(FeatPatch.XantamDamageMinAt)),
                                                                  ByRank(At(FeatPatch.XantamDamageMaxAt))),
            BorrowedSpell.PowerAttack => new BorrowedSpellSettings(price, 0f, ByRank(At(layout.PowerHits)), Array.Empty<int>()),
            BorrowedSpell.BladeBarrier => new BorrowedSpellSettings(price, mana, ByRank(At(layout.BarrierMin)),
                                                                    ByRank(At(layout.BarrierMax))),
            BorrowedSpell.AntimagicField => new BorrowedSpellSettings(price, mana,
                Enumerable.Range(1, BorrowedSpellSettings.Ranks).Select(r => ReadU16(At(layout.FieldFrames + 2 * r)) / 50).ToArray(),
                Array.Empty<int>()),
            BorrowedSpell.SorcerersBlood => new BorrowedSpellSettings(price, 0f, Array.Empty<int>(), Array.Empty<int>(),
                                                                      _data[At(entry + FeatPatch.EntryRanksAt)]),
            BorrowedSpell.XantamOrbs => new BorrowedSpellSettings(price, mana, ByRank(At(layout.OrbsMin)),
                                                                  ByRank(At(layout.OrbsMax)),
                                                                  Third: ByRank(At(layout.OrbsCount))),
            BorrowedSpell.InfernoArrow => new BorrowedSpellSettings(price, BitConverter.ToSingle(_data, At(layout.Inferno![0])),
                ByRank(At(layout.Inferno[1])), ByRank(At(layout.Inferno[2])),
                More: layout.Inferno.Skip(3).Select(t => (IReadOnlyList<int>)ByRank(At(t))).ToArray()),
            BorrowedSpell.Entangle => new BorrowedSpellSettings(price, mana, ByRank(Data2At(FeatPatch.EntangleThornMinAt)),
                ByRank(Data2At(FeatPatch.EntangleThornMaxAt)), Third: ByRank(Data2At(FeatPatch.EntangleVinesAt)),
                More: new IReadOnlyList<int>[]
                {
                    ByRank(Data2At(FeatPatch.EntangleSlowAt)).Select(v => 2 * v).ToArray(), ByRank(Data2At(FeatPatch.EntangleTimeAt))
                }),
            _ => new BorrowedSpellSettings(price, mana, ByRank(At(layout.MightDamage)), ByRank(At(layout.MightDefence)))
        };
    }

    /// <summary>Inferno Arrow's further tables by rank in the data block, in the order of <see cref="BorrowedSpellSettings.More"/>.</summary>
    private static readonly int[] InfernoTables =
    {
        FeatPatch.InfernoBlastAt, FeatPatch.InfernoSpreadAt, FeatPatch.InfernoBurnAt, FeatPatch.InfernoFlameMinAt,
        FeatPatch.InfernoFlameMaxAt
    };

    /// <summary>The file offset of <paramref name="offset"/> in the second data block (version 17's, 16's, 15's, 14's, 13's, 12's and 11's).</summary>
    private int Data2At(int offset) => FileOffset(FeatPatch.Data2Va) + offset;

    /// <summary>The file offset of <paramref name="offset"/> in the third data block (version 17's, 16's, 15's, 14's, 13's and 12's).</summary>
    private int Data3At(int offset) => FileOffset(FeatPatch.Data3Va) + offset;

    /// <summary>The file offset of <paramref name="offset"/> in the fourth data block (version 17's, 16's, 15's, 14's and 13's).</summary>
    private int Data4At(int offset) => FileOffset(FeatPatch.Data4Va) + offset;

    /// <summary>The file offset of <paramref name="offset"/> in the fifth data block (version 17's, 16's, 15's and 14's).</summary>
    private int Data5At(int offset) => FileOffset(FeatPatch.Data5Va) + offset;

    /// <summary>The file offset of <paramref name="offset"/> in the sixth data block (version 17's, 16's and 15's).</summary>
    private int Data6At(int offset) => FileOffset(FeatPatch.Data6Va) + offset;

    /// <summary>The file offset of <paramref name="offset"/> in the seventh data block (version 17's and 16's).</summary>
    private int Data7At(int offset) => FileOffset(FeatPatch.Data7Va) + offset;

    /// <summary>The file offset of <paramref name="offset"/> in the eighth data block (version 17's).</summary>
    private int Data8At(int offset) => FileOffset(FeatPatch.Data8Va) + offset;

    /// <summary>Ranks 1-5 of a by-rank byte table (its entry 0 is rank 0's).</summary>
    private int[] ByRank(int at) => Enumerable.Range(1, BorrowedSpellSettings.Ranks).Select(r => (int)_data[at + r]).ToArray();

    /// <summary>
    /// Puts in, updates (Outdated: to version 17), changes or takes out the borrowed spells so that exactly
    /// <paramref name="spells"/> are in, with their settings (none: <see cref="RemoveBorrowedSpells"/>). Throws
    /// <see cref="ArgumentException"/> on bad settings and <see cref="InvalidOperationException"/> when the state is
    /// Unrecognized; nothing is changed then.
    /// </summary>
    public void SetBorrowedSpells(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        foreach (var (spell, settings) in spells)
        {
            if (settings.Problem(spell) is { } problem) throw new ArgumentException(problem, nameof(spells));
        }

        if (spells.Count == 0)
        {
            RemoveBorrowedSpells();
            return;
        }

        var state = GetBorrowedSpellsState(out var detail);
        if (state == BorrowedSpellsState.Unrecognized)
            throw new InvalidOperationException("The borrowed spells can't be put in: " + detail);
        if (state == BorrowedSpellsState.Outdated)
        {
            var copy = (byte[])_data.Clone();
            RemoveOutdated();
            if (BorrowedSpellsProblem() is { } left)
            {
                _data = copy;
                throw new InvalidOperationException("The borrowed spells can't be updated: " + left);
            }
        }

        var (_, code, _, _, _, _, _, _, _, _, _) = FeatPatchData.Value;
        if (state != BorrowedSpellsState.On)
        {
            foreach (var (va, bytes) in code) bytes.CopyTo(_data, FileOffset(va));
            foreach (var vt in FeatPatch.VTables) FeatVTable(vt).CopyTo(_data, FileOffset(vt.Va));
        }

        FeatDataBlock(spells).CopyTo(_data, FeatDataAt(0));
        FeatData2Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data2Va));
        FeatData3Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data3Va));
        FeatData4Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data4Va));
        FeatData5Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data5Va));
        FeatData6Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data6Va));
        FeatData7Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data7Va));
        FeatData8Block(spells).CopyTo(_data, FileOffset(FeatPatch.Data8Va));
        FeatCastTable(spells.Keys).CopyTo(_data, FileOffset(FeatPatch.CastTableVa));
        var mask = FeatMask(spells.Keys);
        foreach (var (va, game, patched, need) in FeatPatch.Hooks) WriteInt(FileOffset(va), (int)((need & mask) != 0 ? patched : game));
    }

    /// <summary>
    /// The data block for <paramref name="spells"/>, as featpatch.py's data_block makes it: the mask of every one that is on, a
    /// list entry for each but Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter (the third to
    /// eighth blocks'), in the order of <see cref="BorrowedSpells.All"/>, an ability record for each but Power Attack, Sorcerer's
    /// Blood, Inferno Arrow, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter, and each one's
    /// tables.
    /// </summary>
    private byte[] FeatDataBlock(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data.Clone();
        var on = BorrowedSpells.All.Where(s => spells.ContainsKey(s) && InMainBlock(s)).ToList();
        var own = on.Count(s => BorrowedSpells.Data(s).OwnRecord);
        BitConverter.GetBytes(on.Count).CopyTo(block, FeatPatch.CountAt);
        BitConverter.GetBytes(own).CopyTo(block, FeatPatch.AbilityCountAt);
        BitConverter.GetBytes(FeatMask(spells.Keys)).CopyTo(block, FeatPatch.SpellsAt);
        var record = 0;
        for (var i = 0; i < on.Count; i++)
        {
            var spell = on[i];
            var s = spells[spell];
            var data = BorrowedSpells.Data(spell);
            var entry = FeatPatch.EntriesAt + FeatPatch.EntryStride * i;
            Convert.FromBase64String(data.EntryBase64).CopyTo(block, entry);
            block[entry + FeatPatch.EntryPriceAt] = (byte)s.Price;
            block[entry + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
            var ability = -1;
            if (data.OwnRecord)
            {
                ability = FeatPatch.AbilitiesAt + FeatPatch.AbilityStride * record++;
                Convert.FromBase64String(data.AbilityBase64).CopyTo(block, ability);
                BitConverter.GetBytes(s.Mana).CopyTo(block, ability + FeatPatch.AbilityManaAt);
            }

            switch (spell)
            {
                case BorrowedSpell.XantamFire:
                case BorrowedSpell.BladeBarrier:
                case BorrowedSpell.XantamOrbs:
                    // the ability record's damage: the HUD's line until the first cast (the cast then writes the rank's into the slot)
                    BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, ability + FeatPatch.AbilityDamageAt);
                    BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, ability + FeatPatch.AbilityDamageAt + 2);
                    var (min, max) = spell switch
                    {
                        BorrowedSpell.XantamFire => (FeatPatch.XantamDamageMinAt, FeatPatch.XantamDamageMaxAt),
                        BorrowedSpell.BladeBarrier => (FeatPatch.BarrierDamageMinAt, FeatPatch.BarrierDamageMaxAt),
                        _ => (FeatPatch.OrbsDamageMinAt, FeatPatch.OrbsDamageMaxAt)
                    };
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                    {
                        block[min + r + 1] = (byte)s.First[r];
                        block[max + r + 1] = (byte)s.Second[r];
                        if (spell == BorrowedSpell.XantamOrbs) block[FeatPatch.OrbsCountAt + r + 1] = (byte)s.ThirdValues[r];
                    }

                    break;
                case BorrowedSpell.PowerAttack:
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++) block[FeatPatch.PowerHitsAt + r + 1] = (byte)s.First[r];
                    break;
                case BorrowedSpell.AntimagicField:
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                        BitConverter.GetBytes((ushort)(50 * s.First[r])).CopyTo(block, FeatPatch.FieldFramesAt + 2 * (r + 1));
                    break;
                case BorrowedSpell.RighteousMight:
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                    {
                        block[FeatPatch.MightDamageAt + r + 1] = (byte)s.First[r];
                        block[FeatPatch.MightDefenceAt + r + 1] = (byte)s.Second[r];
                    }

                    break;
                case BorrowedSpell.Entangle:
                    // the ability record's damage (a thorn's): the HUD's line until the first cast; its tables are in the second block
                    BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, ability + FeatPatch.AbilityDamageAt);
                    BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, ability + FeatPatch.AbilityDamageAt + 2);
                    break;
                case BorrowedSpell.InfernoArrow:
                    // its mana goes into its slot at every shot (the game's record keeps its own)
                    BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.InfernoManaAt);
                    for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
                    {
                        block[FeatPatch.InfernoDamageMinAt + r + 1] = (byte)s.First[r];
                        block[FeatPatch.InfernoDamageMaxAt + r + 1] = (byte)s.Second[r];
                        for (var k = 0; k < InfernoTables.Length; k++) block[InfernoTables[k] + r + 1] = (byte)s.MoreValues[k][r];
                    }

                    break;
            }
        }

        return block;
    }

    /// <summary>
    /// The second data block for <paramref name="spells"/>, as featpatch.py's data2_block makes it: its mark, the names of the
    /// icons of types 32-36 and their fallbacks, Entangle's settings (its defaults when it is off: the template's) and its fixed
    /// values.
    /// </summary>
    private static byte[] FeatData2Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data2.Clone();
        if (!spells.TryGetValue(BorrowedSpell.Entangle, out var s)) return block;
        for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
        {
            block[FeatPatch.EntangleThornMinAt + r + 1] = (byte)s.First[r];
            block[FeatPatch.EntangleThornMaxAt + r + 1] = (byte)s.Second[r];
            block[FeatPatch.EntangleVinesAt + r + 1] = (byte)s.ThirdValues[r];
            block[FeatPatch.EntangleSlowAt + r + 1] = (byte)(s.MoreValues[0][r] / 2);
            block[FeatPatch.EntangleTimeAt + r + 1] = (byte)s.MoreValues[1][r];
        }

        return block;
    }

    /// <summary>
    /// The third data block for <paramref name="spells"/>, as featpatch.py's data3_block makes it: its mark, Insect Plague's list
    /// entry and ability record (with the counts, 1 and 1) when it is on, the names of its icon and its fallback, its settings (its
    /// defaults when it is off: the template's) and its fixed values; its poison records zero.
    /// </summary>
    private static byte[] FeatData3Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data3.Clone();
        if (!spells.TryGetValue(BorrowedSpell.InsectPlague, out var s)) return block;
        var data = BorrowedSpells.Data(BorrowedSpell.InsectPlague);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3CountAt);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3AbilityCountAt);
        Convert.FromBase64String(data.EntryBase64).CopyTo(block, FeatPatch.Data3EntryAt);
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = (byte)s.Price;
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
        Convert.FromBase64String(data.AbilityBase64).CopyTo(block, FeatPatch.Data3AbilityAt);
        BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);
        // the ability record's damage (a sting's): the HUD's line until the first cast
        BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt);
        BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt + 2);
        for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
        {
            block[FeatPatch.InsectSwarmsAt + r + 1] = (byte)s.ThirdValues[r];
            block[FeatPatch.InsectTimeAt + r + 1] = (byte)s.MoreValues[0][r];
            block[FeatPatch.InsectStingMinAt + r + 1] = (byte)s.First[r];
            block[FeatPatch.InsectStingMaxAt + r + 1] = (byte)s.Second[r];
        }

        return block;
    }

    /// <summary>
    /// The fourth data block for <paramref name="spells"/>, as featpatch.py's data4_block makes it: its mark, Poison Gas Arrow's
    /// list entry and ability record (with the counts, 1 and 1) when it is on, the names of its icon and its fallback, its settings
    /// (its defaults when it is off: the template's) and the frames between the cloud's hits.
    /// </summary>
    private static byte[] FeatData4Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data4.Clone();
        if (!spells.TryGetValue(BorrowedSpell.PoisonGasArrow, out var s)) return block;
        var data = BorrowedSpells.Data(BorrowedSpell.PoisonGasArrow);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3CountAt);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3AbilityCountAt);
        Convert.FromBase64String(data.EntryBase64).CopyTo(block, FeatPatch.Data3EntryAt);
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = (byte)s.Price;
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
        Convert.FromBase64String(data.AbilityBase64).CopyTo(block, FeatPatch.Data3AbilityAt);
        BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);
        // the ability record's damage (the arrow's): the HUD's line until the first shot
        BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt);
        BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt + 2);
        for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
        {
            block[FeatPatch.GasArrowMinAt + r + 1] = (byte)s.First[r];
            block[FeatPatch.GasArrowMaxAt + r + 1] = (byte)s.Second[r];
            for (var k = 0; k < GasTables.Length; k++) block[GasTables[k] + r + 1] = (byte)s.MoreValues[k][r];
        }

        return block;
    }

    /// <summary>
    /// The fifth data block for <paramref name="spells"/>, as featpatch.py's data5_block makes it: its mark, Dimension Door's list
    /// entry and ability record (with the counts, 1 and 1) when it is on, the names of its icon and its fallback, its distances (its
    /// defaults when it is off: the template's; ranks 4 and 5, which its entry doesn't offer, as rank 3), its HUD line's key and its
    /// fixed values.
    /// </summary>
    private static byte[] FeatData5Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data5.Clone();
        if (!spells.TryGetValue(BorrowedSpell.DimensionDoor, out var s)) return block;
        var data = BorrowedSpells.Data(BorrowedSpell.DimensionDoor);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3CountAt);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3AbilityCountAt);
        Convert.FromBase64String(data.EntryBase64).CopyTo(block, FeatPatch.Data3EntryAt);
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = (byte)s.Price;
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
        Convert.FromBase64String(data.AbilityBase64).CopyTo(block, FeatPatch.Data3AbilityAt);
        BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);
        for (var r = 1; r <= BorrowedSpellSettings.Ranks; r++)
            BitConverter.GetBytes((ushort)s.First[Math.Min(r, s.First.Count) - 1]).CopyTo(block, FeatPatch.DoorDistanceAt + 2 * r);
        return block;
    }

    /// <summary>
    /// The sixth data block for <paramref name="spells"/>, as featpatch.py's data6_block makes it: its mark, Expert Focus's list
    /// entry and ability record (with the counts, 1 and 1) when it is on, the names of its icon and its fallback, its percentages
    /// and times by rank (its defaults when it is off: the template's), its HUD line's key and the longest focus; its run-time
    /// fields (each player's focus: the frame it ends, its percentage) zero.
    /// </summary>
    private static byte[] FeatData6Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data6.Clone();
        if (!spells.TryGetValue(BorrowedSpell.ExpertFocus, out var s)) return block;
        var data = BorrowedSpells.Data(BorrowedSpell.ExpertFocus);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3CountAt);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3AbilityCountAt);
        Convert.FromBase64String(data.EntryBase64).CopyTo(block, FeatPatch.Data3EntryAt);
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = (byte)s.Price;
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
        Convert.FromBase64String(data.AbilityBase64).CopyTo(block, FeatPatch.Data3AbilityAt);
        BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);
        for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
        {
            block[FeatPatch.FocusPercentAt + r + 1] = (byte)s.First[r];
            block[FeatPatch.FocusTimeAt + r + 1] = (byte)s.MoreValues[0][r];
        }

        return block;
    }

    /// <summary>
    /// The seventh data block for <paramref name="spells"/>, as featpatch.py's data7_block makes it: its mark, Phase Axe's list
    /// entry and ability record (with the counts, 1 and 1, and the record's damage the first rank's: the HUD's line until the first
    /// cast) when it is on, the names of its icon and its fallback, its damage by rank (its defaults when it is off: the
    /// template's), then the axe's fixed values.
    /// </summary>
    private static byte[] FeatData7Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data7.Clone();
        if (!spells.TryGetValue(BorrowedSpell.PhaseAxe, out var s)) return block;
        var data = BorrowedSpells.Data(BorrowedSpell.PhaseAxe);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3CountAt);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3AbilityCountAt);
        Convert.FromBase64String(data.EntryBase64).CopyTo(block, FeatPatch.Data3EntryAt);
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = (byte)s.Price;
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
        Convert.FromBase64String(data.AbilityBase64).CopyTo(block, FeatPatch.Data3AbilityAt);
        BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);
        BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt);
        BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt + 2);
        for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
        {
            block[FeatPatch.AxeDamageMinAt + r + 1] = (byte)s.First[r];
            block[FeatPatch.AxeDamageMaxAt + r + 1] = (byte)s.Second[r];
        }

        return block;
    }

    /// <summary>
    /// The eighth data block for <paramref name="spells"/>, as featpatch.py's data8_block makes it: its mark, Shatter's list entry
    /// and ability record (with the counts, 1 and 1, and the record's damage the first rank's) when it is on, the names of its icon
    /// and its fallback, its damage, reach, freeze and cooldown by rank (its defaults when it is off: the template's), its HUD line's
    /// key, the ring's fixed values, then its run-time fields, zero.
    /// </summary>
    private static byte[] FeatData8Block(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        var block = (byte[])FeatPatchData.Value.Data8.Clone();
        if (!spells.TryGetValue(BorrowedSpell.Shatter, out var s)) return block;
        var data = BorrowedSpells.Data(BorrowedSpell.Shatter);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3CountAt);
        BitConverter.GetBytes(1).CopyTo(block, FeatPatch.Data3AbilityCountAt);
        Convert.FromBase64String(data.EntryBase64).CopyTo(block, FeatPatch.Data3EntryAt);
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = (byte)s.Price;
        block[FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt] = (byte)s.RankCount;
        Convert.FromBase64String(data.AbilityBase64).CopyTo(block, FeatPatch.Data3AbilityAt);
        BitConverter.GetBytes(s.Mana).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);
        BitConverter.GetBytes((ushort)s.First[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt);
        BitConverter.GetBytes((ushort)s.Second[0]).CopyTo(block, FeatPatch.Data3AbilityAt + FeatPatch.AbilityDamageAt + 2);
        for (var r = 0; r < BorrowedSpellSettings.Ranks; r++)
        {
            block[FeatPatch.ShatterDamageMinAt + r + 1] = (byte)s.First[r];
            block[FeatPatch.ShatterDamageMaxAt + r + 1] = (byte)s.Second[r];
            for (var k = 0; k < ShatterTables.Length; k++) block[ShatterTables[k] + r + 1] = (byte)s.MoreValues[k][r];
        }

        return block;
    }

    /// <summary>
    /// The cast table for <paramref name="spells"/> (its own piece of the pool), as featpatch.py's cast_table makes it: the game's
    /// 23 cases from this file, the default for the rest, and the case of each spell that is on but Sorcerer's Blood (a passive)
    /// and Inferno Arrow and Poison Gas Arrow (types 9 and 38: the game's own case, the arrows'); Entangle's is type 36, Insect
    /// Plague's 37, Dimension Door's 39, Expert Focus's 40, Phase Axe's 41, Shatter's 42.
    /// The state checks it as it checks the code: a table that isn't this one is not WorldExplorer's.
    /// </summary>
    private byte[] FeatCastTable(IEnumerable<BorrowedSpell> spells)
    {
        var table = new byte[4 * FeatPatch.CastTypes];
        for (var t = 0; t < FeatPatch.CastTypes; t++)
        {
            var word = t < FeatPatch.GameCastTypes ? ReadU32(FileOffset(FeatPatch.GameCastTableVa + 4 * t)) : FeatPatch.CastDefault;
            BitConverter.GetBytes(word).CopyTo(table, 4 * t);
        }

        var on = spells.ToHashSet();
        foreach (var spell in BorrowedSpells.All.Where(on.Contains))
        {
            var data = BorrowedSpells.Data(spell);
            if (data.HasCast) BitConverter.GetBytes(data.Cast).CopyTo(table, 4 * (data.CastType - 1));
        }

        return table;
    }

    /// <summary>
    /// Takes the borrowed spells out (On or Outdated): the game's words back at the hooks and the game's own bytes back in the
    /// whole pool (also where a test file's tripwire is). Nothing happens when it is Off; throws
    /// <see cref="InvalidOperationException"/> when Unrecognized.
    /// </summary>
    public void RemoveBorrowedSpells()
    {
        switch (GetBorrowedSpellsState(out var detail))
        {
            case BorrowedSpellsState.Off:
                return;
            case BorrowedSpellsState.Unrecognized:
                throw new InvalidOperationException("The borrowed spells can't be taken out: " + detail);
            case BorrowedSpellsState.Outdated:
                RemoveOutdated();
                return;
        }

        foreach (var (va, game, _, _) in FeatPatch.Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    private void RestorePool()
    {
        foreach (var (a, b) in FeatPatch.Pool) PoolOriginal(a, b).CopyTo(_data.AsSpan(FileOffset(a)));
    }

    /// <summary>Takes out the earlier version that is in (Outdated): version 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6 or 5, or the fourth or third test version.</summary>
    private void RemoveOutdated()
    {
        if (V16Spells() != null) RemoveV16();
        else if (V15Spells() != null) RemoveV15();
        else if (V14Spells() != null) RemoveV14();
        else if (V13Spells() != null) RemoveV13();
        else if (V12Spells() != null) RemoveV12();
        else if (V11Spells() != null) RemoveV11();
        else if (V10Spells() != null) RemoveV10();
        else if (V9Spells() != null) RemoveV9();
        else if (V8Spells() != null) RemoveV8();
        else if (V7Spells() != null) RemoveV7();
        else if (V6Spells() != null) RemoveV6();
        else if (V5Spells() != null) RemoveV5();
        else if (V3Present()) RemoveV3();
        else RemoveV4();
    }

    /// <summary>Takes version 16 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV16()
    {
        foreach (var (va, game, _, _) in FeatPatch.V16Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 15 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV15()
    {
        foreach (var (va, game, _, _) in FeatPatch.V15Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 14 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV14()
    {
        foreach (var (va, game, _, _) in FeatPatch.V14Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 13 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV13()
    {
        foreach (var (va, game, _, _) in FeatPatch.V13Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 12 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV12()
    {
        foreach (var (va, game, _, _) in FeatPatch.V12Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 11 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV11()
    {
        foreach (var (va, game, _, _) in FeatPatch.V11Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 10 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV10()
    {
        foreach (var (va, game, _, _) in FeatPatch.V10Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 9 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV9()
    {
        foreach (var (va, game, _, _) in FeatPatch.V9Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 8 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV8()
    {
        foreach (var (va, game, _, _) in FeatPatch.V8Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 7 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV7()
    {
        foreach (var (va, game, _, _) in FeatPatch.V7Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 6 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV6()
    {
        foreach (var (va, game, _, _) in FeatPatch.V6Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes version 5 out: the game's words back at its hooks, the game's bytes back in the pool.</summary>
    private void RemoveV5()
    {
        foreach (var (va, game, _, _) in FeatPatch.V5Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes the fourth test version out: the game's words back at its six hooks, the game's bytes back in the pool.</summary>
    private void RemoveV4()
    {
        foreach (var (va, game, _) in FeatPatch.V4Hooks) WriteInt(FileOffset(va), (int)game);
        RestorePool();
    }

    /// <summary>Takes the third test version out: the game's words back at its seven hooks, its spell area's bytes zero again.</summary>
    private void RemoveV3()
    {
        foreach (var (va, game, _) in FeatPatch.V3Hooks) WriteInt(FileOffset(va), (int)game);
        Array.Clear(_data, FeatPatch.V3AreaFile, FeatPatch.V3AreaSize);
    }
}
