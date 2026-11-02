// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/Da2Executable.Loot.cs
//
// WHAT A CREATURE DROPS. Every creature's death code calls the drop routine 0x264970 with its own
// gold scale, pile chance, quality level and drop chance (Da2LootData lists the 109 calls; bosses
// make several). Each is a float constant (lui, ori) in the death code: editable when it is used for
// nothing else; most gold scales are the throw speed 2.0 loaded once for both arguments, and a few
// values are shared by two arguments (1.6 gold and quality, 1.0 pile and drop chance), so those cells
// are read-only. How the routine uses them, and the rules it adds, are in Da2Executable.Rules.cs.
//
// THE QUALITY TABLE, 0x3141B8: 12 rows of three floats, the chance of a poor (factor 0.5), plain (1)
// and good (3) roll for quality levels 0..11 (a creature's level is clamped to 0..9 and the routine
// interpolates between its row and the next). Only the first two columns are read (good = the rest);
// WorldExplorer keeps the third equal to 1 - poor - plain.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>One drop call of a creature's death code and its four loot values.</summary>
public sealed class Da2Drop
{
    public static readonly string[] Arguments = { "Gold ×", "Pile chance", "Quality", "Drop chance" };
    public static readonly float[] MaxValues = { 100f, 10f, 9f, 10f };

    public uint Call { get; init; }
    public string Creature { get; init; } = "";
    public IReadOnlyList<string> Names { get; init; } = Array.Empty<string>();

    /// <summary>Which routine / drop of the creature it is, when it has several ("drop 2 of 3").</summary>
    public string Note { get; init; } = "";

    /// <summary>Gold scale, pile chance, quality level, drop chance (NaN where the code computes it).</summary>
    public float[] Values { get; init; } = new float[4];

    /// <summary>The constant behind each value, null when it is shared or computed (read-only).</summary>
    public Da2Constant?[] Constants { get; init; } = new Da2Constant?[4];
}

public sealed partial class Da2Executable
{
    public const uint QualityTable = 0x003141B8;
    public const int QualityRows = 12;

    public List<Da2Drop> GetDrops()
    {
        var constants = Da2LootData.Constants.Select(c => new Da2Constant(c.Lui, c.Ori)).ToArray();
        return Da2LootData.Sites.Select(s =>
        {
            var cells = s.Cells.Select(i => i < 0 ? null : constants[i]).ToArray();
            return new Da2Drop
            {
                Call = s.Call, Creature = s.Creature, Names = s.Names, Note = s.Note,
                Values = s.Values.Select((v, i) => cells[i] is { } c ? ConstantValue(c) : v).ToArray(),
                Constants = cells
            };
        }).ToList();
    }

    /// <summary>The argument (0-3) a drop constant feeds, or -1 when it isn't one WorldExplorer knows.</summary>
    public static int DropArgument(Da2Constant constant)
    {
        foreach (var c in Da2LootData.Constants)
        {
            if (c.Lui == constant.Lui && c.Ori == constant.Ori) return c.Argument;
        }

        return -1;
    }

    public void SetDropValue(Da2Constant constant, float value)
    {
        var argument = DropArgument(constant);
        if (argument < 0) throw new ArgumentException("Not a drop value WorldExplorer knows.");
        if (float.IsNaN(value) || value < 0 || value > Da2Drop.MaxValues[argument])
            throw new ArgumentException($"{Da2Drop.Arguments[argument]} must be from 0 to {Da2Drop.MaxValues[argument]:0}.");
        SetConstant(constant, value);
    }

    // ---- quality table ----

    /// <summary>The chance of a poor, plain and good roll for each quality level 0..11.</summary>
    public float[][] GetQualityTable() =>
        Enumerable.Range(0, QualityRows).Select(r => Enumerable.Range(0, 3)
            .Select(c => F32(QualityTable + (uint)(12 * r + 4 * c))).ToArray()).ToArray();

    /// <summary>Sets the poor and plain chance of a row (0-1 each, together at most 1); good becomes the rest.</summary>
    public void SetQualityRow(int row, float poor, float plain)
    {
        if (row is < 0 or >= QualityRows) throw new ArgumentOutOfRangeException(nameof(row));
        if (float.IsNaN(poor) || float.IsNaN(plain) || poor < 0 || plain < 0 || poor + plain > 1.0001f)
            throw new ArgumentException($"Quality level {row}: the chances must be from 0 to 1 and add up to at most 1.");
        var a = QualityTable + (uint)(12 * row);
        // a row left as it is stays byte for byte the game's (its third column isn't always exactly 1 - poor - plain)
        if (BitConverter.SingleToInt32Bits(F32(a)) == BitConverter.SingleToInt32Bits(poor) &&
            BitConverter.SingleToInt32Bits(F32(a + 4)) == BitConverter.SingleToInt32Bits(plain)) return;
        var good = MathF.Max(0f, 1f - poor - plain);
        SetF32(a, poor);
        SetF32(a + 4, plain);
        SetF32(a + 8, good);
    }

    /// <summary>The chance of a poor, plain and good roll at a quality level as the drop routine works it out.</summary>
    public (float Poor, float Plain, float Good) QualityChances(float quality)
    {
        var q = Math.Clamp(quality, 0f, 9f);
        var row = (int)MathF.Truncate(q);
        var f = q - row;
        var t = GetQualityTable();
        var poor = t[row][0] * (1 - f) + t[row + 1][0] * f;
        var plain = t[row][1] * (1 - f) + t[row + 1][1] * f;
        return (poor, plain, MathF.Max(0, 1 - poor - plain));
    }

    /// <summary>
    /// The chance that a kill drops an item, and a potion, for a creature's drop chance (the treasure bonus
    /// of the players' gear not counted).
    /// </summary>
    public (float Item, float Potion) DropChances(float chance, bool twoPlayers)
    {
        var c = chance < 1f ? chance * GetSetting(Setting("loot.lowChance")) : chance;
        if (twoPlayers) c += c;
        return (Math.Clamp(c, 0f, 1f), Math.Clamp(c + c, 0f, 1f));
    }
}
