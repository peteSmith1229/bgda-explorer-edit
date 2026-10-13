using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>
/// The Script view: every call a script makes to the engine, with its
/// arguments. Changing a value writes the script straight into the archive's
/// pending edits, so the next save (Ctrl+S) puts it in the GOB, and adds a
/// step to this script's undo history.
/// </summary>
public sealed class ScriptEditorViewModel : EntryEditorViewModel
{
    private readonly ListCollectionView _callsView;

    private EditableScript? _script;
    private Dictionary<(int Address, int Index), string>? _savedValues;
    private ScriptFunctionItem? _selectedFunction;
    private string _filterText = "";

    /// <param name="changed">Called after the entry's bytes change (title, tree markers, disassembly).</param>
    public ScriptEditorViewModel(World world, LmpFile archive, string entryName, bool showRewardTools, Action changed)
        : base(world, archive, entryName, "script", changed)
    {
        ShowRewardTools = showRewardTools;
        _callsView = new ListCollectionView(Calls) { Filter = item => IsShown((ScriptCallViewModel)item) };
        ClearFilterCommand = new RelayCommand(() => FilterText = "");
        Initialize();
    }

    /// <summary>Show the Edit Script Rewards / Insert Reward buttons (they exist for the selected game).</summary>
    public bool ShowRewardTools { get; }

    /// <summary>Every call, in code order.</summary>
    public ObservableCollection<ScriptCallViewModel> Calls { get; } = new();

    /// <summary>The calls in the selected function that match the filter.</summary>
    public ICollectionView CallsView => _callsView;

    public ObservableCollection<ScriptFunctionItem> Functions { get; } = new();

    public ScriptFunctionItem? SelectedFunction
    {
        get => _selectedFunction;
        set
        {
            if (!SetProperty(ref _selectedFunction, value)) return;
            RefreshView();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (!SetProperty(ref _filterText, value ?? "")) return;
            OnPropertyChanged(nameof(IsFiltering));
            RefreshView();
        }
    }

    public bool IsFiltering => _filterText.Length > 0;

    public ICommand ClearFilterCommand { get; }

    /// <summary>"265 calls in 34 functions".</summary>
    public string Summary => _script == null
        ? ""
        : $"{Plural.Of(_script.Calls.Count, "call")} in {Plural.Of(_script.Functions.Count, "function")}";

    /// <summary>Shown when no calls are listed.</summary>
    public string EmptyText
    {
        get
        {
            if (_script == null) return "";
            var scope = _selectedFunction?.Name != null ? $" in {_selectedFunction.Title}" : "";
            if (IsFiltering) return $"No calls{scope} match \u201c{_filterText}\u201d.";
            return scope.Length > 0 ? "This function doesn't call the engine." : "This script doesn't call the engine.";
        }
    }

    public bool IsEmpty => _callsView.IsEmpty;

    /// <summary>Asks the view to bring a call into view (after undo or redo).</summary>
    public event Action<ScriptCallViewModel>? RevealRequested;

    protected override bool IsLoaded => _script != null;

    protected override IEnumerable<EditValueViewModel> Values => Calls.SelectMany(c => c.Arguments);

    // ─────────────────────────────────────────────────────────────────────────
    // Editing
    // ─────────────────────────────────────────────────────────────────────────

    internal void Commit(ScriptArgumentViewModel item)
    {
        if (_script == null || IsReadOnly || !item.IsEditable) return;
        var draft = item.Draft;
        if (draft == item.Value)
        {
            item.ClearError();
            return;
        }

        var argument = item.Argument;
        try
        {
            if (argument.Kind == ScriptValueKind.Number)
            {
                if (!NumberText.TryParseInteger(draft, out var number))
                {
                    item.SetError("Enter a whole number, such as 250, -1 or 0xFA.");
                    return;
                }

                if (number == argument.Value)
                {
                    item.Refresh(); // "0x10" for 16: same value, show it the usual way
                    return;
                }

                Apply(item, () => _script.SetNumber(argument, number));
            }
            else
            {
                var problem = EditableScript.CheckText(draft);
                if (problem != null)
                {
                    item.SetError(problem);
                    return;
                }

                Apply(item, () => _script.SetText(argument, draft));
            }

            item.Refresh();
            UpdateTextRoom();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            item.SetError(ex.Message);
        }
    }

