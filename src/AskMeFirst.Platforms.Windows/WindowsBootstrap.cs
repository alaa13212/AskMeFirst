using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Windows;

[SupportedOSPlatform("windows")]
public static class WindowsBootstrap
{
    public static BootstrapContext Create()
    {
        ConsoleLogger logger = new(verbose: false);
        IBrowserInventory inventory = new WindowsBrowserInventory();
        IUrlLauncher launcher = new WindowsUrlLauncher();
        IBrowserProfileDetector profiles = new WindowsBrowserProfileDetector();
        IProcessNameNormalizer normalizer = new WindowsProcessNameNormalizer();
        ISourceAppDetector sourceApp = new WindowsSourceAppDetector(normalizer);
        IConfigPathResolver configPath = new WindowsConfigPathResolver();
        IIconProvider icons = new WindowsIconProvider();
        INotifier notifier = new WindowsNotifier(logger);
        IDefaultBrowserRegistrar registrar = new NullDefaultBrowserRegistrar();
        ISourceAppWindowLocator sourceLocator = new NullSourceAppWindowLocator();
        return new BootstrapContext(inventory, launcher, profiles, sourceApp, normalizer, configPath, icons, notifier, registrar, sourceLocator, "windows");
    }
}