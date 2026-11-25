namespace JetBlackEngineLib.Data.Textures;

public class YakEntryChild
{
    /// <summary>The texture's offset from the start of the child (the VIF model's length).</summary>
    public int TextureOffset;

    /// <summary>The child's total length (model and texture).</summary>
    public int VifLength;

    /// <summary>The child's offset in the file.</summary>
    public int VifOffset;

    /// <summary>Which child: the character class (0 human, 1 elf, 2 dwarf, 3 class 3).</summary>
    public int Index;
}
