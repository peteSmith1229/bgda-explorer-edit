// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/LevelBudgetModel.cs
//
// What the Overview page shows for a Dark Alliance GOB under "LEVEL BUDGET": the
// object pools (the 384 monster slots and the other two), the memory estimate and
// its parts, every object class in objects.ob with what it costs, and the
// archives the level loads (BgdaLevelBudget does the work).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBlackEngineLib.Data.World;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>A usage bar: title, "used / total", a caption under it, and its colour.</summary>
public sealed record BudgetBar(string Title, string Value, string Caption, double Percent, string BrushKey);

/// <summary>One line of the memory breakdown.</summary>
public sealed record BudgetPart(string Label, string Size, string Detail, string BrushKey);

/// <summary>One object class in objects.ob.</summary>
public sealed record BudgetClassRow(string Name, string Kind, string Count, string Size, string Pool,
    string Archive, string Memory, string Source, string BrushKey);

/// <summary>One archive the level loads.</summary>
public sealed record BudgetArchiveRow(string Name, string File, string Sound, string Memory, string Source, string UsedBy);

public sealed class LevelBudgetModel
{
    private LevelBudgetModel(BgdaLevelBudget b)
    {
        Budget = b;
    }

    public BgdaLevelBudget Budget { get; }

    /// <summary>One-line verdict.</summary>
    public string Summary { get; private init; } = "";
    public string SummaryBrushKey { get; private init; } = "Brush.Text";

