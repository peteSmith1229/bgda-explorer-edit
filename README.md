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
- **Edit scripts**: select a level script (`script.scr`) to open the Script view, which lists every call the
  script makes to the engine (quests, dialogs, rewards, variables, monsters…) by function, with its values.
  Change a number or a piece of text and press Enter; Ctrl+Z undoes it, and Ctrl+S saves it into the GOB with
  your other changes. Text longer than the original is added to the end of the script's data, and the code
  itself never moves, so jumps and labels stay valid. The disassembly is still in Details.
- **Tools** for the selected game are in the Tools menu, the Tools drop-down on the toolbar and the start page.
  For Dark Alliance: Executable Tuning (XP per level, starting stats, feats and spells, monster HP, three-player
  co-op and difficulty scaling, saved to a copy of `SLES_506.72`), the Save Game Editor (memory-card exports,
  `.psu`), and Edit Script Rewards and Insert Reward at Call Site, which work on the selected script. Games
  without tools yet say so.
- **Save** with Ctrl+S. Edited items are marked with a dot in the explorer and the title bar shows `●` until
  the file is saved; you are asked before unsaved changes would be lost.
- Level textures for a GOB come from the `.TEX` file with the same name next to it (e.g. `TAVERN.TEX`). They
  are drawn opaque by default; turn off Force opaque in Settings to see their transparency.

Press F1 in the app for every keyboard shortcut. Dark and light themes are available (Ctrl+Shift+T).

## Building

Requires Windows and the .NET 10 SDK. Open `WorldExplorer/WorldExplorer.sln` in Visual Studio or Rider, or run
`dotnet build WorldExplorer/WorldExplorer.sln`.

`dotnet test WorldExplorer/WorldExplorer.sln` runs the library tests. The executable and script tests use the
game files in `Test_files` (`SLES_506.72`, `TAVERN.GOB`) and are skipped if those are missing.

## Credits:

-bigianb (Ian Brown) - Author and progenitor of the bgda-explorer tool.

-BryceBarbara (Bryce Barbara) - Contributor to the bgda-explorer tool.

-kran27 (Kran) - Contributor to the bgda-explorer tool.

-Claude (Anthropic AI) - Contributor to the reverse-engineering of the bgda-explorer tool converting the original viewer into a World editor.

-peteSmith1229 - Instigator.
