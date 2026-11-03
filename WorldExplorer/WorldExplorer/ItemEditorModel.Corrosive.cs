// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/ItemEditorModel.Corrosive.cs
//
// The Corrosive page of the Items tab (ItemEditorView.xaml): Corrosive and Corrosive Burst, the weapon
// modifiers with acid damage on property bits 59 and 60 (BgdaExecutable.Corrosive.cs). The patch that
// makes the game use them goes in when the executable is saved with either of them in use (an item, a
// shop entry or a random-loot list), or when it is turned on here (for item text in levels and scripts,
// which aren't in the executable); an older version of it is brought up to date. Their names go in
// GTEXT.LMP and the item card's acid drop in INVENTRY.LMP, both saved after the executable. Taking the
// patch out here takes the names and the drop out again. The acid glow (a style and a first rule) is
// one click away. Every box is text, so a half-typed value never throws; BuildEdits gathers the changes.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

public sealed partial class ItemEditorModel
{
    private CorrosiveSettings _loadedCorrosive = CorrosiveSettings.Default;
    private bool _corrosiveOn;
    private bool _corrosiveTakeOut;
    private string _corDie = "", _corCriticalDie = "", _corPoison = "", _corResisted = "";
    private byte[]? _inventry;
    private string _acidGlowResult = "";

    /// <summary>Whether the executable has the Corrosive patch, as loaded.</summary>
    public CorrosiveState CorrosiveState { get; private set; } = CorrosiveState.Off;

    /// <summary>The library's sentence about <see cref="CorrosiveState"/>.</summary>
    public string CorrosiveDetail { get; private set; } = "";

    /// <summary>Why Corrosive and Corrosive Burst can't be used with this executable (something else is where the patch goes), or null.</summary>
    public string? CorrosiveProblem => IsSupported && CorrosiveState == CorrosiveState.Unknown ? CorrosiveDetail : null;

    /// <summary>The patch is in, or can be put in.</summary>
    public bool CorrosiveUsable => IsSupported && CorrosiveState != CorrosiveState.Unknown;

    private void LoadCorrosive()
    {
        CorrosiveState = _exe.GetCorrosiveState(out var detail);
        CorrosiveDetail = detail;
        _loadedCorrosive = _exe.GetCorrosiveSettings();
        _corrosiveOn = CorrosiveState is CorrosiveState.On or CorrosiveState.Outdated;
        if (_corrosiveOn) _corrosiveTakeOut = false;
        FillCorrosiveBoxes(_loadedCorrosive);
        OnPropertyChanged(nameof(CorrosiveState));
        OnPropertyChanged(nameof(CorrosiveDetail));
        OnPropertyChanged(nameof(CorrosiveProblem));
        OnPropertyChanged(nameof(CorrosiveUsable));
        CorrosiveEdited();
    }

    // ═══════════════════════════════ on and off ═══════════════════════════════

    /// <summary>True when the patch is wanted whatever uses it: it is in the executable, or it was turned on here.</summary>
    public bool CorrosiveOn => _corrosiveOn;

    /// <summary>The executable will have the patch once saved: it is wanted, or something uses Corrosive or Corrosive Burst.</summary>
    public bool CorrosiveWillBeIn => CorrosiveUsable && (_corrosiveOn || CorrosiveInUse);

    public bool CanTurnOnCorrosive => CorrosiveUsable && !_corrosiveOn;

    /// <summary>The patch can be taken out: it is wanted and nothing uses it (otherwise it stays in).</summary>
    public bool CanTurnOffCorrosive => _corrosiveOn && !CorrosiveInUse;

    /// <summary>Puts the patch in when the executable is saved, even when nothing in it uses Corrosive yet; false when it can't be used here.</summary>
    public bool TurnOnCorrosive()
    {
        if (!CanTurnOnCorrosive) return false;
        _corrosiveOn = true;
        _corrosiveTakeOut = false;
        CorrosiveEdited();
        return true;
    }

    /// <summary>
    /// Takes the patch out when the executable is saved, with the names in GTEXT.LMP and the acid drop in INVENTRY.LMP;
    /// false while something uses Corrosive or Corrosive Burst.
    /// </summary>
    public bool TurnOffCorrosive()
    {
        if (!CanTurnOffCorrosive) return false;
        _corrosiveOn = false;
        _corrosiveTakeOut = CorrosiveState is CorrosiveState.On or CorrosiveState.Outdated;
        CorrosiveEdited();
        return true;
    }

