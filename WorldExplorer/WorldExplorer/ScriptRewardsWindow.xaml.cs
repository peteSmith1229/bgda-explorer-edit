using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JetBlackEngineLib.Data.Scripting;
using System.Windows.Controls;
using WorldExplorer.Themes;

namespace WorldExplorer;

public partial class ScriptRewardsWindow : Window
{
    public sealed class ScriptRewardRow
    {
        public required ScriptRewardCall Call { get; init; }
        public string FunctionName => Call.FunctionName;
        public bool StructuralSafe => Call.StructuralSafe;
        public string[] CallOptions { get; } = { "givePlayerGold", "givePlayerExp", "givePlayerItem" };
        public string SelectedCall { get; set; } = "";

        public string Current => Call.ExternalName == "givePlayerItem"
            ? Call.ItemName
            : Call.IntValue.ToString();

        public string NewValue { get; set; } = "";
        public bool Remove { get; set; }
    }

    private byte[] _scrBytes; // NOTE: no longer readonly — detours regrow it
    private readonly List<ScriptRewardRow> _rows;

    public bool Modified { get; private set; }

    /// <summary>Final script bytes after Apply (may be longer than the input).</summary>
    public byte[] ResultBytes => _scrBytes;

    public ScriptRewardsWindow(byte[] scrBytes)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _scrBytes = scrBytes;
        _rows = ScriptRewardScanner.Scan(scrBytes)
            .Select(c => new ScriptRewardRow { Call = c, SelectedCall = c.ExternalName })
            .ToList();
        grid.ItemsSource = _rows;
        if (_rows.Count == 0)
        {
            emptyText.Visibility = Visibility.Visible;
            applyButton.IsEnabled = false;
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);
        var errors = new List<string>();

        foreach (var row in _rows)
        {
            var txt = row.NewValue?.Trim() ?? "";
            var shapeChanged = row.SelectedCall != row.Call.ExternalName &&
                               (row.SelectedCall == "givePlayerItem" ||
                                row.Call.ExternalName == "givePlayerItem");
            try
            {
                if (row.Remove)
                {
                    ScriptDetourPatcher.RemoveCall(_scrBytes, row.Call);
                    Modified = true;
                }
                else if (shapeChanged)
                {
                    if (string.IsNullOrEmpty(txt))
                    {
                        errors.Add($"{row.FunctionName}: converting to/from an item needs a New value");
                        continue;
                    }

                    var nc = new NewRewardCall { ExternalName = row.SelectedCall };
                    if (row.SelectedCall == "givePlayerItem") nc.ItemName = txt;
                    else if (int.TryParse(txt, out var v)) nc.IntValue = v;
                    else
                    {
                        errors.Add($"{row.FunctionName}: '{txt}' is not a number");
                        continue;
                    }

                    _scrBytes = ScriptDetourPatcher.DetourReplaceCalls(
                        _scrBytes, row.Call, new[] { nc }, out var shift);
                    foreach (var other in _rows) // strings moved by 'shift'
                        if (other.Call.StringScrOffset >= 0)
                            other.Call.StringScrOffset += shift;
                    Modified = true;
                }
                else
                {
                    if (row.SelectedCall != row.Call.ExternalName) // gold <-> exp
                    {
                        ScriptDetourPatcher.SwapSameShape(_scrBytes, row.Call, row.SelectedCall);
                        row.Call.ExternalName = row.SelectedCall;
                        Modified = true;
                    }

                    if (!string.IsNullOrEmpty(txt) && txt != row.Current)
                    {
                        if (row.Call.ExternalName == "givePlayerItem")
                            ScriptRewardScanner.ApplyItemName(_scrBytes, row.Call, txt);
                        else if (int.TryParse(txt, out var v))
                            ScriptRewardScanner.ApplyIntValue(_scrBytes, row.Call, v);
                        else
                        {
                            errors.Add($"{row.FunctionName}: '{txt}' is not a number");
                            continue;
                        }

                        Modified = true;
                    }
                }
            }
            catch (ArgumentException ex) { errors.Add($"{row.FunctionName}: {ex.Message}"); }
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join("\n", errors), "Some edits were not applied",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}