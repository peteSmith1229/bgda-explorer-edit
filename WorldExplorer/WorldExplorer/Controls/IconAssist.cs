using System.Windows;

namespace WorldExplorer.Controls;

/// <summary>
/// Binds a <see cref="GeometryIcon"/>'s geometry and fill to resources whose
/// keys come from data (e.g. a tree node's <c>IconKey</c>). Uses dynamic
/// resource references, so icons re-colour immediately when the theme changes.
/// </summary>
public static class IconAssist
{
    public static readonly DependencyProperty DataKeyProperty =
        DependencyProperty.RegisterAttached("DataKey", typeof(string), typeof(IconAssist),
            new PropertyMetadata(null, OnDataKeyChanged));

    public static string? GetDataKey(DependencyObject d) => (string?)d.GetValue(DataKeyProperty);
    public static void SetDataKey(DependencyObject d, string? value) => d.SetValue(DataKeyProperty, value);

    public static readonly DependencyProperty FillKeyProperty =
        DependencyProperty.RegisterAttached("FillKey", typeof(string), typeof(IconAssist),
            new PropertyMetadata(null, OnFillKeyChanged));

    public static string? GetFillKey(DependencyObject d) => (string?)d.GetValue(FillKeyProperty);
    public static void SetFillKey(DependencyObject d, string? value) => d.SetValue(FillKeyProperty, value);

    private static void OnDataKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GeometryIcon icon) return;
        if (e.NewValue is string key && key.Length > 0)
            icon.SetResourceReference(GeometryIcon.DataProperty, key);
        else
            icon.ClearValue(GeometryIcon.DataProperty);
    }

    private static void OnFillKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GeometryIcon icon) return;
        if (e.NewValue is string key && key.Length > 0)
            icon.SetResourceReference(GeometryIcon.FillProperty, key);
        else
            icon.ClearValue(GeometryIcon.FillProperty);
    }
}
