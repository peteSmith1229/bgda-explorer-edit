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

using HelixToolkit.Wpf;
using JetBlackEngineLib;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Textures;
using JetBlackEngineLib.Data.World;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataExporters;
using WorldExplorer.DataImporters;
using WorldExplorer.TreeView;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace WorldExplorer;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    private FileTreeViewContextManager _contextManager = null!;

    public MainWindowViewModel ViewModel { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        SetupViewports();

        App.LoadSettings();
        JetBlackEngineLib.Data.Textures.PalEntry.ForceOpaque =
            App.Settings.Get("Textures.ForceOpaque", false);

        _contextManager = new FileTreeViewContextManager(this, treeView);
        ViewModel       = new MainWindowViewModel(this, App.Settings.Get("Files.DataPath", "") ?? "");
        DataContext      = ViewModel;

        // Required so Tools → Settings is not greyed out.
        CommandBinding propertiesBinding = new(ApplicationCommands.Properties);
        propertiesBinding.Executed   += Properties_Executed;
        propertiesBinding.CanExecute += Properties_CanExecute;
        CommandBindings.Add(propertiesBinding);

        var lastFile = App.Settings.Get("Files.LastLoadedFile", "") ?? "";
        if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile))
            OpenFile(lastFile);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Title bar
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the window title to reflect whether the loaded archive has
    /// unsaved edits.  Called by the context manager after any edit operation.
    /// </summary>
    public void UpdateTitle()
    {
        var lmpDirty = ViewModel.World?.WorldLmp?.IsDirty == true;
        var gobDirty = ViewModel.World?.WorldGob?.IsDirty == true;
        Title = (lmpDirty || gobDirty) ? "World Explorer [Modified]" : "World Explorer";
        ViewModel.NotifyIsArchiveDirty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Viewport setup (unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private void SetupViewports()
    {
        HelixViewport3D[] viewports = { ModelView.viewport, SkeletonView.viewport, LevelView.viewport };

        foreach (var viewport in viewports)
        {
            viewport.ResetCameraGesture    = null;
            viewport.ResetCameraKeyGesture = null;
            viewport.RotateGesture         = new MouseGesture(MouseAction.LeftClick);
            viewport.PanGesture            = new MouseGesture(MouseAction.MiddleClick);

            viewport.PreviewMouseDown += (s, ev) =>
            {
                if (ev.ChangedButton == MouseButton.Middle && ev.ClickCount > 1)
                {
                    var v = (HelixViewport3D)s;
                    v.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0),
                              new Vector3D(0, 0, 1), 1000);
                    ev.Handled = true;
                }
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // File open / recent files (unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    public void OpenFile(string file)
    {
        ViewModel.LoadFile(file);
        UpdateTitle();

        var recentFiles =
            (App.Settings.Get("Files.RecentFiles", "") ?? "")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var list = recentFiles.ToList();

        if (list.Count >= 10)
            list.RemoveRange(9, list.Count - 9);

        if (list.Contains(file))
            list.Remove(file);

        list.Insert(0, file);

        App.Settings["Files.RecentFiles"]    = string.Join(",", list);
        App.Settings["Files.LastLoadedFile"] = file;
        App.SaveSettings();
    }

    protected override void OnClosed(EventArgs e)
    {
        App.SaveSettings();
    }

    private void Properties_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SettingsWindow window = new() { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (window.ShowDialog() == true)
            ViewModel.SettingsChanged();
    }

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        ViewModel.SelectedNode = e.NewValue;
    }

    private void MenuOpenFileClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Multiselect = false };
        if (dialog.ShowDialog().GetValueOrDefault(false))
            OpenFile(dialog.FileName);
    }

    private void MenuExitClick(object sender, RoutedEventArgs e) => Close();

    private void MenuRecentFilesSubmenuOpened(object sender, RoutedEventArgs e)
    {
        MenuRecentFiles.Items.Clear();

        var recentFiles =
            (App.Settings.Get("Files.RecentFiles", "") ?? "")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (recentFiles.Length > 0)
        {
            foreach (var file in recentFiles)
            {
                MenuItem menu = new() { Header = file, Tag = file };
                menu.Click += (o, _) => OpenFile((string)((MenuItem)o).Tag);
                MenuRecentFiles.Items.Add(menu);
            }
        }
        else
        {
            MenuRecentFiles.Items.Add(new MenuItem { Header = "No Recent Files", IsEnabled = false });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // File → Save Archive (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    private void Menu_SaveArchive_Click(object sender, RoutedEventArgs e)
    {
        var lmpFile = GetActiveLmpFile();
        if (lmpFile == null)
        {
            MessageBox.Show(this, "No archive is currently loaded.", "Save Archive",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!lmpFile.IsDirty)
        {
            MessageBox.Show(this, "No pending changes to save.", "Save Archive",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = lmpFile.Name,
            Filter   = "LMP Archive|*.lmp|All Files|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            AssetImporter.SaveArchive(lmpFile, dialog.FileName);
            UpdateTitle();
            MessageBox.Show(this, $"Archive saved to:\n{dialog.FileName}",
                "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Returns the GOB file for the currently loaded world, or null if no GOB
    /// is open.
    /// </summary>
    private GobFile? GetActiveGobFile() => ViewModel.World?.WorldGob;
 
    // ════════════════════════════════════════════════════════════════
    // 2. Add the click handler for File → Save GOB…
    //    Place it alongside Menu_SaveArchive_Click.
    // ════════════════════════════════════════════════════════════════
 
    private void Menu_SaveGob_Click(object sender, RoutedEventArgs e)
{
    var gob = GetActiveGobFile();
    if (gob == null)
    {
        MessageBox.Show(this, "No GOB file is currently loaded.", "Save GOB",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }
 
    if (!gob.IsDirty)
    {
        MessageBox.Show(this, "No pending changes to save.", "Save GOB",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }
 
    var dialog = new SaveFileDialog
    {
        FileName = gob.Name,
        Filter   = "GOB Archive|*.gob|All Files|*.*"
    };
    if (dialog.ShowDialog() != true) return;
 
    try
    {
        var report = new StringBuilder();
 
        // ── 1. Live in-memory state ──────────────────────────────────────
        var liveCount = ViewModel.TheLevelViewModel.ObjectManager.Objects.Count;
        report.AppendLine($"In-memory objects (ObjectManager): {liveCount}");
 
        // ── 2. Pending-edit state on every dirty LMP ─────────────────────
        var pendingObLength = -1;
        foreach (var (lmpName, lmp) in gob.Directory)
        {
            if (!lmp.IsDirty) continue;
 
            report.AppendLine($"Dirty LMP: {lmpName}");
            foreach (var key in lmp.PendingEdits.Keys)
            {
                var len = lmp.PendingEdits[key].Length;
                report.AppendLine($"   pending edit: {key} ({len} bytes)");
                if (key.Equals("objects.ob", StringComparison.OrdinalIgnoreCase))
                {
                    pendingObLength = len;
                }
            }
        }
        if (pendingObLength < 0)
        {
            report.AppendLine("WARNING: no pending edit for objects.ob found!");
        }
 
        // ── 3. Pack, then verify the packed bytes round-trip ─────────────
        var packed = GobWriter.Pack(gob);
 
        var savedObCount = -1;
        var verifyGob = new GobFile(gob.EngineVersion, "verify.gob", packed);
        foreach (var (lmpName, lmp) in verifyGob.Directory)
        {
            lmp.ReadDirectory();
            if (lmp.Directory.TryGetValue("objects.ob", out var obEntry))
            {
                var objs = ObDecoder.Decode(lmp.FileData, obEntry.StartOffset, obEntry.Length);
                savedObCount = objs.Count;
                report.AppendLine(
                    $"objects.ob decoded from PACKED bytes ({lmpName}): {savedObCount} objects");
                break;
            }
        }
        if (savedObCount < 0)
        {
            report.AppendLine("WARNING: no objects.ob entry found in the packed GOB!");
        }
 
        // ── 4. Write to disk and clear dirty state ───────────────────────
        File.WriteAllBytes(dialog.FileName, packed);
        gob.ClearAllPendingEdits();
        UpdateTitle();
 
        var texCopied = CopyCompanionTexFile(dialog.FileName);
        if (texCopied)
        {
            report.AppendLine("Companion .TEX copied alongside the GOB.");
        }
 
        // Surface the diagnostic both in a dialog and in the Log tab.
        ViewModel.LogText = report.ToString();
        MessageBox.Show(this,
            $"GOB saved to:\n{dialog.FileName}\n\n--- Diagnostics ---\n{report}",
            "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

    // ─────────────────────────────────────────────────────────────────────────
    // Edit → Import (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the bytes of whichever archive entry is currently selected in
    /// the tree with an external file chosen by the user.
    /// </summary>
    private void Menu_Import_ReplaceEntry_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is not LmpEntryTreeViewModel selectedEntry)
        {
            MessageBox.Show(this,
                "Please select an archive entry in the tree first.",
                "Replace Entry", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var lmpFile = selectedEntry.LmpFileProperty;
        if (lmpFile is ClpFile)
        {
            MessageBox.Show(this,
                "Editing CLP archives is not supported (hash-indexed format).",
                "Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ext    = (Path.GetExtension(selectedEntry.Label) ?? "*").TrimStart('.');
        var dialog = new OpenFileDialog
        {
            Title  = $"Select replacement file for '{selectedEntry.Label}'",
            Filter = $"{ext.ToUpper()} Files|*.{ext}|All Files|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            AssetImporter.ReplaceEntryFromFile(lmpFile, selectedEntry.Label, dialog.FileName);
            UpdateTitle();
            MessageBox.Show(this,
                $"'{selectedEntry.Label}' queued for replacement.\n\n" +
                "Save the archive (File → Save Archive…) to write the changes to disk.",
                "Queued", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Replace failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Adds a new file as an entry to the currently loaded top-level LMP archive.
    /// </summary>
    private void Menu_Import_AddEntry_Click(object sender, RoutedEventArgs e)
    {
        var lmpFile = GetActiveLmpFile();
        if (lmpFile == null || lmpFile is ClpFile)
        {
            MessageBox.Show(this,
                "No writable LMP archive is currently loaded.",
                "Add Entry", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var openDialog = new OpenFileDialog
        {
            Title  = $"Select file to add to '{lmpFile.Name}'",
            Filter = "All Files|*.*"
        };
        if (openDialog.ShowDialog() != true) return;

        var nameDialog = new EntryNameDialog(Path.GetFileName(openDialog.FileName))
            { Owner = this };
        if (nameDialog.ShowDialog() != true) return;

        var entryName = nameDialog.EntryName;
        if (string.IsNullOrWhiteSpace(entryName))
        {
            MessageBox.Show(this, "Entry name cannot be empty.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            AssetImporter.AddEntryFromFile(lmpFile, entryName, openDialog.FileName);
            UpdateTitle();
            MessageBox.Show(this,
                $"Entry '{entryName}' added to the pending queue.\n\n" +
                "Save the archive (File → Save Archive…) to write the changes to disk.",
                "Entry Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Add failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edit → Batch Export (NEW)
    // ─────────────────────────────────────────────────────────────────────────

    private void Menu_BatchExport_Textures_Click(object sender, RoutedEventArgs e)
    {
        var lmpFile = GetActiveLmpFile();
        if (lmpFile == null)
        {
            MessageBox.Show(this, "No archive is currently loaded.", "Batch Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var folderDialog = new FolderBrowserDialog
        {
            Description = $"Choose output folder for textures from '{lmpFile.Name}'"
        };
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        try
        {
            var count = AssetImporter.BatchExportTextures(lmpFile, folderDialog.SelectedPath);
            MessageBox.Show(this,
                $"Exported {count} texture(s) to:\n{folderDialog.SelectedPath}",
                "Batch Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Batch export failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Menu_BatchExport_AllEntries_Click(object sender, RoutedEventArgs e)
    {
        var lmpFile = GetActiveLmpFile();
        if (lmpFile == null)
        {
            MessageBox.Show(this, "No archive is currently loaded.", "Batch Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var folderDialog = new FolderBrowserDialog
        {
            Description = $"Choose output folder for all entries from '{lmpFile.Name}'"
        };
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        try
        {
            var count = AssetImporter.BatchExportAllEntries(lmpFile, folderDialog.SelectedPath);
            MessageBox.Show(this,
                $"Exported {count} entr{(count == 1 ? "y" : "ies")} to:\n{folderDialog.SelectedPath}",
                "Batch Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Batch export failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edit → Export (unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private void Menu_Export_Texture_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNodeImage == null)
        {
            MessageBox.Show(this, "No texture currently loaded.", "Error", MessageBoxButton.OK);
            return;
        }

        var dialog = new SaveFileDialog { Filter = "PNG Image|*.png" };
        if (!dialog.ShowDialog(this).GetValueOrDefault(false)) return;

        using var stream  = new FileStream(dialog.FileName, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(ViewModel.SelectedNodeImage));
        encoder.Save(stream);
        stream.Flush();
    }

    private void Menu_Export_TextureWeaved_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNodeImage == null)
        {
            MessageBox.Show(this, "No texture currently loaded.", "Error", MessageBoxButton.OK);
            return;
        }

        var src = ViewModel.SelectedNodeImage;
        if (src.PixelHeight < 2 || (src.PixelHeight & 1) != 0)
        {
            MessageBox.Show(this, "Texture height must be even to weave fields.", "Error",
                MessageBoxButton.OK);
            return;
        }

        var dialog = new SaveFileDialog { Filter = "PNG Image|*.png" };
        if (!dialog.ShowDialog(this).GetValueOrDefault(false)) return;

        var woven   = WeaveInterlacedFields(src);
        using var stream  = new FileStream(dialog.FileName, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(woven));
        encoder.Save(stream);
    }

    /// <summary>
    /// Interleaves a texture stored as two stacked fields (top half + bottom half)
    /// into a single image, taking output line 2k from top-half line k and output
    /// line 2k+1 from bottom-half line k.
    /// </summary>
    private static BitmapSource WeaveInterlacedFields(BitmapSource src)
    {
        var converted = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        int w = converted.PixelWidth, h = converted.PixelHeight, halfH = h / 2;
        int stride = w * 4;
        byte[] pixels = new byte[stride * h];
        converted.CopyPixels(pixels, stride, 0);

        byte[] output = new byte[stride * h];
        for (int y = 0; y < halfH; y++)
        {
            Buffer.BlockCopy(pixels, y * stride,          output, (2 * y)     * stride, stride);
            Buffer.BlockCopy(pixels, (halfH + y) * stride, output, (2 * y + 1) * stride, stride);
        }
        return BitmapSource.Create(w, h, src.DpiX, src.DpiY,
                                   PixelFormats.Bgra32, null, output, stride);
    }

    private void Menu_Export_Model_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TheModelViewModel.VifModel == null)
        {
            MessageBox.Show(this, "No model currently loaded.", "Error", MessageBoxButton.OK);
            return;
        }
        if (ViewModel.TheModelViewModel.Texture == null)
        {
            MessageBox.Show(this, "Model does not have a texture.", "Error", MessageBoxButton.OK);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter      = "GLTF File|*.gltf|OBJ File|*.obj",
            FilterIndex = 1,
            FileName    = "some-model.gltf"
        };
        if (dialog.ShowDialog() != true) return;

        var ext      = Path.GetExtension(dialog.FileName).ToUpperInvariant();
        IVifExporter? exporter = ext switch
        {
            ".OBJ"  => new VifObjExporter(),
            ".GLTF" => new VifGltfExporter(),
            _       => null
        };
        if (exporter == null)
        {
            MessageBox.Show("Unknown file format.", "Error", MessageBoxButton.OK);
            return;
        }

        exporter.SaveToFile(dialog.FileName, ViewModel.TheModelViewModel.VifModel,
            ViewModel.TheModelViewModel.Texture);
    }

    private void Menu_Export_PosedModel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TheModelViewModel.ShowExportForPosedModel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: resolve "active" LMP for the top-level menu actions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the primary LMP file for the currently loaded world.
    /// Prefers the LMP file that the selected tree node belongs to; falls back
    /// to <c>World.WorldLmp</c>.  Returns null if nothing is loaded.
    /// </summary>
    private LmpFile? GetActiveLmpFile()
    {
        // If the selected node belongs to an LMP, use that.
        switch (ViewModel.SelectedNode)
        {
            case LmpEntryTreeViewModel entry:
                return entry.LmpFileProperty;
            case LmpTreeViewModel tree:
                return tree.LmpFileProperty;
        }

        // Fall back to the world's top-level LMP.
        return ViewModel.World?.WorldLmp;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Camera frame helper (unchanged — used by FrameModel)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Frames the viewport camera to show the given model's bounding box.
    /// We choose between viewing along -Y or along -X depending on which
    /// gives the larger visible projection (sizeX·sizeZ vs sizeY·sizeZ).
    /// </summary>
    private static void FrameModel(HelixViewport3D viewport,
                                   JetBlackEngineLib.Data.Models.Model? model)
    {
        if (viewport.Camera is not ProjectionCamera cam) return;

        if (model == null || !TryGetModelBounds(model, out var bounds))
        {
            cam.Position      = new Point3D(0, -100, 50);
            cam.LookDirection = new Vector3D(0, 100, -50);
            cam.UpDirection   = new Vector3D(0, 0, 1);
            if (cam is OrthographicCamera oc0) oc0.Width = 200;
            return;
        }

        var cx       = bounds.X + bounds.SizeX / 2;
        var cy       = bounds.Y + bounds.SizeY / 2;
        var cz       = bounds.Z + bounds.SizeZ / 2;
        var span     = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
        if (span < 1) span = 1;
        var distance  = span * 2.0;
        var elevation = span * 0.4;

        if (bounds.SizeY * bounds.SizeZ > bounds.SizeX * bounds.SizeZ)
        {
            cam.Position      = new Point3D(cx - distance, cy, cz + elevation);
            cam.LookDirection = new Vector3D(distance, 0, -elevation);
        }
        else
        {
            cam.Position      = new Point3D(cx, cy - distance, cz + elevation);
            cam.LookDirection = new Vector3D(0, distance, -elevation);
        }
        cam.UpDirection = new Vector3D(0, 0, 1);
        if (cam is OrthographicCamera oc) oc.Width = distance * 1.5;
    }

    private static bool TryGetModelBounds(JetBlackEngineLib.Data.Models.Model model,
                                          out Rect3D bounds)
    {
        bounds = Rect3D.Empty;
        if (model.MeshList.Count == 0) return false;

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var mesh in model.MeshList)
        {
            foreach (var v in mesh.Positions)
            {
                if (v.X < minX) minX = v.X; if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y; if (v.Y > maxY) maxY = v.Y;
                if (v.Z < minZ) minZ = v.Z; if (v.Z > maxZ) maxZ = v.Z;
            }
        }

        if (minX > maxX) return false;
        bounds = new Rect3D(minX, minY, minZ,
                            maxX - minX, maxY - minY, maxZ - minZ);
        return true;
    }
    
    /// <summary>
    /// Sets the title and subtitle overlay text on one of the three Helix
    /// viewports. Called by MainWindowViewModel when a new asset is selected.
    ///   index 1 → Model viewport
    ///   index 2 → Skeleton viewport
    ///   index 3 → Level viewport
    /// </summary>
    public void SetViewportText(int index, string title, string subTitle)
    {
        switch (index)
        {
            case 1:
                if (title != null) ModelView.viewport.Title    = title;
                if (subTitle != null) ModelView.viewport.SubTitle = subTitle;
                break;
            case 2:
                if (title != null) SkeletonView.viewport.Title    = title;
                if (subTitle != null) SkeletonView.viewport.SubTitle = subTitle;
                break;
            case 3:
                if (title != null) LevelView.viewport.Title    = title;
                if (subTitle != null) LevelView.viewport.SubTitle = subTitle;
                break;
        }
    }
 
    /// <summary>
    /// Re-frames the active viewport's camera to fit the currently loaded
    /// content. For the model and skeleton tabs this calls FrameModel; for the
    /// level tab it calls ZoomExtents on the world bounding box.
    /// Called by MainWindowViewModel after loading a world or switching tabs.
    /// </summary>
    public void ResetCamera()
    {
        switch (tabControl.SelectedIndex)
        {
            case 1:
                FrameModel(ModelView.viewport, ViewModel.TheModelViewModel.VifModel);
                break;
            case 2:
                FrameModel(SkeletonView.viewport, ViewModel.TheModelViewModel.VifModel);
                break;
            case 3:
                var bounds = ViewModel.TheLevelViewModel.WorldBounds;
                if (!bounds.IsEmpty)
                    LevelView.viewport.ZoomExtents(bounds, 1000);
                break;
        }
    }
    
    /// <summary>
    /// BGDA level textures live in a companion .TEX file next to the .GOB
    /// (TOWN.GOB ↔ TOWN.TEX) — they are not inside the archive. When the GOB
    /// is saved to a new folder or name, copy the companion file alongside it
    /// with a matching base name so the level still loads textured.
    /// Returns true if a copy was made.
    /// </summary>
    private bool CopyCompanionTexFile(string gobDestinationPath)
    {
        var world = ViewModel.World;
        if (world == null) return false;

        // Locate the original companion TEX next to the source GOB.
        var baseName  = Path.GetFileNameWithoutExtension(world.Name);
        var sourceTex = Path.Combine(world.DataPath, baseName + ".TEX");
        if (!File.Exists(sourceTex))
        {
            sourceTex = Path.Combine(world.DataPath, baseName + ".tex");
            if (!File.Exists(sourceTex)) return false;   // world has no level TEX
        }

        // Destination: same folder + base name as the saved GOB.
        var destTex = Path.Combine(
            Path.GetDirectoryName(gobDestinationPath) ?? "",
            Path.GetFileNameWithoutExtension(gobDestinationPath) + Path.GetExtension(sourceTex));

        // Saving in place — the TEX is already there.
        if (string.Equals(Path.GetFullPath(sourceTex), Path.GetFullPath(destTex),
                StringComparison.OrdinalIgnoreCase))
            return false;

        // Don't clobber an existing file; the TEX is never modified by the
        // editor, so an existing copy is already correct.
        if (File.Exists(destTex)) return false;

        File.Copy(sourceTex, destTex);
        return true;
    }
    
}
