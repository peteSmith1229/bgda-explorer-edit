// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/WorldExplorer/ItemEditorModel.cs
//
// The Items tab of Executable Tuning (ItemEditorView.xaml): weapons, armour and other items,
// their models, what the shops sell, materials, stance weights, the enchant glow and the
// random-loot modifiers, with the names the game shows from GTEXT.LMP. Every box is text so a
// half-typed value never throws; Problem says what is wrong, and BuildEdits gathers the changes
// for BgdaExecutable.ApplyItemEdits (all or nothing). No WPF types, so it runs in tests.
// On a three-player executable items can be added after the game's 81 (AddItem copies the
// selected one) and removed again; the added items are written all together.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using WorldExplorer.Infrastructure;

namespace WorldExplorer;

/// <summary>A value and the text a list shows for it.</summary>
public sealed record Choice<T>(T Value, string Text)
{
    public override string ToString() => Text;
}

public sealed class ItemEditorModel : ObservableObject
{
    private readonly BgdaExecutable _exe;
    private readonly List<ShopRow> _allShops = new();
    private readonly Dictionary<LootModifierList, List<LootRow>> _loot = new();
    private List<GameItemModel> _models = new();
    private List<string> _nearbyShopTexts = new();

    public ItemEditorModel(BgdaExecutable exe)
    {
        _exe = exe;
        ItemsProblem = exe.ItemsProblem();
        ShopsProblem = ItemsProblem ?? exe.ShopsProblem();
        if (ItemsProblem != null) return;
        Load();
    }

    /// <summary>Why the item tables can't be edited (another build or patch), or null.</summary>
    public string? ItemsProblem { get; }

    public bool IsSupported => ItemsProblem == null;

    /// <summary>Why the shops can't be edited, or null.</summary>
    public string? ShopsProblem { get; }

    public bool ShopsSupported => ShopsProblem == null;

    public ObservableCollection<ItemRow> Items { get; } = new();
    public ObservableCollection<ItemModelRow> Models { get; } = new();
    public ObservableCollection<TuningRow> Materials { get; } = new();
    public ObservableCollection<TuningRow> StanceWeights { get; } = new();
    public ObservableCollection<GlowRow> Glow { get; } = new();

    private ItemRow? _selectedItem;

