// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Scripting/ScriptDetourPatcher.cs
//
// Structural script edits, all validated in-game (TAVERN detour test):
//   • SwapSameShape — retarget a call between same-argument-shape externals
//     (givePlayerGold ↔ givePlayerExp). 4-byte in-place edit.
//   • RemoveCall — overwrite the site's first push with a jump past the pop.
//     In-place; the dead instructions are never executed.
//   • DetourReplaceCalls — replace a call site with any sequence of new reward
//     calls: appends a stub after the code region (jump out / jump back), appends
//     any new strings to the string table, fixes the three header offsets.
//     Append-only: no existing instruction moves, so jumps, internals, and inline
//     switch case tables all stay valid.
// ═══════════════════════════════════════════════════════════════════════════════
using System.Text;

namespace JetBlackEngineLib.Data.Scripting;

/// <summary>One reward call to emit in a detour stub.</summary>
public class NewRewardCall
{
    public string ExternalName = "";   // givePlayerGold / givePlayerExp / givePlayerItem
    public int IntValue;               // for gold/exp
    public string ItemName = "";       // for items
}

public static class ScriptDetourPatcher
{
    private const int HeaderSize = 0x60;

    /// <summary>Retargets a call between same-shape externals, in place.</summary>
    public static void SwapSameShape(byte[] scr, ScriptRewardCall call, string newExternal)
    {
        var extIdx = FindExternalIndex(scr, newExternal);
        if (extIdx < 0) throw new ArgumentException($"script does not import '{newExternal}'");
        if (call.CallExtIdxScrOffset < 0) throw new ArgumentException("call not patchable");
        BitConverter.GetBytes(extIdx).CopyTo(scr, call.CallExtIdxScrOffset);
    }

    /// <summary>Jumps over the whole call site, in place. The call never runs.</summary>
    public static void RemoveCall(byte[] scr, ScriptRewardCall call)
    {
        if (!call.StructuralSafe) throw new ArgumentException("call site not structurally safe");
        BitConverter.GetBytes(0x33).CopyTo(scr, call.SiteFirstPushScrOffset);          // jump
        BitConverter.GetBytes(call.AfterAddr).CopyTo(scr, call.SiteFirstPushScrOffset + 4);
    }

