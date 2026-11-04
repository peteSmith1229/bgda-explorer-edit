// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/EnhancementsModel.cs
//
// The Enhancements tab of Executable Tuning (Views/EnhancementsView.xaml): additions to the game that
// work with or without three players, each turned on or off by itself. The first is Xantam's Fire
// for Adrianna (BgdaExecutable.XantamFire.cs): its price, mana and damage, the names it needs in
// GTEXT.LMP (the Items tab's copy, so one save writes both tabs' texts) and its icon in ICONS.LMP.
// Every box is text so a half-typed value never throws; SettingsProblem says what is wrong. No WPF
// types, so it runs in tests.
//
// Saving (ExecutableEditorWindow.SaveAs_Click): ReadXantamFire checks the boxes before the file
// name is asked for; ApplyXantamFire writes the executable's part once it is chosen, before the
// Three Players tab's (which can then turn three players on or off underneath it); ApplyNames goes
// into GTEXT.LMP just before that is written, IconsToSave is what ICONS.LMP gets; Reload afterwards.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>One rank's damage boxes.</summary>
public sealed class XantamRankRow : ObservableObject
{
    private readonly Action _edited;
    private string _min = "", _max = "";

    public XantamRankRow(int rank, Action edited)
    {
        Rank = rank;
        _edited = edited;
    }

    public int Rank { get; }

    public string Label => $"Rank {Rank}";

    public string Min
    {
        get => _min;
        set
        {
            if (SetProperty(ref _min, value)) _edited();
        }
    }

    public string Max
    {
        get => _max;
        set
        {
            if (SetProperty(ref _max, value)) _edited();
        }
    }

    internal void Fill(XantamFireDamage d)
    {
        SetProperty(ref _min, d.Min.ToString(), nameof(Min));
        SetProperty(ref _max, d.Max.ToString(), nameof(Max));
    }
}

public sealed class EnhancementsModel : ObservableObject
{
    private readonly BgdaExecutable _exe;
    private readonly Func<GameText?> _names;
    private XantamFireState _state;
    private string _detail = "";
    private XantamFireSettings _loaded = XantamFireSettings.Default;
    private bool _xantamOn;
    private string _price = "", _mana = "";
    private byte[]? _icons;
    private bool _namesApplied;

    /// <param name="exe">The executable the window edits.</param>
    /// <param name="names">GTEXT.LMP as the Items tab has it loaded (null when it isn't).</param>
    public EnhancementsModel(BgdaExecutable exe, Func<GameText?> names)
    {
        _exe = exe;
        _names = names;
        Ranks = Enumerable.Range(1, XantamFireSettings.Ranks).Select(r => new XantamRankRow(r, Edited)).ToList();
        Reload();
    }

    /// <summary>Reads Xantam's Fire's state and settings from the executable again (on opening, and after saving).</summary>
    public void Reload()
    {
        _state = _exe.GetXantamFireState(out _detail);
        _loaded = _exe.GetXantamFireSettings();
        _xantamOn = _state is XantamFireState.On or XantamFireState.Outdated;
        _namesApplied = false;
        Fill(_loaded);
        OnPropertyChanged(nameof(XantamOn));
        OnPropertyChanged(nameof(XantamUsable));
        OnPropertyChanged(nameof(XantamState));
        Edited();
    }

    // ═══════════════════════════════ Xantam's Fire ═══════════════════════════════

    public XantamFireState XantamState => _state;

    /// <summary>The checkbox can be used: the file is one the patch knows (not Unrecognized).</summary>
    public bool XantamUsable => _state != XantamFireState.Unrecognized;

    /// <summary>The checkbox: Xantam's Fire is to be in the saved executable.</summary>
    public bool XantamOn
    {
        get => _xantamOn;
        set
        {
            if (!XantamUsable || !SetProperty(ref _xantamOn, value)) return;
            Edited();
        }
    }

    private bool InFile => _state is XantamFireState.On or XantamFireState.Outdated;

