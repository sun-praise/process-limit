using System.IO;
using System.Text.Json;

namespace ProcessLimit.Services;

public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProcessLimit", "rules.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<Models.ProcessRule> LoadRules()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new();
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<List<Models.ProcessRule>>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public void SaveRules(List<Models.ProcessRule> rules)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(rules, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
