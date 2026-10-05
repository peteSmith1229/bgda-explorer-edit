using System.Windows;
using System.Windows.Media;

namespace WorldExplorer.Controls;

/// <summary>
/// Attached properties read by the control templates in Themes/Controls.xaml.
/// They let one template serve several visual variants (accent / subtle
/// buttons, buttons with icons, text boxes with placeholder text) instead of
/// duplicating whole templates per variant.
/// </summary>
public static class ControlAssist
{
    // ── Button / ToggleButton ────────────────────────────────────────────────

    public static readonly DependencyProperty HoverBackgroundProperty =
        DependencyProperty.RegisterAttached("HoverBackground", typeof(Brush), typeof(ControlAssist),
            new FrameworkPropertyMetadata(null));

    public static Brush? GetHoverBackground(DependencyObject d) => (Brush?)d.GetValue(HoverBackgroundProperty);
    public static void SetHoverBackground(DependencyObject d, Brush? value) => d.SetValue(HoverBackgroundProperty, value);

    public static readonly DependencyProperty PressedBackgroundProperty =
        DependencyProperty.RegisterAttached("PressedBackground", typeof(Brush), typeof(ControlAssist),
            new FrameworkPropertyMetadata(null));

    public static Brush? GetPressedBackground(DependencyObject d) => (Brush?)d.GetValue(PressedBackgroundProperty);
    public static void SetPressedBackground(DependencyObject d, Brush? value) => d.SetValue(PressedBackgroundProperty, value);

    public static readonly DependencyProperty CheckedBackgroundProperty =
        DependencyProperty.RegisterAttached("CheckedBackground", typeof(Brush), typeof(ControlAssist),
            new FrameworkPropertyMetadata(null));

    public static Brush? GetCheckedBackground(DependencyObject d) => (Brush?)d.GetValue(CheckedBackgroundProperty);
    public static void SetCheckedBackground(DependencyObject d, Brush? value) => d.SetValue(CheckedBackgroundProperty, value);

    public static readonly DependencyProperty CheckedForegroundProperty =
        DependencyProperty.RegisterAttached("CheckedForeground", typeof(Brush), typeof(ControlAssist),
            new FrameworkPropertyMetadata(null));

    public static Brush? GetCheckedForeground(DependencyObject d) => (Brush?)d.GetValue(CheckedForegroundProperty);
    public static void SetCheckedForeground(DependencyObject d, Brush? value) => d.SetValue(CheckedForegroundProperty, value);

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached("CornerRadius", typeof(CornerRadius), typeof(ControlAssist),
            new FrameworkPropertyMetadata(new CornerRadius(4)));

    public static CornerRadius GetCornerRadius(DependencyObject d) => (CornerRadius)d.GetValue(CornerRadiusProperty);
    public static void SetCornerRadius(DependencyObject d, CornerRadius value) => d.SetValue(CornerRadiusProperty, value);

    /// <summary>Optional icon (24×24 geometry) shown before a button's content.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached("Icon", typeof(Geometry), typeof(ControlAssist),
            new FrameworkPropertyMetadata(null));

    public static Geometry? GetIcon(DependencyObject d) => (Geometry?)d.GetValue(IconProperty);
    public static void SetIcon(DependencyObject d, Geometry? value) => d.SetValue(IconProperty, value);

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.RegisterAttached("IconSize", typeof(double), typeof(ControlAssist),
            new FrameworkPropertyMetadata(16.0));

    public static double GetIconSize(DependencyObject d) => (double)d.GetValue(IconSizeProperty);
    public static void SetIconSize(DependencyObject d, double value) => d.SetValue(IconSizeProperty, value);

    // ── TextBox ──────────────────────────────────────────────────────────────

    /// <summary>Greyed hint shown while a TextBox is empty.</summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(ControlAssist),
            new FrameworkPropertyMetadata(null));

    public static string? GetPlaceholder(DependencyObject d) => (string?)d.GetValue(PlaceholderProperty);
    public static void SetPlaceholder(DependencyObject d, string? value) => d.SetValue(PlaceholderProperty, value);
}
