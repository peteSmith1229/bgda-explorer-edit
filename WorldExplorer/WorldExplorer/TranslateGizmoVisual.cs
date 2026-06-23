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
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WorldExplorer;

/// <summary>
/// A translate-only manipulator gizmo (X / Y / Z arrows) with settable arrow
/// <see cref="Length"/>-equivalent and thickness.
///
/// <para><b>Why this exists.</b> HelixToolkit's <c>CombinedManipulator</c> cannot
/// size its translate arrows: its <c>Diameter</c> is bound only to the rotate
/// rings, so the translate arrows always use <c>TranslateManipulator</c>'s
/// defaults (Length 2, Diameter 0.2) regardless of <c>Diameter</c> — which is
/// tiny on a large level and can't be changed. This builds the three translate
/// arrows directly so their size is settable, and reproduces
/// <c>CombinedManipulator</c>'s drag wiring exactly: all three arrows share one
/// two-way <see cref="TargetTransform"/>, and the whole visual follows it, so a
/// drag on any axis moves the gizmo and updates the shared transform.</para>
///
/// <para>Observe <see cref="TargetTransform"/> (via a
/// <c>DependencyPropertyDescriptor</c>) to track drags, exactly as the gizmos
/// did with <c>CombinedManipulator.TargetTransform</c>.</para>
/// </summary>
public sealed class TranslateGizmoVisual : ModelVisual3D
{
    /// <summary>The cumulative transform produced by dragging the arrows.</summary>
    public static readonly DependencyProperty TargetTransformProperty =
        DependencyProperty.Register(
            nameof(TargetTransform),
            typeof(Transform3D),
            typeof(TranslateGizmoVisual),
            new FrameworkPropertyMetadata(
                Transform3D.Identity, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Transform3D TargetTransform
    {
        get => (Transform3D)GetValue(TargetTransformProperty);
        set => SetValue(TargetTransformProperty, value);
    }

    private readonly TranslateManipulator _x;
    private readonly TranslateManipulator _y;
    private readonly TranslateManipulator _z;

    /// <param name="length">Arrow length in world units — the visible size.</param>
    /// <param name="diameter">Arrow thickness in world units.</param>
    /// <param name="position">World position of the gizmo origin.</param>
    public TranslateGizmoVisual(double length, double diameter, Point3D position)
    {
        _x = new TranslateManipulator
        {
            Direction = new Vector3D(1, 0, 0), Color = Colors.Red,
            Length = length, Diameter = diameter, Position = position,
        };
        _y = new TranslateManipulator
        {
            Direction = new Vector3D(0, 1, 0), Color = Colors.Green,
            Length = length, Diameter = diameter, Position = position,
        };
        _z = new TranslateManipulator
        {
            Direction = new Vector3D(0, 0, 1), Color = Colors.Blue,
            Length = length, Diameter = diameter, Position = position,
        };

        // The whole gizmo follows the drag.
        BindingOperations.SetBinding(this, TransformProperty,
            new Binding(nameof(TargetTransform)) { Source = this });

        // All three arrows share this gizmo's TargetTransform (two-way), so a
        // drag on any axis updates it and the others follow — exactly how
        // CombinedManipulator coordinates its children.
        BindingOperations.SetBinding(_x, Manipulator.TargetTransformProperty,
            new Binding(nameof(TargetTransform)) { Source = this });
        BindingOperations.SetBinding(_y, Manipulator.TargetTransformProperty,
            new Binding(nameof(TargetTransform)) { Source = this });
        BindingOperations.SetBinding(_z, Manipulator.TargetTransformProperty,
            new Binding(nameof(TargetTransform)) { Source = this });

        Children.Add(_x);
        Children.Add(_y);
        Children.Add(_z);
    }
}
