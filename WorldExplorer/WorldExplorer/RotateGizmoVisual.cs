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
/// A single-axis rotation gizmo — one ring around the Z (yaw) axis — with a
/// settable ring size.
///
/// <para>Both objects and elements in these games rotate only about Z, so one
/// ring is all that's needed. This wraps a HelixToolkit <see cref="RotateManipulator"/>
/// (whose <c>Diameter</c> <em>does</em> drive its geometry, unlike
/// <c>CombinedManipulator</c>'s) and — exactly like
/// <see cref="TranslateGizmoVisual"/> — declares its OWN
/// <see cref="TargetTransform"/> dependency property and binds the ring's
/// TargetTransform to it two-way. Observing a property declared on this type is
/// what makes the change-notification reliable; observing the inherited
/// <c>Manipulator.Value</c>/<c>TargetTransform</c> directly is less dependable.</para>
///
/// <para>The ring is symmetric about its axis, so we deliberately do <b>not</b>
/// bind <c>Transform</c> to the rotation — a ring spinning about its own centre
/// is invisible, and leaving it static keeps the manipulator's hit-plane math
/// simple. Controllers read the rotation by observing <see cref="TargetTransform"/>
/// and extracting the yaw with <c>atan2(M12, M11)</c>.</para>
/// </summary>
public sealed class RotateGizmoVisual : ModelVisual3D
{
    /// <summary>The cumulative rotation produced by dragging the ring.</summary>
    public static readonly DependencyProperty TargetTransformProperty =
        DependencyProperty.Register(
            nameof(TargetTransform),
            typeof(Transform3D),
            typeof(RotateGizmoVisual),
            new FrameworkPropertyMetadata(
                Transform3D.Identity, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Transform3D TargetTransform
    {
        get => (Transform3D)GetValue(TargetTransformProperty);
        set => SetValue(TargetTransformProperty, value);
    }

    private readonly RotateManipulator _ring;

    /// <param name="diameter">Outer diameter of the ring (≈ 2 × the arrow length looks balanced).</param>
    /// <param name="innerDiameter">Inner diameter; the gap to <paramref name="diameter"/> is the band thickness.</param>
    /// <param name="length">Ring depth along the Z axis (a thin flat band).</param>
    /// <param name="position">World position of the gizmo origin (the object/element centre).</param>
    public RotateGizmoVisual(double diameter, double innerDiameter, double length, Point3D position)
    {
        _ring = new RotateManipulator
        {
            Axis = new Vector3D(0, 0, 1),   // yaw
            Color = Colors.Gold,            // distinct from the red/green/blue move arrows
            Diameter = diameter,
            InnerDiameter = innerDiameter,
            Length = length,
            Position = position,
        };

        // Two-way: the ring writes its accumulating rotation into TargetTransform
        // as it's dragged, which fires our observer. (Identity default is non-null,
        // which the manipulator's drag code requires.)
        BindingOperations.SetBinding(_ring, Manipulator.TargetTransformProperty,
            new Binding(nameof(TargetTransform)) { Source = this });

        Children.Add(_ring);
    }
}
