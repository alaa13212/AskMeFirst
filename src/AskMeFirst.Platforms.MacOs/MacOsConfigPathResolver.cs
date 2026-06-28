using AskMeFirst.Core.Config;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsConfigPathResolver : IConfigPathResolver
{
    public string DefaultConfigPath
    {
        get
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return ConfigPath.Combine(Path.Combine(home, "Library", "Application Support"));
        }
    }
}