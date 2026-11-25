// ═══════════════════════════════════════════════════════════════════════════════
// The wearer of an ARMOR.YAK child: the character's own model and hair, from its
// archive in the game's DATA folder (HUMAN.LMP man.vif and hair.vif, ELF.LMP elf.vif and
// elf_hairpony.vif, DWARF.LMP dwarf.vif and hair.vif, each with its .tex). They are
// skinned in the same bind pose as the armour, so they line up as they are.
//
// As the game draws a character: the body without the meshes the worn pieces hide
// (ArmorModel.HiddenBodyParts, bit n = mesh n), no hair under a helmet, and with the hair
// the meshes the hair hides too (the elf's ponytail hides her scalp, mask 0x2). Class 3's
// pieces in the game's file are the dwarf's, so it is drawn on the dwarf.
// ═══════════════════════════════════════════════════════════════════════════════
using JetBlackEngineLib.Data.DataContainers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Armour;

public sealed class ArmorWearer
{
    public int Child { get; }

    /// <summary>The archive the models are in: "HUMAN.LMP", "ELF.LMP" or "DWARF.LMP".</summary>
    public string Archive { get; }

    public byte[] BodyVif { get; }
    public byte[] BodyTex { get; }
    public byte[] HairVif { get; }
    public byte[] HairTex { get; }

    private ArmorWearer(int child, string archive, byte[] bodyVif, byte[] bodyTex, byte[] hairVif, byte[] hairTex)
    {
        Child = child;
        Archive = archive;
        BodyVif = bodyVif;
        BodyTex = bodyTex;
        HairVif = hairVif;
        HairTex = hairTex;
    }

    /// <summary>Character <paramref name="child"/>'s archive and the names of its body and hair (without .vif/.tex).</summary>
    public static (string Archive, string Body, string Hair) Files(int child) => child switch
    {
        0 => ("HUMAN.LMP", "man", "hair"),
        1 => ("ELF.LMP", "elf", "elf_hairpony"),
        _ => ("DWARF.LMP", "dwarf", "hair")
    };

    /// <summary>
    /// The wearer of character <paramref name="child"/> from the bytes of its archive (<see cref="Files"/>), or null
    /// with <paramref name="problem"/> when the archive lacks one of the four entries or isn't an archive.
    /// </summary>
    public static ArmorWearer? FromArchive(int child, byte[] archive, out string? problem)
    {
        var (name, body, hair) = Files(child);
        try
        {
            var lmp = new LmpFile(EngineVersion.DarkAlliance, name, archive, 0, archive.Length);
            var parts = new[] { body + ".vif", body + ".tex", hair + ".vif", hair + ".tex" }
                .Select(entry => (entry, bytes: lmp.GetEntryBytes(entry))).ToList();
            foreach (var (entry, bytes) in parts)
            {
                if (bytes != null) continue;
                problem = $"{name} has no {entry}";
                return null;
            }

            var wearer = new ArmorWearer(child, name, parts[0].bytes!, parts[1].bytes!, parts[2].bytes!, parts[3].bytes!);
            if (wearer.BodyVif.Length < 0x30 || wearer.HairVif.Length < 0x30)
            {
                problem = $"{name}'s models are too short to be models";
                return null;
            }

            problem = null;
            return wearer;
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or InvalidOperationException
                                       or System.IO.InvalidDataException)
        {
            problem = $"{name} couldn't be read: {ex.Message}";
            return null;
        }
    }

    /// <summary>How many meshes the body has (one per hidden-body-parts bit): human 11, elf 10, dwarf 12.</summary>
    public int BodyMeshCount => BodyVif[0x12];

    /// <summary>The body meshes the hair hides when it is drawn (the elf's scalp).</summary>
    public ulong HairHides => BitConverter.ToUInt64(HairVif, 0);

    /// <summary>Whether the hair is drawn with a piece of this entry: a helmet hides it.</summary>
    public static bool HairShownWith(int entry) => ArmorYakInfo.SlotOf(entry) != ArmorSlot.Helmet;

    /// <summary>The body meshes left out under pieces hiding <paramref name="pieceMask"/>, with or without the hair.</summary>
    public ulong HiddenMeshes(ulong pieceMask, bool hair) => pieceMask | (hair ? HairHides : 0);

    /// <summary>The body meshes drawn under pieces hiding <paramref name="pieceMask"/>, in order.</summary>
    public IReadOnlyList<int> VisibleMeshes(ulong pieceMask, bool hair)
    {
        var hidden = HiddenMeshes(pieceMask, hair);
        return Enumerable.Range(0, BodyMeshCount).Where(i => (hidden >> i & 1) == 0).ToList();
    }

    private ArmorModel? _body;
    private ArmorTexture? _bodyTexture;

    /// <summary>
    /// The parts of a piece worn by this character that are its own skin, face and hair (see <see cref="ArmorOwnParts"/>):
    /// none for most pieces; most of the elf's clothes body and her clothes gloves' fingers.
    /// </summary>
    public ArmorOwnParts OwnParts(byte[] pieceVif, byte[] pieceTex)
    {
        _body ??= new ArmorModel(BodyVif);
        _bodyTexture ??= new ArmorTexture(BodyTex);
        return ArmorOwnParts.Find(new ArmorModel(pieceVif), new ArmorTexture(pieceTex), _body, _bodyTexture);
    }

    /// <summary>"the human's own model (HUMAN.LMP man.vif) and hair": for the Details view.</summary>
    public string Describe(bool hair)
    {
        var (_, body, hairName) = Files(Child);
        var who = Child switch { 0 => "the human's", 1 => "the elf's", 2 => "the dwarf's", _ => "the dwarf's (class 3's pieces are the dwarf's)" };
        return $"{who} own model ({Archive} {body}.vif)" + (hair ? $" and hair ({hairName}.vif)" : ", no hair under a helmet");
    }
}
