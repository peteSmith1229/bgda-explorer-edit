# WorldExplorer

WorldExplorer browses and edits the game data of Snowblind-engine PS2 games: Baldur's Gate: Dark Alliance and
Dark Alliance II, Champions: Return to Arms, Justice League Heroes and Fallout: Brotherhood of Steel. Pick the
game from the toolbar (or Settings): it decides how files are decoded and which tools are offered. Dark
Alliance II files are read with the Dark Alliance formats for now.

- **Open** a `.GOB`, `.LMP`, `.CLP`, `.DDF`, `.SDB`, `.YAK` or `.HDR` file (Ctrl+O, the start page's recent
  files, or drag and drop). Files load in the background.
- **Explore** the archive in the left panel. Type in the filter box (Ctrl+F) to find entries by name or
  extension, e.g. `.tex` or `tavern`. Right-click an entry for its actions (export, replace, delete…).
- **View** the selection: only the views that apply to it are offered, e.g. a `.world` opens in the Level
  editor, a `.vif` offers Model, Texture and Details, an archive shows an Overview. Switch with the buttons
  above the content or Ctrl+1…6; the app remembers which view you prefer for each kind of item.
- **Edit levels**: Ctrl+click an object or element, then drag the gizmo or type values in the properties panel
  (Enter applies). Copy, paste, duplicate, delete, undo and redo are in the Edit menu.
- **Add archives to a GOB**: right-click the GOB and choose Add Archive (.lmp)…, or use File › Import › Add
  Archive (.LMP) to GOB…, then pick one or more `.lmp` files, e.g. `TMAN2.LMP` into `TAVERN.GOB`. A Dark
  Alliance level loads every archive in its GOB when it starts, so their models, textures and animations can
  be used by the level's objects. New archives go before `env.lmp`, like the game's own. A GOB holds at most
  51 archives. The GOB's Overview shows how much memory is left once they are loaded. Until you save, you can
  right-click an added archive and choose Remove Added Archive.
- **Add entries to an archive**: right-click the archive and choose Add Entry…, or use File › Import › Add New
  Entry to Archive…. The entry appears in the explorer straight away. It is saved in name order, because the
  game looks entries up by name: the order the game uses, in which a region's pictures (`ntsc_back.tex`,
  `pal_back.tex` in `shop.lmp`, the columns in `INVENTRY.LMP`) sort without their `ntsc_` or `pal_`. An archive
  whose entries are out of that order keeps its order and gets the new entry at the end. The game never looks
  for an archive inside another archive, so adding a `.lmp` file to an archive in a GOB offers to add it to the
  GOB instead.
- **Add objects to a level** (Dark Alliance): in the Level view, click + on its toolbar or right-click and
  choose Add Object…. You can also right-click an archive in the GOB and choose Place in Level…. There are two
  kinds of object:
  - **A character or object** is one of the classes the game creates: NPCs, monsters, chests and so on. The
    list shows each class with the archive its model comes from, and whether that archive is in the GOB,
    added to it, loaded from the disc or not found.
  - **A model from an archive** shows any model in one of the GOB's archives with a texture and an animation
    that loops, e.g. `barpatron.vif`, `bp_costume3.tex` and `bp_drinking1.anm` from `tman2.lmp`. This is a
    `UserParamProp` object. It can't talk, walk or fight; that needs a class (e.g. `TownMale1`, whose model
    is in `tman1.lmp`).

  Set the position, the facing (one of 16 directions) and any `name=value` properties, e.g.
  `callName=Patron1`. Undo works as for other edits, and Ctrl+S saves the object into the level's `objects.ob`.
  Each new object takes a slot in one of the level's object pools: a chest takes a small slot, and a character
  or a `UserParamProp` takes a monster slot. The level budget shows how many are left.
- **Edit scripts**: select a level script (`script.scr`) to open the Script view, which lists every call the
  script makes to the engine (quests, dialogs, rewards, variables, monsters…) by function, with its values.
  Change a number or a piece of text and press Enter; Ctrl+Z undoes it, and Ctrl+S saves it into the GOB with
  your other changes. Text longer than the original is added to the end of the script's data, and the code
  itself never moves, so jumps and labels stay valid. The disassembly is still in Details.