    // ═══════════════════════════════ what uses it ═══════════════════════════════

    /// <summary>
    /// What has Corrosive or Corrosive Burst as edited here: items, shop entries whose text adds it (one selling an item that
    /// has it counts as that item) and random-loot lists.
    /// </summary>
    public List<string> CorrosiveUsers
    {
        get
        {
            var list = new List<string>();
            if (!IsSupported) return list;
            var items = CurrentItems();
            for (var i = 0; i < items.Count && i < Items.Count; i++)
            {
                if (Corrosive.Uses(items[i].Flags) && !list.Contains(Items[i].DisplayName)) list.Add(Items[i].DisplayName);
            }

            foreach (var shop in _allShops)
            {
                var text = shop.Text.Trim();
                var parsed = ItemText.Parse(text, items);
                if (parsed.ItemIndex >= 0 && Corrosive.Uses(parsed.Flags & ~items[parsed.ItemIndex].Flags))
                    list.Add($"{ShopName(shop.Stock)} #{shop.Position} ({text})");
            }

            foreach (var choice in LootLists)
            {
                if (_loot.TryGetValue(choice.Value, out var rows) && rows.Any(r => Corrosive.Uses(r.Value))) list.Add($"the {choice.Text} loot list");
            }

            return list;
        }
    }

    public bool CorrosiveInUse => CorrosiveUsers.Count > 0;

    private static string ShopName(ShopStock stock) => ShopStocks.FirstOrDefault(s => s.Value == stock)?.Text.Split(' ')[0] ?? stock.ToString();

    public string CorrosiveUsersText
    {
        get
        {
            var users = CorrosiveUsers;
            if (users.Count == 0)
                return "Nothing in the executable has Corrosive or Corrosive Burst yet. Tick one on an item (Items page), sell one in a shop " +
                       "(item text such as longsword+2+acidburst) or put one in a random-loot list (Loot & effects).";
            var shown = users.Count <= 12 ? users : users.Take(12).Append($"and {users.Count - 12} more").ToList();
            return "Used by: " + string.Join(", ", shown) + ".";
        }
    }

    /// <summary>What the page says about the patch.</summary>
    public string CorrosiveStatus
    {
        get
        {
            if (!IsSupported) return "";
            if (CorrosiveState == CorrosiveState.Unknown)
                return "Corrosive and Corrosive Burst can't be used with this executable" +
                       (CorrosiveInUse ? ": the items, shops and loot lists that have them can't be saved until they're taken off." : ".");
            if (!CorrosiveWillBeIn)
            {
                return CorrosiveState == CorrosiveState.Off
                    ? "No Corrosive patch. It goes in when you save an executable that uses Corrosive or Corrosive Burst. Use it without that " +
                      "for item text in levels and scripts (chests, rewards), which WorldExplorer can't see."
                    : "The Corrosive patch will be taken out when you save, with the names in GTEXT.LMP and the acid drop in INVENTRY.LMP.";
            }

            var reason = _corrosiveOn ? "" : " because Corrosive or Corrosive Burst is in use";
            return CorrosiveState switch
            {
                CorrosiveState.On => "The Corrosive patch (version 3) is in." + (CorrosiveSettingsChanged ? " Its settings change when you save." : ""),
                CorrosiveState.Outdated => CorrosiveDetail,
                _ => $"The Corrosive patch (version {BgdaExecutable.CorrosivePatchVersion}) will be put in when you save{reason}."
            };
        }
    }

    // ═══════════════════════════════ settings ═══════════════════════════════

    public string CorrosiveDie { get => _corDie; set => SetCorrosiveBox(ref _corDie, value); }
    public string CorrosiveCriticalDie { get => _corCriticalDie; set => SetCorrosiveBox(ref _corCriticalDie, value); }
    public string CorrosivePoison { get => _corPoison; set => SetCorrosiveBox(ref _corPoison, value); }
    public string CorrosiveResistedPoison { get => _corResisted; set => SetCorrosiveBox(ref _corResisted, value); }

    private void SetCorrosiveBox(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (SetProperty(ref field, value, name)) CorrosiveEdited();
    }

