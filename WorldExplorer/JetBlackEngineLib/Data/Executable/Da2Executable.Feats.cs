// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Executable.Feats.cs
//
// ABILITY TEMPLATES, 0x358958: 0x58-byte records ended by "END" (102 of them). The first 0x48
// bytes are Dark Alliance's own record (byte for byte the same for the spells both games have):
//   +0x00 name (also the text key the quick-spell list looks up)   +0x20 s16 slot type (the handler)
//   +0x26 u16 damage min   +0x28 u16 damage max   +0x2C f32 energy cost (per frame for the stream
//   spells: Burning Hands, Snowblind, Lightning Bolt, Enervation, Life Drain)
//   +0x30/32/34 u16 hand-glow R, G, B   +0x38/3A/3C u16 light R, G, B (may exceed 255)
//   +0x40 f32 per-level coefficient    DA2 adds +0x48 u64 feat id and +0x50 f32 energy per extra rank
//   (cost of a slot = +0x2C + (rank - 1) x +0x50).
// Learning a feat (0x2B87A0) copies the template into one of the character's 24 spell slots, so an
// edit reaches the spells a character learns afterwards; a slot already in a saved character keeps
// the values it was learned with.
//
// CLASS FEAT TABLES: the game builds the table of each class (0x56D7D0 + class x 0x3408) at every
// new game / load (0x291C10) from static tables of 0x68-byte entries ended by "END":
//   Drizzt 0x34CDB0, Dorn 0x34D568, Ysuran 0x34E130, Vhaidra 0x34EF68, Borador 0x34FB98,
//   Allessia 0x350898, Artemis 0x3516D0 (classes 0-2 start empty; the three-player mod fills them).
//   Entry: +0x00 name key, +0x20 description key, +0x40 state, +0x41 max rank, +0x42 base cost (signed
//   byte), +0x44 f32, +0x48 u64 feat id, +0x54 byte. Ranks are saved by INDEX into the table, so
//   entries are never added or moved here, only their numbers changed.
// PRICE of the next rank (0x2BB158): base x rank for bases 1-4, base + floor((rank - 1) ^ 1.6) for 5
//   and more; Enchant Arrows (24), Hail of Arrows (6), Mordenkainen's Sword (35) and Magic Missile (8)
//   have their own five prices (0x35B598 / 0x35B5B0 / 0x35B5C8 / 0x35B5E0). The three-player mod's
//   feat group, when ticked, prices the feats of its list itself.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>An ability template (spell / feat effect) as the executable holds it.</summary>
public sealed record Da2Ability(int Index, uint Address, string Name, int Type, int DamageMin, int DamageMax, float Energy,
    float EnergyPerRank, int GlowR, int GlowG, int GlowB, int LightR, int LightG, int LightB, float Coefficient, int FeatId)
{
    /// <summary>The handlers of these types bill their energy per frame (the game runs 60 frames a second).</summary>
    public bool IsStream => Type is 0 or 11 or 16 or 43 or 48;
}

/// <summary>An entry of a class feat table.</summary>
public sealed record Da2ClassFeat(int ClassId, int Index, uint Address, string NameKey, int FeatId, int MaxRank, int BaseCost, int State)
{
    public string Name => Da2Names.Feat(FeatId);

    /// <summary>The game's price of each rank (without the mod's feat group).</summary>
    public int[] Prices(Func<int, int[]?> specialPrices) =>
        Enumerable.Range(0, MaxRank).Select(r => Da2Executable.GamePrice(FeatId, BaseCost, r, specialPrices)).ToArray();
}

public sealed partial class Da2Executable
{
    public const uint AbilityTable = 0x00358958;
    public const uint AbilitySize = 0x58;
    private const int MaxAbilities = 128;

    public const uint ClassFeatSize = 0x68;
    public const int MaxRankLimit = 5, MaxBaseCost = 127;

    /// <summary>Static feat table of each class, by class id (null: the class starts with none).</summary>
    public static readonly uint?[] ClassFeatTables =
    {
        null, null, null, 0x0034CDB0, 0x0034D568, 0x0034EF68, 0x0034E130, 0x0034FB98, 0x00350898, 0x003516D0
    };

    /// <summary>The feats with a price table of their own: Enchant Arrows, Hail of Arrows, Mordenkainen's Sword, Magic Missile.</summary>
    public static readonly int[] SpecialPriceFeats = { 24, 6, 35, 8 };

    public static uint SpecialPriceTable(int featId) => featId switch
    {
        24 => 0x0035B598, 6 => 0x0035B5B0, 35 => 0x0035B5C8, 8 => 0x0035B5E0,
        _ => throw new ArgumentOutOfRangeException(nameof(featId))
    };

    public const int MaxSpecialPrice = 255;
    public const float MaxEnergy = 1000f;

    // ---- ability templates ----

    public List<Da2Ability> GetAbilities()
    {
        var list = new List<Da2Ability>();
        for (var i = 0; i < MaxAbilities; i++)
        {
            var a = AbilityTable + (uint)(i * AbilitySize);
            var name = String(a, 32);
            if (name == "END" || name.Length == 0) break;
            list.Add(new Da2Ability(i, a, name, (short)U16(a + 0x20), U16(a + 0x26), U16(a + 0x28), F32(a + 0x2C), F32(a + 0x50),
                U16(a + 0x30), U16(a + 0x32), U16(a + 0x34), U16(a + 0x38), U16(a + 0x3A), U16(a + 0x3C), F32(a + 0x40),
                (int)Word(a + 0x48)));
        }

        return list;
    }

