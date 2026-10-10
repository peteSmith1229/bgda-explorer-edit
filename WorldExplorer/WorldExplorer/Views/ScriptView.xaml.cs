using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WorldExplorer.Views;

/// <summary>
/// Lists a script's engine calls with their arguments and lets numbers and
/// text be changed. A value is applied on Enter or when the box loses focus;
/// Esc puts the current value back.
/// </summary>
public partial class ScriptView : UserControl
{
    private ScriptEditorViewModel? _editor;

    public ScriptView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach(DataContext as ScriptEditorViewModel);
    }

    private void Attach(ScriptEditorViewModel? editor)
    {
        if (_editor != null) _editor.RevealRequested -= Reveal;
        _editor = editor;
        if (_editor != null) _editor.RevealRequested += Reveal;
    }

    private void Reveal(ScriptCallViewModel call)
    {
        if (_editor == null) return;
        // Undoing a change in another function: show the call where it is.
        if (!_editor.CallsView.Contains(call))
        {
            _editor.FilterText = "";
            _editor.SelectedFunction = _editor.Functions.Count > 0 ? _editor.Functions[0] : null;
        }

        CallList.ScrollIntoView(call);
    }

    private static ScriptArgumentViewModel? ArgumentOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as ScriptArgumentViewModel;

    private void ArgumentBox_KeyDown(object sender, KeyEventArgs e)
    {
        var argument = ArgumentOf(sender);
        if (argument == null) return;

        if (e.Key == Key.Enter)
        {
            argument.Commit();
            (sender as TextBox)?.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            argument.ResetDraft();
            e.Handled = true;
        }
    }

    private void ArgumentBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => ArgumentOf(sender)?.Commit();

    private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        FilterBox.Focus();
        FilterBox.SelectAll();
    }

    private void FilterBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _editor != null)
        {
            _editor.FilterText = "";
            e.Handled = true;
        }
    }
}
