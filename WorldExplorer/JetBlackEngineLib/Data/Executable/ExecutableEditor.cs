// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.cs
//
// Reads/writes tuning tables in the BGDA main executable (SLES_506.72, PAL).
// Discovered via reverse engineering (see the ELF toolkit docs). All offsets are
// FILE offsets into the executable. Values are confirmed in-game:
//   • XP-to-level curve        file 0x00134DA0, 16 × int32
//   • Character starting stats file 0x0020A510, 4 classes × 6 ability scores int32
//
// This class is intentionally standalone (no GOB/LMP dependency): the executable is
// a plain file the user opens, edits, and saves. A build check verifies the file is
// the supported executable before any offset is trusted.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.IO;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The four playable classes, in table order.</summary>
public enum BgdaClass
{
    HumanArcaneArcher = 0,
    ElvenSorceress    = 1,
    DwarvenFighter    = 2,
    DrowRanger        = 3
}

public class BgdaExecutable
{
    // ---- supported-build layout (SLES_506.72) ----
    public const int XpTableOffset      = 0x00134DA0;   // 16 × int32
    public const int XpTableCount       = 16;
    public const int StatsTableOffset   = 0x0020A510;   // 4 classes × 6 stats × int32
    public const int StatsClassCount    = 4;
    public const int StatsPerClass      = 6;            // STR, INT, WIS, DEX, CON, CHA

    public static readonly string[] StatNames  = { "STR", "INT", "WIS", "DEX", "CON", "CHA" };
    public static readonly string[] ClassNames =
        { "Human Arcane Archer", "Elven Sorceress", "Dwarven Fighter", "Drow Ranger" };

    private readonly byte[] _data;
    public string FilePath { get; }

    private BgdaExecutable(byte[] data, string path) { _data = data; FilePath = path; }

    /// <summary>
    /// Opens an executable and verifies it is the supported build. Throws
    /// <see cref="NotSupportedException"/> with a clear message otherwise, so the UI
    /// never writes to an unrecognized file at the wrong offsets.
    /// </summary>
    public static BgdaExecutable Open(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 4 || data[0] != 0x7F || data[1] != (byte)'E'
            || data[2] != (byte)'L' || data[3] != (byte)'F')
            throw new NotSupportedException("Not a PS2 executable (missing ELF header).");

        var exe = new BgdaExecutable(data, path);
        if (!exe.LooksLikeSupportedBuild())
            throw new NotSupportedException(
                "This executable isn't the recognized Baldur's Gate: Dark Alliance build " +
                "(PAL SLES-506.72). The tuning tables live at build-specific offsets, so " +
                "editing an unrecognized build is disabled to avoid corrupting it.");
        return exe;
    }

    /// <summary>
    /// Fingerprint the supported build by validating BOTH tables against known-good
    /// value ranges — the XP curve ascends from 0, and every starting stat is a
    /// plausible ability score. This is a strong, offset-specific signature.
    /// </summary>
    private bool LooksLikeSupportedBuild()
    {
        if (StatsTableOffset + StatsClassCount * StatsPerClass * 4 > _data.Length) return false;
        if (XpTableOffset + XpTableCount * 4 > _data.Length) return false;

        // XP curve: strictly ascending, first entry 0, last in a sane range.
        var prev = -1;
        for (var i = 0; i < XpTableCount; i++)
        {
            var v = ReadInt(XpTableOffset + i * 4);
            if (v < 0 || v <= prev && i > 0) return false;
            if (i == 0 && v != 0) return false;
            prev = v;
        }
        if (ReadInt(XpTableOffset) != 0) return false;

        // Starting stats: every value a plausible ability score.
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
            if (values[i] <= values[i - 1])
                throw new ArgumentException(
                    $"XP thresholds must strictly increase (entry {i + 1} ≤ entry {i}).");
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
            for (var s = 0; s < StatsPerClass; s++)
                t[c, s] = GetStat((BgdaClass)c, s);
        return t;
    }

    public void Save(string path) => File.WriteAllBytes(path, _data);

    private int ReadInt(int off) => BitConverter.ToInt32(_data, off);
    private void WriteInt(int off, int v) => BitConverter.GetBytes(v).CopyTo(_data, off);
}
