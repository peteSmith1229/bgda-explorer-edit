using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.DataContainers;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Checks the cutscene subtitle files (.uni) against the five in Test_files/CUTTOWN.GOB
/// (skipped when that file is not present).
/// </summary>
[TestFixture]
public class CutsceneSubtitleTests
{
    private LmpFile _lmp = null!;

    [OneTimeSetUp]
    public void OpenCutTown()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "CUTTOWN.GOB")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Ignore("Test_files/CUTTOWN.GOB was not found above the test directory.");
        }

        var gob = new GobFile(EngineVersion.DarkAlliance, Path.Combine(dir!.FullName, "Test_files", "CUTTOWN.GOB"));
        _lmp = gob.Directory["cuttown.lmp"];
        _lmp.ReadDirectory();
    }

    private byte[] Entry(string name)
    {
        var entry = _lmp.Directory[name];
        return _lmp.FileData.AsSpan(entry.StartOffset, entry.Length).ToArray();
    }

    [TestCase("cuttown_cut.uni")]
    [TestCase("cuttown_cut_fr.uni")]
    [TestCase("cuttown_cut_ge.uni")]
    [TestCase("cuttown_cut_it.uni")]
    [TestCase("cuttown_cut_sp.uni")]
    public void StockFiles_AreReadAndWrittenBackUnchanged(string name)
    {
        var stock = Entry(name);
        var file = CutsceneSubtitles.Read(stock);

        Assert.That(file.Subtitles.Select(s => s.Key), Is.EqualTo(Enumerable.Range(1, 14).Select(i => $"introcut_text{i}")));
        Assert.That(file.Subtitles.All(s => s.Text.Length > 0), Is.True);
        Assert.That(file.ToArray(), Is.EqualTo(stock));
    }

    [Test]
    public void EnglishTexts_AreTheIntrosLines()
    {
        var english = CutsceneSubtitles.Read(Entry("cuttown_cut.uni"));
        Assert.That(english.Subtitles[0].Text, Is.EqualTo("Easy pickings, eh, Karne?"));
        Assert.That(english.Subtitles[5].Text, Is.EqualTo("Yes, but take the gold.  Leave the rest for the watch."));
        Assert.That(english.IndexOf("introcut_text3"), Is.EqualTo(2));
        Assert.That(english.IndexOf("introcut_text99"), Is.EqualTo(-1));
        Assert.That(english.ShownBy("introcut_text1", 14).Count(), Is.EqualTo(14));
        Assert.That(english.ShownBy("introcut_text13", 5).Select(s => s.Key),
            Is.EqualTo(new[] { "introcut_text13", "introcut_text14" }), "the file ends first");
        Assert.That(english.ShownBy("nothing", 3), Is.Empty);

        var french = CutsceneSubtitles.Read(Entry("cuttown_cut_fr.uni"));
        Assert.That(french.Subtitles[2].Text, Is.EqualTo("Halte ! Qui va là ?"));
    }

    [TestCase("cuttown_cut.uni", "English")]
    [TestCase("cuttown_cut_fr.uni", "French")]
    [TestCase("cuttown_cut_ge.uni", "German")]
    [TestCase("cuttown_cut_it.uni", "Italian")]
    [TestCase("cuttown_cut_sp.uni", "Spanish")]
    public void Languages_AreTheGamesSuffixes(string name, string language) =>
        Assert.That(CutsceneSubtitles.LanguageOf(name), Is.EqualTo(language));

    [Test]
    public void EditedFile_IsLaidOutLikeTheGamesFiles()
    {
        var file = CutsceneSubtitles.Read(Entry("cuttown_cut.uni"));
        file.Subtitles[0] = file.Subtitles[0] with { Text = "Ça va? Odd length." };
        file.Subtitles.RemoveRange(2, 12);
        file.Subtitles.Add(new CutsceneSubtitle("k", "Short key"));
        var data = file.ToArray();

        var back = CutsceneSubtitles.Read(data);
        Assert.That(back.Subtitles, Is.EqualTo(file.Subtitles));
        Assert.That(data.AsSpan(data.Length - 4).ToArray(), Is.EqualTo(new byte[4]), "four zero bytes end the file");
        // Each text starts at an even offset, after its key and NUL (and one more NUL when needed).
        var at = 0;
        foreach (var (key, text) in back.Subtitles)
        {
            Assert.That(Encoding.ASCII.GetString(data, at, key.Length), Is.EqualTo(key));
            var start = at + key.Length + 1;
            if ((start & 1) != 0)
            {
                Assert.That(data[start], Is.EqualTo(0));
                start++;
            }

            Assert.That(Encoding.Unicode.GetString(data, start, text.Length * 2), Is.EqualTo(text));
            at = start + text.Length * 2 + 2;
        }

        Assert.That(at, Is.EqualTo(data.Length - 4));
        Assert.That(CutsceneSubtitles.Create().ToArray(), Is.EqualTo(new byte[4]));
    }

    [Test]
    public void BadKeysTextsAndFiles_AreRefused()
    {
        Assert.That(CutsceneSubtitles.CheckKey("introcut_text1"), Is.Null);
        Assert.That(CutsceneSubtitles.CheckKey(""), Is.Not.Null);
        Assert.That(CutsceneSubtitles.CheckKey("intro text"), Is.Not.Null);
        Assert.That(CutsceneSubtitles.CheckKey(new string('k', 40)), Is.Not.Null);
        Assert.That(CutsceneSubtitles.CheckText("Halt!"), Is.Null);
        Assert.That(CutsceneSubtitles.CheckText(""), Is.Not.Null);
        Assert.That(CutsceneSubtitles.CheckText("a\0b"), Is.Not.Null);

        var file = CutsceneSubtitles.Create();
        file.Subtitles.Add(new CutsceneSubtitle("bad key", "text"));
        Assert.Throws<ArgumentException>(() => file.ToArray());

        var stock = Entry("cuttown_cut.uni");
        Assert.Throws<InvalidDataException>(() => CutsceneSubtitles.Read(stock.AsSpan(0, 40).ToArray()));     // text cut off
        Assert.Throws<InvalidDataException>(() => CutsceneSubtitles.Read(Encoding.ASCII.GetBytes("introcut_text1"))); // key cut off
        Assert.That(CutsceneSubtitles.Read(new byte[4]).Subtitles, Is.Empty);
    }
}
