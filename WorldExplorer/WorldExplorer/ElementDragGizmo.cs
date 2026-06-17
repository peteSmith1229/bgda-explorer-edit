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
            CombinedManipulator.TargetTransformProperty, typeof(CombinedManipulator));

    private readonly HelixViewport3D _viewport;

    private CombinedManipulator? _manip;
    private WorldElement? _element;
    private LevelViewModel? _lvm;
    private Point3D _startPos;
    private Vector3D _lastPos;
    private bool _dragStarted;

    public ElementDragGizmo(HelixViewport3D viewport) => _viewport = viewport;

    /// <summary>Shows the gizmo on <paramref name="element"/>, replacing any current gizmo.</summary>
    public void Attach(WorldElement element, LevelViewModel lvm)
    {
        Detach();

        _element = element;
        _lvm = lvm;

        // World element position is used directly (no ×4 — unlike objects).
        _startPos = new Point3D(element.Position.X, element.Position.Y, element.Position.Z);
        _lastPos  = new Vector3D(element.Position.X, element.Position.Y, element.Position.Z);

        var diameter = 10.0;
        if (!lvm.WorldBounds.IsEmpty)
        {
            var span = Math.Max(lvm.WorldBounds.SizeX,
                       Math.Max(lvm.WorldBounds.SizeY, lvm.WorldBounds.SizeZ));
            diameter = Math.Max(2.0, span * 0.08);
        }

        _manip = new CombinedManipulator
        {
            CanRotateX = false,
            CanRotateY = false,
            CanRotateZ = false,
            Diameter   = diameter,
            Position   = _startPos,
        };

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

        var d = _manip.TargetTransform.Transform(new Point3D(0, 0, 0));
        _lastPos = new Vector3D(_startPos.X + d.X, _startPos.Y + d.Y, _startPos.Z + d.Z);

        // Update the element and move only its visual live (no scene rebuild).
        _element.Position = _lastPos;
        var visual = _lvm.GetElementVisual(_element);
        if (visual != null)
            visual.Transform = SceneTransforms.BuildElementTransform(_element);
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
