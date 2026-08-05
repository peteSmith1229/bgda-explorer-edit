// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaMonsters.cs
//
// Per-monster HP editing in SLES_506.72 (PAL).
//
// HOW ENEMY HP WORKS (reverse-engineered and confirmed in-game)
// ------------------------------------------------------------
// Every monster is created through a name-keyed factory. Each monster's constructor
// then calls a shared stat routine at VA 0x0010D7C0, which computes:
//
//     HP = (rand%6 + 12) x ((f07 + 2.33) x 0.3) x (f12 x difficulty)
//
// `f12` is the PER-MONSTER HP MULTIPLIER, set by the constructor immediately before
// that call. Patching it changes that enemy type's HP for every playthrough - no save
// editing required.
//
// The constructor also passes a default HP (`li t0, N`), but the stat routine
// OVERWRITES it for most monsters, so that value is not a reliable edit point.
// Multiplier editing is.
//
// CONFIRMED IN-GAME
//   Slime       f12 1.5 -> 20.0  => ~300 HP  (predicted 313)
//   Kobold      f12 0.5 -> 20.0  => very tough  (predicted ~218-309)
//   SmallSpider f12 2.0 -> 0.5   => dies in ~2 hits  (predicted ~5-8)
//
// Enemy HP is a 16-bit integer at entity +0x15A. (The player's HP, by contrast, is a
// 32-bit float - enemies do not use the player's layout.)
//
// THREE FORMS APPEAR, and only the first is safely patchable here:
//   Immediate  `lui at, X; mtc1 at, f12`  -> patch the lui immediate (upper 16 bits
//                                            of the float)
//   Global     `lwc1 f12, imm(gp)`        -> the value lives in initialised data;
//                                            exposed read-only (shared by several
//                                            monsters, so editing has side effects)
//   Multi      constructor calls the stat routine more than once (runtime-selected
//                                            branches, e.g. GiantRat has four)
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JetBlackEngineLib.Data.Executable;

public enum MonsterHpKind
{
    /// <summary>f12 is a literal immediate - directly editable.</summary>
    Immediate,
    /// <summary>f12 is read from a gp-relative global - shown read-only.</summary>
    Global,
    /// <summary>Constructor has several stat branches - shown read-only.</summary>
    MultiBranch,
    /// <summary>
    /// Constructor writes HP directly to entity +0x15A as a literal, OVERRIDING the
    /// stat routine. Used by bosses. The multiplier is irrelevant for these; edit
    /// <see cref="MonsterHp.DirectHp"/> instead.
    /// </summary>
    DirectHp
}

public sealed class MonsterHp
{
    public string Name { get; init; } = "";
    public MonsterHpKind Kind { get; init; }
    /// <summary>The HP multiplier (f12). Null when it could not be resolved.</summary>
    public float? Multiplier { get; init; }
    /// <summary>File offset of the `lui at, imm` instruction (Immediate kind only).</summary>
    public int PatchFileOffset { get; init; }
    /// <summary>Constructor address (VA), for diagnostics.</summary>
    public uint ConstructorVa { get; init; }
    /// <summary>Number of stat-routine calls in the constructor.</summary>
    public int BranchCount { get; init; }
    /// <summary>Literal HP written straight to +0x15A (DirectHp kind only).</summary>
    public int? DirectHp { get; init; }
    /// <summary>
    /// Elemental resistance bitmask written to entity +0x3C, or null if the
    /// constructor sets none. See <see cref="BgdaMonsters.DescribeResistances"/>.
    /// </summary>
    public uint? ResistMask { get; init; }

    public bool IsEditable =>
        Kind == MonsterHpKind.Immediate || Kind == MonsterHpKind.DirectHp;
}

public class BgdaMonsters
{
    // ── SLES_506.72 layout ──────────────────────────────────────────────────────
    private const uint VaToOffset   = 0xFF000;      // VA = fileOffset + 0xFF000
    private const uint StrcmpVa     = 0x002266A0;
    private const uint OpNewVa      = 0x0012EC10;
    private const uint StatRoutineVa = 0x0010D7C0;
    private const uint BaseCtorVa    = 0x0010D2F0;
    private const uint EntityHpOffset = 0x015A;   // entity +0x15A = current HP (u16)
    private const uint GpValue      = 0x00329F70;

