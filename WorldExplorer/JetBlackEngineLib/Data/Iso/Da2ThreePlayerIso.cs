// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Iso/Da2ThreePlayerIso.cs
//
// Makes or updates the "[3P]" copy of the Dark Alliance II (PS2) ISO that the three-player
// mod needs (the C# version of da2_make_3p_iso.py / make_3p_iso.bat). The files given are
// put into the BG/0MOD folder of the copy:
//
//   MTAPMAN.IRX    multitap driver (from Champions of Norrath: BG/0MOD/MTAPMAN.IRX)
//   HUMAN.LMP      Vahn, the human archer of Dark Alliance 1 (DA1: BG/DATA/HUMAN.LMP)
//   DWARF.LMP      Kromlech, the dwarven fighter of Dark Alliance 1 (DA1: BG/DATA/DWARF.LMP)
//   DA1MOUTH.VAT   the DA1 heroes' spoken lines (DA1: BG/DATA/MOUTH.VAT, under another name so
//                  it doesn't replace DA2's own MOUTH.VAT)
//   LILVARRA.LMP   Lilivarra, the drow priestess of Dark Alliance 1
//   MINOTAUR.LMP   the Minotaur of Dark Alliance 1
//
// The mod finds them at boot (FileTab3 searches \BG\0MOD\<name>;1): MTAPMAN.IRX is loaded
// after sio2man, the DA1 lumps are added to the game's file table or replace DA2's entry of
// the same name, and V_DA1FILES records which ones are there.
//
// - The original ISO is never written. If the [3P] copy exists it is updated in place
//   (files already there with the same content are skipped), otherwise the original is
//   copied first.
// - Each file is appended at the end of the image and gets a record in BG/0MOD, cloned
//   from PADMAN.IRX's (date, flags and the XA data kept); a file with the same name and
//   other content gets its record pointed at the new copy. Records stay sorted by name and
//   never cross a sector; the directory keeps its size, so the path tables stay valid.
// - Order of writes: the file data (beyond the old end of the volume), then the directory,
//   then the volume size in the primary volume descriptor. Then everything is read back.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace JetBlackEngineLib.Data.Iso;

/// <summary>A file to put on the disc: where it is and the 8.3 name it gets in BG/0MOD.</summary>
public sealed record Da2IsoInput(string SourcePath, string NameOnDisc);

/// <summary>One of the files the mod uses, with where it comes from.</summary>
public sealed record Da2IsoFileInfo(string NameOnDisc, string SourceName, string Game, string What, bool Required);

/// <summary>What a build did: the [3P] image, whether it changed, and a line per step.</summary>
public sealed class Da2IsoResult
{
    public string OutputPath { get; init; } = "";
    public bool Changed { get; set; }
    public bool Copied { get; set; }
    public List<string> Log { get; } = new();
    /// <summary>The names in BG/0MOD after the build.</summary>
    public List<string> Folder { get; } = new();
}

public static class Da2ThreePlayerIso
{
    public const int Sector = 2048;

    /// <summary>The files of the [3P] ISO (build 48 onwards), in make_3p_iso.bat's order.</summary>
    public static readonly Da2IsoFileInfo[] Files =
    {
        new("MTAPMAN.IRX", "MTAPMAN.IRX", "Champions of Norrath (BG\\0MOD)", "multitap driver, so the game can read controller 3", true),
        new("HUMAN.LMP", "HUMAN.LMP", "Dark Alliance (BG\\DATA)", "Vahn, the human archer of Dark Alliance 1", false),
        new("DWARF.LMP", "DWARF.LMP", "Dark Alliance (BG\\DATA)", "Kromlech, the dwarven fighter of Dark Alliance 1", false),
        new("DA1MOUTH.VAT", "MOUTH.VAT", "Dark Alliance (BG\\DATA)", "the Dark Alliance 1 heroes' spoken lines", false),
        new("LILVARRA.LMP", "LILVARRA.LMP", "Dark Alliance (BG\\DATA)", "Lilivarra, the drow priestess of Dark Alliance 1", false),
        new("MINOTAUR.LMP", "MINOTAUR.LMP", "Dark Alliance (BG\\DATA)", "the Minotaur of Dark Alliance 1", false),
    };

    /// <summary>"Baldur's Gate - Dark Alliance II (USA).iso" -> "... (USA) [3P].iso" next to it.</summary>
    public static string DefaultOutputPath(string originalIso) =>
        Path.Combine(Path.GetDirectoryName(originalIso) ?? "", Path.GetFileNameWithoutExtension(originalIso) + " [3P].iso");

