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

using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Textures;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace WorldExplorer.DataImporters;

/// <summary>
/// High-level helpers for importing raw files into LMP archives and for
/// batch-exporting archive contents.  All methods operate on the pending-edit
/// layer of <see cref="LmpFile"/> — no data is written to disk until the
/// caller explicitly calls <see cref="LmpWriter.Pack"/> and saves the result.
/// </summary>
public static class AssetImporter
{
    // -------------------------------------------------------------------------
    // Import
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads all bytes from <paramref name="sourceFilePath"/> and queues them
    /// as a replacement for <paramref name="entryName"/> inside
    /// <paramref name="archive"/>.  The change is not written to disk until
    /// the archive is packed and saved.
    /// </summary>
    /// <param name="archive">Target LMP archive.</param>
    /// <param name="entryName">
    /// Name of the entry to replace (must already exist in
    /// <see cref="LmpFile.Directory"/>).
    /// </param>
    /// <param name="sourceFilePath">Path of the replacement file on disk.</param>
    public static void ReplaceEntryFromFile(LmpFile archive,
                                            string entryName,
                                            string sourceFilePath)
    {
        var data = File.ReadAllBytes(sourceFilePath);
        archive.ReplaceEntry(entryName, data);
    }

    /// <summary>
    /// Reads all bytes from <paramref name="sourceFilePath"/> and queues them
    /// as a new entry (or replacement if the name already exists) inside
    /// <paramref name="archive"/>.
    /// </summary>
    /// <param name="archive">Target LMP archive.</param>
    /// <param name="entryName">Name for the new or replaced entry.</param>
    /// <param name="sourceFilePath">Path of the source file on disk.</param>
    public static void AddEntryFromFile(LmpFile archive,
                                        string entryName,
                                        string sourceFilePath)
    {
        var data = File.ReadAllBytes(sourceFilePath);
        archive.AddEntry(entryName, data);
    }

    // -------------------------------------------------------------------------
    // Batch export — textures
    // -------------------------------------------------------------------------

    /// <summary>
    /// Decodes every <c>.tex</c> entry in <paramref name="archive"/> and saves
    /// it as a PNG file inside <paramref name="outputDirectory"/>.
    /// </summary>
    /// <param name="archive">Source LMP archive (must already have its directory populated).</param>
    /// <param name="outputDirectory">Folder to write PNG files into (created if absent).</param>
    /// <returns>The number of textures successfully exported.</returns>
    public static int BatchExportTextures(LmpFile archive, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var count = 0;

        foreach (var (name, info) in archive.Directory)
        {
            if (!name.EndsWith(".tex", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                // Prefer pending-edit bytes if the entry has been replaced.
                byte[] bytes;
                if (archive.PendingEdits.TryGetValue(name, out var edited))
                {
                    bytes = edited;
                }
                else
                {
                    bytes = new byte[info.Length];
                    Buffer.BlockCopy(archive.FileData, info.StartOffset, bytes, 0, info.Length);
                }

                var bitmap = TexDecoder.Decode(bytes);
                if (bitmap == null) continue;

                var outName  = Path.GetFileNameWithoutExtension(name) + ".png";
                var outPath  = Path.Combine(outputDirectory, outName);

                using var stream = new FileStream(outPath, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                count++;
            }
            catch
            {
                // Skip entries that fail to decode; caller gets a count of successes.
            }
        }

        return count;
    }

    // -------------------------------------------------------------------------
    // Batch export — raw entries
    // -------------------------------------------------------------------------

    /// <summary>
    /// Writes every entry in <paramref name="archive"/> to a separate file
    /// inside <paramref name="outputDirectory"/>.  Pending-edit bytes are
    /// preferred over the original <see cref="LmpFile.FileData"/> bytes.
    /// Entries scheduled for deletion are skipped.
    /// </summary>
    /// <returns>The number of entries successfully exported.</returns>
    public static int BatchExportAllEntries(LmpFile archive, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var count = 0;

        foreach (var (name, info) in archive.Directory)
        {
            if (archive.PendingDeletions.Contains(name))
                continue;

            try
            {
                byte[] bytes;
                if (archive.PendingEdits.TryGetValue(name, out var edited))
                {
                    bytes = edited;
                }
                else
                {
                    bytes = new byte[info.Length];
                    Buffer.BlockCopy(archive.FileData, info.StartOffset, bytes, 0, info.Length);
                }

                // Sanitise the entry name so it is safe as a file name.
                var safeName = SanitiseFileName(name);
                var outPath  = Path.Combine(outputDirectory, safeName);
                File.WriteAllBytes(outPath, bytes);
                count++;
            }
            catch
            {
                // Skip problem entries.
            }
        }

        // Also export any pending additions that were added in this session.
        foreach (var (name, data) in archive.PendingAdditions)
        {
            if (archive.PendingDeletions.Contains(name))
                continue;
            try
            {
                var safeName = SanitiseFileName(name);
                var outPath  = Path.Combine(outputDirectory, safeName);
                File.WriteAllBytes(outPath, data);
                count++;
            }
            catch { }
        }

        return count;
    }

    // -------------------------------------------------------------------------
    // Save archive to disk
    // -------------------------------------------------------------------------

    /// <summary>
    /// Packs <paramref name="archive"/> (applying all pending edits) and
    /// writes the result to <paramref name="destinationPath"/>.  On success,
    /// clears <see cref="LmpFile.IsDirty"/> by calling
    /// <see cref="LmpFile.ClearPendingEdits"/>.
    /// </summary>
    public static void SaveArchive(LmpFile archive, string destinationPath)
    {
        var packed = LmpWriter.Pack(archive);
        File.WriteAllBytes(destinationPath, packed);
        archive.ClearPendingEdits();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string SanitiseFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb      = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        return sb.ToString();
    }
    
    /// <summary>
    /// Packs <paramref name="gobFile"/> — re-packing every contained LMP and
    /// applying all pending edits — and writes the result to
    /// <paramref name="destinationPath"/>.  On success clears the dirty state of
    /// every contained LMP by calling <see cref="GobFile.ClearAllPendingEdits"/>.
    /// </summary>
    public static void SaveGob(GobFile gobFile, string destinationPath)
    {
        var packed = GobWriter.Pack(gobFile);
        File.WriteAllBytes(destinationPath, packed);
        gobFile.ClearAllPendingEdits();
    }
}
