using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WorldExplorer.Views;

/// <summary>Start page: open a file, reopen a recent one, or pick the game.</summary>
public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void RecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path } || ViewModel == null) return;

        if (!File.Exists(path))
        {
            ViewModel.RemoveRecentFile(path);
            ViewModel.Notifications.Info("Removed from recent files", $"{path} no longer exists.");
            return;
        }

        ViewModel.MainWindow.OpenFile(path);
    }

    private void ClearRecent_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ClearRecentFiles();
    }
}
