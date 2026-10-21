using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorldExplorer.Themes;

namespace WorldExplorer;

/// <summary>
/// Picks a colour: drag in the square (saturation and brightness) and the bar (hue), type red, green and blue
/// (0 to 255) or a #RRGGBB hex value, or click a ready-made colour. <see cref="Color"/> is the result once the
/// dialog closes with OK.
/// </summary>
public partial class ColorPickerWindow : Window
{
    private double _hue, _saturation, _value;
    private readonly Color _original;
    private bool _updating;

    /// <summary>The picked colour.</summary>
    public Color Color { get; private set; }

    public ColorPickerWindow(Color initial, string? title = null)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        if (title != null) Title = title;

        _original = Color.FromRgb(initial.R, initial.G, initial.B);
        OldSwatch.Background = new SolidColorBrush(_original);
        Presets.ItemsSource = PresetColours.Select(p => new Preset(p.Name, p.Hex)).ToList();

        Loaded += (_, _) =>
        {
            SetColour(_original, fromBoxes: false);
            RBox.Focus();
            RBox.SelectAll();
        };
    }

    /// <summary>Opens the picker for <paramref name="initial"/>; the picked colour, or null when cancelled.</summary>
    public static Color? Pick(Window? owner, Color initial, string? title = null)
    {
        var dialog = new ColorPickerWindow(initial, title) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Color : null;
    }

    private static readonly (string Name, string Hex)[] PresetColours =
    {
        ("White", "#FFFFFF"), ("Silver", "#C0C0C0"), ("Grey", "#808080"), ("Black", "#000000"),
        ("Red", "#FF0000"), ("Orange", "#FF8000"), ("Gold", "#FFD040"), ("Yellow", "#FFFF00"),
        ("Green", "#00FF00"), ("Teal", "#00C0A0"), ("Cyan", "#00FFFF"), ("Icy blue", "#80C0FF"),
        ("Blue", "#0040FF"), ("Purple", "#8000FF"), ("Magenta", "#FF00FF"), ("Pink", "#FF80C0")
    };

    private sealed record Preset(string Name, string Hex)
    {
        public Brush Brush { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Showing a colour
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Shows <paramref name="colour"/> everywhere; the hue is kept when the colour has none (grey).</summary>
    private void SetColour(Color colour, bool fromBoxes)
    {
        var (h, s, v) = ToHsv(colour);
        if (s > 0 && v > 0) _hue = h;
        if (v > 0) _saturation = s;
        _value = v;
        Show(colour, fromBoxes);
    }

    private void SetHsv(double hue, double saturation, double value)
    {
        (_hue, _saturation, _value) = (hue, saturation, value);
        Show(FromHsv(hue, saturation, value), fromBoxes: false);
    }

    private void Show(Color colour, bool fromBoxes)
    {
        Color = colour;
        _updating = true;
        try
        {
            NewSwatch.Background = new SolidColorBrush(colour);
            HueFill.Fill = new SolidColorBrush(FromHsv(_hue, 1, 1));
            Canvas.SetLeft(SvThumb, _saturation * SvArea.ActualWidth - SvThumb.Width / 2);
            Canvas.SetTop(SvThumb, (1 - _value) * SvArea.ActualHeight - SvThumb.Height / 2);
            Canvas.SetTop(HueThumb, _hue / 360 * HueArea.ActualHeight - HueThumb.Height / 2);
            SvThumb.Stroke = _value > 0.6 && _saturation < 0.4 ? Brushes.Black : Brushes.White;

            if (!fromBoxes)
            {
                RBox.Text = colour.R.ToString(CultureInfo.InvariantCulture);
                GBox.Text = colour.G.ToString(CultureInfo.InvariantCulture);
                BBox.Text = colour.B.ToString(CultureInfo.InvariantCulture);
            }

            if (!HexBox.IsKeyboardFocused) HexBox.Text = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";
        }
        finally
        {
            _updating = false;
        }

        ShowProblem(null);
    }

    private void ShowProblem(string? problem)
    {
        ProblemText.Text = problem ?? "";
        ProblemText.Visibility = problem == null ? Visibility.Collapsed : Visibility.Visible;
        OkButton.IsEnabled = problem == null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Square and bar
    // ─────────────────────────────────────────────────────────────────────────

    private void SvArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        SvArea.CaptureMouse();
        PickSv(e.GetPosition(SvArea));
    }

    private void SvArea_MouseMove(object sender, MouseEventArgs e)
    {
        if (SvArea.IsMouseCaptured) PickSv(e.GetPosition(SvArea));
    }

    private void HueArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        HueArea.CaptureMouse();
        PickHue(e.GetPosition(HueArea));
    }

    private void HueArea_MouseMove(object sender, MouseEventArgs e)
    {
        if (HueArea.IsMouseCaptured) PickHue(e.GetPosition(HueArea));
    }

    private void Area_MouseUp(object sender, MouseButtonEventArgs e) => ((UIElement)sender).ReleaseMouseCapture();

    private void PickSv(Point p) => SetHsv(_hue,
        Math.Clamp(p.X / SvArea.ActualWidth, 0, 1),
        Math.Clamp(1 - p.Y / SvArea.ActualHeight, 0, 1));

    private void PickHue(Point p) => SetHsv(Math.Clamp(p.Y / HueArea.ActualHeight, 0, 1) * 360 % 360, _saturation, _value);

    // ─────────────────────────────────────────────────────────────────────────
    // Typed values and ready-made colours
    // ─────────────────────────────────────────────────────────────────────────

    private void Channel_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || !IsLoaded) return;
        int? Read(TextBox box) =>
            int.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v is >= 0 and <= 255
                ? v
                : null;

        if (Read(RBox) is { } r && Read(GBox) is { } g && Read(BBox) is { } b)
        {
            SetColour(Color.FromRgb((byte)r, (byte)g, (byte)b), fromBoxes: true);
        }
        else
        {
            ShowProblem("Red, green and blue are whole numbers from 0 to 255.");
        }
    }

    private void Hex_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || !IsLoaded || !HexBox.IsKeyboardFocused) return;
        var text = HexBox.Text.Trim().TrimStart('#');
        if (text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            SetColour(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb), fromBoxes: false);
        }
        else
        {
            ShowProblem("A hex colour is # and six digits, e.g. #FF8000.");
        }
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is Preset preset)
        {
            SetColour(((SolidColorBrush)preset.Brush).Color, fromBoxes: false);
        }
    }

    private void OldSwatch_Click(object sender, MouseButtonEventArgs e) => SetColour(_original, fromBoxes: false);

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    // ─────────────────────────────────────────────────────────────────────────
    // HSV (hue 0-360, saturation and value 0-1)
    // ─────────────────────────────────────────────────────────────────────────

    internal static (double Hue, double Saturation, double Value) ToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        double hue = 0;
        if (delta > 0)
        {
            if (max == r) hue = 60 * ((g - b) / delta % 6);
            else if (max == g) hue = 60 * ((b - r) / delta + 2);
            else hue = 60 * ((r - g) / delta + 4);
        }

        if (hue < 0) hue += 360;
        return (hue, max == 0 ? 0 : delta / max, max);
    }

    internal static Color FromHsv(double hue, double saturation, double value)
    {
        var c = value * saturation;
        var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - c;
        var (r, g, b) = (hue % 360) switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };
        static byte B(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
        return Color.FromRgb(B(r + m), B(g + m), B(b + m));
    }
}
