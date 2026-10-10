using JetBlackEngineLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace WorldExplorer.Infrastructure;

/// <summary>
/// A game the explorer can open. <see cref="Version"/> is the engine version
/// whose decoders read its files; several games may share one, so tools and
/// settings key on <see cref="Id"/> instead.
/// </summary>
public sealed class GameOption
{
    private GameOption(string id, EngineVersion version, string name, string shortName, string? note = null)
    {
        Id = id;
        Version = version;
        Name = name;
        ShortName = shortName;
        Note = note;
    }

    /// <summary>Stable identifier, stored in the settings as "Core.Game".</summary>
    public string Id { get; }

    /// <summary>The decoders used for this game's files.</summary>
    public EngineVersion Version { get; }

    public string Name { get; }
    public string ShortName { get; }

    /// <summary>Extra information shown where the game is chosen, if any.</summary>
    public string? Note { get; }

    public override string ToString() => Name;

    public static GameOption DarkAlliance { get; } =
        new("DarkAlliance", EngineVersion.DarkAlliance, "Baldur's Gate: Dark Alliance", "Dark Alliance");

    public static GameOption DarkAlliance2 { get; } =
        new("DarkAlliance2", EngineVersion.DarkAlliance, "Baldur's Gate: Dark Alliance II", "Dark Alliance II",
            "Files are read with the Dark Alliance formats for now.");

    public static IReadOnlyList<GameOption> All { get; } = new[]
    {
        DarkAlliance,
        DarkAlliance2,
        new GameOption("ReturnToArms", EngineVersion.ReturnToArms, "Champions: Return to Arms", "Return to Arms"),
        new GameOption("JusticeLeagueHeroes", EngineVersion.JusticeLeagueHeroes, "Justice League Heroes", "Justice League Heroes"),
        new GameOption("BrotherhoodOfSteel", EngineVersion.BrotherhoodOfSteel, "Fallout: Brotherhood of Steel", "Brotherhood of Steel"),
    };

    /// <summary>The first game decoded with <paramref name="version"/>.</summary>
    public static GameOption For(EngineVersion version)
    {
        foreach (var game in All)
        {
            if (game.Version == version) return game;
        }
        return All[0];
    }

    /// <summary>
    /// The game saved in the settings. Settings written before games had their
    /// own identity only hold the engine version, so fall back to that.
    /// </summary>
    public static GameOption FromSettings()
    {
        var id = App.Settings.Get("Core.Game", "", addIfMissing: false);
        foreach (var game in All)
        {
            if (string.Equals(game.Id, id, StringComparison.OrdinalIgnoreCase)) return game;
        }
        return For(App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance));
    }

    /// <summary>Stores the game; the engine version is kept too for code that reads it directly.</summary>
    public static void SaveToSettings(GameOption game)
    {
        App.Settings["Core.Game"] = game.Id;
        App.Settings["Core.EngineVersion"] = game.Version;
    }
}

/// <summary>An entry in the recent-files list on the start page.</summary>
public sealed class RecentFile
{
    public RecentFile(string path)
    {
        Path = path;
        FileName = System.IO.Path.GetFileName(path);
        Folder = System.IO.Path.GetDirectoryName(path) ?? "";
        Exists = File.Exists(path);
    }

    public string Path { get; }
    public string FileName { get; }
    public string Folder { get; }
    public bool Exists { get; }
}