    /// <summary>
    /// Replaces a call site with a stub of new calls (detour). Returns the grown
    /// script; <paramref name="stringShift"/> is how far the string table moved in
    /// the file (add it to other calls' StringScrOffset values).
    /// </summary>
    public static byte[] DetourReplaceCalls(byte[] scr, ScriptRewardCall anchor,
        IReadOnlyList<NewRewardCall> newCalls, out int stringShift)
    {
        if (!anchor.StructuralSafe) throw new ArgumentException("call site not structurally safe");

        int I32(int o) => BitConverter.ToInt32(scr, o);
        var instOff = I32(HeaderSize + 0x0C);
        var strOff  = I32(HeaderSize + 0x10);
        var off3    = I32(HeaderSize + 0x14);
        var off4    = I32(HeaderSize + 0x18);
        var codeLen = strOff - instOff;
        var strTabLen = off3 - strOff;

        // -- resolve strings: reuse when present, else append (word-reversed) --
        var appended = new List<byte>();
        var strRefs = new int[newCalls.Count];
        for (var c = 0; c < newCalls.Count; c++)
        {
            if (newCalls[c].ExternalName != "givePlayerItem") continue;
            var existing = FindString(scr, strOff, strTabLen, newCalls[c].ItemName);
            strRefs[c] = existing >= 0
                ? existing
                : strTabLen + IndexOfAppend(appended, newCalls[c].ItemName);
        }

        // -- build the stub --
        var stub = new List<byte>();
        void Emit(int a, int b2) { stub.AddRange(BitConverter.GetBytes(a)); stub.AddRange(BitConverter.GetBytes(b2)); }
        for (var c = 0; c < newCalls.Count; c++)
        {
            var nc = newCalls[c];
            var extIdx = FindExternalIndex(scr, nc.ExternalName);
            if (extIdx < 0) throw new ArgumentException($"script does not import '{nc.ExternalName}'");
            if (nc.ExternalName == "givePlayerItem")
            {
                Emit(0x27, 0); Emit(0x27, strRefs[c]); Emit(0x27, 8);
                Emit(0x7B, extIdx); Emit(0x2C, 12);
            }
            else
            {
                Emit(0x27, nc.IntValue); Emit(0x27, 4);
                Emit(0x7B, extIdx); Emit(0x2C, 8);
            }
        }
        Emit(0x33, anchor.AfterAddr);                                   // jump back
        var d1 = stub.Count;                                            // code growth
        var d2 = appended.Count;                                        // string growth

        // -- assemble: [..code][stub][strings][new strings][tables..] --
        var result = new byte[scr.Length + d1 + d2];
        Array.Copy(scr, 0, result, 0, HeaderSize + strOff);
        stub.CopyTo(result, HeaderSize + strOff);
        Array.Copy(scr, HeaderSize + strOff, result, HeaderSize + strOff + d1, strTabLen);
        appended.CopyTo(result, HeaderSize + strOff + d1 + strTabLen);
        Array.Copy(scr, HeaderSize + off3, result, HeaderSize + off3 + d1 + d2,
                   scr.Length - HeaderSize - off3);

        // -- header fixups (off5 is -1/unused in BGDA scripts; leave it) --
        BitConverter.GetBytes(strOff + d1).CopyTo(result, HeaderSize + 0x10);
        BitConverter.GetBytes(off3 + d1 + d2).CopyTo(result, HeaderSize + 0x14);
        if (off4 != -1) BitConverter.GetBytes(off4 + d1 + d2).CopyTo(result, HeaderSize + 0x18);

        // -- detour: site's first push becomes jump -> stub (stub addr = old codeLen) --
        BitConverter.GetBytes(0x33).CopyTo(result, anchor.SiteFirstPushScrOffset);
        BitConverter.GetBytes(codeLen).CopyTo(result, anchor.SiteFirstPushScrOffset + 4);

        stringShift = d1;
        return result;
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static int FindExternalIndex(byte[] scr, string name)
    {
        var nExt = BitConverter.ToUInt16(scr, HeaderSize + 0x28);
        var extOff = BitConverter.ToInt32(scr, HeaderSize + 0x2C);
        for (var i = 0; i < nExt; i++)
        {
            var o = HeaderSize + extOff + 0x18 * i + 4;
            var e = o; while (e < scr.Length && scr[e] != 0) e++;
            if (Encoding.ASCII.GetString(scr, o, e - o) == name) return i;
        }
        return -1;
    }

    /// <summary>Finds a NUL-terminated string in the (word-reversed) table; -1 if absent.</summary>
    private static int FindString(byte[] scr, int strOff, int strTabLen, string s)
    {
        var decoded = new byte[strTabLen];
        for (var i = 0; i + 4 <= strTabLen; i += 4)
        {
            decoded[i]     = scr[HeaderSize + strOff + i + 3];
            decoded[i + 1] = scr[HeaderSize + strOff + i + 2];
            decoded[i + 2] = scr[HeaderSize + strOff + i + 1];
            decoded[i + 3] = scr[HeaderSize + strOff + i];
        }
        var needle = Encoding.ASCII.GetBytes(s + "\0");
        for (var i = 0; i + needle.Length <= strTabLen; i++)
        {
            var match = i == 0 || decoded[i - 1] == 0;    // must start at a string boundary
            for (var j = 0; match && j < needle.Length; j++) match = decoded[i + j] == needle[j];
            if (match) return i;
        }
        return -1;
    }

    /// <summary>Appends a word-reversed string (NUL-terminated, word-padded); returns its offset within the appended block.</summary>
    private static int IndexOfAppend(List<byte> appended, string s)
    {
        var off = appended.Count;
        var plain = Encoding.ASCII.GetBytes(s);
        var padded = new byte[((plain.Length + 1 + 3) / 4) * 4];
        plain.CopyTo(padded, 0);
        for (var i = 0; i < padded.Length; i += 4)
        { appended.Add(padded[i + 3]); appended.Add(padded[i + 2]); appended.Add(padded[i + 1]); appended.Add(padded[i]); }
        return off;
    }
}
