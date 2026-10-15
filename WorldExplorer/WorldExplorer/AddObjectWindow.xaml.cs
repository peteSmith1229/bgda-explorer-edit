using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.World;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorldExplorer.Themes;

namespace WorldExplorer;

/// <summary>
/// Chooses a new object for a Dark Alliance level: one of the classes the
/// game's object factory creates (its model comes from the class's archive),
/// or a model from an archive, which the game shows with its UserParamProp
/// object. Then where it goes, which way it faces and its properties.
/// </summary>
public partial class AddObjectWindow : Window
{
    private readonly GobFile? _gob;
    private readonly List<ClassChoice> _classes;

    /// <summary>The object to add, once the dialog closes with Add.</summary>
    public ObjectData? Result { get; private set; }

    /// <param name="gob">The level's GOB: its archives are the ones objects can use without loading from the disc.</param>
    /// <param name="position">Where the object starts, in the level's units.</param>
    /// <param name="facing">Which way it starts facing, in degrees.</param>
    /// <param name="placementHint">Where that position came from, for the hint under it.</param>
    /// <param name="archive">An archive to start with: its classes, or its models when no class uses it.</param>
    public AddObjectWindow(GobFile? gob, (float X, float Y, float Z) position, double facing, string placementHint,
        string? archive = null)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _gob = gob;

        _classes = BgdaObjectClasses.All
            .Select(c => new ClassChoice(c, KindName(c.Kind), Where(c.Archive)))
            .OrderBy(c => KindOrder(c.Class.Kind))
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        ClassList.ItemsSource = _classes;

        var archives = gob?.Directory
            .Select(e => new ArchiveChoice(e.Key, e.Value, gob.AddedArchives.Contains(e.Key)))
            .ToList() ?? new List<ArchiveChoice>();
        ArchiveBox.ItemsSource = archives;

        FacingBox.ItemsSource = Enumerable.Range(0, 16)
            .Select(i => (i * LevelObjects.FacingStep).ToString("0.#", CultureInfo.InvariantCulture) + "°").ToList();
        FacingBox.SelectedIndex = (ushort)LevelObjects.FacingToI6(facing) >> 12;
        XBox.Text = Format(position.X);
        YBox.Text = Format(position.Y);
        ZBox.Text = Format(position.Z);
        PlacementHint.Text = placementHint + " Positions are in the level's units, as in the properties panel.";

