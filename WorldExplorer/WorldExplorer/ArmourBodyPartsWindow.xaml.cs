using JetBlackEngineLib.Data.Armour;
using System.Windows;
using System.Windows.Controls;
using WorldExplorer.Themes;

namespace WorldExplorer;

/// <summary>
/// Ticks which meshes of the wearer's own model an ARMOR.YAK piece hides (its header's u64 mask). Bits past the
/// character's meshes are kept as they are.
/// </summary>
public partial class ArmourBodyPartsWindow : Window
{
    private readonly CheckBox[] _parts;
    private readonly ulong _otherBits;

    /// <summary>The mask with the ticks applied.</summary>
    public ulong Mask { get; private set; }

    public ArmourBodyPartsWindow(int child, ulong mask, string title)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        headerText.Text = title;
        Mask = mask;

        var names = ArmorYakInfo.BodyParts(child);
        _parts = new CheckBox[names.Count];
        for (var bit = 0; bit < names.Count; bit++)
        {
            _parts[bit] = new CheckBox
            {
                Content = char.ToUpperInvariant(names[bit][0]) + names[bit][1..],
                IsChecked = (mask >> bit & 1) != 0,
                Margin = new Thickness(0, 3, 12, 3)
            };
            partsPanel.Children.Add(_parts[bit]);
        }

        _otherBits = names.Count >= 64 ? 0 : mask & ~((1UL << names.Count) - 1);
        if (_otherBits != 0)
        {
            otherBitsText.Text = $"Other bits set: 0x{_otherBits:X} (kept).";
            otherBitsText.Visibility = Visibility.Visible;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var mask = _otherBits;
        for (var bit = 0; bit < _parts.Length; bit++)
        {
            if (_parts[bit].IsChecked == true) mask |= 1UL << bit;
        }

        Mask = mask;
        DialogResult = true;
    }
}
