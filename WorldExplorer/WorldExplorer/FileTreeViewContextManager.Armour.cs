// ═══════════════════════════════════════════════════════════════════════════════
// The explorer's Armour actions for ARMOR.YAK (Dark Alliance): export and import a
// child's texture (PNG) and shape (OBJ), recolour, reshape, choose the body parts it
// hides, switch the enchant glow on or off, copy an entry as a new look, and put
// children back as they were. Every edit is pending until the YAK is saved (File ›
// Save, or the YAK's own menu).
// ═══════════════════════════════════════════════════════════════════════════════
using JetBlackEngineLib;
using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Textures;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WorldExplorer.TreeView;

namespace WorldExplorer;

internal partial class FileTreeViewContextManager
{
    private const string PendingYak = "The change is pending — save the YAK to write it to disk.";

    private static bool IsArmorYak(YakFile yak) => yak.EngineVersion == EngineVersion.DarkAlliance;

    private void BuildYakItems(YakTreeViewModel node)
    {
        if (!IsArmorYak(node.YakFile)) return;
        Add($"Save {node.YakFile.Name}…", "Icon.Save", () => _window.SaveYak());
        AddSeparator();
        Add("Export All as PNG and OBJ…", "Icon.Export", () => ExportArmour(node.YakFile, null));
    }

    private void BuildYakEntryItems(YakTreeViewItem node)
    {
        var yak = node.YakFile;
        if (!IsArmorYak(yak)) return;
        var e = node.EntryIndex;
        Add("Copy as a New Look", "Icon.Duplicate", () => CopyAsNewLook(node));
        if (e == yak.EntryCount - 1 && e >= yak.OriginalEntryCount)
        {
            Add("Take Out This Added Entry", "Icon.Delete", () => RemoveAddedEntry(node));
        }

        AddSeparator();
        Add("Export as PNG and OBJ…", "Icon.Export", () => ExportArmour(yak, e));
        Add("Recolour…", "Icon.Theme", () => Recolour(yak, e, null));
        Add("Reshape…", "Icon.View.Model", () => Reshape(yak, e, null));
        var allGlow = Enumerable.Range(0, 4).All(c => ArmorModel.GlowsIn(yak.GetChild(e, c).Vif));
        Add(allGlow ? "Stop the Enchant Glow" : "Glow When Enchanted", "Icon.Light",
            () => SetGlow(yak, e, Enumerable.Range(0, 4).ToArray(), !allGlow));
        if (e < yak.OriginalEntryCount && Enumerable.Range(0, 4).Any(c => yak.IsChildEdited(e, c)))
        {
            Add("Revert to the Original", "Icon.Undo", () => RevertArmour(node, yak, e, null));
        }
    }

    private void BuildYakChildItems(YakChildTreeViewItem node)
    {
        var yak = node.YakFile;
        if (!IsArmorYak(yak)) return;
        Add("Export Texture as PNG…", "Icon.Export", () => ExportArmourPng(node));
        Add("Import PNG…", "Icon.Import", () => ImportArmourPng(node));
        AddSeparator();
        Add("Export Shape as OBJ…", "Icon.Export", () => ExportArmourObj(node));
        Add("Import Shape from OBJ…", "Icon.Import", () => ImportArmourObj(node));
        AddSeparator();
        Add("Recolour…", "Icon.Theme", () => Recolour(yak, node.EntryIndex, node.ChildIndex));
        Add("Reshape…", "Icon.View.Model", () => Reshape(yak, node.EntryIndex, node.ChildIndex));
        Add("Hidden Body Parts…", "Icon.View.Skeleton", () => EditHiddenParts(node));
        var glows = ArmorModel.GlowsIn(node.Value.Vif);
        Add(glows ? "Stop the Enchant Glow" : "Glow When Enchanted", "Icon.Light",
            () => SetGlow(yak, node.EntryIndex, new[] { node.ChildIndex }, !glows));
        if (node.EntryIndex < yak.OriginalEntryCount && yak.IsChildEdited(node.EntryIndex, node.ChildIndex))
        {
            AddSeparator();
            Add("Revert to the Original", "Icon.Undo", () => RevertArmour(node, yak, node.EntryIndex, node.ChildIndex));
        }
    }

