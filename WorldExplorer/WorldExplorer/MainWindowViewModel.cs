/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using JetBlackEngineLib;
using JetBlackEngineLib.Data.Animation;
using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Scripting;
using JetBlackEngineLib.Data.Textures;
using JetBlackEngineLib.Data.World;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WorldExplorer.Infrastructure;
using WorldExplorer.Logging;
using WorldExplorer.TreeView;

namespace WorldExplorer;

public class MainWindowViewModel : ObservableObject
{
    private const int MaxRecentFiles = 10;

    private string? _gobFile;
    private LevelViewModel _levelViewModel;
    private string? _logText;
    private string? _detailsTitle;
    private ModelViewModel _modelViewModel;
    private object? _selectedNode;
    private WriteableBitmap? _selectedNodeImage;
    private SkeletonViewModel _skeletonViewModel;
    private ViewOption? _selectedView;
    private OverviewModel? _overview;
    private bool _isBusy;
    private string? _busyText;
    private string _statusText = "Ready";
    private string _filterText = "";
    private GameOption _selectedGame;

    // Views the user picked by hand, remembered per kind of selection, so
    // e.g. browsing models while looking at their textures stays on Texture.
    private readonly Dictionary<NodeKind, ContentView> _preferredViews = new();
    private bool _updatingViews;

    // Level decoding runs on a worker thread; the gate keeps two decodes from
    // sharing a level texture cache at the same time.
    private readonly SemaphoreSlim _decodeGate = new(1, 1);
    private readonly HashSet<WorldFileTreeViewModel> _decoding = new();
    private int _selectionVersion;

    private readonly DispatcherTimer _filterTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public MainWindowViewModel(MainWindow window)
    {
        MainWindow = window;

        // Create View Models
        _modelViewModel = new ModelViewModel(this);
        _skeletonViewModel = new SkeletonViewModel(this);
        _levelViewModel = new LevelViewModel(this);

        _selectedGame = GameOption.For(App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance));
        _filterTimer.Tick += (_, _) =>
        {
            _filterTimer.Stop();
            ApplyFilter();
        };

        LoadRecentFiles();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core state
    // ─────────────────────────────────────────────────────────────────────────

    public MainWindow MainWindow { get; }

    public World? World { get; private set; }

    /// <summary>The explorer tree's root (a single node for the opened file).</summary>
    public ObservableCollection<TreeViewItemViewModel> RootNodes { get; } = new();

    public NotificationService Notifications { get; } = new();

    public bool IsFileOpen => World != null;

    public string? FilePath => World?.FilePath;

    public SkeletonViewModel TheSkeletonViewModel
    {
        get => _skeletonViewModel;
        set => SetProperty(ref _skeletonViewModel, value);
    }

    public ModelViewModel TheModelViewModel
    {
        get => _modelViewModel;
        set => SetProperty(ref _modelViewModel, value);
    }

    public LevelViewModel TheLevelViewModel
    {
        get => _levelViewModel;
        set => SetProperty(ref _levelViewModel, value);
    }

