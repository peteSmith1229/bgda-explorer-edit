using System.Windows.Input;

namespace WorldExplorer.Infrastructure;

/// <summary>
/// Application-wide routed commands. Each carries its keyboard shortcut, so
/// menus show the gesture text automatically and the shortcut works anywhere
/// in the main window. Bindings live in MainWindow.
/// <para>
/// Undo/Redo deliberately do not reuse ApplicationCommands.Undo/Redo: those
/// would be swallowed by a focused TextBox, whereas these always target the
/// level editor's history (a focused TextBox still handles its own Ctrl+Z).
/// </para>
/// </summary>
public static class AppCommands
{
    public static readonly RoutedUICommand Open = Create("Open…", nameof(Open), new KeyGesture(Key.O, ModifierKeys.Control));
    public static readonly RoutedUICommand Save = Create("Save", nameof(Save), new KeyGesture(Key.S, ModifierKeys.Control));
    public static readonly RoutedUICommand SaveArchive = Create("Save Selected Archive As…", nameof(SaveArchive), new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
    public static readonly RoutedUICommand CloseFile = Create("Close File", nameof(CloseFile), new KeyGesture(Key.W, ModifierKeys.Control));
    public static readonly RoutedUICommand Reload = Create("Reload", nameof(Reload), new KeyGesture(Key.F5));
    public static readonly RoutedUICommand Exit = Create("Exit", nameof(Exit), new KeyGesture(Key.F4, ModifierKeys.Alt));

    public static readonly RoutedUICommand Undo = Create("Undo", nameof(Undo), new KeyGesture(Key.Z, ModifierKeys.Control));
    public static readonly RoutedUICommand Redo = Create("Redo", nameof(Redo), new KeyGesture(Key.Y, ModifierKeys.Control));

    public static readonly RoutedUICommand Find = Create("Filter Explorer", nameof(Find), new KeyGesture(Key.F, ModifierKeys.Control));

    /// <summary>Shows a view; the parameter is a <see cref="ContentView"/> name.</summary>
    public static readonly RoutedUICommand ShowView = Create("Show View", nameof(ShowView));
    public static readonly RoutedUICommand ResetCamera = Create("Reset Camera", nameof(ResetCamera), new KeyGesture(Key.Home, ModifierKeys.Control));
    public static readonly RoutedUICommand ToggleTheme = Create("Toggle Dark/Light Theme", nameof(ToggleTheme), new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift));
    public static readonly RoutedUICommand ToggleExplorer = Create("Toggle Explorer", nameof(ToggleExplorer), new KeyGesture(Key.B, ModifierKeys.Control));

    public static readonly RoutedUICommand Settings = Create("Settings…", nameof(Settings), new KeyGesture(Key.OemComma, ModifierKeys.Control, "Ctrl+,"));
    public static readonly RoutedUICommand Shortcuts = Create("Keyboard Shortcuts", nameof(Shortcuts), new KeyGesture(Key.F1));
    public static readonly RoutedUICommand About = Create("About WorldExplorer", nameof(About));

    private static RoutedUICommand Create(string text, string name, params InputGesture[] gestures)
        => new(text, name, typeof(AppCommands), new InputGestureCollection(gestures));
}
