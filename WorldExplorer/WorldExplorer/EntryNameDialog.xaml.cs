/*  Copyright (C) 2012 Ian Brown – see COPYING for licence terms */

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WorldExplorer.Themes;

namespace WorldExplorer;

/// <summary>
/// Simple single-field dialog that lets the user confirm or change the name
/// of an entry before it is added to an LMP archive.
/// </summary>
public partial class EntryNameDialog : Window
{
    private readonly Func<string, bool>? _entryExists;

    public string EntryName => entryNameBox.Text.Trim();

    /// <param name="suggestedName">Initial name (usually the source file's name).</param>
    /// <param name="entryExists">Optional: tells whether the archive already has an entry by that name.</param>
    public EntryNameDialog(string suggestedName, Func<string, bool>? entryExists = null)
    {
        _entryExists = entryExists;
        InitializeComponent();
        ThemeManager.Attach(this);

        entryNameBox.Text = suggestedName;
        entryNameBox.SelectAll();
        Validate();
    }

    private void EntryNameBox_TextChanged(object sender, TextChangedEventArgs e) => Validate();

    private void Validate()
    {
        if (okButton == null) return;

        var name = EntryName;
        string? message = null;
        var isError = false;

        if (name.Length == 0)
        {
            isError = true;
        }
        else if (name.Any(c => c > 0x7E || c < 0x20))
        {
            message = "Use plain ASCII characters only.";
            isError = true;
        }
        else if (_entryExists?.Invoke(name) == true)
        {
            message = "An entry with this name already exists — it will be replaced.";
        }

        okButton.IsEnabled = !isError;
        okButton.Content = message != null && !isError ? "Replace" : "Add";
        validationText.Text = message ?? "";
        validationText.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
        validationText.SetResourceReference(TextBlock.ForegroundProperty, isError ? "Brush.Danger" : "Brush.Warning");
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!okButton.IsEnabled) return;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
