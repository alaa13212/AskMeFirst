using System.Reflection;
using System.Text.Json;

namespace AskMeFirst.Core.Config;

public static class ConfigLoader
{
    private const string EmbeddedResourceName = "AskMeFirst.Core.Resources.DefaultConfig.jsonc";

    public static AppConfig LoadDefault()
    {
        Assembly asm = typeof(ConfigLoader).Assembly;
        using Stream stream = asm.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{EmbeddedResourceName}' not found. " +
                $"Available: {string.Join(", ", asm.GetManifestResourceNames())}");
        return Parse(stream);
    }

    public static AppConfig LoadFromFile(string path)
    {
        using FileStream fs = File.OpenRead(path);
        return Parse(fs);
    }

    public static AppConfig LoadOrDefault(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return LoadDefault();
        }
        return LoadFromFile(path);
    }

    public static void WriteAtomic(AppConfig config, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tempPath = path + ".tmp";
        using (FileStream fs = File.Create(tempPath))
        {
            JsonSerializer.Serialize(fs, config, ConfigJsonContext.Default.AppConfig);
        }

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    private static AppConfig Parse(Stream stream)
    {
        AppConfig? config = JsonSerializer.Deserialize(stream, ConfigJsonContext.Default.AppConfig);
        return config ?? throw new InvalidOperationException("Config deserialized to null.");
    }
}