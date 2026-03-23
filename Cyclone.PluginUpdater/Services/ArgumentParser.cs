using Cyclone.PluginUpdater.Models;

namespace Cyclone.PluginUpdater.Services;

public static class ArgumentParser
{
    public static UpdateArguments? Parse(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 将 --key value 解析为字典
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].StartsWith("--"))
                dict[args[i][2..]] = args[i + 1];
        }

        if (!dict.TryGetValue("app-name", out var appName) ||
            !dict.TryGetValue("process", out var process) ||
            !dict.TryGetValue("dir", out var dir) ||
            !dict.TryGetValue("xml-url", out var xmlUrl) ||
            !dict.TryGetValue("current-version", out var currentVersion))
        {
            return null;
        }

        if (!Version.TryParse(currentVersion, out var version))
            return null;

        return new UpdateArguments
        {
            AppName = appName,
            ProcessName = process,
            InstallDir = dir,
            XmlUrl = xmlUrl,
            CurrentVersion = version
        };
    }
}