- **Edit cutscenes**: select a cutscene (`.cut`, e.g. `intro.cut` in `CUTTOWN.GOB`, which plays when a New
  Game starts) to open the Cutscene view. Settings has its subtitles, narration, music and position; Cast has
  each character's name, starting position and facing; the keyframes, listed for the camera, the scene and
  each character, say what happens and when. Change a time or a value and press Enter, or copy or delete a
  keyframe: keyframes stay in time order, and undo and save work as for scripts. Actions whose meaning isn't
  known yet show their stored numbers and can still be moved, copied or deleted. "Play animation" keyframes
  have two lists: the animation, from the archive of the character that plays it (found in the GOB and the
  `.lmp` files in its folder; a player's list has what all three characters share first), and the play mode
  (play once, hold, loop, gesture…). Cast members can be copied or removed too: keyframes refer to them by
  number, so a copy (with copies of its keyframes, 48 units to one side, the next copy 48 to the other) goes
  at the end of the cast, and removing one renumbers the keyframes of those after it. A copy of `Player` is
  `Player2`, then `Player3`; a copy of anyone else keeps the name, and the name's list picks another
  character. `Player` and `Player2` stand in for players 1 and 2; with the ADRIANNA23 executable, `Player3`
  does for player 3 in three-player games.
- **Tools** for the selected game are in the Tools menu, the Tools drop-down on the toolbar and the start page.
  For Dark Alliance: Executable Tuning (XP per level, starting stats, feats and spells, items, monster HP,
  three-player co-op and difficulty scaling, enhancements, saved to a copy of `SLES_506.72`), the Save Game Editor (memory-card
  exports, `.psu`), and Edit Script Rewards and Insert Reward at Call Site, which work on the selected script. For
  Dark Alliance II: Executable Tuning, which makes the PCSX2 cheat file of the three-player mod, Build
  Three-Player ISO, and the same two script tools (Dark Alliance II's level scripts have Dark Alliance's format and
  reward commands). Games without tools yet say so.
- **Edit items** in Executable Tuning's Items tab: every weapon, armour piece, potion, ring and amulet (damage,
  armour, price, weight, slot, stance, models and the 64 property bits, with the game's own damage and price
  formulas previewed), the item models, what each shop sells, material multipliers, the enchant glow and the
  random-loot lists. The eight spell-scroll records the game never uses can become new items, and on a
  three-player executable Add item puts about 90 more after the game's 81 (the three-player patch frees the room
  after the item table). The names the game shows come from `GTEXT.LMP` (found next to the executable or in the
  data folder of Settings) and are saved to it after the executable.
- **Give items their own enchant glow** on the Items tab's Glow tab. Glow styles replace the game's glow
  function: 16 styles (a halo and a core colour, each with a strength), up to 16 rules that give items with
  some modifiers a style or no glow (Frost icy blue, Keen silver, of Defence none …), and a setting for each item
  (automatic, its own style, or never), also for a whole group at once (every axe, every helmet …). The set tried
  in the game is ready-made, and "Back to the game's own glow" takes them out again. With nothing changed, glow
  styles glow exactly as the game does; without them the tab edits the game's own glow colours.
- **Make Corrosive weapons** on the Items tab's Corrosive tab: two weapon modifiers with acid damage, on
  property bits the game leaves unused. Corrosive (item text `+acid`) adds acid to every hit, as Flaming adds
  fire; Corrosive Burst (`+acidburst`) also does more acid, a splash and poison on a critical hit, as Flaming
  Burst sets the target alight. Tick them on an item, sell them in a shop or put them in a random-loot list, and
  the Corrosive patch goes in the executable when it is saved (its dice and poison can be set); their names
  (in all five languages) go in `GTEXT.LMP` and the item card's acid drop in `INVENTRY.LMP`, each saved after the
  executable. "Take Corrosive out" takes all three out again, and the acid glow is one click away.
