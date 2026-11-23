using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorldExplorer;

/// <summary>RGBA pixel arrays (as ArmorTexture uses them) to and from WPF bitmaps and PNG files.</summary>
internal static class ArmourImages
{
    /// <summary>A frozen Bgra32 bitmap of a Width × Height × RGBA array.</summary>
    public static BitmapSource ToBitmap(byte[] rgba, int width, int height)
    {
        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i] = rgba[i + 2];
            bgra[i + 1] = rgba[i + 1];
            bgra[i + 2] = rgba[i];
            bgra[i + 3] = rgba[i + 3];
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>The same picture with every pixel opaque (for previews of textures whose alpha is a blend weight).</summary>
    public static BitmapSource ToOpaqueBitmap(byte[] rgba, int width, int height)
    {
        var copy = (byte[])rgba.Clone();
        for (var i = 3; i < copy.Length; i += 4) copy[i] = 255;
        return ToBitmap(copy, width, height);
    }

    public static void SavePng(string path, byte[] rgba, int width, int height)
    {
        using var stream = new FileStream(path, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(ToBitmap(rgba, width, height)));
        encoder.Save(stream);
    }

    /// <summary>A picture file as Width × Height × RGBA (colour profiles ignored, so the values come back exactly).</summary>
    public static (byte[] Rgba, int Width, int Height) LoadRgba(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();

        var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var width = converted.PixelWidth;
        var height = converted.PixelHeight;
        var bgra = new byte[width * height * 4];
        converted.CopyPixels(bgra, width * 4, 0);
        var rgba = new byte[bgra.Length];
        for (var i = 0; i < bgra.Length; i += 4)
        {
            rgba[i] = bgra[i + 2];
            rgba[i + 1] = bgra[i + 1];
            rgba[i + 2] = bgra[i];
            rgba[i + 3] = bgra[i + 3];
        }

        return (rgba, width, height);
    }
}
