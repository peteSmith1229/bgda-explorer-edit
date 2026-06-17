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

using JetBlackEngineLib.Data.World;
using System.Windows.Media.Media3D;

namespace WorldExplorer.Win3D;

/// <summary>
/// Builds the WPF transforms used to place level geometry in the viewport.
/// </summary>
public static class SceneTransforms
{
    /// <summary>
    /// Builds the world transform for a <see cref="WorldElement"/> — a
    /// translation to <see cref="WorldElement.Position"/> followed by the
    /// element's rotation (rot-flags or cos/sin with optional Y flip).
    ///
    /// <para>
    /// This is the exact logic that previously lived inline in
    /// <c>LevelViewModel.RebuildScene</c>, extracted unchanged so it can be
    /// reused for live drag feedback without altering how elements render.
    /// </para>
    /// </summary>
    public static Transform3D BuildElementTransform(WorldElement element)
    {
        Transform3DGroup transform3DGroup = new();

        transform3DGroup.Children.Add(new TranslateTransform3D(element.Position));
        var mtx = Matrix3D.Identity;
        if (element.UsesRotFlags)
        {
            if ((element.XyzRotFlags & 4) == 4)
            {
                // Flip x, y
                mtx.M11 = 0;
                mtx.M21 = 1;

                mtx.M12 = 1;
                mtx.M22 = 0;
            }

            if ((element.XyzRotFlags & 2) == 2)
            {
                mtx.M11 = -mtx.M11;
                mtx.M21 = -mtx.M21;
            }

            if ((element.XyzRotFlags & 1) == 1)
            {
                mtx.M12 = -mtx.M12;
                mtx.M22 = -mtx.M22;
            }

            if (element.XyzRotFlags == 2)
            {
                mtx.M11 = -mtx.M11;
                mtx.M12 = -mtx.M12;
                mtx.M21 = -mtx.M21;
                mtx.M22 = -mtx.M22;
            }

            if (element.XyzRotFlags == 1)
            {
                mtx.M12 = -mtx.M12;
                mtx.M22 = -mtx.M22;
                mtx.M11 = -mtx.M11;
                mtx.M21 = -mtx.M21;
            }
        }
        else
        {
            // Change handedness by reversing angle (sign on sin)
            mtx.M11 = element.CosAlpha;
            mtx.M21 = -element.SinAlpha;
            mtx.M12 = element.SinAlpha;
            mtx.M22 = element.CosAlpha;
            if (element.NegYaxis)
            {
                // Should this be col1 due to handed change?
                mtx.M12 = -mtx.M12;
                mtx.M22 = -mtx.M22;
            }
        }

        if (!mtx.IsIdentity)
        {
            transform3DGroup.Children.Add(new MatrixTransform3D(mtx));
        }

        return transform3DGroup;
    }
}