    private void FillCorrosiveBoxes(CorrosiveSettings s)
    {
        _corDie = s.Die.ToString(CultureInfo.InvariantCulture);
        _corCriticalDie = s.CriticalDie.ToString(CultureInfo.InvariantCulture);
        _corPoison = NumberText.Format(s.PoisonSeconds);
        _corResisted = NumberText.Format(s.ResistedPoisonSeconds);
        OnPropertyChanged(nameof(CorrosiveDie));
        OnPropertyChanged(nameof(CorrosiveCriticalDie));
        OnPropertyChanged(nameof(CorrosivePoison));
        OnPropertyChanged(nameof(CorrosiveResistedPoison));
    }

    /// <summary>Puts the settings tried in the game back in the boxes.</summary>
    public void CorrosiveDefaults()
    {
        FillCorrosiveBoxes(CorrosiveSettings.Default);
        CorrosiveEdited();
    }

    /// <summary>The settings as typed, or null with the reason.</summary>
    internal CorrosiveSettings? TryCorrosiveSettings(out string? problem)
    {
        problem = null;
        if (!NumberText.TryParseInteger(_corDie, out var die)) problem = "The acid of every hit: enter a whole number.";
        else if (!NumberText.TryParseInteger(_corCriticalDie, out var critical)) problem = "The acid of a critical hit: enter a whole number.";
        else if (!NumberText.TryParseDecimal(_corPoison, out var poison)) problem = "The poison's seconds: enter a number.";
        else if (!NumberText.TryParseDecimal(_corResisted, out var resisted)) problem = "The poison's seconds when the target resists acid: enter a number.";
        else
        {
            var settings = new CorrosiveSettings(die, critical, (float)poison, (float)resisted);
            problem = settings.Problem();
            return problem == null ? settings : null;
        }

        return null;
    }

    public string? CorrosiveSettingsProblem => TryCorrosiveSettings(out var problem) == null ? problem : null;

    /// <summary>True when the boxes differ from the executable's settings.</summary>
    public bool CorrosiveSettingsChanged => TryCorrosiveSettings(out _) is not { } s || s != _loadedCorrosive;

    /// <summary>What each setting does, in a line.</summary>
    public string CorrosiveSettingsText
    {
        get
        {
            if (TryCorrosiveSettings(out _) is not { } s) return "";
            return $"Every hit with either: 1–{s.Die} acid more. A critical hit with Corrosive Burst: 1–{s.CriticalDie} acid more for each step " +
                   $"of the critical multiplier (x2: once, x3: twice, x4: three times) and {NumberText.Format(s.PoisonSeconds)} s of poison " +
                   $"({NumberText.Format(s.ResistedPoisonSeconds)} s when the target resists acid). Acid is halved when the target resists it, " +
                   "and does nothing when it is immune.";
        }
    }

    // ═══════════════════════════════ GTEXT.LMP: the names ═══════════════════════════════

    /// <summary>
    /// GTEXT.LMP has to change for Corrosive: the names go in (the patch will be in and it lacks them) or come out (the patch is
    /// taken out here and it has them).
    /// </summary>
    public bool CorrosiveNamesPending =>
        Names != null && (CorrosiveWillBeIn ? !Corrosive.HasNames(Names) : _corrosiveTakeOut && HasCorrosiveNames(Names));

    private static bool HasCorrosiveNames(GameText names) => Corrosive.Names.Any(n => names.Get(n.Key, n.Language) != null);

    /// <summary>Puts Corrosive's names in <see cref="Names"/>, or takes them out (see <see cref="CorrosiveNamesPending"/>).</summary>
    private void ApplyCorrosiveNames()
    {
        if (Names == null || !CorrosiveNamesPending) return;
        if (CorrosiveWillBeIn) Corrosive.AddNames(Names);
        else Corrosive.RemoveNames(Names);
    }

    public string CorrosiveNamesStatus
    {
        get
        {
            if (Names == null)
                return CorrosiveWillBeIn
                    ? "GTEXT.LMP isn't loaded (Items page, Open GTEXT.LMP…): the names can't be added, and without them a Corrosive weapon's name " +
                      "shows NF:it_corrosive."
                    : "GTEXT.LMP isn't loaded (Items page).";
            if (CorrosiveWillBeIn)
                return Corrosive.HasNames(Names)
                    ? "GTEXT.LMP has the names: Corrosive and Corrosive Burst in English, and in French, German, Italian and Spanish."
                    : "The names (Corrosive and Corrosive Burst, in all five languages) are added to GTEXT.LMP when you save.";
            return _corrosiveTakeOut && HasCorrosiveNames(Names)
                ? "The names are taken out of GTEXT.LMP when you save."
                : "The names go in GTEXT.LMP with the patch.";
        }
    }

