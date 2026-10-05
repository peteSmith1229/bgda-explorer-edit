using JetBlackEngineLib.Data.DataContainers;
using WorldExplorer.Infrastructure;

namespace WorldExplorer.TreeView;

public class SdbTreeViewModel : TreeViewItemViewModel
{
    public SdbFile SdbFile { get; }

    public SdbTreeViewModel(TreeViewItemViewModel? parent, SdbFile sdbFile)
        : base(sdbFile.Name, parent, false)
    {
        SdbFile = sdbFile;
        SdbFile.ReadDirectory();
    }

    public override NodeKind Kind => NodeKind.Text;

    public override string KindDescription => "String database (SDB)";

    public override string? Detail => Plural.Of(SdbFile.Records.Count, "string");

    protected override void LoadChildren()
    {
    }
}
