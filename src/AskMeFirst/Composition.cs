using System.Runtime.InteropServices;
using AskMeFirst.Commands;
using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.Services;
using AskMeFirst.Platforms.Linux;
using AskMeFirst.Platforms.MacOs;
using AskMeFirst.Platforms.Windows;

namespace AskMeFirst;

internal static class Composition
{
    public static CommandContext Bootstrap(bool verbose, CommandRegistry registry)
    {
        BootstrapContext ctx = SelectPlatform();

        ConsoleLogger logger = new(verbose);
        string configPath = ctx.ConfigPath.DefaultConfigPath;
        AppConfig appConfig = ConfigLoader.LoadOrDefault(configPath);
        logger.LogInfo($"config: {configPath} ({appConfig.Rules.Count} rules, {(File.Exists(configPath) ? "user" : "embedded")})");

        if (!ConfigValidator.Validate(appConfig, logger))
        {
            logger.LogWarn("Falling back to embedded default config due to validation errors.");
            appConfig = ConfigLoader.LoadDefault();
        }

        PredicateEvaluator evaluator = new(RoutingDefaults.Matchers());
        IReadOnlyList<ITargetResolver> resolvers = RoutingDefaults.Resolvers(appConfig, evaluator);
        ProfileResolver profileResolver = new(ctx.Profiles, appConfig.Profiles, logger);
        TrackingStripper stripper = new(appConfig);
        IRoutingExecutor executor = new RoutingExecutor(ctx.Inventory, profileResolver, stripper, appConfig);
        IConfigWriter configWriter = new JsonConfigWriter(configPath, logger);
        IPickerLauncher pickerLauncher = new AvaloniaPickerLauncher(logger, configWriter, icons: ctx.Icons);
        IRecentPicksLog recentPicks = new FileRecentPicksLog(configPath, logger);
        RuleRouter router = new(
            resolvers,
            executor,
            ctx.Inventory,
            ctx.SourceApp,
            pickerLauncher,
            usePickerAsCatchAll: true,
            appConfig.Profiles,
            ctx.Profiles,
            ctx.Launcher,
            logger,
            TimeProvider.System);

        return new CommandContext(
            logger,
            ctx.Inventory,
            ctx.Launcher,
            ctx.Profiles,
            ctx.SourceApp,
            ctx.ProcessNameNormalizer,
            ctx.ConfigPath,
            appConfig,
            TimeProvider.System,
            ctx.PlatformName,
            registry,
            router,
            pickerLauncher,
            recentPicks);
    }

    public static IBrowserInventory BuildInventory()
    {
        return SelectPlatform().Inventory;
    }

    private static BootstrapContext SelectPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsBootstrap.Create();
        }
        if (OperatingSystem.IsMacOS())
        {
            return MacOsBootstrap.Create();
        }
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return LinuxBootstrap.Create();
        }
        throw new PlatformNotSupportedException(
            $"askmefirst has no platform integration for {RuntimeInformation.OSDescription}.");
    }
}
