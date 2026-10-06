using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WorldExplorer.Controls;

/// <summary>
/// Draws a vector icon authored on a 24×24 grid (see Themes/Icons.xaml),
/// scaled to the element's size. The fill defaults to the inherited
/// <see cref="TextElement.ForegroundProperty"/>, so an icon placed in a
/// button, menu item or tree row automatically follows that element's text
/// colour — including hover and disabled states — unless
/// <see cref="Fill"/> is set explicitly.
/// <para>
/// Rendering goes straight to the <see cref="DrawingContext"/> (no visual
/// children), which keeps large virtualised trees cheap.
/// </para>
/// </summary>
public sealed class GeometryIcon : FrameworkElement
{
    private const double GridSize = 24.0;

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(Geometry), typeof(GeometryIcon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(GeometryIcon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner(typeof(GeometryIcon),
            new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    static GeometryIcon()
    {
        // A sensible default size; callers override with Width/Height.
        WidthProperty.OverrideMetadata(typeof(GeometryIcon), new FrameworkPropertyMetadata(16.0));
        HeightProperty.OverrideMetadata(typeof(GeometryIcon), new FrameworkPropertyMetadata(16.0));
        SnapsToDevicePixelsProperty.OverrideMetadata(typeof(GeometryIcon), new FrameworkPropertyMetadata(true));
        IsHitTestVisibleProperty.OverrideMetadata(typeof(GeometryIcon), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The icon geometry, in 24×24 grid coordinates.</summary>
    public Geometry? Data
    {
        get => (Geometry?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>Explicit fill. When null the inherited <see cref="Foreground"/> is used.</summary>
    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Inherited text colour, used when <see cref="Fill"/> is null.</summary>
    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var geometry = Data;
        var brush = Fill ?? Foreground;
        if (geometry == null || brush == null) return;

        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;

        var scale = size / GridSize;
        var offsetX = (ActualWidth - size) / 2;
        var offsetY = (ActualHeight - size) / 2;

        drawingContext.PushTransform(new MatrixTransform(scale, 0, 0, scale, offsetX, offsetY));
        drawingContext.DrawGeometry(brush, null, geometry);
        drawingContext.Pop();
    }
}
