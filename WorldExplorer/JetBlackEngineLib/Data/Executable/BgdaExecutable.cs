// ═══════════════════════════════════════════════════════════════════════════════
// COMPLETE FILE — replace WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.cs
//
// Reads/writes tuning tables in the BGDA main executable (SLES_506.72, PAL).
// All offsets are FILE offsets. Values confirmed in-game:
//   • XP-to-level curve        file 0x00134DA0, 16 × int32
//   • Character starting stats file 0x0020A510, 4 classes × 6 ability scores int32
//   • Feat/spell table         file 0x0020E750, 0x50-byte records, "END"-terminated
//
// Feat/spell editable fields are energy cost, effect colour and the displayed
// range. The in-game combat DAMAGE is computed per character at cast time and is
// NOT a static edit (see FEAT_SPELL_FINDINGS.md).
//
// Standalone (no GOB/LMP dependency). A build check validates the tables before
// any offset is trusted.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The four playable classes, in table order.</summary>
public enum BgdaClass
{
    HumanArcaneArcher = 0,
    ElvenSorceress    = 1,
    DwarvenFighter    = 2,
    DrowRanger        = 3
}

/// <summary>One feat or spell record from the executable's ability table.</summary>
public sealed class FeatSpell
{
    /// <summary>Record index (also its position in the table).</summary>
    public int Index { get; init; }

    /// <summary>Internal name, e.g. "fireball", "coneofcold".</summary>
    public string Name { get; init; } = "";

    /// <summary>Damage range minimum (+0x26).</summary>
    public int DamageMin { get; init; }

    /// <summary>Damage range maximum (+0x28).</summary>
    public int DamageMax { get; init; }

    /// <summary>Energy cost (+0x2C). Channeled spells store a per-frame value.</summary>
    public float EnergyCost { get; init; }

    // Hand-glow colour (+0x30/+0x32/+0x34): the aura over the caster's hands.
    public int GlowR { get; init; }
    public int GlowG { get; init; }
    public int GlowB { get; init; }

    // Light-emit colour (+0x38/+0x3A/+0x3C): the light the effect casts on the
    // surrounding environment. Channels are u16 and CAN exceed 255 (e.g. coneofcold
    // ships with values above 255), so treat as 0..65535.
    public int LightR { get; init; }
    public int LightG { get; init; }
    public int LightB { get; init; }

    /// <summary>Per-level scaling coefficient (+0x40). Effect not yet fully confirmed.</summary>
    public float Coefficient { get; init; }

    /// <summary>
    /// True for the spells confirmed in-game to take their damage from the min/max
    /// range (editing those fields changes real combat damage). For other spells the
    /// range is display/secondary — see <see cref="DamageIsEditable"/> usage in the UI.
    /// The character's stats still apply a spread on top (e.g. INT gives fireball ±5).
    /// </summary>
    public bool DamageIsEditable { get; init; }

    /// <summary>Energy per second as the game displays it (PAL: 50 frames/sec).</summary>
    public float EnergyPerSecond => EnergyCost * 50f;
}

public class BgdaExecutable
{
    // ---- supported-build layout (SLES_506.72) ----
    public const int XpTableOffset    = 0x00134DA0;   // 16 × int32
    public const int XpTableCount     = 16;
    public const int StatsTableOffset = 0x0020A510;   // 4 classes × 6 stats × int32
    public const int StatsClassCount  = 4;
    public const int StatsPerClass    = 6;            // STR, INT, WIS, DEX, CON, CHA

    public const int FeatTableOffset  = 0x0020E750;   // VA 0x0030D750
    public const int FeatRecordSize   = 0x50;
    private const int FeatMaxRecords  = 60;           // hard cap while walking to "END"

    // field offsets within a feat/spell record
    private const int FeatName    = 0x00;   // ASCII, NUL-terminated
    private const int FeatDmgMin  = 0x26;   // u16 — damage range min
    private const int FeatDmgMax  = 0x28;   // u16 — damage range max
    private const int FeatEnergy  = 0x2C;   // f32 — energy cost (per-frame; UI ×50)
    private const int FeatGlowR   = 0x30;   // u16 — hand-glow colour
    private const int FeatGlowG   = 0x32;   // u16
    private const int FeatGlowB   = 0x34;   // u16
    private const int FeatLightR  = 0x38;   // u16 — environment light-emit colour
    private const int FeatLightG  = 0x3A;   // u16
    private const int FeatLightB  = 0x3C;   // u16
    private const int FeatCoef    = 0x40;   // f32 — per-level scaling coefficient

