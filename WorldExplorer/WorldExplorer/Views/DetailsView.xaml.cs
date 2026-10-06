using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WorldExplorer.Views;

/// <summary>
/// Read-only text view for decoded data (scripts, object lists, entity
/// records, hex dumps, logs) with find, wrap, copy and save.
/// </summary>
public partial class DetailsView : UserControl
{
    public DetailsView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        FindBox.Focus();
        FindBox.SelectAll();
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FindNext(backwards: (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            FindBox.Clear();
            FindStatus.Text = "";
            Output.Focus();
            e.Handled = true;
        }
    }

    private void FindNext(bool backwards)
    {
        var term = FindBox.Text;
        var text = Output.Text;
        if (string.IsNullOrEmpty(term) || string.IsNullOrEmpty(text))
        {
            FindStatus.Text = "";
            return;
        }

        int index;
        if (backwards)
        {
            var start = Output.SelectionStart - 1;
            index = start < 0 ? -1 : text.LastIndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) index = text.LastIndexOf(term, StringComparison.OrdinalIgnoreCase);   // wrap
        }
        else
        {
            var start = Output.SelectionStart + Output.SelectionLength;
            index = start >= text.Length ? -1 : text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);       // wrap
        }

        if (index < 0)
        {
            FindStatus.Text = "No matches";
            return;
        }

        Output.Select(index, term.Length);
        var line = Output.GetLineIndexFromCharacterIndex(index);
        if (line >= 0) Output.ScrollToLine(line);
        FindStatus.Text = $"Line {line + 1:N0}";
    }

    private void Wrap_Click(object sender, RoutedEventArgs e)
    {
        Output.TextWrapping = WrapButton.IsChecked == true ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Output.Text)) return;
        Clipboard.SetText(Output.Text);
        ViewModel?.Notifications.Info("Copied to the clipboard");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Output.Text)) return;

        var name = ViewModel?.SelectedTreeNode?.Label ?? "details";
        var dialog = new SaveFileDialog
        {
            FileName = Path.GetFileNameWithoutExtension(name) + ".txt",
            Filter = "Text File|*.txt|All Files|*.*"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        File.WriteAllText(dialog.FileName, Output.Text);
        ViewModel?.Notifications.Success("Saved", dialog.FileName);
    }
}
