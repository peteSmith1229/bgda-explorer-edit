// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/EnhancementsModel.cs
//
// The Enhancements tab of Executable Tuning (Views/EnhancementsView.xaml): additions to the game that
// work with or without three players, each turned on or off by itself. Five spells borrowed from the
// game's monsters, the game's own unused vines and three feats the game has but never offers
// (BgdaExecutable.BorrowedSpells.cs), one card each: Xantam's Fire, Sorcerer's Blood and Xantam's Orbs for
// Adrianna; Power Attack, Blade Barrier, Antimagic Field and Righteous Might for Kromlech; Inferno Arrow and
// Entangle for Vahn. Each card has its tick box, price, mana (not Power Attack's: its record's, on the Feats &
// Spells tab) and values by rank (Xantam's Orbs: three, the number of orbs with the least and most damage of
// each; Inferno Arrow: seven, the arrow's damage, the blast's reach, the flames' spread and time, and their
// damage; Entangle: five, the number of vines with the least and most damage of each thorn, how much slower a
// wrapped monster walks and how long a vine holds it); Sorcerer's Blood, a passive,
// has the number of ranks its entry offers instead of mana and values. The names they need in GTEXT.LMP
// (the Items tab's copy, so one save writes both tabs' texts) and their icons in ICONS.LMP are shared
// (Power Attack's, Sorcerer's Blood's and Inferno Arrow's names, and Power Attack's icon, are the game's own).
// Every box is text so a half-typed value never throws; SettingsProblem says what is wrong. No WPF types,
// so it runs in tests.
//
// Saving (ExecutableEditorWindow.SaveAs_Click): ReadSpells checks the boxes before the file name is
// asked for; ApplySpells writes the executable's part once it is chosen, before the Three Players tab's
// (which can then turn three players on or off underneath it); ApplyNames goes into GTEXT.LMP just
// before that is written, IconsToSave is what ICONS.LMP gets; Reload afterwards.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>A further box of a rank's row (Inferno Arrow's blast, flames and burns; Entangle's slow and time): what goes before and after it, and its text.</summary>
public sealed class SpellValueBox : ObservableObject
{
    private readonly Action _edited;
    private string _text = "";

    internal SpellValueBox(BorrowedSpellValue value, string prefix, string suffix, string tip, Action edited)
    {
        Value = value;
        Prefix = prefix;
        Suffix = suffix;
        Tip = tip;
        _edited = edited;
    }

    public BorrowedSpellValue Value { get; }

    public string Prefix { get; }

    public string Suffix { get; }

    public string Tip { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value)) _edited();
        }
    }

    /// <summary>A value as the box shows it (a time in seconds with one decimal: kept in tenths).</summary>
    internal void Fill(int value) =>
        SetProperty(ref _text, Value.Tenths ? NumberText.Format(value / 10.0) : value.ToString(), nameof(Text));

    /// <summary>The box's value (in tenths for a time), or false when it isn't one.</summary>
    internal bool TryRead(out int value)
    {
        value = 0;
        if (!Value.Tenths) return NumberText.TryParseInteger(_text, out value);
        if (!NumberText.TryParseDecimal(_text, out var seconds) || !double.IsFinite(seconds)) return false;
        var tenths = Math.Round(seconds * 10.0);
        if (Math.Abs(seconds * 10.0 - tenths) > 1e-6 || tenths is < int.MinValue or > int.MaxValue) return false;
        value = (int)tenths;
        return true;
    }
}

/// <summary>One rank's boxes on a spell's card: one value, two, or three (Xantam's Orbs, Entangle: how many, then least and most damage), and any further ones (Inferno Arrow's, Entangle's).</summary>
public sealed class SpellRankRow : ObservableObject
{
    private readonly Action _edited;
    private readonly SpellCard _card;
    private string _first = "", _second = "", _third = "";

    internal SpellRankRow(int rank, SpellCard card, Action edited)
    {
        Rank = rank;
        _card = card;
        _edited = edited;
        More = card.Info.More.Select((v, k) => new SpellValueBox(v, card.MorePrefix(k), card.MoreSuffix(k), card.MoreTip(k), edited))
            .ToList();
    }

    /// <summary>The further boxes, after the others (Inferno Arrow's: the blast's reach, the flames' spread and time, their damage; Entangle's: the slow and the time).</summary>
    public IReadOnlyList<SpellValueBox> More { get; }

    public bool HasMore => More.Count > 0;

    public int Rank { get; }

    public string Label => $"Rank {Rank}";

    public bool HasSecond => _card.Info.HasSecond;

    /// <summary>The row has a third box, before the others (Xantam's Orbs: how many orbs; Entangle: how many vines).</summary>
    public bool HasThird => _card.Info.HasThird;

    /// <summary>The card's marks around the boxes ("+10 % − 10 %", "6 s", "4 – 8") and their tooltips.</summary>
    public string FirstPrefix => _card.FirstPrefix;

    public string FirstSuffix => _card.FirstSuffix;

    public string Between => _card.Between;

    public string SecondSuffix => _card.SecondSuffix;

    public string FirstTip => _card.FirstTip;

    public string SecondTip => _card.SecondTip;

    public string ThirdSuffix => _card.ThirdSuffix;

    public string ThirdTip => _card.ThirdTip;

    public string First
    {
        get => _first;
        set
        {
            if (SetProperty(ref _first, value)) _edited();
        }
    }

