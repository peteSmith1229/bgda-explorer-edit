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
public sealed class ScriptEditorViewModel : ObservableObject
{
    private static readonly object OpenedBytes = new();

    private readonly World _world;
    private readonly Action _changed;
    private readonly bool _deleted;
    private readonly Stack<Step> _undo = new();
    private readonly Stack<Step> _redo = new();
    private readonly ListCollectionView _callsView;

    private EditableScript? _script;
    private byte[]? _pending;
    private object? _savedKey;
    private Dictionary<(int Address, int Index), string>? _savedValues;
    private ScriptFunctionItem? _selectedFunction;
    private string _filterText = "";
    private string? _readOnlyReason;
    private int _changeCount;
    private ScriptArgumentViewModel? _errorArgument;

    /// <summary>One edit: the entry's pending bytes before and after it (null: no pending edit).</summary>
    private sealed record Step(byte[]? Before, byte[]? After, ScriptArgumentViewModel? Argument);

    /// <param name="changed">Called after the entry's bytes change (title, tree markers, disassembly).</param>
    public ScriptEditorViewModel(World world, LmpFile archive, string entryName, bool showRewardTools, Action changed)
    {
        _world = world;
        _changed = changed;
        Archive = archive;
        EntryName = entryName;
        ShowRewardTools = showRewardTools;
        _deleted = archive.PendingDeletions.Contains(entryName);
        _pending = PendingBytes();
        _callsView = new ListCollectionView(Calls) { Filter = item => IsShown((ScriptCallViewModel)item) };

        RevertCommand = new RelayCommand(RevertToSaved, () => !IsReadOnly && _world.IsEntryUnsaved(archive, entryName));
        ClearFilterCommand = new RelayCommand(() => FilterText = "");

        if (archive is ClpFile)
        {
            _readOnlyReason = "Scripts in CLP archives can't be edited.";
        }
        else if (_deleted)
        {
            _readOnlyReason = "This script is marked for deletion. Restore it from the explorer to edit it.";
        }

        Load(CurrentBytes());
    }

    public LmpFile Archive { get; }
    public string EntryName { get; }

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

    public ICommand RevertCommand { get; }
    public ICommand ClearFilterCommand { get; }

    /// <summary>Why the script can't be changed, or null when it can.</summary>
    public string? ReadOnlyReason
    {
        get => _readOnlyReason;
        private set
        {
            if (!SetProperty(ref _readOnlyReason, value)) return;
            OnPropertyChanged(nameof(IsReadOnly));
        }
    }

    public bool IsReadOnly => _readOnlyReason != null || _script == null;

    /// <summary>"265 calls in 34 functions".</summary>
    public string Summary => _script == null
        ? ""
        : $"{Plural.Of(_script.Calls.Count, "call")} in {Plural.Of(_script.Functions.Count, "function")}";

    /// <summary>Number of values that differ from the saved file.</summary>
    public int ChangeCount
    {
        get => _changeCount;
        private set
        {
            if (!SetProperty(ref _changeCount, value)) return;
            OnPropertyChanged(nameof(HasChanges));
            OnPropertyChanged(nameof(ChangeSummary));
        }
    }

    public bool HasChanges => _changeCount > 0;

    public string ChangeSummary => _changeCount == 0 ? "" : Plural.Of(_changeCount, "unsaved change");

    /// <summary>Shown when no calls are listed.</summary>
    public string EmptyText
    {
        get
        {
            if (_script == null) return "";
            var scope = _selectedFunction?.Name != null ? $" in {_selectedFunction.Title}" : "";
            if (IsFiltering) return $"No calls{scope} match “{_filterText}”.";
            return scope.Length > 0 ? "This function doesn't call the engine." : "This script doesn't call the engine.";
        }
    }

    /// <summary>Why the last value typed wasn't applied, while it is still in its box.</summary>
    public string? ErrorText => _errorArgument == null
        ? null
        : $"{_errorArgument.Argument.Call.Name} {_errorArgument.Name}: {_errorArgument.Error}";

    public bool HasError => _errorArgument != null;

    public bool IsEmpty => _callsView.IsEmpty;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Asks the view to bring a call into view (after undo or redo).</summary>
    public event Action<ScriptCallViewModel>? RevealRequested;

    /// <summary>
    /// True while the entry still holds the bytes this editor last wrote or
    /// read; otherwise something else changed it and the editor is out of date.
    /// </summary>
    public bool IsCurrent =>
        ReferenceEquals(PendingBytes(), _pending) && Archive.PendingDeletions.Contains(EntryName) == _deleted;

