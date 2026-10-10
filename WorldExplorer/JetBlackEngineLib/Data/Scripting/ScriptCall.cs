namespace JetBlackEngineLib.Data.Scripting;

/// <summary>A call from a script to an engine function (an "external"), with its arguments.</summary>
public sealed class ScriptCall
{
    internal ScriptCall(string function, string name, int address)
    {
        Function = function;
        Name = name;
        Address = address;
    }

    /// <summary>The script function (internal label) the call is in; empty before the first one.</summary>
    public string Function { get; }

    /// <summary>The engine function called.</summary>
    public string Name { get; }

    /// <summary>Code address of the call instruction.</summary>
    public int Address { get; }

    /// <summary>
    /// True when the arguments were matched to the function's known parameters,
    /// which is what allows them to be edited.
    /// </summary>
    public bool IsKnown { get; internal set; }

    /// <summary>Why the call's arguments can't be edited, when they can't.</summary>
    public string? Note { get; internal set; }

    public IReadOnlyList<ScriptArgument> Arguments { get; internal set; } = Array.Empty<ScriptArgument>();

    public override string ToString() =>
        $"{Name}({string.Join(", ", Arguments.Select(a => a.Display))})";
}

/// <summary>One argument of a <see cref="ScriptCall"/>.</summary>
public sealed class ScriptArgument
{
    internal ScriptArgument(EditableScript owner, ScriptCall call, int index, string name, ScriptValueKind kind,
        int immediateOffset, int value, string? text, string? source)
    {
        Owner = owner;
        Call = call;
        Index = index;
        Name = name;
        Kind = kind;
        ImmediateOffset = immediateOffset;
        Value = value;
        Text = text;
        Source = source;
    }

    internal EditableScript Owner { get; }

    /// <summary>File offset of the pushed 4-byte value; -1 for computed arguments.</summary>
    internal int ImmediateOffset { get; }

    public ScriptCall Call { get; }

    /// <summary>Position in the call, from 0.</summary>
    public int Index { get; }

    /// <summary>Parameter name, e.g. "amount".</summary>
    public string Name { get; }

    public ScriptValueKind Kind { get; }

    /// <summary>The pushed value: the number itself, or the text's offset in the data segment.</summary>
    public int Value { get; internal set; }

    /// <summary>The text of a <see cref="ScriptValueKind.Text"/> argument.</summary>
    public string? Text { get; internal set; }

    /// <summary>Where a <see cref="ScriptValueKind.Computed"/> argument's value comes from.</summary>
    public string? Source { get; }

    public bool IsEditable => Call.IsKnown && Kind != ScriptValueKind.Computed;

    /// <summary>The value as the call would be written: 750, "fx.lmp" or (variable).</summary>
    public string Display => Kind switch
    {
        ScriptValueKind.Text => $"\"{Text}\"",
        ScriptValueKind.Computed => $"({Source})",
        _ => Value.ToString()
    };

    public override string ToString() => $"{Name} = {Display}";
}