    private uint AbilityAddress(int index)
    {
        var a = GetAbilities().FirstOrDefault(x => x.Index == index) ?? throw new ArgumentOutOfRangeException(nameof(index));
        return a.Address;
    }

    /// <summary>Energy cost and the energy each extra rank adds (0-1000 each).</summary>
    public void SetAbilityEnergy(int index, float energy, float perRank)
    {
        if (float.IsNaN(energy) || energy < 0 || energy > MaxEnergy || float.IsNaN(perRank) || perRank < 0 || perRank > MaxEnergy)
            throw new ArgumentException($"Energy values must be from 0 to {MaxEnergy:0}.");
        var a = AbilityAddress(index);
        SetF32(a + 0x2C, energy);
        SetF32(a + 0x50, perRank);
    }

    /// <summary>The damage range (no min-below-max rule: the stock data breaks it for summons).</summary>
    public void SetAbilityDamage(int index, int min, int max)
    {
        if (min is < 0 or > 65535 || max is < 0 or > 65535) throw new ArgumentException("Damage values must be from 0 to 65535.");
        var a = AbilityAddress(index);
        SetU16(a + 0x26, min);
        SetU16(a + 0x28, max);
    }

    public void SetAbilityGlow(int index, int r, int g, int b)
    {
        if (r is < 0 or > 255 || g is < 0 or > 255 || b is < 0 or > 255) throw new ArgumentException("Glow colour channels must be from 0 to 255.");
        var a = AbilityAddress(index);
        SetU16(a + 0x30, r);
        SetU16(a + 0x32, g);
        SetU16(a + 0x34, b);
    }

    public void SetAbilityLight(int index, int r, int g, int b)
    {
        if (r is < 0 or > 65535 || g is < 0 or > 65535 || b is < 0 or > 65535) throw new ArgumentException("Light colour channels must be from 0 to 65535.");
        var a = AbilityAddress(index);
        SetU16(a + 0x38, r);
        SetU16(a + 0x3A, g);
        SetU16(a + 0x3C, b);
    }

    public void SetAbilityCoefficient(int index, float value)
    {
        if (float.IsNaN(value) || value < 0 || value > 1000) throw new ArgumentException("The coefficient must be from 0 to 1000.");
        SetF32(AbilityAddress(index) + 0x40, value);
    }

    // ---- class feat tables ----

    /// <summary>Every entry of the seven static class tables (classes 3-9), in class order.</summary>
    public List<Da2ClassFeat> GetClassFeats()
    {
        var list = new List<Da2ClassFeat>();
        for (var c = 0; c < ClassFeatTables.Length; c++)
        {
            if (ClassFeatTables[c] is not { } start) continue;
            for (var i = 0; i < 128; i++)
            {
                var a = start + (uint)(i * ClassFeatSize);
                var key = String(a, 32);
                if (key == "END") break;
                list.Add(new Da2ClassFeat(c, i, a, key, (int)Word(a + 0x48), U8(a + 0x41), (sbyte)U8(a + 0x42), U8(a + 0x40)));
            }
        }

        return list;
    }

    public void SetClassFeat(uint address, int maxRank, int baseCost)
    {
        if (GetClassFeats().All(f => f.Address != address)) throw new ArgumentOutOfRangeException(nameof(address));
        if (maxRank < 1 || maxRank > MaxRankLimit) throw new ArgumentException($"A feat has 1 to {MaxRankLimit} ranks.");
        if (baseCost < 1 || baseCost > MaxBaseCost) throw new ArgumentException($"A base cost is from 1 to {MaxBaseCost}.");
        SetU8(address + 0x41, maxRank);
        SetU8(address + 0x42, baseCost);
    }

    /// <summary>The five prices of a feat with a table of its own (Enchant Arrows, Hail of Arrows, Mordenkainen's Sword, Magic Missile).</summary>
    public int[] GetSpecialPrices(int featId)
    {
        var t = SpecialPriceTable(featId);
        return Enumerable.Range(0, 5).Select(i => I32(t + (uint)(4 * i))).ToArray();
    }

    public void SetSpecialPrices(int featId, int[] prices)
    {
        if (prices.Length != 5) throw new ArgumentException("Five prices are needed.");
        if (prices.Any(p => p < 1 || p > MaxSpecialPrice))
            throw new ArgumentException($"{Da2Names.Feat(featId)}: prices must be from 1 to {MaxSpecialPrice}.");
        var t = SpecialPriceTable(featId);
        for (var i = 0; i < 5; i++) SetI32(t + (uint)(4 * i), prices[i]);
    }

    /// <summary>
    /// The game's price of rank <paramref name="rank"/> + 1 (0x2BB158): its own table for the four DA1 spells,
    /// else base x (rank + 1) for bases 1-4 and base + floor(rank ^ 1.6) from 5 on.
    /// </summary>
    public static int GamePrice(int featId, int baseCost, int rank, Func<int, int[]?> specialPrices)
    {
        if (SpecialPriceFeats.Contains(featId) && specialPrices(featId) is { } table) return table[Math.Min(rank, 4)];
        if (baseCost < 5) return baseCost * (rank + 1);
        return baseCost + (int)MathF.Truncate(MathF.Pow(rank, 1.6f));
    }
}
