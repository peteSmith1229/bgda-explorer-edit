// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/ItemEditorModel.Glow.cs
//
// The Glow tab of the Items tab (ItemEditorView.xaml) and each item's glow setting. Without glow
// styles the tab edits the game's own enchant glow colours (the ENCHANT GLOW table, style 0 here).
// With glow styles (BgdaExecutable.Glow.cs) it edits 16 styles, up to 16 rules that give items
// with some modifiers a style (or no glow), first match first, and each item's own setting
// (automatic, a style, never), item by item on the Items tab or for a group of items at once.
// The tested set (the colours tried in the game) and the game's own glow are ready-made.
// Every box is text, so a half-typed value never throws; BuildEdits gathers the changes.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using JetBlackEngineLib.Data.Executable;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>The ready-made glow settings.</summary>
public enum GlowPreset
{
    /// <summary>Every style the game's own colours, no rules, every item automatic: the game's own glow.</summary>
    Game,

    /// <summary>The nine styles and eight rules tried in the game (Frost, Shock, Fire ...), the short sword Royal, the warhammer never.</summary>
    Tested
}

/// <summary>A named set of glow colours and strengths a style can take.</summary>
public sealed record GlowColourPreset(string Name, GlowStyle Style)
{
    public override string ToString() => Name;
}

/// <summary>A group of items whose glow can be set at once.</summary>
public sealed record GlowGroup(string Text, Func<GameItem, bool> Contains)
{
    public override string ToString() => Text;
}

public sealed partial class ItemEditorModel
{
    private GlowSettings _loadedGlow = GlowSettings.Plain(new EnchantGlow(new Rgb(10, 10, 68), new Rgb(60, 60, 68)));
    private EnchantGlow _loadedColours = new(new Rgb(10, 10, 68), new Rgb(60, 60, 68));
    private bool _glowOn;
    private GlowSettings? _currentGlow;
    private int _quiet;

    /// <summary>True while a change to many rows at once holds back the refreshing each one would cause.</summary>
    internal bool Quiet => _quiet > 0;

    /// <summary>Changes many rows (presets, groups, turning glow styles off) and refreshes once at the end.</summary>
    private void Quietly(Action change)
    {
        _quiet++;
        try
        {
            change();
        }
        finally
        {
            _quiet--;
        }

        if (_quiet == 0) ItemEdited();
    }

    /// <summary>Whether the executable has glow styles, as loaded.</summary>
    public GlowStylesState GlowState { get; private set; } = GlowStylesState.Off;

    /// <summary>Why glow styles can't be used with this executable (another patch changed the glow function), or null.</summary>
    public string? GlowProblem { get; private set; }

    /// <summary>True while glow styles are on: in the executable, or turned on here and put in when it's saved.</summary>
    public bool GlowStylesOn
    {
        get => _glowOn;
        private set
        {
            if (!SetProperty(ref _glowOn, value)) return;
            foreach (var p in new[] { nameof(CanTurnOnGlowStyles), nameof(GlowStatus), nameof(CanAddGlowRule), nameof(GlowRulesText), nameof(CanEditSelectedGlowRule) })
                OnPropertyChanged(p);
            GlowEdited();
            foreach (var row in GlowStyles) row.Refresh();
            foreach (var row in Items) row.RefreshDerived();
        }
    }

    public bool CanTurnOnGlowStyles => IsSupported && !_glowOn && GlowProblem == null;

    /// <summary>Glow styles can be used with this executable: they are on, or can be turned on.</summary>
    public bool GlowStylesUsable => IsSupported && GlowProblem == null;

    /// <summary>What the Glow tab says about the glow this executable will have.</summary>
    public string GlowStatus
    {
        get
        {
            if (GlowProblem != null) return GlowProblem + " Only the game's own glow colours (style 0) can be edited.";
            if (!_glowOn)
            {
                return (GlowState == GlowStylesState.On ? "Glow styles will be taken out when you save: " : "The game's own glow: ") +
                       "every enchanted item glows in style 0's colours (the ENCHANT GLOW colours), Flaming ones not at all. " +
                       "Use glow styles to give each item, and each group of modifiers, its own colours.";
            }

            return (GlowState == GlowStylesState.On ? "Glow styles are on. " : "Glow styles will be put in when you save. ") +
                   "An item set to Never doesn't glow; one with its own style glows in it when it has +N or a modifier a rule " +
                   "looks for; an automatic one glows as the first rule that matches it says, or (no rule) as the game does, in style 0.";
        }
    }

    public ObservableCollection<GlowStyleRow> GlowStyles { get; } = new();
    public ObservableCollection<GlowRuleRow> GlowRules { get; } = new();

    private GlowRuleRow? _selectedGlowRule;

    /// <summary>The rule whose modifiers are shown for ticking.</summary>
    public GlowRuleRow? SelectedGlowRule
    {
        get => _selectedGlowRule;
        set
        {
            if (!SetProperty(ref _selectedGlowRule, value)) return;
            OnPropertyChanged(nameof(CanEditSelectedGlowRule));
        }
    }

    public bool CanEditSelectedGlowRule => _selectedGlowRule != null && _glowOn;

