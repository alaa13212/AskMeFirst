using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;
#if WINDOWS
using AskMeFirst.Platforms.Windows;
#endif
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeFirst;

internal static partial class Composition
{
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
        services.AddSingleton<IOsSettingsOpener, WindowsOsSettingsOpener>();
    }
#endif
}
