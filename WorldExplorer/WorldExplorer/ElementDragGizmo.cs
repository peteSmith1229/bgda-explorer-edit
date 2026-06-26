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
using JetBlackEngineLib.Data.World;
using System;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using WorldExplorer.Win3D;

namespace WorldExplorer;

/// <summary>
/// Shows a HelixToolkit translate gizmo on the currently selected world element
/// and lets the user drag it along X / Y / Z.  The sibling of
/// <see cref="ObjectDragGizmo"/>, with two differences:
///
/// <list type="bullet">
///   <item>An element's world position <b>is</b> <see cref="WorldElement.Position"/>
///         (no ×4 scaling — the on-disk ×16 is applied later by
///         <c>WorldElementPatcher</c>).</item>
///   <item>The live visual is rebuilt from the element via
///         <see cref="SceneTransforms.BuildElementTransform"/>, looked up through
///         <c>LevelViewModel.GetElementVisual</c> (the element's
///         <see cref="ModelVisual3D"/> is recreated on every scene rebuild, so it
///         is resolved fresh each move rather than cached).</item>
/// </list>
///
/// As with objects: the first movement of a drag captures one undo snapshot,
/// and the edit is committed on mouse-up.
/// </summary>
internal sealed class ElementDragGizmo
{
    private static readonly DependencyPropertyDescriptor TargetTransformDpd =
        DependencyPropertyDescriptor.FromProperty(
            TranslateGizmoVisual.TargetTransformProperty, typeof(TranslateGizmoVisual));

    private readonly HelixViewport3D _viewport;

    private TranslateGizmoVisual? _manip;
    private WorldElement? _element;
    private LevelViewModel? _lvm;
    private Point3D _startPos;          // gizmo position = element VISUAL origin
    private Vector3D _origPosition;     // element.Position captured at attach
    private Matrix3D _rotInverse;       // inverse of the element's rotation
    private bool _dragStarted;

    public ElementDragGizmo(HelixViewport3D viewport) => _viewport = viewport;
    
    /// <summary>
    /// Raised whenever a drag changes the element's position, so the properties
    /// panel can refresh its coordinate fields to match. Without this, the
    /// fields keep their pre-drag snapshot and "Apply Changes" reverts the move.
    /// </summary>
    public event Action? ElementMoved;

    /// <summary>Shows the gizmo on <paramref name="element"/>, replacing any current gizmo.</summary>
    public void Attach(WorldElement element, LevelViewModel lvm)
    {
        Detach();

        _element = element;
        _lvm = lvm;
 
        // The element renders at its VISUAL origin (Position × rotation), the
        // transform matrix's offset row — NOT raw Position (BuildElementTransform
        // translates then rotates). Place the gizmo there, and keep the rotation's
        // inverse so a world-space drag can be mapped back onto Position.
        var m = SceneTransforms.BuildElementTransform(element).Value;
        _origPosition = new Vector3D(element.Position.X, element.Position.Y, element.Position.Z);
        _rotInverse = new Matrix3D(
            m.M11, m.M12, m.M13, 0,
            m.M21, m.M22, m.M23, 0,
            m.M31, m.M32, m.M33, 0,
            0,     0,     0,     1);   // rotation (linear) part only
        _rotInverse.Invert();
        _startPos = new Point3D(m.OffsetX, m.OffsetY, m.OffsetZ);

        var scale = App.Settings.Get("Editor.GizmoScale", 1.0);
        var size = 10.0;
        if (!lvm.WorldBounds.IsEmpty)
        {
            var span = Math.Max(lvm.WorldBounds.SizeX,
                Math.Max(lvm.WorldBounds.SizeY, lvm.WorldBounds.SizeZ));
            size = Math.Max(2.0, span * 0.08);
        }
        size *= scale / 2;
 
        // Translate-only gizmo whose arrow length scales with the level and the
        // Editor.GizmoScale setting. (CombinedManipulator can't size its arrows.)
        _manip = new TranslateGizmoVisual(length: size, diameter: size * 0.1, position: _startPos);

        TargetTransformDpd.AddValueChanged(_manip, OnTransformChanged);
        _viewport.Children.Add(_manip);
        _dragStarted = false;
    }

    /// <summary>Removes the gizmo (deselect, object selection, or level change).</summary>
    public void Detach()
    {
        if (_manip != null)
        {
            TargetTransformDpd.RemoveValueChanged(_manip, OnTransformChanged);
            _viewport.Children.Remove(_manip);
            _manip = null;
        }
        _element = null;
        _lvm = null;
        _dragStarted = false;
    }

    private void OnTransformChanged(object? sender, EventArgs e)
    {
        if (_manip == null || _element == null || _lvm == null) return;

        // Capture one undo snapshot at the first movement of this drag,
        // BEFORE mutating element.Position.
        if (!_dragStarted)
        {
            _lvm.PushUndoSnapshot();
            _dragStarted = true;
        }

        // World-space drag offset from the gizmo's start position.
        var d = _manip.TargetTransform.Transform(new Point3D(0, 0, 0));
 
        // The element renders as (p + Position) × rot, so to move the VISUAL by d
        // we change Position by d × rot⁻¹ — then the rebuilt transform shifts the
        // mesh by exactly d. (For unrotated elements rot⁻¹ = identity, so this is
        // the old Position += d behaviour.)
        var dLocal = _rotInverse.Transform(new Vector3D(d.X, d.Y, d.Z));
        _element.Position = _origPosition + dLocal;
 
        var visual = _lvm.GetElementVisual(_element);
        if (visual != null)
            visual.Transform = SceneTransforms.BuildElementTransform(_element);
 
        ElementMoved?.Invoke();
    }

    /// <summary>
    /// Called on viewport mouse-up.  If a drag happened, the element position
    /// is already updated, so this just rebuilds the scene and commits.
    /// Harmless no-op for a plain click.
    /// </summary>
    public void EndDrag()
    {
        if (!_dragStarted || _lvm == null || _element == null)
            return;

        _dragStarted = false;
        _lvm.FinalizeEdit();
    }
}