    private static string FileStem(int entry, int child) => $"{entry:00}_{ArmorYakInfo.Characters[child].Replace(" ", "").ToLowerInvariant()}";

    // ───────────────────────────── export ─────────────────────────────

    public void ExportArmour(YakFile yak, int? entry)
    {
        var folder = PickFolder(entry is { } only
            ? $"Folder for entry {only}'s textures (PNG) and shapes (OBJ)"
            : "Folder for every entry's textures (PNG) and shapes (OBJ)");
        if (folder == null) return;
        var count = 0;
        foreach (var e in entry is { } one ? new[] { one } : Enumerable.Range(0, yak.EntryCount))
        {
            for (var c = 0; c < 4; c++)
            {
                WriteArmourFiles(yak.GetChild(e, c), folder, FileStem(e, c));
                count++;
            }
        }

        ViewModel.Notifications.Success($"Exported {count} textures and shapes", folder);
    }

    private static void WriteArmourFiles(YakFile.Child child, string folder, string stem)
    {
        var texture = new ArmorTexture(child.Tex);
        ArmourImages.SavePng(Path.Combine(folder, stem + ".png"), texture.ToRgba(), texture.Width, texture.Height);
        File.WriteAllText(Path.Combine(folder, stem + ".mtl"), ArmorObj.WriteMtl(stem + ".png"));
        File.WriteAllText(Path.Combine(folder, stem + ".obj"),
            ArmorObj.Write(new ArmorModel(child.Vif), texture.Width, texture.Height, stem, stem + ".mtl"));
    }

    public void ExportArmourPng(YakChildTreeViewItem node)
    {
        var texture = new ArmorTexture(node.Value.Tex);
        var path = PromptSavePath(FileStem(node.EntryIndex, node.ChildIndex) + ".png", "PNG Image|*.png");
        if (path == null) return;
        ArmourImages.SavePng(path, texture.ToRgba(), texture.Width, texture.Height);
        ViewModel.Notifications.Success("Texture exported",
            $"{path}\nOpaque pixels are solid; transparent ones are see-through in the game. Keep the size when you edit it.");
    }

    public void ExportArmourObj(YakChildTreeViewItem node)
    {
        var path = PromptSavePath(FileStem(node.EntryIndex, node.ChildIndex) + ".obj", "OBJ Model|*.obj");
        if (path == null) return;
        var stem = Path.GetFileNameWithoutExtension(path);
        WriteArmourFiles(node.Value, Path.GetDirectoryName(path)!, stem);
        ViewModel.Notifications.Success("Shape exported",
            $"{path} (with its texture and material)\nMove vertices, but keep their number and order; then Import Shape from OBJ.");
    }

    // ───────────────────────────── import ─────────────────────────────

    public void ImportArmourPng(YakChildTreeViewItem node)
    {
        var child = node.Value;
        var texture = new ArmorTexture(child.Tex);
        var dialog = new OpenFileDialog
        {
            Title = $"Choose a {texture.Width}×{texture.Height} picture for {node.Label} ({YakTreeViewModel.EntryLabel(node.YakFile, node.EntryIndex)})",
            Filter = "PNG Image|*.png|All Files|*.*"
        };
        if (dialog.ShowDialog(_window) != true) return;

        var (rgba, width, height) = ArmourImages.LoadRgba(dialog.FileName);
        if (width != texture.Width || height != texture.Height)
        {
            ViewModel.Notifications.Warning("Couldn't import the picture",
                $"It is {width}×{height}; the texture is {texture.Width}×{texture.Height}. The game's layout and the model's " +
                "UVs depend on the size, so it must stay the same.");
            return;
        }

        var (changed, lost) = texture.FromRgba(rgba);
        node.YakFile.SetChild(node.EntryIndex, node.ChildIndex, child.Vif, texture.Data);
        EditApplied();
        ViewModel.Notifications.Success($"Imported into {node.Label}",
            $"{changed:N0} pixels changed" +
            (lost > 0 ? $" ({lost:N0} of them where the file stores nothing: the model never shows them)" : "") +
            ". " + PendingYak);
        Reselect(node);
    }

