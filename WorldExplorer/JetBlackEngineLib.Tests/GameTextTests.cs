using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The game's text files (.uni) and GTEXT.LMP: a small file built here, and the game's own GTEXT.LMP from
/// Test_files (those tests are skipped when that file is not present).
/// </summary>
[TestFixture]
public class GameTextTests
{
    private byte[]? _stock;

    [OneTimeSetUp]
    public void FindGameText()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "GTEXT.LMP")))
        {
            dir = dir.Parent;
        }

        if (dir != null) _stock = File.ReadAllBytes(Path.Combine(dir.FullName, "Test_files", "GTEXT.LMP"));
    }

    private byte[] Stock()
    {
        if (_stock == null) Assert.Ignore("Test_files/GTEXT.LMP was not found above the test directory.");
        return _stock!;
    }

    private static List<(string Name, byte[] Data)> Entries(byte[] lmpBytes)
    {
        var lmp = new LmpFile(EngineVersion.DarkAlliance, "gtext.lmp", lmpBytes, 0, lmpBytes.Length);
        lmp.ReadDirectory();
        return lmp.Directory.Values
            .Select(e => (e.Name, lmpBytes.AsSpan(e.StartOffset, e.Length).ToArray()))
            .ToList();
    }

    /// <summary>A GTEXT.LMP of five small files: an item name in each language and a gendered modifier.</summary>
    private static byte[] SmallGameText()
    {
        var texts = new[]
        {
            new List<(string, string)> { ("longsword_name", "Long Sword"), ("it_flaming", "Flaming"), ("odd", "x") },
            new List<(string, string)> { ("longsword_name", "Epée longue_F"), ("it_flaming_F", "enflammée"), ("odd", "x") },
            new List<(string, string)> { ("longsword_name", "Langschwert_N"), ("it_flaming", "des Aufflammens"), ("odd", "x") },
            new List<(string, string)> { ("longsword_name", "Spada Lunga_f"), ("odd", "x") },
            new List<(string, string)> { ("longsword_name", "Espada larga_f"), ("odd", "x") }
        };
        return LmpWriter.PackEntries(EngineVersion.DarkAlliance,
            GameText.Files.Select((f, i) => (f.File, new UniTextFile(texts[i]).ToArray())).ToList());
    }

    // ═══════════════════════════════ .uni files ═══════════════════════════════

    [Test]
    public void UniFile_IsLaidOutAsTheGameReadsIt()
    {
        var file = new UniTextFile(new List<(string, string)> { ("ab", "Hi"), ("abc", "é"), ("k", "") });
        var bytes = file.ToArray();

        var expected = new List<byte>();
        expected.AddRange(Encoding.ASCII.GetBytes("ab\0\0"));            // key, NUL, a NUL more to an even offset
        expected.AddRange(Encoding.Unicode.GetBytes("Hi\0"));
        expected.AddRange(Encoding.ASCII.GetBytes("abc\0"));             // already even
        expected.AddRange(Encoding.Unicode.GetBytes("é\0"));
        expected.AddRange(Encoding.ASCII.GetBytes("k\0"));
        expected.AddRange(Encoding.Unicode.GetBytes("\0"));
        expected.AddRange(new byte[4]);                                  // the end
        Assert.That(bytes, Is.EqualTo(expected.ToArray()));

        var back = UniTextFile.Read(bytes);
        Assert.That(back.Entries, Is.EqualTo(file.Entries));
        Assert.That(back.Get("abc"), Is.EqualTo("é"));
        Assert.That(back.Get("missing"), Is.Null);

        back.Set("abc", "changed");
        back.Set("new", "added");
        Assert.That(back.Entries.Select(e => e.Key), Is.EqualTo(new[] { "ab", "abc", "k", "new" }));
        Assert.That(back.Get("abc"), Is.EqualTo("changed"));
    }

    [Test]
    public void UniFile_RefusesWhatTheGameCantRead()
    {
        Assert.Throws<InvalidDataException>(() => UniTextFile.Read(Encoding.ASCII.GetBytes("key-without-end")));
        Assert.Throws<InvalidDataException>(() => UniTextFile.Read(Encoding.ASCII.GetBytes("ab\0\0H\0i\0")));
        Assert.Throws<ArgumentException>(() => new UniTextFile(new List<(string, string)> { ("", "x") }).ToArray());
        Assert.Throws<ArgumentException>(() => new UniTextFile(new List<(string, string)> { ("café", "x") }).ToArray());
        Assert.Throws<ArgumentException>(() => new UniTextFile(new List<(string, string)> { ("k", "a\0b") }).ToArray());
    }

    // ═══════════════════════════════ GTEXT.LMP, small ═══════════════════════════════

    [Test]
    public void ItemNames_AreShownWithoutTheirGenderMark()
    {
        var text = GameText.Read(SmallGameText());
        Assert.That(text.GetItemName("longsword"), Is.EqualTo("Long Sword"));
        Assert.That(text.GetItemName("longsword", 1), Is.EqualTo("Epée longue"));
        Assert.That(text.Get("longsword_name", 1), Is.EqualTo("Epée longue_F"));
        Assert.That(text.GetItemName("longsword", 2), Is.EqualTo("Langschwert"));
        Assert.That(text.GetItemName("dagger"), Is.Null);
        Assert.That(GameText.WithoutGender("x"), Is.EqualTo("x"));
        Assert.That(GameText.WithoutGender("Hammer_M"), Is.EqualTo("Hammer"));
        Assert.That(text.InAllLanguages("odd"), Is.True);
        Assert.That(text.InAllLanguages("it_flaming"), Is.False);
        Assert.That(text.Changed, Is.False);
    }

    [Test]
    public void NewItemName_IsAddedToEveryLanguageWithAMasculineMark()
    {
        var small = SmallGameText();
        var text = GameText.Read(small);
        text.SetItemName("flametongue", "Flame Tongue");
        Assert.That(text.Changed, Is.True);

        var saved = text.ToArray();
        var back = GameText.Read(saved);
        Assert.That(back.Get("flametongue_name"), Is.EqualTo("Flame Tongue"));
        Assert.That(back.Get("flametongue_name", 1), Is.EqualTo("Flame Tongue_M"));
        Assert.That(back.Get("flametongue_name", 2), Is.EqualTo("Flame Tongue_M"));
        Assert.That(back.Get("flametongue_name", 3), Is.EqualTo("Flame Tongue_m"));
        Assert.That(back.Get("flametongue_name", 4), Is.EqualTo("Flame Tongue_m"));
        for (var language = 0; language < GameText.Files.Count; language++)
        {
            Assert.That(back.GetItemName("flametongue", language), Is.EqualTo("Flame Tongue"));
            Assert.That(back.Get("longsword_name", language), Is.EqualTo(GameText.Read(small).Get("longsword_name", language)));
        }

        // the new key is added after the file's own entries, which keep their bytes
        var before = Entries(small);
        var after = Entries(saved);
        Assert.That(after.Select(e => e.Name), Is.EqualTo(before.Select(e => e.Name)));
        for (var i = 0; i < before.Count; i++)
        {
            var old = before[i].Data;
            Assert.That(after[i].Data.Take(old.Length - 4).ToArray(), Is.EqualTo(old.Take(old.Length - 4).ToArray()));
        }
    }

    [Test]
    public void RenamingAnItem_ChangesOnlyTheEnglishName()
    {
        var small = SmallGameText();
        var text = GameText.Read(small);
        text.SetItemName("longsword", "Longsword");
        var saved = text.ToArray();

        var before = Entries(small);
        var after = Entries(saved);
        Assert.That(after[0].Data, Is.Not.EqualTo(before[0].Data));
        for (var i = 1; i < before.Count; i++) Assert.That(after[i].Data, Is.EqualTo(before[i].Data));
        Assert.That(GameText.Read(saved).GetItemName("longsword"), Is.EqualTo("Longsword"));
        Assert.That(GameText.Read(saved).GetItemName("longsword", 1), Is.EqualTo("Epée longue"));

        // setting a name to what it already is changes nothing
        var again = GameText.Read(small);
        again.SetItemName("longsword", "Long Sword");
        Assert.That(again.Changed, Is.False);
        Assert.That(again.ToArray(), Is.EqualTo(small));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(" Sword")]
    [TestCase("Sword_M")]
    [TestCase("Tab\there")]
    [TestCase("Sword ★")]
    [TestCase("Thirty-two characters long name!")]
    public void BadItemNames_AreRefused(string name)
    {
        Assert.That(GameText.CheckItemName(name), Is.Not.Null);
        var text = GameText.Read(SmallGameText());
        Assert.Throws<ArgumentException>(() => text.SetItemName("longsword", name));
        Assert.That(text.Changed, Is.False);
    }

    [TestCase("Flame Tongue")]
    [TestCase("Clangeddin's Fist")]
    [TestCase("Épée (+3)")]
    [TestCase("Twinkle (+5 Defending Scimitar)")]
    public void GoodItemNames_AreAccepted(string name) =>
        Assert.That(GameText.CheckItemName(name), Is.Null);

    [Test]
    public void NotGameText_IsRefused()
    {
        Assert.Throws<InvalidDataException>(() => GameText.Read(LmpWriter.PackEntries(EngineVersion.DarkAlliance,
            new List<(string, byte[])> { ("gtext.uni", new UniTextFile(new List<(string, string)>()).ToArray()) })));
    }

    // ═══════════════════════════════ GTEXT.LMP, the game's own ═══════════════════════════════

    [Test]
    public void StockGameText_IsReadAndWrittenBackUnchanged()
    {
        var stock = Stock();
        var entries = Entries(stock);
        Assert.That(entries.Select(e => e.Name), Is.EqualTo(GameText.Files.Select(f => f.File)));

        // every language's file round-trips byte for byte, and so does the archive packed from them
        var repacked = entries.Select(e => (e.Name, UniTextFile.Read(e.Data).ToArray())).ToList();
        for (var i = 0; i < entries.Count; i++) Assert.That(repacked[i].Item2, Is.EqualTo(entries[i].Data), entries[i].Name);
        Assert.That(LmpWriter.PackEntries(EngineVersion.DarkAlliance, repacked), Is.EqualTo(stock));

        var text = GameText.Read(stock);
        Assert.That(text.Changed, Is.False);
        Assert.That(text.ToArray(), Is.EqualTo(stock));
    }

    [Test]
    public void StockGameText_HasTheGamesItemNames()
    {
        var text = GameText.Read(Stock());
        Assert.That(text.GetItemName("longsword"), Is.EqualTo("Long Sword"));
        Assert.That(text.Get("longsword_name", 1), Is.EqualTo("Epée longue_F"));
        Assert.That(text.GetItemName("longsword", 1), Is.EqualTo("Epée longue"));
        Assert.That(text.GetItemName("twinkle"), Is.EqualTo("Twinkle (+5 Defending Scimitar)"));
        Assert.That(text.GetItemName("scimitar"), Is.EqualTo("Scimitar"), "the first of the two scimitar_name entries");
        Assert.That(text.Get("it_flaming"), Is.EqualTo("Flaming"));
        Assert.That(text.Get("it_flaming", 1), Is.Null, "French has only it_flaming_F and it_flaming_M");
        Assert.That(text.Get("it_flaming_M", 1), Is.EqualTo("enflammé"));

        foreach (var name in GameText.ReadyMadeItemNames)
        {
            Assert.That(text.InAllLanguages(GameText.NameKey(name)), Is.True, name);
        }

        Assert.That(text.GetItemName("flametongue"), Is.EqualTo("Flame Tongue"));
        Assert.That(text.GetItemName("lthammer"), Is.EqualTo("Lt. Hammer"));
        Assert.That(text.GetItemName("frostbrand", 2), Is.Not.Null);
    }

    [Test]
    public void StockGameText_TakesANewItemName()
    {
        var stock = Stock();
        var text = GameText.Read(stock);
        text.SetItemName("orcbane", "Orcbane");
        text.SetItemName("longsword", "Longsword");
        var saved = text.ToArray();

        var before = Entries(stock);
        var after = Entries(saved);
        var back = GameText.Read(saved);
        for (var language = 0; language < GameText.Files.Count; language++)
        {
            var old = UniTextFile.Read(before[language].Data).Entries;
            var now = UniTextFile.Read(after[language].Data).Entries;
            Assert.That(now.Count, Is.EqualTo(old.Count + 1), GameText.Files[language].File);
            Assert.That(now[^1], Is.EqualTo(("orcbane_name", "Orcbane" + GameText.NewNameMarks[language])));
            var changed = Enumerable.Range(0, old.Count).Where(i => now[i] != old[i]).Select(i => now[i].Key).ToList();
            Assert.That(changed, Is.EqualTo(language == 0 ? new[] { "longsword_name" } : Array.Empty<string>()));
            Assert.That(back.GetItemName("orcbane", language), Is.EqualTo("Orcbane"));
        }

        Assert.That(back.GetItemName("longsword", 1), Is.EqualTo("Epée longue"));
    }
}
