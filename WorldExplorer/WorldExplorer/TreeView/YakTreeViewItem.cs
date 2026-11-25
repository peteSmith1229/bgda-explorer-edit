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
using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Textures;
using System.Linq;

namespace WorldExplorer.TreeView;

/// <summary>An entry of a YAK file: its four children (one per character class).</summary>
public class YakTreeViewItem : TreeViewItemViewModel
{
    private readonly YakFile _yakFile;

    public YakTreeViewItem(TreeViewItemViewModel parent, YakFile yakFile, int entryIndex)
        : base(YakTreeViewModel.EntryLabel(yakFile, entryIndex), parent, true)
    {
        _yakFile = yakFile;
        EntryIndex = entryIndex;
    }

    public YakFile YakFile => _yakFile;

    public int EntryIndex { get; }

    public override NodeKind Kind => NodeKind.Folder;

    public override string KindDescription => "Armour entry";

    public override bool IsModified => Enumerable.Range(0, 4).Any(c => _yakFile.IsChildUnsaved(EntryIndex, c));

    protected override void LoadChildren()
    {
        for (var c = 0; c < 4; c++)
        {
            var name = _yakFile.EngineVersion == EngineVersion.DarkAlliance ? ArmorYakInfo.Characters[c] : "Child " + c;
            Children.Add(new YakChildTreeViewItem(this, _yakFile, EntryIndex, c, name));
        }
    }
}