    /// <summary>The item the details panel shows.</summary>
    public ItemRow? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value)) OnPropertyChanged(nameof(CanRemoveSelected));
        }
    }

    private void Load()
    {
        Forget();
        Items.Clear();
        foreach (var item in _exe.GetItems()) Items.Add(new ItemRow(this, item));
        _models = _exe.GetItemModels();
        Models.Clear();
        foreach (var m in _models) Models.Add(new ItemModelRow(this, m));

        Materials.Clear();
        var materials = _exe.GetMaterialMultipliers();
        for (var i = 0; i < materials.Length; i++)
        {
            Materials.Add(new TuningRow(this, ItemFormulas.Materials[i].Name, materials[i], ItemFormulas.StockMaterialMultipliers[i], true));
        }

        StanceWeights.Clear();
        var weights = _exe.GetStanceWeights();
        for (var i = 0; i < weights.Length; i++)
        {
            StanceWeights.Add(new TuningRow(this, StanceText((ItemStance)i), weights[i], ItemFormulas.StockStanceWeights[i], false));
        }

        var glow = _exe.GetEnchantGlow();
        Glow.Clear();
        Glow.Add(new GlowRow("Halo (large and faint)", glow.Halo, new Rgb(10, 10, 68)));
        Glow.Add(new GlowRow("Core (small and bright)", glow.Core, new Rgb(60, 60, 68)));

        _loot.Clear();
        foreach (var list in Enum.GetValues<LootModifierList>())
        {
            _loot[list] = _exe.GetLootModifiers(list).Select((v, i) => new LootRow(i + 1, v)).ToList();
        }

        OnPropertyChanged(nameof(LootRows));

        _allShops.Clear();
        if (ShopsSupported)
        {
            _nearbyShopTexts = _exe.NearbyShopTexts(_exe.GetItems());
            foreach (var e in _exe.GetShopEntries()) _allShops.Add(new ShopRow(this, e));
        }

        OnPropertyChanged(nameof(ShopRows));
        SelectedItem = Items.FirstOrDefault(i => i.Index == SelectedItem?.Index) ?? Items.FirstOrDefault();
    }

    // ═══════════════════════════════ names (GTEXT.LMP) ═══════════════════════════════

    /// <summary>GTEXT.LMP, or null when it isn't loaded.</summary>
    public GameText? Names { get; private set; }

    public string? NamesPath { get; private set; }

    public bool NamesLoaded => Names != null;

    public string NamesStatus => Names == null
        ? "GTEXT.LMP isn't loaded, so the names the game shows can't be seen or changed. Open it from the game's data folder."
        : $"Names the game shows come from {NamesPath}.";

    /// <summary>Shows the names of <paramref name="names"/>; typed names that weren't saved are replaced.</summary>
    public void LoadNames(GameText names, string path)
    {
        Names = names;
        NamesPath = path;
        foreach (var row in Items) row.ResetShownName();
        OnPropertyChanged(nameof(Names));
        OnPropertyChanged(nameof(NamesLoaded));
        OnPropertyChanged(nameof(NamesStatus));
        RefreshShops();
    }

    /// <summary>The name the game shows for an item name, from GTEXT.LMP (null when not loaded or missing).</summary>
    internal string? GameName(string itemName) => Names?.GetItemName(itemName);

    /// <summary>True when a shown name differs from GTEXT.LMP's (<see cref="ApplyNames"/> hasn't put it there yet).</summary>
    public bool NamesChanged => Names != null && Items.Any(r => r.ShownNameChanged);

    /// <summary>True when GTEXT.LMP has to be saved: names were typed, or put in it and not saved yet.</summary>
    public bool NamesNeedSaving => Names != null && (Names.Changed || NamesChanged);

    /// <summary>Writes the changed names into <see cref="Names"/> (throws <see cref="ArgumentException"/> on a bad one).</summary>
    public void ApplyNames()
    {
        if (Names == null) return;
        foreach (var row in Items.Where(r => r.ShownNameChanged))
        {
            if (GameText.CheckItemName(row.ShownName) is { } problem) throw new ArgumentException($"{row.Name}: {problem}");
        }

        foreach (var row in Items.Where(r => r.ShownNameChanged)) Names.SetItemName(row.Name.Trim(), row.ShownName);
    }

    /// <summary>
    /// Called by a row whose shown name changed: the records that share its name (the two scimitars, the three
    /// rings and the three amulets) share its text in GTEXT.LMP, so they follow it.
    /// </summary>
    internal void ShownNameEdited(ItemRow edited)
    {
        if (edited.Name == edited.Source.Name)
        {
            foreach (var row in Items)
            {
                if (!ReferenceEquals(row, edited) && row.Source.Name == edited.Source.Name && row.Name == row.Source.Name)
                    row.SetShownNameQuietly(edited.ShownName);
            }
        }

        NameEdited();
    }

    /// <summary>Called when an item name or a shown name changed.</summary>
    internal void NameEdited()
    {
        Forget();
        OnPropertyChanged(nameof(NamesChanged));
        RefreshShops();
        foreach (var m in Models) m.Refresh();
        RefreshRoom();
    }

    // ═══════════════════════════════ added items ═══════════════════════════════

    private string? _addProblem;
    private bool _addProblemKnown;

    /// <summary>Why items can't be added to this executable (the stock one has no room), or null.</summary>
    public string? AddProblem
    {
        get
        {
            if (!_addProblemKnown) (_addProblem, _addProblemKnown) = (IsSupported ? _exe.ItemRoomProblem() : ItemsProblem, true);
            return _addProblem;
        }
    }

    /// <summary>The items after the game's 81, as listed now.</summary>
    public int AddedCount => Items.Count(r => r.IsAdded);

    /// <summary>The bytes of the room after the item table the listed added items leave (negative: they don't fit).</summary>
    public int RoomLeft => BgdaExecutable.AddedItemsRoom - BgdaExecutable.AddedItemsSize(Items.Where(r => r.IsAdded).Select(r => r.Name.Trim()));

    /// <summary>What an item with a name of up to 15 letters takes: its record and 16 bytes of name.</summary>
    private const int TypicalItemSize = BgdaExecutable.ItemRecordSize + 16;

    public bool CanAddItems => AddProblem == null && RoomLeft >= TypicalItemSize;

    public bool CanRemoveSelected => SelectedItem?.IsAdded == true;

    /// <summary>How many items there are and how many more fit, or why none can be added.</summary>
    public string CapacityText
    {
        get
        {
            if (AddProblem is { } problem) return problem;
            var added = AddedCount;
            var first = BgdaExecutable.StockItemCount;
            var head = added switch
            {
                0 => $"The game's {first} items and none added.",
                1 => $"The game's {first} items and 1 added (number {first}).",
                _ => $"The game's {first} items and {added} added (numbers {first}–{first + added - 1})."
            };
            var left = RoomLeft;
            if (left < 0)
                return head + $" They take {-left:N0} bytes more than the {BgdaExecutable.AddedItemsRoom:N0} after the table: remove some or shorten their names.";
            if (left < TypicalItemSize)
                return head + $" No room for more: {left:N0} bytes left, and an item takes {BgdaExecutable.ItemRecordSize} and its name.";
            return head + $" Room for about {left / TypicalItemSize} more ({left:N0} bytes left; an item takes {BgdaExecutable.ItemRecordSize} and its name).";
        }
    }

    private int _shownAddedCount = -1;

    private void RefreshRoom()
    {
        if (AddedCount != _shownAddedCount)
        {
            _shownAddedCount = AddedCount;
            OnPropertyChanged(nameof(AddedCount));
        }

        OnPropertyChanged(nameof(RoomLeft));
        OnPropertyChanged(nameof(CanAddItems));
        OnPropertyChanged(nameof(CapacityText));
        OnPropertyChanged(nameof(CanRemoveSelected));
    }

    /// <summary>Called after the executable changed under the tab (three players turned on or off by Save As).</summary>
    public void RoomChanged()
    {
        _addProblemKnown = false;
        OnPropertyChanged(nameof(AddProblem));
        RefreshRoom();
    }

    /// <summary>
    /// Adds a copy of the selected item after the last one, under a name no item has ("longsword2"), shown as
    /// the original's name and the same number; selects it and returns it (null when no item can be added).
    /// </summary>
    public ItemRow? AddItem()
    {
        if (!CanAddItems || Items.Count == 0) return null;
        var from = SelectedItem ?? Items[^1];
        var copy = (from.TryBuild(out _) ?? from.Source).AsAdded(Items.Count);
        var (name, number) = UniqueName(from.Name.Trim());
        copy.Name = name;
        var row = new ItemRow(this, copy, isNew: true);
        if (NamesLoaded) row.SetAutoShownName(NumberedName(from.DisplayName, number));
        Items.Add(row);
        SelectedItem = row;
        NameEdited();
        return row;
    }

    /// <summary>Removes an added item; the ones after it move down a place (a save that holds them gets other items).</summary>
    public void RemoveItem(ItemRow row)
    {
        var at = Items.IndexOf(row);
        if (at < 0 || !row.IsAdded) return;
        Items.RemoveAt(at);
        for (var i = at; i < Items.Count; i++) Items[i].Renumber(i);
        SelectedItem = Items[Math.Min(at, Items.Count - 1)];
        NameEdited();
    }

    /// <summary>"longsword" gives "longsword2" (or 3, 4 ... until no item has it), within 31 characters.</summary>
    private (string Name, int Number) UniqueName(string from)
    {
        var stem = from.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        if (stem.Length == 0) stem = "item";
        var used = new HashSet<string>(Items.Select(r => r.Name.Trim()), StringComparer.Ordinal);
        for (var n = 2; ; n++)
        {
            var suffix = n.ToString(CultureInfo.InvariantCulture);
            var name = (stem.Length + suffix.Length > BgdaExecutable.MaxAddedItemName
                ? stem[..(BgdaExecutable.MaxAddedItemName - suffix.Length)]
                : stem) + suffix;
            if (!used.Contains(name)) return (name, n);
        }
    }

    /// <summary>"Long Sword" and 2 give "Long Sword 2", shortened to fit the game's 31 characters.</summary>
    private static string NumberedName(string shown, int number)
    {
        var suffix = " " + number.ToString(CultureInfo.InvariantCulture);
        var stem = shown.Trim();
        if (stem.Length + suffix.Length > GameText.MaxItemNameLength) stem = stem[..(GameText.MaxItemNameLength - suffix.Length)].TrimEnd();
        return stem + suffix;
    }

    // ═══════════════════════════════ shared state for the rows ═══════════════════════════════

    /// <summary>The material multipliers as typed (stock values where a box isn't a number).</summary>
    internal float[] CurrentMaterials() => Materials.Select(m => m.ValueOrStock()).ToArray();

    internal float[] CurrentStanceWeights() => StanceWeights.Select(m => m.ValueOrStock()).ToArray();

    private List<GameItem>? _currentItems, _currentNames;

    /// <summary>The items as they would be saved; a row with a problem counts as it was.</summary>
    internal List<GameItem> CurrentItems() => _currentItems ??= Items.Select(r => r.TryBuild(out _) ?? r.Source).ToList();

    /// <summary>Each row's number and typed name: what a renamed record is checked against.</summary>
    internal List<GameItem> CurrentNames() =>
        _currentNames ??= Items.Select(r => new GameItem { Index = r.Index, Name = r.Name.Trim() }).ToList();

    private void Forget()
    {
        _currentItems = null;
        _currentNames = null;
    }

    internal GameItemModel? ModelAt(int index) => index >= 0 && index < _models.Count ? _models[index] : null;

    /// <summary>The names of the items that show <paramref name="model"/> (held, as one of their looks, or on the ground).</summary>
    internal IEnumerable<string> NamesUsingModel(int model)
    {
        var items = CurrentItems();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var held = !item.WornFromArmorYak && model >= item.Model && model < item.Model + ItemBits.Looks(item.Flags);
            if (held || item.PickupModel == model) yield return Items[i].DisplayName;
        }
    }

    internal string? CheckModel(int index, string vif, string tex) => _exe.CheckItemModel(index, vif, tex);

    internal IReadOnlyList<string> NearbyShopTexts => _nearbyShopTexts;

    /// <summary>An item row changed: the shops and the models' "used by" follow.</summary>
    internal void ItemEdited()
    {
        Forget();
        RefreshShops();
        foreach (var m in Models) m.Refresh();
    }

    /// <summary>A material or stance weight changed: values and damage follow.</summary>
    internal void TuningEdited()
    {
        Forget();
        foreach (var row in Items) row.RefreshDerived();
        RefreshShops();
    }

    private void RefreshShops()
    {
        foreach (var s in _allShops) s.Refresh();
        OnPropertyChanged(nameof(TryResult));
    }

    // ═══════════════════════════════ shops ═══════════════════════════════

    public static IReadOnlyList<Choice<ShopStock>> ShopStocks { get; } = new[]
    {
        new Choice<ShopStock>(ShopStock.Bartley, "Bartley (tavern)"),
        new Choice<ShopStock>(ShopStock.Branoch, "Branoch (mine camp)"),
        new Choice<ShopStock>(ShopStock.Sleyvas, "Sleyvas (every other level)"),
        new Choice<ShopStock>(ShopStock.Extreme, "Extreme difficulty (every shop; the rest is random)")
    };

    private ShopStock _selectedShopStock = ShopStock.Bartley;

    public ShopStock SelectedShopStock
    {
        get => _selectedShopStock;
        set
        {
            if (SetProperty(ref _selectedShopStock, value)) OnPropertyChanged(nameof(ShopRows));
        }
    }

    /// <summary>The entries of the shop chosen in <see cref="SelectedShopStock"/>, in the order the game adds them.</summary>
    public List<ShopRow> ShopRows => _allShops.Where(s => s.Stock == SelectedShopStock).ToList();

    public IReadOnlyList<ShopRow> AllShopRows => _allShops;

    private string _tryText = "longsword+masterwork+3+fire";

    /// <summary>Item text to try: what the game would make of it.</summary>
    public string TryText
    {
        get => _tryText;
        set
        {
            if (SetProperty(ref _tryText, value)) OnPropertyChanged(nameof(TryResult));
        }
    }

    public string TryResult => DescribeItemText(TryText.Trim(), out _);

    /// <summary>What the game makes of item text, in English, or why it's wrong.</summary>
    internal string DescribeItemText(string text, out ParsedItem parsed)
    {
        var items = CurrentItems();
        parsed = ItemText.Parse(text, items);
        if (!parsed.IsValid) return parsed.Problems[0];
        var item = items[parsed.ItemIndex];
        var row = Items[parsed.ItemIndex];
        var name = row.DisplayName;
        var metal = ItemBits.Has(parsed.Flags, ItemBits.Metal);
        var extras = ItemBits.Describe(parsed.Flags, metal);
        var parts = new List<string> { extras.Length > 0 ? $"{name} ({extras})" : name };
        if (item.Category == ItemCategory.Weapon && item.Stance != ItemStance.Ammunition)
        {
            var (min, max) = ItemFormulas.Damage(item, parsed.Flags, CurrentMaterials());
            parts.Add($"damage {min}–{max}");
        }
        else if (item.Category == ItemCategory.Armour)
        {
            parts.Add($"armour {ItemFormulas.Armour(item, parsed.Flags)}");
        }

        var value = ItemFormulas.ShopValue(item, parsed.Count, parsed.Flags, CurrentStanceWeights(), CurrentMaterials());
        parts.Add(parsed.Count > 1 ? $"price {value:N0} for {parsed.Count}" : $"price {value:N0}");
        return string.Join(" · ", parts);
    }

    // ═══════════════════════════════ loot lists ═══════════════════════════════

    public static IReadOnlyList<Choice<LootModifierList>> LootLists { get; } = new[]
    {
        new Choice<LootModifierList>(LootModifierList.Weapons, "Weapons"),
        new Choice<LootModifierList>(LootModifierList.WeaponsExtreme, "Weapons on Extreme"),
        new Choice<LootModifierList>(LootModifierList.Armour, "Armour"),
        new Choice<LootModifierList>(LootModifierList.ArmourExtreme, "Armour on Extreme"),
        new Choice<LootModifierList>(LootModifierList.RingsAndAmulets, "Rings and amulets")
    };

    /// <summary>What a loot entry can be: +1 to +15, or one property bit.</summary>
    public static IReadOnlyList<Choice<ulong>> LootChoices { get; } = MakeLootChoices();

    private static List<Choice<ulong>> MakeLootChoices()
    {
        var list = Enumerable.Range(1, 15).Select(n => new Choice<ulong>((ulong)n, $"+{n}")).ToList();
        list.AddRange(ItemBits.Modifiers.Select(b => new Choice<ulong>(1UL << b.Bit, b.Name)));
        for (var bit = 4; bit < 64; bit++)
        {
            if (list.Any(c => c.Value == 1UL << bit)) continue;
            list.Add(new Choice<ulong>(1UL << bit, $"Bit {bit}: {ItemBits.Get(bit).Name}"));
        }

        return list;
    }

    private LootModifierList _selectedLootList = LootModifierList.Weapons;

    public LootModifierList SelectedLootList
    {
        get => _selectedLootList;
        set
        {
            if (SetProperty(ref _selectedLootList, value)) OnPropertyChanged(nameof(LootRows));
        }
    }

    public List<LootRow> LootRows => _loot.TryGetValue(SelectedLootList, out var rows) ? rows : new List<LootRow>();

    // ═══════════════════════════════ saving ═══════════════════════════════

    /// <summary>
    /// The changes to write, checked: throws <see cref="ArgumentException"/> naming the first box that is
    /// wrong. Empty when nothing changed.
    /// </summary>
    public ItemEdits BuildEdits()
    {
        var edits = new ItemEdits();
        if (!IsSupported) return edits;

        // the game's items are written one by one; the added ones all together when any of them changed
        var added = new List<GameItem>();
        var addedChanged = false;
        foreach (var row in Items)
        {
            var item = row.TryBuild(out _);
            if (item == null || row.Problem is { }) throw new ArgumentException($"Item {row.Index} ({row.Source.Name}): {row.Problem}");
            if (row.IsAdded)
            {
                added.Add(item);
                addedChanged |= row.IsNew || row.Index != row.Source.Index || row.IsChanged(item);
            }
            else if (row.IsChanged(item))
            {
                edits.Items.Add(item);
            }
        }

        if (addedChanged || added.Count != _exe.AddedItemCount)
        {
            if (added.Count > 0 && AddProblem is { } problem) throw new InvalidOperationException(problem);
            if (RoomLeft < 0)
                throw new ArgumentException($"The added items take {-RoomLeft:N0} bytes more than the {BgdaExecutable.AddedItemsRoom:N0} " +
                                            "after the item table: remove some or shorten their names.");
            edits.AddedItems = added;
        }

        foreach (var row in Models)
        {
            if (!row.IsChanged) continue;
            if (row.Problem is { } problem) throw new ArgumentException($"Model {row.Index}: {problem}");
            edits.Models[row.Index] = (row.Vif.Trim(), row.Tex.Trim());
        }

        foreach (var row in _allShops)
        {
            if (!row.IsChanged)
            {
                // an entry left as it was can lose its item when items are renamed, removed or changed
                if (row.LostItem is { } lost) throw new ArgumentException($"{BgdaExecutable.ShopStockName(row.Stock)}, entry {row.Position}: {lost}");
                continue;
            }

            if (row.Problem is { } problem) throw new ArgumentException($"{BgdaExecutable.ShopStockName(row.Stock)}, entry {row.Position}: {problem}");
            edits.ShopTexts[row.Number] = row.Text.Trim();
        }

        if (Materials.Any(m => m.IsChanged)) edits.MaterialMultipliers = Materials.Select(m => m.Parse()).ToArray();
        if (StanceWeights.Any(m => m.IsChanged)) edits.StanceWeights = StanceWeights.Select(m => m.Parse()).ToArray();
        if (Glow.Any(g => g.IsChanged)) edits.Glow = new EnchantGlow(Glow[0].Parse(), Glow[1].Parse());
        foreach (var (list, rows) in _loot)
        {
            if (rows.Any(r => r.IsChanged)) edits.Loot[list] = rows.Select(r => r.Value).ToArray();
        }

        return edits;
    }

    /// <summary>Applies <see cref="BuildEdits"/> to the executable (all or nothing) and reads the tables back.</summary>
    public void Apply()
    {
        var edits = BuildEdits();
        if (edits.IsEmpty) return;
        _exe.ApplyItemEdits(edits);
        var names = Items.ToDictionary(r => r.Index, r => r.ShownName);
        Load();
        foreach (var row in Items) row.SetShownNameQuietly(names.TryGetValue(row.Index, out var name) ? name : row.ShownName);
        OnPropertyChanged(nameof(NamesChanged));
        RoomChanged();
    }

    /// <summary>Called after GTEXT.LMP was saved: what was written is the new starting point.</summary>
    public void NamesSaved(GameText saved, string path) => LoadNames(saved, path);

    // ═══════════════════════════════ EQUIPMNT.LMP ═══════════════════════════════

    private HashSet<string>? _equipment;

    public string? EquipmentPath { get; private set; }

    public string EquipmentStatus => EquipmentPath == null
        ? "EQUIPMNT.LMP isn't loaded, so new file names can't be checked against it. The game needs every file a model names in EQUIPMNT.LMP."
        : $"File names are checked against {EquipmentPath}.";

    /// <summary>The entries of EQUIPMNT.LMP, the archive the models' files are loaded from.</summary>
    public void LoadEquipment(IEnumerable<string> entryNames, string path)
    {
        _equipment = new HashSet<string>(entryNames, StringComparer.Ordinal);
        EquipmentPath = path;
        OnPropertyChanged(nameof(EquipmentPath));
        OnPropertyChanged(nameof(EquipmentStatus));
        foreach (var m in Models) m.Refresh();
    }

    /// <summary>Why a model can't load <paramref name="file"/> from EQUIPMNT.LMP, or null (also when it isn't loaded).</summary>
    internal string? MissingEquipmentFile(string file) =>
        _equipment == null || _equipment.Contains(file)
            ? null
            : $"EQUIPMNT.LMP has no {file}: add it there first (main window, Add Entry…), or the game can't load the model.";

    /// <summary>
    /// Where a game file is: <paramref name="fileName"/> in one of <paramref name="folders"/> or in their
    /// BG\DATA or DATA folder (the executable's folder, the data folder of the settings ...), or null.
    /// </summary>
    public static string? FindGameFile(string fileName, IEnumerable<string?> folders)
    {
        foreach (var folder in folders)
        {
            if (string.IsNullOrWhiteSpace(folder) || !System.IO.Directory.Exists(folder)) continue;
            foreach (var sub in new[] { "", "DATA", System.IO.Path.Combine("BG", "DATA") })
            {
                foreach (var name in new[] { fileName, fileName.ToLowerInvariant() })
                {
                    var path = System.IO.Path.Combine(folder, sub, name);
                    if (System.IO.File.Exists(path)) return path;
                }
            }
        }

        return null;
    }

    // ═══════════════════════════════ texts ═══════════════════════════════

    internal static string StanceText(ItemStance stance) => stance switch
    {
        ItemStance.OneHanded => "One-handed",
        ItemStance.TwoHandedSword => "Two-handed sword",
        ItemStance.TwoHandedAxe => "Two-handed axe or hammer",
        ItemStance.Staff => "Staff or polearm",
        ItemStance.Bow => "Bow",
        ItemStance.Ammunition => "Ammunition",
        _ => stance.ToString()
    };

    internal static string SlotText(ItemSlot slot) => slot switch
    {
        ItemSlot.OneHanded => "One hand",
        ItemSlot.TwoHanded => "Two hands",
        ItemSlot.Usable => "Usable (potions, gems ...)",
        _ => slot.ToString()
    };

    internal static string Format(float value) => NumberText.Format(value, 3);
}

