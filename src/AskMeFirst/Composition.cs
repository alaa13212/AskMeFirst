using System.Runtime.InteropServices;
using AskMeFirst.Core;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;
using AskMeConfig = AskMeFirst.Core.Config.Config;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;

namespace AskMeFirst;

internal static class Composition
{
    public static UrlRouter BuildRouter(bool verbose, out string platformName)
    {
        BootstrapContext ctx = SelectPlatform();
        platformName = ctx.PlatformName;

        ILogger logger = new ConsoleLogger(verbose);
        AskMeConfig config = ConfigLoader.LoadDefault();

        return new UrlRouter(ctx.Inventory, ctx.Launcher, logger, config);
    }

    public static IBrowserInventory BuildInventory()
    {
        return SelectPlatform().Inventory;
    }

    private static BootstrapContext SelectPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return AskMeFirst.Platforms.Windows.WindowsBootstrap.Create();
        }
        if (OperatingSystem.IsMacOS())
        {
            return AskMeFirst.Platforms.MacOs.MacOsBootstrap.Create();
        }
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return AskMeFirst.Platforms.Linux.LinuxBootstrap.Create();
        }
        throw new PlatformNotSupportedException(
            $"askmefirst has no platform integration for {RuntimeInformation.OSDescription}.");
    }
}
