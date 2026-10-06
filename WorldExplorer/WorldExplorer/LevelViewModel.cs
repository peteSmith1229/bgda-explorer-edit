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
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Textures;
using JetBlackEngineLib.Data.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.Logging;
using WorldExplorer.TreeView;
using WorldExplorer.Win3D;
using WorldExplorer.WorldDefs;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>
/// Frozen element geometry keyed by (model, texture) identity. Duplicated
/// elements share their source's model and texture, so they share geometry.
/// </summary>
public sealed class ElementGeometryCache
{
    private readonly Dictionary<(Model Model, BitmapSource? Texture), Model3D> _items = new();

    /// <summary>
    /// Returns the frozen geometry for <paramref name="model"/> textured with
    /// <paramref name="texture"/>, building it on first use. Safe to call from
    /// a worker thread provided the texture is frozen (frozen WPF objects can
    /// be shared across threads).
    /// </summary>
    public Model3D Get(Model model, BitmapSource? texture)
    {
        var key = (model, texture);
        if (_items.TryGetValue(key, out var geometry)) return geometry;

        geometry = Conversions.CreateModel3D(model.MeshList, texture, null, 0);
        if (geometry.CanFreeze) geometry.Freeze();
        _items[key] = geometry;
        return geometry;
    }

    /// <summary>Builds geometry for every element of <paramref name="worldData"/>.</summary>
    public static ElementGeometryCache BuildFor(WorldData worldData)
    {
        var cache = new ElementGeometryCache();
        foreach (var element in worldData.WorldElements)
        {
            var model = worldData.GetElementModel(element);
            if (model != null) cache.Get(model, element.Texture);
        }
        return cache;
    }
}

public class LevelViewModel : BaseViewModel
{
    /// <summary>
    /// Editor state that belongs to one level and survives switching to
    /// another level and back: undo history, the structural-edit flag and the
    /// scene caches.
    /// </summary>
    private sealed class LevelSession
    {
        public LevelEditHistory History { get; } = new();

        /// <summary>
        /// True once an element has been added or removed. While set, commits go
        /// through <see cref="WorldElementPatcher.Rebuild"/> (which can change the
        /// element count) instead of the surgical in-place patch.
        /// </summary>
        public bool ElementsDirty { get; set; }

        public ElementGeometryCache Geometry { get; set; } = new();

        /// <summary>Sky-dome geometry, decoded once per level.</summary>
        public List<Model3D>? Dome { get; set; }
    }

    private LmpFile? _domeLmp;
    private bool _enableLights = true;
    private string? _infoText;
    private List<ModelVisual3D>? _scene;
    private WorldElementTreeViewModel? _selectedElement;
    private VisualObjectData? _selectedObject;
    private Rect3D _worldBounds = Rect3D.Empty;
    private WorldData? _worldData;
    private WorldFileTreeViewModel? _worldNode;
    private LevelSession _session = new();
    private readonly Dictionary<WorldElement, ModelVisual3D> _elementVisuals = new();

    public Rect3D WorldBounds
    {
        get => _worldBounds;
        private set
        {
            _worldBounds = value;
            OnPropertyChanged(nameof(WorldBounds));
        }
    }

    public WorldFileTreeViewModel? WorldNode
    {
        get => _worldNode;
        set
        {
            _worldNode = value;
            OnPropertyChanged(nameof(WorldNode));
            OnPropertyChanged(nameof(IsEditable));
        }
    }

    public WorldData? WorldData
    {
        get => _worldData;
        set
        {
            _worldData = value;
            NewWorldLoaded(null);
            OnPropertyChanged(nameof(WorldData));
        }
    }

    /// <summary>
    /// False for read-only previews (BoS levels shown from a DDF entity or a
    /// CLP archive): those can be inspected but edits can't be saved back.
    /// </summary>
    public bool IsEditable => WorldNode != null && WorldNode.LmpFileProperty is not ClpFile;

    /// <summary>Summary shown in the level view's status overlay.</summary>
    public string? InfoText
    {
        get => _infoText;
        set
        {
            _infoText = value;
            OnPropertyChanged(nameof(InfoText));
        }
    }

    public List<ModelVisual3D>? Scene
    {
        get => _scene;
        set
        {
            _scene = value;
            OnPropertyChanged(nameof(Scene));
        }
    }

