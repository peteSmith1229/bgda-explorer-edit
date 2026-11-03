using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Pnach;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Kill XP, XP bonuses, drops, the gold / item value curves, potions and Ransack of Dark Alliance II,
/// against the real executable (Test_files/SLUS_206.75, skipped when it isn't there). The expected numbers
/// are the game's own (read from its code) and the goblin loot measured in the emulator (2026-10-06).
/// </summary>
[TestFixture]
public class Da2RulesTests
{
    private byte[] _stock = Array.Empty<byte>();

    [OneTimeSetUp]
    public void FindExecutable()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "SLUS_206.75"))) dir = dir.Parent;
        if (dir == null) Assert.Ignore("Test_files/SLUS_206.75 (Dark Alliance II, USA) was not found above the test directory.");
        _stock = File.ReadAllBytes(Path.Combine(dir!.FullName, "Test_files", "SLUS_206.75"));
    }

    private Da2Executable Exe() => Da2Executable.FromBytes(_stock, "SLUS_206.75");

    private static readonly Dictionary<string, float> StockValues = new()
    {
        ["xp.exponent"] = 1.5f, ["xp.share"] = 0.75f, ["xp.shareEach"] = 0.6f, ["xp.lyranExponent"] = 1.5f,
        ["xp.lyranShareEach"] = 0.5f, ["xp.trollExponent"] = 1.5f, ["xp.trollShareEach"] = 0.5f,
        ["xp.bonusVahnAdrianna"] = 0.03f, ["xp.bonusDrizztArtemis"] = 0.09f, ["xp.itemBonus"] = 0.05f,
        ["loot.lowChance"] = 0.5f, ["loot.treasurePerPoint"] = 0.01f, ["loot.twoPiles"] = 4536 * 100f / 65536,
        ["loot.threePiles"] = 1536 * 100f / 65536, ["loot.pileBase"] = 40, ["loot.pileSpread"] = 40, ["loot.goldFactor"] = 0.1f,
        ["loot.itemValue"] = 200, ["loot.itemFloor"] = 0.8f, ["loot.itemSpread"] = 0.4f, ["loot.qualityLow"] = 0.5f,
        ["loot.qualityHigh"] = 3,
        ["gold.act1PerLevel"] = 0.3f, ["gold.act1Base"] = 0.7f, ["gold.act2Offset"] = 10, ["gold.act3Offset"] = 25,
        ["gold.act23PerLevel"] = 0.35f, ["gold.act23Base"] = 0.7f, ["gold.act4Offset"] = 30, ["gold.act4PerLevel"] = 0.35f,
        ["gold.act4Base"] = 0.7f, ["gold.scale"] = 1, ["gold.extreme"] = 60, ["gold.extremeAct1"] = 30,
        ["value.act1PerLevel"] = 0.2f, ["value.act1Base"] = 0.7f, ["value.act2Offset"] = 10, ["value.act2PerLevel"] = 0.3f,
        ["value.act2Base"] = 0.7f, ["value.act3Offset"] = 25, ["value.act4Offset"] = 63, ["value.act34PerLevel"] = 0.3f,
        ["value.act34Base"] = 0.7f, ["value.scale"] = 1.8f, ["value.extreme"] = 60, ["value.extremeAct1"] = 30,
        ["potion.healing"] = 30000 * 100f / 65536, ["potion.recall"] = 5536 * 100f / 65536, ["potion.perLevel"] = 0.1f,
        ["potion.act2"] = 0.6f, ["potion.act3"] = 1.2f, ["potion.act4"] = 2, ["potion.extreme"] = 10,
        ["potion.healSpread"] = 0.2f, ["potion.healLesser"] = 1, ["potion.healNormal"] = 2, ["potion.rejuvSpread"] = 0.2f,
        ["potion.rejuvLesser"] = 1, ["potion.rejuvNormal"] = 2,
        ["ransack.chance"] = 0.1f, ["ransack.chancePerRank"] = 0.03f, ["ransack.goldPerRank"] = 0.2f, ["ransack.qualityPerRank"] = 1.2f,
        ["loot.twoPlayerPiles"] = 1.5f,
    };

    [Test]
    public void EverySetting_IsTheGamesCode_WithTheGamesValue()
    {
        var exe = Exe();
        Assert.That(Da2Executable.Settings.Select(s => s.Key), Is.EquivalentTo(StockValues.Keys));
        Assert.That(Da2Executable.Settings.Select(s => s.Key).Distinct().Count(), Is.EqualTo(Da2Executable.Settings.Count));
        foreach (var s in Da2Executable.Settings)
        {
            Assert.That(exe.IsSettingCode(s), Is.True, s.Key);
            Assert.That(exe.GetSetting(s), Is.EqualTo(StockValues[s.Key]).Within(1e-5f), s.Key);
            // a stock value is held exactly as it is (so an unedited box writes nothing)
            Assert.That(Da2Executable.RepresentableSetting(s, exe.GetSetting(s)), Is.EqualTo(exe.GetSetting(s)).Within(1e-6f), s.Key);
        }

        var words = Da2Executable.Settings.SelectMany(Da2Executable.SettingWords).ToList();
        Assert.That(words.Distinct().Count(), Is.EqualTo(words.Count), "no two settings share a word");
    }

    [Test]
    public void NoGroupOfTheMod_WritesARuleADropOrTheQualityTable()
    {
        var mod = PnachFile.Parse(Da2ThreePlayerMod.StockText).Groups.SelectMany(g => g.Patches).Select(p => p.Address & 0x0FFFFFFCu).ToHashSet();
        Assert.That(mod.Count, Is.GreaterThan(20000));
        foreach (var s in Da2Executable.Settings)
            Assert.That(Da2Executable.SettingWords(s).Where(mod.Contains), Is.Empty, s.Key);
        foreach (var d in Exe().GetDrops())
        {
            foreach (var c in d.Constants.Where(c => c != null))
                Assert.That(mod.Contains(c!.Lui) || (c.Ori != 0 && mod.Contains(c.Ori)), Is.False, $"{d.Creature} 0x{d.Call:X6}");
        }

        for (uint a = Da2Executable.QualityTable; a < Da2Executable.QualityTable + 144; a += 4)
            Assert.That(mod.Contains(a), Is.False);
    }

    [Test]
    public void Settings_AreWrittenAsTheTuningGroup_AndReadBack()
    {
        var exe = Exe();
        exe.SetSettings(new Dictionary<string, float>
        {
            ["xp.shareEach"] = 0.8f, ["xp.exponent"] = 1.4f, ["loot.twoPiles"] = 20, ["loot.threePiles"] = 5,
            ["potion.healing"] = 40, ["loot.twoPlayerPiles"] = 1.25f, ["loot.pileBase"] = 60, ["gold.scale"] = 2
        });
        Assert.That(exe.GetSetting(Da2Executable.Setting("xp.shareEach")), Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(exe.GetSetting(Da2Executable.Setting("xp.exponent")), Is.EqualTo(Da2Executable.Representable(1.4f, false)));
        Assert.That(exe.GetSetting(Da2Executable.Setting("loot.twoPiles")), Is.EqualTo(20f).Within(0.002f));
        Assert.That(exe.GetSetting(Da2Executable.Setting("loot.twoPlayerPiles")), Is.EqualTo(1.25f));
        Assert.That(exe.GetSetting(Da2Executable.Setting("loot.pileBase")), Is.EqualTo(60f));
        Assert.That(exe.DescribeChanges(), Does.Contain("XP rules").And.Contain("loot rules").And.Contain("potions"));

        var changed = exe.ChangedWords().Select(w => w.Address).ToHashSet();
        var expected = new[] { "xp.shareEach", "xp.exponent", "loot.twoPiles", "loot.threePiles", "potion.healing", "loot.twoPlayerPiles", "loot.pileBase", "gold.scale" }
            .SelectMany(k => Da2Executable.SettingWords(Da2Executable.Setting(k))).ToHashSet();
        Assert.That(changed.IsSubsetOf(expected), Is.True);

        var again = Exe();
        again.ApplyTuningGroup(exe.ToTuningGroup()!);
        Assert.That(again.GetSettings(), Is.EqualTo(exe.GetSettings()));
    }

    [Test]
    public void WritingBackTheValuesRead_ChangesNothing()
    {
        // what the window does on a save with no edits: every value read is written back
        var exe = Exe();
        exe.SetSettings(exe.GetSettings());
        foreach (var d in exe.GetDrops())
        {
            foreach (var c in d.Constants.Where(c => c != null)) exe.SetDropValue(c!, exe.ConstantValue(c!));
        }

        var table = exe.GetQualityTable();
        for (var r = 0; r < table.Length; r++) exe.SetQualityRow(r, table[r][0], table[r][1]);
        Assert.That(exe.ChangedWords(), Is.Empty);
        Assert.That(exe.ToTuningGroup(), Is.Null);
    }

    [Test]
    public void ChangedCode_IsNoticed()
    {
        var exe = Exe();
        Assert.That(exe.UnexpectedCode(), Is.Null, "the stock executable");
        exe.SetSetting(Da2Executable.Setting("xp.share"), 0.5f);
        Assert.That(exe.UnexpectedCode(), Is.Null, "a value WorldExplorer writes");
        exe.SetWord(0x0025E140, 0x00000000);           // the lui of the kill XP share made a nop
        Assert.That(exe.UnexpectedCode(), Does.Contain("Share of the kill"));
        Assert.Throws<InvalidOperationException>(() => exe.GetSetting(Da2Executable.Setting("xp.share")));
    }

    [Test]
    public void Settings_RefuseValuesOutOfRange_AndWriteNothing()
    {
        var exe = Exe();
        Assert.Throws<ArgumentException>(() => exe.SetSettings(new Dictionary<string, float> { ["xp.share"] = 0.5f, ["xp.exponent"] = 9 }));
        Assert.Throws<ArgumentException>(() => exe.SetSettings(new Dictionary<string, float> { ["loot.twoPiles"] = 2, ["loot.threePiles"] = 3 }));
        Assert.Throws<ArgumentException>(() => exe.SetSettings(new Dictionary<string, float> { ["potion.healing"] = 50, ["potion.recall"] = 60 }));
        Assert.Throws<ArgumentException>(() => exe.SetSettings(new Dictionary<string, float> { ["loot.pileSpread"] = 0 }));
        Assert.That(exe.HasChanges, Is.False);
    }

    [Test]
    public void KillXp_IsTheGamesSum()
    {
        var exe = Exe();
        // a goblin in Trollbark Forest: XP rating (1 + 3) x 7 x 1.0, x 3.5 with two players counted = 98
        Assert.That(exe.KillXp(98, 2, 2), Is.EqualTo(154), "two level-1 players: floor(floor(floor(49^1.5) x 0.75) x 0.6)");
        Assert.That(exe.KillXp(98, 2, 1), Is.EqualTo(257), "a lone survivor keeps the whole share");
        Assert.That(exe.KillXp(98, 3, 3), Is.EqualTo(83), "three level-1 players");
        Assert.That(exe.KillXp(98, 2, 2, kind: 2), Is.EqualTo(171), "a troll's own code: half each");
        Assert.That(Da2Executable.EffectiveXpBonus(0.09f), Is.EqualTo(1f / 11));
        Assert.That(Da2Executable.EffectiveXpBonus(0.03f), Is.EqualTo(1f / 33));
        Assert.That(Da2Executable.EffectiveXpBonus(0.05f), Is.EqualTo(1f / 20));
        Assert.That(Da2Executable.EffectiveXpBonus(2f), Is.EqualTo(1f));
    }

    [Test]
    public void LevelGoldAndItemValue_MatchTheMeasuredGoblinLoot()
    {
        var exe = Exe();
        var trollb = exe.GetLevels().Single(l => l.Name == "trollb");
        // a goblin pile with two players: (40 .. 79) x (2.0 x 2) x 0.1 x gold value
        var normal = exe.LevelLootValue(true, trollb.MonsterLevel, trollb.Flags, 2);
        var extreme = exe.LevelLootValue(true, trollb.MonsterLevel, trollb.Flags, 3);
        Assert.That(normal, Is.EqualTo(0.5f).Within(1e-5f));
        Assert.That((int)(40 * 0.4f * normal), Is.EqualTo(8));
        Assert.That((int)(79 * 0.4f * normal), Is.EqualTo(15), "Hard, Trollbark: 8-15 gold a pile (measured)");
        Assert.That((int)(40 * 0.4f * extreme), Is.EqualTo(224));
        Assert.That((int)(79 * 0.4f * extreme), Is.EqualTo(442), "Extreme, Trollbark: 224-442 a pile (measured)");
        var lyran2 = exe.GetLevels().Single(l => l.Name == "lyran2");
        Assert.That((int)(40 * 0.4f * exe.LevelLootValue(true, lyran2.MonsterLevel, lyran2.Flags, 2)), Is.EqualTo(39), "act 2: 39-77 (measured)");
        Assert.That(exe.LevelLootValue(false, trollb.MonsterLevel, trollb.Flags, 1), Is.EqualTo((1 * 0.2f + 0.7f) * 0.5f * 1.8f).Within(1e-5f));
    }

    [Test]
    public void Drops_AreEveryCreaturesOwn()
    {
        var exe = Exe();
        var drops = exe.GetDrops();
        Assert.That(drops.Count, Is.EqualTo(109));
        var goblin = drops.Single(d => d.Creature == "Goblin");
        Assert.That(goblin.Call, Is.EqualTo(0x147294u));
        Assert.That(goblin.Values, Is.EqualTo(new[] { 2f, 0.6f, 1.8f, 0.15f }));
        Assert.That(goblin.Constants[0], Is.Null, "the goblin's gold scale is its throw speed 2.0 (one constant for both)");
        Assert.That(goblin.Constants.Skip(1).All(c => c != null), Is.True);
        var ogre = drops.Single(d => d.Creature == "Ogre");
        Assert.That(ogre.Values, Is.EqualTo(new[] { 4f, 1f, 6f, 0.5f }));
        Assert.That(ogre.Constants.All(c => c != null), Is.True);
        Assert.That(drops.Count(d => d.Creature == "Argesh"), Is.EqualTo(3));
        Assert.That(drops.Where(d => d.Creature == "Argesh").Select(d => d.Constants[2]).Distinct().Count(), Is.EqualTo(1),
            "Argesh's three drops share one quality constant");
        foreach (var d in drops)
        {
            for (var i = 0; i < 4; i++)
            {
                if (d.Constants[i] is { } c)
                {
                    Assert.That(Da2Executable.DropArgument(c), Is.EqualTo(i), $"{d.Creature} {i}");
                    Assert.That(exe.ConstantValue(c), Is.EqualTo(d.Values[i]), $"{d.Creature} {i}");
                }
            }
        }

        // the DA1 bosses' values are unchanged by the mod's groups: every editable constant is a lui/ori pair
        var constants = drops.SelectMany(d => d.Constants).Where(c => c != null).Distinct().ToList();
        Assert.That(constants.Count, Is.EqualTo(196));
    }

    [Test]
    public void DropValues_ChangeOnlyTheirWords()
    {
        var exe = Exe();
        var goblin = exe.GetDrops().Single(d => d.Creature == "Goblin");
        exe.SetDropValue(goblin.Constants[3]!, 0.3f);
        exe.SetDropValue(goblin.Constants[2]!, 2.5f);
        Assert.That(exe.GetDrops().Single(d => d.Creature == "Goblin").Values, Is.EqualTo(new[] { 2f, 0.6f, 2.5f, 0.3f }));
        Assert.That(exe.ChangedWords().Select(w => w.Address),
            Is.SubsetOf(new[] { goblin.Constants[3]!.Lui, goblin.Constants[3]!.Ori, goblin.Constants[2]!.Lui, goblin.Constants[2]!.Ori }));
        Assert.That(exe.DescribeChanges(), Does.Contain("2 creature drop values"));
        Assert.Throws<ArgumentException>(() => exe.SetDropValue(goblin.Constants[2]!, 12));
        Assert.Throws<ArgumentException>(() => exe.SetDropValue(new Da2Constant(0x264A20, 0), 1));
        // the chance a goblin kill drops an item / a potion with two players counted: 0.15 halved, doubled
        Assert.That(Exe().DropChances(0.15f, true), Is.EqualTo((0.15f, 0.3f)));
        Assert.That(Exe().DropChances(0.15f, false), Is.EqualTo((0.075f, 0.15f)));
    }

    [Test]
    public void QualityTable_IsInterpolatedAsTheGameDoes()
    {
        var exe = Exe();
        var t = exe.GetQualityTable();
        Assert.That(t.Length, Is.EqualTo(12));
        Assert.That(t[1], Is.EqualTo(new[] { 0.9f, 0.09f, 0.01f }));
        Assert.That(t[9], Is.EqualTo(new[] { 0f, 0f, 1f }));
        var goblin = exe.QualityChances(1.8f);           // measured: 74 % / 22.6 % / 3.4 %
        Assert.That(goblin.Poor, Is.EqualTo(0.74f).Within(1e-4f));
        Assert.That(goblin.Plain, Is.EqualTo(0.226f).Within(1e-4f));
        Assert.That(goblin.Good, Is.EqualTo(0.034f).Within(1e-4f));
        exe.SetQualityRow(2, 0.5f, 0.3f);
        Assert.That(exe.GetQualityTable()[2], Is.EqualTo(new[] { 0.5f, 0.3f, 1f - 0.5f - 0.3f }));
        Assert.Throws<ArgumentException>(() => exe.SetQualityRow(3, 0.7f, 0.6f));
        Assert.That(exe.DescribeChanges(), Does.Contain("loot rules"));
    }

    [Test]
    public void LevelBudget_HasEveryLevel()
    {
        Assert.That(Da2LevelBudget.All.Length, Is.EqualTo(107));
        var trollb = Da2LevelBudget.Find("TROLLB")!;
        Assert.That((trollb.Small, trollb.Creatures, trollb.HeapFreeKb), Is.EqualTo((1057, 59, 7420)));
        var mausoleum = Da2LevelBudget.Find("mausoleu")!;
        Assert.That(mausoleum.CreatureSized, Is.EqualTo(185), "182 creatures + the three players at load");
        Assert.That(mausoleum.Room, Is.EqualTo(15));
        Assert.That(Da2LevelBudget.Find("kpn1")!.HeapFreeKb, Is.EqualTo(347), "the tightest level (build 43 report)");
        Assert.That(Da2LevelBudget.Find("nowhere"), Is.Null);
        var exe = Exe();
        Assert.That(Da2LevelBudget.All.Select(b => b.Level), Is.SubsetOf(exe.GetLevels().Select(l => l.Name)));
    }
}
