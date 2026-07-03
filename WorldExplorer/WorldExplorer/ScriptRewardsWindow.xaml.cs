using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JetBlackEngineLib.Data.Scripting;

namespace WorldExplorer;

public partial class ScriptRewardsWindow : Window
{
    private sealed class Row
    {
        public required ScriptRewardCall Call { get; init; }
        public string FunctionName => Call.FunctionName;
        public string ExternalName => Call.ExternalName;
        public string Current => Call.ExternalName == "givePlayerItem"
            ? Call.ItemName : Call.IntValue.ToString();
        public string NewValue { get; set; } = "";
    }

    private readonly byte[] _scrBytes;
    private readonly List<Row> _rows;

    /// <summary>True after Apply if any edit was written into the buffer.</summary>
    public bool Modified { get; private set; }

    /// <param name="scrBytes">The raw .scr entry bytes. Edited IN PLACE on Apply.</param>
    public ScriptRewardsWindow(byte[] scrBytes)
    {
        InitializeComponent();
        _scrBytes = scrBytes;
        _rows = ScriptRewardScanner.Scan(scrBytes).Select(c => new Row { Call = c }).ToList();
        grid.ItemsSource = _rows;
        if (_rows.Count == 0)
        {
            MessageBox.Show(this, "No reward calls (givePlayerGold / givePlayerExp / givePlayerItem) found in this script.",
                "Script Rewards", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        grid.CommitEdit();                     // flush any in-progress cell edit
        var errors = new List<string>();
        foreach (var row in _rows)
        {
            var txt = row.NewValue?.Trim();
            if (string.IsNullOrEmpty(txt) || txt == row.Current) continue;

            try
            {
                if (row.Call.ExternalName == "givePlayerItem")
                    ScriptRewardScanner.ApplyItemName(_scrBytes, row.Call, txt);
                else if (int.TryParse(txt, out var v))
                    ScriptRewardScanner.ApplyIntValue(_scrBytes, row.Call, v);
                else
                { errors.Add($"{row.FunctionName}/{row.ExternalName}: '{txt}' is not a number"); continue; }

                Modified = true;
            }
            catch (ArgumentException ex) { errors.Add($"{row.FunctionName}: {ex.Message}"); }
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join("\n", errors), "Some edits were not applied",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;                            // keep the window open to fix them
        }
        DialogResult = true;
        Close();
    }
}