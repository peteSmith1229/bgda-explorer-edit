using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Pnach;
using System;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The Dark Alliance II tuning tables against the real executable, Test_files/SLUS_206.75 (USA v1.03,
/// CRC B0859096); skipped when that file is not there. Numbers checked here come from the game's code
/// and from the three-player mod's notes (measured in the emulator and in the game).
/// </summary>
[TestFixture]
public class Da2ExecutableTests
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

    [Test]
    public void TheGame_IsRecognisedByItsCrc_AndOthersAreNot()
    {
        Assert.That(Da2Executable.PcsxCrc(_stock), Is.EqualTo(0xB0859096u));
        Assert.That(Exe().HasChanges, Is.False);
        Assert.That(Exe().ToTuningGroup(), Is.Null);

        var other = (byte[])_stock.Clone();
        other[0x2000] ^= 1;
        Assert.Throws<NotSupportedException>(() => Da2Executable.FromBytes(other, "edited"));
        Assert.Throws<NotSupportedException>(() => Da2Executable.FromBytes(new byte[100], "nothing"));
    }

    [Test]
    public void XpTable_IsTheGames()
    {
        var xp = Exe().GetXpTable();
        Assert.That(xp.Length, Is.EqualTo(40));
        Assert.That(xp.Take(6), Is.EqualTo(new[] { 0, 1000, 3000, 6000, 10000, 15000 }));
        Assert.That(xp[15], Is.EqualTo(120000), "a new Drizzt starts at level 16 with 122,000 XP");
        Assert.That(xp[19], Is.EqualTo(190000), "level 20 = 1000 x 20 x 19 / 2");
        Assert.That(xp[20], Is.EqualTo(220000), "level 21: 220,000 where the pattern gives 210,000");
        Assert.That(xp[39], Is.EqualTo(795000));
    }

    [Test]
    public void StartingStats_AreTheClassesOwn()
    {
        var exe = Exe();
        int[] Row(int c) => Enumerable.Range(0, 6).Select(s => exe.GetStat(c, s)).ToArray();
        Assert.That(Row(0), Is.EqualTo(new[] { 17, 15, 14, 11, 13, 11 }), "Vahn");
        Assert.That(Row(1), Is.EqualTo(new[] { 13, 14, 13, 17, 12, 15 }), "Adrianna");
        Assert.That(Row(2), Is.EqualTo(new[] { 18, 12, 15, 10, 11, 10 }), "Kromlech");
        Assert.That(Row(3), Is.EqualTo(new[] { 13, 20, 15, 17, 17, 14 }), "Drizzt");
        Assert.That(Row(4), Is.EqualTo(new[] { 16, 10, 14, 10, 10, 11 }), "Dorn");
    }

    [Test]
    public void AbilitiesAndClassFeats_AreRead()
    {
        var exe = Exe();
        var abilities = exe.GetAbilities();
        Assert.That(abilities.Count, Is.EqualTo(102));
        var fireball = abilities.Single(a => a.Name == "fireball");
        Assert.That((fireball.Type, fireball.DamageMin, fireball.DamageMax, fireball.Energy, fireball.FeatId), Is.EqualTo((1, 1, 20, 10f, 11)));
        Assert.That((fireball.GlowR, fireball.GlowG, fireball.GlowB), Is.EqualTo((255, 200, 100)));
        Assert.That(abilities.Single(a => a.Name == "lightning").IsStream, Is.True);

        var feats = exe.GetClassFeats();
        int Count(int c) => feats.Count(f => f.ClassId == c);
        Assert.That(new[] { Count(3), Count(4), Count(5), Count(6), Count(7), Count(8), Count(9) },
            Is.EqualTo(new[] { 18, 28, 29, 34, 31, 34, 21 }), "Drizzt, Dorn, Vhaidra, Ysuran, Borador, Allessia, Artemis");
        Assert.That(feats.Where(f => f.ClassId is 0 or 1 or 2), Is.Empty);
        Assert.That(feats.All(f => Da2Names.IsFeat(f.FeatId)), Is.True, "every feat has a name");

        int[]? Special(int id) => exe.GetSpecialPrices(id);
        int[] Prices(int classId, string name) => feats.Single(f => f.ClassId == classId && f.Name == name).Prices(Special);
        Assert.That(Prices(3, "Repulsion"), Is.EqualTo(new[] { 5, 6, 8, 10, 14 }));
        Assert.That(Prices(3, "Otiluke's Icy Sphere"), Is.EqualTo(new[] { 14, 15, 17, 19, 23 }));
        Assert.That(Prices(5, "Armor Proficiency"), Is.EqualTo(new[] { 5, 6, 8 }));
        Assert.That(Prices(4, "Hero's Arm"), Is.EqualTo(new[] { 15 }));
        Assert.That(Prices(4, "Two-Weapon Fighting"), Is.EqualTo(new[] { 3, 6, 9, 12, 15 }));
        Assert.That(exe.GetSpecialPrices(8), Is.EqualTo(new[] { 1, 3, 5, 10, 18 }), "Magic Missile");
        Assert.That(exe.GetSpecialPrices(35), Is.EqualTo(new[] { 12, 18, 24, 32, 40 }), "Mordenkainen's Sword");
    }

    [Test]
    public void ItemsAndLevels_AreRead()
    {
        var exe = Exe();
        var items = exe.GetItems();
        Assert.That(items.Count, Is.EqualTo(83));
        var dagger = items.Single(i => i.Name == "dagger");
        Assert.That((dagger.Value, dagger.Weight, dagger.List), Is.EqualTo((20, 1f, 0)));
        Assert.That(items.Single(i => i.Name == "chainmail").Armour, Is.EqualTo(18));
        Assert.That(items.Single(i => i.Name == "xbow").Kind, Is.EqualTo("Crossbow"));

        var levels = exe.GetLevels();
        Assert.That(levels.Count, Is.EqualTo(108));
        var caravan = levels.Single(l => l.Name == "caravan");
        Assert.That((caravan.MonsterLevel, caravan.Act, caravan.FormulaSet, caravan.BasicItemsOnly), Is.EqualTo((1f, 1, 1, true)));
        var kpn1 = levels.Single(l => l.Name == "kpn1");
        Assert.That((kpn1.MonsterLevel, kpn1.Act, kpn1.FormulaSet, kpn1.BasicItemsOnly), Is.EqualTo((3f, 4, 4, false)));
        Assert.That(levels.Single(l => l.Name == "fwcat").MonsterLevel, Is.EqualTo(11f));
        Assert.That(levels.Single(l => l.Name == "fire2").FormulaSet, Is.EqualTo(3));
    }

    [Test]
    public void Difficulty_IsTheGamesAndEditsOnlyItsConstants()
    {
        var exe = Exe();
        var d = exe.GetDifficulty();
        Assert.That(new[] { d.EasyHpDamage, d.NormalHp, d.HardHp, d.HardDamage, d.ExtremeHp, d.ExtremeDamage, d.ExtremeLevel },
            Is.EqualTo(new[] { 0.85f, 1.2f, 1.6f, 1.3f, 1.3f, 2.3f, 30f }));
        Assert.That(new[] { d.TwoPlayerHp, d.TwoPlayerDamage, d.TwoPlayerXp }, Is.EqualTo(new[] { 1.7f, 1.4f, 3.5f }));
        Assert.That(new[] { d.BossEasy, d.BossNormal, d.BossHard, d.BossExtreme, d.BossTwoPlayer },
            Is.EqualTo(new[] { 0.7f, 1f, 1.3f, 1.8f, 1.7f }));

        d.HardHp = 2.25f;
        d.TwoPlayerXp = 5.25f;    // lui only: exact (0x40A80000)
        d.ExtremeLevel = 45f;
        exe.SetDifficulty(d);
        var back = exe.GetDifficulty();
        Assert.That((back.HardHp, back.TwoPlayerXp, back.ExtremeLevel), Is.EqualTo((2.25f, 5.25f, 45f)));
        Assert.That(exe.ChangedWords().Select(w => w.Address), Is.EquivalentTo(new uint[] { 0x24B31C, 0x24B320, 0x24B38C, 0x24B358 }));
        Assert.That(exe.ChangedWords().All(w => (w.Value & 0xFFFF0000) == (Exe().StockWord(w.Address) & 0xFFFF0000)), Is.True,
            "only the immediates change");

        Assert.That(Da2Executable.Representable(1.33f, fullPrecision: false), Is.EqualTo(1.328125f));
        d.BossNormal = 1.33f;
        exe.SetDifficulty(d);
        Assert.That(exe.GetDifficulty().BossNormal, Is.EqualTo(1.328125f));

        d.NormalHp = 0f;
        Assert.Throws<ArgumentException>(() => exe.SetDifficulty(d));
    }

    [Test]
    public void CreatureTable_MatchesTheExecutable()
    {
        var exe = Exe();
        var creatures = exe.GetCreatures();
        Assert.That(creatures.Count, Is.EqualTo(171));
        foreach (var (lui, ori, _) in Da2MonsterData.Constants)
            Assert.DoesNotThrow(() => exe.ConstantValue(new Da2Constant(lui, ori)), $"0x{lui:X6}");
        foreach (var (row, c) in Da2MonsterData.Rows.Zip(creatures))
            Assert.That(c.Values, Is.EqualTo(row.Values).Within(1e-4f), $"{row.Class}: the table's values are the executable's");
        Assert.That(Da2MonsterData.Constants.Select(c => c.Lui).Distinct().Count(), Is.EqualTo(Da2MonsterData.Constants.Length),
            "a constant is listed once");

        var bosses = exe.GetBosses();
        float Hp(string name, string note = "") => bosses.Single(b => b.Class == name && b.Note == note).Hp;
        Assert.That((Hp("Beholder"), Hp("Eldrith"), Hp("Lilivarra"), Hp("GreenDragon"), Hp("RedDragon")),
            Is.EqualTo((900f, 1500f, 1200f, 3500f, 4000f)));
        Assert.That((Hp("mordoc", "in the level shadow"), Hp("mordoc", "elsewhere")), Is.EqualTo((4500f, 3500f)));
    }

    [Test]
    public void Predictions_MatchTheNumbersMeasuredForTrollbark()
    {
        // build 48 notes: Trollbark (act 1, L 1, Normal, two players counted): Minotaur 79-112 HP, damage 15, XP 98;
        // Beholder damage up to 46, XP rating 3920.
        var exe = Exe();
        var d = exe.GetDifficulty();
        var minotaur = exe.GetCreatures().Single(c => c.Class == "Minotaur");
        var s = Da2Executable.Predict(minotaur.Values, 1f, 1, 1, twoPlayers: true, d);
        Assert.That((s.HpMin, s.HpMax, s.DamageMax, s.XpRating), Is.EqualTo((79, 112, 15, 98f)));

        var beholder = exe.GetCreatures().Single(c => c.Class == "Beholder");
        var b = Da2Executable.Predict(beholder.Values, 1f, 1, 1, twoPlayers: true, d);
        Assert.That((b.DamageMax, b.XpRating), Is.EqualTo((46, 3920f)));
    }

    [Test]
    public void Edits_BecomeATuningGroup_ThatReadsBack()
    {
        var exe = Exe();
        var xp = exe.GetXpTable();
        xp[1] = 900;
        exe.SetXpTable(xp);
        exe.SetStat(4, 0, 18);
        exe.SetAbilityEnergy(exe.GetAbilities().Single(a => a.Name == "fireball").Index, 8f, 0f);
        var sunder = exe.GetClassFeats().First(f => f.ClassId == 4 && f.FeatId == 43);
        exe.SetClassFeat(sunder.Address, 5, 3);
        exe.SetItem(exe.GetItems().Single(i => i.Name == "dagger").Index, 1f, 25, 0);
        exe.SetMonsterLevel(exe.GetLevels().Single(l => l.Name == "trollb").Index, 2f);
        var goblinish = exe.GetCreatures().First(c => c.Constants[0] != null);
        exe.SetCreatureMultiplier(goblinish.Constants[0]!, 3f);
        var beholder = exe.GetBosses().Single(b => b.Class == "Beholder");
        exe.SetBossHp(beholder.Constant, 1200f);

        var group = exe.ToTuningGroup()!;
        Assert.That(group.Name, Is.EqualTo(Da2Executable.TuningGroupName));
        Assert.That(group.Description, Does.Contain("XP per level").And.Contain("starting stats").And.Contain("1 item")
            .And.Contain("creature multipliers").And.Contain("boss hit points"));
        Assert.That(group.Patches.All(p => p.Place == 0), Is.True);

        var text = Da2ThreePlayerMod.Write(Da2ThreePlayerMod.DefaultSettings(), group).ToText();
        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(text), Da2Executable.TuningGroupName);
        var again = Exe();
        again.ApplyTuningGroup(read.Tuning!);
        Assert.That(again.ChangedWords(), Is.EqualTo(exe.ChangedWords()));
        Assert.That(again.GetXpTable()[1], Is.EqualTo(900));
        Assert.That(again.GetBosses().Single(b => b.Class == "Beholder").Hp, Is.EqualTo(1200f));
        Assert.That(again.GetClassFeats().First(f => f.ClassId == 4 && f.FeatId == 43).BaseCost, Is.EqualTo(3));

        var mod = PnachFile.Parse(Da2ThreePlayerMod.StockText).Groups;
        Assert.That(again.Conflicts(mod), Is.Empty, "the tuning group writes nothing the mod writes");

        again.ResetToStock();
        Assert.That(again.HasChanges, Is.False);
    }

    [Test]
    public void EveryEditableAddress_IsOutsideTheMod()
    {
        var exe = Exe();
        var modWords = PnachFile.Parse(Da2ThreePlayerMod.StockText).Groups.SelectMany(g => g.Patches).Select(p => p.Address).ToHashSet();
        var mine = new System.Collections.Generic.List<uint>();
        for (uint a = Da2Executable.XpTable; a < Da2Executable.XpTable + 160; a += 4) mine.Add(a);
        for (uint a = Da2Executable.StatsTable; a < Da2Executable.StatsTable + 240; a += 4) mine.Add(a);
        foreach (var a in exe.GetAbilities()) for (uint k = 0; k < Da2Executable.AbilitySize; k += 4) mine.Add(a.Address + k);
        foreach (var f in exe.GetClassFeats()) for (uint k = 0; k < Da2Executable.ClassFeatSize; k += 4) mine.Add(f.Address + k);
        foreach (var i in exe.GetItems()) for (uint k = 0; k < Da2Executable.ItemSize; k += 4) mine.Add(i.Address + k);
        foreach (var l in exe.GetLevels()) for (uint k = 0; k < Da2Executable.LevelSize; k += 4) mine.Add(l.Address + k);
        mine.AddRange(Da2MonsterData.Constants.SelectMany(c => new[] { c.Lui, c.Ori }).Where(a => a != 0));
        mine.AddRange(Da2MonsterData.BossHp.SelectMany(c => new[] { c.Lui, c.Ori }).Where(a => a != 0));
        Assert.That(mine.Where(modWords.Contains), Is.Empty);
    }

    [Test]
    public void BadValues_AreRejected()
    {
        var exe = Exe();
        var xp = exe.GetXpTable();
        xp[5] = xp[4];
        Assert.Throws<ArgumentException>(() => exe.SetXpTable(xp));
        Assert.Throws<ArgumentException>(() => exe.SetStat(3, 0, 31));
        Assert.Throws<ArgumentException>(() => exe.SetClassFeat(exe.GetClassFeats()[0].Address, 6, 1));
        Assert.Throws<ArgumentException>(() => exe.SetSpecialPrices(8, new[] { 1, 2, 3, 4, 0 }));
        Assert.Throws<ArgumentException>(() => exe.SetItem(0, 1f, 0, 0));
        Assert.Throws<ArgumentException>(() => exe.SetMonsterLevel(0, -1f));
        Assert.Throws<ArgumentException>(() => exe.SetBossHp(exe.GetBosses()[0].Constant, 40000f));
        Assert.That(exe.HasChanges, Is.False);

        var bad = new PnachGroup(Da2Executable.TuningGroupName, new[] { "patch=0,EE,000A0000,word,00000001" });
        Assert.Throws<InvalidDataException>(() => exe.ApplyTuningGroup(bad));
    }
}