    /// <summary>What an item's glow setting can be: Automatic, style 0 to 15, Never.</summary>
    public IReadOnlyList<GlowChoice> ItemGlowChoices { get; } =
        new[] { new GlowChoice(ItemGlow.Automatic, "Automatic (the rules decide)") }
            .Concat(Enumerable.Range(0, GlowSettings.StyleCount).Select(s => new GlowChoice(ItemGlow.InStyle(s), $"Style {s}")))
            .Append(new GlowChoice(ItemGlow.Never, "Never"))
            .ToArray();

    /// <summary>What a rule can give: style 0 to 15, or no glow (-1).</summary>
    public IReadOnlyList<GlowStyleChoice> RuleGlowChoices { get; } =
        Enumerable.Range(0, GlowSettings.StyleCount).Select(s => new GlowStyleChoice(s, $"Style {s}"))
            .Append(new GlowStyleChoice(GlowRuleRow.NoGlow, "No glow"))
            .ToArray();

    private void LoadGlow()
    {
        GlowState = IsSupported ? _exe.GetGlowStylesState() : GlowStylesState.Unknown;
        GlowProblem = GlowState == GlowStylesState.Unknown && IsSupported ? _exe.GlowStylesProblem() : null;
        _loadedColours = _exe.GetEnchantGlow();
        _loadedGlow = GlowState == GlowStylesState.On ? _exe.GetGlowSettings() : GlowSettings.Plain(_loadedColours);
        _glowOn = GlowState == GlowStylesState.On;

        GlowStyles.Clear();
        for (var i = 0; i < GlowSettings.StyleCount; i++) GlowStyles.Add(new GlowStyleRow(this, i, _loadedGlow.Styles[i]));
        GlowRules.Clear();
        foreach (var rule in _loadedGlow.Rules) GlowRules.Add(new GlowRuleRow(this, rule));
        _selectedGlowRule = GlowRules.FirstOrDefault();

        foreach (var p in new[]
                 {
                     nameof(GlowState), nameof(GlowProblem), nameof(GlowStylesOn), nameof(CanTurnOnGlowStyles), nameof(GlowStylesUsable), nameof(GlowStatus),
                     nameof(SelectedGlowRule), nameof(CanEditSelectedGlowRule), nameof(CanAddGlowRule), nameof(GlowRulesText)
                 })
        {
            OnPropertyChanged(p);
        }

        GlowEdited();
    }

    // ═══════════════════════════════ on and off ═══════════════════════════════

    /// <summary>Turns glow styles on (they go in when the executable is saved); false when they can't be used here.</summary>
    public bool TurnOnGlowStyles()
    {
        if (!CanTurnOnGlowStyles) return false;
        GlowStylesOn = true;
        return true;
    }

    /// <summary>True when turning glow styles off loses something: a rule, a style unlike style 0, or an item's setting.</summary>
    public bool GlowSettingsWouldBeLost =>
        _glowOn && (GlowRules.Count > 0 || Items.Any(r => !r.Glow.IsAutomatic) ||
                    GlowStyles.Skip(1).Any(s => s.ValueOrSource() != GlowStyles[0].ValueOrSource()) ||
                    GlowStyles[0].ValueOrSource() is var s0 && (s0.Halo.Strength != GlowStyle.GameHaloStrength || s0.Core.Strength != GlowStyle.GameCoreStrength));

    /// <summary>
    /// Back to the game's own glow: every item automatic, style 0's strengths the game's (its colours stay the
    /// ENCHANT GLOW colours). Glow styles come out when the executable is saved.
    /// </summary>
    public void TurnOffGlowStyles()
    {
        if (!_glowOn) return;
        Quietly(() =>
        {
            foreach (var row in Items) row.Glow = ItemGlow.Automatic;
            var s0 = GlowStyles[0];
            s0.HaloStrength = GlowStyle.GameHaloStrength.ToString(CultureInfo.InvariantCulture);
            s0.CoreStrength = GlowStyle.GameCoreStrength.ToString(CultureInfo.InvariantCulture);
            GlowStylesOn = false;
        });
    }

    // ═══════════════════════════════ ready-made settings ═══════════════════════════════

    private static GlowStyle Style(int hr, int hg, int hb, int cr, int cg, int cb) =>
        new(new GlowLayer(new Rgb(hr, hg, hb), GlowStyle.GameHaloStrength), new GlowLayer(new Rgb(cr, cg, cb), GlowStyle.GameCoreStrength));

    /// <summary>Colours a style can take: the game's own, the eight tried in the game, and a few more.</summary>
    public static IReadOnlyList<GlowColourPreset> GlowColourPresets { get; } = new[]
    {
        new GlowColourPreset("The game's own (dark blue)", GlowStyle.Game),
        new GlowColourPreset("Frost (icy blue)", Style(0, 70, 110, 120, 220, 255)),
        new GlowColourPreset("Shock (yellow)", Style(110, 100, 0, 255, 255, 120)),
        new GlowColourPreset("Fire (orange)", Style(110, 40, 0, 255, 140, 20)),
        new GlowColourPreset("Blood (red)", Style(110, 0, 0, 255, 30, 30)),
        new GlowColourPreset("Silver", Style(60, 60, 70, 220, 220, 235)),
        new GlowColourPreset("Royal (purple)", Style(70, 0, 110, 200, 90, 255)),
        new GlowColourPreset("Holy (pale gold)", Style(110, 90, 30, 255, 240, 170)),
        new GlowColourPreset("Haste (green)", Style(0, 110, 20, 120, 255, 140)),
        new GlowColourPreset("Gold", Style(110, 80, 0, 255, 210, 60)),
        new GlowColourPreset("Venom (yellow-green)", Style(40, 90, 0, 170, 255, 60)),
        new GlowColourPreset("Shadow (dark violet)", Style(30, 0, 40, 110, 40, 140)),
        new GlowColourPreset("Ember (deep orange)", Style(90, 20, 0, 255, 90, 20)),
        new GlowColourPreset("Moonlight (white)", Style(70, 90, 110, 230, 245, 255))
    };