    protected override byte[] Serialize() => _script!.ToArray();

    protected override object? FirstChange() => Calls.SelectMany(c => c.Arguments).FirstOrDefault(a => a.IsModified);

    protected override void Reveal(object? focus)
    {
        var call = focus is ScriptArgumentViewModel argument
            ? Calls.FirstOrDefault(c => c.Arguments.Contains(argument))
            : null;
        if (call != null) RevealRequested?.Invoke(call);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads <paramref name="bytes"/>, keeping the rows when the calls are the same ones.</summary>
    protected override void Load(byte[] bytes)
    {
        if (!EditableScript.TryLoad(bytes, out var script, out var error))
        {
            _script = null;
            Calls.Clear();
            Functions.Clear();
            ReadOnlyReason = $"This script can't be edited: {error} Its disassembly is in Details.";
            RefreshView();
            OnPropertyChanged(nameof(Summary));
            return;
        }

        var sameCalls = _script != null && _script.Calls.Count == script.Calls.Count &&
                        _script.Calls.Zip(script.Calls).All(p => p.First.Address == p.Second.Address &&
                                                                 p.First.Arguments.Count == p.Second.Arguments.Count);
        _script = script;
        if (sameCalls)
        {
            for (var i = 0; i < Calls.Count; i++) Calls[i].Rebind(script.Calls[i]);
        }
        else
        {
            Calls.Clear();
            foreach (var call in script.Calls)
            {
                var row = new ScriptCallViewModel(this, call);
                foreach (var argument in row.Arguments) Track(argument);
                Calls.Add(row);
            }

            BuildFunctions(script);
        }

        OnPropertyChanged(nameof(IsReadOnly));
        foreach (var argument in Calls.SelectMany(c => c.Arguments)) argument.Refresh();
        UpdateTextRoom();
        RefreshView();
        OnPropertyChanged(nameof(Summary));
    }

    private void BuildFunctions(EditableScript script)
    {
        var counts = script.Calls.GroupBy(c => c.Function).ToDictionary(g => g.Key, g => g.Count());
        Functions.Clear();
        Functions.Add(new ScriptFunctionItem(null, "All functions", script.Calls.Count));
        foreach (var name in script.Functions.Distinct())
        {
            Functions.Add(new ScriptFunctionItem(name, name, counts.GetValueOrDefault(name)));
        }

        if (counts.ContainsKey(""))
        {
            Functions.Insert(1, new ScriptFunctionItem("", ScriptCallViewModel.TopLevel, counts[""]));
        }

        _selectedFunction = Functions[0];
        OnPropertyChanged(nameof(SelectedFunction));
    }

    /// <summary>Tells text arguments how much they can grow in place.</summary>
    private void UpdateTextRoom()
    {
        if (_script == null) return;
        foreach (var argument in Calls.SelectMany(c => c.Arguments))
        {
            if (argument.IsText) argument.SetRoom(_script.InPlaceLength(argument.Argument));
        }
    }

    protected override void ReadSaved(byte[] saved)
    {
        _savedValues = null; // the saved script can't be read: nothing to compare with
        if (!EditableScript.TryLoad(saved, out var script, out _)) return;

        _savedValues = new Dictionary<(int, int), string>();
        foreach (var argument in script.Calls.SelectMany(c => c.Arguments))
        {
            if (argument.Kind != ScriptValueKind.Computed)
            {
                _savedValues[(argument.Call.Address, argument.Index)] = ScriptArgumentViewModel.Format(argument);
            }
        }
    }

    protected override int MarkChanges()
    {
        var changes = 0;
        foreach (var argument in Calls.SelectMany(c => c.Arguments))
        {
            if (argument.IsComputed || _savedValues == null)
            {
                argument.SetSaved(null);
                continue;
            }

            // A value the saved script doesn't have at all shows as changed from "(none)".
            var saved = _savedValues.GetValueOrDefault((argument.Argument.Call.Address, argument.Argument.Index), "");
            argument.SetSaved(saved != argument.Value ? saved : null);
            if (argument.IsModified) changes++;
        }

        return changes;
    }

    private bool IsShown(ScriptCallViewModel call) =>
        (_selectedFunction?.Name == null || call.Call.Function == _selectedFunction.Name) &&
        (_filterText.Length == 0 || call.Matches(_filterText));

    private void RefreshView()
    {
        _callsView.Refresh();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }
}

/// <summary>An entry in the Script view's function list.</summary>
public sealed class ScriptFunctionItem
{
    public ScriptFunctionItem(string? name, string title, int callCount)
    {
        Name = name;
        Title = title;
        CallCount = callCount;
    }

