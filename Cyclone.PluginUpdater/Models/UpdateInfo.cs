namespace Cyclone.PluginUpdater.Models;

public class UpdateInfo
{
    /// <summary>changelog.html 地址</summary>
    public string ChangelogUrl { get; set; } = string.Empty;

    /// <summary>ZIP 下载地址</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>远程最新版本号</summary>
    public Version Version { get; set; } = new();
}