    private const uint FactoryLoVa  = 0x00120000;
    private const uint FactoryHiVa  = 0x00130000;
    private const uint CtorLoVa     = 0x00100000;
    private const uint CtorHiVa     = 0x00300000;

    private const uint Mtc1AtF12    = 0x44816000;   // mtc1 at, f12
    private const uint LuiAtMask    = 0xFFFF0000;
    private const uint LuiAt        = 0x3C010000;   // lui at, imm
    private const uint Lwc1F12Gp    = 0xC78C0000;   // lwc1 f12, imm(gp)
    private const uint AddiuA1Mask  = 0xFFFF0000;
    private const uint AddiuA1      = 0x24A50000;   // addiu a1, imm
    private const uint LuiA1        = 0x3C050000;   // lui a1, imm

    // ── elemental resistance (entity +0x3C) ─────────────────────────────────────
    // The damage dispatcher at 0x0011F838 tests the attack's damage type against this
    // mask. Each element has a PAIR of bits - one halves damage, one nullifies it:
    //
    //     andi v0, s5, <attack type>     ; s5 = the attack's element
    //     lw   v0, 0x3C(s1)              ; the defender's resistance mask
    //     andi v1, v0, <resist bit>      ; -> damage >> 1   (0x001207A4)
    //     andi v0, v0, <immune bit>      ; -> damage  = 0   (0x001207AC movn s4, zero, t0)
    //
    // CONFIRMED IN-GAME: FireElemental has 0x8040; bit 0x8000 is the immune bit for
    // attack type 0x0020. A melee hit passed damage 121, burning hands passed 0.
    //
    // Confirmed by in-game A/B tests (melee vs elemental against a creature whose mask
    // is known):
    //   0x0020 = fire  - FireElemental (mask 0x8040): melee 121, Burning Hands 0
    //   0x0400 = cold  - creature with mask 0x2100:   melee   3, Snowblind      0
    //   0x0080 = lightning - Wisp (mask 0x1000):      melee   2, Lightning Bolt 0
    //   0x0004 / 0x0008 = the two bladed physical types (slashing + piercing).
    //   0x0001 = blunt  - club on a Skeleton: s5 = 0x1001, full damage 6
    //   0x0008 = bladed - dagger, spear AND sword all report s5 = 0x1008 and are
    //     halved by the Skeleton (mask 0x0105 carries resist bit 0x0004).
    //     The game does NOT distinguish slashing from piercing - all edged
    //     weapons share one type.
    //   0x0004 = ranged/arrows - bow on a Skeleton: s5 = 0x0004, damage halved.
    //   The 0x1000 bit seen on melee hits (0x1001, 0x1008) is a MELEE marker: a
    //     bow shot reports a bare 0x0004 with no such flag. It is not part of
    //     any resistance pair.
    // Remaining types are unlabelled and reported by number rather than guessed at.
    private static readonly (uint Attack, uint Immune, uint Resist)[] ResistPairs =
    {
        (0x0001u, 0x000020u, 0x000010u),   // blunt (confirmed)
        (0x0004u, 0x000002u, 0x000001u),   // ranged / arrows (confirmed)
        (0x0008u, 0x000008u, 0x000004u),   // bladed/edged weapons (confirmed)
        (0x0020u, 0x008000u, 0x004000u),   // fire (confirmed)
        (0x0080u, 0x001000u, 0x000800u),   // lightning (confirmed)
        (0x0200u, 0x000400u, 0x000200u),
        (0x0400u, 0x000100u, 0x000080u),   // cold (confirmed)
        (0x40000u, 0x080000u, 0x040000u)
    };
    private const uint GenericImmune = 0x020000u;
    private const uint GenericResist = 0x010000u;
    private const uint FireAttackType = 0x0020u;
    private const uint ColdAttackType = 0x0400u;
    private const uint LightningAttackType = 0x0080u;
    private const uint BluntAttackType = 0x0001u;
    private const uint BladedAttackType = 0x0008u;
    private const uint RangedAttackType = 0x0004u;