    /// <summary>The function's name; null for "All functions".</summary>
    public string? Name { get; }

    public string Title { get; }
    public int CallCount { get; }
}

/// <summary>A row of the Script view: one engine call.</summary>
public sealed class ScriptCallViewModel : ObservableObject
{
    public const string TopLevel = "(top level)";

    public ScriptCallViewModel(ScriptEditorViewModel owner, ScriptCall call)
    {
        Call = call;
        Arguments = call.Arguments.Select(a => new ScriptArgumentViewModel(owner, a)).ToList();
    }

    public ScriptCall Call { get; private set; }

    public string Name => Call.Name;
    public string Function => Call.Function.Length > 0 ? Call.Function : TopLevel;
    public string Address => $"0x{Call.Address:X4}";
    public IReadOnlyList<ScriptArgumentViewModel> Arguments { get; }
    public bool HasArguments => Arguments.Count > 0;

    /// <summary>Why the call's values can't be changed, when they can't.</summary>
    public string? Note => Call.Note;

    public string ToolTip =>
        $"{Call.Name}({string.Join(", ", Arguments.Select(a => a.Name))})\nIn {Function}, at {Address}" +
        (Call.Note != null ? "\n\n" + Call.Note : "");

    internal void Rebind(ScriptCall call)
    {
        Call = call;
        for (var i = 0; i < Arguments.Count; i++) Arguments[i].Rebind(call.Arguments[i]);
    }

    internal bool Matches(string filter) =>
        Call.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        Function.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        Arguments.Any(a => a.Value.Contains(filter, StringComparison.OrdinalIgnoreCase));
}

/// <summary>One argument of a call in the Script view, with the text being typed into it.</summary>
public sealed class ScriptArgumentViewModel : EditValueViewModel
{
    private readonly ScriptEditorViewModel _owner;
    private int _room;

    public ScriptArgumentViewModel(ScriptEditorViewModel owner, ScriptArgument argument)
    {
        _owner = owner;
        Argument = argument;
        Refresh();
    }

    public ScriptArgument Argument { get; private set; }

    public override string Name => Argument.Name;
    public override string Context => Argument.Call.Name;
    public bool IsText => Argument.Kind == ScriptValueKind.Text;
    public bool IsComputed => Argument.Kind == ScriptValueKind.Computed;
    public override bool IsEditable => Argument.IsEditable && !_owner.IsReadOnly;

    /// <summary>The value in the script, as it is typed: 750, fx.lmp, (variable).</summary>
    public override string Value => Format(Argument);

    protected override IEnumerable<string> Describe()
    {
        if (IsComputed)
        {
            yield return (Argument.Source == "computed"
                ? "Worked out when the script runs"
                : $"Read from a {Argument.Source} when the script runs") + ", so there's no fixed value to edit.";
        }
        else if (!IsEditable)
        {
            yield return Argument.Call.Note ?? "Read-only.";
        }
        else if (IsText)
        {
            yield return _room > 0
                ? $"Text. Up to {_room} characters fit in place; longer text is added to the end of the " +
                  "script's data."
                : "Text. Other code may share this text, so a change is stored as new text for this call only.";
            if (_room > 0 && Draft.Length > _room && Draft != Value)
            {
                yield return "This text is longer, so it will be added to the end of the script's data.";
            }
        }
        else
        {
            yield return "Whole number. Hexadecimal such as 0x1F works too.";
        }
    }

    public override void Commit() => _owner.Commit(this);

    internal static string Format(ScriptArgument argument) => argument.Kind switch
    {
        ScriptValueKind.Text => argument.Text ?? "",
        ScriptValueKind.Computed => $"({argument.Source})",
        _ => argument.Value.ToString(CultureInfo.InvariantCulture)
    };

    internal void Rebind(ScriptArgument argument) => Argument = argument;

    internal void SetRoom(int room)
    {
        if (_room == room) return;
        _room = room;
        RefreshToolTip();
    }
}
