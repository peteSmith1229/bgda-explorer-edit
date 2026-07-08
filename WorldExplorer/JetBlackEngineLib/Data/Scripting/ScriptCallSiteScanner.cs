// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Scripting/ScriptCallSiteScanner.cs
//
// Enumerates EVERY external call site in a script (not just reward calls), so the
// user can pick an anchor from the disassembly they can already see. This is the
// hard-won lesson from the endgame-reward investigation: the right insertion point
// depends on each script's control flow (a "victory" label was dead code; an
// "*Eldrith" arg was an engine death-callback, not a script anchor). Automatic
// "insert at level end" guessing is unreliable — the user must choose a concrete,
// named, verified site.
//
// A site is "insertable" when it is a run of plain-immediate pushes followed by
// CALL then pop — exactly the geometry the detour relies on.
// ═══════════════════════════════════════════════════════════════════════════════
using System.Text;

namespace JetBlackEngineLib.Data.Scripting;

public class ScriptCallSite
{
    public string FunctionName = "";     // internal function the call sits in
    public string ExternalName = "";     // the native being called
    public int CallAddr;                 // instruction-stream address of the 0x7B CALL
    public int ExtIndex;                 // external index invoked
    public int ArgCount;                 // pushes consumed (from the pop's byte count / 4)
    public int SiteFirstPushScrOffset;   // file offset of the first push opcode
    public int SiteFirstPushAddr;        // its instruction-stream address
    public int AfterAddr;                // instruction-stream address just past the pop
    public bool Insertable;              // geometry verified

    /// <summary>Human-readable label for an anchor picker.</summary>
    public string Display => $"{FunctionName}  →  {ExternalName}  @ 0x{CallAddr:X4}"
                             + (Insertable ? "" : "  (not insertable)");
}

public static class ScriptCallSiteScanner
{
    private const int HeaderSize = 0x60;

    public static List<ScriptCallSite> Scan(byte[] scr)
    {
        var sites = new List<ScriptCallSite>();
        if (scr.Length < HeaderSize + 0x30) return sites;
        int B(int o) => HeaderSize + o;
        int I32(int o) => BitConverter.ToInt32(scr, o);

        var instOff = I32(B(0x0C));
        var strOff  = I32(B(0x10));
        var nInt    = BitConverter.ToUInt16(scr, B(0x20));
        var intOff  = I32(B(0x24));
        var nExt    = BitConverter.ToUInt16(scr, B(0x28));
        var extOff  = I32(B(0x2C));
        if (instOff <= 0 || strOff <= instOff) return sites;

        var internals = new SortedDictionary<int, string>();
        for (var i = 0; i < nInt; i++)
            internals[I32(B(intOff + 0x18 * i))] = ReadCString(scr, B(intOff + 0x18 * i) + 4);
        var externals = new Dictionary<int, string>();
        for (var i = 0; i < nExt; i++)
            externals[i] = ReadCString(scr, B(extOff + 0x18 * i) + 4);

        var codeLen = strOff - instOff;
        var pushAddrs = new List<int>();     // instruction addresses of recent pushes
        var curFunc = "";
        for (var i = 0; i < codeLen;)
        {
            if (internals.TryGetValue(i, out var fn)) { curFunc = fn; pushAddrs.Clear(); }
            var op = I32(B(instOff + i));
            switch (op)
            {
                case 0x27: pushAddrs.Add(i); i += 8; break;   // push imm
                case 0x2E: pushAddrs.Clear(); i += 4; break;  // enter
                case 0x7B:                                    // call
                {
                    var site = new ScriptCallSite
                    {
                        FunctionName = curFunc,
                        ExtIndex = I32(B(instOff + i + 4)),
                        CallAddr = i
                    };
                    externals.TryGetValue(site.ExtIndex, out var en);
                    site.ExternalName = en ?? $"ext{site.ExtIndex}";

                    // pop must immediately follow; its byte count / 4 = arg count
                    if (i + 8 < codeLen && I32(B(instOff + i + 8)) == 0x2C)
                    {
                        site.ArgCount = I32(B(instOff + i + 12)) / 4;
                        site.AfterAddr = i + 16;
                        var first = i - 8 * site.ArgCount;
                        var ok = first >= 0 && pushAddrs.Count >= site.ArgCount;
                        for (var p = 0; ok && p < site.ArgCount; p++)
                            ok = I32(B(instOff + first + p * 8)) == 0x27;
                        site.Insertable = ok;
                        site.SiteFirstPushAddr = first;
                        site.SiteFirstPushScrOffset = B(instOff + first);
                    }
                    sites.Add(site);
                    i += 8; break;
                }
                case 0x7D: i += 12; break;
                default:
                    i += SingleOrDouble(op) ? 8 : 4; break;
            }
        }
        return sites;
    }

    // opcodes that carry one 4-byte argument (same table the scanner/decoder use)
    private static bool SingleOrDouble(int op) =>
        op is 0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06 or 0x07 or 0x08 or 0x0A or 0x0B
           or 0x0C or 0x0D or 0x0E or 0x0F or 0x10 or 0x11 or 0x12 or 0x13 or 0x14 or 0x15
           or 0x16 or 0x18 or 0x1A or 0x1C or 0x1D or 0x1E or 0x1F or 0x20 or 0x26 or 0x28
           or 0x29 or 0x2B or 0x2C or 0x31 or 0x33 or 0x34 or 0x35 or 0x36 or 0x37 or 0x38
           or 0x39 or 0x3A or 0x3B or 0x3C or 0x3D or 0x3E or 0x3F or 0x40 or 0x44 or 0x45
           or 0x46 or 0x47 or 0x56 or 0x57 or 0x5A or 0x5B or 0x68 or 0x69 or 0x6C or 0x6D
           or 0x72 or 0x73 or 0x75 or 0x76 or 0x77 or 0x78 or 0x7A or 0x81;

    private static string ReadCString(byte[] d, int o)
    {
        var e = o; while (e < d.Length && d[e] != 0) e++;
        return Encoding.ASCII.GetString(d, o, e - o);
    }
}
