// ═══════════════════════════════════════════════════════════════════════════════
// COMPLETE REPLACEMENT for WorldExplorer/JetBlackEngineLib/Data/Save/BgdaSave.cs
//
// Reads/writes a BGDA PS2 memory-card save (.psu export):
//   * difficulty  (confirmed in-game: edited Easy->Extreme loaded and applied)
//   * character stats (level, XP, ability scores, HP/MP, gold, feat points)
//
// CHARACTER FIELD PROVENANCE
// The save serialises the live player struct starting at region+0x1FE (the character
// name sits there). Field offsets were confirmed three independent ways:
//   1. Pete's Cheat Engine addresses read live from PCSX2 RAM (player struct base
//      0x00298980, per-player stride 0x38E0).
//   2. Those live values match the executable's starting-stats table at file offset
//      0x0020A510 (Vahn / Human Arcane Archer = 17,11,13,15,14,11).
//   3. The same layout applied to save files yields correct values for a DIFFERENT
//      character (Adrianna / Elven Sorceress = 13,17,12,14,13,15).
// The XP field also explains the region+0x256 byte that changes after a kill: it is
// XP gained (24 for a rat), NOT an enemy-kill flag.
//
// ABILITY SCORES ARE STORED THREE TIMES: base (+0x5C), current (+0x74) and display
// (+0x8C). Which one the engine reads for combat is NOT yet confirmed, so SetAbility
// writes ALL THREE to keep them consistent. If an in-game test shows only one matters,
// this can be narrowed later.
//
// CONFIRMED IN-GAME (loaded an edited save on hardware/emulator):
//   * ability scores PERSIST - all six, and all three copies survive the load.
//   * gold PERSISTS.
//   * HP and MP are NOT persistable: the engine RECOMPUTES them on load from CON.
//     CONFIRMED FORMULA:  HP max = level x (class base 12 + CON modifier)
//     Verified on four in-game loads: L1/CON14=14, L1/CON25=19, L1/CON30=22,
//     L5/CON14=70. HP/MP are exposed READ-ONLY; raise CON or Level instead.
//   * LEVEL edits work (level 5 / XP 12000 / next-level 15000 all correct) BUT the
//     game does NOT grant feat points for levels gained this way - those are
//     awarded by the runtime level-up routine. Set FeatPoints explicitly.
//   * ABILITY POINTS: the game lets you raise one ability every 4 levels. Spending
//     a point writes to CURRENT (+0x74) and DISPLAY (+0x8C) but leaves BASE
//     (+0x5C) at the original rolled score - a level-5 character who spent a
//     point on STR read base 17, current 18, display 18. So:
//         base    = original rolled score
//         current = effective score (base + spent points + equipment bonuses)
//         display = UI copy of current
//     These are STORED, not recomputed on load, which is why written values
//     survive intact. SetAbility writes all three so the edited score becomes
//     both the original and the effective value.
//   * An edited Level DOES grant ability points (they appear to derive from the
//     level value) but does NOT grant feat points. The two use different award
//     mechanisms; only feat points need setting by hand.
// Also confirmed by disassembly: the health bar reads +0xA4 / +0xA8 off the player
// struct (lwc1 f00,0xA4(s1); lwc1 f01,0xA8(s1); div.s) with s1 = 0x00298980, and a
// loop at 0x00167408 copies current (+0x74) -> display (+0x8C), so 'display' is a
// derived copy rather than an independent field.
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

public enum BgdaAbility
{
    Strength     = 0,
    Intelligence = 1,
    Wisdom       = 2,
    Dexterity    = 3,
    Constitution = 4,
    Charisma     = 5
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

/// <summary>Character stats read from the serialised player struct.</summary>
public sealed class BgdaCharacter
{
    public int SlotNumber { get; init; }
    public string Name { get; init; } = "";
    public int Level { get; init; }
    public uint Experience { get; init; }
    /// <summary>Base ability scores, indexed by <see cref="BgdaAbility"/>.</summary>
    public uint[] Abilities { get; init; } = new uint[6];
    public float HpCurrent { get; init; }
    public float HpMax { get; init; }
    public float MpCurrent { get; init; }
    public float MpMax { get; init; }
    public uint Gold { get; init; }
    public uint FeatPoints { get; init; }
}

public class BgdaSave
{
    // ── save-region layout ──────────────────────────────────────────────────────
    private const int DifficultyOffsetInRegion = 0x1D4;
    private const int LevelNameOffsetInRegion  = 0x1C0;
    private const int CharNameOffsetInRegion   = 0x1FE;

    /// <summary>Region offset where the serialised player struct begins.</summary>
    private const int PlayerStructInRegion = 0x1FE;

    // ── player-struct field offsets (struct-relative) ───────────────────────────
    private const int OffName        = 0x00;   // ASCII, NUL-terminated
    private const int OffLevel       = 0x55;   // u8
    private const int OffExperience  = 0x58;   // u32
    private const int OffAbilBase    = 0x5C;   // 6 x u32
    private const int OffAbilCurrent = 0x74;   // 6 x u32
    private const int OffAbilDisplay = 0x8C;   // 6 x u32
    private const int OffHpCurrent   = 0xA4;   // f32
    private const int OffHpMax       = 0xA8;   // f32
    private const int OffMpCurrent   = 0xBC;   // f32
    private const int OffMpMax       = 0xC0;   // f32
    private const int OffGold        = 0xDC;   // u32
    private const int OffFeatPoints  = 0xE0;   // u32

