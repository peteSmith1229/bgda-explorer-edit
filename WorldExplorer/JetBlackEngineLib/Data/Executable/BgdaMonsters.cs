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