    private static ulong Bits(params int[] bits) => bits.Aggregate(0UL, (m, b) => m | 1UL << b);

    /// <summary>The rules tried in the game, in order.</summary>
    internal static readonly GlowRule[] TestedRules =
    {
        new(Bits(ItemBits.Disruption), 7),
        new(Bits(ItemBits.Ice, ItemBits.IceBurst), 1),
        new(Bits(ItemBits.Shock, ItemBits.ShockBurst), 2),
        new(Bits(ItemBits.Fire, ItemBits.FireBurst), 3),
        new(Bits(ItemBits.Wounding), 4),
        new(Bits(ItemBits.Speed, ItemBits.SuperHaste), 8),
        new(Bits(ItemBits.Keen), 5),
        new(Bits(ItemBits.Defence), null)
    };

    /// <summary>
    /// Replaces the styles, rules and every item's glow setting with a ready-made set (turning glow styles on
    /// first when they can be used): <see cref="GlowPreset.Game"/> is the game's own glow, <see cref="GlowPreset.Tested"/>
    /// the set tried in the game. False when glow styles can't be used here.
    /// </summary>
    public bool ApplyGlowPreset(GlowPreset preset)
    {
        if (!_glowOn && !TurnOnGlowStyles()) return false;
        Quietly(() =>
        {
            for (var i = 0; i < GlowSettings.StyleCount; i++)
            {
                var style = preset == GlowPreset.Tested && i is >= 1 and <= 8 ? GlowColourPresets[i].Style : GlowStyle.Game;
                GlowStyles[i].Set(style);
            }

            GlowRules.Clear();
            if (preset == GlowPreset.Tested)
            {
                foreach (var rule in TestedRules) GlowRules.Add(new GlowRuleRow(this, rule));
            }

            foreach (var row in Items)
            {
                row.Glow = preset != GlowPreset.Tested ? ItemGlow.Automatic : row.Name.Trim() switch
                {
                    "shortsword" => ItemGlow.InStyle(6),
                    "warhammer" => ItemGlow.Never,
                    _ => ItemGlow.Automatic
                };
            }

            SelectedGlowRule = GlowRules.FirstOrDefault();
            GlowRulesChanged();
        });
        return true;
    }

    // ═══════════════════════════════ rules ═══════════════════════════════

    public bool CanAddGlowRule => _glowOn && GlowRules.Count < GlowSettings.RuleCount;

    public string GlowRulesText => !_glowOn
        ? "Rules need glow styles."
        : $"{GlowRules.Count} of {GlowSettings.RuleCount} rules. The first rule that matches an item decides; a rule works " +
          "even on an item with no +N, so \"Keen → a style\" makes a plain Keen sword glow.";

    /// <summary>Adds a rule after the selected one (nothing ticked yet, style 1) and selects it; null when no more fit.</summary>
    public GlowRuleRow? AddGlowRule()
    {
        if (!CanAddGlowRule) return null;
        var row = new GlowRuleRow(this, new GlowRule(0, Math.Min(1, GlowSettings.StyleCount - 1)));
        var at = _selectedGlowRule != null ? GlowRules.IndexOf(_selectedGlowRule) + 1 : GlowRules.Count;
        GlowRules.Insert(at, row);
        SelectedGlowRule = row;
        GlowRulesChanged();
        return row;
    }

    public void RemoveGlowRule(GlowRuleRow row)
    {
        var at = GlowRules.IndexOf(row);
        if (at < 0) return;
        GlowRules.RemoveAt(at);
        SelectedGlowRule = GlowRules.Count == 0 ? null : GlowRules[Math.Min(at, GlowRules.Count - 1)];
        GlowRulesChanged();
    }

    /// <summary>Moves a rule up (-1) or down (+1): earlier rules win.</summary>
    public void MoveGlowRule(GlowRuleRow row, int delta)
    {
        var at = GlowRules.IndexOf(row);
        var to = at + delta;
        if (at < 0 || to < 0 || to >= GlowRules.Count) return;
        GlowRules.Move(at, to);
        SelectedGlowRule = row;
        GlowRulesChanged();
    }

    private void GlowRulesChanged()
    {
        foreach (var r in GlowRules) r.RefreshPosition();
        OnPropertyChanged(nameof(CanAddGlowRule));
        OnPropertyChanged(nameof(GlowRulesText));
        OnPropertyChanged(nameof(CanEditSelectedGlowRule));
        GlowEdited();
    }

    // ═══════════════════════════════ groups of items ═══════════════════════════════

    private static Func<GameItem, bool> HasBit(int bit) => i => ItemBits.Has(i.Flags, bit);