    // ═══════════════════════════════ INVENTRY.LMP: the acid drop ═══════════════════════════════

    public string? InventryPath { get; private set; }

    public bool InventryLoaded => _inventry != null;

    /// <summary>Uses <paramref name="data"/> as INVENTRY.LMP; throws <see cref="System.IO.InvalidDataException"/> when the drop can't go in it.</summary>
    public void LoadInventry(byte[] data, string path)
    {
        if (CorrosiveIcon.Problem(data) is { } problem) throw new System.IO.InvalidDataException(problem);
        _inventry = data;
        InventryPath = path;
        OnPropertyChanged(nameof(InventryPath));
        OnPropertyChanged(nameof(InventryLoaded));
        CorrosiveEdited();
    }

    /// <summary>INVENTRY.LMP has the acid drop.</summary>
    public bool CorrosiveIconIn => _inventry != null && CorrosiveIcon.IsIn(_inventry);

    /// <summary>INVENTRY.LMP has to change: the drop goes in (the patch will be in) or comes out (taken out here).</summary>
    public bool InventryNeedsSaving => _inventry != null && (CorrosiveWillBeIn ? !CorrosiveIconIn : _corrosiveTakeOut && CorrosiveIconIn);

    /// <summary>INVENTRY.LMP as it is to be saved (see <see cref="InventryNeedsSaving"/>).</summary>
    public byte[] InventryToSave()
    {
        if (_inventry == null) throw new InvalidOperationException("INVENTRY.LMP isn't loaded.");
        if (!InventryNeedsSaving) return _inventry;
        return CorrosiveWillBeIn ? CorrosiveIcon.AddTo(_inventry) : CorrosiveIcon.RemoveFrom(_inventry);
    }

    /// <summary>Called after INVENTRY.LMP was saved: what was written is the new starting point.</summary>
    public void InventrySaved(byte[] saved, string path) => LoadInventry(saved, path);

    public string CorrosiveIconStatus
    {
        get
        {
            if (_inventry == null)
                return "INVENTRY.LMP isn't loaded: open it from the game's data folder to " + (CorrosiveWillBeIn || _corrosiveTakeOut
                    ? (CorrosiveWillBeIn ? "add" : "take out") + " the acid drop the item card shows after the element icons (without it the card shows " +
                      "no drop; nothing else changes)."
                    : "see whether it has the acid drop.");
            if (CorrosiveWillBeIn)
                return CorrosiveIconIn
                    ? $"{InventryPath} has the acid drop (tinyacid.tex)."
                    : $"The acid drop (tinyacid.tex) is added to {InventryPath} when you save: the game finds it with every other icon as before.";
            return _corrosiveTakeOut && CorrosiveIconIn
                ? $"The acid drop is taken out of {InventryPath} when you save: the file is as it was before it went in."
                : $"{InventryPath}: " + (CorrosiveIconIn ? "it has the acid drop." : "the acid drop goes in with the patch.");
        }
    }

