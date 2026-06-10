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

using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Data.DataContainers;

public class GobFile
{
    public EngineVersion EngineVersion { get; }
    public readonly Dictionary<string, LmpFile> Directory = new();
    public string Name { get; set; }

    private readonly byte[] _fileData;

    // -------------------------------------------------------------------------
    // Constructors (unchanged)
    // -------------------------------------------------------------------------

    public GobFile(EngineVersion engineVersion, string filepath)
    {
        EngineVersion = engineVersion;
        Name          = Path.GetFileName(filepath);
        _fileData     = File.ReadAllBytes(filepath);
        ParseFileData();
    }

    public GobFile(EngineVersion engineVersion, string fileName, byte[] fileData)
    {
        EngineVersion = engineVersion;
        Name          = fileName;
        _fileData     = fileData;
        ParseFileData();
    }

    // -------------------------------------------------------------------------
    // Edit-state helpers (NEW)
    // -------------------------------------------------------------------------

    /// <summary>
    /// True when any of the LMP files contained in this GOB has unsaved edits
    /// (replacements, deletions, or additions queued via
    /// <see cref="LmpFile.ReplaceEntry"/>, <see cref="LmpFile.DeleteEntry"/>,
    /// or <see cref="LmpFile.AddEntry"/>).
    /// </summary>
    public bool IsDirty => Directory.Values.Any(lmp => lmp.IsDirty);

    /// <summary>
    /// Clears the pending-edit state on every contained LMP file.
    /// Call this after a successful <see cref="GobWriter.Pack"/> + disk-write
    /// so that <see cref="IsDirty"/> returns <see langword="false"/> again.
    /// </summary>
    public void ClearAllPendingEdits()
    {
        foreach (var lmp in Directory.Values)
            lmp.ClearPendingEdits();
    }

    // -------------------------------------------------------------------------
    // Existing private implementation (unchanged)
    // -------------------------------------------------------------------------

    private void ParseFileData()
    {
        var gobFile = ParseGobFile(EngineVersion, _fileData);
        foreach (var (name, entry) in gobFile.Entries)
        {
            Directory[name] = new LmpFile(EngineVersion, name, _fileData,
                                          entry.Offset, entry.Length);
        }
    }

    private static GobFileData ParseGobFile(EngineVersion engineVersion,
                                            ReadOnlySpan<byte> data)
    {
        var gobFile = new GobFileData { Entries = new Dictionary<string, GobFileEntry>() };
        var index = 0;
        var s = DataUtil.GetString(data, index);
        while (s.Length > 0)
        {
            var lmpOffset = BitConverter.ToInt32(data[(index + 0x20)..]);
            var lmpLen    = BitConverter.ToInt32(data[(index + 0x24)..]);
            gobFile.Entries[s] = new GobFileEntry(lmpOffset, lmpLen);
            index += 0x28;
            s = DataUtil.GetString(data, index);
        }
        return gobFile;
    }
}
