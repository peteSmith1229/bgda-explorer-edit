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
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

/// <summary>
/// A YAK file: one node per entry. For Dark Alliance (ARMOR.YAK) entries are named after the armour they hold.
/// </summary>
public class YakTreeViewModel : TreeViewItemViewModel
{
    private readonly YakFile _yakFile;

    public YakTreeViewModel(TreeViewItemViewModel? parent, YakFile yakFile) : base(yakFile.Name, parent, true)
    {
        _yakFile = yakFile;
    }

    public YakFile YakFile => _yakFile;

    public override NodeKind Kind => NodeKind.Archive;

    public override string KindDescription => "Archive (YAK)";

    public override string? Detail => HasDummyChild ? null : Plural.Of(_yakFile.EntryCount, "entry", "entries");

    public override bool IsModified => !HasDummyChild && _yakFile.HasUnsavedChanges;

    protected override bool SearchLoadsChildren => true;

    protected override void LoadChildren()
    {
        for (var i = 0; i < _yakFile.EntryCount; i++)
        {
            Children.Add(new YakTreeViewItem(this, _yakFile, i));
        }
    }

    /// <summary>Adds nodes for entries added to the file, and drops nodes of entries taken out.</summary>
    public void SyncEntries()
    {
        if (HasDummyChild) return;
        while (Children.Count > _yakFile.EntryCount) Children.RemoveAt(Children.Count - 1);
        for (var i = Children.Count; i < _yakFile.EntryCount; i++)
        {
            Children.Add(new YakTreeViewItem(this, _yakFile, i));
        }

        RaiseDetailChanged();
    }

    /// <summary>"Entry 19 · Plate body" for ARMOR.YAK, "Entry 19" otherwise.</summary>
    public static string EntryLabel(YakFile yak, int entry) =>
        yak.EngineVersion == EngineVersion.DarkAlliance ? $"Entry {entry} · {ArmorYakInfo.EntryName(entry)}" : $"Entry {entry}";
}