    /// <summary>Groups of items whose glow setting can be set at once.</summary>
    public static IReadOnlyList<GlowGroup> GlowGroups { get; } = new[]
    {
        new GlowGroup("Every item", _ => true),
        new GlowGroup("Every weapon", i => i.Category == ItemCategory.Weapon),
        new GlowGroup("Every piece of armour (shields too)", i => i.Category == ItemCategory.Armour),
        new GlowGroup("Every other item (rings, amulets, potions …)", i => i.Category == ItemCategory.Other),
        new GlowGroup("One-handed weapons", i => i.Category == ItemCategory.Weapon && i.Slot == ItemSlot.OneHanded),
        new GlowGroup("Two-handed weapons", i => i.Category == ItemCategory.Weapon && i.Slot == ItemSlot.TwoHanded),
        new GlowGroup("Swords and daggers (bladed)", HasBit(17)),
        new GlowGroup("Blunt weapons", HasBit(ItemBits.Blunt)),
        new GlowGroup("Scimitars", HasBit(18)),
        new GlowGroup("Axes, hammers and bows", HasBit(19)),
        new GlowGroup("Daggers", HasBit(55)),
        new GlowGroup("Bows", i => i.Category == ItemCategory.Weapon && i.Stance == ItemStance.Bow),
        new GlowGroup("Staves and polearms", i => i.Category == ItemCategory.Weapon && i.Stance == ItemStance.Staff),
        new GlowGroup("Arrows", i => i.Category == ItemCategory.Weapon && i.Stance == ItemStance.Ammunition),
        new GlowGroup("Shields", i => i.Category == ItemCategory.Armour && i.Slot == ItemSlot.Shield),
        new GlowGroup("Body armour (glows in the lists only)", i => i.Category == ItemCategory.Armour && i.Slot == ItemSlot.Body),
        new GlowGroup("Helmets (in the lists only)", i => i.Category == ItemCategory.Armour && i.Slot == ItemSlot.Helmet),
        new GlowGroup("Gloves (in the lists only)", i => i.Category == ItemCategory.Armour && i.Slot == ItemSlot.Gloves),
        new GlowGroup("Boots (in the lists only)", i => i.Category == ItemCategory.Armour && i.Slot == ItemSlot.Boots),
        new GlowGroup("Rings", i => i.Category == ItemCategory.Other && i.Slot == ItemSlot.Ring),
        new GlowGroup("Amulets", i => i.Category == ItemCategory.Other && i.Slot == ItemSlot.Amulet),
        new GlowGroup("Metal items", HasBit(ItemBits.Metal)),
        new GlowGroup("Wooden items", HasBit(49)),
        new GlowGroup("Cloth", HasBit(45)),
        new GlowGroup("Leather", HasBit(46)),
        new GlowGroup("Mail", HasBit(47)),
        new GlowGroup("Plate", HasBit(48)),
        new GlowGroup("Items with their own name (Icingdeath, Twinkle …)", HasBit(ItemBits.UniqueName)),
        new GlowGroup("Added items", i => i.IsAdded)
    };

    private GlowGroup _selectedGlowGroup = GlowGroups[1];

    public GlowGroup SelectedGlowGroup
    {
        get => _selectedGlowGroup;
        set
        {
            if (SetProperty(ref _selectedGlowGroup, value)) OnPropertyChanged(nameof(GlowGroupMembers));
        }
    }

    private ItemGlow _groupGlow = ItemGlow.InStyle(1);

    /// <summary>The setting <see cref="ApplyGroupGlow"/> gives the group.</summary>
    public ItemGlow GroupGlow
    {
        get => _groupGlow;
        set => SetProperty(ref _groupGlow, value);
    }

    /// <summary>The items in the chosen group.</summary>
    public string GlowGroupMembers
    {
        get
        {
            var items = CurrentItems();
            var names = Items.Where((r, i) => i < items.Count && _selectedGlowGroup.Contains(items[i])).Select(r => r.DisplayName)
                .Distinct().ToList();
            return names.Count == 0 ? "No item is in this group." : $"{Count(names.Count)}: " + string.Join(", ", names) + ".";
        }
    }

    private string _groupGlowResult = "";

    public string GroupGlowResult
    {
        get => _groupGlowResult;
        private set => SetProperty(ref _groupGlowResult, value);
    }

    /// <summary>Gives every item in the chosen group the chosen setting; returns how many changed (0 when glow styles are off).</summary>
    public int ApplyGroupGlow()
    {
        if (!_glowOn) return 0;
        var items = CurrentItems();
        var changed = 0;
        var members = 0;
        var member = "";
        Quietly(() =>
        {
            for (var i = 0; i < Items.Count && i < items.Count; i++)
            {
                if (!_selectedGlowGroup.Contains(items[i])) continue;
                members++;
                member = Items[i].DisplayName;
                if (Items[i].Glow == _groupGlow) continue;
                Items[i].Glow = _groupGlow;
                changed++;
            }
        });

        var what = ItemGlowChoices.FirstOrDefault(c => c.Value == _groupGlow)?.Text ?? _groupGlow.ToString();
        GroupGlowResult = members == 0 ? "No item is in this group."
            : members == 1 ? (changed == 1 ? $"{member} changed to {what}." : $"{member} is already {what}.")
            : $"{changed} of {members} items changed to {what}.";
        return changed;
    }

    private static string Count(int items) => items == 1 ? "1 item" : $"{items} items";

    // ═══════════════════════════════ what the settings are now ═══════════════════════════════

