// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.Difficulty.cs
//
// Difficulty scaling (Easy / Normal / Hard / Extreme) for every player count.
//
// STOCK CODE
//   Monster stat routine 0x0010D7C0 (every monster set-up, bosses included):
//     Easy     HP and damage die x0.7     (one f32 at 0x32200C)
//     Normal   no scaling
//     Hard     HP x1.3 (0x322010), damage die x2 (add.s - an instruction, not a value)
//     Extreme  HP and damage die x1.3 (0x322014), monster level +30 (lui immediate)
//   The level feeds the HP, damage and XP formulas, so +30 raises all three.
//   Boss HP factor 0x0012C930: Easy x0.7 (0x3222D8), Normal x1.0, Hard x1.3
//   (0x3222DC), Extreme x5.0 (lui immediate). Co-op factors apply on top.
//
// Because Normal and Hard damage are not stored values and Easy/Extreme share one
// value for HP and damage, the editor rewrites both difficulty dispatches - same
// size, same place - into lookups of four tables kept inside the rewritten code:
//     monster HP x / damage-die x / level + (0x0010D804 / 0x0010D814 / 0x0010D824)
//     boss HP x (0x0012C960)
// With the stock values every result is bit-identical to the stock code (verified
// by running both versions: test_we_patch.py in the three-player tools). Setting the stock
// values again restores the original instructions byte for byte. The blocks sit
// outside the three-player patch, so both can be used together in either order.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

public enum DifficultyCodeState
{
    /// <summary>The original difficulty code (values read from where the stock code keeps them).</summary>
    Stock,
    /// <summary>The editor's table code.</summary>
    Table,
    /// <summary>Something else - editing is disabled.</summary>
    Unrecognized
}

/// <summary>Per-difficulty factors; index 0 = Easy, 1 = Normal, 2 = Hard, 3 = Extreme.</summary>
public sealed class DifficultyScaling
{
    /// <summary>Monster HP multiplier.</summary>
    public float[] MonsterHp { get; init; } = new float[4];
    /// <summary>Monster damage-die multiplier.</summary>
    public float[] MonsterDamage { get; init; } = new float[4];
    /// <summary>Added to the monster level (raises HP, damage and XP together).</summary>
    public float[] LevelBonus { get; init; } = new float[4];
    /// <summary>Boss HP multiplier (bosses with a fixed HP).</summary>
    public float[] BossHp { get; init; } = new float[4];

    public DifficultyScaling Clone() => new()
    {
        MonsterHp = (float[])MonsterHp.Clone(),
        MonsterDamage = (float[])MonsterDamage.Clone(),
        LevelBonus = (float[])LevelBonus.Clone(),
        BossHp = (float[])BossHp.Clone()
    };

    /// <summary>True when every value has exactly the same bits.</summary>
    public bool SameAs(DifficultyScaling other)
    {
        static bool Eq(float[] a, float[] b) =>
            a.Length == b.Length && a.Zip(b).All(p => BitConverter.SingleToInt32Bits(p.First) ==
                                                      BitConverter.SingleToInt32Bits(p.Second));
        return Eq(MonsterHp, other.MonsterHp) && Eq(MonsterDamage, other.MonsterDamage) &&
               Eq(LevelBonus, other.LevelBonus) && Eq(BossHp, other.BossHp);
    }
}

public partial class BgdaExecutable
{
    public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Extreme" };

    public const float MinDifficultyFactor = 0.01f;
    public const float MaxDifficultyFactor = 100f;
    public const float MaxLevelBonus = 200f;

    private const int StatWords = 31;   // 0x0010D7C0-0x0010D83B
    private const int BossWords = 24;   // 0x0012C930-0x0012C98F
    private const int StatExtremeLevelWord = (DifficultyPatch.StockExtremeLevel - DifficultyPatch.StatOffset) / 4;
    private const int BossExtremeWord = (DifficultyPatch.StockBossExtreme - DifficultyPatch.BossOffset) / 4;
    private const uint LuiAtMask = 0xFFFF0000u, LuiAtWord = 0x3C010000u;

    /// <summary>The stock game's difficulty values.</summary>
    public static DifficultyScaling StockDifficulty() => new()
    {
        MonsterHp = (float[])DifficultyPatch.StockHp.Clone(),
        MonsterDamage = (float[])DifficultyPatch.StockDamage.Clone(),
        LevelBonus = (float[])DifficultyPatch.StockLevel.Clone(),
        BossHp = (float[])DifficultyPatch.StockBossHp.Clone()
    };

