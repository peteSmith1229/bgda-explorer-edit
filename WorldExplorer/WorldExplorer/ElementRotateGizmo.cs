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
/// Shows a Z-axis rotation ring on the selected world element and lets the user
/// rotate it (yaw). The rotation sibling of <see cref="ElementDragGizmo"/>.
///
/// <para>Element yaw is <b>continuous</b>, stored as <c>CosAlpha</c>/<c>SinAlpha</c>
/// (~16-bit), which <c>WorldElementPatcher</c> already writes back. Rotating
/// puts the element in cos/sin mode (<c>UsesRotFlags = false</c>) at the new
/// angle; the renderer (<c>SceneTransforms.BuildElementTransform</c>) and the
/// patcher then agree.</para>
///
/// <para><b>Caveats (v1):</b>
/// <list type="bullet">
///   <item>An element currently using discrete <c>XyzRotFlags</c> orientations
///         is converted to continuous cos/sin the moment it's rotated; its
///         starting angle is read from its rotation matrix, which approximates
///         a 90°/mirror flag set by its yaw and may not preserve a mirror.</item>
///   <item>A mirrored element (<c>NegYaxis</c>) may appear to rotate in the
///         opposite direction, since its transform includes a reflection.</item>
/// </list>
/// The common case — a plain cos/sin element — rotates exactly as dragged.</para>
/// </summary>
internal sealed class ElementRotateGizmo
{
    private static readonly DependencyPropertyDescriptor TargetTransformDpd =
        DependencyPropertyDescriptor.FromProperty(
            RotateGizmoVisual.TargetTransformProperty, typeof(RotateGizmoVisual));

    private readonly HelixViewport3D _viewport;

    private RotateGizmoVisual? _manip;
    private WorldElement? _element;
    private LevelViewModel? _lvm;
    private double _startAngleRad;   // element yaw (rad) when the gizmo appeared
    private bool _dragStarted;

    public ElementRotateGizmo(HelixViewport3D viewport) => _viewport = viewport;

    /// <summary>Raised whenever a rotation changes the element, for properties sync.</summary>
    public event Action? ElementRotated;

    public void Attach(WorldElement element, LevelViewModel lvm)
    {
        Detach();
        _element = element;
        _lvm = lvm;

        // Current yaw, read from the element's rotation matrix — works for both
        // cos/sin and rot-flags modes (translation doesn't affect M11/M12).
        var m = SceneTransforms.BuildElementTransform(element).Value;
        _startAngleRad = Math.Atan2(m.M12, m.M11);
 
        // The element renders at its VISUAL origin (Position × rotation), which is
        // the transform matrix's offset row — not raw Position. Using raw Position
        // put the ring far from rotated, off-origin elements.
        var startPos = new Point3D(m.OffsetX, m.OffsetY, m.OffsetZ);

        var size = GizmoSize(lvm);
        _manip = new RotateGizmoVisual(
            diameter: size * 2.0, innerDiameter: size * 1.8, length: size * 0.08, position: startPos);

        TargetTransformDpd.AddValueChanged(_manip, OnTransformChanged);
        _viewport.Children.Add(_manip);
        _dragStarted = false;
    }

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

        if (!_dragStarted)
        {
            _lvm.PushUndoSnapshot();
            _dragStarted = true;
        }

        // Cumulative yaw swept since attach (rad): atan2(M12, M11).
        var m = _manip.TargetTransform.Value;
        var draggedRad = Math.Atan2(m.M12, m.M11);

        var angle = _startAngleRad + draggedRad;
        _element.UsesRotFlags = false;           // continuous cos/sin mode
        _element.CosAlpha = Math.Cos(angle);
        _element.SinAlpha = Math.Sin(angle);

        var visual = _lvm.GetElementVisual(_element);
        if (visual != null)
            visual.Transform = SceneTransforms.BuildElementTransform(_element);

        ElementRotated?.Invoke();
    }

    public void EndDrag()
    {
        if (!_dragStarted || _lvm == null || _element == null) return;
        _dragStarted = false;
        _lvm.FinalizeEdit();
    }

    private static double GizmoSize(LevelViewModel lvm)
    {
        var scale = App.Settings.Get("Editor.GizmoScale", 1.0);
        var size = 10.0;
        if (!lvm.WorldBounds.IsEmpty)
        {
            var span = Math.Max(lvm.WorldBounds.SizeX,
                Math.Max(lvm.WorldBounds.SizeY, lvm.WorldBounds.SizeZ));
            size = Math.Max(2.0, span * 0.08);
        }
        return size * scale;
    }
}
