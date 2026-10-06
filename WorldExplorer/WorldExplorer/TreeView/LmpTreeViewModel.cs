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
using System.IO;
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

/// <summary>
/// A simple model that displays a LMP file.
/// </summary>
public class LmpTreeViewModel : AbstractLmpTreeViewModel
{
    public LmpTreeViewModel(World world, TreeViewItemViewModel? parent, LmpFile lmpFile)
        : base(world, parent, lmpFile, lmpFile.Name)
    {
    }

    public override NodeKind Kind => NodeKind.Archive;

    public override string KindDescription => _lmpFile is ClpFile ? "Archive (CLP)" : "Archive (LMP)";

    public override string? Detail => _lmpFile.Directory.Count > 0 ? Plural.Of(_lmpFile.Directory.Count, "item") : null;

    public override bool IsModified => _lmpFile is not ClpFile && _world.HasUnsavedChanges(_lmpFile);

    // Reading an archive directory is cheap, so the explorer filter may do it.
    protected override bool SearchLoadsChildren => true;

    protected override void LoadChildren()
    {
        _lmpFile.ReadDirectory();
        foreach (var entry in _lmpFile.Directory)
        {
            var ext = "";
            try
            {
                ext = (Path.GetExtension(entry.Key) ?? "").ToLower();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }

            TreeViewItemViewModel child;
            switch (ext)
            {
                case ".world":
                    child = new WorldFileTreeViewModel(_world, this, _lmpFile, entry.Key);
                    break;
                default:
                    child = new LmpEntryTreeViewModel(_world, this, _lmpFile, entry.Key);
                    break;
            }

            Children.Add(child);
        }

        RaiseDetailChanged();
    }
}
