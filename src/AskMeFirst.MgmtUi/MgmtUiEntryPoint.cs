namespace AskMeFirst.MgmtUi;

using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using Avalonia;

public sealed record MgmtUiEntryPointComposition(
    IConfigPersister ConfigPersister,
    IRecentPicksReader RecentPicksReader,
    IRulePreviewService RulePreview);

public static class MgmtUiEntryPoint
{
    public static int Run(string? configPath = null)
    {
        MgmtUiEntryPointComposition composition = BuildComposition(configPath);
        AppBuilder builder = AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseSkia();
        builder.StartWithClassicDesktopLifetime(Array.Empty<string>());
        return 0;
    }

    public static MgmtUiEntryPointComposition BuildComposition(string? configPath = null)
    {
        NoOpLogger logger = new();
        string resolvedPath = !string.IsNullOrWhiteSpace(configPath) ? configPath : DefaultConfigPath();
        JsonConfigPersister persister = new(resolvedPath, logger);
        return new MgmtUiEntryPointComposition(
            persister,
            new FileRecentPicksReader(resolvedPath),
            new RulePreviewService());
    }

    private static string DefaultConfigPath()
    {
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, ".config", "askmefirst", ConfigPath.FileName);
    }

    private sealed class NoOpLogger : ILogger
    {
        public void LogInfo(string message)
        {
        }

        public void LogWarn(string message)
        {
        }

        public void LogError(string message)
        {
        }
    }
}