    /// <summary>The styles and rules as typed; a style with a bad box counts as it was, a rule with nothing ticked as none.</summary>
    internal GlowSettings CurrentGlow() =>
        _currentGlow ??= new GlowSettings(GlowStyles.Select(s => s.ValueOrSource()),
                                          _glowOn ? GlowRules.Where(r => r.Bits != 0).Select(r => r.ToRule()) : Array.Empty<GlowRule>());

    /// <summary>A style, a rule or an item's glow changed: what shows the glow follows.</summary>
    internal void GlowEdited()
    {
        _currentGlow = null;
        if (Quiet) return;
        for (var i = 0; i < ItemGlowChoices.Count; i++)
        {
            var style = ItemGlowChoices[i].Value.Style;
            ItemGlowChoices[i].SetSwatches(style is { } s && s < GlowStyles.Count ? GlowStyles[s] : null);
        }

        for (var i = 0; i < RuleGlowChoices.Count; i++)
        {
            var s = RuleGlowChoices[i].Value;
            RuleGlowChoices[i].SetSwatches(s >= 0 && s < GlowStyles.Count ? GlowStyles[s] : null);
        }

        foreach (var row in GlowStyles) row.RefreshUse();
        foreach (var row in GlowRules) row.RefreshSwatches();
        foreach (var row in Items) row.RefreshGlow();
        OnPropertyChanged(nameof(GlowTryResult));
        OnPropertyChanged(nameof(GlowGroupMembers));
        OnPropertyChanged(nameof(GlowChanged));
        RefreshShops();
    }

    /// <summary>True when the glow differs from the executable's.</summary>
    public bool GlowChanged
    {
        get
        {
            if (!IsSupported) return false;
            if (_glowOn != (GlowState == GlowStylesState.On)) return true;
            if (GlowStyles.Any(s => s.TryBuild(out _) is not { } style || style != s.Source)) return true;
            return _glowOn && (GlowRules.Count != _loadedGlow.Rules.Count || GlowRules.Select(r => r.ToRule()).Where((r, i) => r != _loadedGlow.Rules[i]).Any());
        }
    }

    /// <summary>What uses a style: rules, items with it as their own, and (style 0) items no rule gives a style.</summary>
    internal string GlowStyleUsedBy(int style)
    {
        if (!_glowOn) return style == 0 ? "Every enchanted item (the game's own glow)." : "";
        var parts = new List<string>();
        var rules = GlowRules.Where(r => r.Style == style && r.Bits != 0).Select(r => r.Position.ToString(CultureInfo.InvariantCulture)).ToList();
        if (rules.Count > 0) parts.Add((rules.Count == 1 ? "rule " : "rules ") + string.Join(", ", rules));
        var items = Items.Where(r => r.Glow.Style == style).Select(r => r.DisplayName).Distinct().ToList();
        if (items.Count > 0) parts.Add(items.Count <= 4 ? string.Join(", ", items) : string.Join(", ", items.Take(4)) + $" and {items.Count - 4} more");
        if (style == 0) parts.Add("automatic items no rule matches");
        return parts.Count == 0 ? "(nothing)" : string.Join("; ", parts);
    }

    /// <summary>How an item with these bits and this setting glows, in a few words (shop lists, try box).</summary>
    internal string GlowWords(ItemGlow glow, ulong bits)
    {
        if (!_glowOn) return GlowFunction.GameDecide(bits) is { } g ? $"glows ({g.HaloAlpha}/{g.CoreAlpha})" : "no glow";
        return GlowFunction.Decide(CurrentGlow(), glow, bits) is { } r ? $"glow: style {r.Style}" : "no glow";
    }

    /// <summary>The Items tab's line about an item's glow: its setting, and how it glows as it is and with +2.</summary>
    internal string GlowPreview(GameItem item)
    {
        string Say(ulong bits)
        {
            var r = _glowOn ? GlowFunction.Decide(CurrentGlow(), item.Glow, bits) : GlowFunction.GameDecide(bits);
            if (r is not { } g) return "no glow";
            return (_glowOn ? $"style {g.Style}" : "the game's glow") + $" ({g.HaloAlpha}/{g.CoreAlpha})";
        }

        var head = _glowOn
            ? "Glow (" + (item.Glow.IsAutomatic ? "automatic" : item.Glow.IsNever ? "never" : $"its own style {item.Glow.Style}") + "): "
            : "Glow (the game's own): ";
        return head + $"as it is, {Say(item.Flags)}; with +2, {Say(item.Flags | 2)}.";
    }

    // ═══════════════════════════════ try item text ═══════════════════════════════

    private string _glowTryText = "longsword+2+ice";

    public string GlowTryText
    {
        get => _glowTryText;
        set
        {
            if (SetProperty(ref _glowTryText, value)) OnPropertyChanged(nameof(GlowTryResult));
        }
    }

    public string GlowTryResult => DescribeGlow(_glowTryText.Trim());

