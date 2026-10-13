using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.DataContainers;
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
/// The Cutscene view: a cutscene's sound and subtitle settings, its cast and
/// its keyframes. Changing a value, a time, or copying or deleting a keyframe
/// writes the cutscene straight into the archive's pending edits, so the next
/// save (Ctrl+S) puts it in the GOB, and adds a step to its undo history.
/// </summary>
public sealed class CutsceneEditorViewModel : EntryEditorViewModel
{
    private static readonly object SettingsFocus = new();
    private static readonly object CastFocus = new();

    private readonly ListCollectionView _keyframesView;
    private readonly ListCollectionView _sectionsView;

    private EditableCutscene? _cutscene;
    private SavedCutscene? _saved;
    private CutsceneSection? _selectedSection;
    private string _filterText = "";

    /// <param name="changed">Called after the entry's bytes change (title, tree markers, disassembly).</param>
    public CutsceneEditorViewModel(World world, LmpFile archive, string entryName, Action changed)
        : base(world, archive, entryName, "cutscene", changed)
    {
        _keyframesView = new ListCollectionView(Keyframes) { Filter = item => IsShown((CutsceneKeyframeViewModel)item) };
        _sectionsView = new ListCollectionView(Sections);
        _sectionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CutsceneSection.Group)));

        DuplicateCommand = new RelayCommand(p => Duplicate((CutsceneKeyframeViewModel)p!),
            p => !IsReadOnly && p is CutsceneKeyframeViewModel);
        DeleteCommand = new RelayCommand(p => Delete((CutsceneKeyframeViewModel)p!),
            p => !IsReadOnly && p is CutsceneKeyframeViewModel && Keyframes.Count > 1);
        ClearFilterCommand = new RelayCommand(() => FilterText = "");

        TextValues = new[]
        {
            TextValue(CutsceneText.Subtitles, "Subtitles",
                "The subtitle text the scene shows (the name of its text, e.g. introcut_text1). Leave it empty for none."),
            TextValue(CutsceneText.Narration, "Narration",
                "The narration sound file, e.g. nar_bushwacked.vag. Leave it empty for none."),
            TextValue(CutsceneText.Music, "Music",
                "The music file, e.g. mx_cs_mugging_01_l.vag. Leave it empty for none.")
        };
        PositionValues = Enumerable.Range(0, 3).Select(PositionValue).ToArray();

        Initialize();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // What the view shows
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The side list: settings, cast, and the keyframes by who they're for.</summary>
    public ObservableCollection<CutsceneSection> Sections { get; } = new();

    public ICollectionView SectionsView => _sectionsView;

    public CutsceneSection? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (value == null || !SetProperty(ref _selectedSection, value)) return;
            OnPropertyChanged(nameof(ShowSettings));
            OnPropertyChanged(nameof(ShowCast));
            OnPropertyChanged(nameof(ShowKeyframes));
            RefreshView();
        }
    }

    public bool ShowSettings => _selectedSection?.Kind == CutsceneSectionKind.Settings;
    public bool ShowCast => _selectedSection?.Kind == CutsceneSectionKind.Cast;
    public bool ShowKeyframes => _selectedSection?.Kind == CutsceneSectionKind.Keyframes;

    /// <summary>Subtitles, narration and music.</summary>
    public IReadOnlyList<CutsceneValueViewModel> TextValues { get; }

    /// <summary>The header's position (x, y, z).</summary>
    public IReadOnlyList<CutsceneValueViewModel> PositionValues { get; }

    /// <summary>Header values whose meaning isn't known (shown, not edited).</summary>
    public string OtherInfo => _cutscene == null
        ? ""
        : $"Flags 0x{_cutscene.Flags:X4} · audio setting {_cutscene.AudioSetting}";

    public ObservableCollection<CutsceneActorViewModel> Cast { get; } = new();

    /// <summary>Every keyframe, in time order.</summary>
    public ObservableCollection<CutsceneKeyframeViewModel> Keyframes { get; } = new();

    /// <summary>The keyframes of the selected section that match the filter.</summary>
    public ICollectionView KeyframesView => _keyframesView;

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

    public ICommand DuplicateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearFilterCommand { get; }

    /// <summary>"184 keyframes · 6 in the cast · 66.5 s".</summary>
    public string Summary => _cutscene == null
        ? ""
        : $"{Plural.Of(_cutscene.Keyframes.Count, "keyframe")} · {_cutscene.Cast.Count:N0} in the cast · " +
          $"{NumberText.Format(_cutscene.Duration, 1)} s";

    /// <summary>Shown when no keyframes are listed.</summary>
    public string EmptyText
    {
        get
        {
            if (_cutscene == null || _selectedSection == null) return "";
            var scope = _selectedSection.Actor != null ? $" for {_selectedSection.Title}" : "";
            return IsFiltering ? $"No keyframes{scope} match “{_filterText}”." : $"There are no keyframes{scope}.";
        }
    }

    public bool IsEmpty => _keyframesView.IsEmpty;

    /// <summary>Asks the view to bring a keyframe into view (after undo, redo or copying).</summary>
    public event Action<CutsceneKeyframeViewModel>? RevealRequested;

    protected override bool IsLoaded => _cutscene != null;

    protected override IEnumerable<EditValueViewModel> Values =>
        TextValues.Concat(PositionValues)
            .Concat(Cast.SelectMany(a => a.Values))
            .Concat(Keyframes.SelectMany(k => k.Values));

    /// <summary>The name keyframes and lists use for a cast member ("Thief", "Thief #2", …).</summary>
    internal string CastName(int index)
    {
        if (_cutscene == null || index < 0 || index >= _cutscene.Cast.Count) return $"Cast member {index + 1}";
        var name = _cutscene.Cast[index].Name;
        var occurrence = _cutscene.Cast.Take(index).Count(a => a.Name == name) + 1;
        return occurrence == 1 ? name : $"{name} #{occurrence}";
    }

    internal EditableCutscene? Cutscene => _cutscene;

    // ─────────────────────────────────────────────────────────────────────────
    // Editing
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Applies <paramref name="value"/>'s draft with <paramref name="apply"/>, which returns an error or null.</summary>
    internal void Commit(CutsceneValueViewModel value, Func<string, string?> apply)
    {
        if (_cutscene == null || IsReadOnly) return;
        if (value.Draft == value.Value)
        {
            value.ClearError();
            return;
        }

        try
        {
            var problem = apply(value.Draft);
            if (problem != null)
            {
                value.SetError(problem);
                return;
            }

            value.Refresh(); // show the value as stored, e.g. "12.50" as "12.5"
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            value.SetError(ex.Message);
        }
    }

    private CutsceneValueViewModel TextValue(CutsceneText text, string name, string description) =>
        new(this, name, () => "", description, 260, null,
            () => _cutscene?.GetText(text) ?? "",
            draft =>
            {
                var value = draft.Trim();
                var problem = EditableCutscene.CheckText(value);
                if (problem != null) return problem;
                if (value != _cutscene!.GetText(text)) Apply(SettingsFocus, () => _cutscene.SetText(text, value));
                return null;
            });

    private CutsceneValueViewModel PositionValue(int axis) =>
        new(this, "XYZ"[axis].ToString(), () => "Position", "A point in the level stored with the scene, in world " +
                                                             "units. Its exact use isn't known; it may be where the player " +
                                                             "is placed when the scene ends.", 90, null,
            () => _cutscene == null ? "" : NumberText.Format(Axis(axis)),
            draft =>
            {
                if (!NumberText.TryParseDecimal(draft, out var value)) return "Enter a number, such as -1060.5.";
                var problem = EditableCutscene.CheckDecimal(value);
                if (problem != null) return problem;
                if ((float)value != Axis(axis)) Apply(SettingsFocus, () => _cutscene!.SetPosition(axis, (float)value));
                return null;
            });

    private float Axis(int axis) => axis switch
    {
        0 => _cutscene!.PositionX,
        1 => _cutscene!.PositionY,
        _ => _cutscene!.PositionZ
    };

    internal void ApplyCast(Action edit)
    {
        Apply(CastFocus, edit);
        foreach (var keyframe in Keyframes) keyframe.RefreshWho();
        UpdateSections();
    }

    internal void ApplyKeyframe(CutsceneKeyframeViewModel row, Action edit) =>
        Apply(Keyframes.IndexOf(row), edit);

    /// <summary>Keeps a keyframe in view after its new time moved it.</summary>
    internal void RevealKeyframe(CutsceneKeyframeViewModel row) => RevealRequested?.Invoke(row);

    private void Duplicate(CutsceneKeyframeViewModel row)
    {
        if (_cutscene == null || IsReadOnly) return;
        CutsceneKeyframe? copy = null;
        var index = Keyframes.IndexOf(row);
        try
        {
            Apply(index + 1, () => copy = _cutscene.Duplicate(row.Keyframe));
        }
        catch (InvalidOperationException ex)
        {
            row.Time.SetError(ex.Message);
            return;
        }

        var added = Keyframes.FirstOrDefault(k => ReferenceEquals(k.Keyframe, copy));
        if (added != null) RevealRequested?.Invoke(added);
    }

    private void Delete(CutsceneKeyframeViewModel row)
    {
        if (_cutscene == null || IsReadOnly) return;
        try
        {
            Apply(Keyframes.IndexOf(row), () => _cutscene.Remove(row.Keyframe));
        }
        catch (InvalidOperationException ex)
        {
            row.Time.SetError(ex.Message);
        }
    }

    protected override void OnEdited()
    {
        SyncKeyframeRows();
        UpdateSections();
        OnPropertyChanged(nameof(Summary));
    }

    protected override byte[] Serialize() => _cutscene!.ToArray();

    protected override object? FirstChange()
    {
        if (TextValues.Concat(PositionValues).Any(v => v.IsModified)) return SettingsFocus;
        if (Cast.SelectMany(a => a.Values).Any(v => v.IsModified)) return CastFocus;
        var row = Keyframes.FirstOrDefault(k => k.IsModified);
        return row != null ? Keyframes.IndexOf(row) : null;
    }

    protected override void Reveal(object? focus)
    {
        if (ReferenceEquals(focus, SettingsFocus) || ReferenceEquals(focus, CastFocus))
        {
            var kind = ReferenceEquals(focus, SettingsFocus) ? CutsceneSectionKind.Settings : CutsceneSectionKind.Cast;
            SelectedSection = Sections.FirstOrDefault(s => s.Kind == kind) ?? SelectedSection;
            return;
        }

        if (focus is not int index || Keyframes.Count == 0) return;
        var row = Keyframes[Math.Clamp(index, 0, Keyframes.Count - 1)];
        if (!ShowKeyframes || !IsShown(row))
        {
            _filterText = "";
            OnPropertyChanged(nameof(FilterText));
            OnPropertyChanged(nameof(IsFiltering));
            SelectedSection = Sections.First(s => s.Kind == CutsceneSectionKind.Keyframes);
            RefreshView();
        }

        RevealRequested?.Invoke(row);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────────────────────────────────

    protected override void Load(byte[] bytes)
    {
        if (!EditableCutscene.TryLoad(bytes, out var cutscene, out var error))
        {
            _cutscene = null;
            Cast.Clear();
            Keyframes.Clear();
            Sections.Clear();
            ReadOnlyReason = $"This cutscene can't be edited: {error} Its contents are in Details.";
            RefreshView();
            OnPropertyChanged(nameof(Summary));
            return;
        }

        var firstLoad = _cutscene == null;
        _cutscene = cutscene;

        if (Cast.Count != cutscene.Cast.Count)
        {
            Cast.Clear();
            for (var i = 0; i < cutscene.Cast.Count; i++)
            {
                var actor = new CutsceneActorViewModel(this, i);
                foreach (var value in actor.Values) Track(value);
                Cast.Add(actor);
            }
        }

        // Rows follow the keyframes by position: undo and redo restore an earlier order.
        while (Keyframes.Count > cutscene.Keyframes.Count) Keyframes.RemoveAt(Keyframes.Count - 1);
        for (var i = 0; i < cutscene.Keyframes.Count; i++)
        {
            if (i < Keyframes.Count) Keyframes[i].Rebind(cutscene.Keyframes[i]);
            else Keyframes.Add(NewRow(cutscene.Keyframes[i]));
        }

        if (firstLoad)
        {
            foreach (var value in TextValues.Concat(PositionValues)) Track(value);
        }

        foreach (var value in Values) value.Refresh();
        UpdateSections();
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(OtherInfo));
        OnPropertyChanged(nameof(Summary));
        RefreshView();
    }

    private CutsceneKeyframeViewModel NewRow(CutsceneKeyframe keyframe)
    {
        var row = new CutsceneKeyframeViewModel(this, keyframe);
        row.ValuesChanged += () =>
        {
            foreach (var value in row.Values) Track(value);
        };
        foreach (var value in row.Values) Track(value);
        return row;
    }

    /// <summary>Puts the rows in the keyframes' order after an edit: moved, copied and deleted keyframes.</summary>
    private void SyncKeyframeRows()
    {
        if (_cutscene == null) return;
        var keyframes = _cutscene.Keyframes;
        var current = new HashSet<CutsceneKeyframe>(keyframes);
        for (var i = Keyframes.Count - 1; i >= 0; i--)
        {
            if (!current.Contains(Keyframes[i].Keyframe)) Keyframes.RemoveAt(i);
        }

        for (var i = 0; i < keyframes.Count; i++)
        {
            if (i < Keyframes.Count && ReferenceEquals(Keyframes[i].Keyframe, keyframes[i])) continue;
            var existing = -1;
            for (var j = i + 1; j < Keyframes.Count; j++)
            {
                if (!ReferenceEquals(Keyframes[j].Keyframe, keyframes[i])) continue;
                existing = j;
                break;
            }

            if (existing >= 0) Keyframes.Move(existing, i);
            else Keyframes.Insert(i, NewRow(keyframes[i]));
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    /// <summary>Builds the side list, or updates its counts and names, keeping the selection.</summary>
    private void UpdateSections()
    {
        if (_cutscene == null) return;
        var keyframes = _cutscene.Keyframes;
        int Count(short actor) => keyframes.Count(k => k.Actor == actor);

        var wanted = new List<(CutsceneSectionKind Kind, short? Actor, string Title, int? Count)>
        {
            (CutsceneSectionKind.Settings, null, "Settings", null),
            (CutsceneSectionKind.Cast, null, "Cast", _cutscene.Cast.Count),
            (CutsceneSectionKind.Keyframes, null, "All keyframes", keyframes.Count),
            (CutsceneSectionKind.Keyframes, CutsceneActions.CameraActor, "Camera", Count(CutsceneActions.CameraActor)),
            (CutsceneSectionKind.Keyframes, CutsceneActions.SceneActor, "Scene events", Count(CutsceneActions.SceneActor))
        };
        for (var i = 0; i < _cutscene.Cast.Count; i++)
        {
            wanted.Add((CutsceneSectionKind.Keyframes, (short)i, CastName(i), Count((short)i)));
        }

        if (Sections.Count != wanted.Count)
        {
            var selected = _selectedSection;
            Sections.Clear();
            foreach (var (kind, actor, title, count) in wanted) Sections.Add(new CutsceneSection(kind, actor, title, count));
            _selectedSection = Sections.FirstOrDefault(s => selected != null && s.Kind == selected.Kind && s.Actor == selected.Actor)
                               ?? Sections[2];
            OnPropertyChanged(nameof(SelectedSection));
            OnPropertyChanged(nameof(ShowSettings));
            OnPropertyChanged(nameof(ShowCast));
            OnPropertyChanged(nameof(ShowKeyframes));
            return;
        }

        for (var i = 0; i < wanted.Count; i++)
        {
            Sections[i].Title = wanted[i].Title;
            Sections[i].Count = wanted[i].Count;
        }
    }

    protected override void ReadSaved(byte[] saved) =>
        _saved = EditableCutscene.TryLoad(saved, out var cutscene, out _) ? new SavedCutscene(cutscene) : null;

    protected override int MarkChanges()
    {
        if (_cutscene == null) return 0;
        var changes = 0;

        void Mark(EditValueViewModel value, string? saved)
        {
            value.SetSaved(saved == null || saved == value.Value ? null : saved);
            if (value.IsModified) changes++;
        }

        for (var i = 0; i < TextValues.Count; i++) Mark(TextValues[i], _saved?.Texts[i]);
        for (var i = 0; i < PositionValues.Count; i++) Mark(PositionValues[i], _saved?.Position[i]);
        for (var i = 0; i < Cast.Count; i++)
        {
            var saved = _saved != null && i < _saved.Cast.Count ? _saved.Cast[i] : null;
            for (var v = 0; v < Cast[i].Values.Count; v++) Mark(Cast[i].Values[v], saved?[v]);
        }

        if (_saved == null)
        {
            foreach (var row in Keyframes) row.IsModified = false;
            return changes;
        }

        // A keyframe is unchanged if the saved cutscene has an identical one.
        var unmatched = new Dictionary<SavedCutscene.Key, int>(_saved.Keyframes);
        var changedOrNew = 0;
        foreach (var row in Keyframes)
        {
            var key = SavedCutscene.KeyOf(row.Keyframe);
            if (unmatched.TryGetValue(key, out var count) && count > 0)
            {
                unmatched[key] = count - 1;
                row.IsModified = false;
            }
            else
            {
                row.IsModified = true;
                changedOrNew++;
            }
        }

        // A changed keyframe leaves its saved version unmatched too: pair those up,
        // and what is left over on either side was added or deleted.
        return changes + Math.Max(changedOrNew, unmatched.Values.Sum());
    }

    private bool IsShown(CutsceneKeyframeViewModel row) =>
        _selectedSection is { Kind: CutsceneSectionKind.Keyframes } section &&
        (section.Actor == null || row.Keyframe.Actor == section.Actor) &&
        (_filterText.Length == 0 || row.Matches(_filterText));

    private void RefreshView()
    {
        _keyframesView.Refresh();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    /// <summary>What the saved cutscene holds, to mark changes against.</summary>
    private sealed class SavedCutscene
    {
        public readonly record struct Key(float Time, short Actor, short Action, int P1, int P2, int P3);

        public SavedCutscene(EditableCutscene saved)
        {
            Texts = new[] { CutsceneText.Subtitles, CutsceneText.Narration, CutsceneText.Music }.Select(saved.GetText).ToArray();
            Position = new[] { saved.PositionX, saved.PositionY, saved.PositionZ }.Select(v => NumberText.Format(v)).ToArray();
            Cast = saved.Cast.Select(CutsceneActorViewModel.Format).ToList();
            Keyframes = saved.Keyframes.GroupBy(KeyOf).ToDictionary(g => g.Key, g => g.Count());
        }

        public string[] Texts { get; }
        public string[] Position { get; }

        /// <summary>Each cast member's values, as <see cref="CutsceneActorViewModel.Values"/> shows them.</summary>
        public List<string[]> Cast { get; }

        public Dictionary<Key, int> Keyframes { get; }

        public static Key KeyOf(CutsceneKeyframe k) =>
            new(k.Time, k.Actor, k.Action, k.Parameters[0], k.Parameters[1], k.Parameters[2]);
    }
}

public enum CutsceneSectionKind
{
    Settings,
    Cast,
    Keyframes
}

/// <summary>An entry in the Cutscene view's side list: the settings, the cast, or whose keyframes to show.</summary>
public sealed class CutsceneSection : ObservableObject
{
    private string _title;
    private int? _count;

    public CutsceneSection(CutsceneSectionKind kind, short? actor, string title, int? count)
    {
        Kind = kind;
        Actor = actor;
        _title = title;
        _count = count;
    }

    public CutsceneSectionKind Kind { get; }

    /// <summary>"SCENE" or "KEYFRAMES": the list's group headings.</summary>
    public string Group => Kind == CutsceneSectionKind.Keyframes ? "KEYFRAMES" : "SCENE";

    /// <summary>For keyframes: whose (-100 camera, -99 scene events, else a cast member); null for all.</summary>
    public short? Actor { get; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public int? Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }
}

/// <summary>A row of the Cutscene view's cast list: name, start position and facing.</summary>
public sealed class CutsceneActorViewModel
{
    public CutsceneActorViewModel(CutsceneEditorViewModel owner, int index)
    {
        Index = index;
        CutsceneActor Actor() => owner.Cutscene!.Cast[index];
        string Context() => owner.CastName(index);

        Values = new[]
        {
            new CutsceneValueViewModel(owner, "name", Context,
                "The character this cast member is, e.g. Player or Karne. Change it only to another character the " +
                "level has, or the scene may not find them.", 150, null,
                () => owner.Cutscene == null ? "" : Actor().Name,
                draft =>
                {
                    var name = draft.Trim();
                    var problem = EditableCutscene.CheckName(name);
                    if (problem != null) return problem;
                    if (name != Actor().Name) owner.ApplyCast(() => owner.Cutscene!.SetName(Actor(), name));
                    return null;
                }),
            Coordinate(owner, 0, Actor, Context),
            Coordinate(owner, 1, Actor, Context),
            Coordinate(owner, 2, Actor, Context),
            new CutsceneValueViewModel(owner, "facing", Context,
                "Which way the cast member faces at the start, in degrees (0 to 360).", 80, "°",
                () => owner.Cutscene == null ? "" : NumberText.Format(Actor().Facing),
                draft =>
                {
                    if (!NumberText.TryParseDecimal(draft, out var degrees)) return "Enter an angle in degrees, such as 90.";
                    var problem = EditableCutscene.CheckDecimal(degrees);
                    if (problem != null) return problem;
                    if (CutsceneActor.ToFacingUnits(degrees) != Actor().FacingUnits)
                    {
                        owner.ApplyCast(() => owner.Cutscene!.SetFacing(Actor(), degrees));
                    }

                    return null;
                })
        };
    }

    /// <summary>Position in the cast ("#1" is the first).</summary>
    public int Index { get; }

    public string Number => $"#{Index + 1}";

    /// <summary>Name, X, Y, Z and facing.</summary>
    public IReadOnlyList<CutsceneValueViewModel> Values { get; }

    /// <summary>A cast member's values as <see cref="Values"/> shows them.</summary>
    internal static string[] Format(CutsceneActor actor) => new[]
    {
        actor.Name, NumberText.Format(actor.X), NumberText.Format(actor.Y), NumberText.Format(actor.Z),
        NumberText.Format(actor.Facing)
    };

    private static CutsceneValueViewModel Coordinate(CutsceneEditorViewModel owner, int axis, Func<CutsceneActor> actor,
        Func<string> context) =>
        new(owner, "XYZ"[axis].ToString(), context, "Where the cast member starts, in world units.", 90, null,
            () => owner.Cutscene == null ? "" : NumberText.Format(Get(actor(), axis)),
            draft =>
            {
                if (!NumberText.TryParseDecimal(draft, out var value)) return "Enter a number, such as 185.5.";
                var problem = EditableCutscene.CheckDecimal(value);
                if (problem != null) return problem;
                if ((float)value != Get(actor(), axis))
                {
                    owner.ApplyCast(() => owner.Cutscene!.SetPosition(actor(), axis, (float)value));
                }

                return null;
            });

    private static float Get(CutsceneActor actor, int axis) => axis switch
    {
        0 => actor.X,
        1 => actor.Y,
        _ => actor.Z
    };
}

/// <summary>A row of the Cutscene view's keyframe list: when, who, what, and its values.</summary>
public sealed class CutsceneKeyframeViewModel : ObservableObject
{
    private readonly CutsceneEditorViewModel _owner;
    private bool _isModified;

    public CutsceneKeyframeViewModel(CutsceneEditorViewModel owner, CutsceneKeyframe keyframe)
    {
        _owner = owner;
        Keyframe = keyframe;
        Time = new CutsceneValueViewModel(owner, "time", () => Context,
            "Seconds from the start of the scene. Keyframes stay in time order, so a new time can move the row.", 70,
            "s",
            () => NumberText.Format(Keyframe.Time),
            draft =>
            {
                if (!NumberText.TryParseDecimal(draft, out var seconds)) return "Enter a time in seconds, such as 12.5.";
                var problem = EditableCutscene.CheckTime(seconds);
                if (problem != null) return problem;
                if ((float)seconds != Keyframe.Time)
                {
                    _owner.ApplyKeyframe(this, () => _owner.Cutscene!.SetTime(Keyframe, seconds));
                    _owner.RevealKeyframe(this);
                }

                return null;
            });
        Fields = BuildFields();
    }

    public CutsceneKeyframe Keyframe { get; private set; }

    public CutsceneValueViewModel Time { get; }

    /// <summary>The keyframe's editable values (none for events whose meaning isn't known).</summary>
    public IReadOnlyList<CutsceneValueViewModel> Fields { get; private set; }

    /// <summary>The time and the fields.</summary>
    public IEnumerable<CutsceneValueViewModel> Values => Fields.Prepend(Time);

    /// <summary>Raised when the row's keyframe became a different kind (its fields were rebuilt).</summary>
    public event Action? ValuesChanged;

    /// <summary>"Camera", "Scene" or the cast member's name.</summary>
    public string Who => Keyframe.ActorKind switch
    {
        CutsceneActorKind.Camera => "Camera",
        CutsceneActorKind.Scene => "Scene",
        _ => _owner.CastName(Keyframe.Actor)
    };

    /// <summary>What happens: "Position", "Move to", "Next subtitle"…</summary>
    public string What => Keyframe.Description;

    public string IconKey => Keyframe.ActorKind switch
    {
        CutsceneActorKind.Camera => "Icon.Camera",
        CutsceneActorKind.Scene => "Icon.Kind.Dialog",
        _ => "Icon.View.Skeleton"
    };

    /// <summary>The stored parameters of an action that can't be edited (or "").</summary>
    public string RawText => Keyframe.RawText;

    public bool HasRawText => RawText.Length > 0;

    public string ToolTip => (Keyframe.ActorKind, Keyframe.Action) switch
    {
        (CutsceneActorKind.Camera, 0) => "Moves the camera to a point in the level (world units).",
        (CutsceneActorKind.Camera, 1) => "Turns the camera to face a direction (degrees).",
        (CutsceneActorKind.Camera, 2) => "Tilts the camera up or down (degrees).",
        (CutsceneActorKind.Camera, 6) => "How far the camera draws the level.",
        (CutsceneActorKind.Scene, 0) => "Shows the next line of the scene's subtitles.",
        (CutsceneActorKind.Scene, 3) => "Hides the subtitle on screen.",
        (CutsceneActorKind.Cast, 0) => "Makes the cast member walk to a point in the level (world units).",
        (CutsceneActorKind.Cast, 2) => "Plays an animation on the cast member. The animation is stored as a code, " +
                                       "so it can't be changed here.",
        _ when Fields.Count > 0 => "What this value does isn't known for certain.",
        _ => "What this event does isn't known. You can move it in time, copy it or delete it."
    };

    /// <summary>The keyframe differs from every keyframe in the saved file (changed or new).</summary>
    public bool IsModified
    {
        get => _isModified;
        set => SetProperty(ref _isModified, value);
    }

    private string Context => $"{Who} {What.ToLowerInvariant()} at {NumberText.Format(Keyframe.Time)} s";

    internal void Rebind(CutsceneKeyframe keyframe)
    {
        var sameKind = keyframe.Actor == Keyframe.Actor && keyframe.Action == Keyframe.Action;
        Keyframe = keyframe;
        if (!sameKind)
        {
            Fields = BuildFields();
            OnPropertyChanged(nameof(Fields));
            ValuesChanged?.Invoke();
        }

        RefreshWho();
        OnPropertyChanged(nameof(What));
        OnPropertyChanged(nameof(IconKey));
        OnPropertyChanged(nameof(RawText));
        OnPropertyChanged(nameof(HasRawText));
        OnPropertyChanged(nameof(ToolTip));
    }

    internal void RefreshWho() => OnPropertyChanged(nameof(Who));

    internal bool Matches(string filter) =>
        Who.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        What.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        RawText.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        Values.Any(v => v.Value.Contains(filter, StringComparison.OrdinalIgnoreCase));

    private IReadOnlyList<CutsceneValueViewModel> BuildFields() => Keyframe.Fields.Select(field =>
    {
        var (unit, width, hint) = field.Kind switch
        {
            CutsceneValueKind.Angle => ("°", 80.0, "An angle in degrees."),
            CutsceneValueKind.Decimal => ((string?)null, 80.0, "A number."),
            _ => (null, 80.0, field.Name.Length == 1 ? "A position in world units." : "A whole number.")
        };
        return new CutsceneValueViewModel(_owner, field.Name, () => Context, hint, width, unit,
            () => field.Kind switch
            {
                CutsceneValueKind.Integer => ((int)Keyframe.GetValue(field)).ToString(CultureInfo.InvariantCulture),
                CutsceneValueKind.Angle => NumberText.Format(Keyframe.GetValue(field), 2),
                _ => NumberText.Format(Keyframe.GetValue(field))
            },
            draft =>
            {
                if (!NumberText.TryParseDecimal(draft, out var value)) return "Enter a number.";
                var problem = EditableCutscene.CheckValue(field, value);
                if (problem != null) return problem;
                if (Keyframe.WouldChange(field, value))
                {
                    _owner.ApplyKeyframe(this, () => _owner.Cutscene!.SetValue(Keyframe, field, value));
                }

                return null;
            });
    }).ToList();
}

/// <summary>A value box in the Cutscene view.</summary>
public sealed class CutsceneValueViewModel : EditValueViewModel
{
    private readonly CutsceneEditorViewModel _owner;
    private readonly string _name;
    private readonly Func<string> _context;
    private readonly string _description;
    private readonly Func<string> _read;
    private readonly Func<string, string?> _apply;

    /// <param name="apply">Applies a draft (through the editor, as an undoable step) and returns an error, or null.</param>
    public CutsceneValueViewModel(CutsceneEditorViewModel owner, string name, Func<string> context, string description,
        double boxWidth, string? unit, Func<string> read, Func<string, string?> apply)
    {
        _owner = owner;
        _name = name;
        _context = context;
        _description = description;
        _read = read;
        _apply = apply;
        BoxWidth = boxWidth;
        Unit = unit;
        Refresh();
    }

    public override string Name => _name;
    public override string Context => _context();
    public override string Value => _read();
    public override bool IsEditable => !_owner.IsReadOnly;

    public double BoxWidth { get; }

    /// <summary>Shown after the box, e.g. "°" or "s".</summary>
    public string? Unit { get; }

    /// <summary>What the value is (also the start of its tooltip).</summary>
    public string Description => _description;

    protected override IEnumerable<string> Describe()
    {
        yield return _description;
    }

    public override void Commit() => _owner.Commit(this, _apply);
}
