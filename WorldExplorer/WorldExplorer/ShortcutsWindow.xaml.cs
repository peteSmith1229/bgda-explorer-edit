using System.Collections.Generic;
using System.Windows;
using WorldExplorer.Themes;

namespace WorldExplorer;

/// <summary>Reference card of every mouse and keyboard shortcut.</summary>
public partial class ShortcutsWindow : Window
{
    public sealed record Shortcut(string Keys, string Action);

    public sealed record ShortcutGroup(string Title, IReadOnlyList<Shortcut> Items);

    public ShortcutsWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);

        Groups.ItemsSource = new[]
        {
            new ShortcutGroup("FILES", new[]
            {
                new Shortcut("Ctrl+O", "Open a file (or drop one on the window)"),
                new Shortcut("Ctrl+S", "Save the open GOB / archive"),
                new Shortcut("Ctrl+Shift+S", "Save the selected archive as a stand-alone .LMP"),
                new Shortcut("F5", "Reload the open file from disk"),
                new Shortcut("Ctrl+W", "Close the file"),
            }),
            new ShortcutGroup("EXPLORER", new[]
            {
                new Shortcut("Ctrl+F", "Filter the explorer by name or extension"),
                new Shortcut("Esc", "Clear the filter"),
                new Shortcut("Del", "Delete the selected archive entry (on save)"),
                new Shortcut("Ctrl+B", "Show / hide the explorer"),
                new Shortcut("Right-click", "Actions for the item: export, import, replace, delete…"),
            }),
            new ShortcutGroup("VIEWS", new[]
            {
                new Shortcut("Ctrl+1 … Ctrl+6", "Switch to the 1st … 6th view offered for the selection"),
                new Shortcut("Ctrl+Home", "Frame the 3D view on its content"),
                new Shortcut("Ctrl+Shift+T", "Switch between the dark and light theme"),
                new Shortcut("Ctrl+wheel", "Zoom the texture view"),
                new Shortcut("Ctrl+F (Details)", "Find in the decoded text; Enter / Shift+Enter for next / previous"),
                new Shortcut("Space", "Play / pause an animation (Model and Skeleton views)"),
                new Shortcut("Ctrl+G", "Export the model as posed (Model view)"),
            }),
            new ShortcutGroup("LEVEL EDITOR", new[]
            {
                new Shortcut("Left drag", "Rotate the view"),
                new Shortcut("Middle drag", "Pan the view"),
                new Shortcut("Wheel · PgUp / PgDn", "Zoom"),
                new Shortcut("W A S D", "Move the camera"),
                new Shortcut("Double middle-click", "Reset the view"),
                new Shortcut("Ctrl+click", "Select an object or element"),
                new Shortcut("Drag the gizmo", "Move (arrows) or rotate (rings) the selection"),
                new Shortcut("Ctrl+C · Ctrl+V", "Copy / paste an object (pastes at the mouse cursor)"),
                new Shortcut("Ctrl+D", "Duplicate the selection"),
                new Shortcut("Del", "Delete the selection"),
                new Shortcut("Ctrl+Z · Ctrl+Y", "Undo / redo"),
                new Shortcut("L", "Toggle the level's own lighting"),
                new Shortcut("Enter", "Apply the values typed in the properties panel"),
            }),
        };
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