    /// <summary>Human-readable description of a +0x3C resistance mask.</summary>
    public static string DescribeResistances(uint? mask)
    {
        if (mask is null || mask.Value == 0) return "";
        var v = mask.Value;
        var parts = new List<string>();
        var known = 0u;
        foreach (var (attack, immune, resist) in ResistPairs)
        {
            var label = attack == FireAttackType ? "fire"
                      : attack == ColdAttackType ? "cold"
                      : attack == LightningAttackType ? "lightning"
                      : attack == BluntAttackType ? "blunt"
                      : attack == BladedAttackType ? "bladed"
                      : attack == RangedAttackType ? "ranged"
                      : $"type 0x{attack:X}";
            if ((v & immune) != 0) { parts.Add($"immune {label}"); known |= immune; }
            else if ((v & resist) != 0) { parts.Add($"resist {label}"); known |= resist; }
        }
        if ((v & GenericImmune) != 0) { parts.Add("immune magic"); known |= GenericImmune; }
        else if ((v & GenericResist) != 0) { parts.Add("resist magic"); known |= GenericResist; }

        var leftover = v & ~known;
        if (leftover != 0) parts.Add($"+unmapped 0x{leftover:X}");
        return string.Join(", ", parts);
    }

    public const float MinMultiplier = 0.01f;
    public const float MaxMultiplier = 1000f;

    private readonly byte[] _data;
    public string FilePath { get; }

    private BgdaMonsters(byte[] data, string path) { _data = data; FilePath = path; }

    public static BgdaMonsters Open(string path) =>
        new(File.ReadAllBytes(path), path);

    private static int VaToOff(uint va) => (int)(va - VaToOffset);
    private static uint OffToVa(int off) => (uint)off + VaToOffset;

    private static uint JalWord(uint targetVa) =>
        0x0C000000u | ((targetVa >> 2) & 0x03FFFFFFu);

    private uint Word(int off) =>
        off < 0 || off + 4 > _data.Length ? 0 : BitConverter.ToUInt32(_data, off);

    private static uint? JalTarget(uint word)
    {
        var op = word >> 26;
        if (op != 0x02 && op != 0x03) return null;
        return (word & 0x03FFFFFFu) << 2;
    }

    private string ReadAscii(int off, int limit = 48)
    {
        if (off < 0 || off >= _data.Length) return "";
        var end = off;
        while (end < _data.Length && end < off + limit && _data[end] != 0) end++;
        return Encoding.ASCII.GetString(_data, off, end - off);
    }

    /// <summary>Walks the factory's strcmp chain, pairing names with constructors.</summary>
    private List<(string Name, uint CtorVa)> ParseFactory()
    {
        var strcmp = JalWord(StrcmpVa);
        var result = new List<(string Name, uint CtorVa)>();
        var seen = new HashSet<(string, uint)>();

        var lo = VaToOff(FactoryLoVa);
        var hi = VaToOff(FactoryHiVa);
        uint? lastLuiHi = null;

        for (var off = lo; off + 8 <= hi && off + 8 <= _data.Length; off += 4)
        {
            var w = Word(off);
            // the `lui a1, hi` forming a name pointer sits in the PREVIOUS candidate's
            // branch delay slot, so track the most recent one rather than looking back
            // a fixed distance.
            if ((w & 0xFFFF0000) == LuiA1) { lastLuiHi = w & 0xFFFF; continue; }
            if (w != strcmp || lastLuiHi is null) continue;

            var delay = Word(off + 4);
            if ((delay & AddiuA1Mask) != AddiuA1) continue;

            var loHalf = (int)(delay & 0xFFFF);
            if ((loHalf & 0x8000) != 0) loHalf -= 0x10000;      // addiu sign-extends
            var nameVa = (uint)((lastLuiHi.Value << 16) + loHalf);
            var nameOff = VaToOff(nameVa);
            if (nameOff < 0 || nameOff >= _data.Length) continue;

            var name = ReadAscii(nameOff);
            if (name.Length < 2) continue;

            uint? ctor = null;
            for (var fwd = 1; fwd < 40; fwd++)
            {
                var fw = Word(off + fwd * 4);
                if (fw == strcmp) break;
                var t = JalTarget(fw);
                if (t is null || t == OpNewVa || t == StrcmpVa) continue;
                if (t >= CtorLoVa && t < CtorHiVa) { ctor = t; break; }
            }
            if (ctor is null) continue;
            if (seen.Add((name, ctor.Value))) result.Add((name, ctor.Value));
        }
        result.Sort((a, b) => a.CtorVa.CompareTo(b.CtorVa));
        return result;
    }

