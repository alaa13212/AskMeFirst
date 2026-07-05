using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AskMeFirst.Commands;
using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.Services;
using Microsoft.Extensions.DependencyInjection;

#if WINDOWS
using AskMeFirst.Platforms.Windows;
#endif
#if OSX
using AskMeFirst.Platforms.MacOs;
#endif
#if LINUX
using AskMeFirst.Platforms.Linux;
#endif

namespace AskMeFirst;

internal static class Composition
{
    public static CommandContext Bootstrap(bool verbose, CommandRegistry registry)
    {
        ConsoleLogger logger = new(verbose);
        string platformName = GetPlatformName();

        ServiceCollection services = new();
        services.AddSingleton<ILogger>(logger);
        services.AddSingleton(new PlatformInfo(platformName));
        RegisterPlatform(services);

        ServiceProvider tempProvider = services.BuildServiceProvider();
        IConfigPathResolver configPathResolver = tempProvider.GetRequiredService<IConfigPathResolver>();
        string configPath = configPathResolver.DefaultConfigPath;
        AppConfig appConfig = ConfigLoader.LoadOrDefault(configPath);
        logger.LogInfo($"config: {configPath} ({appConfig.Rules.Count} rules, {(File.Exists(configPath) ? "user" : "embedded")})");
        if (!ConfigValidator.Validate(appConfig, logger))
        {
            logger.LogWarn("Falling back to embedded default config due to validation errors.");
            appConfig = ConfigLoader.LoadDefault();
        }
        tempProvider.Dispose();

        services.AddSingleton(appConfig);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PredicateEvaluator>(_ => new PredicateEvaluator(RoutingDefaults.Matchers()));
        services.AddSingleton<IReadOnlyList<ITargetResolver>>(sp =>
            RoutingDefaults.Resolvers(sp.GetRequiredService<AppConfig>(), sp.GetRequiredService<PredicateEvaluator>()));
        services.AddSingleton<ProfileResolver>(sp => new ProfileResolver(
            sp.GetRequiredService<IBrowserProfileDetector>(),
            sp.GetRequiredService<AppConfig>().Profiles,
            sp.GetRequiredService<ILogger>()));
        services.AddSingleton<IRoutingExecutor>(sp => new RoutingExecutor(
            sp.GetRequiredService<IBrowserInventory>(),
            sp.GetRequiredService<ProfileResolver>(),
            new TrackingStripper(sp.GetRequiredService<AppConfig>()),
            sp.GetRequiredService<AppConfig>()));
        services.AddSingleton<IConfigWriter>(sp => new JsonConfigWriter(
            sp.GetRequiredService<IConfigPathResolver>().DefaultConfigPath,
            sp.GetRequiredService<ILogger>()));
        services.AddSingleton<IRecentPicksLog>(sp => new FileRecentPicksLog(
            sp.GetRequiredService<IConfigPathResolver>().DefaultConfigPath,
            sp.GetRequiredService<ILogger>()));
        services.AddSingleton<IPickerLauncher>(sp => new AvaloniaPickerLauncher(
            sp.GetRequiredService<ILogger>(),
            sp.GetRequiredService<IConfigWriter>(),
            sp.GetRequiredService<IIconProvider>(),
            sp.GetRequiredService<IRecentPicksLog>()));
        services.AddSingleton<RuleRouter>(sp => new RuleRouter(
            sp.GetRequiredService<IReadOnlyList<ITargetResolver>>(),
            sp.GetRequiredService<IRoutingExecutor>(),
            sp.GetRequiredService<IBrowserInventory>(),
            sp.GetRequiredService<ISourceAppDetector>(),
            sp.GetRequiredService<IPickerLauncher>(),
            usePickerAsCatchAll: true,
            sp.GetRequiredService<AppConfig>().Profiles,
            sp.GetRequiredService<IBrowserProfileDetector>(),
            sp.GetRequiredService<IUrlLauncher>(),
            sp.GetRequiredService<ILogger>(),
            sp.GetRequiredService<INotifier>(),
            sp.GetRequiredService<TimeProvider>()));

        foreach (ICommand cmd in registry.All())
        {
            services.AddSingleton<ICommand>(cmd);
        }

        ServiceProvider provider = services.BuildServiceProvider();
        IReadOnlyList<ICommand> commands = provider.GetServices<ICommand>().ToList();
        return new CommandContext(registry, provider, commands, verbose);
    }

    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }
        if (OperatingSystem.IsMacOS())
        {
            return "macos";
        }
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return "linux";
        }
        throw new PlatformNotSupportedException(
            $"askmefirst has no platform integration for {RuntimeInformation.OSDescription}.");
    }

    private static void RegisterPlatform(IServiceCollection services)
    {
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            AddWindows(services);
            return;
        }
