// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Executable.Monsters.cs
//
// HOW A CREATURE GETS ITS STATS: its constructor calls setStats 0x24B250(this, HP x, armour x,
// damage x, attack x, XP x) with its own multipliers in f12..f16 (some classes call it again after
// picking a weapon or sub-type: the last call counts). setStats then scales them:
//   difficulty (0x35FD98)  Easy: HP and damage x0.85 (one constant)   Normal: HP x1.2
//                          Hard: HP x1.6, damage x1.3   Extreme: HP x1.3, damage x2.3, monster level +30
//   two players counted (0x3612AC == 2): HP x1.7, damage x1.4, XP rating x3.5 - the three-player mod
//                          keeps that count at 2, so three players get the two-player factors
// and computes, with the level's monster level L (Levels tab) and its formula set (R = random roll):
//   set 1 (act 1): HP R(12-17) x (L + 3.33) x 0.375, damage max (L + 4) x 1.1, armour (L + 9) x 0.8,
//                  XP (L + 3) x 7, attack (L + 4) x 2.42 + 3
//   set 2 (act 2): R(12-19) x (L + 10.3) x 0.44, (L + 8) x 1.925, (L + 14) x 1, (L + 14) x 8.5, (L + 8) x 3.575
//   set 3 (act 3): R(15-22) x (L + 15) x 0.4, (L + 12) x 2.75, (L + 20) x 1.2, (L + 21) x 9, (L + 12) x 3.75
//   set 4 (act 4): R(15-22) x (L + 35.3) x 0.35, (L + 15) x 2.42, (L + 35) x 1.4, (L + 25) x 9.5, (L + 15) x 3.52
//   each times its multiplier; damage min is 1. A placed creature's own properties (hpscale=,
//   damagescale= ...) apply after that.
// Fixed-HP bosses multiply their own hit points (a constant in their constructor) by the boss factor
// 0x24B1B0: Easy x0.7, Normal x1.0, Hard x1.3, Extreme x1.8, and x1.7 with two players counted.
//
// Every factor and multiplier is a float constant in the code, built by "lui at, hi (ori at, at, lo)"
// and moved to the FPU by mtc1. Only constants used for nothing else are edited (Da2MonsterData lists
// them; a 2.0 the compiler loaded once for two arguments stays read-only). A constant without an ori
// keeps only the upper half of the float, so an edit is rounded to the nearest value it can hold
// (2.0, 1.5, 6.0 ... are exact; 900 boss HP is exact, 905 becomes 904).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>A float constant built by lui (and ori) in the code.</summary>
public sealed record Da2Constant(uint Lui, uint Ori)
{
    /// <summary>Without an ori only the upper 16 bits of the float can change.</summary>
    public bool FullPrecision => Ori != 0;
}

/// <summary>A creature class (or one variant of it) and its five setStats multipliers.</summary>
public sealed class Da2Creature
{
    public static readonly string[] Multipliers = { "HP", "Armour", "Damage", "Attack", "XP" };

    public string Class { get; init; } = "";
    public IReadOnlyList<string> Names { get; init; } = Array.Empty<string>();
    public int Variant { get; init; }
    public int Variants { get; init; }
    public IReadOnlyList<uint> Calls { get; init; } = Array.Empty<uint>();

    /// <summary>The five multipliers as the game uses them (edited values for the editable ones).</summary>
    public float[] Values { get; init; } = new float[5];

    /// <summary>The constant behind each multiplier, null when it is computed or shared (read-only).</summary>
    public Da2Constant?[] Constants { get; init; } = new Da2Constant?[5];
}

/// <summary>A boss with fixed hit points (scaled by the boss factor only).</summary>
public sealed record Da2Boss(string Class, string Note, Da2Constant Constant, float Hp);

/// <summary>The difficulty and two-player factors of setStats 0x24B250 and the boss factor 0x24B1B0.</summary>
public sealed class Da2Difficulty
{
    public float EasyHpDamage { get; set; }
    public float NormalHp { get; set; }
    public float HardHp { get; set; }
    public float HardDamage { get; set; }
    public float ExtremeHp { get; set; }
    public float ExtremeDamage { get; set; }
    public float ExtremeLevel { get; set; }
    public float TwoPlayerHp { get; set; }
    public float TwoPlayerDamage { get; set; }
    public float TwoPlayerXp { get; set; }
    public float BossEasy { get; set; }
    public float BossNormal { get; set; }
    public float BossHard { get; set; }
    public float BossExtreme { get; set; }
    public float BossTwoPlayer { get; set; }

