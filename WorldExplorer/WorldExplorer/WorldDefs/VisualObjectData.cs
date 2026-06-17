using JetBlackEngineLib.Data.World;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace WorldExplorer.WorldDefs;

public class VisualObjectData
{
    public ModelVisual3D? Model;
    public ObjectData? ObjectData;
    public Vector3D Offset = new(0, 0, 0);
    public double ZRotation;

    public void AddToScene(List<ModelVisual3D> scene)
    {
        if (Model == null)
        {
            return;
        }
 
        Model.Transform = BuildTransform();
        scene.Add(Model);
    }
 
    /// <summary>
    /// Rebuilds <see cref="Model"/>'s transform from the current <see cref="Offset"/>
    /// and <see cref="ZRotation"/>.  Used for live drag feedback so a single object
    /// can be repositioned without rebuilding the whole scene.
    /// </summary>
    public void UpdateTransform()
    {
        if (Model != null)
            Model.Transform = BuildTransform();
    }
 
    private Transform3D BuildTransform()
    {
        Transform3DGroup transform3DGroup = new();
 
        if (ZRotation != 0.0)
        {
            transform3DGroup.Children.Add(
                new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), ZRotation)));
        }
 
        transform3DGroup.Children.Add(new TranslateTransform3D(Offset));
        return transform3DGroup;
    }
}