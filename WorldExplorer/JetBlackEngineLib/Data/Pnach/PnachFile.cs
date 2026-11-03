// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Pnach/PnachFile.cs
//
// PCSX2 cheat / patch files (.pnach), as the Dark Alliance II three-player mod ships:
//
//     gametitle=Baldur's Gate - Dark Alliance II (SLUS-20675)
//
//     [3 Player mod]
//     author=H + Claude
//     description=...
//     // a comment
//     patch=0,EE,000A0000,word,27BDFFF0
//
// PCSX2 names the file after the game's CRC (SLUS-20675_B0859096.pnach) and lists each
// [group] as a cheat that is ticked on or off in its settings, by name. patch=0 lines are
// written once when the game boots, patch=1 lines on every frame.
//
// The file is kept line for line: Parse then ToText gives back exactly the text that was
// read (line endings and the final newline included), so a group that is not changed is
// written back as it was.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Data.Pnach;

/// <summary>One "patch=" line: when it is applied, the CPU, the address, the size and the value.</summary>
public readonly record struct PnachPatch(int Place, string Cpu, uint Address, string Type, uint Value)
{
    /// <summary>A patch=0 word for the EE (written once, when the game boots).</summary>
    public static PnachPatch Word(uint address, uint value) => new(0, "EE", address, "word", value);

    /// <summary>A patch=1 word for the EE (written again on every frame).</summary>
    public static PnachPatch EveryFrame(uint address, uint value) => new(1, "EE", address, "word", value);

    /// <summary>The line in the format the mod's files use (upper-case hex, eight digits).</summary>
    public string ToLine() =>
        string.Create(CultureInfo.InvariantCulture, $"patch={Place},{Cpu},{Address:X8},{Type},{Value:X8}");

    /// <summary>Reads a "patch=" line; null for any other line or a line that isn't understood.</summary>
    public static PnachPatch? TryParse(string line)
    {
        var text = line.Trim();
        if (!text.StartsWith("patch=", StringComparison.OrdinalIgnoreCase)) return null;
        var parts = text.Substring(6).Split(',');
        if (parts.Length != 5) return null;
        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var place)) return null;
        if (!uint.TryParse(parts[2].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address)) return null;
        // a value can carry a comment after it in some files ("0000000A // ten")
        var value = parts[4].Trim();
        var space = value.IndexOfAny(new[] { ' ', '\t' });
        if (space > 0) value = value.Substring(0, space);
        if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return null;
        return new PnachPatch(place, parts[1].Trim(), address, parts[3].Trim(), v);
    }
}

/// <summary>A [group] of a pnach file: its name and every line after it, up to the next group.</summary>
public sealed class PnachGroup
{
    public PnachGroup(string name, IEnumerable<string>? lines = null)
    {
        Name = name;
        Lines = lines?.ToList() ?? new List<string>();
    }

    /// <summary>The name between the brackets, which PCSX2 shows and remembers the tick by.</summary>
    public string Name { get; set; }

    /// <summary>The lines after "[name]" as they are, a blank line before the next group included.</summary>
    public List<string> Lines { get; }

    /// <summary>The value of the first "key=" line (author, description ...), or null.</summary>
    public string? Get(string key)
    {
        var prefix = key + "=";
        foreach (var line in Lines)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return line.Substring(prefix.Length);
        }

        return null;
    }

    public string? Author => Get("author");
    public string? Description => Get("description");

    /// <summary>Every patch line of the group, in order.</summary>
    public IEnumerable<PnachPatch> Patches
    {
        get
        {
            foreach (var line in Lines)
            {
                if (PnachPatch.TryParse(line) is { } p) yield return p;
            }
        }
    }

    /// <summary>The patch lines' words by address (the last one wins, as in PCSX2); patch=0 / EE / word only.</summary>
    public Dictionary<uint, uint> BootWords()
    {
        var words = new Dictionary<uint, uint>();
        foreach (var p in Patches)
        {
            if (p.Place == 0 && p.Cpu.Equals("EE", StringComparison.OrdinalIgnoreCase) &&
                p.Type.Equals("word", StringComparison.OrdinalIgnoreCase))
                words[p.Address] = p.Value;
        }

        return words;
    }

    public PnachGroup Clone() => new(Name, Lines);

    public override string ToString() => "[" + Name + "]";
}

/// <summary>A whole .pnach file: the lines before the first group, then the groups in order.</summary>
public sealed class PnachFile
{
    public PnachFile(IEnumerable<string>? header = null, IEnumerable<PnachGroup>? groups = null)
    {
        Header = header?.ToList() ?? new List<string>();
        Groups = groups?.ToList() ?? new List<PnachGroup>();
    }

    /// <summary>Lines before the first group ("gametitle=...", comments, blank lines).</summary>
    public List<string> Header { get; }

    public List<PnachGroup> Groups { get; }

    /// <summary>"\n" or "\r\n", as the file that was read used.</summary>
    public string NewLine { get; set; } = "\n";

    /// <summary>Whether the last line ends with a newline.</summary>
    public bool EndsWithNewLine { get; set; } = true;

    /// <summary>The value of "gametitle=" in the header, or null.</summary>
    public string? GameTitle
    {
        get
        {
            foreach (var line in Header)
            {
                if (line.StartsWith("gametitle=", StringComparison.OrdinalIgnoreCase)) return line.Substring(10);
            }

            return null;
        }
    }

    public PnachGroup? Find(string name) =>
        Groups.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.Ordinal));

    /// <summary>Reads pnach text. Never fails: lines it doesn't understand are kept as they are.</summary>
    public static PnachFile Parse(string text)
    {
        var file = new PnachFile { NewLine = text.Contains("\r\n") ? "\r\n" : "\n" };
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
            file.EndsWithNewLine = true;
        }
        else
        {
            file.EndsWithNewLine = false;
        }

        PnachGroup? current = null;
        foreach (var line in lines)
        {
            if (GroupName(line) is { } name)
            {
                current = new PnachGroup(name);
                file.Groups.Add(current);
                continue;
            }

            if (current == null) file.Header.Add(line);
            else current.Lines.Add(line);
        }

        return file;
    }

    public static PnachFile Load(string path) => Parse(File.ReadAllText(path, Encoding.UTF8));

    /// <summary>The group name of a "[name]" line, or null for any other line.</summary>
    public static string? GroupName(string line)
    {
        var t = line.Trim();
        return t.Length >= 2 && t[0] == '[' && t[^1] == ']' ? t.Substring(1, t.Length - 2) : null;
    }

    public string ToText()
    {
        var sb = new StringBuilder();
        var all = new List<string>(Header);
        foreach (var g in Groups)
        {
            all.Add("[" + g.Name + "]");
            all.AddRange(g.Lines);
        }

        for (var i = 0; i < all.Count; i++)
        {
            sb.Append(all[i]);
            if (i < all.Count - 1 || EndsWithNewLine) sb.Append(NewLine);
        }

        return sb.ToString();
    }

    /// <summary>Writes the file as UTF-8 without a byte order mark (as PCSX2's own files are).</summary>
    public void Save(string path) => File.WriteAllText(path, ToText(), new UTF8Encoding(false));
}
