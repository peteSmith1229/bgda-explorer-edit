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

using JetBlackEngineLib.Data.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;


namespace WorldExplorer.WorldDefs;

/// <summary>
/// Copies <see cref="ObjectData"/> to and from the Windows clipboard as a
/// small JSON document.  Using the system clipboard (rather than an
/// app-internal field) means objects can be pasted between two running
/// WorldExplorer instances — e.g. copied from one level's GOB into another —
/// and inspected or hand-edited in any text editor.
///
/// Example payload:
/// <code>
/// {
///   "worldExplorerObject": 1,
///   "name": "Torch",
///   "i6": 4096,
///   "floats": [512.0, 832.0, 16.0],
///   "properties": ["w=10", "h=10"]
/// }
/// </code>
/// </summary>
public static class ObjectClipboard
{
    /// <summary>Format marker so we never misinterpret unrelated clipboard text.</summary>
    private const int FormatVersion = 1;

    private sealed class ObjectDto
    {
        public int WorldExplorerObject { get; set; }
        public string Name { get; set; } = "";
        public short I6 { get; set; }
        public float[] Floats { get; set; } = new float[3];
        public List<string> Properties { get; set; } = new();
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // -------------------------------------------------------------------------

    /// <summary>
    /// Serialises <paramref name="obj"/> and places it on the system
    /// clipboard.  Returns false if the clipboard was unavailable (it can be
    /// locked by another process).
    /// </summary>
    public static bool Copy(ObjectData obj)
    {
        var dto = new ObjectDto
        {
            WorldExplorerObject = FormatVersion,
            Name       = obj.Name ?? "",
            I6         = obj.I6,
            Floats     = obj.Floats.Take(3).ToArray(),
            Properties = new List<string>(obj.Properties)
        };

        try
        {
            Clipboard.SetText(JsonSerializer.Serialize(dto, SerializerOptions));
            return true;
        }
        catch (Exception)
        {
            return false; // Clipboard locked by another process.
        }
    }

    /// <summary>
    /// True if the clipboard currently holds a WorldExplorer object payload.
    /// </summary>
    public static bool HasObject()
    {
        try
        {
            if (!Clipboard.ContainsText()) return false;
            var text = Clipboard.GetText();
            // Cheap pre-check before attempting a full parse.
            return text.Contains("\"worldExplorerObject\"");
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to read an object from the clipboard.  Returns a brand-new
    /// <see cref="ObjectData"/> instance (never a reference to an existing
    /// one), or null if the clipboard does not hold a valid payload.
    /// </summary>
    public static ObjectData? TryPaste()
    {
        try
        {
            if (!Clipboard.ContainsText()) return null;

            var dto = JsonSerializer.Deserialize<ObjectDto>(
                Clipboard.GetText(), SerializerOptions);

            if (dto == null || dto.WorldExplorerObject != FormatVersion)
                return null;

            var floats = new float[3];
            Array.Copy(dto.Floats, floats, Math.Min(dto.Floats.Length, 3));

            // NOTE: if ObjectData's members are initialised via a constructor 
            // rather than settable properties/fields in your tree, adapt this
            // block to match (e.g. new ObjectData(dto.Name, dto.I6, floats, …)).
            return new ObjectData(dto.Name, dto.I6, floats, new List<string>(dto.Properties));
        }
        catch (Exception)
        {
            return null; // Not our payload / malformed JSON / clipboard locked.
        }
    }

    /// <summary>
    /// Convenience: deep-clones an existing object without touching the 
    /// clipboard (used by Duplicate, which shouldn't overwrite whatever the
    /// user has copied).
    /// </summary>
    public static ObjectData Clone(ObjectData source)
    {
        return new ObjectData(source.Name, source.I6, source.Floats.Take(3).ToArray(), new List<string>(source.Properties));
    }
}
