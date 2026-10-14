using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.DataContainers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The game's animation hash, the play modes, and the catalog of animations each cutscene cast member can
/// play. The archives here are made up, with the names of the game's own animations.
/// </summary>
[TestFixture]
public class CutsceneAnimationTests
{
    /// <summary>An archive's directory as the game stores it: a count, then 64-byte entries that start with the name.</summary>
    private static byte[] ArchiveDirectory(params string[] names)
    {
        var data = new byte[4 + 64 * names.Length];
        BitConverter.GetBytes(names.Length).CopyTo(data, 0);
        for (var i = 0; i < names.Length; i++) Encoding.ASCII.GetBytes(names[i]).CopyTo(data, 4 + 64 * i);
        return data;
    }

    /// <summary>A GOB: 40-byte entries {name, offset at 0x20, length at 0x24} up to an empty name, then the archives.</summary>
    private static byte[] Gob(params (string Name, byte[] Data)[] archives)
    {
        var offset = 0x28 * (archives.Length + 1);
        var gob = new byte[offset + archives.Sum(a => a.Data.Length)];
        for (var i = 0; i < archives.Length; i++)
        {
            Encoding.ASCII.GetBytes(archives[i].Name).CopyTo(gob, 0x28 * i);
            BitConverter.GetBytes(offset).CopyTo(gob, 0x28 * i + 0x20);
            BitConverter.GetBytes(archives[i].Data.Length).CopyTo(gob, 0x28 * i + 0x24);
            archives[i].Data.CopyTo(gob, offset);
            offset += archives[i].Data.Length;
        }

        return gob;
    }

    private static readonly (string Archive, string[] Entries)[] Archives =
    {
        ("human.lmp", new[] { "human.tex", "nw_deatha.anm", "nw_idleb.anm", "nw_getup.anm", "man_b_death1.anm" }),
        ("dwarf.lmp", new[] { "nw_deatha.anm", "nw_getup.anm", "nw_idleb.anm", "dwf_b_death1.anm" }),
        ("elf.lmp", new[] { "nw_getup.anm", "nw_deatha.anm", "nw_idleb.anm", "elf_b_death1.anm", "elf.vif" }),
        ("thief1.lmp", new[] { "tf1_01a.anm", "thv1_headcrak.anm", "walk.anm" }),
        ("thief2.lmp", new[] { "tf2_01.anm", "thv2_idle.anm", "walk.anm" }),
        ("karne.lmp", new[] { "krn_attack1.anm", "walk.anm" }),
        ("tkarne.lmp", new[] { "karn04a.anm", "karn04b.anm", "krn_idle1.anm", "walk.anm" })
    };

    private static CutsceneAnimationCatalog Catalog() =>
        new(Archives.Select(a => (a.Archive, (IEnumerable<string>)a.Entries)));

    private static int Hash(string file) => CutsceneAnimations.Hash(file);

    // The hashes the stock intro.cut stores for these animations.
    [TestCase("thv1_headcrak.anm", -833693697)]
    [TestCase("nw_deatha.anm", 1614449972)]
    [TestCase("nw_idleb.anm", 364606483)]
    [TestCase("nw_getup.anm", 1915192063)]
    [TestCase("tf1_01a.anm", -1540156448)]
    [TestCase("tf2_01.anm", 76565302)]
    [TestCase("karn04a.anm", 859885586)]
    [TestCase("karn04b.anm", -982220174)]
    [TestCase("cw2_04.anm", -1168743449)]
    [TestCase("ÿwalk.anm", -1974586700)] // the game adds bytes as signed numbers
    public void Hash_IsTheGames(string file, int hash)
    {
        Assert.That(CutsceneAnimations.Hash(file), Is.EqualTo(hash));
    }

