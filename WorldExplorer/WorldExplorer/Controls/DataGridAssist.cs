using System.Windows;
using System.Windows.Controls;

namespace WorldExplorer.Controls;

/// <summary>
/// DataGrid columns give their cell editors explicit styles, so the themed
/// implicit TextBox style never reaches them. Setting
/// <c>DataGridAssist.ThemeColumns="True"</c> (the implicit DataGrid style does)
/// swaps the default text-column editor style for the themed
/// <c>DataGrid.EditingTextBox</c> one.
/// </summary>
public static class DataGridAssist
{
    private const string EditingTextBoxKey = "DataGrid.EditingTextBox";

    public static readonly DependencyProperty ThemeColumnsProperty = DependencyProperty.RegisterAttached(
        "ThemeColumns", typeof(bool), typeof(DataGridAssist), new PropertyMetadata(false, OnThemeColumnsChanged));

    public static bool GetThemeColumns(DependencyObject element) => (bool)element.GetValue(ThemeColumnsProperty);

    public static void SetThemeColumns(DependencyObject element, bool value) => element.SetValue(ThemeColumnsProperty, value);

    private static void OnThemeColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid || e.NewValue is not true) return;

        // Columns may still be filling in when the style is applied: theme
        // them now and again once the grid has loaded.
        ApplyTo(grid);
        grid.Loaded += (_, _) => ApplyTo(grid);
    }

    private static void ApplyTo(DataGrid grid)
    {
        if (grid.TryFindResource(EditingTextBoxKey) is not Style editingStyle) return;

        foreach (var column in grid.Columns)
        {
            if (column is DataGridTextColumn textColumn &&
                textColumn.ReadLocalValue(DataGridBoundColumn.EditingElementStyleProperty) == DependencyProperty.UnsetValue)
            {
                textColumn.EditingElementStyle = editingStyle;
            }
        }
    }
}