    public void ImportArmourObj(YakChildTreeViewItem node)
    {
        var child = node.Value;
        var dialog = new OpenFileDialog
        {
            Title = $"Choose the edited OBJ for {node.Label} ({YakTreeViewModel.EntryLabel(node.YakFile, node.EntryIndex)})",
            Filter = "OBJ Model|*.obj|All Files|*.*"
        };
        if (dialog.ShowDialog(_window) != true) return;

        var model = new ArmorModel(child.Vif);
        int moved;
        try
        {
            moved = model.EditPositions(ArmorObj.ReadPositions(model, File.ReadAllText(dialog.FileName)));
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or ArgumentException)
        {
            ViewModel.Notifications.Warning("Couldn't import the shape", ex.Message);
            return;
        }

        if (moved == 0)
        {
            ViewModel.Notifications.Info("Nothing moved", "The OBJ has the shape the model already has.");
            return;
        }

        node.YakFile.SetChild(node.EntryIndex, node.ChildIndex, model.Data, child.Tex);
        EditApplied();
        ViewModel.Notifications.Success($"New shape for {node.Label}",
            $"{moved:N0} vertices moved; their normals were worked out again. {PendingYak}");
        Reselect(node);
    }

    // ───────────────────────────── dialogs ─────────────────────────────

    private void Recolour(YakFile yak, int entry, int? child)
    {
        var window = new ArmourRecolourWindow(yak, entry, child ?? 0, child == null, ViewModel.World?.DataPath) { Owner = _window };
        if (window.ShowDialog() != true) return;
        foreach (var (c, tex) in window.Results) yak.SetChild(entry, c, yak.GetChild(entry, c).Vif, tex);
        EditApplied();
        var notes = window.Notes.Count > 0 ? string.Join(" ", window.Notes) + " " : "";
        ViewModel.Notifications.Success("Recoloured", $"{CountOf(window.Results.Count, "character")} of entry {entry}. {notes}{PendingYak}");
        ReselectArmour(yak, entry);
    }

    private void Reshape(YakFile yak, int entry, int? child)
    {
        var window = new ArmourReshapeWindow(yak, entry, child, ViewModel.World?.DataPath) { Owner = _window };
        if (window.ShowDialog() != true) return;
        var problems = new List<string>();
        var done = 0;
        foreach (var c in window.Characters)
        {
            var current = yak.GetChild(entry, c);
            // the same bytes the window's preview showed
            var (vif, problem) = window.ResultFor(c);
            if (problem != null)
            {
                problems.Add($"{ArmorYakInfo.Characters[c]}: {problem}");
                continue;
            }

            yak.SetChild(entry, c, vif, current.Tex);
            done++;
        }

        EditApplied();
        if (problems.Count > 0) ViewModel.Notifications.Warning("Some shapes weren't changed", string.Join("\n", problems));
        if (done > 0) ViewModel.Notifications.Success("Reshaped", $"{CountOf(done, "character")} of entry {entry}. {PendingYak}");
        ReselectArmour(yak, entry);
    }

    public void EditHiddenParts(YakChildTreeViewItem node)
    {
        var child = node.Value;
        var model = new ArmorModel(child.Vif);
        var window = new ArmourBodyPartsWindow(node.ChildIndex, model.HiddenBodyParts,
            $"{YakTreeViewModel.EntryLabel(node.YakFile, node.EntryIndex)}, {node.Label}") { Owner = _window };
        if (window.ShowDialog() != true || window.Mask == model.HiddenBodyParts) return;
        model.HiddenBodyParts = window.Mask;
        node.YakFile.SetChild(node.EntryIndex, node.ChildIndex, model.Data, child.Tex);
        EditApplied();
        ViewModel.Notifications.Success($"{node.Label} now hides: {ArmorYakInfo.MaskText(window.Mask, node.ChildIndex)}", PendingYak);
        Reselect(node);
    }

