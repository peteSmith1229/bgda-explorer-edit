using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Scripting;
using System;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Checks script editing against the tavern's level script in
/// Test_files/TAVERN.GOB (skipped when that file is not present).
/// </summary>
[TestFixture]
public class EditableScriptTests
{
    private const int HeaderSize = 0x60;
    private const int DataStartField = 0x10;
    private const int DataEndField = 0x14;
    private const string ScriptEntry = "script.scr";

    private string _gobPath = "";
    private byte[] _stock = Array.Empty<byte>();

    [OneTimeSetUp]
    public void ReadTavernScript()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "TAVERN.GOB")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Ignore("Test_files/TAVERN.GOB was not found above the test directory.");
        }

        _gobPath = Path.Combine(dir!.FullName, "Test_files", "TAVERN.GOB");
        _stock = Entry(OpenTavern(new GobFile(EngineVersion.DarkAlliance, _gobPath)), ScriptEntry);
    }

    private static LmpFile OpenTavern(GobFile gob)
    {
        var lmp = gob.Directory["tavern.lmp"];
        lmp.ReadDirectory();
        return lmp;
    }

    private static byte[] Entry(LmpFile lmp, string name)
    {
        var entry = lmp.Directory[name];
        return lmp.FileData.AsSpan(entry.StartOffset, entry.Length).ToArray();
    }

    private static int Field(byte[] scr, int field) => BitConverter.ToInt32(scr, HeaderSize + field);

    private static ScriptCall Call(EditableScript script, string name, string? function = null) =>
        script.Calls.First(c => c.Name == name && (function == null || c.Function == function));

    private static int CountDifferences(byte[] a, byte[] b) =>
        a.Zip(b).Count(p => p.First != p.Second) + Math.Abs(a.Length - b.Length);

    [Test]
    public void Tavern_EveryCallIsRecognised()
    {
        var script = EditableScript.Load(_stock);

        Assert.That(script.Calls, Has.Count.EqualTo(265));
        Assert.That(script.Calls.All(c => c.IsKnown), Is.True, "every engine function's arguments are known");
        Assert.That(script.Functions, Has.Count.EqualTo(34));
        Assert.That(Call(script, "startDialog").ToString(), Is.EqualTo("startDialog(\"Alyth\", \"bartend.dbin\", 27)"));
        Assert.That(Call(script, "givePlayerExp", "hoochieReward").Arguments[0].Value, Is.EqualTo(750));
        Assert.That(Call(script, "givePlayerItem", "hoochieReward").Arguments[0].Text, Is.EqualTo("ring+protection+1"));
    }

    [Test]
    public void NumberEdit_ChangesOnlyThatValue()
    {
        var script = EditableScript.Load(_stock);
        var gold = Call(script, "givePlayerGold", "alythRescueReward").Arguments[0];
        Assert.That(gold.Value, Is.EqualTo(250));

        script.SetNumber(gold, 5000);
        var edited = script.ToArray();

        Assert.That(edited, Has.Length.EqualTo(_stock.Length));
        Assert.That(CountDifferences(_stock, edited), Is.InRange(1, 4));
        Assert.That(ScriptRewardScanner.Scan(edited).Count(r => r.IntValue == 5000), Is.EqualTo(1));
        Assert.That(Call(EditableScript.Load(edited), "givePlayerGold", "alythRescueReward").Arguments[0].Value,
            Is.EqualTo(5000));
    }

    [Test]
    public void TextEdit_ThatFits_IsWrittenInPlace()
    {
        var script = EditableScript.Load(_stock);
        var item = Call(script, "givePlayerItem", "hoochieReward").Arguments[0];
        var offset = item.Value;
        Assert.That(script.InPlaceLength(item), Is.EqualTo(19));

        script.SetText(item, "ring+protection+2");
        Assert.That(CountDifferences(_stock, script.ToArray()), Is.EqualTo(1), "one letter changed");

        script.SetText(item, "dagger");
        script.SetText(item, "ring+protection+3"); // back to the length it had: the room is still there
        var edited = script.ToArray();

        Assert.That(edited, Has.Length.EqualTo(_stock.Length));
        Assert.That(item.Value, Is.EqualTo(offset));
        Assert.That(ScrDecoder.Decode(edited, 0, edited.Length).StringTable[offset], Is.EqualTo("ring+protection+3"));
        Assert.That(ScriptRewardScanner.Scan(edited).Any(r => r.ItemName == "ring+protection+3"), Is.True);
    }

    [Test]
    public void TextEdit_ThatDoesNotFit_IsAddedAndTheCodeStaysPut()
    {
        var script = EditableScript.Load(_stock);
        var call = Call(script, "startDialog");
        var speaker = call.Arguments[0];
        var dataLength = script.DataLength;

        script.SetText(speaker, "AlythTheMagnificent");
        var edited = script.ToArray();

        // 19 letters, a terminator, word padding: 20 bytes at the end of the data.
        Assert.That(edited, Has.Length.EqualTo(_stock.Length + 20));
        Assert.That(speaker.Value, Is.EqualTo(dataLength));
        Assert.That(Field(edited, DataEndField), Is.EqualTo(Field(_stock, DataEndField) + 20));
        Assert.That(Field(edited, 0x00), Is.EqualTo(Field(edited, DataEndField)), "the work area follows the data");
        Assert.That(Field(edited, 0x18), Is.EqualTo(Field(_stock, 0x18) + 20));
        Assert.That(Field(edited, 0x24), Is.EqualTo(Field(_stock, 0x24)), "function table unmoved");

        // After the header: the tables, the code and the old data.
        var bodyStart = HeaderSize + 0x30;
        var dataEnd = HeaderSize + Field(_stock, DataStartField) + dataLength;
        Assert.That(CountDifferences(_stock[bodyStart..dataEnd], edited[bodyStart..dataEnd]), Is.InRange(1, 4),
            "only the call's pushed offset changed before the new text");

        var stock = ScrDecoder.Decode(_stock, 0, _stock.Length);
        var decoded = ScrDecoder.Decode(edited, 0, edited.Length);
        Assert.That(decoded.instructions, Has.Count.EqualTo(stock.instructions.Count));
        Assert.That(decoded.StringTable[speaker.Value], Is.EqualTo("AlythTheMagnificent"));
        Assert.That(stock.StringTable.All(s => decoded.StringTable[s.Key] == s.Value), Is.True, "old text kept");
        Assert.That(decoded.Disassemble(), Does.Contain("startDialog AlythTheMagnificent, bartend.dbin, 27"));
        Assert.That(ScriptRewardScanner.Scan(edited), Has.Count.EqualTo(ScriptRewardScanner.Scan(_stock).Count));
        Assert.That(ScriptCallSiteScanner.Scan(edited), Has.Count.EqualTo(ScriptCallSiteScanner.Scan(_stock).Count));

        // Editing the added text again resizes it rather than adding another copy.
        script.SetText(speaker, "AlythTheMagnificentAndWise");
        Assert.That(script.Length, Is.EqualTo(_stock.Length + 28));
        Assert.That(speaker.Value, Is.EqualTo(dataLength));
        Assert.That(EditableScript.Load(script.ToArray()).Calls.First(c => c.Address == call.Address).Arguments[0].Text,
            Is.EqualTo("AlythTheMagnificentAndWise"));
    }

    [Test]
    public void TextOtherCodeMayShare_IsNeverOverwritten()
    {
        var script = EditableScript.Load(_stock);
        // Offset 0 is also a small number other instructions load.
        var variable = Call(script, "setv", "acceptGarikMission").Arguments[0];
        Assert.That(variable.Value, Is.EqualTo(0));
        Assert.That(script.InPlaceLength(variable), Is.EqualTo(0));

        script.SetText(variable, "acceptGarikQuest");
        var edited = script.ToArray();

        Assert.That(variable.Value, Is.Not.EqualTo(0));
        Assert.That(ScrDecoder.Decode(edited, 0, edited.Length).StringTable[0], Is.EqualTo("acceptGarikMission"));
    }

    [Test]
    public void InvalidEdits_AreRejectedWithoutChangingTheScript()
    {
        var script = EditableScript.Load(_stock);
        var item = Call(script, "givePlayerItem").Arguments[0];
        var gold = Call(script, "givePlayerGold").Arguments[0];

        Assert.Throws<ArgumentException>(() => script.SetText(item, ""));
        Assert.Throws<ArgumentException>(() => script.SetText(item, "café"));
        Assert.Throws<ArgumentException>(() => script.SetText(item, new string('a', EditableScript.MaxTextLength + 1)));
        Assert.Throws<InvalidOperationException>(() => script.SetNumber(item, 5));
        Assert.Throws<ArgumentException>(() => EditableScript.Load(_stock).SetNumber(gold, 5));

        Assert.That(script.ToArray(), Is.EqualTo(_stock));
    }

    [Test]
    public void UnreadableScripts_AreRefused()
    {
        Assert.That(EditableScript.TryLoad(Array.Empty<byte>(), out _, out _), Is.False);
        Assert.That(EditableScript.TryLoad(_stock.AsSpan(0, 0x2000).ToArray(), out _, out _), Is.False);

        var unknownInstruction = (byte[])_stock.Clone();
        BitConverter.GetBytes(0x99).CopyTo(unknownInstruction, HeaderSize + Field(_stock, 0x0C));
        Assert.That(EditableScript.TryLoad(unknownInstruction, out _, out var error), Is.False);
        Assert.That(error, Does.Contain("0x99"));
    }

    [Test]
    public void EditedScript_IsSavedIntoTheGob()
    {
        var gob = new GobFile(EngineVersion.DarkAlliance, _gobPath);
        var lmp = OpenTavern(gob);
        var script = EditableScript.Load(Entry(lmp, ScriptEntry));
        script.SetNumber(Call(script, "givePlayerExp", "hoochieReward").Arguments[0], 1500);
        script.SetText(Call(script, "startDialog").Arguments[0], "AlythTheMagnificent");
        var edited = script.ToArray();

        lmp.ReplaceEntry(ScriptEntry, edited);
        var packed = GobWriter.TryPatchInPlace(gob) ?? GobWriter.Pack(gob);

        var saved = new GobFile(EngineVersion.DarkAlliance, "saved.gob", packed);
        var savedLmp = OpenTavern(saved);
        Assert.That(Entry(savedLmp, ScriptEntry), Is.EqualTo(edited));
        foreach (var name in lmp.Directory.Keys.Where(n => n != ScriptEntry))
        {
            Assert.That(Entry(savedLmp, name), Is.EqualTo(Entry(lmp, name)), $"{name} is unchanged");
        }

        var reread = EditableScript.Load(Entry(savedLmp, ScriptEntry));
        Assert.That(Call(reread, "givePlayerExp", "hoochieReward").Arguments[0].Value, Is.EqualTo(1500));
        Assert.That(Call(reread, "startDialog").Arguments[0].Text, Is.EqualTo("AlythTheMagnificent"));
    }
}
