/*  Read-only diagnostic. Dumps the .world internal tables so we can see how
    rendering and collision are organised. Does NOT modify anything.

    Layout follows the BGDA / Dark Alliance reference (bgtools WorldDecode):
      0x00 numElements          0x18 perCellTopo  -> cols*rows offsets, each to a
      0x10 cols  0x14 rows           -1-terminated short list
      0x1C count1c              0x20 offset20     -> count1c records x 0x1c bytes
      0x24 elementBase (render element array)      (each 0x08 -> terrain patch)
      0x30 cols38 0x34 rows38   0x38 offset38     -> a second grid (role unknown)

    v2 of this file: all bounds checks are overflow-safe (offset <= len - size),
    list walks stop the instant the pointer leaves the file, and every section is
    isolated so one malformed table can't crash the dump.

    File: JetBlackEngineLib/Data/World/WorldStructureAnalyzer.cs
*/

using System;
using System.Text;

namespace JetBlackEngineLib.Data.World;

public static class WorldStructureAnalyzer
{
    public static string Analyze(byte[] w, EngineVersion engineVersion)
    {
        var sb = new StringBuilder();

        // Overflow-safe: never computes off+size (which can wrap for junk offsets).
        bool Fits(int off, int size) => off >= 0 && size >= 0 && off <= w.Length - size;
        int   I(int o) => Fits(o, 4) ? BitConverter.ToInt32(w, o) : 0;
        short S(int o) => Fits(o, 2) ? BitConverter.ToInt16(w, o) : (short)0;
        float F(int o) => Fits(o, 4) ? BitConverter.ToSingle(w, o) : 0f;
        string H(int v) => "0x" + v.ToString("X");

        int numElements = I(0x00);
        int cols = I(0x10), rows = I(0x14);
        int perCellTopo = I(0x18), count1c = I(0x1C), offset20 = I(0x20);
        int elementBase = I(0x24);
        int cols38 = I(0x30), rows38 = I(0x34), offset38 = I(0x38);

        int structSize = engineVersion switch
        {
            EngineVersion.ReturnToArms        => 0x3C,
            EngineVersion.JusticeLeagueHeroes => 0x3C,
            EngineVersion.BrotherhoodOfSteel  => 0x50,
            _                                 => 0x38,
        };
        bool isV2 = structSize == 0x3C;   // RTA/JLH pos is i32; V1/BoS pos is i16

        sb.AppendLine("=== .world structure dump (read-only) ===");
        sb.AppendLine($"engine={engineVersion}  fileLen={w.Length} ({H(w.Length)})  elementStruct={H(structSize)}");
        sb.AppendLine();

        // -- header --------------------------------------------------------------
        try
        {
            sb.AppendLine("-- header --");
            sb.AppendLine($"0x00 numElements : {numElements}");
            for (int o = 0x04; o <= 0x6C; o += 4)
            {
                string tag = o switch
                {
                    0x10 => " cols", 0x14 => " rows", 0x18 => " perCellTopo->",
                    0x1C => " count1c", 0x20 => " offset20->", 0x24 => " elementBase->",
                    0x30 => " cols38", 0x34 => " rows38", 0x38 => " offset38->",
                    0x58 => " texll", 0x5C => " texur", 0x64 => " texArrOff->",
                    0x6C => " minimap->", _ => ""
                };
                sb.AppendLine($"{H(o),-6} : {I(o),-12} ({H(I(o))}){tag}");
            }
            sb.AppendLine();
        }
        catch (Exception ex) { sb.AppendLine($"[header dump failed: {ex.Message}]\n"); }

        // -- render elements: Pos vs Bounds -------------------------------------
        // V1 (0x38): Bounds1 0x0C, Bounds2 0x18, Pos 0x2A (i16/16)
        // V2 (0x3C): Bounds1 0x08, Bounds2 0x14, Pos 0x28 (i32/16)
        // BoS(0x50): Pos 0x36 (i16/16); bounds offset uncertain -> skipped
        try
        {
            int b1 = isV2 ? 0x08 : 0x0C;
            int b2 = isV2 ? 0x14 : 0x18;
            int posOff = isV2 ? 0x28 : (structSize == 0x50 ? 0x36 : 0x2A);
            int flagsOff = structSize == 0x38 ? 0x30 : isV2 ? 0x36 : 0x3C;
            bool dumpBounds = structSize != 0x50;

            sb.AppendLine("-- render elements (elementBase): index, Pos(world), bounds center, |center-Pos| --");
            sb.AppendLine("   KEY: if |center-Pos| is small (bounds centre ~= Pos) the bounds are WORLD-space.");

            double sumD = 0, sumPos = 0; int counted = 0;
            int listElems = Math.Min(Math.Max(numElements, 0), 80);
            int safeMax = Math.Min(Math.Max(numElements, 0), 200000);
            for (int i = 0; i < safeMax; i++)
            {
                int e = elementBase + i * structSize;
                if (!Fits(e, structSize)) break;

                double px, py, pz;
                if (isV2) { px = I(e+posOff)/16.0; py = I(e+posOff+4)/16.0; pz = I(e+posOff+8)/16.0; }
                else      { px = S(e+posOff)/16.0; py = S(e+posOff+2)/16.0; pz = S(e+posOff+4)/16.0; }

                string boundsStr = "(bounds n/a)";
                if (dumpBounds)
                {
                    double b1x=F(e+b1), b1y=F(e+b1+4), b1z=F(e+b1+8);
                    double b2x=F(e+b2), b2y=F(e+b2+4), b2z=F(e+b2+8);
                    double cx=(b1x+b2x)/2, cy=(b1y+b2y)/2, cz=(b1z+b2z)/2;
                    double d=Math.Sqrt((cx-px)*(cx-px)+(cy-py)*(cy-py)+(cz-pz)*(cz-pz));
                    sumD += d; sumPos += Math.Sqrt(px*px+py*py+pz*pz); counted++;
                    if (i < listElems)
                        boundsStr = $"B1=({b1x:F1},{b1y:F1},{b1z:F1}) B2=({b2x:F1},{b2y:F1},{b2z:F1}) "
                                  + $"center=({cx:F1},{cy:F1},{cz:F1}) |c-Pos|={d:F1}";
                }
                if (i < listElems)
                    sb.AppendLine($"  [{i}] Pos=({px:F1},{py:F1},{pz:F1}) {boundsStr} "
                                + $"vif={H(I(e))} flags={H(I(e + flagsOff))}");
            }
            if (numElements > listElems) sb.AppendLine($"  ... ({numElements - listElems} more elements)");
            if (counted > 0)
                sb.AppendLine($"  >> avg |center-Pos| = {sumD/counted:F1},  avg |Pos| = {sumPos/counted:F1}  "
                            + "(|center-Pos| << |Pos| means WORLD-space bounds; ~equal means local bounds)");
            sb.AppendLine();
        }
        catch (Exception ex) { sb.AppendLine($"[element dump failed: {ex.Message}]\n"); }

        // -- per-cell topo lists (0x18) -----------------------------------------
        DumpShortGrid(sb, w, "per-cell topo lists (0x18)", perCellTopo, cols, rows, numElements, count1c);

        // -- 0x20 topo / terrain records ----------------------------------------
        try
        {
            if (offset20 > 0 && count1c > 0 && count1c < 200000)
            {
                sb.AppendLine($"-- topo element array (0x20): {count1c} records x 0x1c bytes --");
                int listRecs = Math.Min(count1c, 50);
                for (int i = 0; i < listRecs; i++)
                {
                    int r = offset20 + i * 0x1c;
                    if (!Fits(r, 0x1c)) break;
                    int p8 = I(r + 0x08);
                    string patch = "";
                    if (Fits(p8, 0x10))
                        patch = $"  patch@{H(p8)} rows={I(p8+0x08)} cols={I(p8+0x0C)}";
                    sb.AppendLine($"  [{i}] s=({S(r)},{S(r+2)},{S(r+4)},{S(r+6)}) off8={H(p8)}{patch} "
                                + $"0x0C={I(r+0x0C)} 0x10={I(r+0x10)} 0x14={I(r+0x14)} 0x18={I(r+0x18)}");
                }
                if (count1c > listRecs) sb.AppendLine($"  ... ({count1c - listRecs} more records)");
                sb.AppendLine();
            }
        }
        catch (Exception ex) { sb.AppendLine($"[0x20 dump failed: {ex.Message}]\n"); }

        // -- 0x38 grid (role unknown - dumped defensively) ----------------------
        DumpShortGrid(sb, w, "0x38 grid", offset38, cols38, rows38, numElements, count1c);

        sb.AppendLine("=== end ===");
        return sb.ToString();
    }