    public bool EnableLevelSpecifiedLights
    {
        get => _enableLights;
        set
        {
            _enableLights = value;
            RebuildScene();
            OnPropertyChanged(nameof(EnableLevelSpecifiedLights));
        }
    }

    public VisualObjectData? SelectedObject
    {
        get => _selectedObject;
        set
        {
            _selectedObject = value;
            OnPropertyChanged(nameof(SelectedObject));
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    /// <summary>
    /// Gets or sets the currently selected world element.
    /// </summary>
    public WorldElementTreeViewModel? SelectedElement
    {
        get => _selectedElement;
        set
        {
            _selectedElement = value;
            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => _selectedObject != null || _selectedElement != null;

    public ObjectManager ObjectManager { get; }

    public LevelViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
    {
        ObjectManager = new ObjectManager(this);
    }

    public event EventHandler? SceneUpdated;

    /// <summary>
    /// Shows a decoded level. <paramref name="node"/> is the editable .world
    /// tree node, or null for a read-only preview. <paramref name="geometry"/>
    /// is optional element geometry already built on a worker thread.
    /// </summary>
    public void SetWorld(WorldFileTreeViewModel? node, WorldData? worldData, ElementGeometryCache? geometry = null)
    {
        _worldNode = node;
        _worldData = worldData;
        OnPropertyChanged(nameof(WorldNode));
        OnPropertyChanged(nameof(IsEditable));
        NewWorldLoaded(geometry);
        OnPropertyChanged(nameof(WorldData));
    }

    public void RebuildScene()
    {
        _elementVisuals.Clear();
        List<ModelVisual3D> scene = new();
        AddLights(EnableLevelSpecifiedLights, scene);

        var worldBounds = Rect3D.Empty;
        var elementCount = 0;

        if (_worldData != null)
        {
            foreach (var element in _worldData.WorldElements)
            {
                if (element.IsDeleted) continue;
                var elementModel = _worldData.GetElementModel(element);
                if (elementModel == null) continue;

                // Geometry is built once per level and shared (frozen) across
                // rebuilds; only the cheap per-element visual + transform is new.
                var model3D = _session.Geometry.Get(elementModel, element.Texture);
                ModelVisual3D mv3d = new()
                {
                    Content = model3D,
                    Transform = SceneTransforms.BuildElementTransform(element)
                };

                worldBounds.Union(model3D.Bounds);
                _elementVisuals[element] = mv3d;
                scene.Add(mv3d);
                elementCount++;
            }
        }

        ObjectManager.AddObjectsToScene(scene);

        AddSkyDomeModels(scene);

        WorldBounds = worldBounds;
        Scene = scene;

        InfoText = _worldData == null
            ? null
            : $"{Plural.Of(elementCount, "element")} · {Plural.Of(ObjectManager.Objects.Count, "object")}" +
              (IsEditable ? "" : " · read-only preview");
    }

    private void ResetState()
    {
        _domeLmp = null;
    }

    private void NewWorldLoaded(ElementGeometryCache? geometry)
    {
        ResetState();

        // Restore this level's editor session (undo history, structural-edit
        // flag, caches) or start a fresh one. Previews get a throwaway session.
        if (_worldNode?.EditorState is LevelSession existing)
        {
            _session = existing;
        }
        else
        {
            _session = new LevelSession();
            if (_worldNode != null) _worldNode.EditorState = _session;
        }

        if (geometry != null) _session.Geometry = geometry;

        SelectedObject = null;
        SelectedElement = null;
        LoadObjects();
        LoadSkyDome();
        RebuildScene();
        RaiseHistoryChanged();

        SceneUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void LoadObjects()
    {
        if (WorldNode == null)
        {
            ObjectManager.ReplaceAllObjects(Array.Empty<ObjectData>());
            return;
        }

        var lmpFile = WorldNode.LmpFileProperty;

        if (!lmpFile.Directory.TryGetValue("objects.ob", out var obNode))
        {
            ObjectManager.ReplaceAllObjects(Array.Empty<ObjectData>());
            return;
        }

        // If an edited objects.ob has been queued (paste, duplicate, delete,
        // apply-changes), reload from THOSE bytes — otherwise a scene reload
        // would silently revert the object list to the original file content.
        if (lmpFile.PendingEdits.TryGetValue("objects.ob", out var pendingBytes))
        {
            ObjectManager.LoadScene(pendingBytes, 0, pendingBytes.Length);
        }
        else
        {
            ObjectManager.LoadScene(lmpFile.FileData, obNode.StartOffset, obNode.Length);
        }
    }

    private void LoadSkyDome()
    {
        var gobFile = WorldNode?.Parent?.Parent as GobTreeViewModel;
        var domeFile = gobFile?.Children.OfType<LmpTreeViewModel>()
            .FirstOrDefault(e => e.Label.Equals("dome.lmp", StringComparison.OrdinalIgnoreCase));
        if (domeFile == null)
        {
            _domeLmp = null;
            return;
        }

        if (domeFile.HasDummyChild)
        {
            domeFile.ForceLoadChildren();
        }

        _domeLmp = domeFile.LmpFileProperty;
    }

    private void AddSkyDomeModels(List<ModelVisual3D> scene)
    {
        if (_domeLmp == null)
        {
            return;
        }

        // Decoding the dome on every rebuild (each drag, undo, apply…) was a
        // large share of edit latency; decode once per level instead.
        _session.Dome ??= DecodeSkyDome(_domeLmp);

        foreach (var model in _session.Dome)
        {
            scene.Add(new ModelVisual3D { Content = model });
        }
    }

    private static List<Model3D> DecodeSkyDome(LmpFile domeLmp)
    {
        var models = new List<Model3D>();
        var vifChildren = domeLmp.Directory.Keys
            .Where(e => e.EndsWith(".vif", StringComparison.OrdinalIgnoreCase));

        foreach (var vifFileName in vifChildren)
        {
            if (!domeLmp.Directory.TryGetValue(vifFileName, out var vifEntry))
                continue;
            var texFilename = Path.GetFileNameWithoutExtension(vifFileName) + ".tex";

            if (!domeLmp.Directory.TryGetValue(texFilename.ToLowerInvariant(), out var texEntry))
                // Couldn't find the tex file, ignore this vif entry
                continue;

            var selectedNodeImage =
                TexDecoder.Decode(domeLmp.FileData.AsSpan().Slice(texEntry.StartOffset, texEntry.Length));
            StringLogger log = new();

            Model model = new(VifDecoder.Decode(
                log,
                domeLmp.FileData.AsSpan().Slice(vifEntry.StartOffset, vifEntry.Length),
                selectedNodeImage?.PixelWidth ?? 0,
                selectedNodeImage?.PixelHeight ?? 0));

            var newModel = Conversions.CreateModel3D(model.MeshList, selectedNodeImage);
            if (newModel.CanFreeze) newModel.Freeze();
            models.Add(newModel);
        }

        return models;
    }

    private void AddLights(bool enableLevelSpecifiedLights, List<ModelVisual3D> scene)
    {
        var ambientColor = Color.FromRgb(0x80, 0x80, 0x80);
        var directionalColor = Color.FromRgb(0x80, 0x80, 0x80);
        Vector3D directionalAngle = new(0, -1, -1);

        // Attempt to find the correct values from objects
        if (enableLevelSpecifiedLights)
        {
            if (ObjectManager.TryGetObjectByName("Ambient_Light", out var ambientLightObj))
            {
                ambientColor = Color.FromRgb((byte)ambientLightObj.Floats[0], (byte)ambientLightObj.Floats[1],
                    (byte)ambientLightObj.Floats[2]);
            }

            if (ObjectManager.TryGetObjectByName("Directional_Light", out var dirLightColorObj))
            {
                directionalColor = Color.FromRgb((byte)dirLightColorObj.Floats[0],
                    (byte)dirLightColorObj.Floats[1], (byte)dirLightColorObj.Floats[2]);
            }

            if (ObjectManager.TryGetObjectByName("Directional_LightD", out var dirLightAngleObj))
            {
                directionalAngle = new Vector3D(-dirLightAngleObj.Floats[0], -dirLightAngleObj.Floats[1],
                    -dirLightAngleObj.Floats[2]);
            }
        }

        ModelVisual3D ambientLight = new() {Content = new AmbientLight(ambientColor)};
        ModelVisual3D directionalLight = new() {Content = new DirectionalLight(directionalColor, directionalAngle)};
        scene.Add(ambientLight);
        scene.Add(directionalLight);
    }

    /// <summary>
    /// Adds <paramref name="newObject"/> to the level, rebuilds the scene,
    /// queues the updated objects.ob into the archive's pending-edit layer, and
    /// returns the new visual (or null if the object type produced no visual,
    /// e.g. a black light).
    /// </summary>
    public VisualObjectData? AddObjectToLevel(ObjectData newObject)
    {
        PushUndoSnapshot();
        var vod = ObjectManager.AddObject(newObject);
        RebuildScene();

        if (CommitChangesToArchive())
        {
            MainViewModel.MainWindow.UpdateTitle();
        }

        return vod;
    }

    /// <summary>True when there is a level edit that can be undone / redone.</summary>
    public bool CanUndo => _session.History.CanUndo;
    public bool CanRedo => _session.History.CanRedo;

    private void RaiseHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    /// <summary>
    /// Records the current level state so the next edit can be undone.  MUST be
    /// called at the START of every edit operation, before anything is mutated.
    /// No-op when no editable world is loaded.
    /// </summary>
    public void PushUndoSnapshot()
    {
        if (!IsEditable) return;
        _session.History.PushUndo(CaptureMemento());
        RaiseHistoryChanged();
    }

    /// <summary>Reverts the most recent edit.</summary>
    public void Undo()
    {
        if (!IsEditable) return;
        var restored = _session.History.Undo(CaptureMemento());
        if (restored != null) RestoreMemento(restored);
        RaiseHistoryChanged();
    }

    /// <summary>Re-applies the most recently undone edit.</summary>
    public void Redo()
    {
        if (!IsEditable) return;
        var restored = _session.History.Redo(CaptureMemento());
        if (restored != null) RestoreMemento(restored);
        RaiseHistoryChanged();
    }

    private LevelEditMemento CaptureMemento()
    {
        // Clone objects so the snapshot is never aliased to the live list.
        var objects = ObjectManager.Objects
            .Select(ObjectClipboard.Clone)
            .ToList();

        var transforms = new Dictionary<int, ElementTransform>();
        if (_worldData != null)
        {
            foreach (var el in _worldData.WorldElements)
            {
                transforms[el.ElementIndex] = new ElementTransform
                {
                    Position     = el.Position,
                    BoundingBox  = el.BoundingBox,
                    NegYaxis     = el.NegYaxis,
                    SinAlpha     = el.SinAlpha,
                    CosAlpha     = el.CosAlpha,
                    UsesRotFlags = el.UsesRotFlags,
                    XyzRotFlags  = el.XyzRotFlags,
                };
            }
        }

        return new LevelEditMemento(objects, transforms);
    }

    private void RestoreMemento(LevelEditMemento m)
    {
        // Hand ObjectManager fresh clones so later in-place edits can't corrupt
        // the stored snapshot.
        ObjectManager.ReplaceAllObjects(
            m.Objects.Select(ObjectClipboard.Clone));

        // Write element transforms back into the live elements.
        if (_worldData != null)
        {
            foreach (var el in _worldData.WorldElements)
            {
                if (!m.ElementTransforms.TryGetValue(el.ElementIndex, out var t)) continue;
                el.Position     = t.Position;
                el.BoundingBox  = t.BoundingBox;
                el.NegYaxis     = t.NegYaxis;
                el.SinAlpha     = t.SinAlpha;
                el.CosAlpha     = t.CosAlpha;
                el.UsesRotFlags = t.UsesRotFlags;
                el.XyzRotFlags  = t.XyzRotFlags;
            }
        }

        // The previous selection points at now-replaced instances — clear it so
        // the properties panel doesn't act on stale references.
        SelectedObject = null;
        SelectedElement = null;

        RebuildScene();

        if (CommitChangesToArchive())
            MainViewModel.MainWindow.UpdateTitle();
    }

    /// <summary>
    /// Deletes <paramref name="vod"/> from the level, rebuilds the scene, and
    /// queues the updated objects.ob into the archive's pending-edit layer.
    /// </summary>
    public void DeleteObjectFromLevel(VisualObjectData vod)
    {
        PushUndoSnapshot();
        ObjectManager.DeleteObject(vod);

        if (SelectedObject == vod)
        {
            SelectedObject = null;
        }

        RebuildScene();

        if (CommitChangesToArchive())
        {
            MainViewModel.MainWindow.UpdateTitle();
        }
    }

    /// <summary>
    /// Duplicates the selected world element in place (small offset), sharing the
    /// source's geometry, then selects the copy. Changes the element count, so the
    /// .world is re-serialised with a relocated element array on commit.
    /// </summary>
    public void DuplicateSelectedElement()
    {
        var src = SelectedElement?.WorldElement;
        if (_worldData == null || WorldNode == null || src == null || !IsEditable) return;

        PushUndoSnapshot();

        var clone = src.Clone();
        clone.Position = new Vector3D(src.Position.X + 2.0, src.Position.Y + 2.0, src.Position.Z);
        // Prefer a freed (deleted) slot: the game only draws the original slot
        // range, so a clone appended beyond it is editor-only. In a freed slot it
        // renders in-game like any edited element, and the file keeps its shape.
        var freeIdx = -1;
        for (var i = 0; i < _worldData.WorldElements.Count; i++)
            if (_worldData.WorldElements[i].IsDeleted) { freeIdx = i; break; }

        if (freeIdx >= 0)
        {
            clone.ElementIndex  = freeIdx;
            clone.OriginalIndex = freeIdx;   // owns that original slot now →
            // cell lists / 0x20 stay untouched
            _worldData.WorldElements[freeIdx] = clone;
        }
        else
        {
            // No freed slot: append. Renders in the EDITOR only — the game will
            // not draw past its fixed element count (engine-side, not in .world).
            clone.ElementIndex = _worldData.WorldElements.Count;
            _worldData.WorldElements.Add(clone);
        }

        _session.ElementsDirty = true;
        FinalizeEdit();             // RebuildScene + Rebuild() + renumber + title

        // Rebuild the tree nodes (now renumbered) and select the new element so
        // its gizmo appears immediately.
        WorldNode.ReloadChildren();
        var cloneNode = WorldNode.Children
            .OfType<WorldElementTreeViewModel>()
            .FirstOrDefault(n => ReferenceEquals(n.WorldElement, clone));
        if (cloneNode != null)
            cloneNode.IsSelected = true;
    }

    /// <summary>
    /// Deletes the selected world element. Changes the element count, so the
    /// .world is re-serialised with a relocated element array on commit.
    /// </summary>
    public void DeleteSelectedElement()
    {
        var target = SelectedElement?.WorldElement;
        if (_worldData == null || WorldNode == null || target == null || !IsEditable) return;

        PushUndoSnapshot();

        target.IsDeleted = true;   // keep as a dead slot — no renumber/reorder
        SelectedElement = null;     // detaches the gizmo via the LevelView observer

        _session.ElementsDirty = true;
        FinalizeEdit();             // RebuildScene + Rebuild() + renumber + title

        WorldNode.ReloadChildren(); // refresh the tree with renumbered labels
    }

    /// <summary>
    /// Queues all level edits (moved world elements and edited objects) into the
    /// world LMP's pending-edit layer so they are included the next time the
    /// archive is saved (File → Save GOB… / Save Archive…).
    ///
    /// Two entries are regenerated:
    ///   1. The <c>.world</c> entry — element transforms are surgically patched
    ///      in place via <see cref="WorldElementPatcher"/>; everything else in
    ///      the file stays byte-identical.
    ///   2. The <c>objects.ob</c> entry — fully re-encoded from the in-memory
    ///      object list via <see cref="ObEncoder"/>.
    ///
    /// Returns true if anything was queued.
    /// </summary>
    public bool CommitChangesToArchive()
    {
        if (WorldNode == null || !IsEditable)
            return false;   // No editable world loaded (e.g. BoS preview).

        var lmpFile = WorldNode.LmpFileProperty;
        var queued  = false;

        // ── 1. Patch element transforms back into the .world entry ────────────
        if (_worldData != null && lmpFile.Directory.TryGetValue(WorldNode.Label, out var worldEntry))
        {
            var engineVersion = MainViewModel.World?.EngineVersion
                                ?? App.Settings.Get<EngineVersion>("Core.EngineVersion");

            byte[] newWorldBytes;
            if (_session.ElementsDirty)
            {
                // An element was added/removed → rebuild the array (relocated to
                // the end of the file) from the PRISTINE bytes + the current list.
                // Starting from the original (not the pending) stops the appended
                // array from accumulating across successive edits.
                var originalBytes = new byte[worldEntry.Length];
                Buffer.BlockCopy(lmpFile.FileData, worldEntry.StartOffset,
                                 originalBytes, 0, worldEntry.Length);

                newWorldBytes = WorldElementPatcher.Rebuild(originalBytes,
                    _worldData.WorldElements, engineVersion);

                // Renumber so each element's slot matches its list position
                // (keeps any later surgical patch addressing the right records).
                for (var i = 0; i < _worldData.WorldElements.Count; i++)
                    _worldData.WorldElements[i].ElementIndex = i;

                // Register any duplicated elements in the 0x18 per-cell render lists
                // so the game draws them (renderer walks these lists, not numElements).
                newWorldBytes = WorldElementPatcher.RebuildCellLists(newWorldBytes,
                    _worldData.WorldElements, engineVersion);

                // Reused-slot clones: give each its source's culling footprint.
                WorldElementPatcher.PatchReusedSlotTopo(newWorldBytes,
                    _worldData.WorldElements, engineVersion);
            }
            else
            {
                // Transform-only edit → surgical in-place patch (unchanged path).
                byte[] baseBytes;
                if (lmpFile.PendingEdits.TryGetValue(WorldNode.Label, out var pendingWorld))
                {
                    baseBytes = pendingWorld;
                }
                else
                {
                    baseBytes = new byte[worldEntry.Length];
                    Buffer.BlockCopy(lmpFile.FileData, worldEntry.StartOffset,
                                     baseBytes, 0, worldEntry.Length);
                }

                newWorldBytes = WorldElementPatcher.Patch(baseBytes,
                    _worldData.WorldElements, engineVersion);

                // Slide the 0x20 footprint first (needs the OLD record bounds)...
                WorldElementPatcher.PatchTopoBounds(newWorldBytes,
                    _worldData.WorldElements, engineVersion);

                // Also move each element's world-space bounds so collision/culling
                // tracks the mesh (record holds a static AABB we'd otherwise leave
                // behind). No-op for elements that didn't move.
                WorldElementPatcher.PatchBounds(newWorldBytes,
                    _worldData.WorldElements, engineVersion);
            }

            lmpFile.ReplaceEntry(WorldNode.Label, newWorldBytes);
            queued = true;
        }

        // ── 2. Re-encode objects.ob from the in-memory object list ────────────
        if (lmpFile.Directory.TryGetValue("objects.ob", out var obEntry)
            && ObjectManager.Objects.Count > 0)
        {
            // Preserve the opaque flags i16 at +0x02 of the ORIGINAL entry —
            // ObDecoder skips it on read and the editor never changes it.
            var flags = BitConverter.ToInt16(lmpFile.FileData, obEntry.StartOffset + 2);

            var encoded = ObEncoder.Encode(ObjectManager.Objects, flags);
            lmpFile.ReplaceEntry("objects.ob", encoded);
            queued = true;
        }

        return queued;
    }

    /// <summary>
    /// Rebuilds the scene and commits pending edits to the archive, refreshing the
    /// title.  Called after a viewport drag completes.
    /// </summary>
    public void FinalizeEdit()
    {
        RebuildScene();
        if (CommitChangesToArchive())
            MainViewModel.MainWindow.UpdateTitle();
    }

    /// <summary>
    /// Returns the <see cref="ModelVisual3D"/> built for <paramref name="element"/>
    /// in the most recent RebuildScene, or null if the element has no visual
    /// (e.g. no model). The map is rebuilt every RebuildScene, so this always
    /// reflects the current scene.
    /// </summary>
    public ModelVisual3D? GetElementVisual(WorldElement element)
        => _elementVisuals.TryGetValue(element, out var v) ? v : null;

    /// <summary>
    /// Reverse of <see cref="GetElementVisual"/>: returns the element whose scene
    /// visual is <paramref name="visual"/>, or null. Lets the viewport hit-test
    /// resolve a clicked model to its element without assuming the scene and tree
    /// are index-aligned (they diverge whenever an element is hidden, e.g. deleted).
    /// </summary>
    public WorldElement? GetElementForVisual(ModelVisual3D visual)
    {
        foreach (var kv in _elementVisuals)
            if (ReferenceEquals(kv.Value, visual))
                return kv.Key;
        return null;
    }
}