    /// <summary>HP factor of difficulty 0-3 (Easy .. Extreme).</summary>
    public float Hp(int d) => d switch { 0 => EasyHpDamage, 1 => NormalHp, 2 => HardHp, _ => ExtremeHp };
    public float Damage(int d) => d switch { 0 => EasyHpDamage, 1 => 1f, 2 => HardDamage, _ => ExtremeDamage };
    public float Level(int d) => d == 3 ? ExtremeLevel : 0f;
    public float Boss(int d) => d switch { 0 => BossEasy, 1 => BossNormal, 2 => BossHard, _ => BossExtreme };

    public Da2Difficulty Clone() => (Da2Difficulty)MemberwiseClone();
}

/// <summary>Predicted stats of a creature in a level (no placement properties).</summary>
public readonly record struct Da2CreatureStats(int HpMin, int HpMax, int DamageMax, int Armour, int Attack, float XpRating);

public sealed partial class Da2Executable
{
    public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Extreme" };
    public const float MinFactor = 0.01f, MaxFactor = 100f, MaxLevelBonus = 200f;
    public const float MaxMultiplier = 1000f, MaxBossHp = 30000f;

    /// <summary>The difficulty constants: what, lui address, ori address (0 = none).</summary>
    internal static readonly (string Name, uint Lui, uint Ori)[] DifficultyConstants =
    {
        (nameof(Da2Difficulty.EasyHpDamage), 0x0024B2EC, 0x0024B2F0),
        (nameof(Da2Difficulty.NormalHp), 0x0024B308, 0x0024B30C),
        (nameof(Da2Difficulty.HardHp), 0x0024B31C, 0x0024B320),
        (nameof(Da2Difficulty.HardDamage), 0x0024B328, 0x0024B32C),
        (nameof(Da2Difficulty.ExtremeHp), 0x0024B340, 0x0024B344),
        (nameof(Da2Difficulty.ExtremeDamage), 0x0024B34C, 0x0024B350),
        (nameof(Da2Difficulty.ExtremeLevel), 0x0024B358, 0),
        (nameof(Da2Difficulty.TwoPlayerHp), 0x0024B380, 0x0024B384),
        (nameof(Da2Difficulty.TwoPlayerXp), 0x0024B38C, 0),
        (nameof(Da2Difficulty.TwoPlayerDamage), 0x0024B394, 0x0024B398),
        (nameof(Da2Difficulty.BossNormal), 0x0024B1BC, 0),
        (nameof(Da2Difficulty.BossEasy), 0x0024B1F4, 0x0024B1F8),
        (nameof(Da2Difficulty.BossHard), 0x0024B208, 0x0024B20C),
        (nameof(Da2Difficulty.BossExtreme), 0x0024B21C, 0x0024B220),
        (nameof(Da2Difficulty.BossTwoPlayer), 0x0024B234, 0x0024B238),
    };

    // ---- constants ----

    /// <summary>True when the words are "lui at, x" (+ "ori at, at, y").</summary>
    private bool IsConstant(uint lui, uint ori)
    {
        var w = Word(lui);
        if (w >> 26 != 0x0F) return false;
        if (ori == 0) return true;
        var o = Word(ori);
        var r = (w >> 16) & 31;
        return o >> 26 == 0x0D && ((o >> 16) & 31) == r && ((o >> 21) & 31) == r;
    }

    /// <summary>The float a constant builds.</summary>
    public float ConstantValue(Da2Constant c) => ConstantValue(c.Lui, c.Ori);

    private float ConstantValue(uint lui, uint ori)
    {
        if (!IsConstant(lui, ori)) throw new InvalidOperationException($"No float constant at 0x{lui:X6}.");
        var bits = (Word(lui) & 0xFFFF) << 16 | (ori != 0 ? Word(ori) & 0xFFFF : 0);
        return BitConverter.Int32BitsToSingle((int)bits);
    }

