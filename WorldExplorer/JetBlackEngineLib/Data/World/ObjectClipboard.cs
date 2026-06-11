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
/// Copies <see cref="ObjectData"/> to and from the Windows clipboard as JSON.
/// CORRECTED VERSION: constructs ObjectData through its real constructor
/// (string, short, float[], List&lt;string&gt;) — the Floats and Properties
/// fields are readonly, so object-initializer syntax does not compile.
/// </summary>
public static class ObjectClipboard
{
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

    public static bool HasObject()
    {
        try
        {
            return Clipboard.ContainsText()
                && Clipboard.GetText().Contains("\"worldExplorerObject\"");
        }
        catch (Exception)
        {
            return false;
        }
    }

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

            // Use the real constructor — Floats/Properties are readonly fields.
            return new ObjectData(dto.Name, dto.I6, floats,
                new List<string>(dto.Properties));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static ObjectData Clone(ObjectData source)
    {
        return new ObjectData(
            source.Name,
            source.I6,
            source.Floats.Take(3).ToArray(),
            new List<string>(source.Properties));
    }
}
