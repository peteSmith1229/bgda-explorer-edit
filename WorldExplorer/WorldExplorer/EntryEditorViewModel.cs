using JetBlackEngineLib.Data.DataContainers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>
/// Base for the editors of one archive entry (the Script and Cutscene views).
/// Every change writes the whole entry straight into the archive's pending
/// edits, so the next save (Ctrl+S) puts it in the GOB, and becomes a step of
/// the entry's undo history; undo, redo and Revert put whole snapshots back.
/// </summary>
public abstract class EntryEditorViewModel : ObservableObject
{
    private static readonly object OpenedBytes = new();

    private readonly Action _changed;
    private readonly bool _deleted;
    private readonly Stack<Step> _undo = new();
    private readonly Stack<Step> _redo = new();
    private byte[]? _pending;
    private object? _savedKey;
    private string? _readOnlyReason;
    private int _changeCount;
    private EditValueViewModel? _errorValue;

    /// <summary>One edit: the entry's pending bytes before and after it (null: no pending edit), and what it changed.</summary>
    private sealed record Step(byte[]? Before, byte[]? After, object? Focus);

    /// <param name="noun">What the entry is, e.g. "script", for messages.</param>
    /// <param name="changed">Called after the entry's bytes change (title, tree markers, disassembly).</param>
    protected EntryEditorViewModel(World world, LmpFile archive, string entryName, string noun, Action changed)
    {
        World = world;
        Archive = archive;
        EntryName = entryName;
        _changed = changed;
        _deleted = archive.PendingDeletions.Contains(entryName);
        _pending = PendingBytes();
        RevertCommand = new RelayCommand(RevertToSaved, () => !IsReadOnly && world.IsEntryUnsaved(archive, entryName));

        if (archive is ClpFile)
        {
            _readOnlyReason = $"{char.ToUpper(noun[0], CultureInfo.InvariantCulture)}{noun[1..]}s in CLP archives can't be edited.";
        }
        else if (_deleted)
        {
            _readOnlyReason = $"This {noun} is marked for deletion. Restore it from the explorer to edit it.";
        }
        else if (archive.IsAdded(entryName))
        {
            _readOnlyReason = $"This {noun} was added to the archive. Save the file and open it again to edit it.";
        }
    }

    protected World World { get; }
    public LmpFile Archive { get; }
    public string EntryName { get; }

    public ICommand RevertCommand { get; }

    /// <summary>Why the entry can't be changed, or null when it can.</summary>
    public string? ReadOnlyReason
    {
        get => _readOnlyReason;
        protected set
        {
            if (!SetProperty(ref _readOnlyReason, value)) return;
            OnPropertyChanged(nameof(IsReadOnly));
        }
    }

    public bool IsReadOnly => _readOnlyReason != null || !IsLoaded;

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

    /// <summary>Why the last value typed wasn't applied, while it is still in its box.</summary>
    public string? ErrorText => _errorValue == null
        ? null
        : $"{(_errorValue.Context.Length > 0 ? _errorValue.Context + " " : "")}{_errorValue.Name}: {_errorValue.Error}";

    public bool HasError => _errorValue != null;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// True while the entry still holds the bytes this editor last wrote or
    /// read; otherwise something else changed it and the editor is out of date.
    /// </summary>
    public bool IsCurrent =>
        ReferenceEquals(PendingBytes(), _pending) && Archive.PendingDeletions.Contains(EntryName) == _deleted;

    /// <summary>The entry's current bytes.</summary>
    public byte[] CurrentBytes() => PendingBytes() ?? OriginalBytes();

    // ─────────────────────────────────────────────────────────────────────────
    // What each editor provides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The entry was read (it can still be read-only).</summary>
    protected abstract bool IsLoaded { get; }

    /// <summary>The value boxes, for <see cref="CommitAll"/>.</summary>
    protected abstract IEnumerable<EditValueViewModel> Values { get; }

    /// <summary>Reads (or re-reads, after undo or redo) the entry's bytes into the editor.</summary>
    protected abstract void Load(byte[] bytes);

    /// <summary>The entry's bytes, after an edit to the editor's model.</summary>
    protected abstract byte[] Serialize();

    /// <summary>Remembers what the saved file holds, to compare with.</summary>
    protected abstract void ReadSaved(byte[] saved);

    /// <summary>Marks what differs from the saved file and returns how many changes there are.</summary>
    protected abstract int MarkChanges();

    /// <summary>Called after an edit changed the model, before the changes are marked.</summary>
    protected virtual void OnEdited()
    {
    }

    /// <summary>Something to bring into view after Revert (the first change), or null.</summary>
    protected virtual object? FirstChange() => null;

