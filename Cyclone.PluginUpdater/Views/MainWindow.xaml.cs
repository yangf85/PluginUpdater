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

        // 当 ViewModel 请求打开 changelog 时，弹出子窗口
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ChangelogHtml)
                && !string.IsNullOrEmpty(viewModel.ChangelogHtml))
            {
                var win = new ChangelogWindow(viewModel.ChangelogHtml);
                win.Owner = this;
                win.ShowDialog();
            }
        };
    }
}