    /// <summary>Saving takes it out (it is in the file and the box isn't ticked).</summary>
    private bool TakingOut => InFile && !_xantamOn;

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

    /// <summary>The damage boxes of ranks 1-5.</summary>
    public IReadOnlyList<XantamRankRow> Ranks { get; }

    private void Fill(XantamFireSettings s)
    {
        SetProperty(ref _price, s.Price.ToString(), nameof(Price));
        SetProperty(ref _mana, NumberText.Format(s.Mana), nameof(Mana));
        for (var r = 0; r < Ranks.Count; r++) Ranks[r].Fill(s.Damage[r]);
    }

    /// <summary>The boxes back to what featpatch.py writes: price 16, 15 mana, 1-2 / 1-3 / 2-4 / 2-5 / 3-6.</summary>
    public void Defaults()
    {
        Fill(XantamFireSettings.Default);
        Edited();
    }

    /// <summary>The settings in the boxes, or null with <paramref name="problem"/> saying what is wrong.</summary>
    public XantamFireSettings? TrySettings(out string? problem)
    {
        problem = null;
        if (!NumberText.TryParseInteger(_price, out var price))
        {
            problem = "The price is a whole number from 0 to 30.";
            return null;
        }

        if (!NumberText.TryParseDecimal(_mana, out var mana))
        {
            problem = "The mana a cast takes is a number, such as 15 or 7.5.";
            return null;
        }

        var damage = new XantamFireDamage[Ranks.Count];
        for (var r = 0; r < Ranks.Count; r++)
        {
            if (!NumberText.TryParseInteger(Ranks[r].Min, out var min) || !NumberText.TryParseInteger(Ranks[r].Max, out var max))
            {
                problem = $"Rank {r + 1}: the damage is two whole numbers from 0 to 255.";
                return null;
            }

            damage[r] = new XantamFireDamage(min, max);
        }

        var s = new XantamFireSettings(price, (float)mana, damage);
        problem = s.Problem();
        return problem == null ? s : null;
    }

    /// <summary>What is wrong with the boxes (only while Xantam's Fire is to be in), or null.</summary>
    public string? SettingsProblem => _xantamOn && TrySettings(out var p) == null ? p : null;

    /// <summary>What the ranks cost with the price in the box ("16, 17, 19, 21 and 25 feat points"), or "" when it isn't a price.</summary>
    public string RankCostText
    {
        get
        {
            if (!NumberText.TryParseInteger(_price, out var price) || price is < 0 or > XantamFireSettings.MaxPrice) return "";
            if (price == 0) return "Every rank is free (for testing).";
            var costs = Enumerable.Range(0, XantamFireSettings.Ranks).Select(h => XantamFireSettings.RankCost(price, h).ToString()).ToList();
            return $"Ranks 1-5 cost {string.Join(", ", costs.Take(4))} and {costs[4]} feat points.";
        }
    }

    /// <summary>True when saving changes Xantam's Fire in the executable.</summary>
    public bool XantamChanged =>
        _xantamOn != InFile || _state == XantamFireState.Outdated ||
        (_xantamOn && (TrySettings(out _) is not { } s || !s.Equals(_loaded)));

    /// <summary>What is in the file and what saving does, in a sentence or two.</summary>
    public string XantamStatus
    {
        get
        {
            switch (_state)
            {
                case XantamFireState.Unrecognized:
                    return _detail + " Xantam's Fire can't be put in or taken out here.";
                case XantamFireState.Outdated:
                    return _xantamOn
                        ? "The third test version is in (its spell area, which only three-player builds load). Saving updates it to the " +
                          "version that lives in game code nothing runs, so it works with or without three players; its settings are kept."
                        : "The third test version is taken out when you save: the game's code back, its spell area empty again.";
                case XantamFireState.On:
                    if (!_xantamOn)
                        return "Xantam's Fire is taken out when you save: the game's own code and bytes go back, exactly. A saved game " +
                               "whose Adrianna learnt it keeps the slot, but the spell then does nothing.";
                    return XantamChanged
                        ? "Xantam's Fire is in. Its new settings are written when you save."
                        : "Xantam's Fire is in: Adrianna's level-up screen offers it after Targetting (5 ranks).";
                default:
                    return _xantamOn
                        ? "Xantam's Fire goes in when you save: Adrianna's level-up screen then offers it after Targetting (5 ranks). " +
                          "It works with or without three players."
                        : "Not in this executable. Tick the box to give Adrianna Xantam's Fire.";
            }
        }
    }

