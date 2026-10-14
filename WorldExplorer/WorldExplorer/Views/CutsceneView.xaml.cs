using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WorldExplorer.Views;

/// <summary>
/// Shows a cutscene's settings, cast and keyframes and lets their values be
/// changed. A value is applied on Enter or when the box loses focus (a box with
/// a list also when the list closes on a choice); Esc puts the current value back.
/// </summary>
public partial class CutsceneView : UserControl
{
    private CutsceneEditorViewModel? _editor;

    public CutsceneView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach(DataContext as CutsceneEditorViewModel);
    }

    private void Attach(CutsceneEditorViewModel? editor)
    {
        if (_editor != null) _editor.RevealRequested -= Reveal;
        _editor = editor;
        if (_editor != null) _editor.RevealRequested += Reveal;
    }

    private void Reveal(CutsceneKeyframeViewModel keyframe)
    {
        if (_editor?.KeyframesView.Contains(keyframe) == true) KeyframeList.ScrollIntoView(keyframe);
    }

    private static EditValueViewModel? ValueOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as EditValueViewModel;

    private void ValueBox_KeyDown(object sender, KeyEventArgs e)
    {
        var value = ValueOf(sender);
        if (value == null) return;

        if (e.Key == Key.Enter)
        {
            value.Commit();
            (sender as TextBox)?.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            value.ResetDraft();
            e.Handled = true;
        }
    }

    private void ValueBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => ValueOf(sender)?.Commit();

    /// <summary>A value picked from a list is applied as soon as the list closes on it.</summary>
    private void Choice_DropDownClosed(object? sender, EventArgs e)
    {
        if (sender is ComboBox { SelectedItem: not null }) ValueOf(sender)?.Commit();
    }

    /// <summary>What was typed in a box with a list is applied when the focus leaves the box and its list.</summary>
    private void Choice_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false && sender is ComboBox { IsDropDownOpen: false }) ValueOf(sender)?.Commit();
    }

    private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_editor == null) return;
        if (!_editor.ShowKeyframes)
        {
            _editor.SelectedSection = _editor.Sections.FirstOrDefault(s => s.Kind == CutsceneSectionKind.Keyframes);
        }

        // The filter box appears with the keyframes; focus it once it is shown.
        Dispatcher.BeginInvoke(() =>
        {
            FilterBox.Focus();
            FilterBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
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
