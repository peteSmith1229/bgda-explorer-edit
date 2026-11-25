using JetBlackEngineLib.Data.Armour;
using JetBlackEngineLib.Data.Textures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WorldExplorer.Themes;
using WorldExplorer.TreeView;

namespace WorldExplorer;

/// <summary>
/// Scales an ARMOR.YAK entry's models (each side of a pair about its own centre) and pushes them out along their
/// normals, for the ticked characters. Shows each model's size before and after.
/// </summary>
public partial class ArmourReshapeWindow : Window
{
    private readonly YakFile _yak;
    private readonly int _entry;
    private readonly CheckBox[] _characters = new CheckBox[4];
    private bool _ready;

    public double ScaleX => xSlider.Value / 100;
    public double ScaleY => ySlider.Value / 100;
    public double ScaleZ => zSlider.Value / 100;
    public double Inflate => inflateSlider.Value;

    /// <summary>The ticked characters.</summary>
    public IEnumerable<int> Characters => Enumerable.Range(0, 4).Where(c => _characters[c].IsChecked == true);

    /// <param name="child">The character to tick, or null (the entry's menu) for the human, elf and dwarf.</param>
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

        _ready = true;
        Update();
    }

    private void AnyValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_ready) Update();
    }

    private static string Size(double[] p)
    {
        var n = p.Length / 3;
        var size = Enumerable.Range(0, 3).Select(a =>
            Enumerable.Range(0, n).Max(i => p[3 * i + a]) - Enumerable.Range(0, n).Min(i => p[3 * i + a])).ToArray();
        return string.Format(CultureInfo.CurrentCulture, "{0:0.0} × {1:0.0} × {2:0.0}", size[0], size[1], size[2]);
    }

    private void Update()
    {
        okButton.IsEnabled = Characters.Any() && (ScaleX != 1 || ScaleY != 1 || ScaleZ != 1 || Inflate != 0);
        var text = new StringBuilder("Size in units (left-right × front-back × height):");
        foreach (var c in Characters)
        {
            var model = new ArmorModel(_yak.GetChild(_entry, c).Vif);
            var before = model.GetPositions();
            var after = model.ScaledPairs(ScaleX, ScaleY, ScaleZ);
            if (Inflate != 0)
            {
                var scaled = new ArmorModel(model.Data);
                try
                {
                    scaled.EditPositions(after);
                    after = scaled.Inflated(Inflate);
                }
                catch (ArgumentException)
                {
                    // too big for the header's range: OK reports it
                }
            }

            text.Append($"\n{ArmorYakInfo.Characters[c]}: {Size(before)}  →  {Size(after)}");
        }

        sizeText.Text = text.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
