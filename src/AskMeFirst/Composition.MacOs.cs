using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;
#if OSX
using AskMeFirst.Platforms.MacOs;
#endif
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeFirst;

internal static partial class Composition
{
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
        services.AddSingleton<IOsSettingsOpener, MacOsSettingsOpener>();
    }
#endif
}
