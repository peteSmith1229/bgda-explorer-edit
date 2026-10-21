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
  game looks entries up by name. An archive whose entries are out of order (such as `shop.lmp` in
  `TAVERN.GOB`) keeps its order and gets the new entry at the end. The game never looks for an archive inside
  another archive, so adding a `.lmp` file to an archive in a GOB offers to add it to the GOB instead.
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
  three-player co-op and difficulty scaling, saved to a copy of `SLES_506.72`), the Save Game Editor (memory-card
  exports, `.psu`), and Edit Script Rewards and Insert Reward at Call Site, which work on the selected script. Games
  without tools yet say so.
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
`CUTTOWN.GOB`, `GTEXT.LMP`, `TMAN2.LMP`) and are skipped if those are missing.

## Credits:

-bigianb (Ian Brown) - Author and progenitor of the bgda-explorer tool.

-BryceBarbara (Bryce Barbara) - Contributor to the bgda-explorer tool.

-kran27 (Kran) - Contributor to the bgda-explorer tool.

-Claude (Anthropic AI) - Contributor to the reverse-engineering of the bgda-explorer tool converting the original viewer into a World editor.

-peteSmith1229 - Instigator.
