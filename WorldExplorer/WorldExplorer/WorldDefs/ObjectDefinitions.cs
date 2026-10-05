using HelixToolkit.Wpf;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.Logging;
using WorldExplorer.TreeView;
using WorldExplorer.Win3D;

namespace WorldExplorer.WorldDefs;

public class ObjectDefinitions
{
    private readonly ObjectManager _manager;
    private readonly Dictionary<string, ParseObjectDelegate> _objectDefinitions = new();

    // Decoded object models, shared (frozen) between every object that uses
    // them. Previously each barrel / NPC re-read its LMP from disk and
    // re-decoded it — again on every undo, redo and paste. A null value
    // records a model that couldn't be found. Cleared when a new file opens.
    private readonly Dictionary<string, Model3D?> _externalModelCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(LmpFile Lmp, string File, string Texture), Model3D?> _archiveModelCache = new();
    private World? _cacheWorld;

    public ObjectDefinitions(ObjectManager manager)
    {
        _manager = manager;

        SetupDefinitions();
    }

    private void SetupDefinitions()
    {
        // Lights
        AddDef("Ambient_Light", ParseLightObject);
        AddDef("Ambient_Light2", ParseLightObject);
        AddDef("charAmbient_Light", ParseLightObject);
        AddDef("charAmbient_Light2", ParseLightObject);
        AddDef("Directional_Light", ParseDirectionalObject);
        AddDef("Directional_Light2", ParseDirectionalObject);
        AddDef("Directional_LightD", ParseDirectionalObject);
        AddDef("Directional_LightD2", ParseDirectionalObject);
        AddDef("charDirectional_Light", ParseDirectionalObject);
        AddDef("charDirectional_Light2", ParseDirectionalObject);
        AddDef("charDirectional_LightD", ParseDirectionalObject);
        AddDef("charDirectional_LightD2", ParseDirectionalObject);
        AddDef("clearColor", ParseLightObject);

        AddDef("Trigger", ParseTriggerObject);
        AddDef("PushTrigger", ParsePushTriggerObject);

        AddDef("SaveCrystal", ParseSaveCrystalObject);

        // Characters
        AddDef("Bartender", ParseBartenderObject);
        AddDef("Garik", ParseGarikObject);
        AddDef("Drunk", ParseDrunkObject);
        AddDef("Hoochie", ParseHoochieObject);
        AddDef("CaravanGuard", ParseCaravanGuardObject);
        AddDef("Ethon", ParseEthonObject);
        AddDef("ShopKeep", ParseShopKeepObject);
        AddDef("Lecher", ParseLecherObject);

        // == RTA Objects
        // Characters
        AddDef("NPC", ParseObjectWithLumpProp("name"));
        AddDef("bflya", ParseToMappedLmp("BFLYA.LMP", "BFLYA.vif", "BFLYA.tex"));
        AddDef("bflyb", ParseToMappedLmp("BFLYB.LMP", "BFLYB.vif", "BFLYB.tex"));
        AddDef("Kobold", ParseToMappedLmp("KOBOLD.LMP", "kobold.vif", "kobold.tex"));
        AddDef("Bullfrog", ParseToMappedLmp("BULLFROG.LMP", "bullfrog.vif", "bullfrog.tex"));
        AddDef("Rat", ParseToMappedLmp("RAT.LMP", "rat.vif", "rat.tex"));
        AddDef("Seagull", ParseToMappedLmp("SEAGULL.LMP", "seagull.vif", "seagull.tex"));
        AddDef("fgoblin", ParseToMappedLmp("FGOBLIN.LMP", "firegoblin.vif", "firegoblin.tex"));
        AddDef("Samurai", ParseToMappedLmp("SAMURAI.LMP", "Samurai.vif", "Samurai.tex"));
        AddDef("Player1", ParseToMappedLmp("SELECT.LMP", "im_bodyv1.vif", "im_bodyv1.tex"));

        // Objects
        AddDef("Savepoint", ParseToMappedLmp("SAVEPNT.LMP", "savepoint.vif", "savepoint.tex"));
        AddDef("Portal", ParseObjectWithLumpProp("lump"));
        AddDef("orkbox", ParseObjectWithLumpProp("lumpname"));
        AddDef("lavakeg", ParseObjectWithLumpProp("lumpname"));
        AddDef("gearbox", ParseObjectWithLumpProp("lumpname"));
        AddDef("PowderKeg", ParseToMappedLmp("POWDERK.LMP", "powderkeg.vif", "powderkeg.tex"));
        AddDef("Barrel", ParseToMappedLmp("BARREL.LMP", "barrel.vif", "barrel.tex"));
        AddDef("Wingedv", ParseToMappedLmp("BARREL.LMP", "barrel.vif", "barrel.tex"));
    }