    /// <summary>
    /// Makes or updates the [3P] copy. Throws <see cref="InvalidDataException"/> (or an IO exception)
    /// with a message for the user when something is wrong; the original ISO is only read.
    /// </summary>
    public static Da2IsoResult Build(string originalIso, IReadOnlyList<Da2IsoInput> inputs, string? outputIso = null,
        Action<string>? log = null, CancellationToken cancel = default)
    {
        var dst = outputIso ?? DefaultOutputPath(originalIso);
        var result = new Da2IsoResult { OutputPath = dst };
        void Say(string line)
        {
            result.Log.Add(line);
            log?.Invoke(line);
        }

        if (string.Equals(Path.GetFullPath(originalIso), Path.GetFullPath(dst), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The [3P] ISO must not be the original ISO.");
        if (!File.Exists(originalIso)) throw new InvalidDataException("The original ISO was not found: " + originalIso);

        var wanted = new List<(byte[] Name, byte[] Data, string Path)>();
        foreach (var input in inputs)
        {
            var data = CheckInput(input, out var name);
            if (wanted.Any(w => w.Name.SequenceEqual(name)))
                throw new InvalidDataException("Two files for the same name on the disc: " + Encoding.ASCII.GetString(name));
            wanted.Add((name, data, input.SourcePath));
        }

        using (var f = File.OpenRead(originalIso)) ReadPvd(f, originalIso);
        if (File.Exists(dst))
        {
            using (var f = File.OpenRead(dst)) ReadPvd(f, dst);
            if (new FileInfo(dst).Length < new FileInfo(originalIso).Length)
                throw new InvalidDataException($"{dst} is smaller than the original ISO: delete it and build it again.");
            Say("Updating the existing copy: " + dst);
        }
        else
        {
            Say("Copying the ISO (this can take a minute or two)...");
            Say("  from: " + originalIso);
            Say("  to:   " + dst);
            CopyFile(originalIso, dst, cancel);
            result.Copied = true;
        }

        FileStream stream;
        try
        {
            stream = new FileStream(dst, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            throw new InvalidDataException($"Can't write to {dst} ({ex.Message}). Close PCSX2 (the ISO must not be in use) and try again.");
        }

        using (stream)
        {
            var pvd = ReadPvd(stream, dst);
            var root = pvd.AsSpan(156, 34).ToArray();
            var bg = FindChild(stream, root, "BG");
            var mod = FindChild(stream, bg, "0MOD");
            if (!IsDir(mod)) throw new InvalidDataException("BG/0MOD is not a folder on this ISO.");
            var nSec = (Size(mod) + Sector - 1) / Sector;
            var recs = ParseDir(ReadSectors(stream, Lba(mod), nSec));
            if (recs.Count < 2 || !NameOf(recs[0]).SequenceEqual(new byte[] { 0 }) || !NameOf(recs[1]).SequenceEqual(new byte[] { 1 }))
                throw new InvalidDataException("Unexpected BG/0MOD directory layout.");
            var specials = recs.Take(2).ToList();
            var others = recs.Skip(2).ToList();
            var fileRecs = others.Where(r => !IsDir(r)).ToList();
            if (fileRecs.Count == 0) throw new InvalidDataException("BG/0MOD has no files to copy a record from.");
            var template = fileRecs.FirstOrDefault(r => BaseName(r) == "PADMAN.IRX") ?? fileRecs[0];

            var volSectors = BitConverter.ToUInt32(pvd, 80);
            var end = Math.Max(volSectors, (uint)((stream.Length + Sector - 1) / Sector));
            var todo = new List<(byte[] Data, uint Lba)>();
            var changed = false;
            foreach (var (name, data, _) in wanted)
            {
                cancel.ThrowIfCancellationRequested();
                var text = Encoding.ASCII.GetString(name);
                var old = others.FirstOrDefault(r => BaseName(r) == text);
                if (old != null && Size(old) == data.Length && ReadAt(stream, (long)Lba(old) * Sector, data.Length).SequenceEqual(data))
                {
                    Say($"  {text,-12} already on the ISO");
                    continue;
                }

                var lba = end;
                end += (uint)((data.Length + Sector - 1) / Sector);
                todo.Add((data, lba));
                if (old != null)
                {
                    others[others.IndexOf(old)] = Repoint(old, lba, (uint)data.Length);
                    Say($"  {text,-12} replaced ({data.Length:N0} bytes, sector {lba})");
                }
                else
                {
                    others.Add(BuildRecord(template, name.Concat(Encoding.ASCII.GetBytes(";1")).ToArray(), lba, (uint)data.Length));
                    Say($"  {text,-12} added ({data.Length:N0} bytes, sector {lba})");
                }

                changed = true;
            }

            if (!changed)
            {
                Say("Nothing to do - the ISO is up to date.");
                result.Folder.AddRange(others.Select(BaseName));
                return result;
            }

            others = others.OrderBy(r => Encoding.ASCII.GetString(NameOf(r)).ToUpperInvariant(), StringComparer.Ordinal).ToList();
            var newDir = LayoutDir(specials.Concat(others).ToList(), nSec)
                         ?? throw new InvalidDataException("No room left in the BG/0MOD directory for another entry.");

            // 1. file data (beyond the old end of the volume), 2. directory, 3. volume size
            foreach (var (data, lba) in todo)
            {
                cancel.ThrowIfCancellationRequested();
                stream.Position = (long)lba * Sector;
                stream.Write(data);
                var pad = (Sector - data.Length % Sector) % Sector;
                if (pad > 0) stream.Write(new byte[pad]);
            }

            stream.Flush(true);
            stream.Position = (long)Lba(mod) * Sector;
            stream.Write(newDir);
            stream.Position = 16L * Sector + 80;
            stream.Write(Both32(end));
            stream.Flush(true);
            result.Changed = true;

            // verify
            var check = ParseDir(ReadSectors(stream, Lba(mod), nSec)).Skip(2).ToList();
            foreach (var (name, data, _) in wanted)
            {
                var text = Encoding.ASCII.GetString(name);
                var r = check.FirstOrDefault(x => BaseName(x) == text);
                if (r == null || Size(r) != data.Length || !ReadAt(stream, (long)Lba(r) * Sector, data.Length).SequenceEqual(data))
                    throw new InvalidDataException($"Verification FAILED for {text}: delete {dst} and build it again.");
            }

            result.Folder.AddRange(check.Select(BaseName));
            Say("Verified: BG/0MOD now holds " + string.Join(", ", result.Folder.OrderBy(n => n, StringComparer.Ordinal)));
        }

        Say("Done: " + dst);
        return result;
    }

    /// <summary>Reads and checks one input; returns its data and its upper-case name on the disc.</summary>
    private static byte[] CheckInput(Da2IsoInput input, out byte[] name)
    {
        var path = input.SourcePath;
        if (!File.Exists(path)) throw new InvalidDataException("File not found: " + path);
        var text = input.NameOnDisc.ToUpperInvariant();
        var dot = text.IndexOf('.');
        var stem = dot < 0 ? text : text.Substring(0, dot);
        var ext = dot < 0 ? "" : text.Substring(dot + 1);
        if (stem.Length is < 1 or > 8 || ext.Length is < 1 or > 3 || ext.Contains('.') ||
            !text.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '.'))
            throw new InvalidDataException("Not an 8.3 file name: " + input.NameOnDisc);
        name = Encoding.ASCII.GetBytes(text);

        var data = File.ReadAllBytes(path);
        if (data.Length == 0) throw new InvalidDataException("Empty file: " + path);
        if (text.EndsWith(".IRX", StringComparison.Ordinal) &&
            !(data.Length >= 4 && data[0] == 0x7F && data[1] == (byte)'E' && data[2] == (byte)'L' && data[3] == (byte)'F'))
            throw new InvalidDataException(path + " does not look like an IRX module.");
        if (text.EndsWith(".LMP", StringComparison.Ordinal) && !LooksLikeLump(data))
            throw new InvalidDataException(path + " does not look like a Dark Alliance lump file.");
        if (text.EndsWith(".VAT", StringComparison.Ordinal) && (data.Length % Sector != 0 || data.Length < 0x100000))
            throw new InvalidDataException(path + " does not look like a Dark Alliance voice stream file.");
        return data;
    }

    /// <summary>A lump: entry count, then 64-byte entries {name[56], offset, size}, the first entry inside the file.</summary>
    public static bool LooksLikeLump(byte[] data)
    {
        if (data.Length < 68) return false;
        var n = BitConverter.ToUInt32(data, 0);
        if (n == 0 || n >= 2000 || 4 + n * 64 > data.Length) return false;
        var o = BitConverter.ToUInt32(data, 4 + 56);
        var s = BitConverter.ToUInt32(data, 4 + 60);
        return 4 + n * 64 <= o && (long)o + s <= data.Length;
    }

    /// <summary>Reads a file from the root folder of an ISO (e.g. "SLUS_206.75"); null when it isn't there.</summary>
    public static byte[]? ReadRootFile(string iso, string name)
    {
        using var f = File.OpenRead(iso);
        var pvd = ReadPvd(f, iso);
        var root = pvd.AsSpan(156, 34).ToArray();
        var buf = ReadSectors(f, Lba(root), (Size(root) + Sector - 1) / Sector);
        foreach (var r in ParseDir(buf))
        {
            if (BaseName(r) == name.ToUpperInvariant() && !IsDir(r))
                return ReadAt(f, (long)Lba(r) * Sector, (int)Size(r));
        }

        return null;
    }

    // ---- ISO 9660 helpers ----

    private static void CopyFile(string from, string to, CancellationToken cancel)
    {
        var temp = to + ".part";
        try
        {
            using (var src = File.OpenRead(from))
            using (var dst = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[1 << 20];
                int n;
                while ((n = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancel.ThrowIfCancellationRequested();
                    dst.Write(buffer, 0, n);
                }
            }

            File.Move(temp, to);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static byte[] ReadPvd(Stream f, string what)
    {
        var pvd = ReadAt(f, 16L * Sector, Sector);
        if (pvd.Length < Sector || pvd[0] != 1 || Encoding.ASCII.GetString(pvd, 1, 5) != "CD001")
            throw new InvalidDataException("Not an ISO 9660 image: " + what);
        return pvd;
    }

    private static byte[] ReadAt(Stream f, long position, int count)
    {
        var b = new byte[count];
        f.Position = position;
        var got = 0;
        while (got < count)
        {
            var n = f.Read(b, got, count - got);
            if (n == 0) break;
            got += n;
        }

        return got == count ? b : b.Take(got).ToArray();
    }

    private static byte[] ReadSectors(Stream f, uint lba, long count)
    {
        var b = ReadAt(f, (long)lba * Sector, (int)(count * Sector));
        return b.Length == count * Sector ? b : b.Concat(new byte[count * Sector - b.Length]).ToArray();
    }

    /// <summary>The directory records of a directory extent, in order.</summary>
    private static List<byte[]> ParseDir(byte[] buf)
    {
        var records = new List<byte[]>();
        var off = 0;
        while (off < buf.Length)
        {
            var len = buf[off];
            if (len == 0)
            {
                off = (off / Sector + 1) * Sector;   // the rest of this sector is unused
                continue;
            }

            if (off + len > buf.Length || len < 34) throw new InvalidDataException("A damaged directory record on the ISO.");
            records.Add(buf.AsSpan(off, len).ToArray());
            off += len;
        }

        return records;
    }

    private static byte[] NameOf(byte[] r) => r.AsSpan(33, r[32]).ToArray();
    private static uint Lba(byte[] r) => BitConverter.ToUInt32(r, 2);
    private static uint Size(byte[] r) => BitConverter.ToUInt32(r, 10);
    private static bool IsDir(byte[] r) => (r[25] & 2) != 0;

    private static string BaseName(byte[] r)
    {
        var name = Encoding.ASCII.GetString(NameOf(r));
        var semi = name.IndexOf(';');
        return (semi < 0 ? name : name.Substring(0, semi)).ToUpperInvariant();
    }

    private static byte[] FindChild(Stream f, byte[] dirRecord, string name)
    {
        var buf = ReadSectors(f, Lba(dirRecord), (Size(dirRecord) + Sector - 1) / Sector);
        foreach (var r in ParseDir(buf))
        {
            var n = NameOf(r);
            if (n.Length == 1 && n[0] <= 1) continue;
            if (BaseName(r) == name) return r;
        }

        throw new InvalidDataException($"Could not find the folder {name} on the ISO - is this Dark Alliance II?");
    }

    private static byte[] Both32(uint v)
    {
        var b = new byte[8];
        BitConverter.GetBytes(v).CopyTo(b, 0);
        b[4] = (byte)(v >> 24);
        b[5] = (byte)(v >> 16);
        b[6] = (byte)(v >> 8);
        b[7] = (byte)v;
        return b;
    }

    /// <summary>A file record cloned from <paramref name="template"/> with another name, extent and size.</summary>
    private static byte[] BuildRecord(byte[] template, byte[] name, uint lba, uint size)
    {
        var tLen = template[32];
        var tPad = tLen % 2 == 0 ? 1 : 0;
        var systemUse = template.AsSpan(33 + tLen + tPad).ToArray();   // the XA area
        var pad = name.Length % 2 == 0 ? new byte[] { 0 } : Array.Empty<byte>();
        var body = template.AsSpan(0, 33).ToArray();
        Both32(lba).CopyTo(body, 2);
        Both32(size).CopyTo(body, 10);
        body[32] = (byte)name.Length;
        var rec = body.Concat(name).Concat(pad).Concat(systemUse).ToArray();
        rec[0] = (byte)rec.Length;
        return rec;
    }

    private static byte[] Repoint(byte[] rec, uint lba, uint size)
    {
        var r = (byte[])rec.Clone();
        Both32(lba).CopyTo(r, 2);
        Both32(size).CopyTo(r, 10);
        return r;
    }

    /// <summary>Records into an extent of <paramref name="nSec"/> sectors (a record never crosses a sector); null if they don't fit.</summary>
    private static byte[]? LayoutDir(List<byte[]> records, long nSec)
    {
        var output = new byte[nSec * Sector];
        var off = 0;
        foreach (var r in records)
        {
            if (off / Sector != (off + r.Length - 1) / Sector) off = (off / Sector + 1) * Sector;
            if (off + r.Length > output.Length) return null;
            r.CopyTo(output, off);
            off += r.Length;
        }

        return output;
    }
}
