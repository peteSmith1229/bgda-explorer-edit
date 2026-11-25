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
/// vividness and brightness), with a before/after preview of one character's texture. Only palettes change, except
/// where a piece is partly the character's own body (the elf's clothes): its skin, face and hair keep their colours
/// (<see cref="ArmorTexture.Recolour"/>).
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
    private readonly int _previewChild;
    private readonly ArmorOwnParts?[] _own;
    private bool _ready;

    /// <summary>For each ticked character: its new texture.</summary>
    public List<(int Child, byte[] Tex)> Results { get; } = new();

    /// <summary>What to tell about the results (colours that had to share palette entries).</summary>
    public List<string> Notes { get; } = new();

    /// <param name="previewChild">The character whose texture is previewed (and ticked).</param>
    /// <param name="allCharacters">Tick the human, elf and dwarf (the entry's menu) rather than only the preview's.</param>
    /// <param name="dataFolder">ARMOR.YAK's folder, where the characters' archives (HUMAN.LMP ...) are looked for.</param>
    public ArmourRecolourWindow(YakFile yak, int entry, int previewChild, bool allCharacters, string? dataFolder)
    {
        _yak = yak;
        _entry = entry;
        _previewChild = previewChild;
        _own = ArmourScene.OwnParts(yak, entry, dataFolder);
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

        if (ArmourScene.OwnPartsText(_own) is { } ownText)
        {
            keepText.Text = ownText;
            keepBox.Visibility = Visibility.Visible;
            keepText.Visibility = Visibility.Visible;
            keepBox.Checked += (_, _) => UpdatePreview();
            keepBox.Unchecked += (_, _) => UpdatePreview();
        }

        beforeImage.Source = ArmourImages.ToOpaqueBitmap(Render(_palette, null), _width, _height);
        _ready = true;
        UpdatePreview();
        UpdateOk();
    }

    private byte[] ShiftedPalette(byte[] palette) => ArmorTexture.HueShift(palette, shiftSlider.Value, fromSlider.Value,
        toSlider.Value >= 360 ? 360.0001 : toSlider.Value, minSatSlider.Value, satSlider.Value, valSlider.Value);

    /// <summary>The preview's texture with <paramref name="palette"/>, its kept texels (if any) in the old colours.</summary>
    private byte[] Render(byte[] palette, bool[]? keep)
    {
        var rgba = new byte[_indices.Length * 4];
        for (var i = 0; i < _indices.Length; i++)
        {
            Buffer.BlockCopy(keep?[i] == true ? _palette : palette, 4 * _indices[i], rgba, 4 * i, 3);
            rgba[4 * i + 3] = 255;
        }

        return rgba;
    }

    /// <summary>Character <paramref name="child"/>'s own parts to keep, or null (none, or the box unticked).</summary>
    private ArmorOwnParts? Kept(int child) => keepBox.IsChecked == true && _own[child] is { Any: true } own ? own : null;

    private void AnyValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_ready) UpdatePreview();
    }

    private void UpdatePreview()
    {
        afterImage.Source = ArmourImages.ToOpaqueBitmap(Render(ShiftedPalette(_palette), Kept(_previewChild)?.Texels),
            _width, _height);
    }

    private void UpdateOk()
    {
        if (_ready) okButton.IsEnabled = _characters.Any(c => c.IsChecked == true);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Results.Clear();
        Notes.Clear();
        for (var c = 0; c < 4; c++)
        {
            if (_characters[c].IsChecked != true) continue;
            var texture = new ArmorTexture(_yak.GetChild(_entry, c).Tex);
            var own = Kept(c);
            var (merged, _, requantized) = texture.Recolour(ShiftedPalette, own?.Texels, own?.Others);
            if (requantized)
            {
                Notes.Add($"{ArmorYakInfo.Characters[c]}: its own colours stayed exact; the palette was too full to copy " +
                          $"every colour they share with the clothes, so {merged} of the clothes' new colours were merged " +
                          "with near ones.");
            }

            Results.Add((c, texture.Data));
        }

        DialogResult = true;
    }
}
