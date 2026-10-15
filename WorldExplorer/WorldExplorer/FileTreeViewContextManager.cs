/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using JetBlackEngineLib;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Textures;
using JetBlackEngineLib.Data.World;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorldExplorer.Controls;
using WorldExplorer.DataExporters;
using WorldExplorer.DataImporters;
using WorldExplorer.Infrastructure;
using WorldExplorer.Logging;
using WorldExplorer.TreeView;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace WorldExplorer;

/// <summary>
/// The explorer's right-click menu, plus the archive/entry actions it offers.
/// The actions are public so the main menu and the Overview page reuse them.
/// </summary>
internal class FileTreeViewContextManager
{
    private readonly ContextMenu _menu = new();
    private readonly System.Windows.Controls.TreeView _treeView;
    private readonly MainWindow _window;

    // The node the menu was opened for.
    private TreeViewItemViewModel? _target;

    public FileTreeViewContextManager(MainWindow window, System.Windows.Controls.TreeView treeView)
    {
        _window = window;
        _treeView = treeView;
        _treeView.ContextMenu = _menu;
        _treeView.ContextMenuOpening += OnContextMenuOpening;
    }

    private MainWindowViewModel ViewModel => _window.ViewModel;

    // ─────────────────────────────────────────────────────────────────────────
    // Menu construction
    // ─────────────────────────────────────────────────────────────────────────

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // Opened with the mouse: the row under the cursor (right-click also
        // selects it). From the keyboard: the selected row.
        var item = GetTreeViewItemFromPoint(_treeView, Mouse.GetPosition(_treeView));
        _target = (item?.DataContext ?? _treeView.SelectedItem) as TreeViewItemViewModel;
        if (_target == null)
        {
            e.Handled = true;
            return;
        }

        _menu.Items.Clear();
        BuildItems(_target);

