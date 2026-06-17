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

using JetBlackEngineLib.Data.World;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace WorldExplorer.WorldDefs;

/// <summary>
/// Immutable snapshot of one world element's editable transform — exactly the
/// fields <c>ApplyChangesClicked</c> can modify and that
/// <see cref="WorldElementPatcher"/> writes back to disk.
/// </summary>
public readonly struct ElementTransform
{
    public Vector3D Position { get; init; }
    public bool NegYaxis { get; init; }
    public double SinAlpha { get; init; }
    public double CosAlpha { get; init; }
    public bool UsesRotFlags { get; init; }
    public int XyzRotFlags { get; init; }
}

/// <summary>
/// A complete snapshot of a level's editable state: the object list plus every
/// world element's transform.  Used by <see cref="LevelEditHistory"/>.
///
/// <para>
/// The <see cref="ObjectData"/> instances held here are <b>private clones</b>.
/// <see cref="LevelViewModel"/> clones when capturing and clones again when
/// restoring, so a memento is never aliased to the live object list — an
/// in-place field edit can therefore never corrupt a stored snapshot.
/// </para>
/// </summary>
public sealed class LevelEditMemento
{
    public IReadOnlyList<ObjectData> Objects { get; }
    public IReadOnlyDictionary<int, ElementTransform> ElementTransforms { get; }

    public LevelEditMemento(IReadOnlyList<ObjectData> objects,
                            IReadOnlyDictionary<int, ElementTransform> elementTransforms)
    {
        Objects = objects;
        ElementTransforms = elementTransforms;
    }
}

/// <summary>
/// Two-stack undo/redo history of <see cref="LevelEditMemento"/> snapshots,
/// driven entirely by <see cref="LevelViewModel"/>:
///
/// <list type="bullet">
///   <item>Before any edit: <c>PushUndo(currentSnapshot)</c> — this also
///         invalidates (clears) the redo stack.</item>
///   <item>Undo: <c>restored = Undo(currentSnapshot)</c>, then apply
///         <c>restored</c> if non-null.</item>
///   <item>Redo: <c>restored = Redo(currentSnapshot)</c>, then apply
///         <c>restored</c> if non-null.</item>
/// </list>
///
/// The history is bounded to <see cref="MaxDepth"/> entries; the oldest is
/// dropped when the limit is exceeded.
/// </summary>
public sealed class LevelEditHistory
{
    private const int MaxDepth = 100;
    private readonly List<LevelEditMemento> _undo = new();
    private readonly List<LevelEditMemento> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Records a pre-edit snapshot and invalidates the redo stack.</summary>
    public void PushUndo(LevelEditMemento snapshot)
    {
        _undo.Add(snapshot);
        if (_undo.Count > MaxDepth)
            _undo.RemoveAt(0); // drop the oldest entry
        _redo.Clear();
    }

    /// <summary>
    /// Pops the most recent undo snapshot, pushing <paramref name="current"/>
    /// onto the redo stack.  Returns null if there is nothing to undo.
    /// </summary>
    public LevelEditMemento? Undo(LevelEditMemento current)
    {
        if (_undo.Count == 0) return null;
        var snapshot = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(current);
        return snapshot;
    }

    /// <summary>
    /// Pops the most recent redo snapshot, pushing <paramref name="current"/>
    /// onto the undo stack.  Returns null if there is nothing to redo.
    /// </summary>
    public LevelEditMemento? Redo(LevelEditMemento current)
    {
        if (_redo.Count == 0) return null;
        var snapshot = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(current);
        return snapshot;
    }

    /// <summary>Drops all history — called when a new level is loaded.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
