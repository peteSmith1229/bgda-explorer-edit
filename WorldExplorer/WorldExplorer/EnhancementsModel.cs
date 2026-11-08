// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/EnhancementsModel.cs
//
// The Enhancements tab of Executable Tuning (Views/EnhancementsView.xaml): additions to the game that
// work with or without three players, each turned on or off by itself. Four spells borrowed from the
// game's monsters and one feat the game has but never offers (BgdaExecutable.BorrowedSpells.cs), one card
// each: Xantam's Fire for Adrianna; Power Attack, Blade Barrier, Antimagic Field and Righteous Might for
// Kromlech. Each card has its tick box, price, mana (not Power Attack's: its record's, on the Feats &
// Spells tab) and values by rank; the names they need in GTEXT.LMP (the Items tab's copy, so one save
// writes both tabs' texts) and their icons in ICONS.LMP are shared (Power Attack's are the game's own).
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

/// <summary>One rank's boxes on a spell's card: one value, or two.</summary>
public sealed class SpellRankRow : ObservableObject
{
    private readonly Action _edited;
    private readonly SpellCard _card;
    private string _first = "", _second = "";

    internal SpellRankRow(int rank, SpellCard card, Action edited)
    {
        Rank = rank;
        _card = card;
        _edited = edited;
    }

    public int Rank { get; }

    public string Label => $"Rank {Rank}";

    public bool HasSecond => _card.Info.HasSecond;

    /// <summary>The card's marks around the boxes ("+10 % − 10 %", "6 s", "4 – 8") and their tooltips.</summary>
    public string FirstPrefix => _card.FirstPrefix;

    public string FirstSuffix => _card.FirstSuffix;

    public string Between => _card.Between;

    public string SecondSuffix => _card.SecondSuffix;

    public string FirstTip => _card.FirstTip;

    public string SecondTip => _card.SecondTip;

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

    internal void Fill(int first, int? second)
    {
        SetProperty(ref _first, first.ToString(), nameof(First));
        SetProperty(ref _second, second?.ToString() ?? "", nameof(Second));
    }
}