    public string Second
    {
        get => _second;
        set
        {
            if (SetProperty(ref _second, value)) _edited();
        }
    }

    public string Third
    {
        get => _third;
        set
        {
            if (SetProperty(ref _third, value)) _edited();
        }
    }

    internal void Fill(int first, int? second, int? third = null, IEnumerable<int>? more = null)
    {
        SetProperty(ref _first, first.ToString(), nameof(First));
        SetProperty(ref _second, second?.ToString() ?? "", nameof(Second));
        SetProperty(ref _third, third?.ToString() ?? "", nameof(Third));
        foreach (var (box, value) in More.Zip(more ?? Enumerable.Empty<int>())) box.Fill(value);
    }
}

/// <summary>One hero's cards on the tab, under the hero's name.</summary>
public sealed record SpellGroup(string Hero, IReadOnlyList<SpellCard> Cards)
{
    public string Heading => Hero.ToUpperInvariant();
}

/// <summary>One borrowed spell's card: its tick box, its settings and what saving does with it.</summary>
public sealed class SpellCard : ObservableObject
{
    private readonly EnhancementsModel _owner;
    private bool _on;
    private string _price = "", _mana = "", _rankCount = "";

    internal SpellCard(EnhancementsModel owner, BorrowedSpell spell)
    {
        _owner = owner;
        Spell = spell;
        Info = BorrowedSpells.Info(spell);
        Ranks = Enumerable.Range(1, BorrowedSpellSettings.Ranks).Select(r => new SpellRankRow(r, this, Edited)).ToList();
    }

    public BorrowedSpell Spell { get; }

    public BorrowedSpellInfo Info { get; }

    /// <summary>"Blade Barrier for Kromlech".</summary>
    public string Title => $"{Info.Title} for {Info.Hero}";

    /// <summary>What the spell does in the game.</summary>
    public string Description => Spell switch
    {
        BorrowedSpell.XantamFire =>
            "She casts three trails of fire along the ground in a fan (five from rank 3); every monster in them catches fire and is " +
            "burnt five times a second, and her kills give her the experience. At rank 5 the trails turn green and home in on the " +
            "nearest monster in front of her. They never hurt her or the other players.",
        BorrowedSpell.PowerAttack =>
            "The game's own feat, which it never offers: casting it readies his weapon, and his next melee hits on monsters (the " +
            "rank's number of them) within 10 seconds do double damage, each with a crash. Not with a bow, as the game has it. " +
            "Casting it again starts the count and the 10 seconds again. Its icon, its HUD line (\"Damage: Double Normal Melee " +
            "Damage\") and its texts are the game's.",
        BorrowedSpell.BladeBarrier =>
            "Nine axes whirl around him for 10 seconds, as they do around Ilivarra: once a second every monster in their reach takes " +
            "the rank's damage from him, and his kills give him the experience. They never hit him or the other players. Casting it " +
            "again while it runs starts its 10 seconds again.",
        BorrowedSpell.AntimagicField =>
            "Ilivarra's field: for the rank's time fire, cold, electricity and magic do him no harm (burning, freezing and shocks " +
            "included), and a spell that reaches him bursts into lilac sparks. Weapons still hurt him. Casting it again while it " +
            "runs starts its time again.",
        BorrowedSpell.XantamOrbs =>
            "Xantam's third attack: she hurls the rank's number of his glowing orange orbs (one at rank 1, five at rank 5) in a " +
            "fan from her hand, 12 degrees apart, the middle of the fan at the nearest monster in front of her (straight ahead " +
            "when there is none), so the outer orbs fly past it on either side. They fly as his do, and each bursts on the first " +
            "thing it touches: a monster takes the orb's damage from her (resistances count, and her kills give her the " +
            "experience), and barrels, crates, pots and the like break. They never hurt her or the other players. The cast " +
            "sounds like her Fireball.",
        BorrowedSpell.SorcerersBlood =>
            "The game's own feat, which it never offers, as its description says: \"+1 Mana Point gain per level. This is not " +
            "retroactive.\" Once she has it, every level she gains gives her 1 more mana for each rank (12 in place of 11 at one " +
            "rank); the levels she had before don't count, so it is worth most taken early. A passive feat: no mana, no HUD slot. " +
            "Her extra mana is kept with her in saved games. Its texts are the game's.",
        BorrowedSpell.InfernoArrow =>
            "The game's own feat, cut before it worked (\"This feat will enchant an arrow so that it erupts into flames on " +
            "impact.\"): he looses an arrow that flies like a Flaming Arrow, does the rank's damage to what it hits, and bursts " +
            "where it strikes. Every monster within the blast's reach takes up to the arrow's most damage from him (fire: " +
            "resistances count, his kills give him the experience), less towards the edge, and catches fire for the rank's time; " +
            "barrels, crates and pots break. Then flames run out along the ground all round, as far as the rank's spread, and " +
            "burn there for that time, burning every monster in them five times a second. They never hurt him or the other " +
            "players. Its texts are the game's; its HUD shows the rank's damage.",
        BorrowedSpell.Entangle =>
            "The game's own vines, which nothing in it ever casts: the rank's number of thorny vines (one at rank 1, five at rank " +
            "5) crawl along the ground from his feet, trailing green sparks, each to a different monster within 240 of him, the " +
            "nearest first. A vine wraps its monster in a spiral of green sparks for the rank's time: it walks the rank's " +
            "percentage slower, and every second the thorns do the rank's damage to it from him (piercing, which a shield can't " +
            "block; resistances count, and his kills give him the experience). The vine falls away when its monster dies. Never " +
            "a player. Fewer monsters near him, fewer vines. The cast sounds like Melf's Acid Arrow's; its HUD shows a thorn's " +
            "damage.",
        _ =>
            "Ilivarra's might: for 10 seconds he grows to 1.3 times his size in a green glow, his melee hits do the rank's " +
            "percentage more damage and physical hits on him the rank's percentage less (spells do their full damage). Casting it " +
            "again while it runs starts its 10 seconds again."
    };

