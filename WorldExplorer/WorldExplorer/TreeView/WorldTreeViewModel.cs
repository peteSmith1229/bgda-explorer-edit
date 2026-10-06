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

using System;
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

/// <summary>
/// Root node for an opened .DDF: hosts the entity category folders directly.
/// Other file types use their container node as the root (see
/// <see cref="CreateRoot"/>), so the tree doesn't repeat the file name twice.
/// </summary>
public class WorldTreeViewModel : TreeViewItemViewModel
{
    private readonly World _world;

    public WorldTreeViewModel(World world)
        : base(world.Name, null, true)
    {
        _world = world;
    }

    public World World => _world;

    public override NodeKind Kind => NodeKind.Database;

    public override string? Detail => _world.WorldDdf is { } ddf ? Plural.Of(ddf.Entities.Count, "entity", "entities") : null;

    /// <summary>
    /// Builds the tree root for a loaded <paramref name="world"/>: the GOB,
    /// archive, YAK, HDR/DAT or SDB node itself, or a DDF entity root.
    /// </summary>
    public static TreeViewItemViewModel CreateRoot(World world)
    {
        world.Load();

        if (world.WorldDdf != null && world.WorldLmp == null)
            return new WorldTreeViewModel(world);
        if (world.WorldLmp != null)
            return new LmpTreeViewModel(world, null, world.WorldLmp);
        if (world.WorldGob != null)
            return new GobTreeViewModel(world, null);
        if (world.WorldYak != null)
            return new YakTreeViewModel(null, world.WorldYak);
        if (world.HdrDatFile != null)
            return new HdrDatTreeViewModel(null, world.HdrDatFile);
        if (world.WorldSdb != null)
            return new SdbTreeViewModel(null, world.WorldSdb);

        throw new NotSupportedException("Unknown or corrupted file");
    }

    protected override void LoadChildren()
    {
        _world.Load();

        if (_world.WorldDdf != null)
        {
            // .DDF was opened directly — show the entity-centric view inline,
            // with category folders directly under the root. Avoids the
            // ALL.DDF → ALL.DDF → categories double-nesting.
            DdfTreeBuilder.AddCategoryFolders(this, _world, _world.WorldDdf);
        }
    }
}