/// <summary>One borrowed spell's card: its tick box, its settings and what saving does with it.</summary>
public sealed class SpellCard : ObservableObject
{
    private readonly EnhancementsModel _owner;
    private bool _on;
    private string _price = "", _mana = "";

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
        _ => "The least damage: 0 to 255."
    };

    public string SecondTip => Spell == BorrowedSpell.RighteousMight
        ? "How much less damage physical hits on him do: 0 to 90 percent."
        : "The most damage: 0 to 255.";

    public string DefaultsTip
    {
        get
        {
            var d = Info.Default;
            if (!Info.HasMana)
                return $"Price {d.Price} and {string.Join(", ", d.First)} hits: what featpatch.py writes (its mana is its record's).";
            var values = Spell switch
            {
                BorrowedSpell.AntimagicField => string.Join(", ", d.First) + " seconds",
                BorrowedSpell.RighteousMight => string.Join(", ", d.First.Select((f, r) => $"+{f}/−{d.Second[r]}%")),
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

    /// <summary>The card has a Mana box (not Power Attack's: its record's mana is on the Feats &amp; Spells tab).</summary>
    public bool HasMana => Info.HasMana;

    /// <summary>Where Power Attack's mana is (null for the others, whose box this is).</summary>
    public string? ManaNote => Info.HasMana
        ? null
        : "Mana: its own ability record's" + (_owner.PowerAttackMana is { } m ? $", {NumberText.Format(m)} in this executable" : "") +
          " (\"powerattack\" on the Feats & Spells tab, as the game's other feats).";

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
        for (var r = 0; r < Ranks.Count; r++) Ranks[r].Fill(s.First[r], Info.HasSecond ? s.Second[r] : null);
    }

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

        var first = new int[Ranks.Count];
        var second = new int[Info.HasSecond ? Ranks.Count : 0];
        for (var r = 0; r < Ranks.Count; r++)
        {
            if (!NumberText.TryParseInteger(Ranks[r].First, out first[r]) ||
                Info.HasSecond && !NumberText.TryParseInteger(Ranks[r].Second, out second[r]))
            {
                problem = $"{Info.Title}, rank {r + 1}: " + (Info.HasSecond ? "two whole numbers" : "a whole number") + ".";
                return null;
            }
        }

        var s = new BorrowedSpellSettings(price, (float)mana, first, second);
        problem = s.Problem(Spell);
        return problem == null ? s : null;
    }

    /// <summary>What is wrong with the boxes (only while the spell is to be in), or null.</summary>
    public string? SettingsProblem => _on && TrySettings(out var p) == null ? p : null;

    /// <summary>What the ranks cost with the price in the box ("10, 11, 13, 15 and 19 feat points"), or "" when it isn't a price.</summary>
    public string RankCostText
    {
        get
        {
            if (!NumberText.TryParseInteger(_price, out var price) || price is < 0 or > BorrowedSpellSettings.MaxPrice) return "";
            if (price == 0) return "Every rank is free (for testing).";
            var costs = Enumerable.Range(0, BorrowedSpellSettings.Ranks).Select(h => BorrowedSpellSettings.RankCost(price, h).ToString()).ToList();
            return $"Ranks 1-5 cost {string.Join(", ", costs.Take(4))} and {costs[4]} feat points.";
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
            var where = $"{Info.Hero}'s level-up screen offers it after {Info.After} (5 ranks)";
            if (_owner.State == BorrowedSpellsState.Outdated && InFile)
                return _on
                    ? "In as an earlier test version: saving brings it to the current one, in game code nothing runs, so it works " +
                      "with or without three players; its settings are kept."
                    : "The earlier test version is taken out when you save: the game's code back, exactly.";
            if (InFile)
            {
                if (!_on)
                    return $"Taken out when you save: the game's own code goes back. A saved game whose {Info.Hero} learnt it keeps " +
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
        Reload();
    }

    /// <summary>The five spells' cards: Xantam's Fire, Power Attack, Blade Barrier, Antimagic Field, Righteous Might.</summary>
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

    /// <summary>Those with names and an icon of their own to put in GTEXT.LMP and ICONS.LMP (all but Power Attack, whose are the game's).</summary>
    private static bool HasOwnFiles(SpellCard c) => !c.Info.GameFeat;

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
        _names() is { } names && (WillBeIn.Where(HasOwnFiles).Any(c => !BorrowedSpellTexts.HasNames(names, c.Spell)) ||
                                  TakingOut.Where(HasOwnFiles).Any(c => BorrowedSpellTexts.HasAnyName(names, c.Spell)));

    /// <summary>Called when the Items tab loads or saves GTEXT.LMP: the names' status follows it.</summary>
    public void NamesReloaded() => Edited();

    /// <summary>Puts the names in GTEXT.LMP, or takes them out (see <see cref="NamesPending"/>); called just before it is written.</summary>
    public void ApplyNames()
    {
        if (_names() is not { } names || !NamesPending) return;
        BorrowedSpellTexts.AddNames(names, WillBeIn.Where(HasOwnFiles).Where(c => !BorrowedSpellTexts.HasNames(names, c.Spell)).Select(c => c.Spell));
        BorrowedSpellTexts.RemoveNames(names, TakingOut.Where(HasOwnFiles).Select(c => c.Spell));
        _namesApplied = true;
    }

    public string NamesStatus
    {
        get
        {
            var names = _names();
            var going = WillBeIn.Where(HasOwnFiles).ToList();
            if (names == null)
                return going.Count > 0
                    ? "GTEXT.LMP isn't loaded (Items tab, Open GTEXT.LMP…): without the spells' names the level-up screen and the HUD " +
                      $"show NF:{BorrowedSpellTexts.NameKey(going[0].Spell)} and the like."
                    : "GTEXT.LMP isn't loaded (Items tab).";
            var adding = going.Where(c => !BorrowedSpellTexts.HasNames(names, c.Spell)).Select(c => c.Info.Title).ToList();
            var removing = TakingOut.Where(HasOwnFiles).Where(c => BorrowedSpellTexts.HasAnyName(names, c.Spell)).Select(c => c.Info.Title).ToList();
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
                : "Each spell's name and description go in GTEXT.LMP with it (Power Attack's are the game's: always there).";
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
        _icons is { } icons && (WillBeIn.Where(HasOwnFiles).Any(c => !BorrowedSpellIcons.IsIn(icons, c.Spell)) ||
                                TakingOut.Where(HasOwnFiles).Any(c => BorrowedSpellIcons.IsIn(icons, c.Spell)));

    /// <summary>ICONS.LMP as it is to be saved (see <see cref="IconsNeedSaving"/>).</summary>
    public byte[] IconsToSave()
    {
        if (_icons == null) throw new InvalidOperationException("ICONS.LMP isn't loaded.");
        if (!IconsNeedSaving) return _icons;
        var icons = BorrowedSpellIcons.RemoveFrom(_icons, TakingOut.Where(HasOwnFiles).Select(c => c.Spell));
        return BorrowedSpellIcons.AddTo(icons, WillBeIn.Where(HasOwnFiles).Select(c => c.Spell));
    }

    /// <summary>Called after ICONS.LMP was saved: what was written is the new starting point.</summary>
    public void IconsSaved(byte[] saved, string path) => LoadIcons(saved, path);

    public string IconStatus
    {
        get
        {
            var going = WillBeIn.Where(HasOwnFiles).ToList();
            if (_icons is not { } icons)
                return going.Count > 0
                    ? "ICONS.LMP isn't loaded: open it from the game's data folder to add the spells' HUD icons. Without them the HUD " +
                      "shows a game icon in their place; nothing else changes."
                    : "ICONS.LMP isn't loaded.";
            var adding = going.Where(c => !BorrowedSpellIcons.IsIn(icons, c.Spell)).Select(c => BorrowedSpellIcons.EntryName(c.Spell)).ToList();
            var removing = TakingOut.Where(HasOwnFiles).Where(c => BorrowedSpellIcons.IsIn(icons, c.Spell))
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
                : $"{IconsPath}: each spell's icon goes in with it (Power Attack's is the game's own).";
        }
    }

    /// <summary>
    /// After saving, what the game will lack: names or icons, when GTEXT.LMP or ICONS.LMP wasn't loaded or saved
    /// (<paramref name="namesWritten"/>: GTEXT.LMP was written); null when nothing.
    /// </summary>
    public string? FilesNote(bool namesWritten)
    {
        var going = WillBeIn.Where(HasOwnFiles).ToList();
        if (going.Count == 0) return null;
        var missing = new List<string>();
        var names = _names();
        var noNames = going.Where(c => names == null || !BorrowedSpellTexts.HasNames(names, c.Spell) || _namesApplied && !namesWritten).ToList();
        if (noNames.Count > 0)
            missing.Add($"GTEXT.LMP still needs the names of {Join(noNames.Select(c => c.Info.Title).ToList())} (load it on the Items tab " +
                        $"and save again), or the game shows NF:{BorrowedSpellTexts.NameKey(noNames[0].Spell)}");
        var noIcons = going.Where(c => _icons == null || !BorrowedSpellIcons.IsIn(_icons, c.Spell)).ToList();
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
                   (used > 0 ? $"The spells take {used:N0} bytes of it (the same for one or all five); " : "") +
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