    /// <summary>The caption over the boxes by rank.</summary>
    public string ValuesCaption => Spell switch
    {
        BorrowedSpell.XantamFire => "Damage of each burn, by rank (least to most)",
        BorrowedSpell.PowerAttack => "Melee hits it doubles, by rank",
        BorrowedSpell.BladeBarrier => "Damage of each hit, by rank (least to most)",
        BorrowedSpell.AntimagicField => "Seconds it lasts, by rank",
        BorrowedSpell.SorcerersBlood => "",
        BorrowedSpell.XantamOrbs => "Orbs, and the damage of each (least to most), by rank",
        BorrowedSpell.InfernoArrow =>
            "The arrow's damage (least to most; the blast does up to the most), the blast's reach, how far the flames spread, how " +
            "long they burn, and the damage of each of their burns (least to most), by rank",
        BorrowedSpell.Entangle =>
            "Vines, and the damage of each thorn (least to most, once a second), how much slower a wrapped monster walks and how " +
            "long a vine holds it, by rank",
        _ => "Percentage more melee damage and less physical damage taken, by rank"
    };

    /// <summary>What goes before, between and after a rank's boxes ("4 – 8", "6 s", "+10 % / −10 %").</summary>
    public string FirstPrefix => Spell == BorrowedSpell.RighteousMight ? "+" : "";

    public string FirstSuffix => Spell switch
    {
        BorrowedSpell.AntimagicField => "s", BorrowedSpell.RighteousMight => "%", BorrowedSpell.PowerAttack => "hits", _ => ""
    };

    public string Between => Spell == BorrowedSpell.RighteousMight ? "−" : "–";

    public string SecondSuffix => Spell == BorrowedSpell.RighteousMight ? "%" : "";

    public string FirstTip => Spell switch
    {
        BorrowedSpell.PowerAttack => "How many of his next melee hits on monsters do double damage: 1 to 20 (within 10 seconds of the cast).",
        BorrowedSpell.AntimagicField => "How long the field lasts: 1 to 60 seconds.",
        BorrowedSpell.RighteousMight => "How much more damage his melee hits do: 0 to 200 percent.",
        BorrowedSpell.XantamOrbs => "The least damage of each orb: 0 to 255.",
        BorrowedSpell.Entangle => "The least damage of each thorn (a thorn a second while a vine holds its monster): 0 to 255.",
        _ => "The least damage: 0 to 255."
    };

    public string SecondTip => Spell switch
    {
        BorrowedSpell.RighteousMight => "How much less damage physical hits on him do: 0 to 90 percent.",
        BorrowedSpell.XantamOrbs => "The most damage of each orb: 0 to 255.",
        BorrowedSpell.Entangle => "The most damage of each thorn: 0 to 255.",
        _ => "The most damage: 0 to 255."
    };

    /// <summary>After the third box ("[3] ×  [8] – [12]": three orbs of 8 to 12) and its tooltip.</summary>
    public string ThirdSuffix => Info.HasThird ? "×" : "";

    public string ThirdTip => !Info.HasThird ? "" :
        Spell == BorrowedSpell.Entangle
            ? $"How many vines crawl out, each to a different monster within 240 of him: {Info.ThirdMin} to {Info.ThirdMax}."
            : $"How many orbs she hurls: {Info.ThirdMin} to {Info.ThirdMax}.";

    /// <summary>What goes before and after a further box ("blast [72]", "for [3] s"; "slower [40] %"), and its tooltip (Inferno Arrow's, Entangle's).</summary>
    public string MorePrefix(int k) => Spell == BorrowedSpell.Entangle
        ? (k == 0 ? "slower" : "for")
        : k switch { 0 => "blast", 1 => "flames", 2 => "for", 3 => "burning", _ => "–" };

    public string MoreSuffix(int k) => Spell == BorrowedSpell.Entangle ? (k == 0 ? "%" : "s") : k == 2 ? "s" : "";

    public string MoreTip(int k) => Spell == BorrowedSpell.Entangle ? EntangleTip(k) : InfernoTip(k);

    private static string EntangleTip(int k) => k == 0
        ? "How much slower a wrapped monster walks: an even percentage from 2 to 98 (the game counts in fiftieths; its own " +
          "vine: 50). A monster already slower, chilled say, keeps its own."
        : "How long a vine holds its monster: 0.1 to 25.5 seconds (a tenth at a time). A thorn every whole second of it.";

    private static string InfernoTip(int k) => k switch
    {
        0 => "The blast's reach, measured to a monster's edge: 1 to 255 (Exploding Arrow's is 72). Within a third of it a monster " +
             "takes the arrow's most damage, less beyond (at least 1).",
        1 => "How far the flames run out along the ground all round: 1 to 255 (they run 300 a second).",
        2 => "How long the flames burn, and the monsters the blast sets alight: 0.1 to 25.5 seconds (a tenth at a time). A monster " +
             "that resists fire burns half as long.",
        3 => "The least damage of each burn of the flames (five a second): 0 to 255.",
        _ => "The most damage of each burn of the flames: 0 to 255."
    };

