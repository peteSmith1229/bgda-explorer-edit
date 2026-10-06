using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorldExplorer.Themes;

namespace WorldExplorer.Views;

/// <summary>
/// Texture preview with fit / actual-size / zoom and a selectable backdrop so
/// transparent pixels are visible.
/// </summary>
public partial class TextureView : UserControl
{
    private static readonly double[] ZoomSteps = { 0.25, 0.5, 1, 2, 3, 4, 6, 8, 12, 16 };

    private double _zoom = 1;
    private bool _fit = true;

    public TextureView()
    {
        InitializeComponent();
        ThemeManager.ThemeChanged += (_, _) => UpdateBackdrop();
        DependencyPropertyDescriptorHelper.OnSourceChanged(Picture, ApplyZoom);
        // In fit mode the scale follows the view size: keep the readout current.
        Picture.SizeChanged += (_, _) => { if (_fit) UpdateZoomText(); };
        UpdateBackdrop();
    }

    private void UpdateZoomText()
    {
        var zoom = EffectiveZoom();
        ZoomText.Text = zoom > 0 ? $"{zoom * 100:0}%" : "Fit";
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void ApplyZoom()
    {
        if (_fit || Picture.Source is not BitmapSource bitmap)
        {
            Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Picture.Stretch = Stretch.Uniform;
            Picture.ClearValue(WidthProperty);
            Picture.ClearValue(HeightProperty);
            UpdateZoomText();
            return;
        }

        Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Picture.Stretch = Stretch.Fill;
        Picture.Width = bitmap.PixelWidth * _zoom;
        Picture.Height = bitmap.PixelHeight * _zoom;
        ZoomText.Text = $"{_zoom * 100:0}%";
    }

    private void SetZoom(double zoom)
    {
        _fit = false;
        FitButton.IsChecked = false;
        _zoom = Math.Clamp(zoom, ZoomSteps[0], ZoomSteps[^1]);
        ApplyZoom();
    }

    /// <summary>The zoom currently on screen (in fit mode, derived from the layout).</summary>
    private double EffectiveZoom()
    {
        if (!_fit || Picture.Source is not BitmapSource bitmap || bitmap.PixelWidth == 0) return _zoom;
        return Picture.ActualWidth / bitmap.PixelWidth;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => Step(+1);

    private void ZoomOut_Click(object sender, RoutedEventArgs e) => Step(-1);

    private void Step(int direction)
    {
        var current = EffectiveZoom();
        double next;
        if (direction > 0)
        {
            next = ZoomSteps[^1];
            foreach (var step in ZoomSteps)
            {
                if (step > current + 0.001) { next = step; break; }
            }
        }
        else
        {
            next = ZoomSteps[0];
            for (var i = ZoomSteps.Length - 1; i >= 0; i--)
            {
                if (ZoomSteps[i] < current - 0.001) { next = ZoomSteps[i]; break; }
            }
        }
        SetZoom(next);
    }

    private void Fit_Click(object sender, RoutedEventArgs e)
    {
        _fit = FitButton.IsChecked == true;
        ApplyZoom();
    }

    private void Actual_Click(object sender, RoutedEventArgs e) => SetZoom(1);

    private void Scroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        Step(e.Delta > 0 ? +1 : -1);
        e.Handled = true;
    }

    private void Backdrop_Changed(object sender, SelectionChangedEventArgs e) => UpdateBackdrop();

    private void UpdateBackdrop()
    {
        if (Backdrop == null || BackdropBox == null) return;
        var dark = ThemeManager.Current == AppTheme.Dark;
        Backdrop.Background = BackdropBox.SelectedIndex switch
        {
            1 => new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x12)),
            2 => new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7)),
            _ => CreateCheckerboard(dark
                ? (Color.FromRgb(0x2A, 0x2B, 0x2F), Color.FromRgb(0x35, 0x36, 0x3B))
                : (Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0xE6, 0xE8, 0xEB)))
        };
    }

    private static Brush CreateCheckerboard((Color A, Color B) colours)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(colours.A), null,
            new RectangleGeometry(new Rect(0, 0, 16, 16))));
        var squares = new GeometryGroup();
        squares.Children.Add(new RectangleGeometry(new Rect(0, 0, 8, 8)));
        squares.Children.Add(new RectangleGeometry(new Rect(8, 8, 8, 8)));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(colours.B), null, squares));

        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 16, 16),
            ViewportUnits = BrushMappingMode.Absolute
        };
        brush.Freeze();
        return brush;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.MainWindow.ExportSelectedTexture(weave: false);
    }
}

/// <summary>Small helper: run a callback when an Image's Source changes.</summary>
internal static class DependencyPropertyDescriptorHelper
{
    public static void OnSourceChanged(Image image, Action callback)
    {
        var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        descriptor?.AddValueChanged(image, (_, _) => callback());
    }
}
