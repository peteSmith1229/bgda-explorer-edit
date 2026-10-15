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
using JetBlackEngineLib.Data.World;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.Controls;
using WorldExplorer.Infrastructure;
using WorldExplorer.TreeView;
using WorldExplorer.WorldDefs;

namespace WorldExplorer;

/// <summary>
/// Interaction logic for LevelView.xaml
/// </summary>
public partial class LevelView
{
    private LevelViewModel? _lvm;
    private ObjectDragGizmo? _gizmo;
    private ElementDragGizmo? _elementGizmo;
    private ObjectRotateGizmo? _objectRotateGizmo;
    private ElementRotateGizmo? _elementRotateGizmo;
    // True while we set the selection ourselves, so the PropertyChanged observer
    // syncs the gizmos once at the end instead of on each intermediate change
    // (and avoids re-entrancy via the properties panel's two-way binding).
    private bool _suppressSelectionSync;

    public LevelView()
    {
        InitializeComponent();
        DataContextChanged += LevelView_DataContextChanged;
        viewport.MouseUp += viewport_MouseUp;
        viewport.PreviewKeyDown += Viewport_KeyDown;

        viewport.CalculateCursorPosition = true;
        viewport.ContextMenu = BuildViewportContextMenu();

        _gizmo = new ObjectDragGizmo(viewport);
        _elementGizmo = new ElementDragGizmo(viewport);
        _gizmo.ObjectMoved += () => propertiesArea.RefreshObjectFields();
        _elementGizmo.ElementMoved += () => propertiesArea.RefreshElementFields();

        _objectRotateGizmo = new ObjectRotateGizmo(viewport);
        _elementRotateGizmo = new ElementRotateGizmo(viewport);

        viewport.AddHandler(
            UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler(Viewport_DragMouseUp),
            handledEventsToo: true);

        ElementSelected(null);
    }

    private NotificationService? Notifications => _lvm?.MainViewModel.Notifications;

    private void Viewport_DragMouseUp(object sender, MouseButtonEventArgs e)
    {
        _gizmo?.EndDrag();
        _elementGizmo?.EndDrag();
        _objectRotateGizmo?.EndDrag();
        _elementRotateGizmo?.EndDrag();
    }

    private void Viewport_KeyDown(object sender, KeyEventArgs e)
    {
        if (_lvm == null) return;

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        switch (e.Key)
        {
            case Key.Z when ctrl && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                _lvm.Redo();          // Ctrl+Shift+Z = redo
                e.Handled = true;
                break;

            case Key.Z when ctrl:
                _lvm.Undo();
                e.Handled = true;
                break;

            case Key.Y when ctrl:
                _lvm.Redo();
                e.Handled = true;
                break;

            case Key.L when !ctrl:
                _lvm.EnableLevelSpecifiedLights = !_lvm.EnableLevelSpecifiedLights;
                e.Handled = true;
                break;

            case Key.C when ctrl:
                CopySelectedObject();
                e.Handled = true;
                break;

            case Key.V when ctrl:
                PasteObject();
                e.Handled = true;
                break;

            case Key.D when ctrl:
                DuplicateSelection();
                e.Handled = true;
                break;

            case Key.Delete:
                DeleteSelection();
                e.Handled = true;
                break;
        }
    }

    private void LevelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Stop listening to the previous view-model.
        if (_lvm != null)
            _lvm.PropertyChanged -= Lvm_PropertyChanged;

        if (DataContext is not LevelViewModel lvm)
        {
            // Cleared level view
            _lvm = null;
            SyncGizmosToSelection();   // detaches any gizmos
            return;
        }

