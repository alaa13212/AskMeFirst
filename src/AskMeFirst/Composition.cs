using System.Runtime.InteropServices;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Platforms.Linux;
using AskMeFirst.Platforms.MacOs;
using AskMeFirst.Platforms.Windows;

namespace AskMeFirst;

internal static class Composition
{
    public static CommandContext Bootstrap(bool verbose, CommandRegistry registry)
    {
        BootstrapContext ctx = SelectPlatform();

        ILogger logger = new ConsoleLogger(verbose);
        AppConfig appConfig = ConfigLoader.LoadDefault();

        return new CommandContext(
            logger, ctx.Inventory, ctx.Launcher, ctx.Profiles, appConfig, ctx.PlatformName, registry);
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
