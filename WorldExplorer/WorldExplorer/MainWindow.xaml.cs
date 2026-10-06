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

using HelixToolkit.Wpf;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.World;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WorldExplorer.DataExporters;
using WorldExplorer.DataImporters;
using WorldExplorer.Infrastructure;
using WorldExplorer.Themes;
using WorldExplorer.TreeView;

namespace WorldExplorer;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    public static readonly DependencyProperty IsExplorerVisibleProperty = DependencyProperty.Register(
        nameof(IsExplorerVisible), typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

    private const string FileFilter =
        "Snowblind archives|*.gob;*.lmp;*.clp;*.ddf;*.sdb;*.yak;*.hdr|" +
        "GOB archives (*.gob)|*.gob|LMP / CLP archives|*.lmp;*.clp|" +
        "BoS databases (*.ddf, *.sdb)|*.ddf;*.sdb|All files|*.*";

    private readonly FileTreeViewContextManager _contextManager;
    private readonly HashSet<ContentView> _pendingCameraResets = new();
    private GridLength _explorerWidth = new(300);
    private bool _closeConfirmed;

    public MainWindowViewModel ViewModel { get; }

    /// <summary>Whether the explorer panel is shown (Ctrl+B toggles it).</summary>
    public bool IsExplorerVisible
    {
        get => (bool)GetValue(IsExplorerVisibleProperty);
        set => SetValue(IsExplorerVisibleProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        SetupViewports();
        RestoreWindowPlacement();

        _contextManager = new FileTreeViewContextManager(this, treeView);
        ViewModel = new MainWindowViewModel(this);
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        TreeViewItemViewModel.LoadErrorHandler = (title, ex) => ViewModel.ShowFailure(title, ex);

        ThemeManager.ThemeChanged += (_, _) =>
        {
            UpdateThemeMenu();
            ApplyViewportTheme();
        };
        UpdateThemeMenu();
        ApplyViewportTheme();

        treeView.KeyDown += TreeView_KeyDown;
        DragOver += OnDragOver;
        Drop += OnDrop;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var file = App.StartupFile;
        if (file == null && App.Settings.Get("Files.ReopenLastFile", true))
        {
            var last = App.Settings.Get("Files.LastLoadedFile", "") ?? "";
            if (last.Length > 0 && File.Exists(last)) file = last;
        }

        if (file != null) OpenFile(file);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Title / edit state
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes everything that reflects unsaved edits (title, tree markers,
    /// status bar, Overview). Called after any edit or save.
    /// </summary>
    public void UpdateTitle()
    {
        ViewModel.NotifyIsArchiveDirty();
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>Shows an error as a notification, with the details in the Details view.</summary>
    public void ReportError(string title, Exception exception) => ViewModel.ShowFailure(title, exception);

    // ─────────────────────────────────────────────────────────────────────────
    // Viewports
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerable<HelixViewport3D> Viewports => new[] { ModelView.viewport, SkeletonView.viewport, LevelView.viewport };

    private void SetupViewports()
    {
        foreach (var viewport in Viewports)
        {
            viewport.ResetCameraGesture = null;
            viewport.ResetCameraKeyGesture = null;
            viewport.RotateGesture = new MouseGesture(MouseAction.LeftClick);
            viewport.PanGesture = new MouseGesture(MouseAction.MiddleClick);

            viewport.PreviewMouseDown += (s, ev) =>
            {
                if (ev.ChangedButton == MouseButton.Middle && ev.ClickCount > 1)
                {
                    var v = (HelixViewport3D)s;
                    v.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0),
                        new Vector3D(0, 0, 1), 1000);
                    ev.Handled = true;
                }
            };
        }
    }

    /// <summary>Points the viewports' overlay text at the theme colours.</summary>
    private void ApplyViewportTheme()
    {
        foreach (var viewport in Viewports)
        {
            viewport.SetResourceReference(HelixViewport3D.TextBrushProperty, "Brush.ViewportText");
            viewport.SetResourceReference(HelixViewport3D.InfoForegroundProperty, "Brush.TextSecondary");
            viewport.SetResourceReference(HelixViewport3D.CoordinateSystemLabelForegroundProperty, "Brush.TextSecondary");
            viewport.TitleBackground = Brushes.Transparent;
            viewport.InfoBackground = Brushes.Transparent;
            viewport.TitleFontFamily = new FontFamily("Segoe UI");
            viewport.TitleSize = 14;
            viewport.SubTitleSize = 12;
        }
    }

    /// <summary>Sets the title/subtitle overlay on a 3D view.</summary>
    public void SetViewportText(ContentView view, string? title, string? subTitle)
    {
        var viewport = view switch
        {
            ContentView.Model => ModelView.viewport,
            ContentView.Level => LevelView.viewport,
            ContentView.Skeleton => SkeletonView.viewport,
            _ => null
        };
        if (viewport == null) return;
        if (title != null) viewport.Title = title;
        if (subTitle != null) viewport.SubTitle = subTitle;
    }

    /// <summary>
    /// Asks for <paramref name="view"/>'s camera to be framed on its content —
    /// immediately if that view is on screen, otherwise the next time it is
    /// shown (a hidden viewport has no size to frame against).
    /// </summary>
    public void RequestCameraReset(ContentView view)
    {
        _pendingCameraResets.Add(view);
        Dispatcher.BeginInvoke(ProcessPendingCameraResets, DispatcherPriority.Loaded);
    }

    private void ProcessPendingCameraResets()
    {
        if (ViewModel.ActiveView is { } view && _pendingCameraResets.Remove(view))
        {
            ResetCamera(view, animate: false);
        }
    }

    /// <summary>Re-frames the camera of the given 3D view to fit its content.</summary>
    public void ResetCamera(ContentView view, bool animate)
    {
        switch (view)
        {
            case ContentView.Level:
            {
                var bounds = ViewModel.TheLevelViewModel.WorldBounds;
                if (!bounds.IsEmpty)
                    LevelView.viewport.ZoomExtents(bounds, animate ? 400 : 0);
                break;
            }
            case ContentView.Model:
                FrameModel(ModelView.viewport, ViewModel.TheModelViewModel.VifModel);
                break;
            case ContentView.Skeleton:
            {
                var skeleton = ViewModel.TheSkeletonViewModel.Model;
                if (skeleton != null && !skeleton.Bounds.IsEmpty)
                    FrameBounds(SkeletonView.viewport, skeleton.Bounds);
                else
                    FrameModel(SkeletonView.viewport, ViewModel.TheModelViewModel.VifModel);
                break;
            }
        }
    }

    /// <summary>Re-frames whichever 3D view is on screen.</summary>
    public void ResetCamera()
    {
        if (ViewModel.ActiveView is { } view) ResetCamera(view, animate: true);
    }

    /// <summary>
    /// Frames the viewport camera to show the given model's bounding box.
    /// We choose between viewing along -Y or along -X depending on which
    /// gives the larger visible projection (sizeX·sizeZ vs sizeY·sizeZ).
    /// </summary>
    private static void FrameModel(HelixViewport3D viewport, JetBlackEngineLib.Data.Models.Model? model)
    {
        if (model == null || !TryGetModelBounds(model, out var bounds))
        {
            if (viewport.Camera is not ProjectionCamera cam) return;
            cam.Position = new Point3D(0, -100, 50);
            cam.LookDirection = new Vector3D(0, 100, -50);
            cam.UpDirection = new Vector3D(0, 0, 1);
            if (cam is OrthographicCamera oc0) oc0.Width = 200;
            return;
        }

        FrameBounds(viewport, bounds);
    }

    private static void FrameBounds(HelixViewport3D viewport, Rect3D bounds)
    {
        if (viewport.Camera is not ProjectionCamera cam) return;

        var cx = bounds.X + bounds.SizeX / 2;
        var cy = bounds.Y + bounds.SizeY / 2;
        var cz = bounds.Z + bounds.SizeZ / 2;
        var span = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
        if (span < 1) span = 1;
        var distance = span * 2.0;
        var elevation = span * 0.4;

        if (bounds.SizeY * bounds.SizeZ > bounds.SizeX * bounds.SizeZ)
        {
            cam.Position = new Point3D(cx - distance, cy, cz + elevation);
            cam.LookDirection = new Vector3D(distance, 0, -elevation);
        }
        else
        {
            cam.Position = new Point3D(cx, cy - distance, cz + elevation);
            cam.LookDirection = new Vector3D(0, distance, -elevation);
        }
        cam.UpDirection = new Vector3D(0, 0, 1);
        if (cam is OrthographicCamera oc) oc.Width = distance * 1.5;
    }

    private static bool TryGetModelBounds(JetBlackEngineLib.Data.Models.Model model, out Rect3D bounds)
    {
        bounds = Rect3D.Empty;
        if (model.MeshList.Count == 0) return false;

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var mesh in model.MeshList)
        {
            foreach (var v in mesh.Positions)
            {
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.Z > maxZ) maxZ = v.Z;
            }
        }

        if (minX > maxX) return false;
        bounds = new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // View switching
    // ─────────────────────────────────────────────────────────────────────────

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedView))
        {
            SelectTab(ViewModel.ActiveView);
            Dispatcher.BeginInvoke(ProcessPendingCameraResets, DispatcherPriority.Loaded);
        }
    }

    private void SelectTab(ContentView? view)
    {
        if (view == null) return;
        foreach (TabItem tab in tabControl.Items)
        {
            if (tab.Tag is ContentView kind && kind == view)
            {
                tabControl.SelectedItem = tab;
                return;
            }
        }
    }

    private bool TryResolveView(object? parameter, out ContentView view)
    {
        view = default;
        var available = ViewModel?.AvailableViews;
        if (available == null) return false;

        var text = parameter as string;
        if (int.TryParse(text, out var position))
        {
            if (position < 1 || position > available.Count) return false;
            view = available[position - 1].Kind;
            return true;
        }

        if (!Enum.TryParse(text, out ContentView named)) return false;
        view = named;
        return available.Any(v => v.Kind == named);
    }

    private void ShowView_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = TryResolveView(e.Parameter, out _);

    private void ShowView_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (TryResolveView(e.Parameter, out var view)) ViewModel.TryShowView(view);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Opening / closing
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Opens a file (asking to save unsaved edits first).</summary>
    public async void OpenFile(string path)
    {
        try
        {
            await OpenFileAsync(path);
        }
        catch (Exception ex)
        {
            ReportError($"Couldn't open {Path.GetFileName(path)}", ex);
        }
    }

    public async Task<bool> OpenFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            ViewModel.Notifications.Show("File not found", path, ToastKind.Error,
                "Remove from recent files", () => ViewModel.RemoveRecentFile(path));
            return false;
        }

        if (!ConfirmDiscardChanges("opening another file")) return false;
        return await ViewModel.LoadFileAsync(path);
    }

    /// <summary>Re-reads the open file from disk (callers confirm unsaved edits first).</summary>
    public async Task ReloadAsync()
    {
        var path = ViewModel.OpenFilePath;
        if (path == null) return;
        await ViewModel.LoadFileAsync(path);
    }

    /// <summary>
    /// When there are unsaved edits, asks whether to save them before
    /// <paramref name="action"/>. Returns false if the user cancelled (or the
    /// save didn't happen).
    /// </summary>
    public bool ConfirmDiscardChanges(string action)
    {
        if (!ViewModel.HasUnsavedChanges || ViewModel.World == null) return true;

        var result = MessageBox.Show(this,
            $"{ViewModel.World.Name} has unsaved changes ({ViewModel.UnsavedSummary}).\n\n" +
            $"Save them before {action}?",
            "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        return result switch
        {
            MessageBoxResult.Yes => SaveCurrent(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    /// <summary>Called by the view-model before switching games.</summary>
    public bool ConfirmGameChange(GameOption game)
    {
        return ViewModel.World == null || ConfirmDiscardChanges($"switching to {game.ShortName}");
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_closeConfirmed)
        {
            if (!ConfirmDiscardChanges("closing"))
            {
                e.Cancel = true;
                return;
            }
            _closeConfirmed = true;
        }

        SaveWindowPlacement();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        App.SaveSettings();
        base.OnClosed(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            Activate();
            OpenFile(files[0]);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Window placement
    // ─────────────────────────────────────────────────────────────────────────

    private void RestoreWindowPlacement()
    {
        var width = App.Settings.Get("Window.Width", Width);
        var height = App.Settings.Get("Window.Height", Height);
        var left = App.Settings.Get("Window.Left", double.NaN);
        var top = App.Settings.Get("Window.Top", double.NaN);

        if (width >= MinWidth && height >= MinHeight)
        {
            Width = Math.Min(width, SystemParameters.VirtualScreenWidth);
            Height = Math.Min(height, SystemParameters.VirtualScreenHeight);
        }

        // Only restore the position if it is still on a connected screen.
        if (!double.IsNaN(left) && !double.IsNaN(top) &&
            left >= SystemParameters.VirtualScreenLeft - 50 &&
            top >= SystemParameters.VirtualScreenTop - 50 &&
            left + 100 <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
            top + 100 <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        if (App.Settings.Get("Window.Maximized", false))
        {
            WindowState = WindowState.Maximized;
        }

        var explorerWidth = App.Settings.Get("Layout.ExplorerWidth", 300.0);
        if (explorerWidth >= ExplorerColumn.MinWidth && explorerWidth < 900)
        {
            ExplorerColumn.Width = new GridLength(explorerWidth);
        }

        if (!App.Settings.Get("Layout.ExplorerVisible", true))
        {
            SetExplorerVisible(false);
        }
    }

    private void SaveWindowPlacement()
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (!bounds.IsEmpty)
        {
            // Left/Top are NaN until the window has been placed by the system.
            if (!double.IsNaN(bounds.Left) && !double.IsNaN(bounds.Top))
            {
                App.Settings["Window.Left"] = bounds.Left;
                App.Settings["Window.Top"] = bounds.Top;
            }
            App.Settings["Window.Width"] = bounds.Width;
            App.Settings["Window.Height"] = bounds.Height;
        }
        App.Settings["Window.Maximized"] = WindowState == WindowState.Maximized;
        App.Settings["Layout.ExplorerVisible"] = IsExplorerVisible;
        if (IsExplorerVisible)
        {
            App.Settings["Layout.ExplorerWidth"] = ExplorerColumn.ActualWidth;
        }
    }

    private void SetExplorerVisible(bool visible)
    {
        if (visible == IsExplorerVisible) return;

        if (visible)
        {
            ExplorerColumn.MinWidth = 200;
            ExplorerColumn.Width = _explorerWidth;
            ExplorerPanel.Visibility = Visibility.Visible;
            ExplorerSplitter.Visibility = Visibility.Visible;
        }
        else
        {
            _explorerWidth = ExplorerColumn.Width;
            ExplorerColumn.MinWidth = 0;
            ExplorerColumn.Width = new GridLength(0);
            ExplorerPanel.Visibility = Visibility.Collapsed;
            ExplorerSplitter.Visibility = Visibility.Collapsed;
        }

        IsExplorerVisible = visible;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Explorer
    // ─────────────────────────────────────────────────────────────────────────

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        ViewModel.SelectedNode = e.NewValue;
    }

    /// <summary>Right-click selects the row first, like Windows Explorer.</summary>
    private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var obj = e.OriginalSource as DependencyObject;
        while (obj != null && obj is not TreeViewItem)
            obj = obj is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(obj) : null;

        if (obj is TreeViewItem item)
        {
            item.IsSelected = true;
            item.Focus();
        }
    }

    private void TreeView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && treeView.SelectedItem is LmpEntryTreeViewModel entry &&
            entry.LmpFileProperty is not ClpFile && !entry.IsDeleted)
        {
            _contextManager.DeleteEntry(entry);
            e.Handled = true;
        }
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        static void Collapse(TreeViewItemViewModel node)
        {
            foreach (var child in node.Children) Collapse(child);
            if (node.Parent != null) node.IsExpanded = false;
        }

        foreach (var root in ViewModel.RootNodes) Collapse(root);
    }

    private void FilterBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                ViewModel.FilterText = "";
                e.Handled = true;
                break;
            case Key.Down:
            case Key.Enter:
                treeView.Focus();
                e.Handled = true;
                break;
        }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterText = "";
        FilterBox.Focus();
    }

    private void MenuRecentFiles_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        MenuRecentFiles.Items.Clear();

        if (ViewModel.RecentFiles.Count == 0)
        {
            MenuRecentFiles.Items.Add(new MenuItem { Header = "No recent files", IsEnabled = false });
            return;
        }

        foreach (var recent in ViewModel.RecentFiles)
        {
            var item = new MenuItem
            {
                // Underscores would otherwise be treated as access keys.
                Header = recent.Path.Replace("_", "__"),
                Tag = recent.Path,
                IsEnabled = recent.Exists
            };
            item.Click += (o, _) => OpenFile((string)((MenuItem)o).Tag);
            MenuRecentFiles.Items.Add(item);
        }

        MenuRecentFiles.Items.Add(new Separator());
        var clear = new MenuItem { Header = "Clear Recent Files" };
        clear.Click += (_, _) => ViewModel.ClearRecentFiles();
        MenuRecentFiles.Items.Add(clear);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    private void FileOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = ViewModel?.IsFileOpen == true;

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Multiselect = false, Filter = FileFilter };
        var dataPath = App.Settings.Get("Files.DataPath", "") ?? "";
        var lastFolder = Path.GetDirectoryName(ViewModel.RecentFiles.FirstOrDefault()?.Path ?? "");
        if (Directory.Exists(dataPath)) dialog.InitialDirectory = dataPath;
        else if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder)) dialog.InitialDirectory = lastFolder;

        if (dialog.ShowDialog(this) == true)
        {
            OpenFile(dialog.FileName);
        }
    }

    private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        var world = ViewModel?.World;
        e.CanExecute = world != null && (world.WorldGob != null || world.WorldLmp is { } lmp && lmp is not ClpFile);
    }

    private void Save_Executed(object sender, ExecutedRoutedEventArgs e) => SaveCurrent();

    private void SaveArchive_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = ViewModel != null && GetActiveLmpFile() is { } lmp && lmp is not ClpFile;

    private void SaveArchive_Executed(object sender, ExecutedRoutedEventArgs e) => SaveArchive(GetActiveLmpFile());

    private void CloseFile_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges("closing the file")) return;
        ViewModel.CloseFile();
        ViewModel.StatusText = "Ready";
    }

    private async void Reload_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges("reloading")) return;
        await ReloadAsync();
    }

    private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) => Close();

    private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = ViewModel?.TheLevelViewModel.CanUndo == true;

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e) => ViewModel.TheLevelViewModel.Undo();

    private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = ViewModel?.TheLevelViewModel.CanRedo == true;

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e) => ViewModel.TheLevelViewModel.Redo();

    private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SetExplorerVisible(true);
        FilterBox.Focus();
        FilterBox.SelectAll();
    }

    private void ResetCamera_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        => e.CanExecute = ViewModel?.ActiveView is ContentView.Level or ContentView.Model or ContentView.Skeleton;

    private void ResetCamera_Executed(object sender, ExecutedRoutedEventArgs e) => ResetCamera();

    private void ToggleTheme_Executed(object sender, ExecutedRoutedEventArgs e)
        => ApplyTheme(ThemeManager.Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

    private void MenuThemeDark_Click(object sender, RoutedEventArgs e) => ApplyTheme(AppTheme.Dark);

    private void MenuThemeLight_Click(object sender, RoutedEventArgs e) => ApplyTheme(AppTheme.Light);

    private static void ApplyTheme(AppTheme theme)
    {
        ThemeManager.Apply(theme);
        App.Settings["Appearance.Theme"] = theme.ToString();
        App.SaveSettings();
    }

    private void UpdateThemeMenu()
    {
        MenuThemeDark.IsChecked = ThemeManager.Current == AppTheme.Dark;
        MenuThemeLight.IsChecked = ThemeManager.Current == AppTheme.Light;
    }

    private void ToggleExplorer_Executed(object sender, ExecutedRoutedEventArgs e) => SetExplorerVisible(!IsExplorerVisible);

    private void MenuGames_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        // SubmenuOpened bubbles; only rebuild for this menu's own submenu.
        if (!ReferenceEquals(e.OriginalSource, MenuGames)) return;

        MenuGames.Items.Clear();
        foreach (var game in GameOption.All)
        {
            var item = new MenuItem
            {
                Header = game.Name,
                Tag = game,
                IsChecked = ReferenceEquals(game, ViewModel.SelectedGame)
            };
            item.Click += (o, _) => ViewModel.SelectedGame = (GameOption)((MenuItem)o).Tag;
            MenuGames.Items.Add(item);
        }
    }

    private void Settings_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var previousGame = ViewModel.SelectedGame;
        var previousOpaque = App.Settings.Get("Textures.ForceOpaque", false);

        var window = new SettingsWindow { Owner = this };
        if (window.ShowDialog() != true) return;

        var newGame = GameOption.For(App.Settings.Get("Core.EngineVersion", previousGame.Version));
        var opaqueChanged = App.Settings.Get("Textures.ForceOpaque", false) != previousOpaque;
        var gameChanged = !ReferenceEquals(newGame, previousGame);

        if (gameChanged)
        {
            // Settings already stored the new game; just announce it.
            ViewModel.ApplyGame(newGame);
        }

        if ((gameChanged || opaqueChanged) && ViewModel.IsFileOpen)
        {
            if (ConfirmDiscardChanges("reloading with the new settings"))
            {
                _ = ReloadAsync();
            }
            else
            {
                ViewModel.Notifications.Info("Settings saved", "Reload the file (F5) to apply them.");
            }
        }
    }

    private void Shortcuts_Executed(object sender, ExecutedRoutedEventArgs e)
        => new ShortcutsWindow { Owner = this }.ShowDialog();

    private void About_Executed(object sender, ExecutedRoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();

    // ─────────────────────────────────────────────────────────────────────────
    // Saving
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Saves the open file: the GOB, or a stand-alone LMP. Returns false if not saved.</summary>
    public bool SaveCurrent()
    {
        var world = ViewModel.World;
        if (world?.WorldGob != null) return SaveGob();
        if (world?.WorldLmp is { } lmp && lmp is not ClpFile) return SaveArchive(lmp);

        ViewModel.Notifications.Info("Nothing to save", "This type of file is read-only in WorldExplorer.");
        return false;
    }

    /// <summary>
    /// Saves an archive as a stand-alone .LMP file. Saving the open LMP marks
    /// it saved; saving an LMP from inside a GOB writes a copy (the GOB itself
    /// still needs saving).
    /// </summary>
    public bool SaveArchive(LmpFile? lmpFile)
    {
        if (lmpFile == null) return false;
        if (lmpFile is ClpFile)
        {
            ViewModel.Notifications.Warning("Not supported", "CLP archives are hash-indexed and can't be saved.");
            return false;
        }

        var dialog = new SaveFileDialog
        {
            FileName = lmpFile.Name,
            Filter = "LMP Archive|*.lmp|All Files|*.*"
        };
        if (ViewModel.World != null && Directory.Exists(ViewModel.World.DataPath))
            dialog.InitialDirectory = ViewModel.World.DataPath;
        if (dialog.ShowDialog(this) != true) return false;

        try
        {
            AssetImporter.SaveArchive(lmpFile, dialog.FileName);
            var savedOpenFile = ReferenceEquals(lmpFile, ViewModel.World?.WorldLmp) && IsOpenFile(dialog.FileName);
            if (savedOpenFile)
            {
                ViewModel.World!.MarkSaved(new[] { lmpFile });
            }
            UpdateTitle();
            AnnounceSave(dialog.FileName, savedOpenFile || !ReferenceEquals(lmpFile, ViewModel.World?.WorldLmp));
            return true;
        }
        catch (Exception ex)
        {
            ReportError("Save failed", ex);
            return false;
        }
    }

    /// <summary>
    /// True when <paramref name="path"/> is the open file. Only then does a save
    /// clear the unsaved state; writing anywhere else saves a copy.
    /// </summary>
    private bool IsOpenFile(string path)
    {
        var open = ViewModel.World?.FilePath;
        if (open == null) return false;
        try
        {
            return string.Equals(Path.GetFullPath(path), Path.GetFullPath(open), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void AnnounceSave(string path, bool savedOpenFile)
    {
        if (savedOpenFile)
        {
            ViewModel.Notifications.Success($"Saved {Path.GetFileName(path)}", path);
        }
        else
        {
            ViewModel.Notifications.Success($"Saved a copy as {Path.GetFileName(path)}",
                $"{path}\n{ViewModel.World?.Name} itself still has the unsaved changes.");
        }
        ViewModel.StatusText = $"Saved {path}";
    }

    /// <summary>Returns the GOB file for the currently loaded world, or null.</summary>
    private GobFile? GetActiveGobFile() => ViewModel.World?.WorldGob;

    /// <summary>
    /// Writes the GOB with every pending edit, verifies the result decodes, and
    /// copies the companion .TEX next to it when saving to a new location.
    /// </summary>
    public bool SaveGob()
    {
        var world = ViewModel.World;
        var gob = GetActiveGobFile();
        if (world == null || gob == null)
        {
            ViewModel.Notifications.Info("No GOB file is open");
            return false;
        }

        var dialog = new SaveFileDialog
        {
            FileName = gob.Name,
            Filter = "GOB Archive|*.gob|All Files|*.*"
        };
        if (Directory.Exists(world.DataPath)) dialog.InitialDirectory = world.DataPath;
        if (dialog.ShowDialog(this) != true) return false;

        try
        {
            var report = new StringBuilder();

            // ── 1. Live in-memory state ──────────────────────────────────────
            var liveCount = ViewModel.TheLevelViewModel.ObjectManager.Objects.Count;
            report.AppendLine($"In-memory objects (ObjectManager): {liveCount}");

            // ── 2. Pending-edit state on every dirty LMP ─────────────────────
            var pendingObLength = -1;
            foreach (var (lmpName, lmp) in gob.Directory)
            {
                if (!lmp.IsDirty) continue;

                report.AppendLine($"Dirty LMP: {lmpName}");
                foreach (var key in lmp.PendingEdits.Keys)
                {
                    var len = lmp.PendingEdits[key].Length;
                    report.AppendLine($"   pending edit: {key} ({len} bytes)");
                    if (key.Equals("objects.ob", StringComparison.OrdinalIgnoreCase))
                    {
                        pendingObLength = len;
                    }
                }
                foreach (var key in lmp.PendingDeletions)
                {
                    report.AppendLine($"   pending deletion: {key}");
                }
                foreach (var (name, data) in lmp.PendingAdditions)
                {
                    report.AppendLine($"   pending addition: {name} ({data.Length} bytes)");
                }
            }
            if (pendingObLength < 0)
            {
                report.AppendLine("Note: no pending edit for objects.ob.");
            }

            // ── 3. Pack, then verify the packed bytes round-trip ─────────────
            var packed = GobWriter.TryPatchInPlace(gob) ?? GobWriter.Pack(gob);

            var savedObCount = -1;
            var verifyGob = new GobFile(gob.EngineVersion, "verify.gob", packed);
            foreach (var (lmpName, lmp) in verifyGob.Directory)
            {
                lmp.ReadDirectory();
                if (lmp.Directory.TryGetValue("objects.ob", out var obEntry))
                {
                    var objs = ObDecoder.Decode(lmp.FileData, obEntry.StartOffset, obEntry.Length);
                    savedObCount = objs.Count;
                    report.AppendLine(
                        $"objects.ob decoded from PACKED bytes ({lmpName}): {savedObCount} objects");
                    break;
                }
            }
            if (savedObCount < 0)
            {
                report.AppendLine("WARNING: no objects.ob entry found in the packed GOB!");
            }

            // ── 4. Write to disk ──────────────────────────────────────────────
            // Pending edits are kept: the in-memory archive still holds the
            // original bytes, so later saves must include these edits too.
            File.WriteAllBytes(dialog.FileName, packed);
            var savedOpenFile = IsOpenFile(dialog.FileName);
            if (savedOpenFile)
            {
                world.MarkSaved(gob.Directory.Values);
            }
            UpdateTitle();

            if (CopyCompanionTexFile(dialog.FileName))
            {
                report.AppendLine("Companion .TEX copied alongside the GOB.");
            }

            var reportText = report.ToString();
            ViewModel.Notifications.Show(
                savedOpenFile ? $"Saved {Path.GetFileName(dialog.FileName)}" : $"Saved a copy as {Path.GetFileName(dialog.FileName)}",
                savedOpenFile ? dialog.FileName : $"{dialog.FileName}\n{world.Name} itself still has the unsaved changes.",
                ToastKind.Success, "View save report", () => ViewModel.ShowDetails(reportText, "Save report"));
            ViewModel.StatusText = $"Saved {dialog.FileName}";
            return true;
        }
        catch (Exception ex)
        {
            ReportError("Save failed", ex);
            return false;
        }
    }

    /// <summary>
    /// BGDA level textures live in a companion .TEX file next to the .GOB
    /// (TOWN.GOB ↔ TOWN.TEX) — they are not inside the archive. When the GOB
    /// is saved to a new folder or name, copy the companion file alongside it
    /// with a matching base name so the level still loads textured.
    /// Returns true if a copy was made.
    /// </summary>
    private bool CopyCompanionTexFile(string gobDestinationPath)
    {
        var world = ViewModel.World;
        if (world == null) return false;

        // Locate the original companion TEX next to the source GOB.
        var baseName = Path.GetFileNameWithoutExtension(world.Name);
        var sourceTex = Path.Combine(world.DataPath, baseName + ".TEX");
        if (!File.Exists(sourceTex))
        {
            sourceTex = Path.Combine(world.DataPath, baseName + ".tex");
            if (!File.Exists(sourceTex)) return false;   // world has no level TEX
        }

        // Destination: same folder + base name as the saved GOB.
        var destTex = Path.Combine(
            Path.GetDirectoryName(gobDestinationPath) ?? "",
            Path.GetFileNameWithoutExtension(gobDestinationPath) + Path.GetExtension(sourceTex));

        // Saving in place — the TEX is already there.
        if (string.Equals(Path.GetFullPath(sourceTex), Path.GetFullPath(destTex),
                StringComparison.OrdinalIgnoreCase))
            return false;

        // Don't clobber an existing file; the TEX is never modified by the
        // editor, so an existing copy is already correct.
        if (File.Exists(destTex)) return false;

        File.Copy(sourceTex, destTex);
        return true;
    }

    /// <summary>
    /// Returns the LMP the selected tree node belongs to, falling back to the
    /// open file's top-level LMP. Null if nothing is loaded.
    /// </summary>
    private LmpFile? GetActiveLmpFile()
    {
        return ViewModel.SelectedNode switch
        {
            LmpEntryTreeViewModel entry => entry.LmpFileProperty,
            AbstractLmpTreeViewModel node => node.LmpFileProperty,
            _ => ViewModel.World?.WorldLmp
        };
    }

    /// <summary>Buttons for the Overview page of <paramref name="node"/>.</summary>
    public IEnumerable<OverviewAction> CreateOverviewActions(TreeViewItemViewModel node)
    {
        var world = ViewModel.World;
        if (world == null) yield break;

        switch (node)
        {
            case GobTreeViewModel when world.WorldGob is { } gob:
                yield return new OverviewAction("Save GOB…", "Icon.Save",
                    new RelayCommand(() => SaveGob()), IsPrimary: world.HasUnsavedChanges());
                yield return new OverviewAction("Export All Textures…", "Icon.Export",
                    new RelayCommand(() => RunAction("Export", () => _contextManager.BatchExportTextures(gob))));
                break;

            case LmpTreeViewModel archive:
            {
                var lmp = archive.LmpFileProperty;
                if (lmp is not ClpFile)
                {
                    if (archive.Parent is GobTreeViewModel)
                    {
                        yield return new OverviewAction("Save GOB…", "Icon.Save",
                            new RelayCommand(() => SaveGob()), IsPrimary: world.HasUnsavedChanges());
                    }
                    else
                    {
                        yield return new OverviewAction("Save Archive…", "Icon.Save",
                            new RelayCommand(() => SaveArchive(lmp)), IsPrimary: world.HasUnsavedChanges(lmp));
                        yield return new OverviewAction("Add Entry…", "Icon.Add",
                            new RelayCommand(() => RunAction("Add entry", () => _contextManager.AddEntry(lmp))));
                    }
                }

                yield return new OverviewAction("Export All Textures…", "Icon.Export",
                    new RelayCommand(() => RunAction("Export", () => _contextManager.BatchExportTextures(lmp))));
                yield return new OverviewAction("Export All Entries…", "Icon.Export",
                    new RelayCommand(() => RunAction("Export", () => _contextManager.BatchExportAll(lmp))));
                break;
            }
        }
    }

    private void RunAction(string name, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ReportError($"{name} failed", ex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // File → Import / Export
    // ─────────────────────────────────────────────────────────────────────────

    private void Menu_Import_ReplaceEntry_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is not LmpEntryTreeViewModel entry)
        {
            ViewModel.Notifications.Info("Select an archive entry first");
            return;
        }
        RunAction("Replace", () => _contextManager.ReplaceEntry(entry));
    }

    private void Menu_Import_Texture_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is not LmpEntryTreeViewModel { Kind: NodeKind.Texture } entry)
        {
            ViewModel.Notifications.Info("Select a texture (.tex) entry first");
            return;
        }
        RunAction("Import", () => _contextManager.ImportTexture(entry));
    }

    private void Menu_Import_AddEntry_Click(object sender, RoutedEventArgs e)
    {
        var lmp = GetActiveLmpFile();
        if (lmp == null || lmp is ClpFile)
        {
            ViewModel.Notifications.Info("No editable archive is selected");
            return;
        }
        RunAction("Add entry", () => _contextManager.AddEntry(lmp));
    }

    private void Menu_BatchExport_Textures_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is GobTreeViewModel && GetActiveGobFile() is { } gob)
        {
            RunAction("Export", () => _contextManager.BatchExportTextures(gob));
            return;
        }

        var lmp = GetActiveLmpFile();
        if (lmp == null)
        {
            ViewModel.Notifications.Info("Select an archive first");
            return;
        }
        RunAction("Export", () => _contextManager.BatchExportTextures(lmp));
    }

    private void Menu_BatchExport_AllEntries_Click(object sender, RoutedEventArgs e)
    {
        var lmp = GetActiveLmpFile();
        if (lmp == null)
        {
            ViewModel.Notifications.Info("Select an archive first");
            return;
        }
        RunAction("Export", () => _contextManager.BatchExportAll(lmp));
    }

    private void Menu_Export_Texture_Click(object sender, RoutedEventArgs e) => ExportSelectedTexture(weave: false);

    private void Menu_Export_TextureWeaved_Click(object sender, RoutedEventArgs e) => ExportSelectedTexture(weave: true);

    /// <summary>Saves the texture on screen as a PNG, optionally weaving interlaced fields.</summary>
    public void ExportSelectedTexture(bool weave)
    {
        var src = ViewModel.SelectedNodeImage;
        if (src == null)
        {
            ViewModel.Notifications.Info("No texture is loaded", "Select a texture, model or level element first.");
            return;
        }

        if (weave && (src.PixelHeight < 2 || (src.PixelHeight & 1) != 0))
        {
            ViewModel.Notifications.Warning("Can't weave this texture", "Its height must be even.");
            return;
        }

        var name = ViewModel.SelectedTreeNode?.Label ?? "texture";
        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            FileName = Path.GetFileNameWithoutExtension(name) + (weave ? "_woven" : "") + ".png"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            BitmapSource image = weave ? WeaveInterlacedFields(src) : src;
            using (var stream = new FileStream(dialog.FileName, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
            ViewModel.Notifications.Success("Texture exported", dialog.FileName);
        }
        catch (Exception ex)
        {
            ReportError("Export failed", ex);
        }
    }

    /// <summary>
    /// Interleaves a texture stored as two stacked fields (top half + bottom half)
    /// into a single image, taking output line 2k from top-half line k and output
    /// line 2k+1 from bottom-half line k.
    /// </summary>
    private static BitmapSource WeaveInterlacedFields(BitmapSource src)
    {
        var converted = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        int w = converted.PixelWidth, h = converted.PixelHeight, halfH = h / 2;
        var stride = w * 4;
        var pixels = new byte[stride * h];
        converted.CopyPixels(pixels, stride, 0);

        var output = new byte[stride * h];
        for (var y = 0; y < halfH; y++)
        {
            Buffer.BlockCopy(pixels, y * stride, output, (2 * y) * stride, stride);
            Buffer.BlockCopy(pixels, (halfH + y) * stride, output, (2 * y + 1) * stride, stride);
        }
        return BitmapSource.Create(w, h, src.DpiX, src.DpiY, PixelFormats.Bgra32, null, output, stride);
    }

    private void Menu_Export_Model_Click(object sender, RoutedEventArgs e)
    {
        var modelViewModel = ViewModel.TheModelViewModel;
        if (modelViewModel.VifModel == null)
        {
            ViewModel.Notifications.Info("No model is loaded", "Select a model (.vif) or entity first.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "glTF File|*.gltf|OBJ File|*.obj",
            FilterIndex = 1,
            FileName = Path.GetFileNameWithoutExtension(ViewModel.SelectedTreeNode?.Label ?? "model") + ".gltf"
        };
        if (dialog.ShowDialog(this) != true) return;

        var ext = Path.GetExtension(dialog.FileName).ToUpperInvariant();
        IVifExporter? exporter = ext switch
        {
            ".OBJ" => new VifObjExporter(),
            ".GLTF" => new VifGltfExporter(),
            _ => null
        };
        if (exporter == null)
        {
            ViewModel.Notifications.Warning("Unknown file format", "Choose a .gltf or .obj file name.");
            return;
        }

        try
        {
            if (ext == ".GLTF" && modelViewModel.Parts != null)
            {
                // Composite entities keep one material per part.
                new VifGltfExporter().SavePartsToFile(dialog.FileName, modelViewModel.Parts, null, -1, 1.0);
            }
            else
            {
                exporter.SaveToFile(dialog.FileName, modelViewModel.VifModel, modelViewModel.Texture);
            }
            ViewModel.Notifications.Success("Model exported", dialog.FileName);
        }
        catch (Exception ex)
        {
            ReportError("Export failed", ex);
        }
    }

    private void Menu_Export_PosedModel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TheModelViewModel.ShowExportForPosedModel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edit → level objects
    // ─────────────────────────────────────────────────────────────────────────

    private void Menu_CopyObject_Click(object sender, RoutedEventArgs e) => LevelView.CopySelectedObject();

    private void Menu_PasteObject_Click(object sender, RoutedEventArgs e) => LevelView.PasteObject();

    private void Menu_Duplicate_Click(object sender, RoutedEventArgs e) => LevelView.DuplicateSelection();

    private void Menu_Delete_Click(object sender, RoutedEventArgs e) => LevelView.DeleteSelection();

    // ─────────────────────────────────────────────────────────────────────────
    // Tools (Dark Alliance)
    // ─────────────────────────────────────────────────────────────────────────

    private void MenuEditExecutableClick(object sender, RoutedEventArgs e)
    {
        var open = new OpenFileDialog
        {
            Title = "Open the Dark Alliance executable (e.g. SLES_506.72)",
            Filter = "PS2 Executable|SLES_506.72;SLUS_*;SLES_*;*.*|All Files|*.*",
            Multiselect = false
        };
        if (open.ShowDialog(this) != true) return;

        JetBlackEngineLib.Data.Executable.BgdaExecutable exe;
        try
        {
            exe = JetBlackEngineLib.Data.Executable.BgdaExecutable.Open(open.FileName);
        }
        catch (NotSupportedException ex)
        {
            MessageBox.Show(this, ex.Message, "Unsupported file",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        new ExecutableEditorWindow(exe) { Owner = this }.ShowDialog();
    }

    private void MenuEditSaveClick(object sender, RoutedEventArgs e)
    {
        var open = new OpenFileDialog
        {
            Title = "Open a Dark Alliance PS2 save export (.psu)",
            Filter = "PS2 Save Export|*.psu|All Files|*.*"
        };
        if (open.ShowDialog(this) != true) return;

        var save = JetBlackEngineLib.Data.Save.BgdaSave.Open(open.FileName);
        if (save.GetSlots().Count == 0)
        {
            MessageBox.Show(this,
                "No Dark Alliance save slots found in this file. Expected a .psu export of a " +
                "BESLES-50672 save.", "Unsupported file",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        new SaveEditorWindow(save) { Owner = this }.ShowDialog();
    }
}