    public DifficultyCodeState GetDifficultyState()
    {
        if (DifficultyPatch.StatOffset + StatWords * 4 > _data.Length ||
            DifficultyPatch.BossOffset + BossWords * 4 > _data.Length)
            return DifficultyCodeState.Unrecognized;

        if (IsStockStat() && IsStockBoss()) return DifficultyCodeState.Stock;
        if (IsTableStat() && IsTableBoss()) return DifficultyCodeState.Table;
        return DifficultyCodeState.Unrecognized;
    }

    private uint Word(int off) => BitConverter.ToUInt32(_data, off);

    // the two lui immediates (Extreme level bonus, Extreme boss factor) may hold any value
    private bool IsStockStat()
    {
        for (var i = 0; i < StatWords; i++)
        {
            var w = Word(DifficultyPatch.StatOffset + i * 4);
            if (i == StatExtremeLevelWord ? (w & LuiAtMask) != LuiAtWord : w != DifficultyPatch.StockStatWords[i])
                return false;
        }

        return true;
    }

    private bool IsStockBoss()
    {
        for (var i = 0; i < BossWords; i++)
        {
            var w = Word(DifficultyPatch.BossOffset + i * 4);
            if (i == BossExtremeWord ? (w & LuiAtMask) != LuiAtWord : w != DifficultyPatch.StockBossWords[i])
                return false;
        }

        return true;
    }

    private bool IsTableStat()
    {
        var code = DifficultyPatch.StatCode;
        for (var i = 0; i < code.Length; i++)
        {
            if (Word(DifficultyPatch.StatOffset + i * 4) != code[i]) return false;
        }

        for (var i = code.Length + 12; i < StatWords; i++)
        {
            if (Word(DifficultyPatch.StatOffset + i * 4) != 0) return false;
        }

        return true;
    }

    private bool IsTableBoss()
    {
        var code = DifficultyPatch.BossCode;
        for (var i = 0; i < code.Length; i++)
        {
            if (Word(DifficultyPatch.BossOffset + i * 4) != code[i]) return false;
        }

        for (var i = code.Length + 4; i < BossWords; i++)
        {
            if (Word(DifficultyPatch.BossOffset + i * 4) != 0) return false;
        }

        return true;
    }

    private static float UpperHalfFloat(uint luiWord) =>
        BitConverter.Int32BitsToSingle((int)((luiWord & 0xFFFF) << 16));

    private float Float(int off) => BitConverter.ToSingle(_data, off);

    /// <summary>The current difficulty values (state must not be Unrecognized).</summary>
    public DifficultyScaling GetDifficultyScaling()
    {
        switch (GetDifficultyState())
        {
            case DifficultyCodeState.Stock:
            {
                var easy = Float(DifficultyPatch.StockEasy);
                var hard = Float(DifficultyPatch.StockHardHp);
                var extreme = Float(DifficultyPatch.StockExtreme);
                var level = UpperHalfFloat(Word(DifficultyPatch.StockExtremeLevel));
                var bossExtreme = UpperHalfFloat(Word(DifficultyPatch.StockBossExtreme));
                return new DifficultyScaling
                {
                    MonsterHp = new[] { easy, 1f, hard, extreme },
                    MonsterDamage = new[] { easy, 1f, 2f, extreme },
                    LevelBonus = new[] { 0f, 0f, 0f, level },
                    BossHp = new[]
                    {
                        Float(DifficultyPatch.StockBossEasy), 1f, Float(DifficultyPatch.StockBossHard), bossExtreme
                    }
                };
            }
            case DifficultyCodeState.Table:
            {
                float[] Table(int off) =>
                    Enumerable.Range(0, 4).Select(i => Float(off + i * 4)).ToArray();
                return new DifficultyScaling
                {
                    MonsterHp = Table(DifficultyPatch.HpTable),
                    MonsterDamage = Table(DifficultyPatch.DamageTable),
                    LevelBonus = Table(DifficultyPatch.LevelTable),
                    BossHp = Table(DifficultyPatch.BossTable)
                };
            }
            default:
                throw new InvalidOperationException(
                    "The difficulty code in this executable is not recognised, so it cannot be edited here.");
        }
    }

