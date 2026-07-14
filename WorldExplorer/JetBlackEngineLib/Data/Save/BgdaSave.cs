// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Save/BgdaSave.cs
//
// Reads/writes the difficulty field in a BGDA PS2 memory-card save (.psu export).
//
// CONFIRMED by a controlled four-save diff (Easy/Normal/Hard/Extreme): difficulty is
// a u32 at offset 0x1D4 within each save region, values 0..3. The save regions are
// named "BESLES-50672_slot0..3" (PAL). A diff of four otherwise-identical saves showed
// only 28 differing game-state bytes, all semantic (difficulty, player position,
// health, RNG) — NO checksum-like field in the game-state region. Editing difficulty
// changed exactly one byte.
//
// IMPORTANT: whether the console accepts an edited save without a whole-file checksum
// must be VERIFIED IN-GAME before relying on this. If a save fails to load after
// editing, BGDA validates a checksum this class does not yet handle — do not ship the
// feature until the round-trip is confirmed on hardware.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JetBlackEngineLib.Data.Save;

public enum BgdaDifficulty
{
    Easy    = 0,
    Normal  = 1,
    Hard    = 2,
    Extreme = 3
}

public sealed class BgdaSaveSlot
{
    public int SlotNumber { get; init; }
    /// <summary>Offset of the difficulty u32 within the whole .psu file.</summary>
    public int DifficultyFileOffset { get; init; }
    public BgdaDifficulty Difficulty { get; init; }
    public string LevelName { get; init; } = "";
    public string CharacterName { get; init; } = "";
}

public class BgdaSave
{
    // Difficulty is a u32 at this offset within each save region.
    private const int DifficultyOffsetInRegion = 0x1D4;
    private const int LevelNameOffsetInRegion  = 0x1C0;
    private const int CharNameOffsetInRegion   = 0x1FE;

    // PAL product code; region markers are "<code>_slot0..3".
    private const string RegionMarkerPrefix = "BESLES-50672_slot";

    private readonly byte[] _data;
    public string FilePath { get; }

    private BgdaSave(byte[] data, string path) { _data = data; FilePath = path; }

    public static BgdaSave Open(string path)
    {
        var data = File.ReadAllBytes(path);
        return new BgdaSave(data, path);
    }

    /// <summary>Locates each save slot region and reads its difficulty + labels.</summary>
    public List<BgdaSaveSlot> GetSlots()
    {
        var slots = new List<BgdaSaveSlot>();
        for (var n = 0; n < 4; n++)
        {
            var marker = RegionMarkerPrefix + n;
            var regionStart = IndexOf(marker);
            if (regionStart < 0) continue;

            var diffOff = regionStart + DifficultyOffsetInRegion;
            if (diffOff + 4 > _data.Length) continue;

            var diffVal = BitConverter.ToUInt32(_data, diffOff);
            if (diffVal > 3) continue; // not a valid difficulty region

            slots.Add(new BgdaSaveSlot
            {
                SlotNumber           = n,
                DifficultyFileOffset = diffOff,
                Difficulty           = (BgdaDifficulty)diffVal,
                LevelName            = ReadAscii(regionStart + LevelNameOffsetInRegion, 16),
                CharacterName        = ReadAscii(regionStart + CharNameOffsetInRegion, 16)
            });
        }
        return slots;
    }

    public void SetDifficulty(int slotNumber, BgdaDifficulty difficulty)
    {
        foreach (var slot in GetSlots())
        {
            if (slot.SlotNumber != slotNumber) continue;
            BitConverter.GetBytes((uint)difficulty).CopyTo(_data, slot.DifficultyFileOffset);
            return;
        }
        throw new ArgumentException($"Save slot {slotNumber} not found.");
    }

    public void Save(string path) => File.WriteAllBytes(path, _data);

    private int IndexOf(string ascii)
    {
        var needle = Encoding.ASCII.GetBytes(ascii);
        for (var i = 0; i <= _data.Length - needle.Length; i++)
        {
            var ok = true;
            for (var j = 0; j < needle.Length; j++)
                if (_data[i + j] != needle[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    private string ReadAscii(int off, int maxLen)
    {
        if (off < 0 || off >= _data.Length) return "";
        var end = off;
        while (end < _data.Length && end < off + maxLen && _data[end] != 0) end++;
        return Encoding.ASCII.GetString(_data, off, end - off);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// VERIFY-FIRST CHECKLIST (do before shipping a UI for this):
//   1. Load bgda_mem_card_slot0_extreme.psu onto a memory card (or via emulator).
//   2. Load slot 0 in-game. If it loads and shows Extreme difficulty -> no whole-file
//      checksum; safe to build the editor UI.
//   3. If the save is rejected as corrupt -> BGDA checksums the save; locate and
//      implement the checksum before exposing any save editing.
// Only after step 2 succeeds should this be surfaced as a tool feature.
// ═══════════════════════════════════════════════════════════════════════════════
