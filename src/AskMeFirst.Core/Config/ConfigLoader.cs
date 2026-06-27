using System.Reflection;
using System.Text.Json;

namespace AskMeFirst.Core.Config;

public static class ConfigLoader
{
    private const string EMBEDDED_RESOURCE_NAME = "AskMeFirst.Core.Resources.DefaultConfig.jsonc";

    public static Config LoadDefault()
    {
        Assembly asm = typeof(ConfigLoader).Assembly;
        using Stream? stream = asm.GetManifestResourceStream(EMBEDDED_RESOURCE_NAME)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{EMBEDDED_RESOURCE_NAME}' not found. " +
                $"Available: {string.Join(", ", asm.GetManifestResourceNames())}");
        return Parse(stream);
    }

    public static Config LoadFromFile(string path)
    {
        using FileStream fs = File.OpenRead(path);
        return Parse(fs);
    }

    private static Config Parse(Stream stream)
    {
        Config? config = JsonSerializer.Deserialize(
            stream,
            ConfigJsonContext.Default.Config);
        return config ?? throw new InvalidOperationException("Config deserialized to null.");
    }
}