    /// <summary>Brings into view what an undone or redone step changed.</summary>
    protected virtual void Reveal(object? focus)
    {
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Editing
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads the entry; derived constructors call it last.</summary>
    protected void Initialize()
    {
        Load(CurrentBytes());
        RefreshSaved();
    }

    /// <summary>Applies values that were typed but not yet confirmed (before saving or closing).</summary>
    public void CommitAll()
    {
        foreach (var value in Values.ToList())
        {
            if (value.IsEditable && value.Draft != value.Value) value.Commit();
        }
    }

    /// <summary>
    /// Runs <paramref name="edit"/> on the editor's model, then writes the entry
    /// into the archive as one undoable step. <paramref name="focus"/> is what
    /// to bring into view when the step is undone or redone.
    /// </summary>
    protected void Apply(object? focus, Action edit)
    {
        var before = PendingBytes();
        edit();
        var after = Serialize();
        Archive.ReplaceEntry(EntryName, after);
        _pending = after;
        _undo.Push(new Step(before, after, focus));
        _redo.Clear();

        OnEdited();
        RefreshSaved();
        NotifyHistory();
        _changed();
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        var step = _undo.Pop();
        _redo.Push(step);
        Restore(step.Before, step.Focus);
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        var step = _redo.Pop();
        _undo.Push(step);
        Restore(step.After, step.Focus);
    }

    private void Restore(byte[]? bytes, object? focus)
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
        ClearError();
        Load(bytes ?? OriginalBytes());
        RefreshSaved();
        NotifyHistory();
        _changed();
        Reveal(focus);
    }

    /// <summary>Puts back the saved entry (an undoable step, like any other edit).</summary>
    private void RevertToSaved()
    {
        if (IsReadOnly) return;
        var saved = World.TryGetSavedEdit(Archive, EntryName, out var savedEdit) ? savedEdit : null;
        var before = PendingBytes();
        if (ReferenceEquals(before, saved)) return;

        // Putting back the saved array itself (or no pending edit) leaves nothing unsaved.
        var focus = FirstChange();
        _undo.Push(new Step(before, saved, focus));
        _redo.Clear();
        Restore(saved, focus);
    }

    /// <summary>Re-reads what the saved file holds (after a save), and marks the values that differ from it.</summary>
    public void RefreshSaved()
    {
        if (!IsLoaded) return;
        var key = World.TryGetSavedEdit(Archive, EntryName, out var savedEdit) ? savedEdit : OpenedBytes;
        if (!ReferenceEquals(key, _savedKey))
        {
            _savedKey = key;
            ReadSaved(savedEdit ?? OriginalBytes());
        }

        ChangeCount = MarkChanges();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Errors
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Shows <paramref name="value"/>'s errors in the footer.</summary>
    protected void Track(EditValueViewModel value) => value.ErrorChanged += OnValueErrorChanged;

    private void OnValueErrorChanged(EditValueViewModel value)
    {
        if (value.HasError) _errorValue = value;
        else if (ReferenceEquals(_errorValue, value)) _errorValue = null;
        else return;

        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(HasError));
    }

    protected void ClearError()
    {
        if (_errorValue == null) return;
        _errorValue = null;
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(HasError));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────────────────────────────────

    private byte[]? PendingBytes() => Archive.PendingEdits.TryGetValue(EntryName, out var bytes) ? bytes : null;

    /// <summary>The entry as it is in the opened file.</summary>
    protected byte[] OriginalBytes()
    {
        // An added entry has no original: its added bytes stand in.
        if (Archive.IsAdded(EntryName)) return Archive.GetEntryBytes(EntryName)!;
        var entry = Archive.Directory[EntryName];
        return Archive.FileData.AsSpan(entry.StartOffset, entry.Length).ToArray();
    }

    private void NotifyHistory()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A value box in an entry editor: what is typed into it (<see cref="Draft"/>),
/// the value it edits, whether that differs from the saved file, and why the
/// last attempt to apply it failed.
/// </summary>
public abstract class EditValueViewModel : ObservableObject
{
    private string _draft = "";
    private string? _error;
    private string? _savedValue;

    /// <summary>The label next to the box, e.g. "amount".</summary>
    public abstract string Name { get; }

    /// <summary>What the value belongs to, for error messages, e.g. "givePlayerGold".</summary>
    public virtual string Context => "";

    /// <summary>The value as it is typed.</summary>
    public abstract string Value { get; }

    public abstract bool IsEditable { get; }

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
            var lines = Describe().ToList();
            if (_savedValue != null) lines.Add($"Saved value: {(_savedValue.Length > 0 ? _savedValue : "(none)")}");
            if (IsEditable) lines.Add("Enter applies the change, Esc puts the value back.");
            return string.Join("\n", lines);
        }
    }

    /// <summary>Raised when <see cref="Error"/> appears or clears.</summary>
    public event Action<EditValueViewModel>? ErrorChanged;

    /// <summary>The tooltip's description of the value.</summary>
    protected abstract IEnumerable<string> Describe();

    /// <summary>Applies <see cref="Draft"/>, or sets <see cref="Error"/>.</summary>
    public abstract void Commit();

    /// <summary>Discards what was typed.</summary>
    public void ResetDraft()
    {
        Draft = Value;
        ClearError();
    }

    /// <summary>Shows the current value (after it was applied, undone or redone).</summary>
    public void Refresh()
    {
        _draft = Value;
        OnPropertyChanged(nameof(Draft));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(IsEditable));
        OnPropertyChanged(nameof(ToolTip));
        ClearError();
    }

    public void SetError(string message)
    {
        _error = message;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ToolTip));
        ErrorChanged?.Invoke(this);
    }

    public void ClearError()
    {
        if (_error == null) return;
        _error = null;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ToolTip));
        ErrorChanged?.Invoke(this);
    }

    /// <summary>The saved value when it differs from the current one, else null.</summary>
    public void SetSaved(string? savedValue)
    {
        if (_savedValue == savedValue) return;
        _savedValue = savedValue;
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ToolTip));
    }

    /// <summary>Re-reads the tooltip (when what it describes changed).</summary>
    protected void RefreshToolTip() => OnPropertyChanged(nameof(ToolTip));
}
