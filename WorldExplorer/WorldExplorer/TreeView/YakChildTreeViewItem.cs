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

using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Textures;
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

/// <summary>A child of a YAK entry: one character's model and texture.</summary>
public class YakChildTreeViewItem : TreeViewItemViewModel
{
    public YakChildTreeViewItem(TreeViewItemViewModel parent, YakFile yakFile, int entryIndex, int childIndex,
        string name)
        : base(name, parent, false)
    {
        YakFile = yakFile;
        EntryIndex = entryIndex;
        ChildIndex = childIndex;
    }

    public YakFile YakFile { get; }

    public int EntryIndex { get; }

    /// <summary>The character class: 0 human, 1 elf, 2 dwarf, 3 class 3.</summary>
    public int ChildIndex { get; }

    /// <summary>The model and texture now, with any edit.</summary>
    public YakFile.Child Value => YakFile.GetChild(EntryIndex, ChildIndex);

    public override NodeKind Kind => NodeKind.Model;

    public override string KindDescription => "Armour model and texture";

    public override string? Detail
    {
        get
        {
            var read = ArmorYakInfo.ReadSize(Value.Length);
            var capacity = ArmorYakInfo.Capacity(ArmorYakInfo.SlotOf(EntryIndex));
            return $"{FileSizeConverter.Format(Value.Length)} of {FileSizeConverter.Format(capacity)}" +
                   (read > capacity ? " — too big" : "");
        }
    }

    public override bool IsModified => YakFile.IsChildUnsaved(EntryIndex, ChildIndex);
}