    /// <summary>How the game would make an item of this text glow, and why.</summary>
    internal string DescribeGlow(string text)
    {
        var items = CurrentItems();
        var parsed = ItemText.Parse(text, items);
        if (!parsed.IsValid) return parsed.Problems[0];
        var item = items[parsed.ItemIndex];
        var bits = parsed.Flags;
        var name = Items[parsed.ItemIndex].DisplayName;
        var extras = ItemBits.Describe(bits, ItemBits.Has(bits, ItemBits.Metal));
        var head = (extras.Length > 0 ? $"{name} ({extras})" : name) + $": power {ItemFormulas.Power(bits)}. ";
        if (!_glowOn)
        {
            if (GlowFunction.GameDecide(bits) is { } g)
                return head + $"Glows in the game's own colours, halo {g.HaloAlpha} and core {g.CoreAlpha} of 255.";
            return head + ((bits & GlowFunction.Flaming) != 0 && ItemFormulas.Power(bits) > 0
                ? "No glow: the game gives Flaming weapons flames instead."
                : "No glow: it has no +N and no modifier that adds power.");
        }

        var settings = CurrentGlow();
        var result = GlowFunction.Decide(settings, item.Glow, bits);
        string RuleText(int i) => $"rule {i + 1} ({GlowRuleRow.Describe(settings.Rules[i].Bits)})";
        if (result is { } r)
        {
            var why = !item.Glow.IsAutomatic ? $"its own style ({name} is set to style {r.Style})"
                : r.Rule is { } i ? RuleText(i)
                : "no rule matches, so the game's own rule, in style 0";
            return head + $"Glows in style {r.Style}: {why}. Halo {r.HaloAlpha} and core {r.CoreAlpha} of 255.";
        }

        var firstRule = settings.Rules.Select((x, i) => (x, i)).FirstOrDefault(t => (t.x.Bits & bits) != 0);
        var reason = item.Glow.IsNever ? $"{name} is set to Never"
            : (firstRule.x.Bits & bits) != 0 && firstRule.x.Style == null && item.Glow.IsAutomatic ? RuleText(firstRule.i) + " says no glow"
            : !item.Glow.IsAutomatic ? "it has no +N or modifier that adds power, and no rule matches"
            : (bits & GlowFunction.Flaming) != 0 && ItemFormulas.Power(bits) > 0 ? "no rule matches, and the game gives Flaming weapons flames instead"
            : "it has no +N or modifier that adds power, and no rule matches";
        return head + $"No glow: {reason}.";
    }

    // ═══════════════════════════════ saving ═══════════════════════════════

    /// <summary>The glow part of <see cref="BuildEdits"/>: throws <see cref="ArgumentException"/> naming the first bad box.</summary>
    private void BuildGlowEdits(ItemEdits edits)
    {
        var styles = new List<GlowStyle>();
        foreach (var row in GlowStyles)
        {
            var style = row.TryBuild(out var problem);
            if (style == null) throw new ArgumentException($"Glow style {row.Index}: {problem}");
            styles.Add(style.Value);
        }

        if (_glowOn)
        {
            foreach (var rule in GlowRules)
            {
                if (rule.Problem is { } p) throw new ArgumentException($"Glow rule {rule.Position}: {p}");
            }

            var settings = new GlowSettings(styles, GlowRules.Select(r => r.ToRule()));
            if (settings.Problem() is { } bad) throw new ArgumentException(bad);
            if (GlowState != GlowStylesState.On || !settings.Equals(_loadedGlow)) edits.GlowStyles = settings;
            return;
        }

        if (GlowState == GlowStylesState.On) edits.RemoveGlowStyles = true;
        var colours = new EnchantGlow(styles[0].Halo.Colour, styles[0].Core.Colour);
        if (colours != _loadedColours) edits.Glow = colours;
    }
}

// ═══════════════════════════════ a glow style ═══════════════════════════════

/// <summary>One glow style as boxes: halo and core colour and strength.</summary>
public sealed class GlowStyleRow : ObservableObject
{
    private readonly ItemEditorModel _owner;
    private string _hr = "", _hg = "", _hb = "", _hs = "", _cr = "", _cg = "", _cb = "", _cs = "";

    internal GlowStyleRow(ItemEditorModel owner, int index, GlowStyle source)
    {
        _owner = owner;
        Index = index;
        Source = source;
        Fill(source);
    }

    public int Index { get; }

    /// <summary>The style as it is in the executable.</summary>
    public GlowStyle Source { get; }

    public string Name => Index == 0 ? "0 (standard)" : Index.ToString(CultureInfo.InvariantCulture);

    /// <summary>Style 0 is the ENCHANT GLOW colours, always editable; the others need glow styles.</summary>
    public bool IsEditable => Index == 0 || _owner.GlowStylesOn;

    /// <summary>The strengths are the game's own 64 and 72 without glow styles.</summary>
    public bool StrengthEditable => _owner.GlowStylesOn;

    public string HaloR { get => _hr; set => SetBox(ref _hr, value); }
    public string HaloG { get => _hg; set => SetBox(ref _hg, value); }
    public string HaloB { get => _hb; set => SetBox(ref _hb, value); }
    public string HaloStrength { get => _hs; set => SetBox(ref _hs, value); }
    public string CoreR { get => _cr; set => SetBox(ref _cr, value); }
    public string CoreG { get => _cg; set => SetBox(ref _cg, value); }
    public string CoreB { get => _cb; set => SetBox(ref _cb, value); }
    public string CoreStrength { get => _cs; set => SetBox(ref _cs, value); }

    /// <summary>Picking one copies its colours and strengths into this style (it then shows nothing picked again).</summary>
    public GlowColourPreset? ColourPreset
    {
        get => null;
        set
        {
            if (value == null) return;
            Set(value.Style);
            OnPropertyChanged();
        }
    }

