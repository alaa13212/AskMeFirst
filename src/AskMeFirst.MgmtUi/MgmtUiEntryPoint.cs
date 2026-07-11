namespace AskMeFirst.MgmtUi;

using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using Avalonia;

public sealed record MgmtUiEntryPointComposition(
    IConfigPersister ConfigPersister,
    IRecentPicksReader RecentPicksReader,
    IRulePreviewService RulePreview,
    IConfigFileWatcher FileWatcher,
    IBrowserDiscovery Discovery,
    IPlatformIntegration PlatformIntegration);

public static class MgmtUiEntryPoint
{
    public static int Run(
        string? configPath = null,
        IBrowserInventory? inventory = null,
        IBrowserProfileDetector? profileDetector = null,
        IDefaultBrowserRegistrar? registrar = null)
    {
        MgmtUiEntryPointComposition composition = BuildComposition(
            configPath, inventory, profileDetector, registrar);
        AppBuilder builder = AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseSkia();
        builder.StartWithClassicDesktopLifetime(Array.Empty<string>());
        composition.FileWatcher.Dispose();
        return 0;
    }

    public static MgmtUiEntryPointComposition BuildComposition(
        string? configPath = null,
        IBrowserInventory? inventory = null,
        IBrowserProfileDetector? profileDetector = null,
        IDefaultBrowserRegistrar? registrar = null)
    {
        NoOpLogger logger = new();
        string resolvedPath = !string.IsNullOrWhiteSpace(configPath) ? configPath : DefaultConfigPath();
        EnsureSeedConfig(resolvedPath);
        JsonConfigPersister persister = new(resolvedPath, logger);
        IBrowserDiscovery discovery = inventory is null
            ? NullBrowserDiscovery.Instance
            : new BrowserDiscoveryAdapter(inventory, profileDetector, logger);
        IPlatformIntegration platformIntegration = registrar is null
            ? NullPlatformIntegration.Instance
            : new PlatformIntegrationAdapter(registrar);
        return new MgmtUiEntryPointComposition(
            persister,
            new FileRecentPicksReader(resolvedPath),
            new RulePreviewService(),
            new FileConfigFileWatcher(resolvedPath),
            discovery,
            platformIntegration);
    }

    private static void EnsureSeedConfig(string configPath)
    {
        if (File.Exists(configPath))
        {
            return;
        }
        AppConfig seed = ConfigLoader.LoadDefault();
        ConfigLoader.WriteAtomic(seed, configPath);
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
