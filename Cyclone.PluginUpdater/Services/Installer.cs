using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Cyclone.PluginUpdater.Services;

public class Installer
{
    private const int WaitIntervalMs = 500;
    private const int WaitTimeoutMs = 60_000;

    // 更新时跳过文件名包含 PluginUpdater 的 exe，避免更新器自身被占用

    /// <summary>
    /// 执行安装：逐文件备份旧目录（跳过更新器自身）→ 解压 ZIP → 成功则删除备份，失败则回滚。
    /// 使用逐文件操作而非 Directory.Move，避免更新器自身被占用导致整个目录无法移动。
    /// </summary>
    public async Task InstallAsync(string zipPath, string installDir)
    {
        var dirName = Path.GetFileName(installDir.TrimEnd(Path.DirectorySeparatorChar));
        var backupDir = Path.Combine(Path.GetTempPath(), $"{dirName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}");

        // 清理已有的旧备份
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, recursive: true);
        }

        // 逐文件备份（跳过文件名包含 PluginUpdater 的 exe）
        await Task.Run(() => CopyDirectory(installDir, backupDir));

        try
        {
            // 解压覆盖安装目录（ZIP 包里不含更新器，不会触碰更新器文件）
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, installDir, overwriteFiles: true));

            // 成功：删除备份和临时 ZIP
            Directory.Delete(backupDir, recursive: true);
            File.Delete(zipPath);
        }
        catch
        {
            // 失败：逐文件回滚（跳过文件名包含 PluginUpdater 的 exe）
            await Task.Run(() => CopyDirectory(backupDir, installDir));
            Directory.Delete(backupDir, recursive: true);
            File.Delete(zipPath);

            throw; // 继续向上抛，由 ViewModel 处理提示
        }
    }

    /// <summary>
    /// 递归复制目录，跳过文件名包含 PluginUpdater 的 exe 文件。
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(filePath);

            // 跳过文件名包含 PluginUpdater 的 exe，无论实际命名如何
            var isUpdaterExe = string.Equals(Path.GetExtension(fileName), ".exe", StringComparison.OrdinalIgnoreCase)
                               && fileName.Contains("PluginUpdater", StringComparison.OrdinalIgnoreCase);

            if (isUpdaterExe)
            {
                continue;
            }

            File.Copy(filePath, Path.Combine(destDir, fileName), overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var subDirName = Path.GetFileName(subDir);
            CopyDirectory(subDir, Path.Combine(destDir, subDirName));
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
            if (Process.GetProcessesByName(processName).Length == 0)
            {
                return true;
            }

            await Task.Delay(WaitIntervalMs, ct);
            elapsed += WaitIntervalMs;
        }
        return false;
    }
}