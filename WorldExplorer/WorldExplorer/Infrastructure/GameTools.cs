using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldExplorer.Infrastructure;

/// <summary>A game-specific tool (Tools menu, toolbar drop-down, start page).</summary>
public sealed class GameTool
{
    public GameTool(string id, string name, string description, string iconKey, bool needsScript,
        params GameOption[] games)
    {
        Id = id;
        Name = name;
        Description = description;
        IconKey = iconKey;
        NeedsScript = needsScript;
        Games = games;
    }

    /// <summary>Passed as the parameter of <see cref="AppCommands.RunTool"/>.</summary>
    public string Id { get; }

    public string Name { get; }
    public string Description { get; }
    public string IconKey { get; }

    /// <summary>The tool works on the script (.scr) selected in the explorer.</summary>
    public bool NeedsScript { get; }

    public IReadOnlyList<GameOption> Games { get; }

    /// <summary>The name as a menu header, with the ellipsis of a command that asks for more.</summary>
    public string MenuHeader => Name + "…";
}

/// <summary>Which tools exist for which game.</summary>
public static class GameTools
{
    public const string ExecutableTuning = "ExecutableTuning";
    public const string SaveGameEditor = "SaveGameEditor";
    public const string ScriptRewards = "ScriptRewards";
    public const string InsertReward = "InsertReward";

    // Reverse-engineered against Baldur's Gate: Dark Alliance (PAL SLES-506.72) only.
    public static IReadOnlyList<GameTool> All { get; } = new[]
    {
        new GameTool(ExecutableTuning, "Executable Tuning",
            "XP per level, starting stats, feats and spells, monster HP, three players and difficulty, " +
            "written to a copy of the game executable (SLES_506.72).",
            "Icon.Tools", needsScript: false, GameOption.DarkAlliance),
        new GameTool(SaveGameEditor, "Save Game Editor",
            "Difficulty, character stats and enemy HP in a memory-card save export (.psu).",
            "Icon.Save", needsScript: false, GameOption.DarkAlliance),
        new GameTool(ScriptRewards, "Edit Script Rewards",
            "Change the gold, XP and item rewards in a level script. Works on the .scr selected in the explorer.",
            "Icon.Kind.Script", needsScript: true, GameOption.DarkAlliance),
        new GameTool(InsertReward, "Insert Reward at Call Site",
            "Add gold, XP or item rewards just before a call in a level script. Works on the .scr selected " +
            "in the explorer.",
            "Icon.Add", needsScript: true, GameOption.DarkAlliance),
    };

    public static IReadOnlyList<GameTool> For(GameOption game) =>
        All.Where(tool => tool.Games.Contains(game)).ToList();

    public static bool IsAvailable(string toolId, GameOption game) =>
        All.Any(tool => tool.Id == toolId && tool.Games.Contains(game));

    public static GameTool? Find(string? toolId) =>
        All.FirstOrDefault(tool => string.Equals(tool.Id, toolId, StringComparison.Ordinal));
}