// ═══════════════════════════════ an item ═══════════════════════════════

/// <summary>One record of the item table, as boxes.</summary>
public sealed class ItemRow : ObservableObject
{
    private readonly ItemEditorModel _owner;
    private ulong _otherFlags;
    private string _name, _shownName = "", _weight, _stack, _damageMin, _damageMax, _price, _armour, _model, _pickup,
        _enchantment, _looks;
    private ItemCategory _category;
    private ItemSlot _slot;
    private ItemStance _stance;
    private int _damageType;
    private int _index;
    private string? _autoShownName, _newShownName;

    internal ItemRow(ItemEditorModel owner, GameItem source, bool isNew = false)
    {
        _owner = owner;
        Source = source;
        IsNew = isNew;
        _index = source.Index;
        _name = source.Name;
        _weight = ItemEditorModel.Format(source.Weight);
        _stack = Text(source.StackSize);
        _damageMin = Text(source.DamageMin);
        _damageMax = Text(source.DamageMax);
        _price = Text(source.Price);
        _armour = Text(source.Armour);
        _model = Text(source.Model);
        _pickup = Text(source.PickupModel);
        _enchantment = Text(ItemBits.Enchantment(source.Flags));
        _looks = Text(ItemBits.Looks(source.Flags));
        _category = source.Category;
        _slot = source.Slot;
        _stance = source.Stance;
        _damageType = source.DamageType;
        _otherFlags = source.Flags & ~(ItemBits.EnchantmentMask | ItemBits.LooksMask);
        _shownName = owner.GameName(source.Name) ?? "";
        Bits = ItemBits.All
            .Where(b => b.Kind is ItemBitKind.Modifier or ItemBitKind.Kind ||
                        b.Kind == ItemBitKind.Unknown && ItemBits.Has(source.Flags, b.Bit))
            .Select(b => new ItemBitRow(this, b))
            .ToList();
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>The record as it is in the executable (for an item added here: as it was copied).</summary>
    public GameItem Source { get; }

    /// <summary>Its place in the table: an added item moves down when one before it is removed.</summary>
    public int Index => _index;

    public bool IsSpare => Source.IsSpare;

    /// <summary>An item after the game's 81.</summary>
    public bool IsAdded => Source.IsAdded;

    /// <summary>Added on this tab and not saved yet.</summary>
    public bool IsNew { get; }

    public bool CanRename => Source.IsSpare && Source.NameCapacity > 0 || Source.IsAdded;
    public bool CanChangeCategory => Source.IsSpare || Source.IsAdded;
    public bool CanChangeSlot => Source.IsSpare || Source.IsAdded || Category == ItemCategory.Weapon;
    public bool HasNameHint => Source.IsSpare || Source.IsAdded;

    internal void Renumber(int index)
    {
        if (_index == index) return;
        _index = index;
        OnPropertyChanged(nameof(Index));
        OnPropertyChanged(nameof(NameHint));
        RefreshDerived();
    }
    public bool IsWeapon => Category == ItemCategory.Weapon;
    public bool NamesLoaded => _owner.NamesLoaded;

    public IReadOnlyList<ItemBitRow> Bits { get; }
    public IEnumerable<ItemBitRow> ModifierBits => Bits.Where(b => b.Bit.Kind == ItemBitKind.Modifier);
    public IEnumerable<ItemBitRow> KindBits => Bits.Where(b => b.Bit.Kind != ItemBitKind.Modifier);

    // ── the boxes ──

    /// <summary>The name item text uses ("longsword"): only the unused records and added items can be renamed.</summary>
    public string Name
    {
        get => _name;
        set
        {
            var old = _name;
            if (!Set(ref _name, value)) return;
            // a renamed record takes the name GTEXT.LMP has for its new name, unless one was typed
            if (_owner.NamesLoaded && (_shownName.Length == 0 || _shownName == (_owner.GameName(old) ?? "") || _shownName == _autoShownName))
            {
                _shownName = _owner.GameName(value.Trim()) ?? "";
                _autoShownName = null;
                OnPropertyChanged(nameof(ShownName));
                OnPropertyChanged(nameof(DisplayName));
            }

            OnPropertyChanged(nameof(NameHint));
            OnPropertyChanged(nameof(Warning));
            OnPropertyChanged(nameof(ShownNameChanged));
            _owner.NameEdited();
        }
    }

    /// <summary>The English name the game shows (GTEXT.LMP).</summary>
    public string ShownName
    {
        get => _shownName;
        set
        {
            if (!SetProperty(ref _shownName, value)) return;
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ShownNameChanged));
            OnPropertyChanged(nameof(Problem));
            _owner.ShownNameEdited(this);
        }
    }

    internal void SetShownNameQuietly(string name)
    {
        if (_shownName == name) return;
        _shownName = name;
        OnPropertyChanged(nameof(ShownName));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ShownNameChanged));
        OnPropertyChanged(nameof(Problem));
    }

    /// <summary>A shown name made up for a new item ("Long Sword 2"): renaming the item replaces it like GTEXT.LMP's.</summary>
    internal void SetAutoShownName(string name)
    {
        SetShownNameQuietly(name);
        _autoShownName = _newShownName = name;
    }

    internal void ResetShownName()
    {
        SetShownNameQuietly(_owner.GameName(_name.Trim()) ?? "");
        OnPropertyChanged(nameof(NamesLoaded));
        OnPropertyChanged(nameof(NameHint));
    }

    /// <summary>True when the shown name differs from GTEXT.LMP's.</summary>
    public bool ShownNameChanged =>
        _owner.NamesLoaded && _shownName.Length > 0 && _shownName != (_owner.GameName(_name.Trim()) ?? "");

    /// <summary>The shown name, or the item name when there is none.</summary>
    public string DisplayName => _shownName.Length > 0 ? _shownName : _name;

    public string Weight { get => _weight; set => Set(ref _weight, value); }
    public string StackSize { get => _stack; set => Set(ref _stack, value); }
    public string DamageMin { get => _damageMin; set => Set(ref _damageMin, value); }
    public string DamageMax { get => _damageMax; set => Set(ref _damageMax, value); }
    public string Price { get => _price; set => Set(ref _price, value); }
    public string Armour { get => _armour; set => Set(ref _armour, value); }
    public string Model { get => _model; set => Set(ref _model, value); }
    public string PickupModel { get => _pickup; set => Set(ref _pickup, value); }
    public string Enchantment { get => _enchantment; set => Set(ref _enchantment, value); }
    public string Looks { get => _looks; set => Set(ref _looks, value); }

    public ItemCategory Category
    {
        get => _category;
        set
        {
            if (!Set(ref _category, value)) return;
            var slots = SlotsOf(value);
            if (!slots.Any(s => s.Value == _slot)) Set(ref _slot, slots[0].Value, nameof(Slot));
            if (value != ItemCategory.Weapon) Set(ref _stance, ItemStance.OneHanded, nameof(Stance));
            else if (_damageType == 0) Set(ref _damageType, 8, nameof(DamageType));     // bladed, the commonest
            OnPropertyChanged(nameof(SlotChoices));
            OnPropertyChanged(nameof(IsWeapon));
            OnPropertyChanged(nameof(CanChangeSlot));
        }
    }

    public ItemSlot Slot
    {
        get => _slot;
        set
        {
            if (!Set(ref _slot, value)) return;
            if (value == ItemSlot.Ammunition) Set(ref _stance, ItemStance.Ammunition, nameof(Stance));
            else if (_stance == ItemStance.Ammunition) Set(ref _stance, ItemStance.OneHanded, nameof(Stance));
        }
    }

    public ItemStance Stance
    {
        get => _stance;
        set
        {
            if (!Set(ref _stance, value)) return;
            if (value == ItemStance.Ammunition) Set(ref _slot, ItemSlot.Ammunition, nameof(Slot));
            else if (_slot == ItemSlot.Ammunition) Set(ref _slot, ItemSlot.OneHanded, nameof(Slot));
        }
    }

    public int DamageType { get => _damageType; set => Set(ref _damageType, value); }

    internal bool HasBit(int bit) => ItemBits.Has(_otherFlags, bit);

    internal void SetBit(int bit, bool on)
    {
        var flags = on ? _otherFlags | 1UL << bit : _otherFlags & ~(1UL << bit);
        if (flags == _otherFlags) return;
        _otherFlags = flags;
        Changed(nameof(Bits));
    }

    // ── choices ──

    public static IReadOnlyList<Choice<ItemCategory>> CategoryChoices { get; } = new[]
    {
        new Choice<ItemCategory>(ItemCategory.Weapon, "Weapon"),
        new Choice<ItemCategory>(ItemCategory.Armour, "Armour"),
        new Choice<ItemCategory>(ItemCategory.Other, "Other (rings, amulets, potions ...)")
    };

    public static IReadOnlyList<Choice<ItemStance>> StanceChoices { get; } =
        Enum.GetValues<ItemStance>().Select(s => new Choice<ItemStance>(s, ItemEditorModel.StanceText(s))).ToArray();

    public static IReadOnlyList<Choice<int>> DamageTypeChoices { get; } = new[]
    {
        new Choice<int>(0, "None"), new Choice<int>(1, "Blunt"), new Choice<int>(4, "Ranged"), new Choice<int>(8, "Bladed")
    };

    public IReadOnlyList<Choice<ItemSlot>> SlotChoices => SlotsOf(_category);

    private static IReadOnlyList<Choice<ItemSlot>> SlotsOf(ItemCategory category) => (category switch
    {
        ItemCategory.Weapon => new[] { ItemSlot.OneHanded, ItemSlot.TwoHanded, ItemSlot.Ammunition },
        ItemCategory.Armour => new[] { ItemSlot.Shield, ItemSlot.Body, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Helmet },
        _ => new[] { ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Usable }
    }).Select(s => new Choice<ItemSlot>(s, ItemEditorModel.SlotText(s))).ToArray();

    // ── checking and building ──

    /// <summary>The record the boxes describe, or null; <paramref name="problem"/> says what is wrong.</summary>
    internal GameItem? TryBuild(out string? problem)
    {
        problem = null;
        var item = Source.WithIndex(_index);
        item.Name = _name.Trim();
        if (_weight.Trim() == ItemEditorModel.Format(Source.Weight)) item.Weight = Source.Weight;
        else if (NumberText.TryParseDecimal(_weight, out var w)) item.Weight = (float)w;
        else
        {
            problem = "Weight: enter a number, such as 4 or 0.5.";
            return null;
        }

        int Whole(string text, string what, ref string? why)
        {
            if (NumberText.TryParseInteger(text, out var v)) return v;
            why ??= $"{what}: enter a whole number.";
            return 0;
        }

        item.StackSize = Whole(_stack, "Stack size", ref problem);
        item.DamageMin = Whole(_damageMin, "Damage", ref problem);
        item.DamageMax = Whole(_damageMax, "Damage", ref problem);
        item.Price = Whole(_price, "Price", ref problem);
        item.Armour = Whole(_armour, "Armour", ref problem);
        item.Model = Whole(_model, "Model", ref problem);
        item.PickupModel = Whole(_pickup, "Pickup model", ref problem);
        var enchantment = Whole(_enchantment, "+N", ref problem);
        var looks = Whole(_looks, "Looks", ref problem);
        if (problem != null) return null;
        if (enchantment is < 0 or > 15)
        {
            problem = "+N: 0 to 15.";
            return null;
        }

        if (looks is < 1 or > 7)
        {
            problem = "Looks: 1 to 7.";
            return null;
        }

        item.Category = _category;
        item.Slot = _slot;
        item.Stance = _stance;
        item.DamageType = _damageType;
        var keepLooksBits = looks == ItemBits.Looks(Source.Flags) ? Source.Flags & ItemBits.LooksMask : ItemBits.WithLooks(0, looks);
        item.Flags = _otherFlags | (uint)enchantment | keepLooksBits;

        problem = BgdaExecutable.CheckItem(item, Source, _owner.CurrentNames());
        return problem == null ? item : null;
    }

    /// <summary>True when <paramref name="built"/> differs from the record in the executable.</summary>
    internal bool IsChanged(GameItem built) =>
        built.Name != Source.Name || BitConverter.SingleToInt32Bits(built.Weight) != BitConverter.SingleToInt32Bits(Source.Weight) ||
        built.StackSize != Source.StackSize || built.DamageMin != Source.DamageMin || built.DamageMax != Source.DamageMax ||
        built.Price != Source.Price || built.Armour != Source.Armour || built.Slot != Source.Slot ||
        built.Category != Source.Category || built.Stance != Source.Stance || built.DamageType != Source.DamageType ||
        built.Flags != Source.Flags || built.Model != Source.Model || built.PickupModel != Source.PickupModel;

    /// <summary>What is wrong with the boxes, or null.</summary>
    public string? Problem
    {
        get
        {
            if (TryBuild(out var problem) == null) return problem;
            if (_owner.NamesLoaded && ShownNameChanged && GameText.CheckItemName(_shownName) is { } nameProblem)
                return "Shown name: " + nameProblem;
            if (_owner.NamesLoaded && _shownName.Length == 0 && (_name.Trim() != Source.Name || IsNew))
                return "Shown name: GTEXT.LMP has no name for \"" + _name.Trim() + "\"; enter the name the game shows.";
            return null;
        }
    }

    /// <summary>Something that will look wrong in the game but can be saved, or null.</summary>
    public string? Warning
    {
        get
        {
            var name = _name.Trim();
            if (name == Source.Name && !IsNew || _owner.NamesLoaded || GameText.ReadyMadeItemNames.Contains(name)) return null;
            return $"Open GTEXT.LMP to give \"{name}\" a name: without one the game shows NF:{(name + "_name")[..Math.Min(16, name.Length + 5)]}.";
        }
    }

    /// <summary>True when any box differs from the executable (or the shown name from GTEXT.LMP), or the item is new or moved.</summary>
    public bool IsEdited => IsNew || _index != Source.Index || (TryBuild(out _) is not { } item || IsChanged(item)) || ShownNameChanged;

    // ── what the grid and the panel show ──

    private GameItem Current => TryBuild(out _) ?? Source;

    public string KindText
    {
        get
        {
            var item = Current;
            return item.Category switch
            {
                ItemCategory.Weapon => ItemEditorModel.StanceText(item.Stance),
                ItemCategory.Armour => item.Slot == ItemSlot.Body ? "Body armour" : item.Slot.ToString(),
                _ => item.Slot == ItemSlot.Usable ? "Usable" : item.Slot.ToString()
            };
        }
    }

    public string DamageText
    {
        get
        {
            var item = Current;
            if (item.Category != ItemCategory.Weapon) return "";
            var (min, max) = ItemFormulas.Damage(item, item.Flags, _owner.CurrentMaterials());
            return min == max ? $"{min}" : $"{min}–{max}";
        }
    }

    public string ArmourText
    {
        get
        {
            var item = Current;
            var armour = ItemFormulas.Armour(item, item.Flags);
            return armour == 0 ? "" : armour.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>The price a shop shows for one (for arrows: for a stack).</summary>
    public string ValueText
    {
        get
        {
            var item = Current;
            var count = Math.Max(1, item.StackSize);
            return ItemFormulas.ShopValue(item, count, item.Flags, _owner.CurrentStanceWeights(), _owner.CurrentMaterials())
                .ToString("N0", CultureInfo.InvariantCulture);
        }
    }

    public string WeightText => Current.Weight.ToString("0.##", CultureInfo.InvariantCulture);

    public string ModifiersText
    {
        get
        {
            var item = Current;
            return ItemBits.Describe(item.Flags, ItemBits.Has(item.Flags, ItemBits.Metal));
        }
    }

    /// <summary>The panel's summary: damage at each material, armour, shop prices, the models used.</summary>
    public string Preview
    {
        get
        {
            var item = Current;
            var lines = new List<string>();
            var multipliers = _owner.CurrentMaterials();
            if (item.Category == ItemCategory.Weapon && item.Stance != ItemStance.Ammunition)
            {
                var (min, max) = ItemFormulas.Damage(item, item.Flags, multipliers);
                var materials = new[] { (ItemBits.Fine, "fine"), (ItemBits.Masterwork, "masterwork"), (ItemBits.Mithril, "mithril"), (ItemBits.Adamantine, "adamantine") }
                    .Select(m =>
                    {
                        var (a, b) = ItemFormulas.Damage(item, item.Flags | 1UL << m.Item1, multipliers);
                        return $"{m.Item2} {a}–{b}";
                    });
                lines.Add($"Damage per hit {min}–{max}; " + string.Join(", ", materials) + ".");
            }

            if (item.Category == ItemCategory.Armour) lines.Add($"Armour {ItemFormulas.Armour(item, item.Flags)}; each +N adds 1.");

            var count = Math.Max(1, item.StackSize);
            var value = ItemFormulas.ShopValue(item, count, item.Flags, _owner.CurrentStanceWeights(), multipliers);
            var one = ItemFormulas.ShopValue(item, 1, item.Flags, _owner.CurrentStanceWeights(), multipliers);
            var pays = ItemFormulas.SellPrice(one, item.Stance == ItemStance.Ammunition && item.Category == ItemCategory.Weapon);
            lines.Add(count > 1
                ? $"Shop price {value:N0} for {count}; a shop pays {pays:N0} for 20."
                : $"Shop price {value:N0}; a shop pays {pays:N0}.");
            if (item.Category == ItemCategory.Weapon && item.Stance != ItemStance.Ammunition)
                lines.Add("A weapon's price comes from its damage: the game replaces the price box with it when it starts.");
            else if (item.Category == ItemCategory.Other && item.Slot is not (ItemSlot.Ring or ItemSlot.Amulet))
                lines.Add("The game values this kind at a quarter of the price box (it halves it twice).");

            lines.Add(ModelHint);
            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>Which models (or ARMOR.YAK entries) the item uses.</summary>
    public string ModelHint
    {
        get
        {
            var item = Current;
            var looks = ItemBits.Looks(item.Flags);
            string Files(int m) => _owner.ModelAt(m) is { } model ? model.Vif : "?";
            var held = item.WornFromArmorYak
                ? $"Worn: ARMOR.YAK entry {item.Model} (and {item.Model + 1} when enchanted)."
                : looks == 1
                    ? $"Held: model {item.Model} ({Files(item.Model)})."
                    : $"Held: models {item.Model}–{item.Model + looks - 1} ({string.Join(", ", Enumerable.Range(item.Model, looks).Select(Files))}).";
            return held + $" On the ground: model {item.PickupModel} ({Files(item.PickupModel)}).";
        }
    }

    /// <summary>For an unused record or an added item: what renaming it does, and the names GTEXT.LMP already has.</summary>
    public string NameHint
    {
        get
        {
            if (!HasNameHint) return "";
            var ready = GameText.ReadyMadeItemNames
                .Where(n => n.Length <= Source.NameCapacity && _owner.Items.All(r => r._name.Trim() != n))
                .Select(n => _owner.GameName(n) is { } shown ? $"{n} ({shown})" : n)
                .ToList();
            var start = IsAdded
                ? $"An item added after the game's own (number {_index}, three-player executables only): rename it (up to {Source.NameCapacity} " +
                  "letters, digits and _) and set it up as any item; put its name in a shop list to sell it. "
                : $"An unused spell-scroll record: rename it (up to {Source.NameCapacity} letters) and set it up to make a new item. ";
            return start +
                   (ready.Count > 0 ? "Names the game already has in all five languages: " + string.Join(", ", ready) + ". " : "") +
                   "Under Kind, tick what it is like (Metal for Mithral and Adamantine rather than Darkwood and Oak; Bladed or Blunt " +
                   "for the random modifiers that suit it) and untick Never random loot to let it drop.";
        }
    }

    /// <summary>Puts the boxes back as they are in the executable.</summary>
    public void Revert()
    {
        _name = Source.Name;
        _weight = ItemEditorModel.Format(Source.Weight);
        _stack = Text(Source.StackSize);
        _damageMin = Text(Source.DamageMin);
        _damageMax = Text(Source.DamageMax);
        _price = Text(Source.Price);
        _armour = Text(Source.Armour);
        _model = Text(Source.Model);
        _pickup = Text(Source.PickupModel);
        _enchantment = Text(ItemBits.Enchantment(Source.Flags));
        _looks = Text(ItemBits.Looks(Source.Flags));
        _category = Source.Category;
        _slot = Source.Slot;
        _stance = Source.Stance;
        _damageType = Source.DamageType;
        _otherFlags = Source.Flags & ~(ItemBits.EnchantmentMask | ItemBits.LooksMask);
        _shownName = _owner.GameName(Source.Name) ?? _newShownName ?? "";
        _autoShownName = _newShownName;
        OnPropertyChanged(string.Empty);
        foreach (var b in Bits) b.Refresh();
        _owner.ShownNameEdited(this);
        _owner.ItemEdited();
    }

    private bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (!SetProperty(ref field, value, name)) return false;
        Changed(name);
        return true;
    }

    private void Changed(string? name)
    {
        RefreshDerived();
        if (name == nameof(Bits)) foreach (var b in Bits) b.Refresh();
        _owner.ItemEdited();
    }

    internal void RefreshDerived()
    {
        foreach (var p in new[]
                 {
                     nameof(Problem), nameof(Warning), nameof(IsEdited), nameof(KindText), nameof(DamageText), nameof(ArmourText), nameof(ValueText),
                     nameof(WeightText), nameof(ModifiersText), nameof(Preview), nameof(ModelHint)
                 })
        {
            OnPropertyChanged(p);
        }
    }
}

/// <summary>One property bit of an item, as a tick box.</summary>
public sealed class ItemBitRow : ObservableObject
{
    private readonly ItemRow _row;

    internal ItemBitRow(ItemRow row, ItemBit bit)
    {
        _row = row;
        Bit = bit;
    }

    public ItemBit Bit { get; }
    public string Name => Bit.Name;
    public string Effect => $"Bit {Bit.Bit}" + (Bit.Keyword != null ? $", item text \"+{Bit.Keyword}\"" : "") + ". " + Bit.Effect;

    public bool IsSet
    {
        get => _row.HasBit(Bit.Bit);
        set => _row.SetBit(Bit.Bit, value);
    }

    internal void Refresh() => OnPropertyChanged(nameof(IsSet));
}

// ═══════════════════════════════ a model ═══════════════════════════════

/// <summary>One record of the model table: a .vif model and a .tex texture of EQUIPMNT.LMP.</summary>
public sealed class ItemModelRow : ObservableObject
{
    private readonly ItemEditorModel _owner;
    private string _vif, _tex;

    internal ItemModelRow(ItemEditorModel owner, GameItemModel source)
    {
        _owner = owner;
        Source = source;
        _vif = source.Vif;
        _tex = source.Tex;
    }

    public GameItemModel Source { get; }
    public int Index => Source.Index;

    public string Vif
    {
        get => _vif;
        set
        {
            if (SetProperty(ref _vif, value)) Refresh();
        }
    }

    public string Tex
    {
        get => _tex;
        set
        {
            if (SetProperty(ref _tex, value)) Refresh();
        }
    }

    public bool IsChanged => _vif.Trim() != Source.Vif || _tex.Trim() != Source.Tex;

    public string? Problem => IsChanged
        ? _owner.CheckModel(Index, _vif.Trim(), _tex.Trim()) ??
          _owner.MissingEquipmentFile(_vif.Trim()) ?? _owner.MissingEquipmentFile(_tex.Trim())
        : null;

    /// <summary>The items that show this model (held, as one of their looks, or on the ground).</summary>
    public string UsedBy
    {
        get
        {
            var names = _owner.NamesUsingModel(Index).Distinct().ToList();
            return names.Count == 0 ? "(no item)" : string.Join(", ", names);
        }
    }

    internal void Refresh()
    {
        OnPropertyChanged(nameof(Problem));
        OnPropertyChanged(nameof(IsChanged));
        OnPropertyChanged(nameof(UsedBy));
    }
}

// ═══════════════════════════════ a shop entry ═══════════════════════════════

/// <summary>One item a shop sells, as item text.</summary>
public sealed class ShopRow : ObservableObject
{
    private readonly ItemEditorModel _owner;
    private string _text;

    internal ShopRow(ItemEditorModel owner, ShopEntry source)
    {
        _owner = owner;
        Source = source;
        _text = source.Text;
        Position = owner.AllShopRows.Count(r => r.Stock == source.Stock) + 1;
    }

    public ShopEntry Source { get; }
    public int Number => Source.Number;
    public ShopStock Stock => Source.Stock;

    /// <summary>1, 2, 3 ... within its shop.</summary>
    public int Position { get; }

    public string ListText => Source.List switch
    {
        ItemCategory.Weapon => "Weapons",
        ItemCategory.Armour => "Armour",
        _ => "Other"
    };

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value)) Refresh();
        }
    }

    public bool IsChanged => _text.Trim() != Source.Text;

    /// <summary>What the game makes of the text, in English.</summary>
    public string Result => _owner.DescribeItemText(_text.Trim(), out _);

    public string? Problem
    {
        get
        {
            var text = _text.Trim();
            var description = _owner.DescribeItemText(text, out var parsed);
            if (!parsed.IsValid) return description;
            var items = _owner.CurrentItems();
            if (items[parsed.ItemIndex].Category != Source.List)
                return $"This is the {ListText.ToLowerInvariant()} list; {text} isn't one of those.";
            if (!Source.AnyText && text != Source.Text && !_owner.NearbyShopTexts.Contains(text))
                return "This entry can only sell text the game has nearby: " + string.Join(", ", _owner.NearbyShopTexts) + ".";
            return null;
        }
    }

    /// <summary>Why the item this entry sells is gone (renamed, removed or made another kind), or null.</summary>
    internal string? LostItem
    {
        get
        {
            var text = _text.Trim();
            var items = _owner.CurrentItems();
            var parsed = ItemText.Parse(text, items);
            if (parsed.ItemIndex < 0) return parsed.Problems[^1] + " Sell something else here, or keep the item.";
            return items[parsed.ItemIndex].Category != Source.List
                ? $"This is the {ListText.ToLowerInvariant()} list; {text} isn't one of those any more."
                : null;
        }
    }

    internal void Refresh()
    {
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(Problem));
        OnPropertyChanged(nameof(IsChanged));
    }
}