    /// <summary>
    /// Switches the enchant glow (<see cref="ArmorModel.GlowFlag"/>) on or off for these characters of an entry: worn,
    /// the piece then glows as an enchanted weapon does. Children already as asked are left alone.
    /// </summary>
    private void SetGlow(YakFile yak, int entry, IReadOnlyList<int> children, bool on)
    {
        var changed = 0;
        foreach (var c in children)
        {
            var child = yak.GetChild(entry, c);
            var model = new ArmorModel(child.Vif);
            if (model.Glows == on) continue;
            model.Glows = on;
            yak.SetChild(entry, c, model.Data, child.Tex);
            changed++;
        }

        if (changed == 0) return;
        EditApplied();
        var who = children.Count == 1 ? ArmorYakInfo.Characters[children[0]] : CountOf(changed, "character");
        if (on)
        {
            var note = ArmorYakInfo.GlowNote(entry) is { } n ? n + ". " : "";
            ViewModel.Notifications.Success($"Entry {entry} glows when enchanted ({who})",
                $"Worn, it glows as an enchanted weapon does when the item has power. {note}{PendingYak}");
        }
        else
        {
            ViewModel.Notifications.Info($"Entry {entry} no longer glows ({who})", PendingYak);
        }

        ReselectArmour(yak, entry);
    }

    // ───────────────────────────── entries ─────────────────────────────

    private void CopyAsNewLook(YakTreeViewItem node)
    {
        var yak = node.YakFile;
        var e = node.EntryIndex;
        // a look and its enchanted look (the next entry); clothes have none, so the look is copied twice
        int first, second;
        var shipped = ArmorYakInfo.ShippedEntry(e);
        if (shipped is { Enchanted: true }) (first, second) = (e - 1, e);
        else if (shipped != null && e >= 3) (first, second) = (e, e + 1);
        else if (shipped == null && (e - ArmorYakInfo.ShippedEntries) % 2 == 1) (first, second) = (e - 1, e);
        else if (shipped == null && e + 1 < yak.EntryCount) (first, second) = (e, e + 1);
        else (first, second) = (e, e);

        if (yak.EntryCount + 2 > ArmorYakInfo.MaxEntries)
        {
            ViewModel.Notifications.Warning("No room for a new look",
                $"The game reads at most {ArmorYakInfo.MaxEntries} entries; this file has {yak.EntryCount}.");
            return;
        }

        var added = yak.AddEntries(first, 1);
        yak.AddEntries(second, 1);
        (node.Parent as YakTreeViewModel)?.SyncEntries();
        EditApplied();
        ViewModel.Notifications.Success($"Entries {added} and {added + 1} are a new look",
            $"Copies of {first} and {second}; they share their bytes until you change them. Point an item's model at " +
            $"{added} (its enchanted look is {added + 1}) in Executable Tuning › Items. {PendingYak}");
    }

    private void RemoveAddedEntry(YakTreeViewItem node)
    {
        var yak = node.YakFile;
        yak.RemoveLastAddedEntry();
        if (node.Parent is YakTreeViewModel root)
        {
            if (ReferenceEquals(ViewModel.SelectedNode, node) ||
                ViewModel.SelectedNode is TreeViewItemViewModel { Parent: var parent } && ReferenceEquals(parent, node))
                root.IsSelected = true;
            root.SyncEntries();
        }

        EditApplied();
        ViewModel.Notifications.Info($"Entry {node.EntryIndex} taken out");
    }

    private void RevertArmour(TreeViewItemViewModel node, YakFile yak, int entry, int? child)
    {
        foreach (var c in child is { } one ? new[] { one } : Enumerable.Range(0, 4)) yak.RevertChild(entry, c);
        EditApplied();
        ViewModel.Notifications.Info("Back as it was when the file was opened", PendingYak);
        ReselectArmour(yak, entry);
        Reselect(node);
    }

    /// <summary>Re-displays the selected child when it belongs to <paramref name="entry"/>, so the preview shows the edit.</summary>
    private void ReselectArmour(YakFile yak, int entry)
    {
        if (ViewModel.SelectedNode is YakChildTreeViewItem selected && ReferenceEquals(selected.YakFile, yak) &&
            selected.EntryIndex == entry)
        {
            Reselect(selected);
        }
    }

    private static string CountOf(int count, string noun) => Infrastructure.Plural.Of(count, noun);
}