- **Add enhancements** on Executable Tuning's Enhancements tab: additions to the game that work with or without
  three players, each turned on or off by itself. Four spells borrowed from the game's monsters and one feat the
  game has but never offers, each a feat on its hero's level-up screen (5 ranks; price, mana and the values of
  each rank can be set):
  - Xantam's Fire for Adrianna: the beholder Xantam's fire trails, which burn the monsters in their path;
  - Power Attack for Kromlech: the game's own cut feat, finished: casting it readies his weapon, and his next
    melee hits (one to five, by rank) within 10 seconds do double damage. Its icon, HUD line and texts are the
    game's, and its mana is its record's on the Feats & Spells tab;
  - Blade Barrier for Kromlech: Ilivarra's whirling axes, which hit every monster in reach once a second;
  - Antimagic Field for Kromlech: fire, cold, electricity and magic do him no harm while it lasts;
  - Righteous Might for Kromlech: he grows, his melee hits do more damage and physical hits on him less.

  They go into game code nothing ever runs (about 32 KB of it, the same in the stock executable and every
  three-player build, so three players can still be turned on or off), the borrowed spells' names and
  descriptions (in all five languages) into `GTEXT.LMP` and their HUD icons into `ICONS.LMP`, each saved after
  the executable. Unticking one puts the game's own code back exactly. A file with the first test version of
  Kromlech's spells is brought up to date when it is saved, every spell keeping its settings.
- **Pick colours** by clicking a colour box: a glow style's halo or core on the Glow tab, or a player's automap
  marker on the Three Players tab. Drag in the square and the hue bar, type red, green and blue or a `#RRGGBB`
  value, or click a ready-made colour; OK puts the colour in the red, green and blue boxes.
