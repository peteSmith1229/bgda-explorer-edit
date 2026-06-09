/*  Copyright (C) 2012 Ian Brown – see COPYING for licence terms */

using System.Windows;

namespace WorldExplorer;

/// <summary>
/// Simple single-field dialog that lets the user confirm or change the name
/// of an entry before it is added to an LMP archive.
/// </summary>
public partial class EntryNameDialog : Window
{
    public string EntryName => entryNameBox.Text.Trim();

    public EntryNameDialog(string suggestedName)
    {
        InitializeComponent();
        entryNameBox.Text = suggestedName;
        entryNameBox.SelectAll();
        entryNameBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryNameBox.Text))
        {
            System.Windows.MessageBox.Show("Entry name cannot be empty.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