// ═══════════════════════════════ tuning ═══════════════════════════════

/// <summary>A material multiplier or a stance weight.</summary>
public sealed class TuningRow : ObservableObject
{
    private readonly ItemEditorModel _owner;
    private readonly bool _upperHalf;
    private string _value;

    internal TuningRow(ItemEditorModel owner, string name, float value, float stock, bool upperHalf)
    {
        _owner = owner;
        Name = name;
        Source = value;
        StockValue = stock;
        _upperHalf = upperHalf;
        _value = ItemEditorModel.Format(value);
    }

    public string Name { get; }
    public float Source { get; }
    public float StockValue { get; }
    public string Stock => ItemEditorModel.Format(StockValue);

    public string Value
    {
        get => _value;
        set
        {
            if (!SetProperty(ref _value, value)) return;
            OnPropertyChanged(nameof(Note));
            _owner.TuningEdited();
        }
    }

    public bool IsChanged => _value.Trim() != ItemEditorModel.Format(Source);

    /// <summary>The value to write; throws <see cref="ArgumentException"/> when the box isn't a number in range.</summary>
    internal float Parse()
    {
        if (!IsChanged) return Source;
        if (!NumberText.TryParseDecimal(_value, out var v)) throw new ArgumentException($"{Name}: enter a number.");
        var (min, max) = _upperHalf ? (0.0, 100.0) : (0.01, 100.0);
        if (v < min || v > max) throw new ArgumentException($"{Name}: {min} to {max}.");
        return (float)v;
    }