- **Tune Dark Alliance II and its three-player mod** with Dark Alliance II's Executable Tuning: open
  `SLUS_206.75` (USA v1.03, PCSX2 CRC B0859096) or the game's disc image. It has Dark Alliance's tabs, in the same
  order and layout, and two more. It saves the PCSX2 cheat file `SLUS-20675_B0859096.pnach` (in PCSX2's cheats
  folder, e.g. `Documents\PCSX2\cheats`). PCSX2 finds a cheat file by the executable's CRC, so the executable is
  never changed: the edits to the game's tables become one more group of the cheat file, `[WorldExplorer -
  executable tuning]`, written once when the game boots. The cheat file of the last session (or the one in PCSX2's
  cheats folder) is opened with it; saving over a file keeps a `.bak` copy of it as it was, and groups that aren't
  the mod's are kept as they are. Tick the groups in PCSX2's cheat list.
  - **XP per Level**: the XP table, how a kill's XP is shared out (exponent, share, each player's part, and the
    own rules of Lyran, trolls and owlbears) with a worked example for one to three players, and the XP bonuses
    (Vahn and Adrianna, Drizzt and Artemis, the equipment's XP bonus).
  - **Starting Stats**: the ability scores of the ten classes, the DA1 heroes' rows included.
  - **Feats & Spells**: the ability templates (energy, damage, colours), the class feat tables (ranks, base cost
    and the DA1 spells' price tables), and the feat prices of the mod's feat group.
  - **Items**: the base items (weight, value, armour); the mod's shops group (the random stock of every visit);
    and **Loot**: every creature's drop values (gold scale, pile chance, quality, drop chance — editable where the
    game's code holds the value on its own, with the item and potion chance per kill), how a drop is rolled, the
    quality table, the gold and item value of a level by act (with an example for any level and difficulty),
    potions and Borador's Ransack.
  - **Monster HP**: each creature's HP, armour, damage, attack and XP multipliers with the HP they give on every
    difficulty in a chosen level (one player, or two and three), its damage and XP rating, and the fixed HP of the
    bosses. Most of these values keep only the upper half of a float in the code: the boxes show the value as
    written when you save.
  - **Three Players**: the mod's groups — `[3 Player mod]` itself, the HUD sizes (any from 25 to 100 %), the
    `[P3 character - …]` groups, the DA1 heroes' shouts in sound memory, the test helpers (feat points, gold, the
    Trade Way goblins with player spells) — the two-player factors three-player games use (the mod keeps the
    game's player count at 2: monster HP, damage, XP rating and boss HP, and the loot factors) and the difficulty
    scaling. With the default settings the file is build 51 of the mod byte for byte; a file of an older build (16,
    17 or 20 to 50) is recognised, its settings are read, and it is saved as build 51.
  - **Creatures**: creatures placed in a level when it first loads, one group each: build 51's DA1 bosses in
    Trollbark Forest, or others (up to 24: level, creature type, position, facing and placement properties such as
    `hpscale=2`), with the creature slots the level then uses at load. Any set other than build 51's bosses moves
    the spawn table; that layout has been checked in an emulator, not yet in the game.
  - **Levels**: the monster level of every level, and what each level uses when it loads in the stock game: its
    creatures, the 256 creature-sized object slots and the room left for more creatures, the 1,280 small objects,
    the free memory with the heaviest party and the lumps loaded (read from the game's level files).
- **Build the [3P] disc image** of Dark Alliance II with Build Three-Player ISO: it copies the original image (or
  updates the copy made before) and adds to its `BG\0MOD` folder the multitap driver `MTAPMAN.IRX` from Champions of
  Norrath and the Dark Alliance 1 files of the DA1 heroes and bosses (`HUMAN.LMP`, `DWARF.LMP`, `MOUTH.VAT` as
  `DA1MOUTH.VAT`, `LILVARRA.LMP`, `MINOTAUR.LMP`). The files are found in folders of extracted disc files or picked
  by hand; the original image is only read.
- **Save** with Ctrl+S. Edited items are marked with a dot in the explorer and the title bar shows `●` until
  the file is saved; you are asked before unsaved changes would be lost.
- Level textures for a GOB come from the `.TEX` file with the same name next to it (e.g. `TAVERN.TEX`). They
  are drawn opaque by default; turn off Force opaque in Settings to see their transparency.
- Entries without a decoder, or that fail to decode, are shown in Details as a hex dump of their first 256
  bytes. Change Hex dump length in Settings (16 to 1,048,576 bytes) to see more or less of them.

Press F1 in the app for every keyboard shortcut. Dark and light themes are available (Ctrl+Shift+T).

## Building

Requires Windows and the .NET 10 SDK. Open `WorldExplorer/WorldExplorer.sln` in Visual Studio or Rider, or run
`dotnet build WorldExplorer/WorldExplorer.sln`.

`dotnet test WorldExplorer/WorldExplorer.sln` runs the library tests. The executable, item, script, cutscene,
archive and text tests use the game files in `Test_files` (`SLES_506.72`, `TAVERN.GOB`, `TEST.GOB`,
`CUTTOWN.GOB`, `GTEXT.LMP`, `TMAN2.LMP`) and are skipped if those are missing. The Dark Alliance II tests use
`Test_files/SLUS_206.75` (USA v1.03), `Test_files/Da2Builds` (older builds of the mod's cheat file,
`da2_3p_build16.pnach` …) and `Test_files/Da2Data` (level files of the game, `BG/DATA/*.gob`); they aren't
included, and those tests are skipped without them.

## Credits:

- [bigianb](https://github.com/bigianb) (Ian Brown) - Author and progenitor of the bgda-explorer tool.

- [Claude](https://www.claude.ai) (Anthropic AI) - Contributor to the reverse-engineering of the bgda-explorer tool converting the original viewer into a World editor.

- [hughmcg786-spec](https://github.com/hughmcg786-spec) (hughmcg786) - Instigator for Baldur's Gate Dark Alliance 2.

- [adyboi786](https://github.com/adyboi786) (adyboi786) - Reviewer and tester of the new WorldExplorer tool.

- [peteSmith1229](https://github.com/peteSmith1229) (Pete Smith) - Instigator for Baldur's Gate Dark Alliance 1.
