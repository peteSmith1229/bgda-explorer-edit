// ═══════════════════════════════════════════════════════════════════════════════
// The wearer drawn under an ARMOR.YAK piece in the model view and in Reshape's preview:
// the character's own body, without the meshes the piece hides, and its hair, except
// under a helmet (ArmorWearer). The models come from HUMAN.LMP, ELF.LMP or DWARF.LMP
// next to ARMOR.YAK or in the data folder of the settings, and are decoded once.
// ═══════════════════════════════════════════════════════════════════════════════
using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using WorldExplorer.Logging;

namespace WorldExplorer;

internal static class ArmourScene
{
    /// <summary>Whether armour is drawn on its wearer (the model view's figure button; remembered in the settings).</summary>
    public static bool ShowWearer
    {
        get => App.Settings.Get("Armour.ShowWearer", true);
        set => App.Settings["Armour.ShowWearer"] = value;
    }

    /// <summary>A character's own models, decoded for drawing.</summary>
    public sealed record Wearer(ArmorWearer Models, List<Mesh> Body, BitmapSource? BodyTexture, List<Mesh> Hair,
        BitmapSource? HairTexture);

    private static readonly Dictionary<(string Path, int Child), (DateTime Stamp, Wearer Wearer)> Cache = new();

    /// <summary>
    /// The wearer of character <paramref name="child"/>: its archive next to ARMOR.YAK (<paramref name="yakFolder"/>)
    /// or in the data folder of the settings, read and decoded once (again when the file changes). Null, with
    /// <paramref name="problem"/>, when it can't be found or read.
    /// </summary>
    public static Wearer? Find(int child, string? yakFolder, out string? problem)
    {
        var (archive, _, _) = ArmorWearer.Files(child);
        var path = ItemEditorModel.FindGameFile(archive, new[] { yakFolder, App.Settings.Get("Files.DataPath", "") });
        if (path == null)
        {
            problem = $"{archive} wasn't found next to ARMOR.YAK or in the data folder of the settings";
            return null;
        }

        try
        {
            var stamp = File.GetLastWriteTimeUtc(path);
            if (Cache.TryGetValue((path, child), out var hit) && hit.Stamp == stamp)
            {
                problem = null;
                return hit.Wearer;
            }

            var models = ArmorWearer.FromArchive(child, File.ReadAllBytes(path), out problem);
            if (models == null) return null;
            var bodyTexture = Decode(models.BodyTex);
            var hairTexture = Decode(models.HairTex);
            var wearer = new Wearer(models,
                VifDecoder.Decode(new StringLogger(), models.BodyVif, bodyTexture?.PixelWidth ?? 0, bodyTexture?.PixelHeight ?? 0),
                bodyTexture,
                VifDecoder.Decode(new StringLogger(), models.HairVif, hairTexture?.PixelWidth ?? 0, hairTexture?.PixelHeight ?? 0),
                hairTexture);
            Cache[(path, child)] = (stamp, wearer);
            return wearer;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or IndexOutOfRangeException or InvalidOperationException)
        {
            problem = $"{archive} couldn't be read: {ex.Message}";
            return null;
        }
    }

    private static BitmapSource? Decode(byte[] tex)
    {
        var bitmap = TexDecoder.Decode(tex);
        if (bitmap is { IsFrozen: false, CanFreeze: true }) bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// What to draw under a piece of <paramref name="entry"/> whose model is <paramref name="pieceVif"/>: the body
    /// without the meshes the piece hides, and the hair unless the piece is a helmet.
    /// </summary>
    public static List<(Model Vif, BitmapSource? Texture)> Parts(Wearer wearer, int entry, byte[] pieceVif)
    {
        var hair = ArmorWearer.HairShownWith(entry);
        var mask = pieceVif.Length >= 8 ? BitConverter.ToUInt64(pieceVif, 0) : 0;
        var body = wearer.Models.VisibleMeshes(mask, hair).Where(i => i < wearer.Body.Count).Select(i => wearer.Body[i]).ToList();
        var parts = new List<(Model Vif, BitmapSource? Texture)>();
        if (body.Count > 0) parts.Add((new Model(body), wearer.BodyTexture));
        if (hair && wearer.Hair.Count > 0) parts.Add((new Model(wearer.Hair), wearer.HairTexture));
        return parts;
    }

    /// <summary>The Details line about the wearer.</summary>
    public static string Describe(Wearer? wearer, int entry, string? problem, bool shown) =>
        !shown ? "Wearer: not drawn (the figure button at the top right of the model view draws it)"
        : wearer == null ? $"Wearer: not drawn: {problem}"
        : $"Wearer: {wearer.Models.Describe(ArmorWearer.HairShownWith(entry))}, without the parts this piece hides";
}
