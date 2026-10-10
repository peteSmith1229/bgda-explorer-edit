using System.Collections.Generic;

namespace WorldExplorer.Infrastructure;

/// <summary>
/// The pages the right-hand pane can show. Which ones are offered depends on
/// what is selected in the explorer — see <c>MainWindowViewModel.ShowViews</c>.
/// </summary>
public enum ContentView
{
    Overview,
    Level,
    Model,
    Skeleton,
    Texture,
    Script,
    Details
}

/// <summary>An entry in the view switcher.</summary>
public sealed class ViewOption
{
    private ViewOption(ContentView kind, string title, string iconKey, string description)
    {
        Kind = kind;
        Title = title;
        IconKey = iconKey;
        ToolTip = description;
    }

    public ContentView Kind { get; }
    public string Title { get; }

    /// <summary>Resource key of the icon geometry (see Themes/Icons.xaml).</summary>
    public string IconKey { get; }

    public string ToolTip { get; }

    public static readonly ViewOption Overview =
        new(ContentView.Overview, "Overview", "Icon.View.Overview", "Summary of the selected archive or folder");

    public static readonly ViewOption Level =
        new(ContentView.Level, "Level", "Icon.View.Level", "Level editor — Ctrl+click objects and elements to select them");

    public static readonly ViewOption Model =
        new(ContentView.Model, "Model", "Icon.View.Model", "3D model preview");

    public static readonly ViewOption Skeleton =
        new(ContentView.Skeleton, "Skeleton", "Icon.View.Skeleton", "Animation skeleton");

    public static readonly ViewOption Texture =
        new(ContentView.Texture, "Texture", "Icon.View.Texture", "Texture preview");

    public static readonly ViewOption Script =
        new(ContentView.Script, "Script", "Icon.View.Script", "Engine calls in the script — edit their values");

    public static readonly ViewOption Details =
        new(ContentView.Details, "Details", "Icon.View.Details", "Decoded data, logs and hex dumps");

    /// <summary>Every view, in the order the switcher shows them.</summary>
    public static IReadOnlyList<ViewOption> All { get; } =
        new[] { Overview, Level, Model, Skeleton, Texture, Script, Details };

    public static ViewOption For(ContentView kind) => kind switch
    {
        ContentView.Overview => Overview,
        ContentView.Level => Level,
        ContentView.Model => Model,
        ContentView.Skeleton => Skeleton,
        ContentView.Texture => Texture,
        ContentView.Script => Script,
        _ => Details
    };
}