    public VisualObjectData? Parse(VisualObjectData obj)
    {
        if (obj.ObjectData != null && _objectDefinitions.TryGetValue(obj.ObjectData.Name, out var del))
        {
            try
            {
                return del.Invoke(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing object definition\r\n" + ex);
                obj.Model = new ModelVisual3D();
                obj.Model.Children.Add(CreateBox(5, Color.FromRgb(255, 0, 0)));
                return obj;
            }
        }

        // Didn't match anything
        return ParseBaseObject(obj);
    }

    private VisualObjectData ParseBaseObject(VisualObjectData obj)
    {
        obj.Model = CreateBox(5, Color.FromArgb(255, 255, 247, 0));

        return obj;
    }

    // Utility Methods
    private void AddDef(string name, ParseObjectDelegate del)
    {
        _objectDefinitions.Add(name, del);
    }

    private Color ColorFromArray(float[] floats)
    {
        return Color.FromRgb((byte)floats[0], (byte)floats[1], (byte)floats[2]);
    }

    /// <summary>Drops cached models when a different file has been opened.</summary>
    private void EnsureCacheMatchesWorld(World world)
    {
        if (ReferenceEquals(world, _cacheWorld)) return;
        _externalModelCache.Clear();
        _archiveModelCache.Clear();
        _cacheWorld = world;
    }

    private static Visual3D InstantiateObjectModel(Model3D? model)
    {
        if (model == null) return CreateBox(5, Color.FromRgb(255, 0, 0));
        return new ModelVisual3D
        {
            Content = model,
            Transform = new ScaleTransform3D(1.0 / 4, 1.0 / 4, 1.0 / 4)
        };
    }

    private Visual3D LoadModelFromExternalLmp(string lmpName, string file, string textureFile)
    {
        var world = _manager.LevelViewModel.MainViewModel.World;
        if (world == null)
            throw new InvalidOperationException("Missing world instance");
        EnsureCacheMatchesWorld(world);

        var dataPath = world.DataPath;
        var lmpPath = Path.Combine(dataPath, lmpName.ToUpperInvariant());
        var cacheKey = lmpPath + "|" + file + "|" + textureFile;

        if (!_externalModelCache.TryGetValue(cacheKey, out var model))
        {
            model = DecodeExternalModel(world, lmpPath, lmpName, file, textureFile);
            _externalModelCache[cacheKey] = model;
        }

        return InstantiateObjectModel(model);
    }

    private static Model3D? DecodeExternalModel(World world, string lmpPath, string lmpName, string file,
        string textureFile)
    {
        if (File.Exists(lmpPath))
        {
            var data = File.ReadAllBytes(lmpPath);
            LmpFile externalLmp = new(world.EngineVersion, lmpName,
                data,
                0, data.Length);
            externalLmp.ReadDirectory();

            var entry = externalLmp.FindFile(file);
            var texEntry = externalLmp.FindFile(textureFile);

            if (entry == null || texEntry == null)
            {
                return null;
            }

            var tex =
                TexDecoder.Decode(externalLmp.FileData.AsSpan().Slice(texEntry.StartOffset, texEntry.Length));
            tex?.Freeze();

            StringLogger logger = new();
            var vifModel = VifDecoder.Decode(
                logger,
                externalLmp.FileData.AsSpan().Slice(entry.StartOffset, entry.Length),
                tex?.PixelWidth ?? 0,
                tex?.PixelHeight ?? 0);

            var model = Conversions.CreateModel3D(vifModel, tex);
            if (model.CanFreeze) model.Freeze();
            return model;
        }

        return null;
    }

    private Visual3D LoadModelFromOtherLmp(string lmpName, string file, string textureFile)
    {
        var par = _manager.LevelViewModel.WorldNode?.Parent?.Parent;

        if (par is GobTreeViewModel gob)
        {
            foreach (var child in gob.Children.OfType<LmpTreeViewModel>())
            {
                if (string.Compare(child.Label, lmpName, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    continue;
                }

                var world = _manager.LevelViewModel.MainViewModel.World;
                if (world != null) EnsureCacheMatchesWorld(world);

                var key = (child.LmpFileProperty, file, textureFile);
                if (!_archiveModelCache.TryGetValue(key, out var model))
                {
                    model = DecodeModelFromLmpChild(file, textureFile, child);
                    _archiveModelCache[key] = model;
                }

                return InstantiateObjectModel(model);
            }
        }

        return CreateBox(5, Color.FromRgb(255, 0, 0));
    }

    private static Model3D? DecodeModelFromLmpChild(string file, string textureFile, LmpTreeViewModel child)
    {
        child.ForceLoadChildren();
        var entry = child.LmpFileProperty.FindFile(file);
        var texEntry = child.LmpFileProperty.FindFile(textureFile);

        if (entry == null || texEntry == null)
        {
            return null;
        }

        var tex = TexDecoder.Decode(child.LmpFileProperty.FileData.AsSpan()
            .Slice(texEntry.StartOffset, texEntry.Length));
        tex?.Freeze();

        StringLogger logger = new();
        var vifModel = VifDecoder.Decode(
            logger,
            child.LmpFileProperty.FileData.AsSpan().Slice(entry.StartOffset, entry.Length),
            tex?.PixelWidth ?? 0,
            tex?.PixelHeight ?? 0);

        var model = Conversions.CreateModel3D(vifModel, tex);
        if (model.CanFreeze) model.Freeze();
        return model;
    }

    // Static Utility Methods
    private static BoxVisual3D CreateBox(double all, Color color)
    {
        return CreateBox(all, all, all, color);
    }

    private static BoxVisual3D CreateBox(double width, double height, double length, Color color)
    {
        BoxVisual3D box = new()
        {
            Width = width,
            Height = height,
            Length = length,
            Material = new DiffuseMaterial(new SolidColorBrush(color))
        };

        return box;
    }

    private delegate VisualObjectData? ParseObjectDelegate(VisualObjectData obj);

    #region Object Definitions

    // Ambient_Light
    private VisualObjectData? ParseLightObject(VisualObjectData obj)
    {
        if (obj.ObjectData == null)
        {
            return null;
        }
            
        obj.Offset = new Vector3D(0, 0, 0);
        SphereVisual3D sphere = new() {Radius = 2.5};
            
        var color = ColorFromArray(obj.ObjectData.Floats);

        // Black light, don't show so that we can see actual colors
        if (color.R == 0 && color.G == 0 && color.B == 0)
        {
            return null;
        }

        sphere.Material = new DiffuseMaterial(new SolidColorBrush(color));

        obj.Model = sphere;
        return obj;
    }

    private VisualObjectData? ParseDirectionalObject(VisualObjectData obj)
    {
        if (obj.ObjectData == null)
        {
            return null;
        }
            
        obj.Offset = new Vector3D(0, 0, 0);

        if (obj.ObjectData.Name.EndsWith("D") || obj.ObjectData.Name.EndsWith("D2"))
        {
            return null;
        }

        var dName = obj.ObjectData.Name;

        dName = dName.EndsWith("2") ? dName[..^1] + "D2" : dName + "D";

        var directionObj = _manager.GetObjectByName(dName);

        if (directionObj == null)
        {
            Debug.Fail("Cannot find direction object");
            return null;
        }

        var color = ColorFromArray(obj.ObjectData.Floats);

        // Black light, don't show so that we can see actual colors
        if (color.R == 0 && color.G == 0 && color.B == 0)
        {
            return null;
        }

        Point3D point1 = new(0, 0, 0);
        Point3D point2 = new(directionObj.Floats[0] * 10, directionObj.Floats[1] * 10,
            directionObj.Floats[2] * 10);

        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(
            new SphereVisual3D {Center = point2, Radius = 1.0, Fill = new SolidColorBrush(color)});
        obj.Model.Children.Add(new ArrowVisual3D
        {
            Point1 = point2, Point2 = point1, Diameter = 0.5, Fill = new SolidColorBrush(color)
        });

        //obj.Model = sphere;
        return obj;
    }

    private VisualObjectData ParseTriggerObject(VisualObjectData obj)
    {
        BoxVisual3D box = new();


        if (obj.ObjectData == null || !double.TryParse(obj.ObjectData.GetProperty("w"), out var tempValue))
        {
            tempValue = 2.5 * 4;
        }

        box.Width = tempValue / 4;
        box.Length = tempValue / 4;
        if (obj.ObjectData == null || !double.TryParse(obj.ObjectData.GetProperty("h"), out tempValue))
        {
            tempValue = 2.5 * 4;
        }

        box.Height = tempValue / 4;

        box.BackMaterial =
            box.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 171, 0)));


