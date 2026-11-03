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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public override string? Detail
    {
        get
        {
            var count = _lmpFile.Directory.Count + _lmpFile.PendingAdditions.Count;
            return count > 0 ? Plural.Of(count, "item") : null;
        }
    }

    public override bool IsModified => _lmpFile is not ClpFile &&
                                       (_world.HasUnsavedChanges(_lmpFile) || _world.IsArchiveUnsaved(_lmpFile));

    // Reading an archive directory is cheap, so the explorer filter may do it.
    protected override bool SearchLoadsChildren => true;

    protected override void LoadChildren()
    {
        _lmpFile.ReadDirectory();
        foreach (var name in EntryNames())
        {
            if (CreateChild(name) is { } child) Children.Add(child);
        }

        RaiseDetailChanged();
    }

    /// <summary>
    /// The entries to list: the directory (deleted ones too, marked) and the
    /// entries added since opening, in the order they will be saved.
    /// </summary>
    private List<string> EntryNames()
    {
        var names = _lmpFile.Directory.Keys.ToList();
        if (_lmpFile is ClpFile) return names;
        names.AddRange(_lmpFile.PendingAdditions.Select(a => a.Name));
        return _lmpFile.IsDirectorySorted() ? LmpFile.InGameOrder(names) : names;
    }

    private TreeViewItemViewModel? CreateChild(string name)
    {
        string ext;
        try
        {
            ext = (Path.GetExtension(name) ?? "").ToLower();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }

        return ext == ".world"
            ? new WorldFileTreeViewModel(_world, this, _lmpFile, name)
            : new LmpEntryTreeViewModel(_world, this, _lmpFile, name);
    }

    /// <summary>Lists an entry just added to the archive, where it will be saved, and returns its node.</summary>
    public TreeViewItemViewModel? ShowAddedEntry(string name)
    {
        if (HasDummyChild)
        {
            ForceLoadChildren(); // lists every entry, the new one included
        }
        else if (Children.All(c => c.Label != name) && CreateChild(name) is { } child)
        {
            var index = EntryNames().IndexOf(name);
            Children.Insert(Math.Clamp(index, 0, Children.Count), child);
        }

        RaiseDetailChanged();
        return Children.FirstOrDefault(c => c.Label == name);
    }

    /// <summary>Stops listing an added entry that was taken back.</summary>
    public void RemoveEntryNode(string name)
    {
        var node = Children.FirstOrDefault(c => c.Label == name);
        if (node != null) Children.Remove(node);
        RaiseDetailChanged();
    }
}
