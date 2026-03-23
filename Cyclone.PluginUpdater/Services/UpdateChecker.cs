using Cyclone.PluginUpdater.Models;
using System.IO;
using System.Net.Http;
using System.Xml.Linq;

namespace Cyclone.PluginUpdater.Services;

public class UpdateChecker
{
    private readonly HttpClient _http;

    /// <summary>
    /// 请求远程 update.xml，解析并返回 UpdateInfo。
    /// 若请求或解析失败则抛出异常，由调用方处理。
    /// </summary>
    public async Task<UpdateInfo> FetchUpdateInfoAsync(string xmlUrl)
    {
        var xml = await _http.GetStringAsync(xmlUrl);
        var doc = XDocument.Parse(xml);
        var item = doc.Root ?? throw new InvalidDataException("update.xml 格式无效：缺少根节点");

        var version = item.Element("version")?.Value
                           ?? throw new InvalidDataException("update.xml 缺少 <version> 字段");
        var url = item.Element("url")?.Value
                           ?? throw new InvalidDataException("update.xml 缺少 <url> 字段");
        var changelogUrl = item.Element("changelog-url")?.Value
                           ?? throw new InvalidDataException("update.xml 缺少 <changelog-url> 字段");

        return new UpdateInfo
        {
            Version = Version.Parse(version),
            Url = url,
            ChangelogUrl = changelogUrl
        };
    }

    /// <summary>
    /// 对比版本号，返回是否有可用更新。
    /// </summary>
    public bool HasUpdate(Version current, Version remote) => remote > current;

    public UpdateChecker(HttpClient http)
    {
        _http = http;
    }
}