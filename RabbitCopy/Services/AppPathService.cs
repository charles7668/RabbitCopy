namespace RabbitCopy.Services;

public class AppPathService
{
    public string ConfigIdentityListFile { get; } = "config-list.json";
    public string ConfigSaveDir { get; } = "configs";

    public string HistoryFile { get; } = "history.json";
}