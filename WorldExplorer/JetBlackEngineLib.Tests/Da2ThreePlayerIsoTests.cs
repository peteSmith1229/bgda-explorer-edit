using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Iso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The [3P] ISO builder on small ISO 9660 images made here (the layout of the Dark Alliance II disc:
/// SLUS_206.75 in the root, BG/0MOD with the IOP modules, BG/DATA).
/// </summary>
[TestFixture]
public class Da2ThreePlayerIsoTests
{
    private const int Sector = 2048;
    private string _dir = "";

    [SetUp]
    public void MakeFolder()
    {
        _dir = Path.Combine(Path.GetTempPath(), "Da2IsoTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void RemoveFolder()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static byte[] Both32(uint v) =>
        BitConverter.GetBytes(v).Concat(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }).ToArray();

    private static byte[] Record(string name, uint lba, uint size, bool dir, bool xa)
    {
        var n = name switch { "." => new byte[] { 0 }, ".." => new byte[] { 1 }, _ => Encoding.ASCII.GetBytes(name) };
        var body = new byte[33];
        Both32(lba).CopyTo(body, 2);
        Both32(size).CopyTo(body, 10);
        new byte[] { 103, 3, 14, 12, 0, 0, 0 }.CopyTo(body, 18);   // 2003-03-14
        body[25] = (byte)(dir ? 2 : 0);
        body[28] = 1;
        body[31] = 1;
        body[32] = (byte)n.Length;
        var pad = n.Length % 2 == 0 ? new byte[] { 0 } : Array.Empty<byte>();
        var su = xa ? new byte[] { 0, 0, 0, 0, 0x0D, 0x55, (byte)'X', (byte)'A', 0, 0, 0, 0, 0, 0 } : Array.Empty<byte>();
        var rec = body.Concat(n).Concat(pad).Concat(su).ToArray();
        rec[0] = (byte)rec.Length;
        return rec;
    }

    /// <summary>An image with SLUS_206.75 (a small stand-in unless one is given), BG/0MOD (PADMAN.IRX, SIO2MAN.IRX) and BG/DATA.</summary>
    private string MakeIso(bool xa = true, int extraModules = 0, byte[]? executable = null)
    {
        var elf = executable ?? new byte[5000];
        if (executable == null)
        {
            new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' }.CopyTo(elf, 0);
            elf[100] = 42;
        }

        var padman = Enumerable.Range(0, 3000).Select(i => (byte)i).ToArray();
        var sio2 = Enumerable.Range(0, 1500).Select(i => (byte)(i * 7)).ToArray();
        const uint root = 18, bg = 19, mod = 20, data = 21, elfAt = 22;
        var padAt = elfAt + (uint)((elf.Length + Sector - 1) / Sector);
        var sioAt = padAt + 2;
        var total = sioAt + 1;

        var image = new byte[total * Sector];
        void Put(uint sector, byte[] bytes) => bytes.CopyTo(image, sector * Sector);
        byte[] Dir(uint self, uint parent, params byte[][] recs) =>
            new[] { Record(".", self, Sector, true, xa), Record("..", parent, Sector, true, xa) }.Concat(recs).SelectMany(r => r).ToArray();

        var pvd = new byte[Sector];
        pvd[0] = 1;
        Encoding.ASCII.GetBytes("CD001").CopyTo(pvd, 1);
        pvd[6] = 1;
        Both32(total).CopyTo(pvd, 80);
        Record(".", root, Sector, true, false).Take(34).ToArray().CopyTo(pvd, 156);
        Put(16, pvd);
        var term = new byte[Sector];
        term[0] = 255;
        Encoding.ASCII.GetBytes("CD001").CopyTo(term, 1);
        Put(17, term);

        Put(root, Dir(root, root, Record("BG", bg, Sector, true, xa), Record("SLUS_206.75;1", elfAt, (uint)elf.Length, false, xa)));
        Put(bg, Dir(bg, root, Record("0MOD", mod, Sector, true, xa), Record("DATA", data, Sector, true, xa)));
        var modules = new List<byte[]> { Record("PADMAN.IRX;1", padAt, (uint)padman.Length, false, xa), Record("SIO2MAN.IRX;1", sioAt, (uint)sio2.Length, false, xa) };
        for (var i = 0; i < extraModules; i++) modules.Add(Record($"MOD{i:D5}.IRX;1", sioAt, 1, false, xa));
        Put(mod, Dir(mod, bg, modules.ToArray()));
        Put(data, Dir(data, bg));
        Put(elfAt, elf);
        Put(padAt, padman);
        Put(sioAt, sio2);

        var path = Path.Combine(_dir, "Dark Alliance II (USA).iso");
        File.WriteAllBytes(path, image);
        return path;
    }

    private string Irx(string name, int size)
    {
        var b = Enumerable.Range(0, size).Select(i => (byte)(i * 13 + size)).ToArray();
        new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' }.CopyTo(b, 0);
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, b);
        return path;
    }

    private string Lump(string name, int entries, byte fill)
    {
        var dataStart = 4 + entries * 64;
        var b = new byte[dataStart + entries * 100];
        BitConverter.GetBytes(entries).CopyTo(b, 0);
        for (var i = 0; i < entries; i++)
        {
            Encoding.ASCII.GetBytes($"entry{i}.vif").CopyTo(b, 4 + i * 64);
            BitConverter.GetBytes(dataStart + i * 100).CopyTo(b, 4 + i * 64 + 56);
            BitConverter.GetBytes(100).CopyTo(b, 4 + i * 64 + 60);
            for (var k = 0; k < 100; k++) b[dataStart + i * 100 + k] = (byte)(fill + k);
        }

        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, b);
        return path;
    }

    private string Vat(string name)
    {
        var b = new byte[0x100000 + 4 * Sector];
        new Random(5).NextBytes(b);
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, b);
        return path;
    }

    /// <summary>The names, sectors and sizes in BG/0MOD of an image.</summary>
    private static List<(string Name, uint Lba, uint Size)> Folder(string iso)
    {
        var img = File.ReadAllBytes(iso);
        uint U(int at) => BitConverter.ToUInt32(img, at);
        List<(string, uint, uint)> Records(uint lba)
        {
            var list = new List<(string, uint, uint)>();
            var at = (int)lba * Sector;
            var end = at + Sector;
            while (at < end && img[at] != 0)
            {
                var len = img[at];
                list.Add((Encoding.ASCII.GetString(img, at + 33, img[at + 32]), U(at + 2), U(at + 10)));
                at += len;
            }

            return list;
        }

        var root = U(16 * Sector + 156 + 2);
        var bg = Records(root).First(r => r.Item1 == "BG").Item2;
        var mod = Records(bg).First(r => r.Item1 == "0MOD").Item2;
        return Records(mod).Skip(2).ToList();
    }

    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Test]
    public void Build_AddsTheFiles_LeavesTheOriginal_AndCanRunAgain()
    {
        var iso = MakeIso();
        var original = Sha(iso);
        var inputs = new[]
        {
            new Da2IsoInput(Irx("MTAPMAN.IRX", 7000), "MTAPMAN.IRX"),
            new Da2IsoInput(Lump("HUMAN.LMP", 3, 10), "HUMAN.LMP"),
            new Da2IsoInput(Vat("MOUTH.VAT"), "DA1MOUTH.VAT"),
        };

        var result = Da2ThreePlayerIso.Build(iso, inputs);
        Assert.That(result.Copied && result.Changed, Is.True);
        Assert.That(result.OutputPath, Is.EqualTo(Path.Combine(_dir, "Dark Alliance II (USA) [3P].iso")));
        Assert.That(Sha(iso), Is.EqualTo(original), "the original is never written");

        var folder = Folder(result.OutputPath);
        Assert.That(folder.Select(r => r.Name), Is.EqualTo(new[]
            { "DA1MOUTH.VAT;1", "HUMAN.LMP;1", "MTAPMAN.IRX;1", "PADMAN.IRX;1", "SIO2MAN.IRX;1" }));
        var img = File.ReadAllBytes(result.OutputPath);
        foreach (var input in inputs)
        {
            var r = folder.Single(x => x.Name == input.NameOnDisc + ";1");
            Assert.That(img.Skip((int)r.Lba * Sector).Take((int)r.Size), Is.EqualTo(File.ReadAllBytes(input.SourcePath)));
        }

        Assert.That(BitConverter.ToUInt32(img, 16 * Sector + 80) * Sector, Is.EqualTo(img.Length), "volume size");
        Assert.That(img[16 * Sector + 84], Is.EqualTo((byte)(img.Length / Sector >> 24)), "big-endian copy of the size");

        var again = Da2ThreePlayerIso.Build(iso, inputs);
        Assert.That(again.Changed || again.Copied, Is.False);
        Assert.That(again.Log, Has.Some.Contains("Nothing to do"));
    }

    [Test]
    public void ARecordOfTheSameName_IsPointedAtTheNewCopy()
    {
        var iso = MakeIso();
        var lump = Lump("DWARF.LMP", 2, 1);
        var first = Da2ThreePlayerIso.Build(iso, new[] { new Da2IsoInput(lump, "DWARF.LMP") });
        var before = Folder(first.OutputPath);

        Lump("DWARF.LMP", 4, 77);
        var second = Da2ThreePlayerIso.Build(iso, new[] { new Da2IsoInput(lump, "DWARF.LMP") });
        Assert.That(second.Changed, Is.True);
        var after = Folder(second.OutputPath);
        Assert.That(after.Count, Is.EqualTo(before.Count), "replaced, not added");
        var r = after.Single(x => x.Name == "DWARF.LMP;1");
        Assert.That(r.Lba, Is.GreaterThan(before.Single(x => x.Name == "DWARF.LMP;1").Lba));
        Assert.That(File.ReadAllBytes(second.OutputPath).Skip((int)r.Lba * Sector).Take((int)r.Size), Is.EqualTo(File.ReadAllBytes(lump)));
        Assert.That(after.Where(x => x.Name != "DWARF.LMP;1"), Is.EqualTo(before.Where(x => x.Name != "DWARF.LMP;1")));
    }

    [Test]
    public void RecordsWithoutXaData_Work_AndTheExecutableCanBeRead()
    {
        var iso = MakeIso(xa: false);
        var result = Da2ThreePlayerIso.Build(iso, new[] { new Da2IsoInput(Irx("MTAPMAN.IRX", 900), "MTAPMAN.IRX") });
        Assert.That(Folder(result.OutputPath).Select(r => r.Name), Does.Contain("MTAPMAN.IRX;1"));

        var elf = Da2ThreePlayerIso.ReadRootFile(iso, "SLUS_206.75");
        Assert.That(elf, Is.Not.Null);
        Assert.That(elf!.Length, Is.EqualTo(5000));
        Assert.That(elf[100], Is.EqualTo(42));
        Assert.That(Da2ThreePlayerIso.ReadRootFile(iso, "SLES_506.72"), Is.Null);
    }

    [Test]
    public void TheTuningOpensTheExecutableOnTheImage_WithoutReadingAllOfIt()
    {
        var standIn = MakeIso();
        var ex = Assert.Throws<NotSupportedException>(() => Da2Executable.Open(standIn));
        Assert.That(ex!.Message, Does.Contain("B0859096"), "the image's executable isn't the game's: says why");

        var noise = Path.Combine(_dir, "noise.bin");
        File.WriteAllBytes(noise, Enumerable.Range(0, 70000).Select(i => (byte)(i * 31 + 7)).ToArray());
        Assert.Throws<NotSupportedException>(() => Da2Executable.Open(noise));

        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "SLUS_206.75"))) dir = dir.Parent;
        if (dir == null) Assert.Ignore("Test_files/SLUS_206.75 (Dark Alliance II, USA) was not found: the real executable part is skipped.");
        var real = File.ReadAllBytes(Path.Combine(dir!.FullName, "Test_files", "SLUS_206.75"));
        var iso = MakeIso(executable: real);
        var exe = Da2Executable.Open(iso);
        Assert.That(exe.Source, Does.StartWith(iso).And.EndWith("(SLUS_206.75)"));
        Assert.That(exe.GetXpTable()[1], Is.EqualTo(1000));
        Assert.That(Da2Executable.Open(Path.Combine(dir.FullName, "Test_files", "SLUS_206.75")).HasChanges, Is.False);
    }

    [Test]
    public void BadInputs_AreRefusedBeforeAnythingIsWritten()
    {
        var iso = MakeIso();
        var output = Da2ThreePlayerIso.DefaultOutputPath(iso);
        void Refused(params Da2IsoInput[] inputs)
        {
            Assert.Throws<InvalidDataException>(() => Da2ThreePlayerIso.Build(iso, inputs));
            Assert.That(File.Exists(output), Is.False);
        }

        var notElf = Path.Combine(_dir, "NOTELF.IRX");
        File.WriteAllBytes(notElf, new byte[100]);
        var notLump = Path.Combine(_dir, "BAD.LMP");
        File.WriteAllBytes(notLump, new byte[200]);
        var irx = Irx("MTAPMAN.IRX", 500);

        Refused(new Da2IsoInput(notElf, "MTAPMAN.IRX"));
        Refused(new Da2IsoInput(notLump, "HUMAN.LMP"));
        Refused(new Da2IsoInput(irx, "TOOLONGNAME.IRX"));
        Refused(new Da2IsoInput(irx, "MTAPMAN.IRX"), new Da2IsoInput(irx, "mtapman.irx"));
        Refused(new Da2IsoInput(Path.Combine(_dir, "missing.lmp"), "MISSING.LMP"));
        Assert.Throws<InvalidDataException>(() => Da2ThreePlayerIso.Build(iso, new[] { new Da2IsoInput(irx, "MTAPMAN.IRX") }, iso));
    }

    [Test]
    public void AFullDirectory_IsReported()
    {
        var iso = MakeIso(extraModules: 29);   // 34 bytes left in the 0MOD sector, a new record needs 60
        Assert.Throws<InvalidDataException>(() =>
            Da2ThreePlayerIso.Build(iso, new[] { new Da2IsoInput(Irx("MTAPMAN.IRX", 500), "MTAPMAN.IRX") }));
    }
}