    /// <summary>Dumps a cols*rows table of offsets, each pointing at a -1-terminated
    /// short list, plus a summary of the max index seen. Overflow-safe and bails on
    /// out-of-range pointers, so a table that isn't actually this shape just reports
    /// few/no refs instead of crashing.</summary>
    private static void DumpShortGrid(StringBuilder sb, byte[] w, string title,
        int tableOff, int cols, int rows, int numElements, int count1c)
    {
        try
        {
            bool Fits(int off, int size) => off >= 0 && size >= 0 && off <= w.Length - size;
            int   I(int o) => Fits(o, 4) ? BitConverter.ToInt32(w, o) : 0;
            short S(int o) => Fits(o, 2) ? BitConverter.ToInt16(w, o) : (short)0;

            if (tableOff <= 0 || cols <= 0 || rows <= 0 || (long)cols * rows > 100000
                || !Fits(tableOff, 4))
            {
                sb.AppendLine($"-- {title}: not present / out of range "
                            + $"(off={tableOff:X}, {cols}x{rows}) --\n");
                return;
            }

            sb.AppendLine($"-- {title}: {cols}x{rows} cells @ 0x{tableOff:X} --");
            int maxVal = -1, totalRefs = 0, shownCells = 0, badOffsets = 0;
            for (int c = 0; c < cols * rows; c++)
            {
                int listOff = I(tableOff + c * 4);
                if (listOff <= 0 || !Fits(listOff, 2)) { badOffsets++; continue; }

                var line = new StringBuilder();
                int p = listOff, n = 0;
                while (Fits(p, 2) && n < 8192)
                {
                    short v = S(p);
                    if (v < 0) break;                 // -1 terminator
                    if (shownCells < 50) line.Append(v).Append(' ');
                    if (v > maxVal) maxVal = v;
                    totalRefs++; n++; p += 2;
                }
                if (n > 0 && shownCells < 50) { sb.AppendLine($"  cell {c}: {line}"); shownCells++; }
            }
            sb.AppendLine($"  >> max index = {maxVal}, total refs = {totalRefs}, "
                        + $"out-of-range cell offsets = {badOffsets}  "
                        + $"(numElements={numElements}, count1c={count1c})");
            sb.AppendLine("  >> max ~= numElements-1 means it indexes RENDER elements; "
                        + "max ~= count1c-1 means it indexes the 0x20 array.");
            sb.AppendLine("  >> many out-of-range offsets means this table is NOT a "
                        + "cols*rows offset->short-list (wrong shape).");
            sb.AppendLine();
        }
        catch (Exception ex) { sb.AppendLine($"[{title} dump failed: {ex.Message}]\n"); }
    }
}