namespace JetBlackEngineLib.Data.Scripting;

/// <summary>What a script call argument holds.</summary>
public enum ScriptValueKind
{
    /// <summary>A number pushed as an immediate value.</summary>
    Number,

    /// <summary>A string literal in the script's data segment (the pushed value is its offset).</summary>
    Text,

    /// <summary>Worked out while the script runs (a variable or register), so it has no fixed value.</summary>
    Computed
}

/// <summary>A parameter of an engine function that scripts call.</summary>
public sealed record ScriptParameter(string Name, ScriptValueKind Kind);

/// <summary>
/// The engine functions Baldur's Gate: Dark Alliance scripts call (their
/// "externals"), with the arguments each takes, in call order. Worked out
/// from the game's scripts; functions not listed here are shown read-only.
/// </summary>
public static class ScriptFunctions
{
    private const ScriptValueKind N = ScriptValueKind.Number;
    private const ScriptValueKind T = ScriptValueKind.Text;

    private static readonly Dictionary<string, ScriptParameter[]> Known = new(StringComparer.Ordinal)
    {
        ["setv"] = P(("variable", T), ("value", N)),
        ["getv"] = P(("variable", T)),
        ["addQuest"] = P(("title", T), ("description", T)),
        ["removeQuest"] = P(("title", T)),
        ["addHelpMessage"] = P(("title", T), ("message", T)),
        ["soundSequence"] = P(("archive", T), ("sequence", T)),
        ["startPropAnim"] = P(("prop", T), ("animation", T)),
        ["stopPropAnim"] = P(("prop", T)),
        ["startDialog"] = P(("speaker", T), ("dialog file", T), ("entry", N)),
        ["givePlayerItem"] = P(("item", T), ("flag", N)),
        ["givePlayerExp"] = P(("amount", N)),
        ["givePlayerGold"] = P(("amount", N)),
        ["setNoCollide"] = P(("object", T), ("value", N)),
        ["callScript"] = P(("script", T), ("value", N)),
        ["hideMonster"] = P(("slot", N)),
        ["loadMonsterSlot"] = P(("slot", N), ("monster", T), ("value", N)),
        ["setTalkTarget"] = P(("name", T), ("value 1", N), ("value 2", N), ("value 3", N), ("value 4", N),
            ("value 5", N)),
        ["moveTalkTarget"] = P(("value 1", N), ("value 2", N), ("value 3", N)),
        ["getScaledTime"] = P(("time", N)),
        ["acquireCamera"] = P(),
        ["activateStore"] = P(),
        ["checkNewState"] = P(),
        ["clearRecall"] = P(),
        ["dialogRunning"] = P(),
        ["endGame"] = P(),
        ["getRand"] = P(),
        ["getScriptState"] = P(),
        ["releaseCamera"] = P(),
        ["storeRunning"] = P(),
    };

    /// <summary>The parameters of <paramref name="name"/>, or null when they aren't known.</summary>
    public static IReadOnlyList<ScriptParameter>? ParametersOf(string name) =>
        Known.TryGetValue(name, out var parameters) ? parameters : null;

    private static ScriptParameter[] P(params (string Name, ScriptValueKind Kind)[] parameters) =>
        parameters.Select(p => new ScriptParameter(p.Name, p.Kind)).ToArray();
}
