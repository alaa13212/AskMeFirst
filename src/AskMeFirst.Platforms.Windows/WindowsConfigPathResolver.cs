using AskMeFirst.Core.Config;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsConfigPathResolver : IConfigPathResolver
{
    public string DefaultConfigPath
    {
        get
        {
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create);
            return ConfigPath.Combine(appData);
        }
    }
}