    /// <summary>
    /// After saving, what the game will lack for Corrosive: the names or the drop when the executable has the patch but GTEXT.LMP or
    /// INVENTRY.LMP wasn't loaded or saved; null when nothing.
    /// </summary>
    public string? CorrosiveFilesNote()
    {
        if (CorrosiveState is not (CorrosiveState.On or CorrosiveState.Outdated)) return null;
        var lines = new List<string>();
        if (Names == null || !Corrosive.HasNames(Names))
            lines.Add("The executable has the Corrosive patch but GTEXT.LMP " + (Names == null ? "wasn't loaded" : "wasn't saved") +
                      " with its names: Corrosive weapons' names show NF:it_corrosive until it has them (Items tab, Corrosive page).");
        if (_inventry == null || !CorrosiveIconIn)
            lines.Add("INVENTRY.LMP " + (_inventry == null ? "wasn't loaded" : "wasn't saved") +
                      " with the acid drop, so the item card shows none (nothing else changes).");
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    // ═══════════════════════════════ the acid glow ═══════════════════════════════

    /// <summary>The acid glow can be added: glow styles can be used, a rule is free and none looks for Corrosive yet.</summary>
    public bool CanAddAcidGlow =>
        GlowStylesUsable && GlowRules.Count < GlowSettings.RuleCount && !GlowRules.Any(r => (r.Bits & Corrosive.Bits) != 0);

    public string AcidGlowResult
    {
        get => _acidGlowResult;
        private set => SetProperty(ref _acidGlowResult, value);
    }

    /// <summary>
    /// Turns glow styles on when they are off, makes the first style after those in use the acid glow and puts the rule
    /// "Corrosive or Corrosive Burst: that style" first (what glow_acid_rule.py writes). False when it can't (and says why).
    /// </summary>
    public bool AddAcidGlow()
    {
        if (!CanAddAcidGlow)
        {
            AcidGlowResult = !GlowStylesUsable ? "Glow styles can't be used with this executable."
                : GlowRules.Count >= GlowSettings.RuleCount ? $"All {GlowSettings.RuleCount} glow rules are in use: remove one first (Glow page)."
                : "A glow rule already looks for Corrosive or Corrosive Burst (Glow page).";
            return false;
        }

        int style;
        try
        {
            var current = new GlowSettings(GlowStyles.Select(s => s.ValueOrSource()), GlowRules.Where(r => r.Bits != 0).Select(r => r.ToRule()));
            Corrosive.WithAcidGlow(current, out style);
        }
        catch (InvalidOperationException e)
        {
            AcidGlowResult = e.Message;
            return false;
        }

        if (!_glowOn && !TurnOnGlowStyles()) return false;
        Quietly(() =>
        {
            GlowStyles[style].Set(Corrosive.AcidGlow);
            var rule = new GlowRuleRow(this, new GlowRule(Corrosive.Bits, style));
            GlowRules.Insert(0, rule);
            SelectedGlowRule = rule;
            GlowRulesChanged();
        });
        AcidGlowResult = $"Style {style} is the acid glow now, and rule 1 gives it to Corrosive and Corrosive Burst weapons (Glow page).";
        CorrosiveEdited();
        return true;
    }

    // ═══════════════════════════════ refreshing and saving ═══════════════════════════════

    /// <summary>Something Corrosive depends on changed (an item, a shop entry, a loot list, a box, GTEXT.LMP or INVENTRY.LMP).</summary>
    internal void CorrosiveEdited()
    {
        foreach (var p in new[]
                 {
                     nameof(CorrosiveOn), nameof(CorrosiveWillBeIn), nameof(CanTurnOnCorrosive), nameof(CanTurnOffCorrosive), nameof(CorrosiveStatus),
                     nameof(CorrosiveUsersText), nameof(CorrosiveSettingsProblem), nameof(CorrosiveSettingsText), nameof(CorrosiveNamesStatus),
                     nameof(CorrosiveIconStatus), nameof(CanAddAcidGlow), nameof(CorrosiveChanged)
                 })
        {
            OnPropertyChanged(p);
        }
    }

    /// <summary>True when saving changes the patch: puts it in, brings it up to date, changes its settings or takes it out.</summary>
    public bool CorrosiveChanged =>
        IsSupported && (CorrosiveWillBeIn
            ? CorrosiveState != CorrosiveState.On || CorrosiveSettingsChanged
            : CorrosiveState is CorrosiveState.On or CorrosiveState.Outdated);

    /// <summary>The Corrosive part of <see cref="BuildEdits"/>: throws <see cref="ArgumentException"/> when it can't be saved.</summary>
    private void BuildCorrosiveEdits(ItemEdits edits)
    {
        if (CorrosiveWillBeIn)
        {
            var settings = TryCorrosiveSettings(out var problem) ?? throw new ArgumentException("Corrosive: " + problem);
            if (CorrosiveState != CorrosiveState.On || settings != _loadedCorrosive) edits.Corrosive = settings;
            return;
        }

        if (CorrosiveUsers is { Count: > 0 } users)
            throw new ArgumentException($"{users[0]} has Corrosive or Corrosive Burst, but they can't be used with this executable: {CorrosiveDetail}");
        if (CorrosiveState is CorrosiveState.On or CorrosiveState.Outdated) edits.RemoveCorrosive = true;
    }
}