    [Test]
    public void Names_HaveTheirExtensionAdded()
    {
        Assert.That(CutsceneAnimations.FileName("nw_deatha"), Is.EqualTo("nw_deatha.anm"));
        Assert.That(CutsceneAnimations.FileName("nw_deatha.anm"), Is.EqualTo("nw_deatha.anm"));
        Assert.That(CutsceneAnimations.FileName("NW_DEATHA.ANM"), Is.EqualTo("NW_DEATHA.ANM"));
        Assert.That(CutsceneAnimations.ShortName("nw_deatha.anm"), Is.EqualTo("nw_deatha"));
        Assert.That(CutsceneAnimations.ShortName("walk"), Is.EqualTo("walk"));
        Assert.That(Hash("nw_deatha"), Is.Not.EqualTo(Hash("nw_deatha.anm")), "the game hashes the name with .anm");
    }

    [Test]
    public void Modes_AreTheSixTheGameKnows()
    {
        Assert.That(CutsceneAnimations.Modes.Select(m => m.Number), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
        Assert.That(CutsceneAnimations.ModeOf(1)!.Text, Is.EqualTo("1 · Play and hold"));
        Assert.That(CutsceneAnimations.ModeOf(1)!.Description, Does.Contain("falling"));
        Assert.That(CutsceneAnimations.ModeOf(6), Is.Null);
        Assert.That(CutsceneAnimations.ModeOf(-1), Is.Null);
    }

    [Test]
    public void Players_CanPlayWhatTheirCharacterHas()
    {
        var choices = Catalog().ChoicesFor("Player3", Array.Empty<int>());

        Assert.That(choices.Archives, Is.EqualTo(new[] { "human.lmp", "dwarf.lmp", "elf.lmp" }));
        Assert.That(choices.Animations.Select(a => a.Name),
            Is.EqualTo(new[] { "nw_deatha", "nw_getup", "nw_idleb", "dwf_b_death1", "elf_b_death1", "man_b_death1" }),
            "those every character has first");
        Assert.That(choices.Animations.Select(a => a.InAll), Is.EqualTo(new[] { true, true, true, false, false, false }));
        Assert.That(choices.Find("ELF_B_DEATH1.anm")!.Archives, Is.EqualTo(new[] { "elf.lmp" }));
        Assert.That(choices.Find("nw_deatha")!.Archives, Is.EqualTo(new[] { "human.lmp", "dwarf.lmp", "elf.lmp" }));
        Assert.That(choices.Find("thv1_headcrak"), Is.Null);
        Assert.That(choices.Source, Does.Contain("human.lmp, dwarf.lmp or elf.lmp"));
        Assert.That(Catalog().ChoicesFor("Player", new[] { Hash("walk.anm") }).Archives, Is.EqualTo(choices.Archives),
            "a player's keyframes don't change which characters he can be");

        // Only the names the game makes stand-ins for.
        Assert.That(Catalog().ChoicesFor("Player4", Array.Empty<int>()).Archives, Is.Empty);
        Assert.That(Catalog().ChoicesFor("player", Array.Empty<int>()).Archives, Is.Empty);

        var someFound = new CutsceneAnimationCatalog(Archives.Where(a => a.Archive != "dwarf.lmp")
            .Select(a => (a.Archive, (IEnumerable<string>)a.Entries))).ChoicesFor("Player", Array.Empty<int>());
        Assert.That(someFound.Archives, Is.EqualTo(new[] { "human.lmp", "elf.lmp" }));
        Assert.That(someFound.Source, Does.Contain("weren't found"));
    }

    [Test]
    public void Others_UseTheArchiveTheirKeyframesPlayFrom()
    {
        var catalog = Catalog();

        // A townsperson class (Thief1, Thief2) plays from its class's archive, whatever its keyframes play.
        var thief = catalog.ChoicesFor("Thief1", new[] { Hash("walk.anm") });
        Assert.That(thief.Archives, Is.EqualTo(new[] { "thief1.lmp" }));
        Assert.That(thief.Animations.Select(a => a.Name), Is.EqualTo(new[] { "tf1_01a", "thv1_headcrak", "walk" }));
        Assert.That(thief.Animations.All(a => a.InAll), Is.True);
        Assert.That(thief.Source, Does.Contain("Thief1 class"));
        Assert.That(thief.IsCertain, Is.True);
        var thief2 = catalog.ChoicesFor("Thief2", new[] { Hash("thv1_headcrak.anm"), Hash("tf1_01a.anm") });
        Assert.That(thief2.Archives, Is.EqualTo(new[] { "thief2.lmp" }), "given Thief1's animations, Thief2 still has his own");
        Assert.That(thief2.IsCertain, Is.True);
        Assert.That(catalog.ChoicesFor("Player", Array.Empty<int>()).IsCertain, Is.True);

        // The Karne class (a monster) has karne.lmp, but the intro's Karne plays tkarne.lmp's animations.
        var karne = catalog.ChoicesFor("Karne", new[] { Hash("karn04a.anm"), Hash("karn04b.anm"), Hash("walk.anm") });
        Assert.That(karne.Archives, Is.EqualTo(new[] { "tkarne.lmp" }));
        Assert.That(karne.Source, Does.Contain("its keyframes play"));
        Assert.That(karne.IsCertain, Is.False);
        Assert.That(catalog.ChoicesFor("Karne", new[] { Hash("walk.anm") }).Archives, Is.EqualTo(new[] { "karne.lmp" }),
            "on a tie, the class's archive");
        var lilivarra = new CutsceneAnimationCatalog(new (string, IEnumerable<string>)[]
        {
            ("lilivarra_fx.lmp", new[] { "glow.anm", "walk.anm" }),
            ("lilvarra.lmp", new[] { "lil_cast.anm", "walk.anm" })
        }).ChoicesFor("Lilivarra", new[] { Hash("walk.anm") });
        Assert.That(lilivarra.Archives, Is.EqualTo(new[] { "lilvarra.lmp" }),
            "the class's archive (lilvarra.lmp) before one named like the cast member");
        Assert.That(catalog.ChoicesFor("Karne", new[] { Hash("karn04a.anm"), 12345 }).Source, Does.Contain("1 of the 2"));
        Assert.That(catalog.ChoicesFor("Karne", Array.Empty<int>()).Archives, Is.EqualTo(new[] { "karne.lmp" }),
            "with no keyframes to go by, the class's archive");

        // A name that isn't a class: the archive with its animations, else one named after it.
        Assert.That(catalog.ChoicesFor("Bandit", new[] { Hash("tf2_01.anm") }).Archives, Is.EqualTo(new[] { "thief2.lmp" }));
        Assert.That(catalog.ChoicesFor("TKarne", Array.Empty<int>()).Archives, Is.EqualTo(new[] { "tkarne.lmp" }));
        Assert.That(catalog.ChoicesFor("Bandit", Array.Empty<int>()), Is.SameAs(CutsceneAnimationChoices.None));
        Assert.That(catalog.ChoicesFor("Bandit", new[] { 12345 }), Is.SameAs(CutsceneAnimationChoices.None));
        Assert.That(catalog.ChoicesFor("Bandit", new[] { Hash("walk.anm") }), Is.SameAs(CutsceneAnimationChoices.None),
            "walk.anm alone doesn't say which of four archives");
        Assert.That(catalog.ChoicesFor("Thief9", new[] { Hash("walk.anm") }).Archives, Is.EqualTo(new[] { "thief1.lmp" }),
            "an archive named like it");

        Assert.That(CutsceneAnimationCatalog.Empty.ChoicesFor("Player", Array.Empty<int>()), Is.SameAs(CutsceneAnimationChoices.None));
        Assert.That(CutsceneAnimationCatalog.Empty.ChoicesFor("Thief1", new[] { Hash("walk.anm") }),
            Is.SameAs(CutsceneAnimationChoices.None));
    }

    [Test]
    public void Hashes_AreNamed()
    {
        var catalog = Catalog();
        var thief2 = catalog.ChoicesFor("Thief2", Array.Empty<int>());

        Assert.That(catalog.NameOf(Hash("walk.anm"), thief2), Is.EqualTo("walk"));
        Assert.That(catalog.NameOf(Hash("thv1_headcrak.anm"), thief2), Is.EqualTo("thv1_headcrak"), "found in another archive");
        Assert.That(catalog.NameOf(12345, thief2), Is.Null);
        Assert.That(catalog.FileNamesOf(Hash("walk.anm")), Is.EqualTo(new[] { "walk.anm" }));
        Assert.That(catalog.FileNamesOf(12345), Is.Empty);
        Assert.That(catalog.FindName("THV1_HEADCRAK.ANM"), Is.EqualTo("thv1_headcrak"), "spelled as the archive spells it");
        Assert.That(catalog.FindName("walk"), Is.EqualTo("walk"));
        Assert.That(catalog.FindName("nw_death"), Is.Null);
        Assert.That(catalog.AnimationsIn("ELF.LMP"),
            Is.EqualTo(new[] { "nw_getup.anm", "nw_deatha.anm", "nw_idleb.anm", "elf_b_death1.anm" }), "in the archive's order");
        Assert.That(catalog.AnimationsIn("nobody.lmp"), Is.Null);
        Assert.That(catalog.Archives.Count, Is.EqualTo(7));
    }

    [Test]
    public void Catalog_ReadsTheGobAndTheArchiveFiles()
    {
        var folder = Path.Combine(Path.GetTempPath(), "cutscene-animations-" + Guid.NewGuid().ToString("N"));
        var other = Path.Combine(folder, "other");
        Directory.CreateDirectory(other);
        try
        {
            File.WriteAllBytes(Path.Combine(folder, "HUMAN.LMP"), ArchiveDirectory("human.tex", "nw_deatha.anm"));
            File.WriteAllBytes(Path.Combine(folder, "thief1.lmp"), ArchiveDirectory("thv1_headcrak.anm"));
            File.WriteAllBytes(Path.Combine(folder, "broken.lmp"), BitConverter.GetBytes(5000));  // 5,000 entries promised, none there
            File.WriteAllBytes(Path.Combine(folder, "empty.lmp"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(folder, "notes.txt"), ArchiveDirectory("notes.anm"));
            File.WriteAllBytes(Path.Combine(other, "human.lmp"), ArchiveDirectory("other.anm"));
            File.WriteAllBytes(Path.Combine(other, "elf.lmp"), ArchiveDirectory("nw_deatha.anm", "elf_b_death1.anm"));

            // A GOB whose archive has an unsaved deletion and an unsaved addition.
            var gob = new GobFile(EngineVersion.DarkAlliance, "test.gob",
                Gob(("thief1.lmp", ArchiveDirectory("tf1_01a.anm", "walk.anm")), ("env.lmp", ArchiveDirectory("env.tex"))));
            var thief1 = gob.Directory["thief1.lmp"];
            thief1.ReadDirectory();
            thief1.DeleteEntry("walk.anm");
            thief1.AddEntry("tf1_01b.anm", new byte[4]);

            var catalog = CutsceneAnimationCatalog.Read(gob,
                new[] { folder, null, "", other, folder + Path.DirectorySeparatorChar, Path.Combine(folder, "missing") });

            Assert.That(catalog.Archives.OrderBy(a => a, StringComparer.Ordinal),
                Is.EqualTo(new[] { "elf.lmp", "env.lmp", "human.lmp", "thief1.lmp" }), "broken files and other types skipped");
            Assert.That(catalog.AnimationsIn("thief1.lmp"), Is.EqualTo(new[] { "tf1_01a.anm", "tf1_01b.anm" }),
                "the GOB's archive, as it would be saved, before the file of the same name");
            Assert.That(catalog.AnimationsIn("human.lmp"), Is.EqualTo(new[] { "nw_deatha.anm" }), "the first folder first");
            Assert.That(catalog.AnimationsIn("elf.lmp"), Is.EqualTo(new[] { "nw_deatha.anm", "elf_b_death1.anm" }));
            Assert.That(catalog.AnimationsIn("env.lmp"), Is.Empty);
            Assert.That(catalog.ChoicesFor("Player2", Array.Empty<int>()).Animations.Select(a => (a.Name, a.InAll)),
                Is.EqualTo(new[] { ("nw_deatha", true), ("elf_b_death1", false) }));

            Assert.That(CutsceneAnimationCatalog.Read(null, Array.Empty<string>()).Archives, Is.Empty);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
