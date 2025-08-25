using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JetBrains.Annotations;

namespace RabbitCopy.Services;

public class HistoryService
{
    public HistoryService(AppPathService appPathService)
    {
        _appPathService = appPathService;
    }

    private readonly AppPathService _appPathService;

    [UsedImplicitly]
    public void LoadHistory(out List<string> srcHistories, out List<string> dstHistories)
    {
        var historyFile = _appPathService.HistoryFile;
        srcHistories = [];
        dstHistories = [];
        try
        {
            using var sr = new StreamReader(historyFile, Encoding.UTF8);
            var jsonText = sr.ReadToEnd();
            using var jd = JsonDocument.Parse(jsonText);
            var rootElement = jd.RootElement;
            if (rootElement.TryGetProperty("src-his", out var src) && src.GetArrayLength() > 0)
            {
                foreach (var s in src.EnumerateArray())
                {
                    var temp = s.GetString();
                    if (temp is not null)
                        srcHistories.Add(temp);
                }
            }

            if (rootElement.TryGetProperty("dst-his", out var dst) && dst.GetArrayLength() > 0)
            {
                foreach (var s in dst.EnumerateArray())
                {
                    var temp = s.GetString();
                    if (temp is not null)
                        dstHistories.Add(temp);
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    [UsedImplicitly]
    public void UpdateHistory(IEnumerable<string> updateSrc, IEnumerable<string> updateDst)
    {
        var historyFile = _appPathService.HistoryFile;

        LoadHistory(out var srcHistories, out var dstHistories);

        try
        {
            srcHistories.InsertRange(0, updateSrc);
            dstHistories.InsertRange(0, updateDst);
            srcHistories = srcHistories.Distinct().Take(20).ToList();
            dstHistories = dstHistories.Distinct().Take(20).ToList();

            using var fs = new FileStream(historyFile, FileMode.Create, FileAccess.Write, FileShare.None);
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            JsonObject jo = new()
            {
                ["src-his"] = new JsonArray(srcHistories.Select(s => JsonValue.Create(s)).ToArray<JsonNode?>()),
                ["dst-his"] = new JsonArray(dstHistories.Select(s => JsonValue.Create(s)).ToArray<JsonNode?>())
            };
            sw.WriteLine(jo.ToJsonString());
        }
        catch
        {
            // ignore
        }
    }
}