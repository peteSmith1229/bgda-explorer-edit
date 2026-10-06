using JetBlackEngineLib;
using System.Collections.Generic;
using System.IO;

namespace WorldExplorer.Infrastructure;

/// <summary>A Snowblind-engine game the explorer can decode (the "engine version").</summary>
public sealed class GameOption
{
    private GameOption(EngineVersion version, string name, string shortName)
    {
        Version = version;
        Name = name;
        ShortName = shortName;
    }

    public EngineVersion Version { get; }
    public string Name { get; }
    public string ShortName { get; }

    public override string ToString() => Name;

    public static IReadOnlyList<GameOption> All { get; } = new[]
    {
        new GameOption(EngineVersion.DarkAlliance, "Baldur's Gate: Dark Alliance", "Dark Alliance"),
        new GameOption(EngineVersion.ReturnToArms, "Champions: Return to Arms", "Return to Arms"),
        new GameOption(EngineVersion.JusticeLeagueHeroes, "Justice League Heroes", "Justice League Heroes"),
        new GameOption(EngineVersion.BrotherhoodOfSteel, "Fallout: Brotherhood of Steel", "Brotherhood of Steel"),
    };

    public static GameOption For(EngineVersion version)
    {
        foreach (var game in All)
        {
            if (game.Version == version) return game;
        }
        return All[0];
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