        _lvm = lvm;
        _lvm.PropertyChanged += Lvm_PropertyChanged;
        SyncGizmosToSelection();       // reflect any pre-existing selection
    }

    /// <summary>
    /// Keeps the gizmos in step with the view-model's selection. Any code that
    /// sets <see cref="LevelViewModel.SelectedObject"/> or
    /// <see cref="LevelViewModel.SelectedElement"/> — the viewport hit-test, the
    /// tree, paste/duplicate — drives the gizmos through here.
    /// </summary>
    private void Lvm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSelectionSync) return;
        if (e.PropertyName is nameof(LevelViewModel.SelectedObject) or nameof(LevelViewModel.SelectedElement)
            or nameof(LevelViewModel.IsEditable))
        {
            SyncGizmosToSelection();
        }
    }

    /// <summary>
    /// Attaches the move + rotate gizmos for whichever of object / element is
    /// currently selected (or detaches all when nothing is). Exactly one of the
    /// two selections is expected to be non-null; object wins if both are set.
    /// Read-only previews get no gizmos.
    /// </summary>
    private void SyncGizmosToSelection()
    {
        var obj = _lvm?.SelectedObject;
        var ele = _lvm?.SelectedElement;
        var editable = _lvm?.IsEditable == true;

        if (_lvm != null && editable && obj != null)
        {
            _elementGizmo?.Detach();
            _elementRotateGizmo?.Detach();
            _gizmo?.Attach(obj, _lvm);
            _objectRotateGizmo?.Attach(obj, _lvm);
        }
        else if (_lvm != null && editable && ele != null)
        {
            _gizmo?.Detach();
            _objectRotateGizmo?.Detach();
            _elementGizmo?.Attach(ele.WorldElement, _lvm);
            _elementRotateGizmo?.Attach(ele.WorldElement, _lvm);
        }
        else
        {
            _gizmo?.Detach();
            _objectRotateGizmo?.Detach();
            _elementGizmo?.Detach();
            _elementRotateGizmo?.Detach();
        }

        // Open the Properties panel when something is selected (any source).
        if ((obj != null || ele != null) && PropertiesToggle.IsChecked != true)
            PropertiesToggle.IsChecked = true;
    }

    private void viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            var hitResult = GetHitTestResult(e.GetPosition(viewport));

            if (hitResult == null || _lvm == null) return;

            var worldNode = _lvm.WorldNode;

            WorldElementTreeViewModel? selectedElement = null;

            if (worldNode == null)
            {
                ElementSelected(null);
                return;
            }

            var vod = _lvm.ObjectManager.HitTest(hitResult);

            if (vod != null)
            {
                ObjectSelected(vod);
                return;
            }

            if (_lvm.Scene != null)
            {
                var hitElement = _lvm.GetElementForVisual(hitResult);
                if (hitElement != null)
                {
                    if (worldNode.HasDummyChild) worldNode.ForceLoadChildren();
                    selectedElement = worldNode.Children
                        .OfType<WorldElementTreeViewModel>()
                        .FirstOrDefault(n => ReferenceEquals(n.WorldElement, hitElement));
                }
            }

            ElementSelected(selectedElement);
        }
    }

    private void ElementSelected(WorldElementTreeViewModel? ele)
    {
        if (_lvm == null) return;

        _suppressSelectionSync = true;
        _lvm.SelectedObject = null;
        _lvm.SelectedElement = ele;
        _suppressSelectionSync = false;

        SyncGizmosToSelection();
    }

    private void ObjectSelected(VisualObjectData? obj)
    {
        if (_lvm == null) return;

        _suppressSelectionSync = true;
        _lvm.SelectedElement = null;
        _lvm.SelectedObject = obj;
        _suppressSelectionSync = false;

        SyncGizmosToSelection();
    }

    private ModelVisual3D? GetHitTestResult(Point location)
    {
        var result = VisualTreeHelper.HitTest(viewport, location);
        if (result is { VisualHit: ModelVisual3D visual })
        {
            return visual;
        }

        return null;
    }

    private void Frame_Click(object sender, RoutedEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.ResetCamera(ContentView.Level, animate: true);
    }

    private void HideProperties_Click(object sender, RoutedEventArgs e)
    {
        PropertiesToggle.IsChecked = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Selection actions (also used by the Edit menu)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Copies the selected object to the system clipboard as JSON.</summary>
    public void CopySelectedObject()
    {
        var selected = _lvm?.SelectedObject;
        if (selected?.ObjectData == null)
        {
            Notifications?.Info("Select an object to copy", "Ctrl+click an object in the Level view.");
            return;
        }

        ObjectClipboard.Copy(selected.ObjectData);
        Notifications?.Info($"Copied '{selected.ObjectData.Name}'");
    }

    /// <summary>
    /// Pastes the clipboard object into the level.  Position priority:
    ///   1. The 3D cursor position (where the mouse is over level geometry),
    ///   2. otherwise the source position nudged by a couple of units so the
    ///      copy is visibly distinct from the original.
    /// Remember: ObjectData.Floats are 4× the displayed world offset
    /// (ObjectManager computes Offset = Floats / 4).
    /// </summary>
    public void PasteObject()
    {
        if (_lvm == null || !_lvm.IsEditable) return;

        if (!ObjectClipboard.HasObject())
        {
            Notifications?.Info("Nothing to paste",
                "Select an object (Ctrl+click) and press Ctrl+C first.");
            return;
        }

        var pasted = ObjectClipboard.TryPaste();
        if (pasted == null)
        {
            Notifications?.Warning("Couldn't paste", "The clipboard content could not be read as an object.");
            return;
        }

        var cursor = viewport.CursorPosition;
        if (cursor.HasValue)
        {
            pasted.Floats[0] = (float)(cursor.Value.X * 4.0);
            pasted.Floats[1] = (float)(cursor.Value.Y * 4.0);
            pasted.Floats[2] = (float)(cursor.Value.Z * 4.0);
        }
        else
        {
            pasted.Floats[0] += 8.0f;
            pasted.Floats[1] += 8.0f;
        }

        var vod = _lvm.AddObjectToLevel(pasted);
        if (vod != null)
        {
            ObjectSelected(vod);
        }
        else
        {
            // Parse produced no visual (e.g. a black light) — the object IS in
            // the level data and will be saved, it just has nothing to render.
            Notifications?.Info($"'{pasted.Name}' added",
                "It has no visual (some object types, such as black lights, render nothing).");
        }
    }

    private void AddObject_Click(object sender, RoutedEventArgs e) => AddObject();

    /// <summary>
    /// Adds a new object (one of the game's classes, or a model from an
    /// archive) to the level and selects it. <paramref name="archive"/> starts
    /// the dialog on what that archive holds.
    /// </summary>
    public void AddObject(string? archive = null)
    {
        if (_lvm?.WorldNode == null || !_lvm.IsEditable)
        {
            Notifications?.Info("Open a level first", "Select a level (.world) in the explorer, then add objects to it.");
            return;
        }

        if (_lvm.MainViewModel.EngineVersion != EngineVersion.DarkAlliance)
        {
            Notifications?.Info("Not for this game yet", "Adding objects knows the object classes of Baldur's Gate: Dark Alliance.");
            return;
        }

        var (position, facing, hint) = NewObjectPlacement();
        var dialog = new AddObjectWindow(_lvm.MainViewModel.World?.WorldGob, position, facing, hint, archive)
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() != true || dialog.Result is not { } added) return;

        var vod = _lvm.AddObjectToLevel(added);
        if (vod != null)
        {
            ObjectSelected(vod);
        }

        Notifications?.Success($"{added.Name} added to {_lvm.WorldNode.Label}",
            "Save the GOB (Ctrl+S) to write it into the level.");
    }

    /// <summary>Where a new object starts: under the mouse, next to the selection, or by the players' start.</summary>
    private ((float X, float Y, float Z) Position, double Facing, string Hint) NewObjectPlacement()
    {
        var objects = _lvm?.ObjectManager.Objects ?? Array.Empty<ObjectData>();
        var cursor = viewport.CursorPosition;
        if (cursor.HasValue)
        {
            // From above, the mouse is often over a roof: stand it as high as the nearest character instead.
            var (x, y) = ((float)(cursor.Value.X * 4.0), (float)(cursor.Value.Y * 4.0));
            var nearest = objects
                .Where(o => BgdaObjectClasses.Find(o.Name)?.Kind is BgdaObjectKind.Npc or BgdaObjectKind.Monster
                    or BgdaObjectKind.Ambient or BgdaObjectKind.PlayerStart)
                .OrderBy(o => (o.Floats[0] - x) * (o.Floats[0] - x) + (o.Floats[1] - y) * (o.Floats[1] - y))
                .FirstOrDefault();
            return nearest != null
                ? ((x, y, nearest.Floats[2]), 0,
                    $"It starts where the mouse last was over the level, as high as the nearest {nearest.Name}.")
                : ((x, y, (float)(cursor.Value.Z * 4.0)), 0, "It starts where the mouse last was over the level.");
        }

        if (_lvm?.SelectedObject?.ObjectData is { } selected)
        {
            return ((selected.Floats[0] + 8, selected.Floats[1] + 8, selected.Floats[2]),
                LevelObjects.FacingOf(selected.I6), $"It starts next to the selected {selected.Name}.");
        }

        var start = objects.FirstOrDefault(o => o.Name == "Player1" && o.GetProperty("default") == "1") ??
                    objects.FirstOrDefault(o => o.Name == "Player1");
        if (start != null)
        {
            return ((start.Floats[0] + 8, start.Floats[1] + 8, start.Floats[2]),
                LevelObjects.FacingOf(start.I6), "It starts next to where the players start.");
        }

        return ((0, 0, 0), 0, "It starts at the middle of the level.");
    }

    /// <summary>
    /// Duplicates the selected object (small offset, clipboard untouched) or
    /// the selected element, and selects the copy.
    /// </summary>
    public void DuplicateSelection()
    {
        if (_lvm == null || !_lvm.IsEditable) return;

        var selected = _lvm.SelectedObject;
        if (selected?.ObjectData != null)
        {
            var clone = ObjectClipboard.Clone(selected.ObjectData);
            clone.Floats[0] += 8.0f;   // 2 world units × 4
            clone.Floats[1] += 8.0f;

            var vod = _lvm.AddObjectToLevel(clone);
            if (vod != null)
            {
                ObjectSelected(vod);
            }
            return;
        }

        if (_lvm.SelectedElement != null)
        {
            _lvm.DuplicateSelectedElement();
            return;
        }

        Notifications?.Info("Nothing selected", "Ctrl+click an object or element to select it.");
    }

    /// <summary>Deletes the selected object or element.</summary>
    public void DeleteSelection()
    {
        if (_lvm == null || !_lvm.IsEditable) return;

        var selected = _lvm.SelectedObject;
        if (selected != null)
        {
            _lvm.DeleteObjectFromLevel(selected);
            ObjectSelected(null);   // collapse selection in the properties panel
            return;
        }

        if (_lvm.SelectedElement != null)
        {
            _lvm.DeleteSelectedElement();
        }
    }

    /// <summary>
    /// Builds the viewport right-click menu. Item visibility/enablement is
    /// refreshed each time the menu opens: object actions when an object is
    /// selected, element actions when an element is selected.
    /// </summary>
    private ContextMenu BuildViewportContextMenu()
    {
        MenuItem Item(string header, string gesture, string iconKey, Action action)
        {
            var item = new MenuItem
            {
                Header = header,
                InputGestureText = gesture,
                Icon = new GeometryIcon { Data = TryFindResource(iconKey) as Geometry }
            };
            item.Click += (_, _) => action();
            return item;
        }

        var addItem = Item("Add Object…", "", "Icon.Add", () => AddObject());
        var copyItem = Item("Copy Object", "Ctrl+C", "Icon.Copy", CopySelectedObject);
        var pasteItem = Item("Paste Object", "Ctrl+V", "Icon.Paste", PasteObject);
        var duplicateItem = Item("Duplicate", "Ctrl+D", "Icon.Duplicate", DuplicateSelection);
        var deleteItem = Item("Delete", "Del", "Icon.Delete", DeleteSelection);
        var frameItem = Item("Frame Level", "Ctrl+Home", "Icon.Frame",
            () => (Window.GetWindow(this) as MainWindow)?.ResetCamera(ContentView.Level, animate: true));

        var menu = new ContextMenu();
        menu.Items.Add(addItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(copyItem);
        menu.Items.Add(pasteItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(duplicateItem);
        menu.Items.Add(deleteItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(frameItem);

        menu.Opened += (_, _) =>
        {
            var editable = _lvm?.IsEditable == true;
            var hasObject = _lvm?.SelectedObject != null;
            var hasSelection = hasObject || _lvm?.SelectedElement != null;

            addItem.IsEnabled = editable && _lvm?.MainViewModel.EngineVersion == EngineVersion.DarkAlliance;
            copyItem.IsEnabled = hasObject;
            pasteItem.IsEnabled = editable && ObjectClipboard.HasObject();
            duplicateItem.IsEnabled = editable && hasSelection;
            deleteItem.IsEnabled = editable && hasSelection;
            duplicateItem.Header = hasObject ? "Duplicate Object" : "Duplicate Element";
            deleteItem.Header = hasObject ? "Delete Object" : "Delete Element";
        };

        return menu;
    }
}
