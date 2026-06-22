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

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.TreeView;
using WorldExplorer.WorldDefs;
using System.Windows.Controls;

namespace WorldExplorer;

/// <summary>
/// Interaction logic for LevelView.xaml
/// </summary>
public partial class LevelView
{
    private LevelViewModel? _lvm;
    private ObjectDragGizmo? _gizmo;
    private ElementDragGizmo? _elementGizmo;

    public LevelView()
    {
        InitializeComponent();
        DataContextChanged += LevelView_DataContextChanged;
        viewport.MouseUp += viewport_MouseUp;
        viewport.PreviewKeyDown += Viewport_KeyDown;
        
        viewport.CalculateCursorPosition = true;        // ← NEW: enables paste-at-cursor
        viewport.ContextMenu = BuildViewportContextMenu(); // ← NEW
        _gizmo = new ObjectDragGizmo(viewport);
        _elementGizmo = new ElementDragGizmo(viewport); 
        _gizmo.ObjectMoved += () => propertiesArea.RefreshObjectFields(); 
        _elementGizmo.ElementMoved += () => propertiesArea.RefreshElementFields();
        
        
        viewport.AddHandler(
            UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler(Viewport_DragMouseUp),
            handledEventsToo: true);
        
        ElementSelected(null);
    }
    private void Viewport_DragMouseUp(object sender, MouseButtonEventArgs e)
    {
        _gizmo?.EndDrag();
        _elementGizmo?.EndDrag(); 
    }

    private void Viewport_KeyDown(object sender, KeyEventArgs e)
    {
        if (_lvm == null) return;
 
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
 
        switch (e.Key)
        {
            case Key.Z when ctrl && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                _lvm?.Redo();          // Ctrl+Shift+Z = redo
                e.Handled = true;
                break;
 
            case Key.Z when ctrl:
                _lvm?.Undo();
                e.Handled = true;
                break;
 
            case Key.Y when ctrl:
                _lvm?.Redo();
                e.Handled = true;
                break;
            
            case Key.L:
                // Toggle lighting (existing behaviour)
                _lvm.EnableLevelSpecifiedLights = !_lvm.EnableLevelSpecifiedLights;
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
                DuplicateSelectedObject();
                e.Handled = true;
                break;
 
            case Key.Delete:
                DeleteSelectedObject();
                e.Handled = true;
                break;
        }
    }

    private void LevelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(DataContext is LevelViewModel lvm))
        {
            // Cleared level view
            _lvm = null;
            return;
        }

        _lvm = lvm;
    }

    private Brush? TryGettingAmbientLightColor()
    {
        var ambientLight = _lvm?.ObjectManager.GetObjectByName("Ambient_Light");
        if (ambientLight == null)
        {
            return null;
        }

        return new SolidColorBrush(Color.FromRgb((byte)ambientLight.Floats[0], (byte)ambientLight.Floats[1],
            (byte)ambientLight.Floats[2]));
    }

    protected void OnSceneUpdated()
    {
        _gizmo?.Detach();
        _elementGizmo?.Detach(); 
        Background = TryGettingAmbientLightColor() ?? Brushes.White;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        viewport.CameraController.MoveSensitivity = 30;
        base.OnRender(drawingContext);
    }

    private void viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            var hitResult = GetHitTestResult(e.GetPosition(viewport));

            if (hitResult == null) return;

            var levelViewModel = (LevelViewModel)DataContext;
            var worldNode = levelViewModel.WorldNode;

            WorldElementTreeViewModel? selectedElement = null;

            if (worldNode == null)
            {
                ElementSelected(null);
                return;
            }

            var vod = levelViewModel.ObjectManager.HitTest(hitResult);

            if (vod != null)
            {
                ObjectSelected(vod);
                return;
            }

            if (levelViewModel.Scene != null)
            {
                for (var i = 0; i < worldNode.Children.Count; i++)
                {
                    if (levelViewModel.Scene[i + 2] == hitResult)
                    {
                        selectedElement = (WorldElementTreeViewModel)worldNode.Children[i];
                        break;
                    }
                }
            }

            ElementSelected(selectedElement);
        }
    }

    private void ElementSelected(WorldElementTreeViewModel? ele)
    {
        _gizmo?.Detach();
        if (_lvm != null)
        {
            _lvm.SelectedObject = null;
            _lvm.SelectedElement = ele;
            if (ele != null)
                _elementGizmo?.Attach(ele.WorldElement, _lvm);
            else
                _elementGizmo?.Detach();
        }

        // Expand after values have changed
        if (!editorExpander.IsExpanded)
        {
            editorExpander.IsExpanded = true;
        }
    }

    private void ObjectSelected(VisualObjectData obj)
    {
        if (_lvm != null)
        {
            _lvm.SelectedElement = null;
            _lvm.SelectedObject = obj;
            _elementGizmo?.Detach();
            _gizmo?.Attach(obj, _lvm);
        }

        // Expand after values have changed
        if (!editorExpander.IsExpanded)
        {
            editorExpander.IsExpanded = true;
        }
    }

    private ModelVisual3D? GetHitTestResult(Point location)
    {
        var result = VisualTreeHelper.HitTest(viewport, location);
        if (result is {VisualHit: ModelVisual3D})
        {
            var visual = (ModelVisual3D)result.VisualHit;
            return visual;
        }

        return null;
    }
    /// <summary>Copies the selected object to the system clipboard as JSON.</summary>