#endif
#if OSX
        if (OperatingSystem.IsMacOS())
        {
            AddMacOs(services);
            return;
        }
#endif
#if LINUX
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            AddLinux(services);
            return;
        }
#endif
        throw new PlatformNotSupportedException(
            $"askmefirst has no platform integration for {RuntimeInformation.OSDescription}.");
    }

#if WINDOWS
    [SupportedOSPlatform("windows")]
    private static void AddWindows(IServiceCollection services)
    {
        services.AddSingleton<IBrowserInventory, WindowsBrowserInventory>();
        services.AddSingleton<IUrlLauncher, WindowsUrlLauncher>();
        services.AddSingleton<IBrowserProfileDetector, WindowsBrowserProfileDetector>();
        services.AddSingleton<IProcessNameNormalizer, WindowsProcessNameNormalizer>();
        services.AddSingleton<ISourceAppDetector, WindowsSourceAppDetector>();
        services.AddSingleton<IConfigPathResolver, WindowsConfigPathResolver>();
        services.AddSingleton<IIconProvider, WindowsIconProvider>();
        services.AddSingleton<INotifier, WindowsNotifier>();
        services.AddSingleton<IDefaultBrowserRegistrar, WindowsDefaultBrowserRegistrar>();
        services.AddSingleton<ISourceAppWindowLocator, WindowsSourceAppWindowLocator>();
    }
#endif

#if OSX
    [SupportedOSPlatform("osx")]
    private static void AddMacOs(IServiceCollection services)
    {
        services.AddSingleton<IBrowserInventory, MacOsBrowserInventory>();
        services.AddSingleton<IUrlLauncher, MacOsUrlLauncher>();
        services.AddSingleton<IBrowserProfileDetector, MacOsBrowserProfileDetector>();
        services.AddSingleton<IProcessNameNormalizer, MacOsProcessNameNormalizer>();
        services.AddSingleton<ISourceAppDetector, MacOsSourceAppDetector>();
        services.AddSingleton<IConfigPathResolver, MacOsConfigPathResolver>();
        services.AddSingleton<IIconProvider, MacIconProvider>();
        services.AddSingleton<INotifier, MacNotifier>();
        services.AddSingleton<IDefaultBrowserRegistrar, MacOsDefaultBrowserRegistrar>();
        services.AddSingleton<ISourceAppWindowLocator, MacSourceAppWindowLocator>();
    }
#endif

#if LINUX
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    private static void AddLinux(IServiceCollection services)
    {
        services.AddSingleton<IBrowserInventory, LinuxBrowserInventory>();
        services.AddSingleton<IUrlLauncher, LinuxUrlLauncher>();
        services.AddSingleton<IBrowserProfileDetector, LinuxBrowserProfileDetector>();
        services.AddSingleton<IProcessNameNormalizer, LinuxProcessNameNormalizer>();
        services.AddSingleton<ISourceAppDetector, LinuxSourceAppDetector>();
        services.AddSingleton<IConfigPathResolver, LinuxConfigPathResolver>();
        services.AddSingleton<IIconProvider, LinuxIconProvider>();
        services.AddSingleton<INotifier, LinuxNotifier>();
        services.AddSingleton<IDefaultBrowserRegistrar, LinuxDefaultBrowserRegistrar>();
        services.AddSingleton<ISourceAppWindowLocator, NullSourceAppWindowLocator>();
    }
#endif
}