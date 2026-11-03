using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Pnach;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Creatures placed when a level loads (the mod's spawn table): build 51's three bosses come out as
/// they were; any other set of creatures gets the table moved to 0xAE200 and reads back as written.
/// </summary>
[TestFixture]
public class Da2PlacementTests
{
    private const string Tuning = "WorldExplorer - executable tuning";
    private const uint HookSite = 0x271C10, SpawnHook = 0xAE0D8;

    private static PnachFile Stock() => PnachFile.Parse(Da2ThreePlayerMod.StockText);

    [Test]
    public void Build51_HasTheThreeBossesInTrollbark()
    {
        var p = Da2ThreePlayerMod.StockPlacements();
        Assert.That(p.Select(x => x.Type), Is.EqualTo(new[] { "Beholder", "Lilivarra", "Minotaur" }));
        Assert.That(p.All(x => x.Level == "trollb" && x.Include && x.Properties.Count == 0), Is.True);
        var minotaur = p[2];
        Assert.That((minotaur.X, minotaur.Y, minotaur.Z, minotaur.Facing), Is.EqualTo((280f, -1590f, 5f, 20480)));
        Assert.That((p[0].X, p[0].Y, p[0].Facing), Is.EqualTo((628f, 728f, 8192)));
        Assert.That((p[1].X, p[1].Y, p[1].Facing), Is.EqualTo((-2400f, 900f, -16385)));
        Assert.That(p.Select(x => x.NeedsFiles), Is.EqualTo(new[] { 0, 8, 16 }));
        Assert.That(p.Select(x => x.Name), Is.EqualTo(Enumerable.Range(0, 3).Select(Da2ThreePlayerMod.BossGroup)));
    }

    [Test]
    public void LeavingABossOut_KeepsTheOthersAsBuild51HasThem()
    {
        var s = Da2ThreePlayerMod.DefaultSettings();
        s.Placements[1].Include = false;
        var file = Da2ThreePlayerMod.Write(s);
        var stock = Stock();
        Assert.That(file.Find(Da2ThreePlayerMod.BossGroup(1)), Is.Null);
        foreach (var i in new[] { 0, 2 })
            Assert.That(file.Find(Da2ThreePlayerMod.BossGroup(i))!.Patches, Is.EqualTo(stock.Find(Da2ThreePlayerMod.BossGroup(i))!.Patches));

        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(file.ToText()), Tuning);
        Assert.That(read.Settings.Placements.Select(x => (x.Type, x.Include)),
            Is.EqualTo(new[] { ("Beholder", true), ("Lilivarra", false), ("Minotaur", true) }));
        Assert.That(Da2ThreePlayerMod.Write(read.Settings).ToText(), Is.EqualTo(file.ToText()));
        Assert.That(read.OtherGroups, Is.Empty);
    }

    [Test]
    public void OtherCreatures_MoveTheTable_AndReadBack()
    {
        var s = Da2ThreePlayerMod.DefaultSettings();
        s.Placements[2].X = 300f;                                    // the Minotaur a little further east
        s.Placements.Add(new Da2Placement
        {
            Name = "Creature - Ogre in caravan", Level = "caravan", Type = "Ogre", X = -500.5f, Y = 1200f, Z = 2f,
            Facing = -4096, Properties = new List<string> { "hpscale=2", "damagescale=1.5" }
        });
        s.Placements.Add(new Da2Placement { Name = "Creature - Lilivarra in goblair", Level = "goblair", Type = "Lilivarra", Z = 1f });
        var file = Da2ThreePlayerMod.Write(s);

        var groups = s.Placements.Select(p => file.Find(p.Name)!).ToList();
        Assert.That(groups.All(g => g != null), Is.True);
        var words = groups.Select(g => g.BootWords()).ToList();
        foreach (var w in words)
        {
            Assert.That(w[HookSite], Is.EqualTo(0x0C000000u | (SpawnHook >> 2)));
            Assert.That(w[0xAE0F4], Is.EqualTo(0x3C10000Bu), "lui s0, %hi(0xAE200)");
            Assert.That(w[0xAE0F8], Is.EqualTo(0x2610E200u), "addiu s0, s0, %lo(0xAE200)");
            Assert.That(w.Keys.Where(a => a < 0x100000).All(a => a >= 0xAE0D8 && a < 0xAF000), Is.True, "inside the spawn area");
        }

        // the same code and table in every group; each sets only its own enable word
        var shared = words[0].Where(p => p.Value != 1 || p.Key >= 0xAE200).ToDictionary(p => p.Key, p => p.Value);
        for (var i = 0; i < words.Count; i++)
        {
            var mine = words[i].Where(p => p.Key >= 0xAE1A0 && p.Key < 0xAE200).ToList();
            Assert.That(mine, Is.EqualTo(new[] { new KeyValuePair<uint, uint>(0xAE1A0u + (uint)(4 * i), 1u) }));
            foreach (var (a, v) in shared.Where(p => p.Key < 0xAE1A0 || p.Key >= 0xAE200))
                Assert.That(words[i][a], Is.EqualTo(v));
        }

        // the stock code otherwise unchanged
        var stockWords = Stock().Find(Da2ThreePlayerMod.BossGroup(0))!.BootWords();
        for (uint a = 0xAE0D8; a < 0xAE1A0; a += 4)
            if (a != 0xAE0F4 && a != 0xAE0F8) Assert.That(words[0][a], Is.EqualTo(stockWords[a]), $"0x{a:X5}");

        Assert.That(words[1].ContainsKey(0x12446C) && words[4].ContainsKey(0x12446C), Is.True, "Lilivarra's groups make her hostile");
        Assert.That(words[0].ContainsKey(0x12446C) || words[3].ContainsKey(0x12446C), Is.False);

        var read = Da2ThreePlayerMod.Read(PnachFile.Parse(file.ToText()), Tuning);
        Assert.That(read.OtherGroups, Is.Empty);
        var back = read.Settings.Placements;
        Assert.That(back.Count, Is.EqualTo(5));
        for (var i = 0; i < 5; i++)
        {
            Assert.That(back[i].SamePlace(s.Placements[i]), Is.True, s.Placements[i].Name);
            Assert.That(back[i].Name, Is.EqualTo(s.Placements[i].Name));
        }

        Assert.That(back[3].Properties, Is.EqualTo(new[] { "hpscale=2", "damagescale=1.5" }));
        Assert.That(back[3].Description, Does.Contain("Ogre in the level caravan at (-500.5, 1200, 2)"));
        Assert.That(Da2ThreePlayerMod.Write(read.Settings).ToText(), Is.EqualTo(file.ToText()));
    }

    [Test]
    public void BadPlacements_AreRejected()
    {
        void Bad(Action<Da2ModSettings> change)
        {
            var s = Da2ThreePlayerMod.DefaultSettings();
            change(s);
            Assert.Throws<ArgumentException>(() => Da2ThreePlayerMod.Write(s));
        }

        Bad(s => s.Placements[0].Name = s.Placements[1].Name);
        Bad(s => s.Placements[0].Name = "HUD size 50%");
        Bad(s => s.Placements[0].Level = "troll b");
        Bad(s => s.Placements[0].Type = "");
        Bad(s => s.Placements[0].X = float.NaN);
        Bad(s => s.Placements[0].Properties.Add("hpscale"));
        Bad(s =>
        {
            for (var i = 0; i < Da2ThreePlayerMod.MaxPlacements; i++)
                s.Placements.Add(new Da2Placement { Name = "Creature " + i, Level = "trollb", Type = "Goblin" });
        });
    }
}
