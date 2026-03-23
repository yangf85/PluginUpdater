using Cyclone.PluginUpdater.Services;
using Cyclone.PluginUpdater.ViewModels;
using Cyclone.PluginUpdater.Views;
using System.Net.Http;
using System.Windows;

namespace Cyclone.PluginUpdater;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 解析命令行参数
        var arguments = ArgumentParser.Parse(e.Args);
        if (arguments is null)
        {
            MessageBox.Show("参数无效，请通过插件调用此程序。", "Cyclone.PluginUpdater", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // 组装依赖
        var http = new HttpClient();
        var checker = new UpdateChecker(http);
        var downloader = new Downloader(http);
        var installer = new Installer();
        var viewModel = new MainViewModel(checker, downloader, installer);

        // 启动主窗口
        var window = new MainWindow(viewModel);
        window.Show();

        // 开始检查更新
        await viewModel.InitializeAsync(arguments);
    }
}