    internal float ValueOrStock()
    {
        try
        {
            var v = Parse();
            return _upperHalf ? BgdaExecutable.UpperHalfToFloat(BgdaExecutable.FloatToUpperHalf(v)) : v;
        }
        catch (ArgumentException)
        {
            return Source;
        }
    }

    /// <summary>A problem, or (for materials) the value the game will actually get when it is rounded.</summary>
    public string Note
    {
        get
        {
            try
            {
                var v = Parse();
                if (!_upperHalf) return "";
                var saved = BgdaExecutable.UpperHalfToFloat(BgdaExecutable.FloatToUpperHalf(v));
                return saved == v ? "" : $"saved as {ItemEditorModel.Format(saved)}";
            }
            catch (ArgumentException e)
            {
                return e.Message;
            }
        }
    }
}

/// <summary>One layer of the enchant glow.</summary>
public sealed class GlowRow : ObservableObject
{
    private string _r, _g, _b;

    internal GlowRow(string name, Rgb value, Rgb stock)
    {
        Name = name;
        Source = value;
        Stock = $"{stock.R}, {stock.G}, {stock.B}";
        _r = value.R.ToString(CultureInfo.InvariantCulture);
        _g = value.G.ToString(CultureInfo.InvariantCulture);
        _b = value.B.ToString(CultureInfo.InvariantCulture);
    }