        obj.Model = box;
        return obj;
    }

    private VisualObjectData ParsePushTriggerObject(VisualObjectData obj)
    {
        SphereVisual3D sphere = new();


        if (obj.ObjectData == null || !double.TryParse(obj.ObjectData.GetProperty("radius"), out var tempRadius))
        {
            tempRadius = 2.5 * 4;
        }

        tempRadius = tempRadius / 4;
        sphere.Radius = tempRadius;
        obj.Offset.Z += tempRadius / 2;

        sphere.BackMaterial = sphere.Material =
            new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 171, 0)));

        obj.Model = sphere;
        return obj;
    }

    private VisualObjectData ParseSaveCrystalObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("crystal.lmp", "book.vif", "book.tex"));

        return obj;
    }

    private VisualObjectData ParseBartenderObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("bartend.lmp", "bartender.vif", "bartender.tex"));
        return obj;
    }

    private VisualObjectData ParseGarikObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("garik.lmp", "garik.vif", "garik.tex"));
        return obj;
    }

    private VisualObjectData ParseDrunkObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("drunk.lmp", "drunk.vif", "drunk.tex"));
        return obj;
    }

    private VisualObjectData ParseHoochieObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("hoochie.lmp", "hoochie.vif", "hoochie.tex"));
        return obj;
    }

    private VisualObjectData ParseCaravanGuardObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("cguard.lmp", "caravang.vif", "caravang.tex"));
        return obj;
    }

    private VisualObjectData ParseEthonObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("ethon.lmp", "ethon.vif", "ethon.tex"));
        return obj;
    }

    private VisualObjectData ParseShopKeepObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("bartley.lmp", "shopkeep1.vif", "shopkeep1.tex"));
        return obj;
    }

    private VisualObjectData ParseLecherObject(VisualObjectData obj)
    {
        obj.Model = new ModelVisual3D();
        obj.Model.Children.Add(LoadModelFromOtherLmp("nebbish.lmp", "nebbish.vif", "nebbish.tex"));
        return obj;
    }

    private ParseObjectDelegate ParseObjectWithLumpProp(string propName)
    {
        return obj =>
        {
            if (obj.ObjectData != null)
            {
                var props = obj.ObjectData.Properties.Select(e => e.Split('='))
                    .ToDictionary(e => e[0], e => e.Length > 1 ? e[1] : e[0]);
                if (props.TryGetValue(propName, out var lump) && !string.IsNullOrEmpty(lump))
                {
                    lump = lump.Trim().TrimQuotes();
                    obj.Model = new ModelVisual3D();
                    obj.Model.Children.Add(LoadModelFromExternalLmp(lump + ".lmp", lump + ".vif", lump + ".tex"));
                }
                else
                {
                    obj.Model ??= new ModelVisual3D();
                    obj.Model.Children.Add(CreateBox(5, Color.FromRgb(255, 0, 0)));
                }
            }

            return obj;
        };
    }

    private ParseObjectDelegate ParseToMappedLmp(string lmpFile, string vifFile, string texFile)
    {
        return obj =>
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromExternalLmp(lmpFile, vifFile, texFile));
            return obj;
        };
    }

    #endregion
}