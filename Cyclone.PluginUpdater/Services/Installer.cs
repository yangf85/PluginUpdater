using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Cyclone.PluginUpdater.Services;

public class Installer
{
    private const int WaitIntervalMs = 500;
    private const int WaitTimeoutMs = 60_000;

    /// <summary>
    /// 执行安装：备份旧目录 → 解压 ZIP → 成功则删除备份，失败则回滚。
    /// </summary>
    public async Task InstallAsync(string zipPath, string installDir)
    {
        var backupDir = $"{installDir.TrimEnd(Path.DirectorySeparatorChar)}_backup_{DateTime.Now:yyyyMMdd_HHmmss}";

        // 清理已有的旧备份
        if (Directory.Exists(backupDir))
            Directory.Delete(backupDir, recursive: true);

        // 备份旧目录
        Directory.Move(installDir, backupDir);

        try
        {
            Directory.CreateDirectory(installDir);
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, installDir, overwriteFiles: true));

            // 成功：删除备份和临时 ZIP
            Directory.Delete(backupDir, recursive: true);
            File.Delete(zipPath);
        }
        catch
        {
            // 失败：回滚
            if (Directory.Exists(installDir))
                Directory.Delete(installDir, recursive: true);

            Directory.Move(backupDir, installDir);
            File.Delete(zipPath);

            throw; // 继续向上抛，由 ViewModel 处理提示
        }
    }

    /// <summary>
    /// 等待宿主进程完全退出。
    /// 超时后返回 false。
    /// </summary>
    public async Task<bool> WaitForProcessExitAsync(string processName, CancellationToken ct = default)
    {
        var elapsed = 0;
        while (elapsed < WaitTimeoutMs)
        {
            if (!Process.GetProcessesByName(processName).Any())
                return true;

            await Task.Delay(WaitIntervalMs, ct);
            elapsed += WaitIntervalMs;
        }
        return false;
    }
}