namespace Cyclone.PluginUpdater.Models;

public class UpdateArguments
{
    /// <summary>界面展示的插件名称</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>当前已安装的版本号</summary>
    public Version CurrentVersion { get; set; } = new();

    /// <summary>插件安装目录</summary>
    public string InstallDir { get; set; } = string.Empty;

    /// <summary>需要等待退出的宿主进程名（不含 .exe）</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>update.xml 的远程地址</summary>
    public string XmlUrl { get; set; } = string.Empty;
}