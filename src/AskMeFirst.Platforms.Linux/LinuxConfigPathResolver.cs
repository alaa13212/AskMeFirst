using AskMeFirst.Core.Config;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxConfigPathResolver : IConfigPathResolver
{
    public string DefaultConfigPath
    {
        get
        {
            string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string root = !string.IsNullOrEmpty(xdg)
                ? xdg
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            return Path.Combine(root, "askmefirst", ConfigPath.FileName);
        }
    }
}