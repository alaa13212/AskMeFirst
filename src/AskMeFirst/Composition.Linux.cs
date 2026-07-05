using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;
#if LINUX
using AskMeFirst.Platforms.Linux;
#endif
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeFirst;

internal static partial class Composition
{
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
        services.AddSingleton<IOsSettingsOpener, NullOsSettingsOpener>();
    }
#endif
}