    public string HaloSwatch => Swatch(_hr, _hg, _hb);
    public string CoreSwatch => Swatch(_cr, _cg, _cb);

    private static string Swatch(string r, string g, string b) =>
        Channel(r) is { } R && Channel(g) is { } G && Channel(b) is { } B ? $"#{R:X2}{G:X2}{B:X2}" : "#00000000";

    private static int? Channel(string text) =>
        NumberText.TryParseInteger(text, out var v) && v is >= 0 and <= 255 ? v : null;

    public string UsedBy => _owner.GlowStyleUsedBy(Index);

    public bool IsChanged => TryBuild(out _) is not { } s || s != Source;

    public string? Problem
    {
        get
        {
            TryBuild(out var problem);
            return problem;
        }
    }

    internal GlowStyle? TryBuild(out string? problem)
    {
        problem = null;
        int Box(string text, string what, ref string? why)
        {
            if (Channel(text) is { } v) return v;
            why ??= $"{what}: 0 to 255.";
            return 0;
        }

        var hr = Box(_hr, "Halo red", ref problem);
        var hg = Box(_hg, "Halo green", ref problem);
        var hb = Box(_hb, "Halo blue", ref problem);
        var hs = Box(_hs, "Halo strength", ref problem);
        var cr = Box(_cr, "Core red", ref problem);
        var cg = Box(_cg, "Core green", ref problem);
        var cb = Box(_cb, "Core blue", ref problem);
        var cs = Box(_cs, "Core strength", ref problem);
        if (problem != null) return null;
        return new GlowStyle(new GlowLayer(new Rgb(hr, hg, hb), hs), new GlowLayer(new Rgb(cr, cg, cb), cs));
    }

    internal GlowStyle ValueOrSource() => TryBuild(out _) ?? Source;

    internal void Set(GlowStyle style)
    {
        Fill(style);
        foreach (var p in new[] { nameof(HaloR), nameof(HaloG), nameof(HaloB), nameof(HaloStrength), nameof(CoreR), nameof(CoreG), nameof(CoreB), nameof(CoreStrength) })
            OnPropertyChanged(p);
        Changed();
    }

    private void Fill(GlowStyle s)
    {
        static string T(int v) => v.ToString(CultureInfo.InvariantCulture);
        (_hr, _hg, _hb, _hs) = (T(s.Halo.Colour.R), T(s.Halo.Colour.G), T(s.Halo.Colour.B), T(s.Halo.Strength));
        (_cr, _cg, _cb, _cs) = (T(s.Core.Colour.R), T(s.Core.Colour.G), T(s.Core.Colour.B), T(s.Core.Strength));
    }

    private void SetBox(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (!SetProperty(ref field, value, name)) return;
        Changed();
    }

    private void Changed()
    {
        OnPropertyChanged(nameof(HaloSwatch));
        OnPropertyChanged(nameof(CoreSwatch));
        OnPropertyChanged(nameof(Problem));
        OnPropertyChanged(nameof(IsChanged));
        _owner.GlowEdited();
    }

    internal void Refresh()
    {
        OnPropertyChanged(nameof(IsEditable));
        OnPropertyChanged(nameof(StrengthEditable));
        RefreshUse();
    }

    internal void RefreshUse() => OnPropertyChanged(nameof(UsedBy));
}

// ═══════════════════════════════ a glow rule ═══════════════════════════════

/// <summary>One glow rule: the modifiers (and kinds) it looks for and the style it gives, or no glow.</summary>
public sealed class GlowRuleRow : ObservableObject
{
    /// <summary>The <see cref="Style"/> of a rule that says no glow.</summary>
    public const int NoGlow = -1;

    private readonly ItemEditorModel _owner;
    private ulong _bits;
    private int _style;

    internal GlowRuleRow(ItemEditorModel owner, GlowRule rule)
    {
        _owner = owner;
        _bits = rule.Bits;
        _style = rule.Style ?? NoGlow;
        var bits = RuleBits.Where(b => b.Kind is ItemBitKind.Modifier or ItemBitKind.Kind or ItemBitKind.Enchantment || ItemBits.Has(rule.Bits, b.Bit))
            .Select(b => new GlowBitRow(this, b)).ToList();
        ModifierBits = bits.Where(b => b.Bit.Kind is ItemBitKind.Modifier or ItemBitKind.Enchantment).ToList();
        KindBits = bits.Where(b => b.Bit.Kind is not (ItemBitKind.Modifier or ItemBitKind.Enchantment)).ToList();
    }

    /// <summary>Bit 3 of the +N, set on +8 to +15: a rule that looks for it picks the strongest items.</summary>
    internal static readonly ItemBit HighEnchantment = new(3, ItemBitKind.Enchantment, "+8 or more", null, null,
        "Bit 3 of the +N: set on +8 to +15, so a rule that looks for it picks the strongest items.");

    /// <summary>What a rule can look for: +8 or more, the modifiers and the kinds (not the +N's other bits or the looks).</summary>
    private static IEnumerable<ItemBit> RuleBits =>
        new[] { HighEnchantment }.Concat(ItemBits.All.Where(b => b.Bit >= 4 && b.Bit != ItemBits.LooksShift));