    /// <summary>The image shown by the Texture view.</summary>
    public WriteableBitmap? SelectedNodeImage
    {
        get => _selectedNodeImage;
        set
        {
            _selectedNodeImage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TextureInfo));
        }
    }

    /// <summary>"256 × 256" for the Texture view's toolbar.</summary>
    public string? TextureInfo => _selectedNodeImage == null
        ? null
        : $"{_selectedNodeImage.PixelWidth} × {_selectedNodeImage.PixelHeight} px";

    /// <summary>Text shown by the Details view (decoded data, logs, hex dumps).</summary>
    public string? LogText
    {
        get => _logText;
        set
        {
            _logText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailsLineCount));
        }
    }

    /// <summary>Heading for the Details view, e.g. "Script disassembly".</summary>
    public string? DetailsTitle
    {
        get => _detailsTitle;
        set => SetProperty(ref _detailsTitle, value);
    }

    public string DetailsLineCount
    {
        get
        {
            if (string.IsNullOrEmpty(_logText)) return "";
            var lines = 1;
            foreach (var c in _logText) if (c == '\n') lines++;
            return $"{lines:N0} lines";
        }
    }

    public OverviewModel? Overview
    {
        get => _overview;
        private set => SetProperty(ref _overview, value);
    }

    public object? SelectedNode
    {
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            _selectionVersion++;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTreeNode));

            LogText = null;
            DetailsTitle = null;

            try
            {
                DispatchSelection(value);
            }
            catch (Exception ex)
            {
                ShowFailure($"Couldn't display {(value as TreeViewItemViewModel)?.Label ?? "the selection"}", ex,
                    replaceViews: true);
            }
        }
    }

    /// <summary>The selection as a tree node, for the content header.</summary>
    public TreeViewItemViewModel? SelectedTreeNode => _selectedNode as TreeViewItemViewModel;

    // ─────────────────────────────────────────────────────────────────────────
    // Views
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The views that make sense for the current selection.</summary>
    public ObservableCollection<ViewOption> AvailableViews { get; } = new();

    /// <summary>The view on screen. Bound two-way to the view switcher.</summary>
    public ViewOption? SelectedView
    {
        get => _selectedView;
        set
        {
            // The switcher momentarily pushes null while its items change.
            if (value == null || ReferenceEquals(value, _selectedView)) return;
            _selectedView = value;

            if (!_updatingViews && SelectedTreeNode != null)
            {
                _preferredViews[SelectedTreeNode.Kind] = value.Kind;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveView));
        }
    }

    public ContentView? ActiveView => _selectedView?.Kind;

    /// <summary>
    /// Offers <paramref name="views"/> (plus <paramref name="defaultView"/>)
    /// and shows the default — or the view the user last chose for this kind
    /// of item, when it is on offer.
    /// </summary>
    private void ShowViews(ContentView defaultView, params ContentView[] views)
    {
        var offered = new HashSet<ContentView>(views) { defaultView };
        var target = defaultView;
        if (SelectedTreeNode != null &&
            _preferredViews.TryGetValue(SelectedTreeNode.Kind, out var preferred) &&
            offered.Contains(preferred))
        {
            target = preferred;
        }

        _updatingViews = true;
        try
        {
            var ordered = ViewOption.All.Where(v => offered.Contains(v.Kind)).ToList();
            if (!ordered.SequenceEqual(AvailableViews))
            {
                AvailableViews.Clear();
                foreach (var option in ordered) AvailableViews.Add(option);
            }

            _selectedView = null;   // force a change notification even if unchanged
            SelectedView = ViewOption.For(target);
        }
        finally
        {
            _updatingViews = false;
        }
    }

    /// <summary>Adds <paramref name="view"/> to the offered views without changing the active one.</summary>
    private void OfferView(ContentView view)
    {
        if (AvailableViews.Any(v => v.Kind == view)) return;
        var current = ActiveView ?? view;
        ShowViews(current, AvailableViews.Select(v => v.Kind).Append(view).ToArray());
    }

    /// <summary>Switches to <paramref name="view"/> if it is offered for the selection.</summary>
    public bool TryShowView(ContentView view)
    {
        var option = AvailableViews.FirstOrDefault(v => v.Kind == view);
        if (option == null) return false;
        SelectedView = option;
        return true;
    }

    /// <summary>Shows text in the Details view and switches to it.</summary>
    public void ShowDetails(string text, string? title = null)
    {
        LogText = text;
        DetailsTitle = title;
        OfferView(ContentView.Details);
        TryShowView(ContentView.Details);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Status, busy state and errors
    // ─────────────────────────────────────────────────────────────────────────

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? BusyText
    {
        get => _busyText;
        private set => SetProperty(ref _busyText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private void SetBusy(string text)
    {
        BusyText = text;
        IsBusy = true;
        StatusText = text;
    }

    private void ClearBusy()
    {
        IsBusy = false;
        BusyText = null;

        // Work finished off the UI thread: refresh Save/Undo/… enablement now
        // rather than on the next input event.
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Reports a failure: a notification plus the full details in the Details
    /// view. <paramref name="replaceViews"/> drops the other views (used when
    /// the failure is about the current selection, so stale content from the
    /// previous item isn't left on offer).
    /// </summary>
    public void ShowFailure(string title, Exception ex, bool replaceViews = false)
    {
        // Reading a file with the wrong game's formats typically fails like this.
        var hint = ex is ArgumentException or IndexOutOfRangeException or EndOfStreamException
                       or InvalidDataException or OverflowException or FormatException
            ? $"If this file is from a game other than {_selectedGame.Name}, choose its game in the toolbar."
            : null;
        var details = $"{title}\n\n{ex.GetType().Name}: {ex.Message}\n\n" +
                      (hint == null ? "" : hint + "\n\n") + ex.StackTrace;

        LogText = details;
        DetailsTitle = "Error";
        if (replaceViews || AvailableViews.Count == 0)
        {
            ShowViews(ContentView.Details, ContentView.Details);
        }
        else
        {
            OfferView(ContentView.Details);
        }

        Notifications.Show(title, hint == null ? ex.Message : $"{ex.Message} {hint}", ToastKind.Error, "Show details",
            () => TryShowView(ContentView.Details));
        StatusText = title;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Game (engine version)
    // ─────────────────────────────────────────────────────────────────────────

    public IReadOnlyList<GameOption> Games => GameOption.All;

    /// <summary>
    /// The game whose formats are decoded. Changing it reloads the open file
    /// (after confirming if there are unsaved edits).
    /// </summary>
    public GameOption SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (value == null || ReferenceEquals(value, _selectedGame)) return;
            if (!MainWindow.ConfirmGameChange(value))
            {
                // Revert the combo box once the current binding update finishes.
                MainWindow.Dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(SelectedGame)));
                return;
            }

            ApplyGame(value);
            if (World != null) _ = MainWindow.ReloadAsync();
        }
    }

    /// <summary>Stores and announces a game change without reloading.</summary>
    public void ApplyGame(GameOption game)
    {
        _selectedGame = game;
        App.Settings["Core.EngineVersion"] = game.Version;
        App.SaveSettings();
        OnPropertyChanged(nameof(SelectedGame));
        OnPropertyChanged(nameof(IsDarkAlliance));
        OnPropertyChanged(nameof(IsNotDarkAlliance));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public EngineVersion EngineVersion => _selectedGame.Version;

    /// <summary>
    /// The executable / save / script-reward tools are reverse-engineered
    /// against Baldur's Gate: Dark Alliance only, so they're hidden for the
    /// other games.
    /// </summary>
    public bool IsDarkAlliance => EngineVersion == EngineVersion.DarkAlliance;

    public bool IsNotDarkAlliance => !IsDarkAlliance;

    // ─────────────────────────────────────────────────────────────────────────
    // Unsaved changes / title
    // ─────────────────────────────────────────────────────────────────────────

    public bool HasUnsavedChanges => World?.HasUnsavedChanges() == true;

    public string UnsavedSummary
    {
        get
        {
            var count = World?.CountUnsavedEntries() ?? 0;
            return count switch
            {
                0 => HasUnsavedChanges ? "Unsaved changes" : "",
                1 => "1 unsaved change",
                _ => $"{count} unsaved changes"
            };
        }
    }

    public string WindowTitle
    {
        get
        {
            if (World == null) return "WorldExplorer";
            var dirty = HasUnsavedChanges ? "● " : "";
            return $"{dirty}{World.Name} — {_selectedGame.ShortName} — WorldExplorer";
        }
    }

    /// <summary>True when the loaded top-level archive has unsaved edits.</summary>
    public bool IsArchiveDirty => World?.WorldLmp is { } lmp && World.HasUnsavedChanges(lmp);

    /// <summary>
    /// Called after any edit or save: refreshes the title, the tree's change
    /// markers and the Overview page.
    /// </summary>
    public void NotifyIsArchiveDirty()
    {
        OnPropertyChanged(nameof(IsArchiveDirty));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(UnsavedSummary));
        OnPropertyChanged(nameof(WindowTitle));

        foreach (var root in RootNodes) root.RefreshState();

        if (ActiveView == ContentView.Overview && SelectedTreeNode != null)
        {
            Overview = BuildOverview(SelectedTreeNode);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Recent files
    // ─────────────────────────────────────────────────────────────────────────

    public ObservableCollection<RecentFile> RecentFiles { get; } = new();

    public bool HasRecentFiles => RecentFiles.Count > 0;

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var path in ReadRecentFilePaths())
        {
            RecentFiles.Add(new RecentFile(path));
        }
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    private static List<string> ReadRecentFilePaths()
    {
        var stored = App.Settings.Get("Files.RecentFiles", "") ?? "";
        // '|' can't appear in a Windows path; older versions used ','.
        var separator = stored.Contains('|') ? '|' : ',';
        return stored.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxRecentFiles)
            .ToList();
    }

    public void AddRecentFile(string file)
    {
        var list = ReadRecentFilePaths();
        list.RemoveAll(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, file);
        if (list.Count > MaxRecentFiles) list.RemoveRange(MaxRecentFiles, list.Count - MaxRecentFiles);

        App.Settings["Files.RecentFiles"] = string.Join("|", list);
        App.Settings["Files.LastLoadedFile"] = file;
        App.SaveSettings();
        LoadRecentFiles();
    }

    public void RemoveRecentFile(string file)
    {
        var list = ReadRecentFilePaths();
        list.RemoveAll(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase));
        App.Settings["Files.RecentFiles"] = string.Join("|", list);
        App.SaveSettings();
        LoadRecentFiles();
    }

    public void ClearRecentFiles()
    {
        App.Settings["Files.RecentFiles"] = "";
        App.SaveSettings();
        LoadRecentFiles();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Explorer filter
    // ─────────────────────────────────────────────────────────────────────────

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (!SetProperty(ref _filterText, value ?? "")) return;
            OnPropertyChanged(nameof(IsFiltering));
            // Debounced so typing stays smooth on large archives.
            _filterTimer.Stop();
            _filterTimer.Start();
        }
    }

    public bool IsFiltering => _filterText.Length > 0;

    private void ApplyFilter()
    {
        var filter = _filterText.Trim();
        foreach (var root in RootNodes)
        {
            root.ApplyFilter(filter.Length == 0 ? null : filter);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Opening / closing files
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens <paramref name="file"/>. Parsing runs on a worker thread so the
    /// window stays responsive (opening a BoS .DDF scans every CLP under the
    /// game's data folder). Returns false if the file couldn't be opened.
    /// </summary>
    public async Task<bool> LoadFileAsync(string file)
    {
        var name = Path.GetFileName(file);
        var folderPath = Path.GetDirectoryName(file) ?? Environment.CurrentDirectory;
        var world = new World(EngineVersion, folderPath, name);

        SetBusy($"Opening {name}…");
        var stopwatch = Stopwatch.StartNew();
        TreeViewItemViewModel root;
        try
        {
            await Task.Run(world.Load);
            root = WorldTreeViewModel.CreateRoot(world);
        }
        catch (Exception ex)
        {
            ClearBusy();
            StatusText = $"Couldn't open {name}";
            Notifications.Show($"Couldn't open {name}", DescribeLoadError(ex), ToastKind.Error);
            return false;
        }

        CloseFile();

        World = world;
        _gobFile = file;
        RootNodes.Add(root);
        root.IsExpanded = true;

        ClearBusy();
        StatusText = $"Opened {name} in {stopwatch.ElapsedMilliseconds:N0} ms";
        AddRecentFile(file);

        OnPropertyChanged(nameof(World));
        OnPropertyChanged(nameof(IsFileOpen));
        OnPropertyChanged(nameof(FilePath));
        NotifyIsArchiveDirty();

        root.IsSelected = true;
        return true;
    }

    private string DescribeLoadError(Exception ex) => ex switch
    {
        NotSupportedException => $"{ex.Message}. Supported files: .GOB, .LMP, .CLP, .DDF, .SDB, .YAK and .HDR.",
        FileNotFoundException fnf => $"File not found: {fnf.FileName ?? fnf.Message}",
        _ => $"{ex.Message}\n\nIs \"{_selectedGame.Name}\" the right game for this file? " +
             "Change it from the toolbar."
    };

    /// <summary>Closes the open file (unsaved edits are discarded — confirm first).</summary>
    public void CloseFile()
    {
        _filterTimer.Stop();
        _filterText = "";
        OnPropertyChanged(nameof(FilterText));
        OnPropertyChanged(nameof(IsFiltering));

        _selectedNode = null;
        _selectionVersion++;
        OnPropertyChanged(nameof(SelectedNode));
        OnPropertyChanged(nameof(SelectedTreeNode));

        _levelViewModel.SetWorld(null, null);
        _modelViewModel.AnimData = null;
        _modelViewModel.Texture = null;
        _modelViewModel.VifModel = null;
        _skeletonViewModel.AnimData = null;
        SelectedNodeImage = null;
        LogText = null;
        DetailsTitle = null;
        Overview = null;
        AvailableViews.Clear();
        _selectedView = null;
        OnPropertyChanged(nameof(SelectedView));
        OnPropertyChanged(nameof(ActiveView));

        RootNodes.Clear();
        World = null;
        _gobFile = null;

        OnPropertyChanged(nameof(World));
        OnPropertyChanged(nameof(IsFileOpen));
        OnPropertyChanged(nameof(FilePath));
        NotifyIsArchiveDirty();
    }

    /// <summary>The path of the open file, for reloading.</summary>
    public string? OpenFilePath => _gobFile;

    // ─────────────────────────────────────────────────────────────────────────
    // Selection dispatch
    // ─────────────────────────────────────────────────────────────────────────

    private void DispatchSelection(object? node)
    {
        switch (node)
        {
            case null:
                Overview = null;
                AvailableViews.Clear();
                _selectedView = null;
                OnPropertyChanged(nameof(SelectedView));
                OnPropertyChanged(nameof(ActiveView));
                break;
            case LmpEntryTreeViewModel lmpEntry:
                OnLmpEntrySelected(lmpEntry);
                break;
            case WorldFileTreeViewModel worldFile:
                _ = OnWorldEntrySelectedAsync(worldFile);
                break;
            case WorldElementTreeViewModel element:
                OnWorldElementSelected(element);
                break;
            case YakChildTreeViewItem yakChild:
                OnYakChildElementSelected(yakChild);
                break;
            case HdrDatChildTreeViewItem hdrChild:
                OnHdrDatChildElementSelected(hdrChild);
                break;
            case SdbTreeViewModel sdbNode:
                OnSdbSelected(sdbNode);
                break;
            case DdfEntityTreeViewModel entityNode:
                OnDdfEntitySelected(entityNode);
                break;
            case DdfMissingAssetTreeViewModel missing:
                ShowDetails(
                    $"{missing.Label}\n\nThis asset (hash 0x{missing.Asset.Hash:X8}, role {missing.Asset.Role}) " +
                    "lives in an archive that isn't loaded. Open the archive that contains it to inspect it.",
                    "Missing asset");
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            case TreeViewItemViewModel container:
                Overview = BuildOverview(container);
                ShowViews(ContentView.Overview, ContentView.Overview);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Overview
    // ─────────────────────────────────────────────────────────────────────────

    private OverviewModel BuildOverview(TreeViewItemViewModel node)
    {
        var overview = new OverviewModel(node.Label, node.KindDescription, node.IconKey, node.IconBrushKey);

        switch (node)
        {
            case GobTreeViewModel when World?.WorldGob is { } gob:
            {
                overview.Facts.Add(new("Game", _selectedGame.Name));
                overview.Facts.Add(new("Location", World.FilePath));
                overview.Facts.Add(new("File size", FileSizeOnDisk(World.FilePath)));
                overview.Facts.Add(new("Archives", gob.Directory.Count.ToString("N0")));

                var entries = 0;
                var rows = new List<(string Name, long Size)>();
                foreach (var lmp in gob.Directory.Values)
                {
                    if (lmp.Directory.Count == 0) lmp.ReadDirectory();
                    entries += lmp.Directory.Count;
                    rows.AddRange(lmp.Directory.Select(e => (e.Key, (long)e.Value.Length)));
                }

                overview.Facts.Add(new("Entries", entries.ToString("N0")));
                AddBreakdown(overview, rows);
                foreach (var lmp in gob.Directory.Values) AddPendingChanges(overview, lmp, prefixArchive: true);
                overview.Facts.Add(new("Level textures", World.WorldTex != null
                    ? $"{World.WorldTex.FileName} (companion .TEX)"
                    : $"Not found — put {CompanionTexName(World)} next to the GOB"));
                overview.Note = "Expand an archive to browse its contents. Level files (.world) open in the " +
                                "Level editor; Ctrl+click objects and elements there to select them.";
                break;
            }
            case LmpTreeViewModel lmpNode:
            {
                var lmp = lmpNode.LmpFileProperty;
                if (lmp.Directory.Count == 0) lmp.ReadDirectory();
                overview.Facts.Add(new("Entries", lmp.Directory.Count.ToString("N0")));
                overview.Facts.Add(new("Size",
                    FileSizeConverter.Format(lmp.Directory.Values.Sum(e => (long)e.Length))));
                if (lmpNode.Parent != null) overview.Facts.Add(new("Contained in", lmpNode.Location));
                else if (World != null) overview.Facts.Add(new("Location", World.FilePath));
                if (lmp is ClpFile) overview.Note = "CLP archives are hash-indexed and can be browsed but not edited.";
                AddBreakdown(overview, lmp.Directory.Select(e => (e.Key, (long)e.Value.Length)));
                AddPendingChanges(overview, lmp, prefixArchive: false);
                break;
            }
            case WorldTreeViewModel when World?.WorldDdf is { } ddf:
            {
                overview.Facts.Add(new("Game", _selectedGame.Name));
                overview.Facts.Add(new("Location", World.FilePath));
                overview.Facts.Add(new("Entities", ddf.Entities.Count.ToString("N0")));
                overview.Facts.Add(new("Archives indexed", World.LoadedClps.Count.ToString("N0")));
                overview.Facts.Add(new("Assets resolved", World.AssetIndex.Count.ToString("N0")));
                overview.Note = "Entities are grouped by category. Selecting one previews its meshes and " +
                                "shows its record in Details.";
                break;
            }
            case DdfCategoryTreeViewModel category:
                overview.Facts.Add(new("Category code", category.CategoryCode.ToString()));
                overview.Facts.Add(new("Entities", category.Count.ToString("N0")));
                break;
            default:
            {
                if (node.HasDummyChild) node.ForceLoadChildren();
                overview.Facts.Add(new("Items", node.Children.Count.ToString("N0")));
                if (node.Parent != null) overview.Facts.Add(new("Contained in", node.Location));
                break;
            }
        }

        overview.Actions.AddRange(MainWindow.CreateOverviewActions(node));
        return overview;
    }

    private static string FileSizeOnDisk(string path)
    {
        try
        {
            return FileSizeConverter.Format(new FileInfo(path).Length);
        }
        catch (Exception)
        {
            return "—";
        }
    }

    private static void AddBreakdown(OverviewModel overview, IEnumerable<(string Name, long Size)> entries)
    {
        var groups = entries
            .GroupBy(e => NodeKinds.FromFileName(e.Name))
            .Select(g => (Kind: g.Key, Count: g.Count(), Size: g.Sum(e => e.Size)))
            .OrderByDescending(g => g.Count);

        foreach (var (kind, count, size) in groups)
        {
            overview.Breakdown.Add(new OverviewBreakdownRow(NodeKinds.IconKey(kind), NodeKinds.BrushKey(kind),
                NodeKinds.Describe(kind), count, FileSizeConverter.Format(size)));
        }
    }

    private void AddPendingChanges(OverviewModel overview, LmpFile lmp, bool prefixArchive)
    {
        if (World == null || !World.HasUnsavedChanges(lmp)) return;
        var prefix = prefixArchive ? lmp.Name + " › " : "";

        foreach (var name in lmp.PendingEdits.Keys.Concat(lmp.PendingDeletions)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Where(n => World.IsEntryUnsaved(lmp, n)))
        {
            var verb = lmp.PendingDeletions.Contains(name) ? "Deleted" : "Modified";
            overview.PendingChanges.Add($"{verb}   {prefix}{name}");
        }

        foreach (var (name, _) in lmp.PendingAdditions)
        {
            overview.PendingChanges.Add($"Added   {prefix}{name}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Entry bytes (honouring pending edits)
    // ─────────────────────────────────────────────────────────────────────────

    private readonly record struct EntryData(byte[] Data, int Offset, int Length)
    {
        public ReadOnlySpan<byte> Span => Data.AsSpan(Offset, Length);
    }

    /// <summary>
    /// The entry's current bytes: an imported texture or edited script shows
    /// what will be saved rather than the original file content.
    /// </summary>
    private static EntryData GetEntryData(LmpFile lmp, string label, LmpFile.EntryInfo entry)
    {
        if (lmp.PendingEdits.TryGetValue(label, out var pending)) return new EntryData(pending, 0, pending.Length);
        return new EntryData(lmp.FileData, entry.StartOffset, entry.Length);
    }

    private static EntryData GetEntryData(LmpFile lmp, LmpFile.EntryInfo entry)
    {
        if (lmp.PendingEdits.Count > 0)
        {
            foreach (var (label, info) in lmp.Directory)
            {
                if (ReferenceEquals(info, entry)) return GetEntryData(lmp, label, entry);
            }
        }
        return new EntryData(lmp.FileData, entry.StartOffset, entry.Length);
    }

    /// <summary>Decodes a texture and freezes it (shareable, and its material can be cached).</summary>
    private static WriteableBitmap? DecodeTexture(ReadOnlySpan<byte> data)
    {
        var bitmap = TexDecoder.Decode(data);
        if (bitmap is { IsFrozen: false, CanFreeze: true }) bitmap.Freeze();
        return bitmap;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DDF entities
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selecting a DDF entity:
    ///   - Renders all of its Mesh assets in the model viewport (each mesh
    ///     keeps its DDF-paired texture). Multi-mesh entities — most commonly
    ///     cat-0 characters with separate body and hair — show every part
    ///     instead of the first one only.
    ///   - Writes a structured record dump to the Details view so parameter-only
    ///     categories (cat 5 emitters, cat 9 AI, cat 10 lights, cat 11 beam
    ///     effects) and the per-entity floats on asset-bearing categories are
    ///     legible.
    ///   - Clears the viewport when nothing renderable is available, so the
    ///     previous entity's model doesn't linger.
    /// </summary>
    private void OnDdfEntitySelected(DdfEntityTreeViewModel entityNode, bool showViews = true)
    {
        if (World == null) return;
        var entity = entityNode.Entity;

        // Cat-8 (level/container) entities point at a per-level world-layout
        // file via their Other-role asset (type-5 slot). When we can find
        // those bytes in a loaded CLP, decode them with the BoS world decoder
        // and route to the Level view.
        if (entity.CategoryCode == 8 && showViews && TryRenderCat8World(entity))
        {
            return;
        }

        RenderEntityMeshes(entity, showViews);
    }

    private void RenderEntityMeshes(DdfFile.EntityRecord entity, bool showViews, string? extraLog = null)
    {
        if (World == null) return;

        // Collect every Mesh asset that resolves to a loaded CLP entry.
        // Cat 12 (debris) records list 4 interchangeable mesh variants; render
        // only the first to keep the view legible — the parser already pairs
        // every cat-12 mesh to the same texture, so picking any one looks the
        // same as picking the canonical first variant.
        var meshParts = new List<(Model vif, BitmapSource? texture)>();
        var renderedLabels = new List<string>();
        var firstMeshOnly = entity.CategoryCode == 12;
        var decodeErrors = new List<string>();
        foreach (var asset in entity.Assets)
        {
            if (asset.Role != DdfFile.AssetRole.Mesh) continue;
            if (!World.AssetIndex.TryGetValue(asset.Hash, out var loc)) continue;
            var clp = loc.Clp;
            if (!clp.Directory.TryGetValue(loc.EntryLabel, out var meshEntry)) continue;

            // Wrap per-asset decode so a single bad asset (e.g. a format the
            // current decoder doesn't recognize) lands in the log instead of
            // aborting the rest of the entity's meshes.
            try
            {
                BitmapSource? texture = null;
                // Same mesh hash can appear in multiple entities with
                // different paired textures (cyrus skins, bottle-cap
                // variants, etc.), so prefer the per-entity pairing the
                // DDF parser already recorded. The parser stores it in two
                // directions depending on category: cat-12 (debris) puts
                // the shared-texture hash on the mesh asset; other
                // categories put the mesh hash on the texture asset.
                LmpFile.EntryInfo? pairedTex = null;
                LmpFile pairedTexClp = clp;
                uint? entityPairedTexHash = asset.PairedHash;
                if (entityPairedTexHash == null)
                {
                    foreach (var other in entity.Assets)
                    {
                        if (other.Role != DdfFile.AssetRole.Texture) continue;
                        if (other.PairedHash != asset.Hash) continue;
                        entityPairedTexHash = other.Hash;
                        break;
                    }
                }
                if (entityPairedTexHash is uint pairedHash
                    && World.AssetIndex.TryGetValue(pairedHash, out var texLoc)
                    && texLoc.Clp.Directory.TryGetValue(texLoc.EntryLabel, out var texEntry))
                {
                    pairedTex = texEntry;
                    pairedTexClp = texLoc.Clp;
                }
                pairedTex ??= FindSiblingTex(clp, loc.EntryLabel);
                if (pairedTex != null)
                {
                    try
                    {
                        texture = DecodeTexture(GetEntryData(pairedTexClp, pairedTex).Span);
                    }
                    catch (Exception ex)
                    {
                        decodeErrors.Add($"  TEX decode of paired texture for {loc.EntryLabel} failed: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                var uvW = texture?.PixelWidth ?? 256;
                var uvH = texture?.PixelHeight ?? 256;
                var vif = new Model(VifDecoder.Decode(
                    new StringLogger(),
                    GetEntryData(clp, loc.EntryLabel, meshEntry).Span,
                    uvW, uvH));

                meshParts.Add((vif, texture));
                renderedLabels.Add(loc.EntryLabel);
                if (firstMeshOnly) break;
            }
            catch (Exception ex)
            {
                decodeErrors.Add($"  VIF decode of {loc.EntryLabel} failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var dump = DumpEntityRecord(entity);
        if (decodeErrors.Count > 0)
        {
            dump += "\nDecode errors:\n" + string.Join("\n", decodeErrors);
        }
        if (extraLog != null)
        {
            dump += "\n" + extraLog;
        }
        LogText = dump;
        DetailsTitle = "Entity record";

        if (meshParts.Count > 0)
        {
            // Show the first part's texture in the texture view as a hint.
            SelectedNodeImage = meshParts[0].texture as WriteableBitmap;
            _modelViewModel.SetCompositeModel(meshParts);
            MainWindow.SetViewportText(ContentView.Model, entity.Name,
                meshParts.Count > 1 ? $"{meshParts.Count} meshes: {string.Join(", ", renderedLabels)}" : "");
            RequestCameraReset(ContentView.Model);

            if (showViews)
            {
                var views = new List<ContentView> { ContentView.Model, ContentView.Details };
                if (SelectedNodeImage != null) views.Add(ContentView.Texture);
                ShowViews(ContentView.Model, views.ToArray());
            }
        }
        else
        {
            SelectedNodeImage = null;
            _modelViewModel.Texture = null;
            _modelViewModel.AnimData = null;
            _modelViewModel.VifModel = null;
            MainWindow.SetViewportText(ContentView.Model, entity.Name + " (no model)", "");
            if (showViews) ShowViews(ContentView.Details, ContentView.Details);
        }
    }

    /// <summary>
    /// Look up the BoS level texture atlas. Tries two strategies:
    ///   1. Hash "&lt;entity_name&gt;.tex" directly — covers BAR, T_GAZ, GARDEN,
    ///      etc. where the CLP and asset names match.
    ///   2. Fallback: locate the sibling "&lt;entity&gt;_T.CLP" archive and pick
    ///      its largest entry — covers cases where the CLP name is a short
    ///      code but the asset is the full word (WARE_1 → warehouse_1.tex,
    ///      TUTOR → tutorial.tex). The .tex is always the biggest of the 2–3
    ///      entries in a _T.CLP (they hold .tex / .hsh / .vat where .hsh is
    ///      always tiny and .vat is medium).
    /// </summary>
    private static WorldTexFile? LoadBosLevelTex(World world, string entityName)
    {
        var lowerName = entityName.Trim().ToLowerInvariant();

        var hash = BosNameTable.Hash(lowerName + ".tex");
        if (world.AssetIndex.TryGetValue(hash, out var loc)
            && loc.Clp.Directory.TryGetValue(loc.EntryLabel, out var entry))
        {
            var bytes = new byte[entry.Length];
            Buffer.BlockCopy(loc.Clp.FileData, entry.StartOffset, bytes, 0, entry.Length);
            return new WorldTexFile(world.EngineVersion, bytes, lowerName + ".tex");
        }

        var siblingName = entityName.Trim().ToUpperInvariant() + "_T.CLP";
        foreach (var clp in world.LoadedClps)
        {
            if (!string.Equals(clp.Name, siblingName, StringComparison.OrdinalIgnoreCase)) continue;
            // The .tex is always the biggest entry in a _T.CLP.
            string? bestKey = null;
            var bestSize = 0;
            foreach (var (key, info) in clp.Directory)
            {
                if (info.Length > bestSize) { bestSize = info.Length; bestKey = key; }
            }
            if (bestKey != null)
            {
                var info = clp.Directory[bestKey];
                var bytes = new byte[info.Length];
                Buffer.BlockCopy(clp.FileData, info.StartOffset, bytes, 0, info.Length);
                return new WorldTexFile(world.EngineVersion, bytes, bestKey);
            }
        }
        return null;
    }

    /// <summary>
    /// Decode a cat-8 entity's type-5 (Other-role) asset as a BoS world file
    /// and surface it in the Level view. Returns false if the entity has no
    /// usable Other reference — in which case the caller falls through to the
    /// regular mesh/log path.
    /// </summary>
    private bool TryRenderCat8World(DdfFile.EntityRecord entity)
    {
        if (World == null) return false;
        // The world-layout slot is the cat-8 type-5 reference. We attributed
        // it as AssetRole.Other in the DDF parser. There can be multiple
        // Other-role assets (types 5/6/7), so try each in turn.
        foreach (var asset in entity.Assets)
        {
            if (asset.Role != DdfFile.AssetRole.Other) continue;
            if (!World.AssetIndex.TryGetValue(asset.Hash, out var loc)) continue;
            if (!loc.Clp.Directory.TryGetValue(loc.EntryLabel, out var entry)) continue;

            var data = loc.Clp.FileData.AsSpan(entry.StartOffset, entry.Length);
            // Quick header sanity-check before paying the parse cost: a
            // real WorldFileHeader has a small NumberOfElements at +0
            // and an ElementArrayStart at +0x24 inside the file.
            if (data.Length < 0x68) continue;
            var ne = BitConverter.ToInt32(data.Slice(0, 4));
            var eas = BitConverter.ToInt32(data.Slice(0x24, 4));
            var wtoo = BitConverter.ToInt32(data.Slice(0x64, 4));
            if (ne <= 0 || ne > 8000 || eas < 100 || eas >= data.Length) continue;
            if (wtoo < 100 || wtoo >= data.Length) continue;

            // Derive the level texture atlas from the *archive* the world
            // file lives in (e.g. LAB_1B.CLP → lab_1b.tex / LAB_1B_T.CLP),
            // NOT from the entity SDB name. With our recursive SDB scan,
            // an entity like "Vault Lab 1, mutated" (which lives in
            // GTEXT.SDB) wins the SDB lookup race over the level ID
            // "LAB_1b" (in LAB_1B.SDB / GLOBAL.SDB), and the localized
            // display name doesn't match any CLP archive name. Falling
            // back to the CLP filename matches what the .world dispatch
            // does and finds the right atlas.
            var levelKey = Path.GetFileNameWithoutExtension(loc.Clp.Name);
            var bytes = data.ToArray();
            LogText = DumpEntityRecord(entity);
            DetailsTitle = "Entity record";
            _ = ShowPreviewWorldAsync(entity.Name, levelKey, bytes,
                onFailure: ex => RenderEntityMeshes(entity, showViews: true,
                    $"Cat-8 world decode failed for asset 0x{asset.Hash:X8}: {ex.GetType().Name}: {ex.Message}"));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Build a human-readable text dump of a DDF entity record: header info,
    /// asset list with names where known, and the populated float / int slots
    /// in the record body. Used as the Details preview when an entity is
    /// selected so parameter-only records (emitters, AI, lights, beam effects)
    /// are legible and asset-bearing entities also expose their gameplay
    /// stats.
    /// </summary>
    private string DumpEntityRecord(DdfFile.EntityRecord entity)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("Entity: {0}\n", entity.Name);
        sb.AppendFormat("  SDB hash:      0x{0:X8}\n", entity.SdbHash);
        sb.AppendFormat("  Category:      {0}\n", entity.CategoryCode);
        sb.AppendFormat("  Record offset: 0x{0:X}\n", entity.RecordOffset);

        if (entity.Assets.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Assets:");
            foreach (var a in entity.Assets)
            {
                var name = BosNameTable.Get(a.Hash);
                var nameStr = name != null ? $"  '{name}'" : "";
                var pair = a.PairedHash.HasValue ? $"  paired→0x{a.PairedHash.Value:X8}" : "";
                sb.AppendFormat("  {0,-9} 0x{1:X8}{2}{3}\n", a.Role, a.Hash, nameStr, pair);
            }
        }

        var ddf = World?.WorldDdf;
        if (ddf != null && entity.RecordOffset + 0x14 <= ddf.FileData.Length)
        {
            var size = (int)BitConverter.ToUInt32(ddf.FileData, entity.RecordOffset + 0x10);
            size = Math.Min(size, ddf.FileData.Length - entity.RecordOffset);
            if (size > 0x14)
            {
                sb.AppendLine();
                sb.AppendFormat("Record body ({0} bytes total, showing non-zero u32/float pairs from +0x14):\n", size);
                for (var off = 0x14; off + 4 <= size; off += 4)
                {
                    var u = BitConverter.ToUInt32(ddf.FileData, entity.RecordOffset + off);
                    if (u == 0) continue;
                    var f = BitConverter.ToSingle(ddf.FileData, entity.RecordOffset + off);
                    var i = (int)u;
                    var floatStr = (Math.Abs(f) > 0.0001f && Math.Abs(f) < 1e10f) ? f.ToString("F4") : "—";
                    sb.AppendFormat("  +0x{0:X3}  u32=0x{1:X8} ({2,11})  float={3}\n", off, u, i, floatStr);
                }
            }
        }
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Archive entries
    // ─────────────────────────────────────────────────────────────────────────

    private void OnLmpEntrySelected(LmpEntryTreeViewModel lmpEntry)
    {
        var lmpFile = lmpEntry.LmpFileProperty;
        if (!lmpFile.Directory.TryGetValue(lmpEntry.Label, out var entry))
        {
            ShowDetails($"{lmpEntry.Label}\n\nThis entry is no longer in the archive directory.", "Missing entry");
            ShowViews(ContentView.Details, ContentView.Details);
            return;
        }

        var ext = (Path.GetExtension(lmpEntry.Label) ?? "").ToLower();

        // Selecting an .anm directly under a DDF entity should preview the
        // clip on the entity's composite mesh, not on whatever model happened
        // to be loaded last (a sibling sub-mesh, a different entity, or
        // nothing — in which case the .anm path would otherwise fall through
        // to the skeleton-only view). Rebuild the composite first so the
        // subsequent AnimData assignment lands on the right meshes.
        if (ext == ".anm" && lmpEntry.Parent is DdfEntityTreeViewModel entityNode)
        {
            OnDdfEntitySelected(entityNode, showViews: false);
        }

        var data = GetEntryData(lmpFile, lmpEntry.Label, entry);
        try
        {
            DispatchLmpEntry(lmpFile, data, lmpEntry, ext);
        }
        catch (Exception ex)
        {
            LogText = $"Failed to decode {lmpEntry.Label} as {ext}: {ex.GetType().Name}: {ex.Message}\n\n"
                      + HexDump(data.Data, data.Offset, Math.Min(data.Length, 256));
            DetailsTitle = "Decode failed";
            ShowViews(ContentView.Details, ContentView.Details);
        }
    }

    private static LmpFile.EntryInfo? FindSiblingTex(LmpFile lmp, string vifLabel)
    {
        // Strongest signal: ask the DDF for the specific texture hash that
        // shares this mesh's asset record. When an entity has several variants
        // (e.g. multiple Bottle Caps drops, each with its own mesh+texture),
        // each variant binds the right texture to the right mesh.
        if (lmp is ClpFile clp &&
            clp.TexturePairResolver != null &&
            clp.HashByLabel.TryGetValue(vifLabel, out var meshHash))
        {
            var paired = clp.TexturePairResolver(meshHash);
            if (paired.HasValue)
            {
                foreach (var (key, info) in lmp.Directory)
                {
                    if (!key.EndsWith(".tex")) continue;
                    if (clp.HashByLabel.TryGetValue(key, out var texHash) && texHash == paired.Value)
                    {
                        return info;
                    }
                }
            }
        }

        // Legacy BGDA naming convention — same basename, .tex extension.
        var legacy = Path.GetFileNameWithoutExtension(vifLabel) + ".tex";
        if (lmp.Directory.TryGetValue(legacy, out var byBasename)) return byBasename;

        // Last resort: any .tex with the same entity-name token.
        var vifEntity = ExtractEntityToken(vifLabel);
        if (vifEntity == null) return null;
        foreach (var (key, info) in lmp.Directory)
        {
            if (!key.EndsWith(".tex")) continue;
            if (ExtractEntityToken(key) == vifEntity) return info;
        }
        return null;
    }

    private static string? ExtractEntityToken(string label)
    {
        var dot = label.LastIndexOf('.');
        return dot > 0 ? label.Substring(0, dot) : label;
    }

    private void OnSdbSelected(SdbTreeViewModel node)
    {
        var sdb = node.SdbFile;
        var sb = new StringBuilder();
        sb.AppendFormat("{0}\nString database — {1} records.\n", sdb.Name, sdb.Records.Count);
        sb.AppendLine();
        sb.AppendLine("  slot   stored hash   string");
        sb.AppendLine("  -----  -----------   ------------------------------------------------");
        foreach (var rec in sdb.Records)
        {
            var preview = rec.Text ?? "<missing string>";
            if (preview.Length > 96) preview = preview.Substring(0, 93) + "...";
            preview = preview.Replace('\r', ' ').Replace('\n', ' ');
            sb.AppendFormat("  {0,5}  0x{1:X8}    {2}\n", rec.Slot, rec.Hash, preview);
        }
        LogText = sb.ToString();
        DetailsTitle = "String table";
        ShowViews(ContentView.Details, ContentView.Details);
    }

    private static string DescribeVag(string label, byte[] data, int offset, int length)
    {
        var sb = new StringBuilder();
        sb.AppendLine(label);
        if (length < 0x40)
        {
            sb.AppendLine("VAG file is too short to read header.");
            return sb.ToString();
        }
        // VAG header is big-endian.
        var version = (data[offset + 4] << 24) | (data[offset + 5] << 16) | (data[offset + 6] << 8) | data[offset + 7];
        var dataSize = (data[offset + 0xC] << 24) | (data[offset + 0xD] << 16) | (data[offset + 0xE] << 8) | data[offset + 0xF];
        var sampleRate = (data[offset + 0x10] << 24) | (data[offset + 0x11] << 16) | (data[offset + 0x12] << 8) | data[offset + 0x13];
        var name = new StringBuilder();
        for (var i = 0; i < 16 && data[offset + 0x20 + i] != 0; i++) name.Append((char)data[offset + 0x20 + i]);
        sb.AppendFormat("VAG (PS2 ADPCM audio)\n");
        sb.AppendFormat("  Internal name: {0}\n", name);
        sb.AppendFormat("  Version:       0x{0:X8}\n", version);
        sb.AppendFormat("  Sample rate:   {0} Hz\n", sampleRate);
        sb.AppendFormat("  Data size:     {0} bytes ({1:F1} KB)\n", dataSize, dataSize / 1024.0);
        sb.AppendFormat("  File size:     {0} bytes\n", length);
        return sb.ToString();
    }

    private static string DescribeNameTable(string label, byte[] data, int offset, int length)
    {
        var sb = new StringBuilder();
        sb.AppendLine(label);
        sb.AppendFormat("32-byte name table, {0} records\n\n", length / 32);
        for (var i = 0; i < length; i += 32)
        {
            var name = new StringBuilder();
            for (var j = 0; j < 32 && i + j < length; j++)
            {
                var b = data[offset + i + j];
                if (b == 0) break;
                name.Append(b >= 0x20 && b < 0x7f ? (char)b : '.');
            }
            if (name.Length == 0) continue;
            sb.AppendFormat("  {0:D4}: {1}\n", i / 32, name);
        }
        return sb.ToString();
    }

    private static string HexDump(byte[] data, int offset, int length)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < length; i += 16)
        {
            sb.AppendFormat("{0:X8}: ", offset + i);
            for (var j = 0; j < 16 && i + j < length; j++)
            {
                sb.AppendFormat("{0:X2} ", data[offset + i + j]);
            }
            sb.Append(' ');
            for (var j = 0; j < 16 && i + j < length; j++)
            {
                var b = data[offset + i + j];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private void DispatchLmpEntry(LmpFile lmpFile, EntryData data, LmpEntryTreeViewModel lmpEntry, string ext)
    {
        switch (ext)
        {
            case ".tex":
            case ".etex":
            {
                SelectedNodeImage = DecodeTexture(data.Span);
                if (SelectedNodeImage == null)
                {
                    throw new InvalidDataException("The texture decoder didn't recognise this data.");
                }
                ShowViews(ContentView.Texture, ContentView.Texture);
                break;
            }
            case ".vif":
            {
                var pairedTex = FindSiblingTex(lmpFile, lmpEntry.Label);
                SelectedNodeImage = pairedTex != null
                    ? DecodeTexture(GetEntryData(lmpFile, pairedTex).Span)
                    : null;
                StringLogger log = new();
                _modelViewModel.Texture = SelectedNodeImage;
                _modelViewModel.AnimData = null;
                // VifDecoder divides UV coords by (textureDim * 16). With dim 0 we get
                // NaN UVs and WPF silently drops the geometry; fall back to 256 so the
                // Model viewer can paint with the checkerboard brush instead.
                var uvW = SelectedNodeImage?.PixelWidth ?? 256;
                var uvH = SelectedNodeImage?.PixelHeight ?? 256;
                Model model = new(VifDecoder.Decode(log, data.Span, uvW, uvH));
                _modelViewModel.VifModel = model;

                var decodeLog = log.ToString();
                LogText = decodeLog.Length > 0 ? decodeLog : null;
                DetailsTitle = "Decoder log";

                MainWindow.SetViewportText(ContentView.Model, lmpEntry.Label, "");
                RequestCameraReset(ContentView.Model);

                var views = new List<ContentView> { ContentView.Model };
                if (SelectedNodeImage != null) views.Add(ContentView.Texture);
                if (LogText != null) views.Add(ContentView.Details);
                ShowViews(ContentView.Model, views.ToArray());
                break;
            }
            case ".anm":
            {
                var animData = AnmDecoder.Decode(World?.EngineVersion ?? EngineVersion, data.Span);
                _skeletonViewModel.AnimData = animData;
                LogText = animData.ToString();
                DetailsTitle = "Animation data";

                var defaultView = ContentView.Skeleton;
                var views = new List<ContentView> { ContentView.Skeleton, ContentView.Details };
                if (_modelViewModel.VifModel != null)
                {
                    var boneCount = _modelViewModel.VifModel.CountBones();
                    // CountBones returns (max-skinned-bone-id + 1) — only bones
                    // that vertex weights reference. animData.NumBones counts
                    // the full skeleton, including unskinned helpers (root, IK,
                    // attach points). Requiring equality rejects legitimate
                    // skeletons with trailing unskinned bones (e.g. the baby
                    // deathclaw). The pose lookup in Conversions.CreateModel3D
                    // only needs every referenced bone id to be < NumBones.
                    if (boneCount != 0 && boneCount <= animData.NumBones)
                    {
                        _modelViewModel.AnimData = animData;
                        views.Add(ContentView.Model);

                        // Keep showing the model if that's what's on screen —
                        // previewing clips on the model is the common case.
                        if (ActiveView == ContentView.Model) defaultView = ContentView.Model;
                    }
                }

                MainWindow.SetViewportText(ContentView.Skeleton, lmpEntry.Label, "");
                RequestCameraReset(ContentView.Skeleton);
                ShowViews(defaultView, views.ToArray());
                break;
            }
            case ".ob":
            {
                var objects = ObDecoder.Decode(data.Data, data.Offset, data.Length);

                StringBuilder sb = new();
                foreach (var obj in objects)
                {
                    sb.AppendFormat("Name: {0}\n", obj.Name);
                    sb.AppendFormat("I6: {0}\n", obj.I6.ToString("X4"));
                    sb.AppendFormat("Floats: {0},{1},{2}\n", obj.Floats[0], obj.Floats[1], obj.Floats[2]);
                    foreach (var prop in obj.Properties)
                    {
                        sb.AppendFormat("Property: {0}\n", prop);
                    }

                    sb.Append('\n');
                }

                LogText = sb.ToString();
                DetailsTitle = $"Object list — {Plural.Of(objects.Count, "object")}";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            }
            case ".scr":
            {
                var script = ScrDecoder.Decode(data.Data, data.Offset, data.Length);
                LogText = script.Disassemble();
                DetailsTitle = "Script disassembly";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            }
            case ".cut":
            {
                var scene = CutDecoder.Decode(data.Data, data.Offset, data.Length);
                LogText = scene.Disassemble();
                DetailsTitle = "Cutscene";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            }
            case ".bin":
            {
                var dialog = DialogDecoder.Decode(data.Data, data.Offset, data.Length);
                StringBuilder sb = new();

                foreach (var obj in dialog)
                {
                    sb.AppendFormat("Name: {0}\n", obj.Name);
                    sb.AppendFormat("Start offset in VA File: 0x{0:x}\n", obj.StartOffsetInVAFile);
                    sb.AppendFormat("Length: 0x{0:x}\n", obj.Length);
                    sb.Append('\n');
                }

                LogText = sb.ToString();
                DetailsTitle = "Dialog table";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            }
            case ".vag":
                LogText = DescribeVag(lmpEntry.Label, data.Data, data.Offset, data.Length);
                DetailsTitle = "Audio";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            case ".names":
                LogText = DescribeNameTable(lmpEntry.Label, data.Data, data.Offset, data.Length);
                DetailsTitle = "Name table";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            case ".adpcm":
                LogText = $"{lmpEntry.Label}\nRaw PS2 ADPCM stream (header-less VAG body).\n  {data.Length} bytes = {data.Length / 16} frames = {data.Length * 28 / 16} samples\n\n"
                          + HexDump(data.Data, data.Offset, Math.Min(data.Length, 128));
                DetailsTitle = "Audio";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
            case ".world":
            {
                // A .world reached through a DDF entity (BoS): read-only preview.
                // Derive the texture atlas from the parent CLP's name. The
                // user typically opens e.g. "BAR.CLP" — the matching atlas is
                // "bar.tex" living in BAR_T.CLP, hashable via BosNameTable.
                var levelName = Path.GetFileNameWithoutExtension(lmpFile.Name);
                _ = ShowPreviewWorldAsync(lmpEntry.Label, levelName, data.Span.ToArray(),
                    onFailure: ex => ShowFailure($"Couldn't decode {lmpEntry.Label}", ex, replaceViews: true));
                break;
            }
            default:
                LogText = $"{lmpEntry.Label}\nNo decoder for extension '{ext}'. First {Math.Min(data.Length, 256)} bytes:\n\n"
                          + HexDump(data.Data, data.Offset, Math.Min(data.Length, 256));
                DetailsTitle = "Hex dump";
                ShowViews(ContentView.Details, ContentView.Details);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Levels
    // ─────────────────────────────────────────────────────────────────────────

    private sealed record DecodedWorld(WorldData Data, ElementGeometryCache Geometry, string Summary);

    /// <summary>
    /// Decodes a level and builds its element geometry on a worker thread.
    /// Everything handed back is frozen, so the UI thread can use it directly.
    /// </summary>
    private async Task<DecodedWorld> DecodeWorldAsync(World world, string levelName, byte[] bytes)
    {
        await _decodeGate.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                WorldFileDecoder decoder = world.EngineVersion switch
                {
                    EngineVersion.ReturnToArms or EngineVersion.JusticeLeagueHeroes => new WorldFileV2Decoder(),
                    // BoS stores 80-byte element records (BGDA: 56), so it needs its own decoder.
                    EngineVersion.BrotherhoodOfSteel => new WorldFileV1BoSDecoder(),
                    _ => new WorldFileV1Decoder()
                };

                var texFile = world.WorldTex;
                if (world.EngineVersion == EngineVersion.BrotherhoodOfSteel)
                {
                    texFile = LoadBosLevelTex(world, levelName) ?? texFile;
                }

                var worldData = decoder.Decode(bytes, texFile);

                // Textures and mesh lists (Point3DCollection & co.) were created
                // on this worker thread: freeze them so the UI thread may use them
                // (and so texture materials can be cached).
                texFile?.FreezeCachedBitmaps();
                foreach (var element in worldData.WorldElements)
                {
                    if (element.Texture is { IsFrozen: false, CanFreeze: true } texture) texture.Freeze();
                    FreezeMeshes(element.Model);
                }

                var geometry = ElementGeometryCache.BuildFor(worldData);
                return new DecodedWorld(worldData, geometry, worldData.ToString() ?? "");
            });
        }
        finally
        {
            _decodeGate.Release();
        }
    }

    private static void FreezeMeshes(Model? model)
    {
        if (model == null) return;
        foreach (var mesh in model.MeshList)
        {
            Freeze(mesh.Positions);
            Freeze(mesh.Normals);
            Freeze(mesh.TextureCoordinates);
            Freeze(mesh.TriangleIndices);
        }

        static void Freeze(object list)
        {
            if (list is System.Windows.Freezable { IsFrozen: false, CanFreeze: true } freezable) freezable.Freeze();
        }
    }

    private async Task OnWorldEntrySelectedAsync(WorldFileTreeViewModel node)
    {
        var world = World;
        if (world == null) return;

        // Already decoded this session: show it as-is. Re-decoding the original
        // bytes would discard in-memory edits and the undo history.
        if (node.WorldData != null)
        {
            ShowWorld(node, null, null);
            return;
        }

        if (!_decoding.Add(node)) return;   // a decode for this node is in flight

        var version = _selectionVersion;
        var lmpFile = node.LmpFileProperty;
        if (!lmpFile.Directory.TryGetValue(node.Label, out var entry))
        {
            _decoding.Remove(node);
            return;
        }

        // Decode the archive's original bytes: the level editor's commit path
        // (WorldElementPatcher) patches relative to them.
        var bytes = new byte[entry.Length];
        Buffer.BlockCopy(lmpFile.FileData, entry.StartOffset, bytes, 0, entry.Length);

        // Show an empty Level view under the progress overlay rather than the
        // previous item's content.
        _levelViewModel.SetWorld(null, null);
        ShowViews(ContentView.Level, ContentView.Level);
        SetBusy($"Decoding {node.Label}…");
        var stopwatch = Stopwatch.StartNew();
        DecodedWorld decoded;
        try
        {
            decoded = await DecodeWorldAsync(world, Path.GetFileNameWithoutExtension(lmpFile.Name), bytes);
        }
        catch (Exception ex)
        {
            ClearBusy();
            if (version == _selectionVersion)
            {
                ShowFailure($"Couldn't decode {node.Label}", ex, replaceViews: true);
            }
            return;
        }
        finally
        {
            _decoding.Remove(node);
        }

        ClearBusy();
        if (!ReferenceEquals(World, world)) return;   // file closed meanwhile

        node.WorldData = decoded.Data;
        node.ReloadChildren();
        StatusText = $"Decoded {node.Label} — {Plural.Of(decoded.Data.WorldElements.Count, "element")} in {stopwatch.ElapsedMilliseconds:N0} ms";

        // The user may have moved on while this ran; the level stays cached on
        // the node and appears instantly when they come back.
        if (!ReferenceEquals(_selectedNode, node) || version != _selectionVersion)
        {
            _pendingGeometry[node] = decoded.Geometry;
            return;
        }

        ShowWorld(node, decoded.Geometry, decoded.Summary);
    }

    // Geometry built for a level that finished decoding after the user moved on.
    private readonly Dictionary<WorldFileTreeViewModel, ElementGeometryCache> _pendingGeometry = new();

    private void ShowWorld(WorldFileTreeViewModel node, ElementGeometryCache? geometry, string? summary)
    {
        if (World == null || node.WorldData == null) return;

        if (geometry == null && _pendingGeometry.Remove(node, out var pending))
        {
            geometry = pending;
        }

        var alreadyShown = ReferenceEquals(_levelViewModel.WorldNode, node) &&
                           ReferenceEquals(_levelViewModel.WorldData, node.WorldData);
        if (!alreadyShown)
        {
            World.WorldData = node.WorldData;
            _levelViewModel.SetWorld(node, node.WorldData, geometry);
            RequestCameraReset(ContentView.Level);
        }

        WarnIfLevelTexturesMissing();
        LogText = summary ?? node.WorldData.ToString();
        DetailsTitle = "Level data";
        MainWindow.SetViewportText(ContentView.Level, node.Label,
            Plural.Of(node.WorldData.WorldElements.Count(e => !e.IsDeleted), "element"));
        ShowViews(ContentView.Level, ContentView.Level, ContentView.Details);
    }

    private static string CompanionTexName(World world) => Path.GetFileNameWithoutExtension(world.Name) + ".TEX";

    // The file we last warned about, so the warning shows once per opened file.
    private World? _missingTexturesWarned;

    /// <summary>
    /// A GOB's level textures live in the .TEX file beside it. Without it every
    /// surface gets the placeholder pattern, which looks like a bug unless said.
    /// </summary>
    private void WarnIfLevelTexturesMissing()
    {
        var world = World;
        if (world?.WorldGob == null || world.WorldTex != null || ReferenceEquals(_missingTexturesWarned, world)) return;

        _missingTexturesWarned = world;
        Notifications.Warning("Level textures not found",
            $"{CompanionTexName(world)} isn't next to {world.Name}, so surfaces use a placeholder pattern.");
    }

    /// <summary>Decodes and shows a level that can't be edited (BoS DDF / cat-8 entity).</summary>
    private async Task ShowPreviewWorldAsync(string title, string levelName, byte[] bytes,
        Action<Exception> onFailure)
    {
        var world = World;
        if (world == null) return;
        var version = _selectionVersion;

        _levelViewModel.SetWorld(null, null);
        ShowViews(ContentView.Level, ContentView.Level, ContentView.Details);
        SetBusy($"Decoding {title}…");
        DecodedWorld decoded;
        try
        {
            decoded = await DecodeWorldAsync(world, levelName, bytes);
        }
        catch (Exception ex)
        {
            ClearBusy();
            if (version == _selectionVersion) onFailure(ex);
            return;
        }

        ClearBusy();
        if (version != _selectionVersion || !ReferenceEquals(World, world)) return;

        world.WorldData = decoded.Data;
        _levelViewModel.SetWorld(null, decoded.Data, decoded.Geometry);

        // Clear the model viewport so a previously-selected character mesh
        // doesn't overlap visually.
        _modelViewModel.VifModel = null;
        _modelViewModel.Texture = null;
        _modelViewModel.AnimData = null;
        SelectedNodeImage = null;

        LogText = string.IsNullOrEmpty(LogText) ? decoded.Summary : LogText + "\n" + decoded.Summary;
        DetailsTitle ??= "Level data";
        MainWindow.SetViewportText(ContentView.Level, title, Plural.Of(decoded.Data.WorldElements.Count, "element"));
        RequestCameraReset(ContentView.Level);
        ShowViews(ContentView.Level, ContentView.Level, ContentView.Details);
    }

    private void OnWorldElementSelected(WorldElementTreeViewModel worldElementModel)
    {
        // Make sure the Level view is showing this element's level (another
        // level may be on screen if several are expanded in the tree).
        if (worldElementModel.Parent is WorldFileTreeViewModel worldNode &&
            worldNode.WorldData != null &&
            !ReferenceEquals(_levelViewModel.WorldNode, worldNode))
        {
            World!.WorldData = worldNode.WorldData;
            _levelViewModel.SetWorld(worldNode, worldNode.WorldData, null);
            MainWindow.SetViewportText(ContentView.Level, worldNode.Label,
                Plural.Of(worldNode.WorldData.WorldElements.Count(e => !e.IsDeleted), "element"));
            RequestCameraReset(ContentView.Level);
        }

        // Keep the element's model available in the Model view (switch to the
        // Model view to inspect it in isolation).
        SelectedNodeImage = worldElementModel.WorldElement.Texture;
        _modelViewModel.Texture = SelectedNodeImage;
        _modelViewModel.AnimData = null;
        _modelViewModel.VifModel = worldElementModel.WorldElement.Model;
        MainWindow.SetViewportText(ContentView.Model, worldElementModel.Label, "");
        RequestCameraReset(ContentView.Model);

        // Drive the Level editor: selecting the element here makes its move +
        // rotate gizmos and the properties panel follow the tree (LevelView
        // observes these two properties). Clearing SelectedObject keeps the
        // selection unambiguous (object-vs-element).
        _levelViewModel.SelectedObject = null;
        _levelViewModel.SelectedElement = worldElementModel;

        // Show the Level view so the gizmo is on screen. We deliberately do NOT
        // reset its camera here — that would throw away the user's current
        // viewpoint on every tree click.
        MainWindow.SetViewportText(ContentView.Level, worldElementModel.Label, "");
        var views = new List<ContentView> { ContentView.Level };
        if (_modelViewModel.VifModel != null) views.Add(ContentView.Model);
        if (SelectedNodeImage != null) views.Add(ContentView.Texture);
        ShowViews(ContentView.Level, views.ToArray());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // YAK / HDR-DAT children
    // ─────────────────────────────────────────────────────────────────────────

    private void OnYakChildElementSelected(YakChildTreeViewItem childEntry)
    {
        if (childEntry.Value == null) return;
        SelectedNodeImage = DecodeTexture(childEntry.YakFile.FileData.AsSpan()[(childEntry.Value.TextureOffset + childEntry.Value.VifOffset)..]);
        StringLogger log = new();
        _modelViewModel.Texture = SelectedNodeImage;
        _modelViewModel.AnimData = null;
        Model model = new(VifDecoder.Decode(
            log,
            childEntry.YakFile.FileData.AsSpan()
                .Slice(childEntry.Value.VifOffset, childEntry.Value.TextureOffset),
            SelectedNodeImage?.PixelWidth ?? 0,
            SelectedNodeImage?.PixelHeight ?? 0));
        _modelViewModel.VifModel = model;

        ShowModelWithLog(log, childEntry.Label + " of " + (childEntry.Parent as YakTreeViewItem)?.Label);
    }

    private void OnHdrDatChildElementSelected(HdrDatChildTreeViewItem childEntry)
    {
        SelectedNodeImage = DecodeTexture(childEntry.CacheFile.FileData.AsSpan()[childEntry.Value.TexOffset..]);
        StringLogger log = new();
        _modelViewModel.Texture = SelectedNodeImage;
        _modelViewModel.AnimData = null;
        Model model = new(VifDecoder.Decode(
            log,
            childEntry.CacheFile.FileData.AsSpan()
                .Slice(childEntry.Value.VifOffset, childEntry.Value.VifLength),
            SelectedNodeImage?.PixelWidth ?? 0,
            SelectedNodeImage?.PixelHeight ?? 0));
        _modelViewModel.VifModel = model;

        ShowModelWithLog(log, childEntry.Label + " of " + (childEntry.Parent as HdrDatTreeViewItem)?.Label);
    }

    private void ShowModelWithLog(StringLogger log, string title)
    {
        var decodeLog = log.ToString();
        LogText = decodeLog.Length > 0 ? decodeLog : null;
        DetailsTitle = "Decoder log";

        MainWindow.SetViewportText(ContentView.Model, title, "");
        RequestCameraReset(ContentView.Model);

        var views = new List<ContentView> { ContentView.Model };
        if (SelectedNodeImage != null) views.Add(ContentView.Texture);
        if (LogText != null) views.Add(ContentView.Details);
        ShowViews(ContentView.Model, views.ToArray());
    }

    private void RequestCameraReset(ContentView view) => MainWindow.RequestCameraReset(view);
}
