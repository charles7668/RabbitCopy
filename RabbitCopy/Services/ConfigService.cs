using System.IO;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using RabbitCopy.Models;

namespace RabbitCopy.Services;

public class ConfigService(AppPathService appPathService)
{
    [UsedImplicitly]
    public bool IsConfigExist(string name)
    {
        List<ConfigIdentity> list = LoadConfigIdentityList();
        return list.Exists(x => x.Name == name);
    }

    [UsedImplicitly]
    public Config LoadConfig(ConfigIdentity identity)
    {
        var filePath = Path.Join(appPathService.ConfigSaveDir, identity.Guid + ".json");
        using var sr = new StreamReader(filePath, Encoding.UTF8);
        var jsonText = sr.ReadToEnd();
        var config = JsonSerializer.Deserialize<Config>(jsonText);
        return config ?? throw new JsonException();
    }

    [UsedImplicitly]
    public List<ConfigIdentity> LoadConfigIdentityList()
    {
        try
        {
            using var sr = new StreamReader(appPathService.ConfigIdentityListFile, Encoding.UTF8);
            var jsonContent = sr.ReadToEnd();
            var configList = JsonSerializer.Deserialize<List<ConfigIdentity>>(jsonContent);
            return configList ?? [];
        }
        catch
        {
            return [];
        }
    }

    [UsedImplicitly]
    public void SaveNewConfig(string name, Config config)
    {
        ConfigIdentity identity = new()
        {
            Name = name,
            Guid = Guid.NewGuid().ToString()
        };
        List<ConfigIdentity> original = LoadConfigIdentityList();
        original.Add(identity);
        using (var fs = new FileStream(appPathService.ConfigIdentityListFile, FileMode.Create,
                   FileAccess.Write, FileShare.None))
        {
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                var json = JsonSerializer.Serialize(original);
                sw.Write(json);
            }
        }

        Directory.CreateDirectory(appPathService.ConfigSaveDir);
        var configPath = Path.Join(appPathService.ConfigSaveDir, identity.Guid + ".json");
        using var configStream = new FileStream(configPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(configStream, Encoding.UTF8);
        var configJson = JsonSerializer.Serialize(config);
        writer.Write(configJson);
    }

    [UsedImplicitly]
    public void RemoveConfig(ConfigIdentity identity)
    {
        List<ConfigIdentity> identities = LoadConfigIdentityList();
        var item = identities.FirstOrDefault(i => i == identity);
        if (item == null) return;
        identities.Remove(item);
        using var fs = new FileStream(appPathService.ConfigIdentityListFile, FileMode.Create,
            FileAccess.Write, FileShare.None);
        using var sw = new StreamWriter(fs, Encoding.UTF8);
        var json = JsonSerializer.Serialize(identities);
        sw.Write(json);

        var configPath = Path.Join(appPathService.ConfigSaveDir, identity.Guid + ".json");
        if (File.Exists(configPath))
            File.Delete(configPath);
    }
}