private void CopySelectedObject()
{
    var selected = _lvm?.SelectedObject;
    if (selected?.ObjectData == null) return;
 
    ObjectClipboard.Copy(selected.ObjectData);
}
 
/// <summary>
/// Pastes the clipboard object into the level.  Position priority:
///   1. The 3D cursor position (where the mouse is over level geometry),
///   2. otherwise the source position nudged by a couple of units so the
///      copy is visibly distinct from the original.
/// Remember: ObjectData.Floats are 4× the displayed world offset
/// (ObjectManager computes Offset = Floats / 4).
/// </summary>
private void PasteObject()
{
    if (_lvm == null) return;
 
    if (!ObjectClipboard.HasObject())
    {
        MessageBox.Show(
            "The clipboard does not contain a WorldExplorer object.\n" +
            "Select an object (Ctrl+Click) and press Ctrl+C first.",
            "Paste Object", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }
 
    var pasted = ObjectClipboard.TryPaste();
    if (pasted == null)
    {
        MessageBox.Show(
            "The clipboard content could not be read as an object.",
            "Paste Object", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        MessageBox.Show(
            $"'{pasted.Name}' was added to the level data but produced no " +
            "visual (some object types, such as black lights, render nothing).",
            "Paste Object", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
 
/// <summary>
/// Duplicates the selected object in place (with a small offset) without
/// touching the clipboard, and selects the copy.
/// </summary>
private void DuplicateSelectedObject()
{
    var selected = _lvm?.SelectedObject;
    if (_lvm == null || selected?.ObjectData == null) return;
 
    var clone = ObjectClipboard.Clone(selected.ObjectData);
    clone.Floats[0] += 8.0f;   // 2 world units × 4
    clone.Floats[1] += 8.0f;
 
    var vod = _lvm.AddObjectToLevel(clone);
    if (vod != null)
    {
        ObjectSelected(vod);
    }
}
 
/// <summary>Deletes the selected object from the level.</summary>
private void DeleteSelectedObject()
{
    var selected = _lvm?.SelectedObject;
    if (_lvm == null || selected == null) return;
 
    _lvm.DeleteObjectFromLevel(selected);
    ObjectSelected(null!);   // collapse selection in the properties panel
}
 
/// <summary>
/// Builds the viewport right-click menu.  Item enablement is refreshed each
/// time the menu opens, based on the current selection and clipboard state.
/// </summary>
private ContextMenu BuildViewportContextMenu()
{
    var copyItem      = new MenuItem { Header = "Copy Object\tCtrl+C" };
    var pasteItem     = new MenuItem { Header = "Paste Object\tCtrl+V" };
    var duplicateItem = new MenuItem { Header = "Duplicate Object\tCtrl+D" };
    var deleteItem    = new MenuItem { Header = "Delete Object\tDel" };
 
    copyItem.Click      += (_, _) => CopySelectedObject();
    pasteItem.Click     += (_, _) => PasteObject();
    duplicateItem.Click += (_, _) => DuplicateSelectedObject();
    deleteItem.Click    += (_, _) => DeleteSelectedObject();
 
    var menu = new ContextMenu();
    menu.Items.Add(copyItem);
    menu.Items.Add(pasteItem);
    menu.Items.Add(duplicateItem);
    menu.Items.Add(new Separator());
    menu.Items.Add(deleteItem);
 
    menu.Opened += (_, _) =>
    {
        var hasSelection = _lvm?.SelectedObject != null;
        copyItem.IsEnabled      = hasSelection;
        duplicateItem.IsEnabled = hasSelection;
        deleteItem.IsEnabled    = hasSelection;
        pasteItem.IsEnabled     = _lvm != null && ObjectClipboard.HasObject();
    };
 
    return menu;
}
}