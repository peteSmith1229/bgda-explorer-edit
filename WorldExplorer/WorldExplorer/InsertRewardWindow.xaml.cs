using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using JetBlackEngineLib.Data.Scripting;
using System.Windows.Controls;
using WorldExplorer.Themes;

namespace WorldExplorer;

public partial class InsertRewardWindow : Window
{
    public sealed class RewardRow
    {
        public string[] CallOptions { get; } = { "givePlayerGold", "givePlayerExp", "givePlayerItem" };
        public string ExternalName { get; set; } = "givePlayerGold";
        public string Value { get; set; } = "";
    }

    private byte[] _scrBytes;
    private readonly ObservableCollection<RewardRow> _rows = new();
    public bool Modified { get; private set; }
    public byte[] ResultBytes => _scrBytes;

    public InsertRewardWindow(byte[] scrBytes)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        _scrBytes = scrBytes;

        var sites = ScriptCallSiteScanner.Scan(scrBytes).Where(s => s.Insertable).ToList();
        anchorCombo.ItemsSource = sites;
        if (sites.Count > 0)
        {
            anchorCombo.SelectedIndex = 0;
        }
        else
        {
            anchorCombo.IsEnabled = false;
            insertButton.IsEnabled = false;
            noAnchorsText.Visibility = Visibility.Visible;
        }

        _rows.Add(new RewardRow());
        rewardGrid.ItemsSource = _rows;
    }

    private void AddReward_Click(object sender, RoutedEventArgs e)
    {
        rewardGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var row = new RewardRow();
        _rows.Add(row);
        rewardGrid.SelectedItem = row;
        rewardGrid.ScrollIntoView(row);
    }

    private void Insert_Click(object sender, RoutedEventArgs e)
    {
        rewardGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        rewardGrid.CommitEdit(DataGridEditingUnit.Row, true);

        if (anchorCombo.SelectedItem is not ScriptCallSite anchor)
        { MessageBox.Show(this, "Choose the call to anchor the reward to.", Title, MessageBoxButton.OK, MessageBoxImage.Information); return; }

        var newCalls = new List<NewRewardCall>();
        foreach (var row in _rows)
        {
            var v = row.Value?.Trim();
            if (string.IsNullOrEmpty(v)) continue;
            var nc = new NewRewardCall { ExternalName = row.ExternalName };
            if (row.ExternalName == "givePlayerItem") nc.ItemName = v;
            else if (int.TryParse(v, out var n)) nc.IntValue = n;
            else { MessageBox.Show(this, $"'{v}' is not a number for {row.ExternalName}.", Title, MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            newCalls.Add(nc);
        }
        if (newCalls.Count == 0) { MessageBox.Show(this, "Enter an amount or item name for at least one reward.", Title, MessageBoxButton.OK, MessageBoxImage.Information); return; }

        try
        {
            _scrBytes = ScriptDetourPatcher.InsertRewardsBeforeCall(_scrBytes, anchor, newCalls);
            Modified = true;
            DialogResult = true;
            Close();
        }
        catch (ArgumentException ex)
        { MessageBox.Show(this, ex.Message, "Insert failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}