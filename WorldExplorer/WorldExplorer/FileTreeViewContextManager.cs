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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorldExplorer.DataExporters;
using WorldExplorer.DataImporters;
using WorldExplorer.Logging;
using WorldExplorer.TreeView;
using MessageBox  = System.Windows.MessageBox;
using OpenFileDialog  = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog  = Microsoft.Win32.SaveFileDialog;

namespace WorldExplorer;

internal class FileTreeViewContextManager
{
    // ── original items ────────────────────────────────────────────────────────
    private readonly MenuItem _logTexData;
    private readonly ContextMenu _menu = new();
    private readonly MenuItem _saveParsedVifData;
    private readonly MenuItem _saveRawData;
    private readonly System.Windows.Controls.TreeView _treeView;
    private readonly MainWindow _window;

    // ── new: per-entry actions ────────────────────────────────────────────────
    private readonly MenuItem _exportAsPng;
    private readonly MenuItem _exportAsModel;
    private readonly MenuItem _replaceEntry;
    private readonly MenuItem _deleteEntry;

    // ── new: per-archive (LmpTree) actions ───────────────────────────────────
    private readonly MenuItem _addNewEntry;
    private readonly MenuItem _batchExportTextures;
    private readonly MenuItem _batchExportAll;
    private readonly MenuItem _saveArchive;

    // ── separator items ───────────────────────────────────────────────────────
    private readonly Separator _sep1 = new();
    private readonly Separator _sep2 = new();
    private readonly Separator _sep3 = new();

    // ─────────────────────────────────────────────────────────────────────────

