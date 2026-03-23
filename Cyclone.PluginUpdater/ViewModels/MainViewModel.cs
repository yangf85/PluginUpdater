using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cyclone.PluginUpdater.Models;
using Cyclone.PluginUpdater.Services;

namespace Cyclone.PluginUpdater.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly UpdateChecker _checker;
    private readonly Downloader _downloader;
    private readonly Installer _installer;

    [ObservableProperty] private string _appName = string.Empty;
    private UpdateArguments? _arguments;
    [ObservableProperty] private string? _changelogHtml;
    [ObservableProperty] private string _currentVersion = string.Empty;
    [ObservableProperty] private int _downloadProgress;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isFinished;
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private string _latestVersion = string.Empty;

    // ── 绑定属性 ────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "正在检查更新…";

    private UpdateInfo? _updateInfo;

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
        if (_updateInfo is null) return;
        try
        {
            ChangelogHtml = await _downloader.DownloadChangelogAsync(_updateInfo.ChangelogUrl);
        }
        catch
        {
            ChangelogHtml = "<p>更新日志加载失败，请检查网络。</p>";
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

    // ── 初始化入口 ───────────────────────────────────────────────
    // ── 查看更新内容 ─────────────────────────────────────────────
    // ── 立即更新 ─────────────────────────────────────────────────
}