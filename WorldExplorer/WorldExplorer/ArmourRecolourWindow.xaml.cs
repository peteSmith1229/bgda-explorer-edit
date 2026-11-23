using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WorldExplorer.Themes;
using WorldExplorer.TreeView;

namespace WorldExplorer;

/// <summary>
/// Recolours an ARMOR.YAK entry's textures by rotating the hue of a range of palette colours (and scaling their
/// vividness and brightness), with a before/after preview of one character's texture. Only palettes change.
/// </summary>
public partial class ArmourRecolourWindow : Window
{
    private readonly YakFile _yak;
    private readonly int _entry;
    private readonly byte[] _palette;
    private readonly byte[] _indices;
    private readonly int _width;
    private readonly int _height;
    private readonly CheckBox[] _characters = new CheckBox[4];
    private bool _ready;

    /// <summary>For each ticked character: its new texture.</summary>
    public List<(int Child, byte[] Tex)> Results { get; } = new();

    /// <param name="previewChild">The character whose texture is previewed (and ticked).</param>
    /// <param name="allCharacters">Tick the human, elf and dwarf (the entry's menu) rather than only the preview's.</param>
    public ArmourRecolourWindow(YakFile yak, int entry, int previewChild, bool allCharacters)
    {
        _yak = yak;
        _entry = entry;
        InitializeComponent();
        ThemeManager.Attach(this);

        var texture = new ArmorTexture(yak.GetChild(entry, previewChild).Tex);
        _palette = texture.GetPalette();
        _indices = texture.GetIndices();
        _width = texture.Width;
        _height = texture.Height;

        headerText.Text = YakTreeViewModel.EntryLabel(yak, entry);
        afterCaption.Text = $"After ({ArmorYakInfo.Characters[previewChild].ToLowerInvariant()})";
        for (var c = 0; c < 4; c++)
        {
            _characters[c] = new CheckBox
            {
                Content = ArmorYakInfo.Characters[c],
                Margin = new Thickness(0, 0, 18, 0),
                IsChecked = allCharacters ? c < 3 : c == previewChild
            };
            _characters[c].Checked += (_, _) => UpdateOk();
            _characters[c].Unchecked += (_, _) => UpdateOk();
            characterPanel.Children.Add(_characters[c]);
        }

        beforeImage.Source = ArmourImages.ToOpaqueBitmap(Render(_palette), _width, _height);
        _ready = true;
        UpdatePreview();
        UpdateOk();
    }

    private byte[] ShiftedPalette(byte[] palette) => ArmorTexture.HueShift(palette, shiftSlider.Value, fromSlider.Value,
        toSlider.Value >= 360 ? 360.0001 : toSlider.Value, minSatSlider.Value, satSlider.Value, valSlider.Value);

    private byte[] Render(byte[] palette)
    {
        var rgba = new byte[_indices.Length * 4];
        for (var i = 0; i < _indices.Length; i++)
        {
            Buffer.BlockCopy(palette, 4 * _indices[i], rgba, 4 * i, 3);
            rgba[4 * i + 3] = 255;
        }

        return rgba;
    }

    private void AnyValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_ready) UpdatePreview();
    }

    private void UpdatePreview()
    {
        afterImage.Source = ArmourImages.ToOpaqueBitmap(Render(ShiftedPalette(_palette)), _width, _height);
    }

    private void UpdateOk()
    {
        if (_ready) okButton.IsEnabled = _characters.Any(c => c.IsChecked == true);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Results.Clear();
        for (var c = 0; c < 4; c++)
        {
            if (_characters[c].IsChecked != true) continue;
            var texture = new ArmorTexture(_yak.GetChild(_entry, c).Tex);
            texture.SetPalette(ShiftedPalette(texture.GetPalette()));
            Results.Add((c, texture.Data));
        }

        DialogResult = true;
    }
}
