// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Scripting/ScriptRewardScanner.cs
//
// Scans a compiled script (script.scr) for reward calls — givePlayerGold,
// givePlayerExp, givePlayerItem — and reports each with enough location data to
// patch it in place: the file-relative offset of the pushed immediate (for
// gold/exp) or of the string-table entry (for items). In-place edits only, so
// jumps/offsets never need rebuilding: ints overwrite 4 bytes; item names must
// fit within the existing string's length (padded with NULs when shorter).
//
// The instruction walk mirrors ScrDecoder's arg-size table for the opcodes that
// matter and tracks pushes (opcode 0x27) with their immediate offsets. The BGDA
// script calling convention pushes the value args, then the arg-byte count, then
// CALL (0x7B) — so the editable argument is the second-from-top push.
// ═══════════════════════════════════════════════════════════════════════════════
using System.Text;

namespace JetBlackEngineLib.Data.Scripting;

public class ScriptRewardCall
{
    public string FunctionName = "";      // internal (script function) containing the call
    public string ExternalName = "";      // givePlayerGold / givePlayerExp / givePlayerItem
    public int InstructionAddr;           // address within the instruction stream (info)

    // For gold/exp: current value and the script-relative offset of the 4-byte immediate.
    public int IntValue;
    public int IntValueScrOffset = -1;

    // For items: current name, script-relative offset of the string bytes, max length.
    public string ItemName = "";
    public int StringScrOffset = -1;
    public int StringMaxLen;
    
    // Call-site geometry (instruction-stream addresses unless named ScrOffset).
    public int CallAddr;                  // address of the 0x7B CALL instruction
    public int CallExtIdxScrOffset = -1;  // file offset of the CALL's external index
    public int ArgPushCount;              // pushes belonging to this call (incl. count)
    public int SiteFirstPushScrOffset;    // file offset of the site's first push OPCODE
    public int SiteFirstPushAddr;         // its instruction-stream address
    public int AfterAddr;                 // address just past the site's pop
    public bool StructuralSafe;           // geometry verified → swap/remove/detour OK
}

public static class ScriptRewardScanner
{
    private const int HeaderSize = 0x60;