    // ── validation limits ───────────────────────────────────────────────────────
    // NOTE (mordensword lesson): every limit below must accept STOCK data unchanged.
    // Stock observed: level 1, XP 0, abilities 10..20, HP/MP 10..14, gold 0, feats 0.
    public const int MinLevel = 1;
    public const int MaxLevel = 16;          // XP-to-level table has 16 entries
    public const uint MaxExperience = 9_999_999;
    public const uint MinAbility = 1;
    public const uint MaxAbility = 99;
    public const uint MaxGold = 9_999_999;
    public const uint MaxFeatPoints = 999;

    private const string RegionMarkerPrefix = "BESLES-50672_slot";

    private readonly byte[] _data;
    public string FilePath { get; }

    private BgdaSave(byte[] data, string path) { _data = data; FilePath = path; }

    public static BgdaSave Open(string path)
    {
        var data = File.ReadAllBytes(path);
        return new BgdaSave(data, path);
    }

    // ── slots / difficulty ──────────────────────────────────────────────────────

    /// <summary>Locates each save slot region and reads its difficulty + labels.</summary>
    public List<BgdaSaveSlot> GetSlots()
    {
        var slots = new List<BgdaSaveSlot>();
        for (var n = 0; n < 4; n++)
        {
            var regionStart = FindRegion(n);
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
        var regionStart = RequireRegion(slotNumber);
        BitConverter.GetBytes((uint)difficulty)
                    .CopyTo(_data, regionStart + DifficultyOffsetInRegion);
    }

    // ── character stats ─────────────────────────────────────────────────────────

    /// <summary>Reads the serialised player struct for one slot.</summary>
    public BgdaCharacter GetCharacter(int slotNumber)
    {
        var b = RequireStructBase(slotNumber);
        var abilities = new uint[6];
        for (var i = 0; i < 6; i++)
            abilities[i] = BitConverter.ToUInt32(_data, b + OffAbilBase + i * 4);

        return new BgdaCharacter
        {
            SlotNumber = slotNumber,
            Name       = ReadAscii(b + OffName, 16),
            Level      = _data[b + OffLevel],
            Experience = BitConverter.ToUInt32(_data, b + OffExperience),
            Abilities  = abilities,
            HpCurrent  = BitConverter.ToSingle(_data, b + OffHpCurrent),
            HpMax      = BitConverter.ToSingle(_data, b + OffHpMax),
            MpCurrent  = BitConverter.ToSingle(_data, b + OffMpCurrent),
            MpMax      = BitConverter.ToSingle(_data, b + OffMpMax),
            Gold       = BitConverter.ToUInt32(_data, b + OffGold),
            FeatPoints = BitConverter.ToUInt32(_data, b + OffFeatPoints)
        };
    }

    public List<BgdaCharacter> GetCharacters()
    {
        var list = new List<BgdaCharacter>();
        foreach (var slot in GetSlots()) list.Add(GetCharacter(slot.SlotNumber));
        return list;
    }

    public void SetLevel(int slotNumber, int level)
    {
        if (level < MinLevel || level > MaxLevel)
            throw new ArgumentException($"Level must be {MinLevel}-{MaxLevel}.");
        _data[RequireStructBase(slotNumber) + OffLevel] = (byte)level;
    }

    public void SetExperience(int slotNumber, uint xp)
    {
        if (xp > MaxExperience)
            throw new ArgumentException($"XP must be 0-{MaxExperience}.");
        BitConverter.GetBytes(xp).CopyTo(_data, RequireStructBase(slotNumber) + OffExperience);
    }

    /// <summary>
    /// Writes an ability score. The value is written to ALL THREE copies (base,
    /// current, display) because which one the engine reads is not yet confirmed.
    /// </summary>
    public void SetAbility(int slotNumber, BgdaAbility ability, uint value)
    {
        if (value < MinAbility || value > MaxAbility)
            throw new ArgumentException($"Ability scores must be {MinAbility}-{MaxAbility}.");
        var b = RequireStructBase(slotNumber);
        var i = (int)ability * 4;
        var bytes = BitConverter.GetBytes(value);
        bytes.CopyTo(_data, b + OffAbilBase    + i);
        bytes.CopyTo(_data, b + OffAbilCurrent + i);
        bytes.CopyTo(_data, b + OffAbilDisplay + i);
    }

    public void SetGold(int slotNumber, uint gold)
    {
        if (gold > MaxGold) throw new ArgumentException($"Gold must be 0-{MaxGold}.");
        BitConverter.GetBytes(gold).CopyTo(_data, RequireStructBase(slotNumber) + OffGold);
    }

    public void SetFeatPoints(int slotNumber, uint points)
    {
        if (points > MaxFeatPoints)
            throw new ArgumentException($"Feat points must be 0-{MaxFeatPoints}.");
        BitConverter.GetBytes(points).CopyTo(_data, RequireStructBase(slotNumber) + OffFeatPoints);
    }

    public void Save(string path) => File.WriteAllBytes(path, _data);

    // ── helpers ─────────────────────────────────────────────────────────────────

    private int FindRegion(int slotNumber) => IndexOf(RegionMarkerPrefix + slotNumber);

    private int RequireRegion(int slotNumber)
    {
        var start = FindRegion(slotNumber);
        if (start < 0) throw new ArgumentException($"Save slot {slotNumber} not found.");
        return start;
    }

    private int RequireStructBase(int slotNumber) =>
        RequireRegion(slotNumber) + PlayerStructInRegion;

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