    /// <summary>
    /// Internal names of the spells confirmed in-game to draw combat damage from the
    /// min/max range fields. Editing the range for these changes real damage; for
    /// others it is display/secondary (multi-projectile spells, per-second channels,
    /// and charge moves use different damage handling keyed by the engine).
    /// </summary>
    private static readonly HashSet<string> DamageEditableSpells = new()
    {
        "fireball", "acidarrow", "Otilukes", "knockback", "ClangeddinsFist",
        "flamingarrow", "explodingarrow", "icearrow", "shockarrow"
    };

    public static readonly string[] StatNames  = { "STR", "INT", "WIS", "DEX", "CON", "CHA" };
    public static readonly string[] ClassNames =
        { "Human Arcane Archer", "Elven Sorceress", "Dwarven Fighter", "Drow Ranger" };

    private readonly byte[] _data;
    public string FilePath { get; }

    private BgdaExecutable(byte[] data, string path)
    {
        _data = data;
        FilePath = path;
    }

    /// <summary>
    /// Opens an executable and verifies it is the supported build. Throws
    /// <see cref="NotSupportedException"/> otherwise, so the UI never writes to an
    /// unrecognized file at the wrong offsets.
    /// </summary>
    public static BgdaExecutable Open(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 4 || data[0] != 0x7F || data[1] != (byte)'E'
            || data[2] != (byte)'L' || data[3] != (byte)'F')
        {
            throw new NotSupportedException("Not a PS2 executable (missing ELF header).");
        }

        var exe = new BgdaExecutable(data, path);
        if (!exe.LooksLikeSupportedBuild())
        {
            throw new NotSupportedException(
                "This executable isn't the recognized Baldur's Gate: Dark Alliance build " +
                "(PAL SLES-506.72). The tuning tables live at build-specific offsets, so " +
                "editing an unrecognized build is disabled to avoid corrupting it.");
        }