    public string DefaultsTip
    {
        get
        {
            var d = Info.Default;
            if (Info.Passive)
                return $"Price {d.Price} and {RanksText(d.RankCount)}: what featpatch.py writes, as the game's description has it (+1 mana a level).";
            if (!Info.HasMana)
                return $"Price {d.Price} and {string.Join(", ", d.First)} hits: what featpatch.py writes (its mana is its record's).";
            var values = Spell switch
            {
                BorrowedSpell.AntimagicField => string.Join(", ", d.First) + " seconds",
                BorrowedSpell.RighteousMight => string.Join(", ", d.First.Select((f, r) => $"+{f}/−{d.Second[r]}%")),
                BorrowedSpell.XantamOrbs => string.Join(", ", d.ThirdValues) + " orbs of " +
                                            (d.First.Distinct().Count() == 1 && d.Second.Distinct().Count() == 1
                                                ? $"{d.First[0]}-{d.Second[0]}"
                                                : string.Join(", ", d.First.Select((f, r) => $"{f}-{d.Second[r]}"))),
                BorrowedSpell.InfernoArrow => string.Join(", ", d.First.Select((f, r) => $"{f}-{d.Second[r]}")) + " damage, a blast of " +
                                              string.Join(", ", d.MoreValues[0]) + ", flames spreading " +
                                              string.Join(", ", d.MoreValues[1]) + " for " +
                                              string.Join(", ", d.MoreValues[2].Select(t => NumberText.Format(t / 10.0))) +
                                              " seconds, burning " +
                                              string.Join(", ", d.MoreValues[3].Select((f, r) => $"{f}-{d.MoreValues[4][r]}")),
                BorrowedSpell.Entangle => string.Join(", ", d.ThirdValues) + " vines, thorns of " +
                                          string.Join(", ", d.First.Select((f, r) => $"{f}-{d.Second[r]}")) + ", " +
                                          string.Join(", ", d.MoreValues[0]) + " percent slower for " +
                                          string.Join(", ", d.MoreValues[1].Select(t => NumberText.Format(t / 10.0))) + " seconds",
                _ => string.Join(", ", d.First.Select((f, r) => $"{f}-{d.Second[r]}"))
            };
            return $"Price {d.Price}, {NumberText.Format(d.Mana)} mana, and {values}: what featpatch.py writes.";
        }
    }

    /// <summary>The tick box can be used: the file is one the patch knows (not Unrecognized).</summary>
    public bool Usable => _owner.Usable;

    /// <summary>The tick box: the spell is to be in the saved executable.</summary>
    public bool On
    {
        get => _on;
        set
        {
            if (!Usable || !SetProperty(ref _on, value)) return;
            _owner.Edited();
        }
    }

    /// <summary>The executable has the spell now (On; or Outdated, Xantam's Fire).</summary>
    public bool InFile => _owner.Loaded.ContainsKey(Spell);

    /// <summary>The spell will be in the saved executable.</summary>
    public bool WillBeIn => Usable && _on;

    /// <summary>Saving takes it out (it is in the file and the box isn't ticked).</summary>
    public bool TakingOut => Usable && InFile && !_on;

