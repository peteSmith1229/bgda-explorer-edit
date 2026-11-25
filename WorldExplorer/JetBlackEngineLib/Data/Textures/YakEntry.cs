namespace JetBlackEngineLib.Data.Textures;

public class YakEntry
{
    public readonly YakEntryChild?[] Children;

    /// <summary>The entry's number in the file (what an item's model field holds).</summary>
    public int Index { get; init; }

    public YakEntry(YakEntryChild?[] children)
    {
        Children = children;
    }
}