        return exe;
    }

    /// <summary>
    /// Fingerprints the supported build by validating the XP curve (ascending from
    /// zero) and the starting stats (plausible ability scores) at their known offsets.
    /// </summary>
    private bool LooksLikeSupportedBuild()
    {
        if (XpTableOffset + XpTableCount * 4 > _data.Length) return false;
        if (StatsTableOffset + StatsClassCount * StatsPerClass * 4 > _data.Length) return false;
        if (FeatTableOffset + FeatRecordSize > _data.Length) return false;

        if (ReadInt(XpTableOffset) != 0) return false;
        var prev = 0;
        for (var i = 1; i < XpTableCount; i++)
        {
            var v = ReadInt(XpTableOffset + i * 4);
            if (v <= prev) return false;
            prev = v;
        }

        for (var i = 0; i < StatsClassCount * StatsPerClass; i++)
        {
            var v = ReadInt(StatsTableOffset + i * 4);
            if (v < 1 || v > 30) return false;
        }

        return true;
    }

    // ---- XP-to-level curve ----

    public int[] GetXpTable()
    {
        var t = new int[XpTableCount];
        for (var i = 0; i < XpTableCount; i++) t[i] = ReadInt(XpTableOffset + i * 4);
        return t;
    }

    public void SetXpTable(int[] values)
    {
        if (values.Length != XpTableCount)
            throw new ArgumentException($"XP table needs exactly {XpTableCount} values.");
        if (values[0] != 0)
            throw new ArgumentException("Level 1 threshold must be 0.");
        for (var i = 1; i < XpTableCount; i++)
        {
            if (values[i] <= values[i - 1])
                throw new ArgumentException(
                    $"XP thresholds must strictly increase (entry {i + 1} is not greater than entry {i}).");
        }

        for (var i = 0; i < XpTableCount; i++) WriteInt(XpTableOffset + i * 4, values[i]);
    }

    // ---- starting stats ----

    public int GetStat(BgdaClass cls, int statIndex) =>
        ReadInt(StatsTableOffset + ((int)cls * StatsPerClass + statIndex) * 4);

    public void SetStat(BgdaClass cls, int statIndex, int value)
    {
        if (value < 1 || value > 30)
            throw new ArgumentException("Ability scores must be between 1 and 30.");
        WriteInt(StatsTableOffset + ((int)cls * StatsPerClass + statIndex) * 4, value);
    }

    public int[,] GetStatsTable()
    {
        var t = new int[StatsClassCount, StatsPerClass];
        for (var c = 0; c < StatsClassCount; c++)
        {
            for (var s = 0; s < StatsPerClass; s++) t[c, s] = GetStat((BgdaClass)c, s);
        }

        return t;
    }

    // ---- feats & spells ----

    /// <summary>
    /// Reads every feat/spell record, stopping at the record named "END".
    /// Unrelated data follows the terminator, so a fixed count must not be used.
    /// </summary>
    public List<FeatSpell> GetFeatSpells()
    {
        var list = new List<FeatSpell>();
        for (var i = 0; i < FeatMaxRecords; i++)
        {
            var rec = FeatTableOffset + i * FeatRecordSize;
            if (rec + FeatRecordSize > _data.Length) break;

            var name = ReadAscii(rec + FeatName, 24);
            if (name.Length == 0 || !char.IsLetter(name[0])) break;
            if (name == "END") break;

            list.Add(new FeatSpell
            {
                Index            = i,
                Name             = name,
                DamageMin        = ReadU16(rec + FeatDmgMin),
                DamageMax        = ReadU16(rec + FeatDmgMax),
                EnergyCost       = BitConverter.ToSingle(_data, rec + FeatEnergy),
                GlowR            = ReadU16(rec + FeatGlowR),
                GlowG            = ReadU16(rec + FeatGlowG),
                GlowB            = ReadU16(rec + FeatGlowB),
                LightR           = ReadU16(rec + FeatLightR),
                LightG           = ReadU16(rec + FeatLightG),
                LightB           = ReadU16(rec + FeatLightB),
                Coefficient      = BitConverter.ToSingle(_data, rec + FeatCoef),
                DamageIsEditable = DamageEditableSpells.Contains(name)
            });
        }

        return list;
    }

    public void SetFeatEnergy(int index, float energy)
    {
        if (float.IsNaN(energy) || energy < 0f || energy > 1000f)
            throw new ArgumentException("Energy cost must be between 0 and 1000.");
        BitConverter.GetBytes(energy).CopyTo(_data, RecordOffset(index) + FeatEnergy);
    }

    /// <summary>Hand-glow colour (aura over the caster's hands). 0–255 per channel.</summary>
    public void SetFeatGlow(int index, int r, int g, int b)
    {
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            throw new ArgumentException("Glow colour channels must be between 0 and 255.");
        var rec = RecordOffset(index);
        WriteU16(rec + FeatGlowR, r);
        WriteU16(rec + FeatGlowG, g);
        WriteU16(rec + FeatGlowB, b);
    }

    /// <summary>
    /// Light the effect casts on the environment. Channels are u16 and the stock data
    /// uses values above 255 (e.g. coneofcold), so the range is 0–65535, not 0–255.
    /// </summary>
    public void SetFeatLight(int index, int r, int g, int b)
    {
        if (r < 0 || r > 65535 || g < 0 || g > 65535 || b < 0 || b > 65535)
            throw new ArgumentException("Light colour channels must be between 0 and 65535.");
        var rec = RecordOffset(index);
        WriteU16(rec + FeatLightR, r);
        WriteU16(rec + FeatLightG, g);
        WriteU16(rec + FeatLightB, b);
    }

    /// <summary>Per-level scaling coefficient (+0x40). Effect still under investigation.</summary>
    public void SetFeatCoefficient(int index, float value)
    {
        if (float.IsNaN(value) || value < 0f || value > 1000f)
            throw new ArgumentException("Coefficient must be between 0 and 1000.");
        BitConverter.GetBytes(value).CopyTo(_data, RecordOffset(index) + FeatCoef);
    }

    /// <summary>
    /// Writes the displayed range. NOTE: no max >= min constraint is enforced — the
    /// stock game data violates it (mordensword ships with min=1, max=0, because for
    /// a summon these fields are not a damage range). Rejecting that would make it
    /// impossible to save an unmodified executable.
    /// </summary>
    public void SetFeatRange(int index, int min, int max)
    {
        if (min < 0 || min > 65535 || max < 0 || max > 65535)
            throw new ArgumentException("Range values must be between 0 and 65535.");
        var rec = RecordOffset(index);
        WriteU16(rec + FeatDmgMin, min);
        WriteU16(rec + FeatDmgMax, max);
    }

    private int RecordOffset(int index)
    {
        if (index < 0 || index >= FeatMaxRecords)
            throw new ArgumentOutOfRangeException(nameof(index));
        var rec = FeatTableOffset + index * FeatRecordSize;
        if (rec + FeatRecordSize > _data.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return rec;
    }

    // ---- io ----

    public void Save(string path) => File.WriteAllBytes(path, _data);

    private int ReadInt(int off) => BitConverter.ToInt32(_data, off);

    private void WriteInt(int off, int value) => BitConverter.GetBytes(value).CopyTo(_data, off);

    private int ReadU16(int off) => BitConverter.ToUInt16(_data, off);

    private void WriteU16(int off, int value)
    {
        _data[off]     = (byte)(value & 0xFF);
        _data[off + 1] = (byte)((value >> 8) & 0xFF);
    }

    private string ReadAscii(int off, int maxLen)
    {
        var end = off;
        while (end < _data.Length && end < off + maxLen && _data[end] != 0) end++;
        return Encoding.ASCII.GetString(_data, off, end - off);
    }
}
