using JetBlackEngineLib.Data.Models;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace JetBlackEngineLib.Data.World;

public class WorldElement
{
    public Rect3D BoundingBox { get; set; }
    public Model? Model { get; set; }
    public WriteableBitmap? Texture { get; set; }
    /// <summary>
    /// The position before rotation.
    /// </summary>
    public Vector3D Position { get; set; }
    /// <summary>
    /// Indicates if the y axis should be flipped. Does not apply when <see cref="UsesRotFlags" /> is true.
    /// </summary>
    public bool NegYaxis { get; set; }
    public double SinAlpha { get; set; }
    public double CosAlpha { get; set; }
    public bool UsesRotFlags { get; set; }
    public int XyzRotFlags { get; set; }
    public int ElementIndex { get; set; }

    /// <summary>
    /// The element's ORIGINAL on-disk record index, used as the byte template
    /// when the array is re-serialised (so VifDataOffset / Tex2 / bounds and any
    /// other unmodelled fields are preserved). Set once at decode time; a
    /// duplicate inherits its source's value so it reuses the source geometry.
    /// Unlike <see cref="ElementIndex"/> (the current slot) this never changes.
    /// </summary>
    public int SourceIndex { get; set; }
    
    /// <summary>
    /// This element's index in the ORIGINAL on-disk array — fixed at decode and
    /// never renumbered. Unique per original element; -1 for elements created in
    /// the editor (duplicates). Used to remap the 0x18 render lists old→new when the
    /// array is rebuilt after add/delete, and to tell originals from clones (a clone
    /// has -1). Distinct from <see cref="SourceIndex"/>, the template-copy index a
    /// clone shares with its source.
    /// </summary>
    public int OriginalIndex { get; set; } = -1;
        
    /// <summary>
    /// Contains info on data this element references.
    /// </summary>
    public WorldElementDataInfo? DataInfo { get; set; }

    public int RawFlags { get; set; }
    
    /// <summary>
    /// Editor-only: this element has been deleted. It is kept in the array as a dead
    /// slot (so deleting never renumbers/reorders the array, which would corrupt the
    /// 0x20-indexed cell-list references) and is dropped from every 0x18 cell list, so
    /// the game never draws it. Hidden in the scene and tree. Not persisted; re-derived
    /// on load (an element referenced by no cell list is marked deleted).
    /// </summary>
    public bool IsDeleted { get; set; }
    
    
    /// <summary>
    /// Returns a copy that shares this element's geometry and texture
    /// (<see cref="Model"/>, <see cref="Texture"/>, <see cref="DataInfo"/>) and
    /// the same record template (<see cref="SourceIndex"/>). The caller typically
    /// offsets <see cref="Position"/> and assigns a new <see cref="ElementIndex"/>.
    /// </summary>
    public WorldElement Clone()
    {
        return new WorldElement
        {
            BoundingBox  = BoundingBox,
            Model        = Model,
            Texture      = Texture,
            Position     = Position,
            NegYaxis     = NegYaxis,
            SinAlpha     = SinAlpha,
            CosAlpha     = CosAlpha,
            UsesRotFlags = UsesRotFlags,
            XyzRotFlags  = XyzRotFlags,
            ElementIndex = ElementIndex,
            SourceIndex  = SourceIndex,
            OriginalIndex = -1,          // ← clone is NOT an original
            DataInfo     = DataInfo,
            RawFlags     = RawFlags,
        };
    }
}