    /// <summary>The settings to save, checked (null when Xantam's Fire is to be out); throws <see cref="ArgumentException"/> on a bad box.</summary>
    public XantamFireSettings? ReadXantamFire()
    {
        if (!_xantamOn) return null;
        return TrySettings(out var problem) ?? throw new ArgumentException("Xantam's Fire (Enhancements tab): " + problem);
    }

    /// <summary>
    /// Writes Xantam's Fire into the executable as the tab says: put in, updated, its settings changed, or taken out
    /// (<paramref name="settings"/> from <see cref="ReadXantamFire"/>). Throws InvalidOperationException when the file can't take it.
    /// </summary>
    public void ApplyXantamFire(XantamFireSettings? settings)
    {
        if (!XantamUsable || !XantamChanged) return;
        if (settings != null) _exe.SetXantamFire(settings);
        else _exe.RemoveXantamFire();
    }

    // ═══════════════════════════════ GTEXT.LMP: the names ═══════════════════════════════

    /// <summary>Xantam's Fire will be in the saved executable.</summary>
    private bool WillBeIn => XantamUsable && _xantamOn;

    /// <summary>GTEXT.LMP has to change: the names go in (it will be in and they are missing) or come out (taken out here).</summary>
    public bool NamesPending =>
        _names() is { } names && (WillBeIn ? !XantamFire.HasNames(names) : TakingOut && XantamFire.HasAnyName(names));

    /// <summary>Called when the Items tab loads or saves GTEXT.LMP: the names' status follows it.</summary>
    public void NamesReloaded() => Edited();

    /// <summary>Puts the names in GTEXT.LMP, or takes them out (see <see cref="NamesPending"/>); called just before it is written.</summary>
    public void ApplyNames()
    {
        if (_names() is not { } names || !NamesPending) return;
        if (WillBeIn) XantamFire.AddNames(names);
        else XantamFire.RemoveNames(names);
        _namesApplied = true;
    }

    public string NamesStatus
    {
        get
        {
            var names = _names();
            if (names == null)
                return WillBeIn
                    ? "GTEXT.LMP isn't loaded (Items tab, Open GTEXT.LMP…): without its names the level-up screen and the HUD show " +
                      "NF:xantamfire_name."
                    : "GTEXT.LMP isn't loaded (Items tab).";
            if (WillBeIn)
                return XantamFire.HasNames(names)
                    ? "GTEXT.LMP has its name and description, in all five languages."
                    : "Its name and description (in all five languages) are added to GTEXT.LMP when you save.";
            return TakingOut && XantamFire.HasAnyName(names)
                ? "Its name and description are taken out of GTEXT.LMP when you save."
                : "Its name and description go in GTEXT.LMP with it.";
        }
    }

    // ═══════════════════════════════ ICONS.LMP: the HUD icon ═══════════════════════════════

    public string? IconsPath { get; private set; }

    public bool IconsLoaded => _icons != null;

    /// <summary>Uses <paramref name="data"/> as ICONS.LMP; throws <see cref="System.IO.InvalidDataException"/> when the icon can't go in it.</summary>
    public void LoadIcons(byte[] data, string path)
    {
        if (XantamFireIcon.Problem(data) is { } problem) throw new System.IO.InvalidDataException(problem);
        _icons = data;
        IconsPath = path;
        OnPropertyChanged(nameof(IconsPath));
        OnPropertyChanged(nameof(IconsLoaded));
        Edited();
    }

    public bool IconIn => _icons != null && XantamFireIcon.IsIn(_icons);

