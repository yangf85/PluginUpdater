using System.Windows;
using Cyclone.PluginUpdater.ViewModels;

namespace Cyclone.PluginUpdater.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }
}