    /// <summary>The value a constant without an ori would hold for <paramref name="value"/> (nearest upper half).</summary>
    public static float Representable(float value, bool fullPrecision)
    {
        if (fullPrecision) return value;
        var bits = (uint)BitConverter.SingleToInt32Bits(value);
        var hi = (bits + 0x8000) >> 16;
        return BitConverter.Int32BitsToSingle((int)(hi << 16));
    }

    public void SetConstant(Da2Constant c, float value) => SetConstant(c.Lui, c.Ori, value);

    private void SetConstant(uint lui, uint ori, float value)
    {
        if (!IsConstant(lui, ori)) throw new InvalidOperationException($"No float constant at 0x{lui:X6}.");
        var bits = (uint)BitConverter.SingleToInt32Bits(Representable(value, ori != 0));
        SetWord(lui, (Word(lui) & 0xFFFF0000) | (bits >> 16));
        if (ori != 0) SetWord(ori, (Word(ori) & 0xFFFF0000) | (bits & 0xFFFF));
    }

    // ---- difficulty ----

    public Da2Difficulty GetDifficulty()
    {
        var d = new Da2Difficulty();
        foreach (var (name, lui, ori) in DifficultyConstants)
            typeof(Da2Difficulty).GetProperty(name)!.SetValue(d, ConstantValue(lui, ori));
        return d;
    }

    /// <summary>Writes the factors (each checked first; nothing is written if one is out of range).</summary>
    public void SetDifficulty(Da2Difficulty d)
    {
        foreach (var (name, _, _) in DifficultyConstants)
        {
            var v = (float)typeof(Da2Difficulty).GetProperty(name)!.GetValue(d)!;
            var level = name == nameof(Da2Difficulty.ExtremeLevel);
            if (float.IsNaN(v) || (level ? v < 0 || v > MaxLevelBonus : v < MinFactor || v > MaxFactor))
                throw new ArgumentException(level
                    ? $"The Extreme monster level bonus must be from 0 to {MaxLevelBonus:0}."
                    : $"{DescribeFactor(name)} must be from {MinFactor} to {MaxFactor:0}.");
        }

        foreach (var (name, lui, ori) in DifficultyConstants)
            SetConstant(lui, ori, (float)typeof(Da2Difficulty).GetProperty(name)!.GetValue(d)!);
    }

    public static string DescribeFactor(string property) => property switch
    {
        nameof(Da2Difficulty.EasyHpDamage) => "Easy HP and damage",
        nameof(Da2Difficulty.NormalHp) => "Normal HP",
        nameof(Da2Difficulty.HardHp) => "Hard HP",
        nameof(Da2Difficulty.HardDamage) => "Hard damage",
        nameof(Da2Difficulty.ExtremeHp) => "Extreme HP",
        nameof(Da2Difficulty.ExtremeDamage) => "Extreme damage",
        nameof(Da2Difficulty.ExtremeLevel) => "Extreme monster level bonus",
        nameof(Da2Difficulty.TwoPlayerHp) => "Two-player HP",
        nameof(Da2Difficulty.TwoPlayerDamage) => "Two-player damage",
        nameof(Da2Difficulty.TwoPlayerXp) => "Two-player XP rating",
        nameof(Da2Difficulty.BossEasy) => "Boss HP on Easy",
        nameof(Da2Difficulty.BossNormal) => "Boss HP on Normal",
        nameof(Da2Difficulty.BossHard) => "Boss HP on Hard",
        nameof(Da2Difficulty.BossExtreme) => "Boss HP on Extreme",
        nameof(Da2Difficulty.BossTwoPlayer) => "Two-player boss HP",
        _ => property
    };

    /// <summary>Whether a difficulty value is held with full precision (an ori) or rounded (lui only).</summary>
    public static bool DifficultyFullPrecision(string property) =>
        DifficultyConstants.First(c => c.Name == property).Ori != 0;

    // ---- creatures ----