    public string Name { get; }
    public Rgb Source { get; }
    public string Stock { get; }

    public string R { get => _r; set => SetChannel(ref _r, value); }
    public string G { get => _g; set => SetChannel(ref _g, value); }
    public string B { get => _b; set => SetChannel(ref _b, value); }

    private void SetChannel(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (!SetProperty(ref field, value, name)) return;
        OnPropertyChanged(nameof(Swatch));
    }

    public bool IsChanged => Parse(throwOnError: false) is { } c && c != Source || Parse(throwOnError: false) == null;

    /// <summary>The colour as #RRGGBB for the swatch (transparent while a box isn't 0-255).</summary>
    public string Swatch => Parse(throwOnError: false) is { } c ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : "#00000000";

    internal Rgb Parse() => Parse(throwOnError: true)!.Value;

    private Rgb? Parse(bool throwOnError)
    {
        int Channel(string text, string what)
        {
            if (NumberText.TryParseInteger(text, out var v) && v is >= 0 and <= 255) return v;
            throw new ArgumentException($"Glow {Name.Split(' ')[0].ToLowerInvariant()} {what}: 0 to 255.");
        }

        try
        {
            return new Rgb(Channel(_r, "red"), Channel(_g, "green"), Channel(_b, "blue"));
        }
        catch (ArgumentException) when (!throwOnError)
        {
            return null;
        }
    }
}

/// <summary>One entry of a random-loot list.</summary>
public sealed class LootRow : ObservableObject
{
    private ulong _value;

    internal LootRow(int position, ulong value)
    {
        Position = position;
        Source = value;
        _value = value;
    }

    public int Position { get; }
    public ulong Source { get; }

    public ulong Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value)) OnPropertyChanged(nameof(Meaning));
        }
    }

    public bool IsChanged => _value != Source;

    public string Meaning
    {
        get
        {
            if (_value is >= 1 and <= 15) return $"+{_value}: adds {_value} to damage, armour and the other bonuses.";
            var bit = System.Numerics.BitOperations.TrailingZeroCount(_value);
            return ItemBits.Get(bit).Effect;
        }
    }
}