        if (_menu.Items.Count == 0)
        {
            e.Handled = true;
        }
    }

    private void BuildItems(TreeViewItemViewModel node)
    {
        switch (node)
        {
            case LmpEntryTreeViewModel entry:
                BuildEntryItems(entry);
                break;
            case WorldFileTreeViewModel worldFile:
                Add("Log World Structure", "Icon.View.Details", () => LogWorldStructure(worldFile));
                if (ViewModel.EngineVersion != EngineVersion.DarkAlliance)
                {
                    // Dark Alliance-format level textures don't use this table layout.
                    Add("Log .TEX Data", "Icon.View.Texture", LogTexData);
                }
                AddSeparator();
                Add("Save Raw Data…", "Icon.Save", () => SaveRawData(worldFile));
                break;
            case WorldElementTreeViewModel element:
                Add("Save Parsed VIF Data…", "Icon.Save", () => SaveParsedElementData(element));
                break;
            case LmpTreeViewModel archive:
                BuildArchiveItems(archive);
                break;
            case GobTreeViewModel:
                if (ViewModel.World?.WorldGob is { } gob)
                {
                    Add("Save GOB…", "Icon.Save", () => _window.SaveGob());
                    Add("Add Archive (.lmp)…", "Icon.Add", () => AddArchive(gob));
                    AddSeparator();
                    Add("Export All Textures as PNG…", "Icon.Export", () => BatchExportTextures(gob));
                }
                break;
        }

        AddSeparator();
        Add("Copy Name", "Icon.Copy", () => CopyName(node));
        RemoveTrailingSeparators();
    }

    private void BuildEntryItems(LmpEntryTreeViewModel entry)
    {
        var editable = entry.LmpFileProperty is not ClpFile;

        switch (entry.Kind)
        {
            case NodeKind.Texture:
                Add("Export as PNG…", "Icon.Export", () => ExportTexturePng(entry));
                if (editable)
                {
                    Add("Import PNG…", "Icon.Import", () => ImportTexture(entry));
                }
                AddSeparator();
                break;
            case NodeKind.Model:
                Add("Export Model (glTF / OBJ)…", "Icon.Export", () => ExportModel(entry));
                Add("Save Parsed VIF Data…", "Icon.Save", () => SaveParsedVifData(entry));
                AddSeparator();
                break;
            case NodeKind.Script when editable && GameTools.IsAvailable(GameTools.ScriptRewards, ViewModel.SelectedGame):
                // Reward patching follows the Dark Alliance script calling convention.
                Add("Edit Script Rewards…", "Icon.Kind.Script", () => EditScriptRewards(entry));
                Add("Insert Reward at Call Site…", "Icon.Add", () => InsertReward(entry));
                AddSeparator();
                break;
        }

        Add("Save Raw Data…", "Icon.Save", () => SaveRawData(entry));

        if (editable)
        {
            AddSeparator();
            Add("Replace with File…", "Icon.Replace", () => ReplaceEntry(entry));
            if (entry.IsDeleted)
            {
                Add("Restore Entry", "Icon.Undo", () => RestoreEntry(entry));
            }
            else
            {
                Add("Delete Entry", "Icon.Delete", () => DeleteEntry(entry));
            }
        }
    }

    private void BuildArchiveItems(LmpTreeViewModel archive)
    {
        var lmp = archive.LmpFileProperty;
        var inGob = archive.Parent is GobTreeViewModel;

        if (lmp is not ClpFile)
        {
            if (inGob)
            {
                Add("Save GOB…", "Icon.Save", () => _window.SaveGob());
            }
            Add(inGob ? "Save This Archive As .LMP…" : "Save Archive…", "Icon.Save",
                () => _window.SaveArchive(lmp));
            Add("Add Entry…", "Icon.Add", () => AddEntry(lmp));
            if (inGob && ViewModel.World?.WorldGob is { } gob)
            {
                if (ViewModel.EngineVersion == EngineVersion.DarkAlliance)
                {
                    Add("Place in Level…", "Icon.View.Level", () => _window.PlaceInLevel(lmp));
                }

                if (gob.AddedArchives.Contains(lmp.Name))
                {
                    Add("Remove Added Archive", "Icon.Delete", () => RemoveAddedArchive(archive));
                }
            }
            AddSeparator();
        }

        Add("Export All Textures as PNG…", "Icon.Export", () => BatchExportTextures(lmp));
        Add("Export All Entries…", "Icon.Export", () => BatchExportAll(lmp));
    }

    private void Add(string header, string iconKey, Action action)
    {
        var item = new MenuItem
        {
            Header = header,
            Icon = new GeometryIcon { Data = _window.TryFindResource(iconKey) as Geometry }
        };
        item.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ViewModel.ShowFailure($"{header.TrimEnd('…')} failed", ex);
            }
        };
        _menu.Items.Add(item);
    }

    private void AddSeparator()
    {
        if (_menu.Items.Count > 0 && _menu.Items[^1] is not Separator)
        {
            _menu.Items.Add(new Separator());
        }
    }

    private void RemoveTrailingSeparators()
    {
        while (_menu.Items.Count > 0 && _menu.Items[^1] is Separator)
        {
            _menu.Items.RemoveAt(_menu.Items.Count - 1);
        }
    }

    private static TreeViewItem? GetTreeViewItemFromPoint(UIElement treeView, Point point)
    {
        var obj = treeView.InputHitTest(point) as DependencyObject;
        while (obj != null && obj is not TreeViewItem)
            obj = VisualTreeHelper.GetParent(obj);
        return obj as TreeViewItem;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The entry's current bytes (pending edit or original) as a private copy:
    /// some editors patch in place, and a pending edit's array must never
    /// change under it (unsaved-change tracking compares arrays by identity).
    /// </summary>
    private static byte[] GetEntryBytes(LmpFile lmp, string entryName)
    {
        if (lmp.GetEntryBytes(entryName) is { } current) return (byte[])current.Clone();
        var entry = lmp.Directory[entryName]; // deleted: the bytes it had
        var bytes = new byte[entry.Length];
        Buffer.BlockCopy(lmp.FileData, entry.StartOffset, bytes, 0, entry.Length);
        return bytes;
    }

    private static string? PickFolder(string description)
    {
        using var dialog = new FolderBrowserDialog { Description = description, UseDescriptionForTitle = true };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private string? PromptSavePath(string fileName, string filter = "All Files|*.*")
    {
        var dialog = new SaveFileDialog { FileName = fileName, Filter = filter };
        return dialog.ShowDialog(_window) == true ? dialog.FileName : null;
    }

    private void EditApplied()
    {
        _window.UpdateTitle();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Export
    // ─────────────────────────────────────────────────────────────────────────

    public void ExportTexturePng(LmpEntryTreeViewModel entry)
    {
        var bitmap = TexDecoder.Decode(GetEntryBytes(entry.LmpFileProperty, entry.Label));
        if (bitmap == null)
        {
            ViewModel.Notifications.Warning("Couldn't decode texture", entry.Label);
            return;
        }

        var path = PromptSavePath(Path.GetFileNameWithoutExtension(entry.Label) + ".png", "PNG Image|*.png");
        if (path == null) return;

        using (var stream = new FileStream(path, FileMode.Create))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }

        ViewModel.Notifications.Success("Texture exported", path);
    }

    public void ExportModel(LmpEntryTreeViewModel entry)
    {
        // Right-click selects the row, so the model is already decoded.
        if (!ReferenceEquals(ViewModel.SelectedNode, entry) || ViewModel.TheModelViewModel.VifModel == null)
        {
            ViewModel.Notifications.Info("Select the model first",
                "The model is exported as it is shown in the Model view.");
            return;
        }

        ViewModel.TheModelViewModel.ShowExportForPosedModel();
    }

    public void SaveRawData(TreeViewItemViewModel node)
    {
        switch (node)
        {
            case LmpTreeViewModel archive:
            {
                var lmp = archive.LmpFileProperty;
                var path = PromptSavePath(archive.Label);
                if (path == null) return;
                File.WriteAllBytes(path, lmp.FileData);
                ViewModel.Notifications.Success("Raw data saved", path);
                break;
            }
            case AbstractLmpTreeViewModel entryNode:
            {
                var path = PromptSavePath(entryNode.Label);
                if (path == null) return;
                File.WriteAllBytes(path, GetEntryBytes(entryNode.LmpFileProperty, entryNode.Label));
                ViewModel.Notifications.Success("Raw data saved", path);
                break;
            }
        }
    }

    public void SaveParsedVifData(LmpEntryTreeViewModel entry)
    {
        var lmp = entry.LmpFileProperty;
        var path = PromptSavePath(entry.Label + ".txt", "Text File|*.txt|All Files|*.*");
        if (path == null) return;

        var texName = Path.GetFileNameWithoutExtension(entry.Label) + ".tex";
        var tex = lmp.Directory.ContainsKey(texName) ? TexDecoder.Decode(GetEntryBytes(lmp, texName)) : null;
        var chunks = VifDecoder.DecodeChunks(NullLogger.Instance, GetEntryBytes(lmp, entry.Label),
            tex?.PixelWidth ?? 0, tex?.PixelHeight ?? 0);
        VifChunkExporter.WriteChunks(path, chunks);
        ViewModel.Notifications.Success("Parsed VIF data saved", path);
    }

    /// <summary>
    /// Writes the vertex chunks of one level element. The element's VIF data
    /// lives inside its .world entry: <c>VifDataOffset</c> is relative to that
    /// entry, <c>VifDataLength</c> counts 16-byte quadwords, and the VIF stream
    /// starts after an (nRegs + 2)-quadword header (mirrors WorldFileDecoder).
    /// </summary>
    public void SaveParsedElementData(WorldElementTreeViewModel element)
    {
        var info = element.WorldElement.DataInfo;
        if (element.Parent is not WorldFileTreeViewModel worldNode || info == null)
        {
            ViewModel.Notifications.Warning("No VIF data", "This element has no VIF data reference.");
            return;
        }

        var lmp = worldNode.LmpFileProperty;
        var worldEntry = lmp.Directory[worldNode.Label];
        var world = lmp.FileData.AsSpan(worldEntry.StartOffset, worldEntry.Length);

        var fullLength = info.VifDataLength * 0x10;
        if (fullLength <= 0 || info.VifDataOffset + 0x10 >= world.Length)
        {
            ViewModel.Notifications.Warning("No VIF data", "This element's VIF data is empty.");
            return;
        }

        var headerLength = (world[info.VifDataOffset + 0x10] + 2) * 0x10;
        var vifStart = info.VifDataOffset + headerLength;
        var vifLength = Math.Min(fullLength - headerLength, world.Length - vifStart);
        if (vifLength <= 0)
        {
            ViewModel.Notifications.Warning("No VIF data", "This element's VIF data is empty.");
            return;
        }

        var path = PromptSavePath(element.Label + ".txt", "Text File|*.txt|All Files|*.*");
        if (path == null) return;

        VifChunkExporter.WriteChunks(path, VifDecoder.ReadVerts(NullLogger.Instance, world.Slice(vifStart, vifLength)));
        ViewModel.Notifications.Success("Parsed VIF data saved", path);
    }

    public void BatchExportTextures(LmpFile lmp)
    {
        var folder = PickFolder($"Choose a folder for the textures from '{lmp.Name}'");
        if (folder == null) return;

        if (lmp.Directory.Count == 0) lmp.ReadDirectory();
        var count = AssetImporter.BatchExportTextures(lmp, folder);
        ViewModel.Notifications.Success($"Exported {count} texture{(count == 1 ? "" : "s")}", folder);
    }

    /// <summary>Exports the textures of every archive in a GOB, one sub-folder per archive.</summary>
    public void BatchExportTextures(GobFile gob)
    {
        var folder = PickFolder($"Choose a folder for the textures from '{gob.Name}'");
        if (folder == null) return;

        var count = 0;
        foreach (var lmp in gob.Directory.Values)
        {
            if (lmp.Directory.Count == 0) lmp.ReadDirectory();
            count += AssetImporter.BatchExportTextures(lmp,
                Path.Combine(folder, Path.GetFileNameWithoutExtension(lmp.Name)));
        }

        ViewModel.Notifications.Success($"Exported {count} texture{(count == 1 ? "" : "s")}", folder);
    }

    public void BatchExportAll(LmpFile lmp)
    {
        var folder = PickFolder($"Choose a folder for all entries from '{lmp.Name}'");
        if (folder == null) return;

        if (lmp.Directory.Count == 0) lmp.ReadDirectory();
        var count = AssetImporter.BatchExportAllEntries(lmp, folder);
        ViewModel.Notifications.Success($"Exported {count} entr{(count == 1 ? "y" : "ies")}", folder);
    }

    private static void CopyName(TreeViewItemViewModel node)
    {
        Clipboard.SetText(node.Label);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Logs
    // ─────────────────────────────────────────────────────────────────────────

    public void LogTexData()
    {
        var worldTex = ViewModel.World?.WorldTex;
        if (worldTex == null)
        {
            ViewModel.Notifications.Warning("No level texture file", "This file has no companion .TEX loaded.");
            return;
        }

        var entries = WorldTexFile.ReadEntries(worldTex.FileData);
        var sb = new StringBuilder();
        sb.AppendLine($"Debug Info For: {worldTex.FileName}");
        sb.AppendLine();

        for (var i = 0; i < entries.Length; i++)
        {
            sb.AppendLine("Entry " + i);
            sb.AppendLine("Cell Offset: " + entries[i].CellOffset);
            sb.AppendLine("Directory Offset: " + entries[i].DirectoryOffset);
            sb.AppendLine("Size: " + entries[i].Size);
            if (i < entries.Length - 1) sb.AppendLine();
        }

        ViewModel.ShowDetails(sb.ToString(), "Level texture table");
    }

    public void LogWorldStructure(WorldFileTreeViewModel worldFile)
    {
        var lmpFile = worldFile.LmpFileProperty;
        if (!lmpFile.Directory.ContainsKey(worldFile.Label)) return;

        // Use the pending (edited) bytes if present, else the original entry —
        // so the dump reflects whatever the editor would currently save.
        var bytes = GetEntryBytes(lmpFile, worldFile.Label);
        var engineVersion = ViewModel.World?.EngineVersion
                            ?? App.Settings.Get<EngineVersion>("Core.EngineVersion");

        if (!ReferenceEquals(ViewModel.SelectedNode, worldFile))
        {
            worldFile.IsSelected = true;
        }
        ViewModel.ShowDetails(WorldStructureAnalyzer.Analyze(bytes, engineVersion), "World structure");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Editing
    // ─────────────────────────────────────────────────────────────────────────

    public void ReplaceEntry(LmpEntryTreeViewModel entry)
    {
        var lmpFile = entry.LmpFileProperty;
        if (lmpFile is ClpFile)
        {
            ViewModel.Notifications.Warning("Not supported", "CLP archives are hash-indexed and can't be edited.");
            return;
        }

        var ext = (Path.GetExtension(entry.Label) ?? "*").TrimStart('.');
        var dialog = new OpenFileDialog
        {
            Title = $"Select replacement file for '{entry.Label}'",
            Filter = $"{ext.ToUpper()} Files|*.{ext}|All Files|*.*"
        };
        if (dialog.ShowDialog(_window) != true) return;

        AssetImporter.ReplaceEntryFromFile(lmpFile, entry.Label, dialog.FileName);
        EditApplied();
        ViewModel.Notifications.Success($"'{entry.Label}' replaced",
            "The change is pending — save to write it to disk.");
        Reselect(entry);
    }

    public void ImportTexture(LmpEntryTreeViewModel entry)
    {
        var lmpFile = entry.LmpFileProperty;

        // The current entry bytes are the encoder template (same dimensions/format).
        var templateBytes = GetEntryBytes(lmpFile, entry.Label);
        if (!TexEncoder.CanEncodeInto(templateBytes))
        {
            ViewModel.Notifications.Info("Import not supported for this texture",
                "Import currently supports 256-colour (PSMT8) textures only.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"Choose a PNG to import into '{entry.Label}'",
            Filter = "PNG Image|*.png|All Files|*.*"
        };
        if (dialog.ShowDialog(_window) != true) return;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.None;
            image.UriSource = new Uri(dialog.FileName);
            image.EndInit();
            image.Freeze();

            var newTex = TexEncoder.Encode(templateBytes, image);
            lmpFile.ReplaceEntry(entry.Label, newTex);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // Dimension mismatch, or not a 256-colour texture.
            ViewModel.Notifications.Warning("Couldn't import texture", ex.Message);
            return;
        }

        EditApplied();
        ViewModel.Notifications.Success($"Imported into '{entry.Label}'",
            "The change is pending — save to write it to disk.");
        Reselect(entry);
    }

    public void DeleteEntry(LmpEntryTreeViewModel entry)
    {
        var lmp = entry.LmpFileProperty;
        if (lmp.IsAdded(entry.Label))
        {
            // Never saved: taking it back leaves nothing to restore.
            lmp.DeleteEntry(entry.Label);
            (entry.Parent as LmpTreeViewModel)?.RemoveEntryNode(entry.Label);
            if (ReferenceEquals(ViewModel.SelectedNode, entry) && entry.Parent != null) entry.Parent.IsSelected = true;
            EditApplied();
            ViewModel.Notifications.Info($"'{entry.Label}' taken out of {lmp.Name}");
            return;
        }

        var result = MessageBox.Show(_window,
            $"Delete '{entry.Label}'?\n\nThe entry is removed when the archive is next saved. " +
            "Until then you can restore it from this menu.",
            "Delete Entry", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        entry.LmpFileProperty.DeleteEntry(entry.Label);
        EditApplied();
        ViewModel.Notifications.Info($"'{entry.Label}' will be deleted on save");
    }

    public void RestoreEntry(LmpEntryTreeViewModel entry)
    {
        entry.LmpFileProperty.PendingDeletions.Remove(entry.Label);
        EditApplied();
        ViewModel.Notifications.Info($"'{entry.Label}' restored");
    }

    public void AddEntry(LmpFile lmpFile)
    {
        if (lmpFile is ClpFile)
        {
            ViewModel.Notifications.Warning("Not supported", "CLP archives are hash-indexed and can't be edited.");
            return;
        }

        var openDialog = new OpenFileDialog
        {
            Title = $"Select file to add to '{lmpFile.Name}'",
            Filter = "All Files|*.*"
        };
        if (openDialog.ShowDialog(_window) != true) return;

        // The game loads the archives a GOB holds; an archive inside another one is never read.
        if (Path.GetExtension(openDialog.FileName).Equals(".lmp", StringComparison.OrdinalIgnoreCase) &&
            ViewModel.World?.WorldGob is { } gob && gob.Directory.ContainsValue(lmpFile))
        {
            var answer = MessageBox.Show(_window,
                $"{Path.GetFileName(openDialog.FileName)} is an archive. The game only loads archives that are in " +
                $"{gob.Name} itself, not archives inside {lmpFile.Name}.\n\nAdd it to {gob.Name} as an archive instead?",
                "Add Entry", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return;
            if (answer == MessageBoxResult.Yes)
            {
                AddArchiveFiles(gob, new[] { openDialog.FileName });
                return;
            }
        }

        var nameDialog = new EntryNameDialog(Path.GetFileName(openDialog.FileName), lmpFile.HasEntry) { Owner = _window };
        if (nameDialog.ShowDialog() != true) return;

        // A name that differs from an entry's only in case replaces that entry.
        var replacing = lmpFile.HasEntry(nameDialog.EntryName);
        var entryName = lmpFile.AddedEntryName(nameDialog.EntryName);
        AssetImporter.AddEntryFromFile(lmpFile, entryName, openDialog.FileName);
        EditApplied();

        if (FindArchiveNode(lmpFile)?.ShowAddedEntry(entryName) is { } node)
        {
            if (ReferenceEquals(ViewModel.SelectedNode, node)) Reselect(node);
            else _window.RevealAndSelect(node);
        }

        ViewModel.Notifications.Success(replacing ? $"'{entryName}' replaced" : $"'{entryName}' added to {lmpFile.Name}",
            "The change is pending — save to write it to disk.");
    }

    /// <summary>Adds .lmp files to the open GOB as archives the level loads.</summary>
    public void AddArchive(GobFile gob)
    {
        var dialog = new OpenFileDialog
        {
            Title = $"Choose archives to add to {gob.Name}",
            Filter = "LMP Archives|*.lmp|All Files|*.*",
            Multiselect = true
        };
        if (ViewModel.World is { } world && Directory.Exists(world.DataPath)) dialog.InitialDirectory = world.DataPath;
        if (dialog.ShowDialog(_window) != true) return;

        AddArchiveFiles(gob, dialog.FileNames);
    }

    private void AddArchiveFiles(GobFile gob, IEnumerable<string> paths)
    {
        var added = new List<LmpFile>();
        var problems = new List<string>();
        foreach (var path in paths)
        {
            var fileName = Path.GetFileName(path);
            try
            {
                if (gob.CheckNewArchiveName(fileName) is { } problem)
                {
                    problems.Add($"{fileName}: {problem}");
                    continue;
                }

                added.Add(gob.AddArchive(fileName, File.ReadAllBytes(path)));
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                problems.Add($"{fileName}: {ex.Message}");
            }
        }

        if (added.Count > 0)
        {
            EditApplied();
            var gobNode = ViewModel.RootNodes.OfType<GobTreeViewModel>().FirstOrDefault();
            TreeViewItemViewModel? last = null;
            foreach (var lmp in added) last = gobNode?.ShowAddedArchive(lmp) ?? last;
            if (last != null) _window.RevealAndSelect(last);

            ViewModel.Notifications.Success(
                added.Count == 1 ? $"{added[0].Name} added to {gob.Name}" : $"{added.Count} archives added to {gob.Name}",
                "Save the GOB to write it; the level loads it when it starts. In the Level view, Add Object… " +
                "places what it holds.");
        }

        if (problems.Count > 0)
        {
            ViewModel.Notifications.Warning(
                problems.Count == 1 ? "Couldn't add an archive" : $"Couldn't add {problems.Count} archives",
                string.Join("\n", problems));
        }
    }

    /// <summary>Takes an added archive back out of the GOB.</summary>
    public void RemoveAddedArchive(LmpTreeViewModel node)
    {
        var gob = ViewModel.World?.WorldGob;
        var lmp = node.LmpFileProperty;
        if (gob == null || !gob.AddedArchives.Contains(lmp.Name)) return;

        gob.RemoveAddedArchive(lmp.Name);
        if (node.Parent is GobTreeViewModel gobNode)
        {
            if (ReferenceEquals(ViewModel.SelectedNode, node) || ViewModel.SelectedNode is LmpEntryTreeViewModel { Parent: var parent } && ReferenceEquals(parent, node))
            {
                gobNode.IsSelected = true;
            }

            gobNode.RemoveArchiveNode(lmp);
        }

        EditApplied();
        ViewModel.Notifications.Info($"{lmp.Name} taken out of {gob.Name}");
    }

    /// <summary>The explorer node of an archive, when its parent has listed it.</summary>
    private LmpTreeViewModel? FindArchiveNode(LmpFile lmp)
    {
        foreach (var root in ViewModel.RootNodes)
        {
            if (root is LmpTreeViewModel archive && ReferenceEquals(archive.LmpFileProperty, lmp)) return archive;
            if (root is GobTreeViewModel gobNode)
            {
                var node = gobNode.Children.OfType<LmpTreeViewModel>()
                    .FirstOrDefault(n => ReferenceEquals(n.LmpFileProperty, lmp));
                if (node != null) return node;
            }
        }

        return null;
    }

    public void EditScriptRewards(LmpEntryTreeViewModel entry)
    {
        var lmpFile = entry.LmpFileProperty;
        var wnd = new ScriptRewardsWindow(GetEntryBytes(lmpFile, entry.Label)) { Owner = _window };
        if (wnd.ShowDialog() == true && wnd.Modified)
        {
            lmpFile.ReplaceEntry(entry.Label, wnd.ResultBytes);
            EditApplied();
            ViewModel.Notifications.Success("Script rewards updated",
                "The change is pending — save to write it to disk.");
            Reselect(entry);
        }
    }

    public void InsertReward(LmpEntryTreeViewModel entry)
    {
        var lmpFile = entry.LmpFileProperty;
        var wnd = new InsertRewardWindow(GetEntryBytes(lmpFile, entry.Label)) { Owner = _window };
        if (wnd.ShowDialog() == true && wnd.Modified)
        {
            lmpFile.ReplaceEntry(entry.Label, wnd.ResultBytes);
            EditApplied();
            ViewModel.Notifications.Success("Reward inserted",
                "The change is pending — save to write it to disk.");
            Reselect(entry);
        }
    }

    /// <summary>Re-displays an entry after its bytes changed, so the preview shows the edit.</summary>
    private void Reselect(TreeViewItemViewModel node)
    {
        if (ReferenceEquals(ViewModel.SelectedNode, node))
        {
            ViewModel.SelectedNode = node;
        }
    }
}