    /// <summary>Every creature class and variant with its multipliers (editable ones as they are now).</summary>
    public List<Da2Creature> GetCreatures()
    {
        var constants = Da2MonsterData.Constants.Select(c => new Da2Constant(c.Lui, c.Ori)).ToArray();
        return Da2MonsterData.Rows.Select(r =>
        {
            var cells = r.Cells.Select(i => i < 0 ? null : constants[i]).ToArray();
            var values = r.Values.Select((v, i) => cells[i] is { } c ? ConstantValue(c) : v).ToArray();
            return new Da2Creature
            {
                Class = r.Class, Names = r.Names, Variant = r.Variant, Variants = r.Variants, Calls = r.Calls,
                Values = values, Constants = cells
            };
        }).ToList();
    }

    public void SetCreatureMultiplier(Da2Constant constant, float value)
    {
        if (!Da2MonsterData.Constants.Any(c => c.Lui == constant.Lui && c.Ori == constant.Ori))
            throw new ArgumentException("Not a creature multiplier WorldExplorer knows.");
        if (float.IsNaN(value) || value < 0 || value > MaxMultiplier)
            throw new ArgumentException($"Creature multipliers must be from 0 to {MaxMultiplier:0}.");
        SetConstant(constant, value);
    }

    public List<Da2Boss> GetBosses() =>
        Da2MonsterData.BossHp.Select(b =>
        {
            var c = new Da2Constant(b.Lui, b.Ori);
            return new Da2Boss(b.Class, b.Note, c, ConstantValue(c));
        }).ToList();

    public void SetBossHp(Da2Constant constant, float hp)
    {
        if (!Da2MonsterData.BossHp.Any(b => b.Lui == constant.Lui && b.Ori == constant.Ori))
            throw new ArgumentException("Not a boss WorldExplorer knows.");
        if (float.IsNaN(hp) || hp < 1 || hp > MaxBossHp)
            throw new ArgumentException($"Boss hit points must be from 1 to {MaxBossHp:N0} (the game keeps HP in 16 bits).");
        SetConstant(constant, hp);
    }

    /// <summary>
    /// The stats setStats gives a creature with these multipliers in a level of monster level
    /// <paramref name="level"/> and formula set <paramref name="formulaSet"/>, on difficulty
    /// <paramref name="difficulty"/> (0-3), with one or two players counted.
    /// </summary>
    public static Da2CreatureStats Predict(float[] m, float level, int formulaSet, int difficulty, bool twoPlayers, Da2Difficulty d)
    {
        var hpX = m[0] * d.Hp(difficulty) * (twoPlayers ? d.TwoPlayerHp : 1f);
        var dmgX = m[2] * d.Damage(difficulty) * (twoPlayers ? d.TwoPlayerDamage : 1f);
        var xpX = m[4] * (twoPlayers ? d.TwoPlayerXp : 1f);
        var l = level + d.Level(difficulty);
        float rLo, rHi, hp, dmg, armour, xp, attack;
        switch (formulaSet)
        {
            case 2:
                (rLo, rHi) = (12, 19);
                hp = (l + 10.3f) * 0.44f; dmg = (l + 8) * 1.925f; armour = l + 14; xp = (l + 14) * 8.5f; attack = (l + 8) * 3.575f;
                break;
            case 3:
                (rLo, rHi) = (15, 22);
                hp = (l + 15) * 0.4f; dmg = (l + 12) * 2.75f; armour = (l + 20) * 1.2f; xp = (l + 21) * 9; attack = (l + 12) * 3.75f;
                break;
            case 4:
                (rLo, rHi) = (15, 22);
                hp = (l + 35.3f) * 0.35f; dmg = (l + 15) * 2.42f; armour = (l + 35) * 1.4f; xp = (l + 25) * 9.5f; attack = (l + 15) * 3.52f;
                break;
            default:
                (rLo, rHi) = (12, 17);
                hp = (l + 3.33f) * 0.375f; dmg = (l + 4) * 1.1f; armour = (l + 9) * 0.8f; xp = (l + 3) * 7; attack = (l + 4) * 2.42f + 3;
                break;
        }

        static int I(float v) => (int)Math.Clamp(MathF.Truncate(v), -32768, 32767);
        return new Da2CreatureStats(Math.Max(1, I(rLo * hp * hpX)), Math.Max(1, I(rHi * hp * hpX)), Math.Max(2, I(dmg * dmgX)),
            I(armour * m[1]), I(attack * m[3]), xp * xpX);
    }
}
