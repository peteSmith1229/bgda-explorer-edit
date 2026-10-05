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

namespace WorldExplorer.TreeView;

public abstract class AbstractLmpTreeViewModel : TreeViewItemViewModel
{
    protected LmpFile _lmpFile;
    protected World _world;

    public LmpFile LmpFileProperty => _lmpFile;

    public World World => _world;

    protected AbstractLmpTreeViewModel(World world, TreeViewItemViewModel? parent, LmpFile lmpFile, string entryName,
        bool lazyLoadChildren = true)
        : base(entryName, parent, lazyLoadChildren)
    {
        _lmpFile = lmpFile;
        _world = world;
    }

    /// <summary>Size of this entry's current bytes (the pending edit if there is one).</summary>
    protected long? EntrySize
    {
        get
        {
            if (_lmpFile.PendingEdits.TryGetValue(Label, out var pending)) return pending.Length;
            return _lmpFile.Directory.TryGetValue(Label, out var entry) ? entry.Length : null;
        }
    }
}