    /// <summary>
    /// Extracts each monster's HP multiplier. Every constructor's scan is bounded by
    /// the next constructor's start, so it cannot read into a neighbouring function -
    /// without that bound two monsters can report the same patch offset.
    /// </summary>
    public List<MonsterHp> GetMonsters()
    {
        var factory = ParseFactory();
        var jalStat = JalWord(StatRoutineVa);
        var list = new List<MonsterHp>();

        for (var i = 0; i < factory.Count; i++)
        {
            var (name, ctorVa) = factory[i];
            var start = VaToOff(ctorVa);
            var end = i + 1 < factory.Count
                ? VaToOff(factory[i + 1].CtorVa)
                : start + 0x1000;
            end = Math.Min(end, _data.Length - 4);

            MonsterHp? pending = null;
            var calls = 0;
            uint? resistMask = null;

            // `sw rt, 0x3C(rs)` - the elemental resistance mask
            for (var a = start; a + 4 <= end; a += 4)
            {
                var sw = Word(a);
                if ((sw >> 26) != 0x2B || (sw & 0xFFFF) != 0x003C) continue;
                var srt = (sw >> 16) & 0x1F;
                uint acc = 0; var got = false;
                for (var b = 1; b <= 7; b++)
                {
                    if (a - b * 4 < start) break;
                    var pw = Word(a - b * 4);
                    var pop = pw >> 26;
                    if (((pw >> 16) & 0x1F) != srt) continue;
                    if (pop == 0x09 && ((pw >> 21) & 0x1F) == 0) { acc |= pw & 0xFFFF; got = true; break; }
                    if (pop == 0x0D) { acc |= pw & 0xFFFF; got = true; }
                    if (pop == 0x0F) { acc |= (pw & 0xFFFF) << 16; got = true; break; }
                }
                if (got) { resistMask = acc; break; }
            }

            for (var a = start; a + 4 <= end; a += 4)
            {
                var w = Word(a);
                if (w == jalStat)
                {
                    calls++;
                    if (pending is not null) { list.Add(pending); pending = null; }
                }
                else if (w == Mtc1AtF12)
                {
                    for (var b = 1; b <= 4; b++)
                    {
                        if (a - b * 4 < start) break;
                        var pw = Word(a - b * 4);
                        if ((pw & LuiAtMask) != LuiAt) continue;
                        pending = new MonsterHp
                        {
                            Name = name,
                            ResistMask = resistMask,
                            Kind = MonsterHpKind.Immediate,
                            Multiplier = BitConverter.ToSingle(
                                BitConverter.GetBytes((pw & 0xFFFF) << 16), 0),
                            PatchFileOffset = a - b * 4,
                            ConstructorVa = ctorVa
                        };
                        break;
                    }
                }
                else if ((w & 0xFFFF0000) == Lwc1F12Gp)
                {
                    var disp = (short)(w & 0xFFFF);
                    var g = (uint)((int)GpValue + disp);
                    var go = VaToOff(g);
                    pending = new MonsterHp
                    {
                        Name = name,
                        ResistMask = resistMask,
                        Kind = MonsterHpKind.Global,
                        Multiplier = go >= 0 && go + 4 <= _data.Length
                            ? BitConverter.ToSingle(_data, go) : null,
                        PatchFileOffset = go,
                        ConstructorVa = ctorVa
                    };
                }
            }

            // A constructor may also write HP directly to +0x15A after the stat call,
            // overriding it entirely (bosses do this). Detect `sh rt, 0x15A(rs)` and
            // the `addiu rt, zero, imm` that loaded the value.
            var sawCall = false;
            for (var a = start; a + 4 <= end; a += 4)
            {
                var w = Word(a);
                if (w == jalStat || w == JalWord(BaseCtorVa)) { sawCall = true; continue; }
                if ((w >> 26) != 0x29 || (w & 0xFFFF) != EntityHpOffset) continue;
                if (!sawCall) break;
                var rt = (w >> 16) & 0x1F;
                for (var b = 1; b <= 9; b++)
                {
                    if (a - b * 4 < start) break;
                    var pw = Word(a - b * 4);
                    if ((pw >> 26) != 0x09) continue;                 // addiu
                    if (((pw >> 16) & 0x1F) != rt) continue;          // same register
                    if (((pw >> 21) & 0x1F) != 0) continue;           // from zero => li
                    list.Add(new MonsterHp
                    {
                        Name = name,
                        Kind = MonsterHpKind.DirectHp,
                        ResistMask = resistMask,
                        DirectHp = (int)(pw & 0xFFFF),
                        PatchFileOffset = a - b * 4,
                        ConstructorVa = ctorVa
                    });
                    break;
                }
                break;
            }

            if (calls > 1)
                list.Add(new MonsterHp
                {
                    Name = name,
                    Kind = MonsterHpKind.MultiBranch,
                    ResistMask = resistMask,
                    ConstructorVa = ctorVa,
                    BranchCount = calls
                });
        }
        return list;
    }

