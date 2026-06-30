using System.Text.Json;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Config;

public sealed class JsonConfigWriter(string configPath, ILogger logger) : IConfigWriter
{
    public void AppendRule(Rule rule)
    {
        AppConfig config = ConfigLoader.LoadOrDefault(configPath);

        List<Rule> updated = [.. config.Rules, rule];
        AppConfig next = config with { Rules = updated };

        WriteAtomic(next);
        logger.LogInfo($"Appended rule '{rule.Name}' (priority {rule.Priority}, origin {rule.Origin}).");
    }

    private void WriteAtomic(AppConfig config)
    {
        string? dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tempPath = configPath + ".tmp";
        using (FileStream fs = File.Create(tempPath))
        {
            JsonSerializer.Serialize(fs, config, ConfigJsonContext.Default.AppConfig);
        }

        if (File.Exists(configPath))
        {
            File.Replace(tempPath, configPath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tempPath, configPath);
        }
    }
}