using System.Windows;
using System.Windows.Controls;

namespace Cyclone.PluginUpdater.Views;

public partial class ChangelogWindow : Window
{
    private readonly string _html;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var browser = (WebBrowser)FindName("WebBrowserControl");
        browser.NavigateToString(_html);
    }

    public ChangelogWindow(string html)
    {
        _html = html;
        InitializeComponent();
        Loaded += OnLoaded;
    }
}