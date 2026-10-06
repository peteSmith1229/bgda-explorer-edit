using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace WorldExplorer.TreeView;

/// <summary>
/// Base class for all ViewModel classes displayed by TreeViewItems.
/// This acts as an adapter between a raw data object and a TreeViewItem.
/// </summary>
public class TreeViewItemViewModel : INotifyPropertyChanged
{
    #region Data

    private static readonly TreeViewItemViewModel DummyChild = new();

    /// <summary>
    /// Receives errors thrown while lazily loading children (set by the main
    /// window so they surface as notifications rather than raw dialogs).
    /// </summary>
    public static Action<string, Exception>? LoadErrorHandler { get; set; }

    private string _label;
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isVisible = true;

    #endregion // Data

    #region Constructors

    protected TreeViewItemViewModel(string label, TreeViewItemViewModel? parent, bool lazyLoadChildren)
    {
        _label = label;
        Parent = parent;

        Children = new ObservableCollection<TreeViewItemViewModel>();

        if (lazyLoadChildren)
        {
            Children.Add(DummyChild);
        }
    }

    // This is used to create the DummyChild instance.
    private TreeViewItemViewModel()
    {
        _label = "Dummy Child";
        Children = new ObservableCollection<TreeViewItemViewModel>();
    }

    #endregion // Constructors

    #region Presentation Members

    /// <summary>
    /// Returns the logical child items of this object.
    /// </summary>
    public ObservableCollection<TreeViewItemViewModel> Children { get; }

    /// <summary>
    /// Returns true if this object's Children have not yet been populated.
    /// </summary>
    public bool HasDummyChild => Children.Count == 1 && Children[0] == DummyChild;

    /// <summary>
    /// Gets/sets whether the TreeViewItem
    /// associated with this object is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (value != _isExpanded)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }

            // Expand all the way up to the root.
            if (_isExpanded && Parent != null)
            {
                Parent.IsExpanded = true;
            }

            // Lazy load the child items, if necessary.
            if (HasDummyChild)
            {
                Children.Remove(DummyChild);
                try
                {
                    LoadChildren();
                }
                catch (Exception ex)
                {
                    if (LoadErrorHandler != null)
                    {
                        LoadErrorHandler($"Couldn't read the contents of {Label}", ex);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            $"There was an error while trying to load the item's details.\n\n{ex}",
                            "Error Loading Item", System.Windows.MessageBoxButton.OK);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets/sets the text shown next to this item in the tree view.
    /// </summary>
    public string Label
    {
        get => _label;
        set
        {
            if (value != _label)
            {
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }
    }

    /// <summary>
    /// Gets/sets whether the TreeViewItem
    /// associated with this object is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value != _isSelected)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    /// <summary>False while the explorer filter hides this node.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set
        {
            if (value != _isVisible)
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public TreeViewItemViewModel? Parent { get; }

    #endregion // Presentation Members

    #region Appearance

    /// <summary>What this node represents; drives the icon and description.</summary>
    public virtual NodeKind Kind => NodeKind.Folder;

    public string IconKey => NodeKinds.IconKey(Kind);

    public string IconBrushKey => NodeKinds.BrushKey(Kind);

    /// <summary>Human-readable type, shown in the content header.</summary>
    public virtual string KindDescription => NodeKinds.Describe(Kind);

    /// <summary>Dimmed secondary text shown after the label (a size, a count…).</summary>
    public virtual string? Detail => null;

    /// <summary>Changed since the file was last saved.</summary>
    public virtual bool IsModified => false;

    /// <summary>Scheduled for deletion on the next save.</summary>
    public virtual bool IsDeleted => false;

    /// <summary>Tooltip for the tree row.</summary>
    public virtual string ToolTip
    {
        get
        {
            var text = new StringBuilder(Label).Append('\n').Append(KindDescription);
            if (Detail != null) text.Append(" · ").Append(Detail);
            if (IsDeleted) text.Append("\nScheduled for deletion");
            else if (IsModified) text.Append("\nModified — not yet saved");
            return text.ToString();
        }
    }

    /// <summary>"GOB › archive › entry" path of the parents, for the content header.</summary>
    public string Location
    {
        get
        {
            var path = new StringBuilder();
            for (var node = Parent; node != null; node = node.Parent)
            {
                path.Insert(0, path.Length == 0 ? node.Label : node.Label + "  ›  ");
            }
            return path.ToString();
        }
    }

    /// <summary>"Texture (TEX) · 12.4 KB · TAVERN.GOB › tavern.lmp" for the content header.</summary>
    public string Subtitle
    {
        get
        {
            var text = new StringBuilder(KindDescription);
            if (!string.IsNullOrEmpty(Detail)) text.Append("  ·  ").Append(Detail);
            var location = Location;
            if (location.Length > 0) text.Append("  ·  ").Append(location);
            if (IsDeleted) text.Append("  ·  deleted on save");
            else if (IsModified) text.Append("  ·  modified");
            return text.ToString();
        }
    }

    /// <summary>Call when <see cref="Detail"/> changes.</summary>
    protected void RaiseDetailChanged()
    {
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(ToolTip));
    }

    /// <summary>
    /// Re-raises the change-state properties for this node and its loaded
    /// descendants, after an edit or a save.
    /// </summary>
    public void RefreshState()
    {
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsDeleted));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(Subtitle));
        foreach (var child in Children)
        {
            if (child != DummyChild) child.RefreshState();
        }
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Whether the explorer filter may load this node's children to search
    /// them. Only cheap containers (archive directories) opt in; nodes whose
    /// children require heavy decoding (levels) do not.
    /// </summary>
    protected virtual bool SearchLoadsChildren => false;

    /// <summary>
    /// Shows nodes whose label contains <paramref name="filter"/> plus their
    /// ancestors, expanding the ancestors to reveal matches. A null/empty
    /// filter shows everything again. Returns whether this subtree is visible.
    /// </summary>
    public bool ApplyFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                if (child != DummyChild) child.ApplyFilter(null);
            }
            return true;
        }

        if (Label.Contains(filter, StringComparison.OrdinalIgnoreCase))
        {
            // A matching container keeps all of its contents visible.
            ApplyFilter(null);
            return true;
        }

        if (HasDummyChild && SearchLoadsChildren)
        {
            ForceLoadChildren();
        }

        var anyChildMatches = false;
        foreach (var child in Children)
        {
            if (child != DummyChild && child.ApplyFilter(filter))
            {
                anyChildMatches = true;
            }
        }

        IsVisible = anyChildMatches;
        if (anyChildMatches && !IsExpanded)
        {
            IsExpanded = true;
        }

        return anyChildMatches;
    }

    #endregion

    #region LoadChildren

    /// <summary>
    /// Invoked when the child items need to be loaded on demand.
    /// Subclasses can override this to populate the Children collection.
    /// </summary>
    protected virtual void LoadChildren()
    {
    }

    /// <summary>
    /// Forces the item to load its children without expanding.
    /// </summary>
    public void ForceLoadChildren()
    {
        if (HasDummyChild)
        {
            Children.Remove(DummyChild);
            LoadChildren();
        }
    }

    #endregion

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion // INotifyPropertyChanged Members
}