    /// <summary>The script's current bytes.</summary>
    public byte[] CurrentBytes() => PendingBytes() ?? OriginalBytes();

    // ─────────────────────────────────────────────────────────────────────────
    // Editing
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Applies values that were typed but not yet confirmed (before saving or closing).</summary>
    public void CommitAll()
    {
        foreach (var argument in Calls.SelectMany(c => c.Arguments))
        {
            if (argument.IsEditable && argument.Draft != argument.Value) Commit(argument);
        }
    }

    /// <summary>The footer shows the error of the argument that failed last, until it is fixed.</summary>
    internal void OnErrorChanged(ScriptArgumentViewModel item)
    {
        if (item.HasError) _errorArgument = item;
        else if (ReferenceEquals(_errorArgument, item)) _errorArgument = null;
        else return;

        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(HasError));
    }

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
                if (!TryParseNumber(draft, out var number))
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
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            item.SetError(ex.Message);
        }
    }

    private void Apply(ScriptArgumentViewModel item, Action edit)
    {
        var before = PendingBytes();
        edit();
        var after = _script!.ToArray();
        Archive.ReplaceEntry(EntryName, after);
        _pending = after;
        _undo.Push(new Step(before, after, item));
        _redo.Clear();

        item.Refresh();
        UpdateTextRoom();
        RefreshSaved();
        NotifyHistory();
        _changed();
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        var step = _undo.Pop();
        _redo.Push(step);
        Restore(step.Before, step.Argument);
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        var step = _redo.Pop();
        _undo.Push(step);
        Restore(step.After, step.Argument);
    }

    private void Restore(byte[]? bytes, ScriptArgumentViewModel? argument)
    {
        if (bytes == null)
        {
            Archive.PendingEdits.Remove(EntryName);
        }
        else
        {
            Archive.ReplaceEntry(EntryName, bytes);
        }

        _pending = bytes;
        Load(bytes ?? OriginalBytes());
        NotifyHistory();
        _changed();

        var call = argument == null ? null : Calls.FirstOrDefault(c => c.Arguments.Contains(argument));
        if (call != null) RevealRequested?.Invoke(call);
    }

    /// <summary>Puts back the saved script (an undoable step, like any other edit).</summary>
    private void RevertToSaved()
    {
        if (_script == null || IsReadOnly) return;
        var saved = _world.TryGetSavedEdit(Archive, EntryName, out var savedEdit) ? savedEdit : null;
        var before = PendingBytes();
        if (ReferenceEquals(before, saved)) return;

        // Putting back the saved array itself (or no pending edit) leaves nothing unsaved.
        var changed = Calls.SelectMany(c => c.Arguments).FirstOrDefault(a => a.IsModified);
        _undo.Push(new Step(before, saved, changed));
        _redo.Clear();
        Restore(saved, changed);
    }

    private static bool TryParseNumber(string text, out int value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
        {
            var negative = text[0] == '-';
            if (uint.TryParse(text[(negative ? 3 : 2)..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                    out var hex) && (!negative || hex <= 0x80000000u))
            {
                value = negative ? (int)-(long)hex : unchecked((int)hex);
                return true;
            }

            value = 0;
            return false;
        }

        return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────────────────────────────────

    private byte[]? PendingBytes() => Archive.PendingEdits.TryGetValue(EntryName, out var bytes) ? bytes : null;

    private byte[] OriginalBytes()
    {
        var entry = Archive.Directory[EntryName];
        return Archive.FileData.AsSpan(entry.StartOffset, entry.Length).ToArray();
    }

    /// <summary>Reads <paramref name="bytes"/>, keeping the rows when the calls are the same ones.</summary>
    private void Load(byte[] bytes)
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

        _errorArgument = null;
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(HasError));

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
            foreach (var call in script.Calls) Calls.Add(new ScriptCallViewModel(this, call));
            BuildFunctions(script);
        }

        OnPropertyChanged(nameof(IsReadOnly));
        foreach (var argument in Calls.SelectMany(c => c.Arguments)) argument.Refresh();
        UpdateTextRoom();
        RefreshSaved();
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

    /// <summary>Re-reads what the saved file holds, and marks the values that differ from it.</summary>
    public void RefreshSaved()
    {
        if (_script == null) return;
        var key = _world.TryGetSavedEdit(Archive, EntryName, out var savedEdit) ? savedEdit : OpenedBytes;
        if (!ReferenceEquals(key, _savedKey))
        {
            _savedKey = key;
            _savedValues = null; // the saved script can't be read: nothing to compare with
            if (EditableScript.TryLoad(savedEdit ?? OriginalBytes(), out var saved, out _))
            {
                _savedValues = new Dictionary<(int, int), string>();
                foreach (var argument in saved.Calls.SelectMany(c => c.Arguments))
                {
                    if (argument.Kind != ScriptValueKind.Computed)
                    {
                        _savedValues[(argument.Call.Address, argument.Index)] = ScriptArgumentViewModel.Format(argument);
                    }
                }
            }
        }

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

        ChangeCount = changes;
    }

    private void NotifyHistory()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        CommandManager.InvalidateRequerySuggested();
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
public sealed class ScriptArgumentViewModel : ObservableObject
{
    private readonly ScriptEditorViewModel _owner;
    private string _draft = "";
    private string? _error;
    private string? _savedValue;
    private int _room;

