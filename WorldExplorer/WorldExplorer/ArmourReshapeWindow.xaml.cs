using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Models;
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WorldExplorer.Logging;
using WorldExplorer.Themes;
using WorldExplorer.TreeView;
using WorldExplorer.Win3D;

namespace WorldExplorer;

/// <summary>
/// Scales an ARMOR.YAK entry's models (each side of a pair about its own centre) and pushes them out along their
/// normals, for the ticked characters. Shows each model's size before and after, and a live 3D preview of one
/// character's piece: the bytes <see cref="ArmorModel.Reshaped"/> gives, drawn the way the model view draws them.
/// </summary>
public partial class ArmourReshapeWindow : Window
{
    private readonly YakFile _yak;
    private readonly int _entry;
    private readonly CheckBox[] _characters = new CheckBox[4];
    private readonly BitmapSource?[] _textures = new BitmapSource?[4];
    private readonly bool[] _decoded = new bool[4];

    // Reshaping the biggest pieces (the clothes bodies) takes tens of milliseconds, so slider moves are gathered
    // and drawn at most this often rather than once per step.
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(60) };
    private bool _ready;

    public double ScaleX => xSlider.Value / 100;
    public double ScaleY => ySlider.Value / 100;
    public double ScaleZ => zSlider.Value / 100;
    public double Inflate => inflateSlider.Value;

    /// <summary>The ticked characters.</summary>
    public IEnumerable<int> Characters => Enumerable.Range(0, 4).Where(c => _characters[c].IsChecked == true);

    /// <param name="child">The character to tick and preview, or null (the entry's menu) for the human, elf and dwarf.</param>
    public ArmourReshapeWindow(YakFile yak, int entry, int? child)
    {
        _yak = yak;
        _entry = entry;
        InitializeComponent();
        ThemeManager.Attach(this);

        headerText.Text = YakTreeViewModel.EntryLabel(yak, entry);
        for (var c = 0; c < 4; c++)
        {
            _characters[c] = new CheckBox
            {
                Content = ArmorYakInfo.Characters[c],
                Margin = new Thickness(0, 0, 18, 0),
                IsChecked = child is { } one ? c == one : c < 3
            };
            _characters[c].Checked += (_, _) => Update();
            _characters[c].Unchecked += (_, _) => Update();
            characterPanel.Children.Add(_characters[c]);
        }

        previewBox.ItemsSource = ArmorYakInfo.Characters;
        previewBox.SelectedIndex = child ?? 0;
        previewBox.SelectionChanged += (_, _) => Update(frame: true);
        originalBox.Checked += (_, _) => ShowPreview(frame: false);
        originalBox.Unchecked += (_, _) => ShowPreview(frame: false);

        MainWindow.SetupViewport(viewport);
        viewport.SetResourceReference(HelixToolkit.Wpf.HelixViewport3D.CoordinateSystemLabelForegroundProperty,
            "Brush.TextSecondary");

        _updateTimer.Tick += (_, _) =>
        {
            _updateTimer.Stop();
            Update();
        };
        Closed += (_, _) => _updateTimer.Stop();
        Loaded += (_, _) => FramePreview();

        _ready = true;
        Update();
    }

    private void AnyValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // the first move starts the timer; moves until it fires are drawn together
        if (_ready && !_updateTimer.IsEnabled) _updateTimer.Start();
    }

    private static string Size(double[] p)
    {
        var n = p.Length / 3;
        var size = Enumerable.Range(0, 3).Select(a =>
            Enumerable.Range(0, n).Max(i => p[3 * i + a]) - Enumerable.Range(0, n).Min(i => p[3 * i + a])).ToArray();
        return string.Format(CultureInfo.CurrentCulture, "{0:0.0} × {1:0.0} × {2:0.0}", size[0], size[1], size[2]);
    }

    /// <summary>Character <paramref name="c"/>'s model as Reshape would write it with the sliders as they are.</summary>
    private (byte[] Vif, string? Problem) Reshape(int c)
    {
        var vif = _yak.GetChild(_entry, c).Vif;
        try
        {
            return (new ArmorModel(vif).Reshaped(ScaleX, ScaleY, ScaleZ, Inflate), null);
        }
        catch (ArgumentException ex)
        {
            return (vif, ex.Message);
        }
    }

    private void Update(bool frame = false)
    {
        if (!_ready) return;
        okButton.IsEnabled = Characters.Any() && (ScaleX != 1 || ScaleY != 1 || ScaleZ != 1 || Inflate != 0);

        // each character is reshaped once: for its size here and, if it's the one previewed, for the preview
        var shapes = new Dictionary<int, (byte[] Vif, string? Problem)>();
        var text = new StringBuilder("Size in units (left-right × front-back × height):");
        foreach (var c in Characters)
        {
            var shape = Reshape(c);
            shapes[c] = shape;
            var before = new ArmorModel(_yak.GetChild(_entry, c).Vif).GetPositions();
            var after = shape.Problem == null ? Size(new ArmorModel(shape.Vif).GetPositions()) : "too big to store";
            text.Append($"\n{ArmorYakInfo.Characters[c]}: {Size(before)}  →  {after}");
        }

        sizeText.Text = text.ToString();
        var previewed = previewBox.SelectedIndex;
        ShowPreview(frame, previewed >= 0 && shapes.TryGetValue(previewed, out var s) ? s : null);
    }

    /// <summary>Draws the previewed character's piece: reshaped (<paramref name="reshaped"/>, when already made), or as it is now.</summary>
    private void ShowPreview(bool frame, (byte[] Vif, string? Problem)? reshaped = null)
    {
        if (!_ready) return;
        var c = previewBox.SelectedIndex;
        if (c < 0) return;
        var name = ArmorYakInfo.Characters[c];
        var current = _yak.GetChild(_entry, c);
        var asItIs = originalBox.IsChecked == true;

        byte[] vif;
        string? problem = null;
        if (asItIs) vif = current.Vif;
        else (vif, problem) = reshaped ?? Reshape(c);

        Model3D? content = null;
        string? failure = null;
        try
        {
            var texture = Texture(c, current.Tex);
            var meshes = VifDecoder.Decode(new StringLogger(), vif, texture?.PixelWidth ?? 0, texture?.PixelHeight ?? 0);
            content = Conversions.CreateModel3D(meshes, texture);
        }
        catch (Exception ex)
        {
            // a preview that can't be drawn mustn't close the window; Reshape itself is unaffected
            failure = ex.Message;
        }

        previewObject.Content = content ?? new Model3DGroup(); // empty: nothing drawn
        if (frame) FramePreview();

        var state = failure != null ? $"{name}: the preview couldn't be drawn ({failure})."
            : asItIs ? $"{name}: as it is now."
            : problem != null ? $"{name}: can't be reshaped this far ({problem}). Shown as it is now."
            : $"{name}: as Reshape will write it.";
        if (_characters[c].IsChecked != true) state += $" {name} isn't ticked, so Reshape leaves it as it is.";
        previewText.Text = state;
    }

    /// <summary>Character <paramref name="c"/>'s texture, decoded the first time it's previewed.</summary>
    private BitmapSource? Texture(int c, byte[] tex)
    {
        if (_decoded[c]) return _textures[c];
        _decoded[c] = true;
        var bitmap = TexDecoder.Decode(tex);
        if (bitmap is { IsFrozen: false, CanFreeze: true }) bitmap.Freeze();
        _textures[c] = bitmap;
        return bitmap;
    }

    /// <summary>Points the camera at the piece shown, as the model view's Frame button does.</summary>
    private void FramePreview()
    {
        if (previewObject.Content is { } content && !content.Bounds.IsEmpty)
        {
            MainWindow.FrameBounds(viewport, content.Bounds);
        }
    }

    private void Frame_Click(object sender, RoutedEventArgs e) => FramePreview();

    private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
