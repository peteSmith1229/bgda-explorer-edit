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
using System;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using WorldExplorer.WorldDefs;

namespace WorldExplorer;

/// <summary>
/// Shows a HelixToolkit translate gizmo (a <see cref="CombinedManipulator"/>
/// with rotation disabled) on the currently selected level object and lets the
/// user drag it along the X / Y / Z axes.
///
/// <para>Behaviour:</para>
/// <list type="bullet">
///   <item>While dragging, only the selected object's own visual is moved
///         (via <see cref="VisualObjectData.UpdateTransform"/>) — no scene
///         rebuild, so it stays smooth on large levels.</item>
///   <item>The first movement of each drag captures one undo snapshot, so a
///         whole drag is a single Ctrl+Z.</item>
///   <item>On mouse-up the final position is written to the object data
///         (<c>Floats = worldPos × 4</c>), the scene is rebuilt, and the edit
///         is committed to the archive's pending-edit layer.</item>
/// </list>
///
/// <para><b>Coordinate mapping:</b> an object's world position is
/// <c>Floats / 4</c> (the convention in <c>ObjectManager.ParseObject</c>), so
/// dragging to a world position <c>p</c> stores <c>Floats = p × 4</c>.</para>
///
/// <para><b>HelixToolkit API touchpoints</b> (verify against your version if it
/// doesn't behave): <see cref="CombinedManipulator"/> with
/// <c>CanRotateX/Y/Z</c>, <c>Diameter</c>, <c>Position</c>, and
/// <c>TargetTransformProperty</c>; the gizmo updates <c>TargetTransform</c> as
/// it is dragged, which we observe via a <see cref="DependencyPropertyDescriptor"/>.</para>
/// </summary>
internal sealed class ObjectDragGizmo
{
    // Lets us observe the manipulator's TargetTransform as the user drags it.
    private static readonly DependencyPropertyDescriptor TargetTransformDpd =
        DependencyPropertyDescriptor.FromProperty(
            TranslateGizmoVisual.TargetTransformProperty, typeof(TranslateGizmoVisual));

    private readonly HelixViewport3D _viewport;

    private TranslateGizmoVisual? _manip;
    private VisualObjectData? _target;
    private LevelViewModel? _lvm;
    private Point3D _startPos;       // object world position when the gizmo appeared
    private Vector3D _lastOffset;    // most recent dragged world position
    private bool _dragStarted;       // has TargetTransform changed since attach?

    public ObjectDragGizmo(HelixViewport3D viewport) => _viewport = viewport;
    
    /// <summary>
    /// Raised after a drag writes the final position into the object data, so
    /// the properties panel can refresh its Float fields to match.
    /// </summary>
    public event Action? ObjectMoved;

    /// <summary>
    /// Shows the gizmo on <paramref name="target"/>, replacing any current
    /// gizmo.  No-op for objects with no data.
    /// </summary>
    public void Attach(VisualObjectData target, LevelViewModel lvm)
    {
        Detach();
        if (target.ObjectData == null) return;

        _target = target;
        _lvm = lvm;

        _startPos = new Point3D(
            target.ObjectData.Floats[0] / 4.0,
            target.ObjectData.Floats[1] / 4.0,
            target.ObjectData.Floats[2] / 4.0);
        _lastOffset = new Vector3D(_startPos.X, _startPos.Y, _startPos.Z);

        // Size the gizmo relative to the level so it's grabbable but not huge.
        var scale = App.Settings.Get("Editor.GizmoScale", 1.0);
        var size = 10.0;
        if (!lvm.WorldBounds.IsEmpty)
        {
            var span = Math.Max(lvm.WorldBounds.SizeX,
                Math.Max(lvm.WorldBounds.SizeY, lvm.WorldBounds.SizeZ));
            size = Math.Max(2.0, span * 0.08);
        }
        size *= scale;
 
        // Translate-only gizmo whose arrow length scales with the level and the
        // Editor.GizmoScale setting. (CombinedManipulator can't size its arrows.)
        _manip = new TranslateGizmoVisual(length: size, diameter: size * 0.1, position: _startPos);

        TargetTransformDpd.AddValueChanged(_manip, OnTransformChanged);
        _viewport.Children.Add(_manip);
        _dragStarted = false;
    }

    /// <summary>Removes the gizmo (deselect, element selection, or level change).</summary>
    public void Detach()
    {
        if (_manip != null)
        {
            TargetTransformDpd.RemoveValueChanged(_manip, OnTransformChanged);
            _viewport.Children.Remove(_manip);
            _manip = null;
        }
        _target = null;
        _lvm = null;
        _dragStarted = false;
    }

    private void OnTransformChanged(object? sender, EventArgs e)
    {
        if (_manip == null || _target?.ObjectData == null) return;

        // Capture one undo snapshot at the first movement of this drag.
        if (!_dragStarted)
        {
            _lvm?.PushUndoSnapshot();
            _dragStarted = true;
        }

        // TargetTransform accumulates from identity since the gizmo appeared;
        // applying it to the origin gives the cumulative drag offset.
        var d = _manip.TargetTransform.Transform(new Point3D(0, 0, 0));
        _lastOffset = new Vector3D(_startPos.X + d.X, _startPos.Y + d.Y, _startPos.Z + d.Z);

        // Live feedback: move only this object's visual (cheap), no commit yet.
        _target.Offset = _lastOffset;
        _target.UpdateTransform();
    }

    /// <summary>
    /// Called on viewport mouse-up.  If a drag happened, writes the final
    /// position into the object data, rebuilds the scene, and commits.
    /// Harmless no-op for a plain click (no drag).
    /// </summary>
    public void EndDrag()
    {
        if (!_dragStarted || _lvm == null || _target?.ObjectData == null)
            return;

        _dragStarted = false;

        _target.ObjectData.Floats[0] = (float)(_lastOffset.X * 4.0);
        _target.ObjectData.Floats[1] = (float)(_lastOffset.Y * 4.0);
        _target.ObjectData.Floats[2] = (float)(_lastOffset.Z * 4.0);
        
        ObjectMoved?.Invoke();

        _lvm.FinalizeEdit();
    }
}
