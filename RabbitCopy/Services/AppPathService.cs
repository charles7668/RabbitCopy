using System.IO;
using System.Reflection;

namespace RabbitCopy.Services;

public class AppPathService
{
    public AppPathService()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        ConfigIdentityListFile = Path.Combine(assemblyPath, "config-list.json");
        ConfigSaveDir = Path.Combine(assemblyPath, "configs");
        HistoryFile = Path.Combine(assemblyPath, "history.json");
    }

    public string ConfigIdentityListFile { get; }

    public string ConfigSaveDir { get; }

    public string HistoryFile { get; }
}