    /// <summary>ICONS.LMP has to change: the icon goes in (it will be in) or comes out (taken out here).</summary>
    public bool IconsNeedSaving => _icons != null && (WillBeIn ? !IconIn : TakingOut && IconIn);

    /// <summary>ICONS.LMP as it is to be saved (see <see cref="IconsNeedSaving"/>).</summary>
    public byte[] IconsToSave()
    {
        if (_icons == null) throw new InvalidOperationException("ICONS.LMP isn't loaded.");
        if (!IconsNeedSaving) return _icons;
        return WillBeIn ? XantamFireIcon.AddTo(_icons) : XantamFireIcon.RemoveFrom(_icons);
    }

    /// <summary>Called after ICONS.LMP was saved: what was written is the new starting point.</summary>
    public void IconsSaved(byte[] saved, string path) => LoadIcons(saved, path);

    public string IconStatus
    {
        get
        {
            if (_icons == null)
                return WillBeIn
                    ? "ICONS.LMP isn't loaded: open it from the game's data folder to add the spell's icon (the beholder's eye over " +
                      "fire trails). Without it the HUD shows Fire Shield's icon; nothing else changes."
                    : "ICONS.LMP isn't loaded.";
            if (WillBeIn)
                return IconIn
                    ? $"{IconsPath} has its icon (icon_xantamfire.tex)."
                    : $"Its icon (icon_xantamfire.tex) is added to {IconsPath} when you save: the game finds every other icon as before.";
            return TakingOut && IconIn
                ? $"Its icon is taken out of {IconsPath} when you save: the file is as it was before the icon went in."
                : $"{IconsPath}: " + (IconIn ? "it has the icon." : "the icon goes in with the spell.");
        }
    }

    /// <summary>
    /// After saving, what the game will lack: the names or the icon, when GTEXT.LMP or ICONS.LMP wasn't loaded or saved
    /// (<paramref name="namesWritten"/>: GTEXT.LMP was written); null when nothing.
    /// </summary>
    public string? FilesNote(bool namesWritten)
    {
        if (!WillBeIn) return null;
        var missing = new List<string>();
        if (_names() is not { } names || !XantamFire.HasNames(names) || (_namesApplied && !namesWritten))
            missing.Add("GTEXT.LMP still needs Xantam's Fire's name and description (load it on the Items tab and save again), or the game shows NF:xantamfire_name");
        if (!IconIn)
            missing.Add("ICONS.LMP doesn't have its icon yet (open it on the Enhancements tab and save again), so the HUD shows Fire Shield's");
        return missing.Count == 0 ? null : string.Join(". ", missing) + ".";
    }

    // ═══════════════════════════════ room for more ═══════════════════════════════

    /// <summary>Where new feats go and how much room is left.</summary>
    public string PoolText
    {
        get
        {
            var used = WillBeIn ? BgdaExecutable.XantamFireBytes : 0;
            return $"New feats go into game code that nothing ever runs: {BgdaExecutable.FeatPoolStretches} stretches, " +
                   $"{BgdaExecutable.FeatPoolBytes:N0} bytes, the same in the stock executable and every three-player build. " +
                   (used > 0 ? $"Xantam's Fire takes {used:N0} bytes of it; " : "") +
                   $"{BgdaExecutable.FeatPoolBytes - used:N0} bytes are free for more.";
        }
    }

    /// <summary>Refreshes everything the boxes and the checkbox feed.</summary>
    private void Edited()
    {
        OnPropertyChanged(nameof(XantamStatus));
        OnPropertyChanged(nameof(XantamChanged));
        OnPropertyChanged(nameof(SettingsProblem));
        OnPropertyChanged(nameof(RankCostText));
        OnPropertyChanged(nameof(NamesStatus));
        OnPropertyChanged(nameof(NamesPending));
        OnPropertyChanged(nameof(IconStatus));
        OnPropertyChanged(nameof(IconIn));
        OnPropertyChanged(nameof(IconsNeedSaving));
        OnPropertyChanged(nameof(PoolText));
    }
}
