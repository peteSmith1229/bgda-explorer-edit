using System.Collections.Generic;
using System.Windows.Input;

namespace WorldExplorer;

/// <summary>A label/value pair on the Overview page.</summary>
public sealed record OverviewFact(string Label, string Value);

/// <summary>One row of the "Contents" breakdown: how many entries of a kind, and their size.</summary>
public sealed record OverviewBreakdownRow(string IconKey, string BrushKey, string Label, int Count, string Size);

/// <summary>A button on the Overview page (e.g. "Export all textures…").</summary>
public sealed record OverviewAction(string Title, string IconKey, ICommand Command, bool IsPrimary = false);

/// <summary>
/// Summary shown when a container (GOB, archive, folder, database) is selected,
/// instead of leaving the previous item's content on screen.
/// </summary>
public sealed class OverviewModel
{
    public OverviewModel(string title, string subtitle, string iconKey, string iconBrushKey)
    {
        Title = title;
        Subtitle = subtitle;
        IconKey = iconKey;
        IconBrushKey = iconBrushKey;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string IconKey { get; }
    public string IconBrushKey { get; }

    public List<OverviewFact> Facts { get; } = new();
    public List<OverviewBreakdownRow> Breakdown { get; } = new();

    /// <summary>Entries changed since the last save.</summary>
    public List<string> PendingChanges { get; } = new();

    public List<OverviewAction> Actions { get; } = new();

    /// <summary>Optional hint shown under the facts.</summary>
    public string? Note { get; set; }

    public bool HasBreakdown => Breakdown.Count > 0;
    public bool HasPendingChanges => PendingChanges.Count > 0;
    public bool HasActions => Actions.Count > 0;
}
