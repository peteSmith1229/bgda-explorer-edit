using JetBlackEngineLib.Data.DataContainers;

namespace JetBlackEngineLib.Data.World;

/// <summary>
/// New objects for a Baldur's Gate: Dark Alliance level (objects.ob). An object
/// is a class name (what the game's object factory creates, see
/// <see cref="BgdaObjectClasses"/>), a position, a facing and "name=value"
/// properties. The class decides the archive its model comes from; the game
/// takes that archive from the level's GOB, or from the disc when the GOB
/// doesn't have it.
/// </summary>
public static class LevelObjects
{
    /// <summary>
    /// The class that shows a model from any archive: lump names the archive;
    /// model, texture and anim name the files in it (exact names, as the game
    /// finds them by binary search); noCollide=1 lets characters walk through it.
    /// </summary>
    public const string ModelPropClass = "UserParamProp";

    /// <summary>An object faces one of 16 directions, 22.5° apart.</summary>
    public const double FacingStep = 22.5;

    /// <summary>The I6 value for <paramref name="degrees"/>: the direction (0-15) in the top four bits.</summary>
    public static short FacingToI6(double degrees)
    {
        if (!double.IsFinite(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees));
        var step = (int)(Math.Round(degrees / FacingStep) % 16);
        if (step < 0) step += 16;
        return unchecked((short)(step << 12));
    }

    /// <summary>The direction an object faces, in degrees (0 to 337.5).</summary>
    public static double FacingOf(short i6) => FacingStep * ((ushort)i6 >> 12);

    /// <summary>Why <paramref name="className"/> can't be an object's class, or null when it can.</summary>
    public static string? CheckClassName(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return "Choose what to add.";
        foreach (var c in className)
        {
            if (c <= ' ' || c > '~') return "A class name is plain letters and digits, without spaces.";
        }

        return null;
    }

    /// <summary>Why <paramref name="property"/> can't be an object property, or null when it can.</summary>
    public static string? CheckProperty(string property)
    {
        var equals = property.IndexOf('=');
        if (equals <= 0) return $"\"{property}\" isn't name=value.";
        foreach (var c in property)
        {
            if (c < ' ' || c > '~') return $"\"{property}\" has characters the game can't read; use plain ASCII.";
        }

        return null;
    }

    /// <summary>
    /// A new object of <paramref name="className"/> at (x, y, z) in the level's
    /// units, facing <paramref name="degrees"/>, with "name=value"
    /// <paramref name="properties"/>. Throws <see cref="ArgumentException"/>
    /// when the class or a property can't be written.
    /// </summary>
    public static ObjectData Create(string className, float x, float y, float z, double degrees,
        IEnumerable<string> properties)
    {
        Require(CheckClassName(className), nameof(className));
        var list = properties.ToList();
        foreach (var property in list) Require(CheckProperty(property), nameof(properties));
        foreach (var value in new[] { x, y, z })
        {
            if (!float.IsFinite(value)) throw new ArgumentException("Enter a position.", nameof(x));
        }

        return new ObjectData(className, FacingToI6(degrees), new[] { x, y, z }, list);
    }

    /// <summary>The properties of a <see cref="ModelPropClass"/> showing <paramref name="model"/> from <paramref name="archive"/>.</summary>
    public static List<string> ModelPropProperties(string archive, string model, string texture, string anim,
        bool noCollide)
    {
        var properties = new List<string>();
        if (noCollide) properties.Add("noCollide=1");
        properties.Add($"lump={archive}");
        properties.Add($"model={model}");
        properties.Add($"texture={texture}");
        properties.Add($"anim={anim}");
        return properties;
    }

    /// <summary>
    /// Why <paramref name="archive"/> can't give a <see cref="ModelPropClass"/>
    /// these files, or null when it can. Each must be an entry of the archive as
    /// it will be saved, spelled exactly: the game compares names byte by byte.
    /// </summary>
    public static string? CheckModelProp(LmpFile archive, string model, string texture, string anim)
    {
        var names = new HashSet<string>(archive.EffectiveEntryNames(), StringComparer.Ordinal);

        string? Check(string file, string extension, string what)
        {
            if (string.IsNullOrEmpty(file)) return $"Choose the {what} ({extension}).";
            if (!file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) return $"The {what} must be a {extension} file.";
            return names.Contains(file) ? null : $"{archive.Name} has no {file}.";
        }

        return Check(model, ".vif", "model") ?? Check(texture, ".tex", "texture") ?? Check(anim, ".anm", "animation");
    }

    /// <summary>The classes whose model comes from <paramref name="archive"/> (e.g. chest.lmp: Chest).</summary>
    public static IReadOnlyList<BgdaObjectClass> ClassesUsing(string archive) => BgdaObjectClasses.All
        .Where(c => string.Equals(c.Archive, archive, StringComparison.OrdinalIgnoreCase))
        .ToList();

    private static void Require(string? problem, string parameter)
    {
        if (problem != null) throw new ArgumentException(problem, parameter);
    }
}