    /// <summary>
    /// Sets a monster's HP multiplier. Only <see cref="MonsterHpKind.Immediate"/>
    /// entries can be edited; the instruction is verified before writing.
    ///
    /// The immediate holds only the upper 16 bits of the float, so the value is
    /// quantised - <paramref name="applied"/> returns what was actually stored.
    /// </summary>
    public void SetMultiplier(MonsterHp monster, float multiplier, out float applied)
    {
        if (!monster.IsEditable)
            throw new ArgumentException(
                $"{monster.Name}: multiplier is not a literal ({monster.Kind}); " +
                "it cannot be edited safely here.");
        if (multiplier < MinMultiplier || multiplier > MaxMultiplier)
            throw new ArgumentException(
                $"Multiplier must be {MinMultiplier}-{MaxMultiplier}.");

        var bits = BitConverter.ToUInt32(BitConverter.GetBytes(multiplier), 0);
        bits &= 0xFFFF0000;                                  // quantise to the immediate
        applied = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

        var off = monster.PatchFileOffset;
        if (off < 0 || off + 4 > _data.Length)
            throw new ArgumentException($"{monster.Name}: patch offset outside file.");

        var old = Word(off);
        if ((old & LuiAtMask) != LuiAt)
            throw new ArgumentException(
                $"{monster.Name}: expected `lui at, imm` at file offset 0x{off:X6} " +
                $"but found 0x{old:X8}. Refusing to write.");

        BitConverter.GetBytes(LuiAt | (bits >> 16)).CopyTo(_data, off);
    }

    /// <summary>
    /// Sets a boss's literal HP (<see cref="MonsterHpKind.DirectHp"/>). The target
    /// instruction is verified to be `addiu rt, zero, imm` before writing.
    /// </summary>
    public void SetDirectHp(MonsterHp monster, int hp)
    {
        if (monster.Kind != MonsterHpKind.DirectHp)
            throw new ArgumentException($"{monster.Name} does not write HP directly.");
        if (hp < 1 || hp > 0xFFFF)
            throw new ArgumentException("HP must be 1-65535.");

        var off = monster.PatchFileOffset;
        if (off < 0 || off + 4 > _data.Length)
            throw new ArgumentException($"{monster.Name}: patch offset outside file.");

        var old = Word(off);
        if ((old >> 26) != 0x09 || ((old >> 21) & 0x1F) != 0)
            throw new ArgumentException(
                $"{monster.Name}: expected `addiu rt, zero, imm` at file offset " +
                $"0x{off:X6} but found 0x{old:X8}. Refusing to write.");

        var rebuilt = (old & 0xFFFF0000) | (uint)(hp & 0xFFFF);
        BitConverter.GetBytes(rebuilt).CopyTo(_data, off);
    }

    public void Save(string path) => File.WriteAllBytes(path, _data);
}
