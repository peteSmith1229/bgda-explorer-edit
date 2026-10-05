using System.Reflection;
using System.Windows;
using WorldExplorer.Themes;

namespace WorldExplorer;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version == null ? "" : $"Version {version.ToString(3)}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
