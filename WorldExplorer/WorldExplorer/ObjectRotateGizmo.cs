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
/// Shows a Z-axis rotation ring on the selected object and lets the user rotate
/// it (yaw). The rotation sibling of <see cref="ObjectDragGizmo"/>; both are
/// attached at once so move and rotate are available without a mode switch.
///
/// <para><b>Engine constraint — 22.5° steps.</b> An object's yaw is stored in
/// the top 4 bits of <see cref="ObjectData.I6"/> as <c>22.5° × nibble</c>
/// (<c>ObjectManager.ParseObject</c> reads <c>22.5 * (I6 &gt;&gt; 12)</c>) — so
/// only 16 discrete orientations exist. The ring therefore SNAPS to the nearest
/// 22.5° as it's dragged (so you see exactly what you'll get), and the chosen
/// nibble is packed into <c>I6</c> on mouse-up, preserving the low 12 bits.
/// (Element rotation is continuous — see <see cref="ElementRotateGizmo"/>.)</para>
///
/// <para>Lifecycle matches the drag gizmos: first movement captures one undo
/// snapshot; the live visual updates without a scene rebuild; mouse-up writes
/// the value and commits via <c>FinalizeEdit</c>.</para>
/// </summary>
internal sealed class ObjectRotateGizmo
{
    // Observe the ring wrapper's own TargetTransform (declared on the type, so
    // change notifications are reliable).
    private static readonly DependencyPropertyDescriptor TargetTransformDpd =
        DependencyPropertyDescriptor.FromProperty(
            RotateGizmoVisual.TargetTransformProperty, typeof(RotateGizmoVisual));

    private readonly HelixViewport3D _viewport;

    private RotateGizmoVisual? _manip;
    private VisualObjectData? _target;
    private LevelViewModel? _lvm;
    private double _startZRotation;   // object yaw (deg) when the gizmo appeared
    private int _lastNibble;          // most recent snapped orientation (0..15)
    private bool _dragStarted;

    public ObjectRotateGizmo(HelixViewport3D viewport) => _viewport = viewport;

    /// <summary>Raised after a rotation commits, so the properties panel can refresh.</summary>
    public event Action? ObjectRotated;

    public void Attach(VisualObjectData target, LevelViewModel lvm)
    {
        Detach();
        if (target.ObjectData == null) return;

        _target = target;
        _lvm = lvm;
        _startZRotation = target.ZRotation;
        _lastNibble = ((int)Math.Round(_startZRotation / 22.5)) & 0xF;

        var startPos = new Point3D(
            target.ObjectData.Floats[0] / 4.0,
            target.ObjectData.Floats[1] / 4.0,
            target.ObjectData.Floats[2] / 4.0);

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
        _target = null;
        _lvm = null;
        _dragStarted = false;
    }

    private void OnTransformChanged(object? sender, EventArgs e)
    {
        if (_manip == null || _target?.ObjectData == null || _lvm == null) return;

        if (!_dragStarted)
        {
            _lvm.PushUndoSnapshot();
            _dragStarted = true;
        }

        // Cumulative yaw swept since attach (WPF Z rotation): atan2(M12, M11).
        var m = _manip.TargetTransform.Value;
        var draggedDeg = Math.Atan2(m.M12, m.M11) * 180.0 / Math.PI;

        // Snap live to the engine's 22.5° steps so the user sees the real options.
        var raw = _startZRotation + draggedDeg;
        _lastNibble = ((int)Math.Round(raw / 22.5)) & 0xF;

        _target.ZRotation = 22.5 * _lastNibble;
        _target.UpdateTransform();
    }

    public void EndDrag()
    {
        if (!_dragStarted || _lvm == null || _target?.ObjectData == null) return;
        _dragStarted = false;

        // Pack the orientation into I6's top nibble, preserving the low 12 bits.
        _target.ObjectData.I6 =
            (short)((_target.ObjectData.I6 & 0x0FFF) | ((_lastNibble & 0xF) << 12));

        ObjectRotated?.Invoke();
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
