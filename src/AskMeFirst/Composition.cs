using System.Runtime.InteropServices;
using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeFirst;

internal static partial class Composition
{
    public static CommandContext Bootstrap(bool verbose, CommandRegistry registry)
    {
        ConsoleLogger logger = new(verbose);
        string platformName = GetPlatformName();

        ServiceCollection services = new();
        services.AddSingleton<ILogger>(logger);
        services.AddSingleton(new PlatformInfo(platformName));
        RegisterPlatform(services);

        services.AddSingleton<AppConfig>(sp => LoadAndValidateConfig(sp, logger));

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
            sp.GetRequiredService<TrackingStripper>(),
            sp.GetRequiredService<AppConfig>()));
        services.AddSingleton<TrackingStripper>(sp => new TrackingStripper(sp.GetRequiredService<AppConfig>()));
        services.AddSingleton<IShortenerDomainList>(sp => new ConfigShortenerDomainList(sp.GetRequiredService<AppConfig>()));
        services.AddSingleton<IUnshortener>(sp => new HttpUnshortener(
            new SocketsHttpHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 10 },
            TimeSpan.FromSeconds(1),
            sp.GetRequiredService<ILogger>()));
        services.AddSingleton<IUnshortenTaskBuilder>(sp => new UnshortenTaskBuilder(
            sp.GetRequiredService<IUnshortener>(),
            sp.GetRequiredService<IShortenerDomainList>(),
            sp.GetRequiredService<TrackingStripper>(),
            sp.GetRequiredService<ILogger>()));
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
            sp.GetRequiredService<IPickerLauncher>(),
            usePickerAsCatchAll: true,
            sp.GetRequiredService<AppConfig>().Profiles,
            sp.GetRequiredService<IBrowserProfileDetector>(),
            sp.GetRequiredService<IUrlLauncher>(),
            sp.GetRequiredService<ILogger>(),
            sp.GetRequiredService<INotifier>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IUnshortenTaskBuilder>()));

        foreach (ICommand cmd in registry.All())
        {
            services.AddSingleton<ICommand>(cmd);
        }

        ServiceProvider provider = services.BuildServiceProvider();
        return new CommandContext(registry, provider, verbose);
    }

    private static AppConfig LoadAndValidateConfig(IServiceProvider sp, ConsoleLogger logger)
    {
        IConfigPathResolver configPathResolver = sp.GetRequiredService<IConfigPathResolver>();
        string configPath = configPathResolver.DefaultConfigPath;
        AppConfig appConfig = ConfigLoader.LoadOrDefault(configPath);
        logger.LogInfo($"config: {configPath} ({appConfig.Rules.Count} rules, {(File.Exists(configPath) ? "user" : "embedded")})");
        if (!ConfigValidator.Validate(appConfig, logger))
        {
            logger.LogWarn("Falling back to embedded default config due to validation errors.");
            appConfig = ConfigLoader.LoadDefault();
        }
        return appConfig;
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
}