    public FileTreeViewContextManager(MainWindow window,
                                      System.Windows.Controls.TreeView treeView)
    {
        _window   = window;
        _treeView = treeView;
        _treeView.ContextMenu = _menu;
        _treeView.ContextMenuOpening += MenuOnContextMenuOpening;

        // ── original items ────────────────────────────────────────────────
        _saveRawData      = AddItem("Save Raw Data",    SaveRawDataClicked);
        _saveParsedVifData = AddItem("Save Parsed Data", SaveParsedDataClicked);
        _logTexData       = AddItem("Log .TEX Data",    LogTexDataClicked);

        // ── separator ────────────────────────────────────────────────────
        _menu.Items.Add(_sep1);

        // ── per-entry export shortcuts ────────────────────────────────────
        _exportAsPng   = AddItem("Export Entry as PNG",       ExportAsPngClicked);
        _exportAsModel = AddItem("Export Entry as GLTF/OBJ…", ExportAsModelClicked);

        // ── separator ────────────────────────────────────────────────────
        _menu.Items.Add(_sep2);

        // ── per-entry edit actions ────────────────────────────────────────
        _replaceEntry  = AddItem("Replace Entry…", ReplaceEntryClicked);
        _deleteEntry   = AddItem("Delete Entry",   DeleteEntryClicked);

        // ── separator ────────────────────────────────────────────────────
        _menu.Items.Add(_sep3);

        // ── per-archive actions ───────────────────────────────────────────
        _addNewEntry        = AddItem("Add New Entry…",              AddNewEntryClicked);
        _batchExportTextures = AddItem("Batch Export All Textures…", BatchExportTexturesClicked);
        _batchExportAll     = AddItem("Batch Export All Entries…",   BatchExportAllClicked);
        _saveArchive        = AddItem("Save Archive…",               SaveArchiveClicked);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Visibility wiring
    // ─────────────────────────────────────────────────────────────────────────

    private void MenuOnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var child = GetTreeViewItemFromPoint(_treeView, Mouse.GetPosition(_treeView));
        if (child == null) { e.Handled = true; return; }

        var dataContext = child.DataContext;
        _menu.DataContext = null;

        // Default: hide everything new, show Save Raw Data
        SetVisibility(Visibility.Visible,
            _saveRawData);
        SetVisibility(Visibility.Collapsed,
            _saveParsedVifData, _logTexData,
            _sep1, _exportAsPng, _exportAsModel,
            _sep2, _replaceEntry, _deleteEntry,
            _sep3, _addNewEntry, _batchExportTextures, _batchExportAll, _saveArchive);

        switch (dataContext)
        {
            // ── individual entry inside an LMP / CLP ───────────────────────
            case LmpEntryTreeViewModel lmpEntry:
            {
                var ext = (Path.GetExtension(lmpEntry.Label) ?? "").ToUpperInvariant();

                // Parsed VIF data
                if (ext == ".VIF")
                    _saveParsedVifData.Visibility = Visibility.Visible;

                // Export shortcuts
                _sep1.Visibility = Visibility.Visible;
                if (ext == ".TEX")
                    _exportAsPng.Visibility = Visibility.Visible;
                if (ext is ".VIF")
                    _exportAsModel.Visibility = Visibility.Visible;

                // Edit actions — only for plain LmpFile, not CLP
                if (lmpEntry.LmpFileProperty is not ClpFile)
                {
                    _sep2.Visibility     = Visibility.Visible;
                    _replaceEntry.Visibility = Visibility.Visible;
                    _deleteEntry.Visibility  = Visibility.Visible;
                }

                _menu.DataContext = lmpEntry;
                break;
            }

            // ── LMP file node ──────────────────────────────────────────────
            case LmpTreeViewModel lmpTree:
            {
                // Archive-level actions — only for writable LmpFile, not CLP
                if (lmpTree.LmpFileProperty is not ClpFile)
                {
                    _sep3.Visibility            = Visibility.Visible;
                    _addNewEntry.Visibility     = Visibility.Visible;
                    _saveArchive.Visibility     = Visibility.Visible;
                }
                _batchExportTextures.Visibility = Visibility.Visible;
                _batchExportAll.Visibility      = Visibility.Visible;
                // Make the sep3 visible even for CLP batch exports
                _sep3.Visibility = Visibility.Visible;
                _menu.DataContext = lmpTree;
                break;
            }

            // ── .world files ───────────────────────────────────────────────
            case WorldFileTreeViewModel:
                _logTexData.Visibility = Visibility.Visible;
                _menu.DataContext      = dataContext;
                break;

            // ── world element cells ────────────────────────────────────────
            case WorldElementTreeViewModel worldElement:
                _saveRawData.Visibility      = Visibility.Collapsed;
                _saveParsedVifData.Visibility = Visibility.Visible;
                _menu.DataContext             = worldElement;
                break;

            default:
                e.Handled = true;
                return;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private MenuItem AddItem(string text, RoutedEventHandler clickHandler)
    {
        MenuItem item = new() { Header = text };
        item.Click += clickHandler;
        _menu.Items.Add(item);
        return item;
    }

    private static void SetVisibility(Visibility v, params UIElement[] items)
    {
        foreach (var item in items) item.Visibility = v;
    }

    private static TreeViewItem? GetTreeViewItemFromPoint(UIElement treeView, Point point)
    {
        var obj = treeView.InputHitTest(point) as DependencyObject;
        while (obj != null && obj is not TreeViewItem)
            obj = VisualTreeHelper.GetParent(obj);
        return obj as TreeViewItem;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Click handlers — original
    // ─────────────────────────────────────────────────────────────────────────

    #region Original click handlers

    private void SaveRawDataClicked(object sender, RoutedEventArgs e)
    {
        switch (_menu.DataContext)
        {
            case LmpTreeViewModel lmpItem:
            {
                var lmpFile = lmpItem.LmpFileProperty;
                PromptToSaveData(lmpItem.Label, saveFilePath =>
                {
                    using FileStream stream = new(saveFilePath, FileMode.Create);
                    stream.Write(lmpFile.FileData, 0, lmpFile.FileData.Length);
                    stream.Flush();
                });
                break;
            }

            case LmpEntryTreeViewModel lmpEntry:
                SaveLmpEntryData(lmpEntry.LmpFileProperty, lmpEntry.Label);
                break;

            case WorldFileTreeViewModel tvm:
                SaveLmpEntryData(tvm.LmpFileProperty, tvm.Label);
                break;

            case WorldElementTreeViewModel:
                MessageBox.Show(
                    "Saving raw world element data is not supported due to the " +
                    "scattered layout of the data.",
                    "Error");
                break;
        }
    }

    private void SaveLmpEntryData(LmpFile lmpFile, string entryName)
    {
        var entry = lmpFile.Directory[entryName];
        PromptToSaveData(entryName, saveFilePath =>
        {
            using FileStream stream = new(saveFilePath, FileMode.Create);
            stream.Write(lmpFile.FileData, entry.StartOffset, entry.Length);
            stream.Flush();
        });
    }

    private void SaveParsedDataClicked(object sender, RoutedEventArgs e)
    {
        switch (_menu.DataContext)
        {
            case LmpEntryTreeViewModel lmpEntry:
            {
                var lmpFile = lmpEntry.LmpFileProperty;
                var entry   = lmpFile.Directory[lmpEntry.Label];

                if (!lmpEntry.Label.EndsWith(".vif", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Not a .vif file!", "Error");
                    return;
                }

                PromptToSaveVifData(lmpEntry.Label + ".txt", () =>
                {
                    var texEntry =
                        lmpFile.Directory[
                            Path.GetFileNameWithoutExtension(lmpEntry.Label) + ".tex"];
                    var texData = lmpFile.FileData.AsSpan()
                        .Slice(texEntry.StartOffset, texEntry.Length);
                    var tex    = TexDecoder.Decode(texData);
                    var vifData = lmpFile.FileData.AsSpan()
                        .Slice(entry.StartOffset, entry.Length);
                    return VifDecoder.DecodeChunks(
                        NullLogger.Instance,
                        vifData,
                        tex?.PixelWidth  ?? 0,
                        tex?.PixelHeight ?? 0);
                });
                break;
            }

            case WorldElementTreeViewModel itemModel:
            {
                var lmpFile = (itemModel.Parent as LmpTreeViewModel)?.LmpFileProperty;
                var element = itemModel.WorldElement;
                if (lmpFile == null || element.DataInfo == null) return;

                PromptToSaveVifData(itemModel.Label + ".txt", () =>
                {
                    var vifData = lmpFile.FileData.AsSpan().Slice(
                        element.DataInfo.VifDataOffset,
                        element.DataInfo.VifDataOffset + element.DataInfo.VifDataLength);
                    return VifDecoder.ReadVerts(NullLogger.Instance, vifData);
                });
                break;
            }
        }
    }

    private void LogTexDataClicked(object sender, RoutedEventArgs e)
    {
        var engineVersion = App.Settings.Get<EngineVersion>("Core.EngineVersion");
        if (EngineVersion.DarkAlliance == engineVersion)
        {
            MessageBox.Show(_window, "Not supported for Dark Alliance files.",
                "Error", MessageBoxButton.OK);
            return;
        }

        var worldTex = _window.ViewModel.World?.WorldTex;
        if (worldTex == null)
        {
            MessageBox.Show(_window, "Error: Missing World Tex data.",
                "Error", MessageBoxButton.OK);
            return;
        }

        var entries = WorldTexFile.ReadEntries(worldTex.FileData);
        var sb      = new StringBuilder();
        sb.AppendLine($"Debug Info For: {worldTex.FileName}");
        sb.AppendLine();

        for (var i = 0; i < entries.Length; i++)
        {
            sb.AppendLine("Entry " + i);
            sb.AppendLine("Cell Offset: "      + entries[i].CellOffset);
            sb.AppendLine("Directory Offset: " + entries[i].DirectoryOffset);
            sb.AppendLine("Size: "             + entries[i].Size);
            if (i < entries.Length - 1) sb.AppendLine();
        }

        _window.ViewModel.LogText = sb.ToString();
        _window.tabControl.SelectedIndex = 4; // Log View
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // Click handlers — new: per-entry export shortcuts
    // ─────────────────────────────────────────────────────────────────────────

    #region Per-entry export shortcuts

    private void ExportAsPngClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpEntryTreeViewModel lmpEntry) return;

        var lmpFile = lmpEntry.LmpFileProperty;
        var entry   = lmpFile.Directory[lmpEntry.Label];

        byte[] bytes;
        if (lmpFile.PendingEdits.TryGetValue(lmpEntry.Label, out var pending))
            bytes = pending;
        else
        {
            bytes = new byte[entry.Length];
            Buffer.BlockCopy(lmpFile.FileData, entry.StartOffset, bytes, 0, entry.Length);
        }

        var bitmap = TexDecoder.Decode(bytes);
        if (bitmap == null)
        {
            MessageBox.Show("Could not decode texture.", "Error", MessageBoxButton.OK);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = Path.GetFileNameWithoutExtension(lmpEntry.Label) + ".png",
            Filter   = "PNG Image|*.png"
        };
        if (dialog.ShowDialog() != true) return;

        using var stream  = new FileStream(dialog.FileName, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);

        MessageBox.Show($"Texture saved to:\n{dialog.FileName}", "Export Complete",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportAsModelClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpEntryTreeViewModel lmpEntry) return;

        // Delegate to the main window's model export flow so we reuse the
        // already-decoded Model / Texture from the current selection.
        if (_window.ViewModel.TheModelViewModel.VifModel == null)
        {
            MessageBox.Show(
                "Please select the .vif entry in the tree first so the model is loaded.",
                "No Model Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _window.ViewModel.TheModelViewModel.ShowExportForPosedModel();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // Click handlers — new: per-entry edit actions
    // ─────────────────────────────────────────────────────────────────────────

    #region Per-entry edit actions

    private void ReplaceEntryClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpEntryTreeViewModel lmpEntry) return;

        var lmpFile = lmpEntry.LmpFileProperty;
        var ext     = (Path.GetExtension(lmpEntry.Label) ?? "*").TrimStart('.');
        var dialog  = new OpenFileDialog
        {
            Title  = $"Select replacement file for '{lmpEntry.Label}'",
            Filter = $"{ext.ToUpper()} Files|*.{ext}|All Files|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            AssetImporter.ReplaceEntryFromFile(lmpFile, lmpEntry.Label, dialog.FileName);
            _window.UpdateTitle();
            MessageBox.Show(
                $"'{lmpEntry.Label}' has been queued for replacement with:\n{dialog.FileName}\n\n" +
                "Use 'Save Archive…' (right-click the archive node) to write the changes to disk.",
                "Entry Queued", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Replace failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteEntryClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpEntryTreeViewModel lmpEntry) return;

        var result = MessageBox.Show(
            $"Schedule '{lmpEntry.Label}' for deletion?\n\n" +
            "The entry will be removed the next time you save the archive.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            lmpEntry.LmpFileProperty.DeleteEntry(lmpEntry.Label);
            _window.UpdateTitle();
            MessageBox.Show(
                $"'{lmpEntry.Label}' is scheduled for deletion.\n\n" +
                "Use 'Save Archive…' to write the changes to disk.",
                "Deletion Queued", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // Click handlers — new: per-archive actions
    // ─────────────────────────────────────────────────────────────────────────

    #region Per-archive actions

    private void AddNewEntryClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpTreeViewModel lmpTree) return;

        var lmpFile = lmpTree.LmpFileProperty;

        var openDialog = new OpenFileDialog
        {
            Title  = $"Select file to add to '{lmpFile.Name}'",
            Filter = "All Files|*.*"
        };
        if (openDialog.ShowDialog() != true) return;

        var suggestedName = Path.GetFileName(openDialog.FileName);

        // Ask the user to confirm / rename the entry
        var nameDialog = new EntryNameDialog(suggestedName) { Owner = _window };
        if (nameDialog.ShowDialog() != true) return;

        var entryName = nameDialog.EntryName;
        if (string.IsNullOrWhiteSpace(entryName))
        {
            MessageBox.Show("Entry name cannot be empty.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            AssetImporter.AddEntryFromFile(lmpFile, entryName, openDialog.FileName);
            _window.UpdateTitle();
            MessageBox.Show(
                $"Entry '{entryName}' added to the pending queue.\n\n" +
                "Use 'Save Archive…' to write the changes to disk.",
                "Entry Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Add failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BatchExportTexturesClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpTreeViewModel lmpTree) return;

        var lmpFile = lmpTree.LmpFileProperty;

        using var folderDialog = new FolderBrowserDialog
        {
            Description = $"Choose output folder for textures from '{lmpFile.Name}'"
        };
        if (folderDialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var count = AssetImporter.BatchExportTextures(lmpFile, folderDialog.SelectedPath);
            MessageBox.Show(
                $"Exported {count} texture(s) to:\n{folderDialog.SelectedPath}",
                "Batch Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Batch export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BatchExportAllClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpTreeViewModel lmpTree) return;

        var lmpFile = lmpTree.LmpFileProperty;

        using var folderDialog = new FolderBrowserDialog
        {
            Description = $"Choose output folder for all entries from '{lmpFile.Name}'"
        };
        if (folderDialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var count = AssetImporter.BatchExportAllEntries(lmpFile, folderDialog.SelectedPath);
            MessageBox.Show(
                $"Exported {count} entr{(count == 1 ? "y" : "ies")} to:\n{folderDialog.SelectedPath}",
                "Batch Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Batch export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveArchiveClicked(object sender, RoutedEventArgs e)
    {
        if (_menu.DataContext is not LmpTreeViewModel lmpTree) return;

        var lmpFile = lmpTree.LmpFileProperty;

        var dialog = new SaveFileDialog
        {
            FileName = lmpFile.Name,
            Filter   = "LMP Archive|*.lmp|All Files|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            AssetImporter.SaveArchive(lmpFile, dialog.FileName);
            _window.UpdateTitle();
            MessageBox.Show(
                $"Archive saved to:\n{dialog.FileName}",
                "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // Shared prompt helpers (original, unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private void PromptToSaveData(string fileName, Action<string> saveFunc)
    {
        var dialog = new SaveFileDialog { FileName = fileName };
        if (dialog.ShowDialog() != true) return;
        saveFunc(dialog.FileName);
    }

    private void PromptToSaveVifData(string fileName, Func<List<VifDecoder.Chunk>> chunkFunc)
    {
        PromptToSaveData(fileName, saveFilePath =>
        {
            VifChunkExporter.WriteChunks(saveFilePath, chunkFunc());
        });
    }
}