    /// <summary>1, 2, 3 …: earlier rules win.</summary>
    public int Position => _owner.GlowRules.IndexOf(this) + 1;

    public ulong Bits => _bits;

    /// <summary>The style it gives (0-15), or <see cref="NoGlow"/>.</summary>
    public int Style
    {
        get => _style;
        set
        {
            if (!SetProperty(ref _style, value)) return;
            RefreshSwatches();
            _owner.GlowEdited();
        }
    }

    public IReadOnlyList<GlowStyleChoice> StyleChoices => _owner.RuleGlowChoices;
    public IReadOnlyList<GlowBitRow> ModifierBits { get; }
    public IReadOnlyList<GlowBitRow> KindBits { get; }

    /// <summary>The ticked modifiers and kinds, in words.</summary>
    public string Summary => _bits == 0 ? "(nothing ticked yet)" : Describe(_bits);

    public string HaloSwatch => _style >= 0 && _style < _owner.GlowStyles.Count ? _owner.GlowStyles[_style].HaloSwatch : "#00000000";
    public string CoreSwatch => _style >= 0 && _style < _owner.GlowStyles.Count ? _owner.GlowStyles[_style].CoreSwatch : "#00000000";

    public string? Problem => _bits == 0 ? "Tick at least one modifier or kind it looks for, or remove the rule." : null;

    /// <summary>A warning when the rule looks for a kind: plain items of that kind then glow too.</summary>
    public string? Warning
    {
        get
        {
            if (_style == NoGlow) return null;
            var kinds = KindBits.Where(b => b.IsSet).Select(b => b.Name).ToList();
            return kinds.Count == 0
                ? null
                : $"A rule makes every item it matches glow, plain ones too: every {string.Join(" or ", kinds).ToLowerInvariant()} item would glow. " +
                  "To give a kind of item its own colours only when enchanted, set those items' glow (group below) instead.";
        }
    }

    internal static string Describe(ulong bits) =>
        string.Join(", ", RuleBits.Where(b => ItemBits.Has(bits, b.Bit)).Select(b => b.Name));

    internal GlowRule ToRule() => new(_bits, _style == NoGlow ? null : _style);

    internal bool HasBit(int bit) => ItemBits.Has(_bits, bit);

    internal void SetBit(int bit, bool on)
    {
        var bits = on ? _bits | 1UL << bit : _bits & ~(1UL << bit);
        if (bits == _bits) return;
        _bits = bits;
        OnPropertyChanged(nameof(Bits));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(Problem));
        OnPropertyChanged(nameof(Warning));
        foreach (var b in ModifierBits.Concat(KindBits)) b.Refresh();
        _owner.GlowEdited();
    }

    internal void RefreshPosition() => OnPropertyChanged(nameof(Position));

    internal void RefreshSwatches()
    {
        OnPropertyChanged(nameof(HaloSwatch));
        OnPropertyChanged(nameof(CoreSwatch));
        OnPropertyChanged(nameof(Warning));
    }
}

/// <summary>A property bit a glow rule looks for, as a tick box.</summary>
public sealed class GlowBitRow : ObservableObject
{
    private readonly GlowRuleRow _rule;

    internal GlowBitRow(GlowRuleRow rule, ItemBit bit)
    {
        _rule = rule;
        Bit = bit;
    }

    public ItemBit Bit { get; }
    public string Name => Bit.Name;
    public string Effect => $"Bit {Bit.Bit}" + (Bit.Keyword != null ? $", item text \"+{Bit.Keyword}\"" : "") + ". " + Bit.Effect;

    public bool IsSet
    {
        get => _rule.HasBit(Bit.Bit);
        set => _rule.SetBit(Bit.Bit, value);
    }

    internal void Refresh() => OnPropertyChanged(nameof(IsSet));
}

/// <summary>An item glow setting a list offers, with the colours of its style.</summary>
public sealed class GlowChoice : ObservableObject
{
    internal GlowChoice(ItemGlow value, string text)
    {
        Value = value;
        Text = text;
    }

    public ItemGlow Value { get; }
    public string Text { get; }
    public string HaloSwatch { get; private set; } = "#00000000";
    public string CoreSwatch { get; private set; } = "#00000000";

    internal void SetSwatches(GlowStyleRow? style)
    {
        HaloSwatch = style?.HaloSwatch ?? "#00000000";
        CoreSwatch = style?.CoreSwatch ?? "#00000000";
        OnPropertyChanged(nameof(HaloSwatch));
        OnPropertyChanged(nameof(CoreSwatch));
    }

    public override string ToString() => Text;
}

/// <summary>A style a glow rule can give (or no glow), with its colours.</summary>
public sealed class GlowStyleChoice : ObservableObject
{
    internal GlowStyleChoice(int value, string text)
    {
        Value = value;
        Text = text;
    }

    public int Value { get; }
    public string Text { get; }
    public string HaloSwatch { get; private set; } = "#00000000";
    public string CoreSwatch { get; private set; } = "#00000000";

    internal void SetSwatches(GlowStyleRow? style)
    {
        HaloSwatch = style?.HaloSwatch ?? "#00000000";
        CoreSwatch = style?.CoreSwatch ?? "#00000000";
        OnPropertyChanged(nameof(HaloSwatch));
        OnPropertyChanged(nameof(CoreSwatch));
    }

    public override string ToString() => Text;
}