    public string Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value)) Edited();
        }
    }

    public string Mana
    {
        get => _mana;
        set
        {
            if (SetProperty(ref _mana, value)) Edited();
        }
    }

    /// <summary>The card has a Mana box (not Power Attack's: its record's mana is on the Feats &amp; Spells tab; not a passive's).</summary>
    public bool HasMana => Info.HasMana;

    /// <summary>The card has boxes by rank (every one but Sorcerer's Blood's).</summary>
    public bool HasValues => Info.HasValues;

    /// <summary>The card has a box for the number of ranks its entry offers (Sorcerer's Blood's).</summary>
    public bool HasRankCount => Info.Passive;

    /// <summary>Where Power Attack's mana is, or that a passive takes none (null for the others, whose box this is).</summary>
    public string? ManaNote => Info.HasMana
        ? null
        : Info.Passive
            ? "No mana: a passive feat. Each rank adds 1 mana for every level she gains once she has it; the levels before don't count."
            : "Mana: its own ability record's" + (_owner.PowerAttackMana is { } m ? $", {NumberText.Format(m)} in this executable" : "") +
              " (\"powerattack\" on the Feats & Spells tab, as the game's other feats).";

    /// <summary>The number of ranks the level-up screen offers (Sorcerer's Blood: 1 to 5, each adding 1 mana a level).</summary>
    public string RankCount
    {
        get => _rankCount;
        set
        {
            if (SetProperty(ref _rankCount, value)) Edited();
        }
    }

    /// <summary>The boxes of ranks 1-5.</summary>
    public IReadOnlyList<SpellRankRow> Ranks { get; }

    internal void Load(bool on, BorrowedSpellSettings settings)
    {
        SetProperty(ref _on, on, nameof(On));
        Fill(settings);
    }

    private void Fill(BorrowedSpellSettings s)
    {
        SetProperty(ref _price, s.Price.ToString(), nameof(Price));
        SetProperty(ref _mana, Info.HasMana ? NumberText.Format(s.Mana) : "", nameof(Mana));
        SetProperty(ref _rankCount, s.RankCount.ToString(), nameof(RankCount));
        if (!Info.HasValues) return;
        for (var r = 0; r < Ranks.Count; r++)
            Ranks[r].Fill(s.First[r], Info.HasSecond ? s.Second[r] : null, Info.HasThird ? s.ThirdValues[r] : null,
                          s.MoreValues.Select(v => v[r]));
    }

    /// <summary>"1 rank", "3 ranks".</summary>
    private static string RanksText(int n) => n == 1 ? "1 rank" : $"{n} ranks";

    /// <summary>The boxes back to what featpatch.py writes.</summary>
    public void Defaults()
    {
        Fill(Info.Default);
        Edited();
    }

    /// <summary>The settings in the boxes, or null with <paramref name="problem"/> saying what is wrong.</summary>
    public BorrowedSpellSettings? TrySettings(out string? problem)
    {
        problem = null;
        if (!NumberText.TryParseInteger(_price, out var price))
        {
            problem = $"{Info.Title}: the price is a whole number from 0 to {BorrowedSpellSettings.MaxPrice}.";
            return null;
        }

        var mana = 0.0;
        if (Info.HasMana && !NumberText.TryParseDecimal(_mana, out mana))
        {
            problem = $"{Info.Title}: the mana a cast takes is a number, such as 15 or 7.5.";
            return null;
        }

        if (Info.Passive)
        {
            if (!NumberText.TryParseInteger(_rankCount, out var count))
            {
                problem = $"{Info.Title}: the number of ranks is a whole number from 1 to {BorrowedSpellSettings.MaxRankCount}.";
                return null;
            }

            var p = new BorrowedSpellSettings(price, 0f, Array.Empty<int>(), Array.Empty<int>(), count);
            problem = p.Problem(Spell);
            return problem == null ? p : null;
        }

        var first = new int[Ranks.Count];
        var second = new int[Info.HasSecond ? Ranks.Count : 0];
        var third = Info.HasThird ? new int[Ranks.Count] : null;
        var more = Info.More.Select(_ => new int[Ranks.Count]).ToArray();
        for (var r = 0; r < Ranks.Count; r++)
        {
            if (!NumberText.TryParseInteger(Ranks[r].First, out first[r]) ||
                Info.HasSecond && !NumberText.TryParseInteger(Ranks[r].Second, out second[r]) ||
                third != null && !NumberText.TryParseInteger(Ranks[r].Third, out third[r]))
            {
                problem = $"{Info.Title}, rank {r + 1}: " +
                          (third != null ? "three whole numbers" : Info.HasSecond ? "two whole numbers" : "a whole number") + ".";
                return null;
            }

            for (var k = 0; k < more.Length; k++)
            {
                if (Ranks[r].More[k].TryRead(out more[k][r])) continue;
                var v = Info.More[k];
                problem = $"{Info.Title}, rank {r + 1}: {v.Name} is " +
                          (v.Tenths ? "a number of seconds, such as 2 or 2.5 (a tenth at a time)." : "a whole number.");
                return null;
            }
        }

        var s = new BorrowedSpellSettings(price, (float)mana, first, second, Third: third,
                                          More: more.Length > 0 ? more.Select(m => (IReadOnlyList<int>)m).ToArray() : null);
        problem = s.Problem(Spell);
        return problem == null ? s : null;
    }

    /// <summary>What is wrong with the boxes (only while the spell is to be in), or null.</summary>
    public string? SettingsProblem => _on && TrySettings(out var p) == null ? p : null;

    /// <summary>The ranks the level-up screen offers as the boxes say (5, or Sorcerer's Blood's box), or 0 when its box isn't a number.</summary>
    private int OfferedRanks =>
        !Info.Passive ? BorrowedSpellSettings.Ranks
        : NumberText.TryParseInteger(_rankCount, out var n) && n is >= 1 and <= BorrowedSpellSettings.MaxRankCount ? n : 0;

    /// <summary>What the ranks cost with the price in the box ("10, 11, 13, 15 and 19 feat points"), or "" when it isn't a price.</summary>
    public string RankCostText
    {
        get
        {
            if (!NumberText.TryParseInteger(_price, out var price) || price is < 0 or > BorrowedSpellSettings.MaxPrice) return "";
            var ranks = OfferedRanks;
            if (ranks == 0) return "";
            if (price == 0) return ranks == 1 ? "It is free (for testing)." : "Every rank is free (for testing).";
            var costs = Enumerable.Range(0, ranks).Select(h => BorrowedSpellSettings.RankCost(price, h).ToString()).ToList();
            return ranks == 1
                ? $"It costs {costs[0]} feat points."
                : $"Ranks 1-{ranks} cost {string.Join(", ", costs.Take(ranks - 1))} and {costs[^1]} feat points.";
        }
    }

    /// <summary>True when saving changes this spell in the executable.</summary>
    public bool Changed =>
        Usable && (_on != InFile || _on && _owner.State == BorrowedSpellsState.Outdated ||
                   _on && (TrySettings(out _) is not { } s || !s.Equals(_owner.Loaded[Spell])));

    /// <summary>What is in the file and what saving does with this spell, in a sentence or two.</summary>
    public string Status
    {
        get
        {
            if (!Usable) return "Can't be put in or taken out here (see above).";
            var ranks = OfferedRanks;
            var where = $"{Info.Hero}'s level-up screen offers it after {Info.After}" + (ranks > 0 ? $" ({RanksText(ranks)})" : "");
            if (_owner.State == BorrowedSpellsState.Outdated && InFile)
                return _on
                    ? "In as an earlier test version: saving brings it to the current one, in game code nothing runs, so it works " +
                      "with or without three players; its settings are kept."
                    : "The earlier test version is taken out when you save: the game's code back, exactly.";
            if (InFile)
            {
                if (!_on)
                    return Info.Passive
                        ? $"Taken out when you save: the game's own code goes back, and {Info.Hero}'s maximum mana is the game's again " +
                          "(a saved game keeps her extra mana, which counts again if the feat is put back)."
                        : $"Taken out when you save: the game's own code goes back. A saved game whose {Info.Hero} learnt it keeps " +
                          "the slot, but the spell then does nothing.";
                return Changed ? $"In. Its new settings are written when you save." : $"In: {where}.";
            }

            return _on
                ? $"Goes in when you save: {where}. It works with or without three players."
                : $"Not in this executable. Tick the box to give {Info.Hero} {Info.Title}.";
        }
    }

    private void Edited() => _owner.Edited();

    internal void Refresh()
    {
        OnPropertyChanged(nameof(On));
        OnPropertyChanged(nameof(Usable));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Changed));
        OnPropertyChanged(nameof(SettingsProblem));
        OnPropertyChanged(nameof(RankCostText));
        OnPropertyChanged(nameof(ManaNote));
        OnPropertyChanged(nameof(RankCount));
    }
}