    public List<BudgetBar> Bars { get; } = new();
    public List<BudgetPart> Parts { get; } = new();
    public List<BudgetClassRow> Classes { get; } = new();
    public List<BudgetArchiveRow> Archives { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool HasWarnings => Warnings.Count > 0;
    public bool HasClasses => Classes.Count > 0;

    /// <summary>How the numbers were worked out and how to check them in the game.</summary>
    public string Note { get; private init; } = "";

    public static string StatusBrush(BgdaBudgetStatus s) => s switch
    {
        BgdaBudgetStatus.Ok => "Brush.Success",
        BgdaBudgetStatus.Tight => "Brush.Warning",
        _ => "Brush.Danger"
    };

    private static string Size(long bytes) => FileSizeConverter.Format(Math.Max(0, bytes));

    private static double Percent(long used, long total) =>
        total <= 0 ? 0 : Math.Clamp(100.0 * used / total, 0, 100);

    private static string N(int n) => n.ToString("N0", CultureInfo.CurrentCulture);

    public static LevelBudgetModel From(BgdaLevelBudget b)
    {
        var slots = b.SlotStatus;
        var memory = b.MemoryStatus;
        var worst = (BgdaBudgetStatus)Math.Max((int)slots, (int)memory);
        var free = b.FreeAfterLoadBytes;
        var facts = $"{N(b.Medium.Used)} of {N(b.Medium.Slots)} monster slots used, about {Size(free)} of memory free after loading";
        var summary = worst switch
        {
            BgdaBudgetStatus.Ok => $"Fits: {facts}.",
            BgdaBudgetStatus.Tight =>
                $"Tight on {(slots == BgdaBudgetStatus.Tight && memory == BgdaBudgetStatus.Tight ? "monster slots and memory" : slots == BgdaBudgetStatus.Tight ? "monster slots" : "memory")}: " +
                $"{facts}. Test it in the game.",
            _ when free < 0 =>
                $"Too big: the level needs about {Size(-free)} more memory than the game has.",
            _ => $"Over the limit: {(slots == BgdaBudgetStatus.Over ? $"{N(b.NotCreated)} objects would not be created" : "")}" +
                 $"{(slots == BgdaBudgetStatus.Over && memory == BgdaBudgetStatus.Over ? ", and " : "")}" +
                 $"{(memory == BgdaBudgetStatus.Over ? $"only {Size(free)} of memory free after loading" : "")}."
        };

        var m = new LevelBudgetModel(b)
        {
            Summary = summary,
            SummaryBrushKey = StatusBrush(worst),
            Note = $"For three players with ADRIANNA21 (the player characters take {b.Large.Players} of the 1 KB pool). " +
                   "Pool slots are counted from objects.ob as the game places them when the level starts; memory is " +
                   "estimated from the archives in this GOB, those the level needs from the disc, and the buffers the " +
                   $"level asks for, out of the game's {Size(BgdaLevelBudget.HeapBytes)} heap. Checked against save states of " +
                   "Town, Crypt1, Mine2, towert, burnfrst and cellar1 with a pasted SteelGolem (within 2 KB). A disc archive's " +
                   "sound data is only known from a copy (the .LMP, or a GOB holding it, in this folder); without one its " +
                   "whole file is counted. While playing, spells and dropped items take more slots and conversations more " +
                   "memory: the ADRIANNA21 counters (p2s_memreport.py) show the lowest free values a level really reached."
        };

        // ---- bars ----
        var med = b.Medium;
        var medStatus = med.Free < 0 || b.Classes.Any(c => c.NotCreated > 0) ? BgdaBudgetStatus.Over
            : med.Free - b.ExpectedSmallSpill < BgdaLevelBudget.SpellSlots ? BgdaBudgetStatus.Tight : BgdaBudgetStatus.Ok;
        m.Bars.Add(new BudgetBar("Monster and character slots (768-byte pool)",
            $"{N(med.Used)} / {N(med.Slots)}",
            (med.Free > 0 ? $"{N(med.Free)} free - room for about {N(b.MonsterRoom)} more monsters, keeping {BgdaLevelBudget.SpellSlots} " +
                            "for spells and missiles" : "Full") +
            (b.ExpectedSmallSpill > 0 ? $" and {N(b.ExpectedSmallSpill)} for small objects that won't fit their own pool" : "") +
            (med.SpilledIn > 0 ? $". {N(med.SpilledIn)} of the slots hold small objects from the full 256-byte pool" : "") + ".",
            Percent(med.Used, med.Slots), StatusBrush(medStatus)));

        var small = b.Small;
        var smallStatus = small.Used >= small.Slots ? BgdaBudgetStatus.Over
            : small.Used + small.Spare > small.Slots ? BgdaBudgetStatus.Tight : BgdaBudgetStatus.Ok;
        m.Bars.Add(new BudgetBar("Small objects (256-byte pool)",
            $"{N(small.Used)} / {N(small.Slots)}",
            $"Props, doors, triggers, lights and treasure. Spells, gold and items take more while playing (30 in a Meteor Swarm " +
            "fight); when it is full they take monster slots.",
            Percent(small.Used, small.Slots), StatusBrush(smallStatus)));

        var large = b.Large;
        var largeStatus = large.Free < 0 || large.Used >= large.Slots && b.NotCreated > 0 ? BgdaBudgetStatus.Over
            : large.Free < 2 ? BgdaBudgetStatus.Tight : BgdaBudgetStatus.Ok;
        m.Bars.Add(new BudgetBar("Large creatures (1 KB pool)",
            $"{N(large.Used)} / {N(large.Slots)}",
            $"The {large.Players} player characters, Beholders, Eldrith and wisps" +
            (large.SpilledIn > 0 ? $", and {N(large.SpilledIn)} objects that didn't fit the 768-byte pool" : "") + ".",
            Percent(large.Used, large.Slots), StatusBrush(largeStatus)));

        var used = BgdaLevelBudget.HeapBytes - free;
        m.Bars.Add(new BudgetBar("Memory free after loading",
            free >= 0 ? $"{Size(free)} free" : $"{Size(-free)} short",
            $"{Size(used)} of the {Size(BgdaLevelBudget.HeapBytes)} heap in use once the level has loaded. The original levels " +
            "leave 0.5-0.8 MB (Town, Crypt1, burnfrst) to 9 MB (towert); conversations, items and archives loaded later " +
            "take up to about 0.4 MB more.",
            Percent(used, BgdaLevelBudget.HeapBytes), StatusBrush(memory)));

        // ---- memory parts ----
        foreach (var p in b.MemoryParts)
            m.Parts.Add(new BudgetPart(p.Label, Size(p.Bytes), p.Detail, p.IsLevel ? "Brush.Text" : "Brush.TextSecondary"));
        m.Parts.Add(new BudgetPart("Free after loading", free >= 0 ? Size(free) : "-" + Size(-free),
            "what is left for conversations, items, effects and archives loaded while playing", StatusBrush(memory)));

        // ---- classes: monsters and characters first, then the 1 KB pool, small objects, names with no object ----
        static int Group(BgdaClassUse c) => c.Pool switch
        {
            BgdaPool.Medium => 0,
            BgdaPool.Large => 1,
            BgdaPool.Small => 2,
            _ => 3
        };
        foreach (var c in b.Classes.OrderBy(Group).ThenByDescending(c => c.Count))
        {
            var cls = c.Class;
            var pool = cls == null ? "-" : c.Pool switch
            {
                BgdaPool.Small => "256",
                BgdaPool.Medium => "768",
                _ => "1 KB"
            };
            if (c.NotCreated > 0) pool += $" ({N(c.NotCreated)} won't fit)";
            else if (cls != null && c.Placed[(int)c.Pool!.Value] < c.Count) pool += " (some spill)";
            var a = c.Archive;
            // the model's archive, and what the constructor loads besides (Eldrith: mtlcrate.lmp; UserParamProp: lump=)
            var all = (a == null ? c.AlsoLoads : c.AlsoLoads.Prepend(a)).Distinct().ToList();
            m.Classes.Add(new BudgetClassRow(
                c.Name,
                cls != null ? Describe(cls.Kind)
                : BgdaLevelBudget.SpecialNames.TryGetValue(c.Name, out var special) ? special : "not created by the game",
                N(c.Count),
                cls == null ? "" : $"{cls.Size} B",
                pool,
                string.Join(" + ", all.Select(x => x.Name)),
                all.Count == 0 ? "" : all.Any(x => x.Source == BgdaArchiveSource.Missing) ? "missing"
                : (all.Any(x => x.RamIsUpperEstimate) ? "≤ " : "") + Size(all.Sum(x => x.RamBytes)),
                string.Join(", ", all.Select(SourceText).Distinct()),
                c.NotCreated > 0 || all.Any(x => x.Source == BgdaArchiveSource.Missing) ? "Brush.Danger"
                : a?.Source is BgdaArchiveSource.Disc or BgdaArchiveSource.DiscFileFound ? "Brush.Warning"
                : cls == null ? (BgdaLevelBudget.SpecialNames.ContainsKey(c.Name) ? "Brush.TextMuted" : "Brush.Warning")
                : "Brush.Text"));
        }

        // ---- archives ----
        foreach (var a in b.Archives)
        {
            m.Archives.Add(new BudgetArchiveRow(
                a.Name + (a.IsLevelArchive ? " (level)" : ""),
                a.Source == BgdaArchiveSource.Missing ? "" : Size(a.FileBytes),
                a.SoundBytes > 0 ? Size(a.SoundBytes) : "",
                a.Source == BgdaArchiveSource.Missing ? "missing" : (a.RamIsUpperEstimate ? "≤ " : "") + Size(a.RamBytes),
                SourceText(a),
                string.Join(", ", a.UsedBy)));
        }

        m.Warnings.AddRange(b.Warnings);
        return m;
    }

    private static string Describe(BgdaObjectKind kind) => kind switch
    {
        BgdaObjectKind.Monster => "monster",
        BgdaObjectKind.Ambient => "harmless creature",
        BgdaObjectKind.Npc => "character",
        BgdaObjectKind.PlayerStart => "player start",
        BgdaObjectKind.Light => "light",
        _ => "object"
    };

    private static string SourceText(BgdaArchiveUse a) => a.Source switch
    {
        BgdaArchiveSource.Gob => "this GOB",
        BgdaArchiveSource.DiscFileFound => "disc (file found next to the GOB)",
        BgdaArchiveSource.Disc when a.FoundInGob != null => $"disc (measured from {a.FoundInGob})",
        BgdaArchiveSource.Disc => "disc",
        _ => a.FoundInGob != null ? $"missing ({a.FoundInGob} has it)" : "missing"
    };
}