    /// <summary>
    /// Scans script bytes (the raw .scr entry, header included) and returns all
    /// reward calls found. Offsets in the results are relative to the start of
    /// the .scr data, ready to be applied to the same byte range.
    /// </summary>
    public static List<ScriptRewardCall> Scan(byte[] scr)
    {
        var results = new List<ScriptRewardCall>();
        if (scr.Length < HeaderSize + 0x30) return results;

        int B(int o) => HeaderSize + o;
        int I32(int o) => BitConverter.ToInt32(scr, o);

        var instOff = I32(B(0x0C));
        var strOff  = I32(B(0x10));
        var off3    = I32(B(0x14));
        var nInt    = BitConverter.ToUInt16(scr, B(0x20));
        var intOff  = I32(B(0x24));
        var nExt    = BitConverter.ToUInt16(scr, B(0x28));
        var extOff  = I32(B(0x2C));
        if (instOff <= 0 || strOff <= instOff || off3 < strOff) return results;

        // internals: addr -> name; externals: index -> name
        var internals = new SortedDictionary<int, string>();
        for (var i = 0; i < nInt; i++)
        {
            var o = B(intOff + 0x18 * i);
            internals[I32(o)] = ReadCString(scr, o + 4);
        }
        var externals = new Dictionary<int, string>();
        for (var i = 0; i < nExt; i++)
            externals[i] = ReadCString(scr, B(extOff + 0x18 * i) + 4);

        // decoded string table (bytes are reversed within each 32-bit word)
        var strTabLen = off3 - strOff;
        var decoded = new byte[strTabLen];
        for (var i = 0; i + 4 <= strTabLen; i += 4)
        {
            decoded[i]     = scr[B(strOff + i) + 3];
            decoded[i + 1] = scr[B(strOff + i) + 2];
            decoded[i + 2] = scr[B(strOff + i) + 1];
            decoded[i + 3] = scr[B(strOff + i)];
        }
        string StringAt(int off)
        {
            if (off < 0 || off >= strTabLen) return "";
            var e = off;
            while (e < strTabLen && decoded[e] != 0) e++;
            return Encoding.ASCII.GetString(decoded, off, e - off);
        }

        // walk instructions, tracking (value, immediateScrOffset) pushes
        var codeLen = strOff - instOff;
        var pushes = new List<(int Value, int ScrOff)>();
        var curFunc = "";
        for (var i = 0; i < codeLen;)
        {
            if (internals.TryGetValue(i, out var fn)) { curFunc = fn; pushes.Clear(); }

            var op = I32(B(instOff + i));
            switch (op)
            {
                case 0x27:                                    // push imm
                    pushes.Add((I32(B(instOff + i + 4)), instOff + i + 4));
                    i += 8; break;
                case 0x2E: pushes.Clear(); i += 4; break;     // enter
                case 0x7B:                                    // call external
                {
                    var extIdx = I32(B(instOff + i + 4));
                    externals.TryGetValue(extIdx, out var name);
                    // convention: ..., valueArg, argByteCount, CALL → arg is 2nd from top
                    if (pushes.Count >= 2 &&
                        (name == "givePlayerGold" || name == "givePlayerExp" || name == "givePlayerItem"))
                    {
                        var arg = pushes[^2];
                        var call = new ScriptRewardCall
                        {
                            FunctionName = curFunc, ExternalName = name!, InstructionAddr = i
                        };
                        if (name == "givePlayerItem")
                        {
                            call.ItemName = StringAt(arg.Value);
                            call.StringScrOffset = HeaderSize + strOff + arg.Value;
                            call.StringMaxLen = call.ItemName.Length;
                        }
                        else
                        {
                            call.IntValue = arg.Value;
                            call.IntValueScrOffset = arg.ScrOff + HeaderSize;
                        }
                        
                        // ---- call-site geometry (for swap / remove / detour) ----
                        call.CallAddr = i;
                        call.CallExtIdxScrOffset = B(instOff + i + 4);
                        call.ArgPushCount = name == "givePlayerItem" ? 3 : 2;

                        // a patchable site is: ArgPushCount contiguous immediate
                        // pushes, then CALL, then pop — verify before trusting it
                        var first = i - 8 * call.ArgPushCount;
                        var ok = first >= 0;
                        for (var p = 0; ok && p < call.ArgPushCount; p++)
                            ok = I32(B(instOff + first + p * 8)) == 0x27;
                        ok = ok && i + 8 < codeLen && I32(B(instOff + i + 8)) == 0x2C;

                        call.SiteFirstPushAddr = first;
                        call.SiteFirstPushScrOffset = B(instOff + first);
                        call.AfterAddr = i + 16;          // past CALL(8) + pop(8)
                        call.StructuralSafe = ok;
                        
                        results.Add(call);
                    }
                    i += 8; break;
                }
                case 0x7D: i += 12; break;                    // debug line
                // remaining opcodes: 4 bytes + one 4-byte arg for most; conservative walk
                case 0x01: case 0x02: case 0x03: case 0x04: case 0x05: case 0x06: case 0x07:
                case 0x08: case 0x0A: case 0x0B: case 0x0C: case 0x0D: case 0x0E: case 0x0F:
                case 0x10: case 0x11: case 0x12: case 0x13: case 0x14: case 0x15: case 0x16:
                case 0x18: case 0x1A: case 0x1C: case 0x1D: case 0x1E: case 0x1F: case 0x20:
                case 0x26: case 0x28: case 0x29: case 0x2A: case 0x2B: case 0x2C:
                case 0x31: case 0x33: case 0x34: case 0x35: case 0x36: case 0x37: case 0x38:
                case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3E: case 0x3F:
                case 0x44: case 0x45: case 0x46: case 0x47: case 0x56: case 0x57: case 0x5A:
                case 0x5B: case 0x68: case 0x69: case 0x6C: case 0x6D: case 0x72: case 0x73:
                case 0x75: case 0x76: case 0x77: case 0x78: case 0x7A: case 0x81:
                    i += 8; break;
                default:
                    i += 4; break;                            // no-arg opcodes
            }
        }
        return results;
    }

    /// <summary>Overwrites a gold/exp immediate in place.</summary>
    public static void ApplyIntValue(byte[] scr, ScriptRewardCall call, int newValue)
    {
        if (call.IntValueScrOffset < 0 || call.IntValueScrOffset + 4 > scr.Length)
            throw new ArgumentException("call has no patchable int offset");
        BitConverter.GetBytes(newValue).CopyTo(scr, call.IntValueScrOffset);
    }

    /// <summary>
    /// Overwrites an item name in place (word-reversed encoding). The new name must
    /// not exceed the current name's length; shorter names are NUL-padded.
    /// </summary>
    public static void ApplyItemName(byte[] scr, ScriptRewardCall call, string newName)
    {
        if (call.StringScrOffset < 0) throw new ArgumentException("call has no string offset");
        if (newName.Length > call.StringMaxLen)
            throw new ArgumentException(
                $"'{newName}' is {newName.Length} chars; must be {call.StringMaxLen} or fewer " +
                "(in-place edit cannot grow the string table)");

        // plain bytes, padded to the old length, then padded to word alignment
        var plain = new byte[((call.StringMaxLen + 1 + 3) / 4) * 4];   // include NUL
        Encoding.ASCII.GetBytes(newName).CopyTo(plain, 0);

        // write back word-reversed, without running past the original extent
        var end = Math.Min(call.StringScrOffset + call.StringMaxLen + 1, scr.Length);
        for (var i = 0; call.StringScrOffset + i < end; i++)
        {
            var word = i / 4; var bin = i % 4;
            var fileIndex = call.StringScrOffset + word * 4 + (3 - bin);
            if (fileIndex < scr.Length) scr[fileIndex] = plain[i];
        }
    }

    private static string ReadCString(byte[] d, int o)
    {
        var e = o;
        while (e < d.Length && d[e] != 0) e++;
        return Encoding.ASCII.GetString(d, o, e - o);
    }
}