    /// <summary>
    /// Writes difficulty values. Unchanged values write nothing; the stock values
    /// restore the original instructions (when the stock constants are untouched);
    /// anything else switches both blocks to the table code. Every value is checked
    /// first and nothing is written if any is out of range.
    /// </summary>
    public void SetDifficultyScaling(DifficultyScaling d)
    {
        ValidateDifficulty(d);
        d = Normalised(d);
        if (GetDifficultyState() == DifficultyCodeState.Unrecognized)
            throw new InvalidOperationException(
                "The difficulty code in this executable is not recognised, so it cannot be edited here.");

        if (GetDifficultyScaling().SameAs(d)) return;

        if (d.SameAs(StockDifficulty()) && StockConstantsIntact())
        {
            for (var i = 0; i < StatWords; i++)
                BitConverter.GetBytes(DifficultyPatch.StockStatWords[i]).CopyTo(_data, DifficultyPatch.StatOffset + i * 4);
            for (var i = 0; i < BossWords; i++)
                BitConverter.GetBytes(DifficultyPatch.StockBossWords[i]).CopyTo(_data, DifficultyPatch.BossOffset + i * 4);
            return;
        }

        var stat = new uint[StatWords];
        DifficultyPatch.StatCode.CopyTo(stat, 0);
        var at = DifficultyPatch.StatCode.Length;
        foreach (var v in d.MonsterHp.Concat(d.MonsterDamage).Concat(d.LevelBonus))
            stat[at++] = (uint)BitConverter.SingleToInt32Bits(v);

        var boss = new uint[BossWords];
        DifficultyPatch.BossCode.CopyTo(boss, 0);
        at = DifficultyPatch.BossCode.Length;
        foreach (var v in d.BossHp) boss[at++] = (uint)BitConverter.SingleToInt32Bits(v);

        for (var i = 0; i < StatWords; i++)
            BitConverter.GetBytes(stat[i]).CopyTo(_data, DifficultyPatch.StatOffset + i * 4);
        for (var i = 0; i < BossWords; i++)
            BitConverter.GetBytes(boss[i]).CopyTo(_data, DifficultyPatch.BossOffset + i * 4);
    }

    /// <summary>-0 becomes +0, so typing "-0" still compares equal to the stock 0.</summary>
    private static DifficultyScaling Normalised(DifficultyScaling d) => new()
    {
        MonsterHp = d.MonsterHp.Select(v => v + 0f).ToArray(),
        MonsterDamage = d.MonsterDamage.Select(v => v + 0f).ToArray(),
        LevelBonus = d.LevelBonus.Select(v => v + 0f).ToArray(),
        BossHp = d.BossHp.Select(v => v + 0f).ToArray()
    };

    private bool StockConstantsIntact()
    {
        bool Is(int off, float v) =>
            BitConverter.SingleToInt32Bits(Float(off)) == BitConverter.SingleToInt32Bits(v);
        var s = StockDifficulty();
        return Is(DifficultyPatch.StockEasy, s.MonsterHp[0]) && Is(DifficultyPatch.StockHardHp, s.MonsterHp[2]) &&
               Is(DifficultyPatch.StockExtreme, s.MonsterHp[3]) && Is(DifficultyPatch.StockBossEasy, s.BossHp[0]) &&
               Is(DifficultyPatch.StockBossHard, s.BossHp[2]);
    }

    /// <summary>Throws <see cref="ArgumentException"/> naming the first bad value.</summary>
    public static void ValidateDifficulty(DifficultyScaling d)
    {
        if (d.MonsterHp.Length != 4 || d.MonsterDamage.Length != 4 || d.LevelBonus.Length != 4 || d.BossHp.Length != 4)
            throw new ArgumentException("Difficulty scaling needs four values per column.");

        for (var i = 0; i < 4; i++)
        {
            var name = DifficultyNames[i];
            void Factor(string what, float v)
            {
                if (float.IsNaN(v) || v < MinDifficultyFactor || v > MaxDifficultyFactor)
                    throw new ArgumentException(
                        $"{name} {what} must be between {Inv(MinDifficultyFactor)} and {Inv(MaxDifficultyFactor)}.");
            }

            Factor("monster HP", d.MonsterHp[i]);
            Factor("monster damage", d.MonsterDamage[i]);
            Factor("boss HP", d.BossHp[i]);
            if (float.IsNaN(d.LevelBonus[i]) || d.LevelBonus[i] < 0f || d.LevelBonus[i] > MaxLevelBonus)
                throw new ArgumentException($"{name} level bonus must be between 0 and {Inv(MaxLevelBonus)}.");
        }
    }
}
