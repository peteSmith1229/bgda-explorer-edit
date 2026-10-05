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
/// A simple model that displays an entry in a LMP file.
/// </summary>
public class LmpEntryTreeViewModel : AbstractLmpTreeViewModel
{
    private readonly NodeKind _kind;

    public LmpEntryTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
        : base(world, parent, lmpFile, entryName, lazyLoadChildren: false)
    {
        _kind = NodeKinds.FromFileName(entryName);
    }

    public override NodeKind Kind => _kind;

    public override string KindDescription
    {
        get
        {
            string ext;
            try
            {
                ext = (Path.GetExtension(Label) ?? "").TrimStart('.').ToUpperInvariant();
            }
            catch (ArgumentException)
            {
                ext = "";
            }

            var description = NodeKinds.Describe(Kind);
            return ext.Length > 0 ? $"{description} ({ext})" : description;
        }
    }

    public override string? Detail => EntrySize is { } size ? FileSizeConverter.Format(size) : null;

    public override bool IsModified => _world.IsEntryUnsaved(_lmpFile, Label);

    public override bool IsDeleted => _lmpFile.PendingDeletions.Contains(Label);
}
