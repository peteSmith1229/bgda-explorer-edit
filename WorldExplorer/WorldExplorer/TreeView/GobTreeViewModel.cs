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
using System;
using System.Linq;
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

/// <summary>
/// A simple model that displays a GOB file.
/// </summary>
public class GobTreeViewModel : TreeViewItemViewModel
{
    private readonly World _world;

    public GobTreeViewModel(World world, TreeViewItemViewModel? parent)
        : base(world.WorldGob?.Name ?? "[ERROR: NO GOB FILE]", parent, true)
    {
        _world = world;
    }

    public World World => _world;

    public override NodeKind Kind => NodeKind.Gob;

    public override string? Detail => _world.WorldGob is { } gob ? Plural.Of(gob.Directory.Count, "archive") : null;

    public override bool IsModified => _world.HasUnsavedChanges();

    protected override bool SearchLoadsChildren => true;

    protected override void LoadChildren()
    {
        if (_world.WorldGob == null) return;
        foreach (var entry in _world.WorldGob.Directory)
        {
            Children.Add(new LmpTreeViewModel(_world, this, entry.Value));
        }
    }

    /// <summary>Lists an archive just added to the GOB, where it will be saved, and returns its node.</summary>
    public LmpTreeViewModel? ShowAddedArchive(LmpFile lmp)
    {
        if (HasDummyChild)
        {
            ForceLoadChildren(); // lists every archive, the new one included
        }
        else if (_world.WorldGob != null)
        {
            var index = _world.WorldGob.Directory.Keys.ToList().IndexOf(lmp.Name);
            Children.Insert(Math.Clamp(index, 0, Children.Count), new LmpTreeViewModel(_world, this, lmp));
        }

        RaiseDetailChanged();
        return Children.OfType<LmpTreeViewModel>().FirstOrDefault(n => ReferenceEquals(n.LmpFileProperty, lmp));
    }

    /// <summary>Stops listing an archive taken back out of the GOB.</summary>
    public void RemoveArchiveNode(LmpFile lmp)
    {
        var node = Children.OfType<LmpTreeViewModel>().FirstOrDefault(n => ReferenceEquals(n.LmpFileProperty, lmp));
        if (node != null) Children.Remove(node);
        RaiseDetailChanged();
    }
}
