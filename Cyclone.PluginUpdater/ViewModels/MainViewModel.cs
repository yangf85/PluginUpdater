using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cyclone.PluginUpdater.Models;
using Cyclone.PluginUpdater.Services;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Windows;

namespace Cyclone.PluginUpdater.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly UpdateChecker _checker;
    private readonly Downloader _downloader;
    private readonly Installer _installer;

    [ObservableProperty]
    private string _appName = string.Empty;

    private UpdateArguments? _arguments;

    // ── 绑定属性 ────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "正在检查更新…";

    private UpdateInfo? _updateInfo;

    [ObservableProperty]
    public partial string CurrentVersion { get; set; }

    [ObservableProperty]
    public partial int DownloadProgress { get; set; }

    [ObservableProperty]
    public partial bool IsDownloading { get; set; }

    [ObservableProperty]
    public partial bool IsFinished { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    [ObservableProperty]
    public partial string LatestVersion { get; set; }

    [RelayCommand]
    private async Task StartUpdateAsync()
    {
        if (_updateInfo is null || _arguments is null) return;

        IsDownloading = true;
        StatusMessage = "正在等待宿主软件退出…";

        // 等待宿主进程退出
        var exited = await _installer.WaitForProcessExitAsync(_arguments.ProcessName);
        if (!exited)
        {
            StatusMessage = "等待软件退出超时，请手动关闭后重试";
            IsDownloading = false;
            return;
        }

        // 下载 ZIP
        StatusMessage = "正在下载更新包…";
        string zipPath;
        try
        {
            var progress = new Progress<int>(p => DownloadProgress = p);
            zipPath = await _downloader.DownloadAsync(_updateInfo.Url, progress);
        }
        catch (Exception ex)
        {
            StatusMessage = $"下载失败：{ex.Message}";
            IsDownloading = false;
            return;
        }

        // 安装
        StatusMessage = "正在安装…";
        try
        {
            await _installer.InstallAsync(zipPath, _arguments.InstallDir);
            StatusMessage = "更新完成！";
            IsFinished = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"安装失败，已自动回滚：{ex.Message}";
        }

        IsDownloading = false;
    }

    [RelayCommand]
    private async Task ViewChangelogAsync()
    {
        if (string.IsNullOrEmpty(_updateInfo?.ChangelogUrl)) return;

        try
        {
            // 1. 下载 HTML 内容
            // 确保这里的 URL 是 Raw 格式，例如: https://gitee.com/.../raw/master/changelog.html
            string htmlContent = await _downloader.DownloadChangelogAsync(_updateInfo.ChangelogUrl);

            if (string.IsNullOrWhiteSpace(htmlContent)) return;

            // 2. 获取程序运行目录并拼凑文件名
            // AppDomain.CurrentDomain.BaseDirectory 是获取程序根目录最稳妥的方法
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(exePath, "change.html");

            // 3. 写入文件（UTF8 编码，防止中文乱码）
            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);

            // 4. 用默认浏览器打开
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("无法在程序目录创建文件，请尝试以管理员身份运行，或检查文件夹权限。", "权限错误");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"显示更新日志失败：{ex.Message}", "错误");
        }
    }

    public async Task InitializeAsync(UpdateArguments arguments)
    {
        _arguments = arguments;
        AppName = arguments.AppName;
        CurrentVersion = arguments.CurrentVersion.ToString(3);

        try
        {
            _updateInfo = await _checker.FetchUpdateInfoAsync(arguments.XmlUrl);
            LatestVersion = _updateInfo.Version.ToString(3);

            if (_checker.HasUpdate(arguments.CurrentVersion, _updateInfo.Version))
            {
                IsUpdateAvailable = true;
                StatusMessage = $"发现新版本 {LatestVersion}";
            }
            else
            {
                StatusMessage = "已是最新版本";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"无法连接服务器，请检查网络（{ex.Message}）";
        }
    }

    public MainViewModel(UpdateChecker checker, Downloader downloader, Installer installer)
    {
        _checker = checker;
        _downloader = downloader;
        _installer = installer;
    }
}