public sealed class EnhancementsModel : ObservableObject
{
    private readonly BgdaExecutable _exe;
    private readonly Func<GameText?> _names;
    private BorrowedSpellsState _state;
    private string _detail = "";
    private byte[]? _icons;
    private bool _namesApplied;

    /// <param name="exe">The executable the window edits.</param>
    /// <param name="names">GTEXT.LMP as the Items tab has it loaded (null when it isn't).</param>
    public EnhancementsModel(BgdaExecutable exe, Func<GameText?> names)
    {
        _exe = exe;
        _names = names;
        Spells = BorrowedSpells.All.Select(s => new SpellCard(this, s)).ToList();
        Groups = HeroOrder.Select(h => new SpellGroup(h, Spells.Where(c => c.Info.Hero == h)
                                                           .OrderBy(c => c.Info.Title, StringComparer.CurrentCultureIgnoreCase).ToList()))
                          .Where(g => g.Cards.Count > 0).ToList();
        Reload();
    }

    /// <summary>The order of the heroes' groups on the tab.</summary>
    private static readonly string[] HeroOrder = { "Adrianna", "Kromlech", "Vahn" };

    /// <summary>The cards as the tab shows them: Adrianna's, then Kromlech's, then Vahn's, each hero's in alphabetical order.</summary>
    public IReadOnlyList<SpellGroup> Groups { get; }

    /// <summary>Ticks every card ("Enable all"); each keeps the settings in its boxes.</summary>
    public void EnableAll()
    {
        foreach (var card in Spells) card.On = true;
    }

    /// <summary>The nine spells' cards in the library's order (Card() indexes it): Xantam's Fire, Power Attack, Blade Barrier, Antimagic Field, Righteous Might, Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle. The tab shows Groups.</summary>
    public IReadOnlyList<SpellCard> Spells { get; }

    public SpellCard Card(BorrowedSpell spell) => Spells[(int)spell];

    /// <summary>The spells in the executable as it was loaded or last saved, with their settings.</summary>
    internal IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> Loaded { get; private set; } =
        new Dictionary<BorrowedSpell, BorrowedSpellSettings>();

    /// <summary>The mana in the game's Power Attack record as the executable has it (null when it can't be read).</summary>
    internal float? PowerAttackMana { get; private set; }