    public ScriptArgumentViewModel(ScriptEditorViewModel owner, ScriptArgument argument)
    {
        _owner = owner;
        Argument = argument;
        _draft = Value;
    }

    public ScriptArgument Argument { get; private set; }

    public string Name => Argument.Name;
    public bool IsText => Argument.Kind == ScriptValueKind.Text;
    public bool IsComputed => Argument.Kind == ScriptValueKind.Computed;
    public bool IsEditable => Argument.IsEditable && !_owner.IsReadOnly;

    /// <summary>The value in the script, as it is typed: 750, fx.lmp, (variable).</summary>
    public string Value => Format(Argument);

    /// <summary>What is in the box. Applied when confirmed (Enter, Tab or leaving the box).</summary>
    public string Draft
    {
        get => _draft;
        set
        {
            if (!SetProperty(ref _draft, value ?? "")) return;
            ClearError();
            OnPropertyChanged(nameof(ToolTip));
        }
    }

    public string? Error => _error;
    public bool HasError => _error != null;

    /// <summary>True when the value differs from the saved file.</summary>
    public bool IsModified => _savedValue != null;

    public string ToolTip
    {
        get
        {
            if (_error != null) return _error;

            var lines = new List<string>();
            if (IsComputed)
            {
                lines.Add((Argument.Source == "computed"
                    ? "Worked out when the script runs"
                    : $"Read from a {Argument.Source} when the script runs") + ", so there's no fixed value to edit.");
            }
            else if (!IsEditable)
            {
                lines.Add(Argument.Call.Note ?? "Read-only.");
            }
            else if (IsText)
            {
                lines.Add(_room > 0
                    ? $"Text. Up to {_room} characters fit in place; longer text is added to the end of the " +
                      "script's data."
                    : "Text. Other code may share this text, so a change is stored as new text for this call only.");
                if (_room > 0 && _draft.Length > _room && _draft != Value)
                {
                    lines.Add("This text is longer, so it will be added to the end of the script's data.");
                }
            }
            else
            {
                lines.Add("Whole number. Hexadecimal such as 0x1F works too.");
            }

            if (_savedValue != null) lines.Add($"Saved value: {(_savedValue.Length > 0 ? _savedValue : "(none)")}");
            if (IsEditable) lines.Add("Enter applies the change, Esc puts the value back.");
            return string.Join("\n", lines);
        }
    }

    public void Commit() => _owner.Commit(this);

    /// <summary>Discards what was typed.</summary>
    public void ResetDraft()
    {
        Draft = Value;
        ClearError();
    }

    internal static string Format(ScriptArgument argument) => argument.Kind switch
    {
        ScriptValueKind.Text => argument.Text ?? "",
        ScriptValueKind.Computed => $"({argument.Source})",
        _ => argument.Value.ToString(CultureInfo.InvariantCulture)
    };

    internal void Rebind(ScriptArgument argument) => Argument = argument;

    /// <summary>Shows the current value (after it was applied, undone or redone).</summary>
    internal void Refresh()
    {
        _draft = Value;
        OnPropertyChanged(nameof(Draft));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(IsEditable));
        OnPropertyChanged(nameof(ToolTip));
        ClearError();
    }

    internal void SetError(string message)
    {
        _error = message;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ToolTip));
        _owner.OnErrorChanged(this);
    }

    internal void ClearError()
    {
        if (_error == null) return;
        _error = null;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ToolTip));
        _owner.OnErrorChanged(this);
    }

    /// <summary>The saved value when it differs from the current one, else null.</summary>
    internal void SetSaved(string? savedValue)
    {
        if (_savedValue == savedValue) return;
        _savedValue = savedValue;
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ToolTip));
    }

    internal void SetRoom(int room)
    {
        if (_room == room) return;
        _room = room;
        OnPropertyChanged(nameof(ToolTip));
    }
}