        // Start where the archive leads: the classes that use it, or the models it holds.
        var start = archive == null ? null : archives.FirstOrDefault(a => a.Name.Equals(archive, StringComparison.OrdinalIgnoreCase));
        ArchiveBox.SelectedItem = start ?? archives.FirstOrDefault(a => HasModels(a.Lmp)) ?? archives.FirstOrDefault();
        if (archive != null && LevelObjects.ClassesUsing(archive).Count == 0 && start != null)
        {
            ModelMode.IsChecked = true;
        }
        else
        {
            ClassMode.IsChecked = true;
            if (archive != null) ClassFilter.Text = archive;
            ClassList.SelectedIndex = 0;
        }
    }

    private sealed record ClassChoice(BgdaObjectClass Class, string Kind, string Where)
    {
        public string Name => Class.Name;

        public string Detail => Class.Archive == null
            ? $"{Kind} · no model of its own"
            : $"{Kind} · {Class.Archive} · {Where}";
    }

    private sealed record ArchiveChoice(string Name, LmpFile Lmp, bool Added)
    {
        public string Display => Added ? $"{Name}  (added)" : Name;
    }

    private static string KindName(BgdaObjectKind kind) => kind switch
    {
        BgdaObjectKind.Npc => "Character",
        BgdaObjectKind.Monster => "Monster",
        BgdaObjectKind.Ambient => "Animal",
        BgdaObjectKind.PlayerStart => "Player start",
        BgdaObjectKind.Light => "Light",
        _ => "Object"
    };

    private static int KindOrder(BgdaObjectKind kind) => kind switch
    {
        BgdaObjectKind.Npc => 0,
        BgdaObjectKind.Monster => 1,
        BgdaObjectKind.Ambient => 2,
        BgdaObjectKind.Object => 3,
        BgdaObjectKind.PlayerStart => 4,
        _ => 5
    };

    private bool InGob(string archive) =>
        _gob?.Directory.Keys.Any(k => k.Equals(archive, StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>Where the game finds <paramref name="archive"/> for this level.</summary>
    private string Where(string? archive)
    {
        if (archive == null) return "";
        if (_gob != null && _gob.AddedArchives.Any(a => a.Equals(archive, StringComparison.OrdinalIgnoreCase)))
            return $"added to {_gob.Name}";
        if (InGob(archive)) return $"in {_gob!.Name}";
        return BgdaObjectClasses.DiscArchives.ContainsKey(archive) ? "loaded from the disc" : "not found";
    }

    private static bool HasModels(LmpFile lmp)
    {
        try
        {
            var names = lmp.EffectiveEntryNames();
            return names.Any(n => n.EndsWith(".vif", StringComparison.OrdinalIgnoreCase)) &&
                   names.Any(n => n.EndsWith(".anm", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or InvalidDataException)
        {
            return false;
        }
    }

    private static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // ─────────────────────────────────────────────────────────────────────────
    // A character or object
    // ─────────────────────────────────────────────────────────────────────────

    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
        if (ClassPanel == null || ModelPanel == null) return;
        var model = ModelMode.IsChecked == true;
        ClassPanel.Visibility = model ? Visibility.Collapsed : Visibility.Visible;
        ModelPanel.Visibility = model ? Visibility.Visible : Visibility.Collapsed;
        ClearProblem();
    }

    private void ClassFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        var words = ClassFilter.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var selected = ClassList.SelectedItem;
        var shown = _classes.Where(c => words.All(w =>
            c.Name.Contains(w, StringComparison.OrdinalIgnoreCase) ||
            c.Kind.Contains(w, StringComparison.OrdinalIgnoreCase) ||
            (c.Class.Archive?.Contains(w, StringComparison.OrdinalIgnoreCase) ?? false))).ToList();
        ClassList.ItemsSource = shown;
        ClassList.SelectedItem = shown.Contains(selected) ? selected : shown.FirstOrDefault();
    }

    private void ClassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ClearProblem();
        if (ClassList.SelectedItem is not ClassChoice choice)
        {
            ClassInfo.Text = ClassList.Items.Count == 0 ? "Nothing matches the filter." : "";
            return;
        }

        var c = choice.Class;
        var gobName = _gob?.Name ?? "the GOB";
        string text;
        if (c.Archive == null)
        {
            text = c.ArchiveProperty != null
                ? $"It shows a model from the archive its {c.ArchiveProperty}= property names: choose “A model from an archive” to set one up."
                : "It has no model of its own.";
        }
        else if (InGob(c.Archive))
        {
            text = $"Its model comes from {c.Archive}, which {gobName} holds.";
        }
        else if (BgdaObjectClasses.DiscArchives.TryGetValue(c.Archive, out var size))
        {
            text = $"Its model comes from {c.Archive}, which {gobName} doesn't hold: the game loads it from the disc " +
                   $"({size / 1024:N0} KB) when the level starts. Add it to the GOB with Add Archive… in the explorer " +
                   "to keep it with the level.";
        }
        else
        {
            text = $"Its model comes from {c.Archive}, which isn't in {gobName} or on the game disc. Add it to the GOB first.";
        }

        if (c.ExtraArchive != null) text += $" It also loads {c.ExtraArchive}.";
        text += $" It takes one of the level's {SlotName(c.Size)}.";
        ClassInfo.Text = text;
    }

    /// <summary>The level-budget pool an object of <paramref name="size"/> bytes takes a slot in.</summary>
    private static string SlotName(int size) => BgdaLevelBudget.PoolFor(size) switch
    {
        BgdaPool.Small => "small-object slots",
        BgdaPool.Medium => "monster and character slots",
        _ => "large-creature slots"
    };

    private void ClassList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ClassList.SelectedItem is ClassChoice) Add_Click(sender, e);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // A model from an archive
    // ─────────────────────────────────────────────────────────────────────────

    private void ArchiveBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ClearProblem();
        var names = new List<string>();
        if (ArchiveBox.SelectedItem is ArchiveChoice archive)
        {
            try
            {
                names = archive.Lmp.EffectiveEntryNames();
            }
            catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or InvalidDataException)
            {
                // Not an archive these files can be listed from: nothing to choose.
            }
        }

        List<string> Of(string extension) =>
            names.Where(n => n.EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToList();

        ModelBox.ItemsSource = Of(".vif");
        TextureBox.ItemsSource = Of(".tex");
        AnimationBox.ItemsSource = Of(".anm");
        ModelBox.SelectedIndex = ModelBox.Items.Count > 0 ? 0 : -1;
        AnimationBox.SelectedIndex = AnimationBox.Items.Count > 0 ? 0 : -1;
        if (TextureBox.SelectedIndex < 0 && TextureBox.Items.Count > 0) TextureBox.SelectedIndex = 0;

        ModelInfo.Text =
            "The game shows it with its UserParamProp object: the model, with the texture, playing the animation " +
            "over and over. It doesn't talk, walk about or fight. It takes one of the level's " +
            $"{SlotName(BgdaObjectClasses.Find(LevelObjects.ModelPropClass)?.Size ?? 0)}." +
            (AnimationBox.Items.Count == 0 && ModelBox.Items.Count > 0
                ? " This archive has no animation (.anm), which UserParamProp needs."
                : "");
    }

    /// <summary>The texture named like the model, when the archive has one.</summary>
    private void ModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelBox.SelectedItem is not string model) return;
        var texture = Path.GetFileNameWithoutExtension(model) + ".tex";
        var match = TextureBox.Items.OfType<string>()
            .FirstOrDefault(t => t.Equals(texture, StringComparison.OrdinalIgnoreCase));
        if (match != null) TextureBox.SelectedItem = match;
        else if (TextureBox.SelectedIndex < 0 && TextureBox.Items.Count > 0) TextureBox.SelectedIndex = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Adding
    // ─────────────────────────────────────────────────────────────────────────

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var problem = TryBuild(out var result);
        if (problem != null)
        {
            ProblemText.Text = problem;
            ProblemText.Visibility = Visibility.Visible;
            return;
        }

        Result = result;
        DialogResult = true;
    }

    private void ClearProblem()
    {
        if (ProblemText == null) return;
        ProblemText.Text = "";
        ProblemText.Visibility = Visibility.Collapsed;
    }

    private string? TryBuild(out ObjectData? result)
    {
        result = null;
        if (!TryReadNumber(XBox.Text, out var x) || !TryReadNumber(YBox.Text, out var y) ||
            !TryReadNumber(ZBox.Text, out var z))
        {
            return "Enter the position as numbers, such as -120.5.";
        }

        var properties = PropertiesBox.Text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
        foreach (var property in properties)
        {
            if (LevelObjects.CheckProperty(property) is { } bad) return bad;
        }

        string className;
        if (ModelMode.IsChecked == true)
        {
            if (ArchiveBox.SelectedItem is not ArchiveChoice archive) return "Choose an archive.";
            var model = ModelBox.SelectedItem as string ?? "";
            var texture = TextureBox.SelectedItem as string ?? "";
            var animation = AnimationBox.SelectedItem as string ?? "";
            if (LevelObjects.CheckModelProp(archive.Lmp, model, texture, animation) is { } problem) return problem;

            // The game reads the first property of a name, so these come before any typed below.
            properties = LevelObjects.ModelPropProperties(archive.Name, model, texture, animation,
                NoCollideBox.IsChecked == true).Concat(properties).ToList();
            className = LevelObjects.ModelPropClass;
        }
        else
        {
            if (ClassList.SelectedItem is not ClassChoice choice) return "Choose what to add.";
            className = choice.Name;
        }

        result = LevelObjects.Create(className, x, y, z, FacingBox.SelectedIndex * LevelObjects.FacingStep, properties);
        return null;
    }

    private static bool TryReadNumber(string text, out float value)
    {
        value = 0;
        if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
            !double.IsFinite(number) || Math.Abs(number) > 1e6)
        {
            return false;
        }

        value = (float)number;
        return true;
    }
}
