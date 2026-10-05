# bgda-explorer
Code written to try and figure out the data files used in Baldur's Gate Dark Alliance for the PS2

This is a collection of tools used to figure out the format of the data files used in the PS2 version of Baldur's Gate Dark Alliance.

The bgtools directory contains java code to mainly dump decodes files as text.

The WorldExplorer directory contains a C# application to visualise the model and levels.

## WorldExplorer

WorldExplorer browses and edits the game data of Snowblind-engine PS2 games: Baldur's Gate: Dark Alliance,
Champions: Return to Arms, Justice League Heroes and Fallout: Brotherhood of Steel. Pick the game from the
toolbar (or Settings): it decides how files are decoded, and the Dark Alliance-only tools (executable tuning,
save editing, script rewards) only appear for Dark Alliance.

- **Open** a `.GOB`, `.LMP`, `.CLP`, `.DDF`, `.SDB`, `.YAK` or `.HDR` file (Ctrl+O, the start page's recent
  files, or drag and drop). Files load in the background.
- **Explore** the archive in the left panel. Type in the filter box (Ctrl+F) to find entries by name or
  extension, e.g. `.tex` or `tavern`. Right-click an entry for its actions (export, replace, delete…).
- **View** the selection: only the views that apply to it are offered, e.g. a `.world` opens in the Level
  editor, a `.vif` offers Model, Texture and Details, an archive shows an Overview. Switch with the buttons
  above the content or Ctrl+1…6; the app remembers which view you prefer for each kind of item.
- **Edit levels**: Ctrl+click an object or element, then drag the gizmo or type values in the properties panel
  (Enter applies). Copy, paste, duplicate, delete, undo and redo are in the Edit menu.
- **Save** with Ctrl+S. Edited items are marked with a dot in the explorer and the title bar shows `●` until
  the file is saved; you are asked before unsaved changes would be lost.
- Level textures for a GOB come from the `.TEX` file with the same name next to it (e.g. `TAVERN.TEX`).

Press F1 in the app for every keyboard shortcut. Dark and light themes are available (Ctrl+Shift+T).

### Building

Requires Windows and the .NET 10 SDK. Open `WorldExplorer/WorldExplorer.sln` in Visual Studio or Rider, or run
`dotnet build WorldExplorer/WorldExplorer.sln`.

Credits:

-bigianb (Ian Brown) - Author and progenitor of the bgda-explorer tool.

-BryceBarbara (Bryce Barbara) - Contributor to the bgda-explorer tool.

-kran27 (Kran) - Contributor to the bgda-explorer tool.

-Claude (Anthropic AI) - Contributor to the reverse-engineering of the bgda-explorer tool converting the original viewer into a World editor.