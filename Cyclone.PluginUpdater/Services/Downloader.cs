using System.IO;
using System.Net.Http;

namespace Cyclone.PluginUpdater.Services;

public class Downloader
{
    private readonly HttpClient _http;

    /// <summary>
    /// 下载文件到临时目录，通过 progress 回调报告进度（0~100）。
    /// 返回下载后的本地临时文件路径。
    /// </summary>
    public async Task<string> DownloadAsync(string url, IProgress<int> progress, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"cyclone_update_{Guid.NewGuid():N}.zip");

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long downloadedBytes = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            downloadedBytes += read;

            if (totalBytes > 0)
                progress.Report((int)(downloadedBytes * 100 / totalBytes));
        }

        return tempPath;
    }

    /// <summary>
    /// 下载 changelog.html 并返回 HTML 字符串。
    /// </summary>
    public async Task<string> DownloadChangelogAsync(string changelogUrl, CancellationToken ct = default)
    {
        return await _http.GetStringAsync(changelogUrl, ct);
    }

    public Downloader(HttpClient http)
    {
        _http = http;
    }
}