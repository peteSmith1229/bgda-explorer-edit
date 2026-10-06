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
using JetBlackEngineLib.Data.World;
using System.Linq;
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

public class WorldFileTreeViewModel : AbstractLmpTreeViewModel
{
    public WorldFileTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
        : base(world, parent, lmpFile, entryName)
    {
    }

    /// <summary>
    /// The decoded level, kept for the whole session. Re-selecting the node
    /// reuses it instead of re-decoding the original bytes — which would throw
    /// away in-memory element edits and the undo history.
    /// </summary>
    public WorldData? WorldData { get; set; }

    /// <summary>Level-editor state (undo history etc.) that must survive switching away.</summary>
    public object? EditorState { get; set; }

    public override NodeKind Kind => NodeKind.World;

    public override string? Detail
    {
        get
        {
            if (WorldData != null)
            {
                return Plural.Of(WorldData.WorldElements.Count(e => !e.IsDeleted), "element");
            }
            return EntrySize is { } size ? FileSizeConverter.Format(size) : null;
        }
    }

    // The level's objects live in the same archive's objects.ob.
    public override bool IsModified =>
        _world.IsEntryUnsaved(_lmpFile, Label) || _world.IsEntryUnsaved(_lmpFile, "objects.ob");

    public void ReloadChildren()
    {
        Children.Clear();
        LoadChildren();
        RaiseDetailChanged();
    }

    protected override void LoadChildren()
    {
        if (WorldData == null)
        {
            // Elements only exist once the level is decoded, which happens on
            // selection; selecting re-enters ReloadChildren afterwards.
            IsSelected = true;
            return;
        }

        foreach (var element in WorldData.WorldElements)
        {
            if (element.IsDeleted) continue;            // ← hide deleted elements
            Children.Add(new WorldElementTreeViewModel(element,
                "Element " + element.ElementIndex + " 0x" + element.RawFlags.ToString("X4"),
                this, WorldData));
        }
    }
}