    /// <summary>Reads the state and the spells from the executable again (on opening, and after saving).</summary>
    public void Reload()
    {
        _state = _exe.GetBorrowedSpellsState(out _detail);
        Loaded = _exe.GetBorrowedSpells();
        PowerAttackMana = _exe.GetFeatSpells().FirstOrDefault(f => f.Name == "powerattack")?.EnergyCost;
        _namesApplied = false;
        foreach (var card in Spells)
            card.Load(Loaded.ContainsKey(card.Spell), Loaded.TryGetValue(card.Spell, out var s) ? s : card.Info.Default);
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(Usable));
        Edited();
    }

    public BorrowedSpellsState State => _state;

    /// <summary>The tick boxes can be used: the file is one the patch knows (not Unrecognized).</summary>
    public bool Usable => _state != BorrowedSpellsState.Unrecognized;

    /// <summary>What the executable has, for the top of the tab.</summary>
    public string StateText => _state switch
    {
        BorrowedSpellsState.Unrecognized => _detail + " The borrowed spells can't be put in or taken out here.",
        BorrowedSpellsState.Outdated => _detail,
        BorrowedSpellsState.On => $"{_detail} Saving writes the cards' settings; an unticked one is taken out.",
        _ => "None of them is in this executable yet. Tick the ones to give the heroes: each goes in by itself, and they work " +
             "with or without three players."
    };

    /// <summary>True when saving changes the executable's spells.</summary>
    public bool Changed => Usable && (Spells.Any(c => c.Changed) || _state == BorrowedSpellsState.Outdated);

    private List<SpellCard> WillBeIn => Spells.Where(c => c.WillBeIn).ToList();

    private List<SpellCard> TakingOut => Spells.Where(c => c.TakingOut).ToList();

    /// <summary>Those with names of their own to put in GTEXT.LMP (all but Power Attack, Sorcerer's Blood and Inferno Arrow, whose are the game's).</summary>
    private static bool HasOwnNames(SpellCard c) => c.Info.HasOwnTexts;

    /// <summary>Those with an icon of their own to put in ICONS.LMP (all but Power Attack, whose is the game's, and Sorcerer's Blood, a passive).</summary>
    private static bool HasOwnIcon(SpellCard c) => BorrowedSpellIcons.HasOwnIcon(c.Spell);

    /// <summary>
    /// The settings to save, checked (empty when every spell is to be out); throws <see cref="ArgumentException"/> on a bad box.
    /// </summary>
    public IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> ReadSpells()
    {
        var spells = new Dictionary<BorrowedSpell, BorrowedSpellSettings>();
        foreach (var card in WillBeIn)
            spells[card.Spell] = card.TrySettings(out var problem) ?? throw new ArgumentException("Enhancements tab: " + problem);
        return spells;
    }

    /// <summary>
    /// Writes the spells into the executable as the tab says (<paramref name="spells"/> from <see cref="ReadSpells"/>): put in,
    /// updated, their settings changed, or taken out. Throws InvalidOperationException when the file can't take them.
    /// </summary>
    public void ApplySpells(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> spells)
    {
        if (!Changed) return;
        if (spells.Count > 0) _exe.SetBorrowedSpells(spells);
        else _exe.RemoveBorrowedSpells();
    }

    // ═══════════════════════════════ GTEXT.LMP: the names ═══════════════════════════════

    /// <summary>GTEXT.LMP has to change: names go in (a spell will be in and its names are missing) or come out (taken out here).</summary>
    public bool NamesPending =>
        _names() is { } names && (WillBeIn.Where(HasOwnNames).Any(c => !BorrowedSpellTexts.HasNames(names, c.Spell)) ||
                                  TakingOut.Where(HasOwnNames).Any(c => BorrowedSpellTexts.HasAnyName(names, c.Spell)));

    /// <summary>Called when the Items tab loads or saves GTEXT.LMP: the names' status follows it.</summary>
    public void NamesReloaded() => Edited();

    /// <summary>Puts the names in GTEXT.LMP, or takes them out (see <see cref="NamesPending"/>); called just before it is written.</summary>
    public void ApplyNames()
    {
        if (_names() is not { } names || !NamesPending) return;
        BorrowedSpellTexts.AddNames(names, WillBeIn.Where(HasOwnNames).Where(c => !BorrowedSpellTexts.HasNames(names, c.Spell)).Select(c => c.Spell));
        BorrowedSpellTexts.RemoveNames(names, TakingOut.Where(HasOwnNames).Select(c => c.Spell));
        _namesApplied = true;
    }

    public string NamesStatus
    {
        get
        {
            var names = _names();
            var going = WillBeIn.Where(HasOwnNames).ToList();
            if (names == null)
                return going.Count > 0
                    ? "GTEXT.LMP isn't loaded (Items tab, Open GTEXT.LMP…): without the spells' names the level-up screen and the HUD " +
                      $"show NF:{BorrowedSpellTexts.NameKey(going[0].Spell)} and the like."
                    : "GTEXT.LMP isn't loaded (Items tab).";
            var adding = going.Where(c => !BorrowedSpellTexts.HasNames(names, c.Spell)).Select(c => c.Info.Title).ToList();
            var removing = TakingOut.Where(HasOwnNames).Where(c => BorrowedSpellTexts.HasAnyName(names, c.Spell)).Select(c => c.Info.Title).ToList();
            var parts = new List<string>();
            if (adding.Count > 0)
                parts.Add($"The names and descriptions of {Join(adding)} (in all five languages) are added to GTEXT.LMP when you save.");
            if (removing.Count > 0)
                parts.Add(adding.Count > 0
                    ? $"Those of {Join(removing)} are taken out of it."
                    : $"The names and descriptions of {Join(removing)} are taken out of GTEXT.LMP when you save.");
            if (parts.Count > 0) return string.Join(" ", parts);
            return going.Count > 0
                ? "GTEXT.LMP has the spells' names and descriptions, in all five languages."
                : "Each spell's name and description go in GTEXT.LMP with it (Power Attack's, Sorcerer's Blood's and Inferno Arrow's are the " +
                  "game's: always there).";
        }
    }

    // ═══════════════════════════════ ICONS.LMP: the HUD icons ═══════════════════════════════

    public string? IconsPath { get; private set; }

    public bool IconsLoaded => _icons != null;

    /// <summary>Uses <paramref name="data"/> as ICONS.LMP; throws <see cref="System.IO.InvalidDataException"/> when the icons can't go in it.</summary>
    public void LoadIcons(byte[] data, string path)
    {
        if (BorrowedSpellIcons.Problem(data) is { } problem) throw new System.IO.InvalidDataException(problem);
        _icons = data;
        IconsPath = path;
        OnPropertyChanged(nameof(IconsPath));
        OnPropertyChanged(nameof(IconsLoaded));
        Edited();
    }

    /// <summary>ICONS.LMP has to change: icons go in (a spell will be in) or come out (taken out here).</summary>
    public bool IconsNeedSaving =>
        _icons is { } icons && (WillBeIn.Where(HasOwnIcon).Any(c => !BorrowedSpellIcons.IsIn(icons, c.Spell)) ||
                                TakingOut.Where(HasOwnIcon).Any(c => BorrowedSpellIcons.IsIn(icons, c.Spell)));

    /// <summary>ICONS.LMP as it is to be saved (see <see cref="IconsNeedSaving"/>).</summary>
    public byte[] IconsToSave()
    {
        if (_icons == null) throw new InvalidOperationException("ICONS.LMP isn't loaded.");
        if (!IconsNeedSaving) return _icons;
        var icons = BorrowedSpellIcons.RemoveFrom(_icons, TakingOut.Where(HasOwnIcon).Select(c => c.Spell));
        return BorrowedSpellIcons.AddTo(icons, WillBeIn.Where(HasOwnIcon).Select(c => c.Spell));
    }

    /// <summary>Called after ICONS.LMP was saved: what was written is the new starting point.</summary>
    public void IconsSaved(byte[] saved, string path) => LoadIcons(saved, path);

    public string IconStatus
    {
        get
        {
            var going = WillBeIn.Where(HasOwnIcon).ToList();
            if (_icons is not { } icons)
                return going.Count > 0
                    ? "ICONS.LMP isn't loaded: open it from the game's data folder to add the spells' HUD icons. Without them the HUD " +
                      "shows a game icon in their place; nothing else changes."
                    : "ICONS.LMP isn't loaded.";
            var adding = going.Where(c => !BorrowedSpellIcons.IsIn(icons, c.Spell)).Select(c => BorrowedSpellIcons.EntryName(c.Spell)).ToList();
            var removing = TakingOut.Where(HasOwnIcon).Where(c => BorrowedSpellIcons.IsIn(icons, c.Spell))
                .Select(c => BorrowedSpellIcons.EntryName(c.Spell)).ToList();
            var parts = new List<string>();
            if (adding.Count > 0)
                parts.Add($"{Join(adding)} {(adding.Count == 1 ? "is" : "are")} added to {IconsPath} when you save: the game finds every other icon as before.");
            if (removing.Count > 0)
                parts.Add($"{Join(removing)} {(removing.Count == 1 ? "is" : "are")} taken out of " +
                          (adding.Count > 0 ? "it." : $"{IconsPath} when you save."));
            if (parts.Count > 0) return string.Join(" ", parts);
            return going.Count > 0
                ? $"{IconsPath} has the spells' icons."
                : $"{IconsPath}: each spell's icon goes in with it (Power Attack's is the game's own; Sorcerer's Blood, a passive feat, has none).";
        }
    }

    /// <summary>
    /// After saving, what the game will lack: names or icons, when GTEXT.LMP or ICONS.LMP wasn't loaded or saved
    /// (<paramref name="namesWritten"/>: GTEXT.LMP was written); null when nothing.
    /// </summary>
    public string? FilesNote(bool namesWritten)
    {
        var going = WillBeIn.Where(c => HasOwnNames(c) || HasOwnIcon(c)).ToList();
        if (going.Count == 0) return null;
        var missing = new List<string>();
        var names = _names();
        var noNames = going.Where(HasOwnNames)
            .Where(c => names == null || !BorrowedSpellTexts.HasNames(names, c.Spell) || _namesApplied && !namesWritten).ToList();
        if (noNames.Count > 0)
            missing.Add($"GTEXT.LMP still needs the names of {Join(noNames.Select(c => c.Info.Title).ToList())} (load it on the Items tab " +
                        $"and save again), or the game shows NF:{BorrowedSpellTexts.NameKey(noNames[0].Spell)}");
        var noIcons = going.Where(HasOwnIcon).Where(c => _icons == null || !BorrowedSpellIcons.IsIn(_icons, c.Spell)).ToList();
        if (noIcons.Count > 0)
            missing.Add($"ICONS.LMP doesn't have the icons of {Join(noIcons.Select(c => c.Info.Title).ToList())} yet (open it on the " +
                        "Enhancements tab and save again), so the HUD shows a game icon in their place");
        return missing.Count == 0 ? null : string.Join(". ", missing) + ".";
    }

    // ═══════════════════════════════ room for more ═══════════════════════════════

    /// <summary>Where new feats go and how much room is left.</summary>
    public string PoolText
    {
        get
        {
            var used = WillBeIn.Count > 0 ? BgdaExecutable.BorrowedSpellsBytes : 0;
            return $"New feats go into game code that nothing ever runs: {BgdaExecutable.FeatPoolStretches} stretches, " +
                   $"{BgdaExecutable.FeatPoolBytes:N0} bytes, the same in the stock executable and every three-player build. " +
                   (used > 0 ? $"The spells take {used:N0} bytes of it (the same for one or all nine); " : "") +
                   $"{BgdaExecutable.FeatPoolBytes - used:N0} bytes are free for more.";
        }
    }

    private static string Join(IReadOnlyList<string> names) =>
        names.Count < 2 ? string.Join("", names) : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];

    /// <summary>Refreshes everything the boxes and the tick boxes feed.</summary>
    internal void Edited()
    {
        foreach (var card in Spells) card.Refresh();
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(Changed));
        OnPropertyChanged(nameof(NamesStatus));
        OnPropertyChanged(nameof(NamesPending));
        OnPropertyChanged(nameof(IconStatus));
        OnPropertyChanged(nameof(IconsNeedSaving));
        OnPropertyChanged(nameof(PoolText));
    }
}
