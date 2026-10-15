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
    
    /// <summary>
    /// The complete original bytes of this GOB exactly as loaded from disk.
    /// Every contained <see cref="LmpFile"/> shares this same array as its
    /// <see cref="LmpFile.FileData"/>; entry <c>StartOffset</c> values are
    /// absolute within it.  Used by <see cref="GobWriter.TryPatchInPlace"/>.
    /// </summary>
    public byte[] RawFileData => _fileData;
    
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
    /// Most archives a GOB can hold. The game reads the directory as one
    /// 2,048-byte sector: 40 bytes an archive, then the terminating zero.
    /// </summary>
    public const int MaxArchives = 51;

    /// <summary>Longest archive name: the directory's name field is 32 bytes, terminator included.</summary>
    public const int MaxArchiveNameLength = 31;

    private readonly List<string> _addedArchives = new();

    /// <summary>Archives added since the GOB was opened, in the order they were added.</summary>
    public IReadOnlyList<string> AddedArchives => _addedArchives;

    /// <summary>
    /// True when an archive was added or any of the LMP files contained in this
    /// GOB has unsaved edits (replacements, deletions, or additions queued via
    /// <see cref="LmpFile.ReplaceEntry"/>, <see cref="LmpFile.DeleteEntry"/>,
    /// or <see cref="LmpFile.AddEntry"/>).
    /// </summary>
    public bool IsDirty => _addedArchives.Count > 0 || Directory.Values.Any(lmp => lmp.IsDirty);

    /// <summary>
    /// Clears the pending-edit state on every contained LMP file.
    /// Call this after a successful <see cref="GobWriter.Pack"/> + disk-write
    /// so that <see cref="IsDirty"/> returns <see langword="false"/> again.
    /// Added archives stay added: they aren't in <see cref="RawFileData"/>.
    /// </summary>
    public void ClearAllPendingEdits()
    {
        foreach (var lmp in Directory.Values)
            lmp.ClearPendingEdits();
    }

    // -------------------------------------------------------------------------
    // Adding archives
    // -------------------------------------------------------------------------

    /// <summary>
    /// The name an archive is stored under: lower case, as in the game's own
    /// GOBs, because the game looks archives up by exact name ("tman2.lmp").
    /// </summary>
    public static string NormalizeArchiveName(string name) => name.Trim().ToLowerInvariant();

    /// <summary>Why an archive called <paramref name="name"/> can't be added, or null when it can.</summary>
    public string? CheckNewArchiveName(string name)
    {
        name = NormalizeArchiveName(name);
        if (name.Length == 0) return "Enter a name.";
        if (name.Length > MaxArchiveNameLength) return $"Use {MaxArchiveNameLength} characters or fewer.";
        if (!name.EndsWith(".lmp", StringComparison.Ordinal)) return "The name must end in .lmp.";
        foreach (var c in name)
        {
            if (c <= ' ' || c > '~' || c is '/' or '\\') return "Use plain letters, digits and punctuation, without spaces or slashes.";
        }

        if (Directory.Keys.Any(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)))
            return $"{Name} already has {name}.";
        if (Directory.Count >= MaxArchives)
            return $"{Name} is full: a GOB can hold at most {MaxArchives} archives.";
        return null;
    }

    /// <summary>
    /// Adds an archive (the bytes of an .lmp file) to the GOB; saving writes it
    /// and the game loads it with the level. It goes before env.lmp (or the
    /// level's own archive), where the game's GOBs keep the archives their
    /// objects use. Throws <see cref="ArgumentException"/> for a name that can't
    /// be used (see <see cref="CheckNewArchiveName"/>) and
    /// <see cref="InvalidDataException"/> when the data isn't an archive.
    /// </summary>
    public LmpFile AddArchive(string name, byte[] data)
    {
        var problem = CheckNewArchiveName(name);
        if (problem != null) throw new ArgumentException(problem, nameof(name));
        name = NormalizeArchiveName(name);

        var lmp = new LmpFile(EngineVersion, name, data, 0, data.Length);
        CheckArchive(lmp);

        var entries = Directory.ToList();
        entries.Insert(InsertionIndex(entries), new KeyValuePair<string, LmpFile>(name, lmp));
        ReplaceDirectory(entries);
        _addedArchives.Add(name);
        return lmp;
    }

    /// <summary>Takes back an archive added with <see cref="AddArchive"/>.</summary>
    public void RemoveAddedArchive(string name)
    {
        if (!_addedArchives.Remove(name))
            throw new ArgumentException($"{name} wasn't added to {Name}.", nameof(name));
        ReplaceDirectory(Directory.Where(e => e.Key != name).ToList());
    }

    /// <summary>Before env.lmp, waterenv.lmp or the level's archive, whichever comes first; else last.</summary>
    private int InsertionIndex(List<KeyValuePair<string, LmpFile>> entries)
    {
        var level = Path.GetFileNameWithoutExtension(Name).ToLowerInvariant() + ".lmp";
        var index = entries.FindIndex(e => e.Key.Equals("env.lmp", StringComparison.OrdinalIgnoreCase) ||
                                           e.Key.Equals("waterenv.lmp", StringComparison.OrdinalIgnoreCase) ||
                                           e.Key.Equals(level, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? entries.Count : index;
    }

    /// <summary>Rebuilds the directory so it enumerates (and is saved) in <paramref name="entries"/>' order.</summary>
    private void ReplaceDirectory(List<KeyValuePair<string, LmpFile>> entries)
    {
        Directory.Clear();
        foreach (var (name, lmp) in entries) Directory.Add(name, lmp);
    }

    /// <summary>Throws <see cref="InvalidDataException"/> unless <paramref name="lmp"/> reads as an archive.</summary>
    private static void CheckArchive(LmpFile lmp)
    {
        var data = lmp.FileData;
        if (lmp.EngineVersion is not (EngineVersion.ReturnToArms or EngineVersion.JusticeLeagueHeroes))
        {
            var count = data.Length >= 4 ? BitConverter.ToInt32(data, 0) : 0;
            if (count <= 0 || 4L + 64L * count > data.Length)
                throw new InvalidDataException($"{lmp.Name} isn't an archive: its directory doesn't fit the file.");
        }

        try
        {
            lmp.ReadDirectory();
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or InvalidOperationException)
        {
            throw new InvalidDataException($"{lmp.Name} isn't an archive this game can read.", ex);
        }

        if (lmp.Directory.Count == 0) throw new InvalidDataException($"{lmp.Name} has no entries.");
        foreach (var entry in lmp.Directory.Values)
        {
            if (entry.StartOffset < 0 || entry.Length < 0 || (long)entry.StartOffset + entry.Length > data.Length)
                throw new InvalidDataException($"{lmp.Name} isn't an archive: {entry.Name} lies outside the file.");
        }
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
