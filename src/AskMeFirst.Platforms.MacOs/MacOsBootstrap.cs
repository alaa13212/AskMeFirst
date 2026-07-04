using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.MacOs;

[SupportedOSPlatform("osx")]
public static class MacOsBootstrap
{
    public static BootstrapContext Create(ILogger logger)
    {
        IBrowserInventory inventory = new MacOsBrowserInventory();
        IUrlLauncher launcher = new MacOsUrlLauncher(logger);
        IBrowserProfileDetector profiles = new MacOsBrowserProfileDetector();
        IProcessNameNormalizer normalizer = new MacOsProcessNameNormalizer();
        ISourceAppDetector sourceApp = new MacOsSourceAppDetector(normalizer);
        IConfigPathResolver configPath = new MacOsConfigPathResolver();
        IIconProvider icons = new MacIconProvider();
        INotifier notifier = new MacNotifier(logger);
        IDefaultBrowserRegistrar registrar = new MacOsDefaultBrowserRegistrar();
        ISourceAppWindowLocator sourceLocator = new MacSourceAppWindowLocator();
        return new BootstrapContext(inventory, launcher, profiles, sourceApp, normalizer, configPath, icons, notifier, registrar, sourceLocator, "macos");
    }
}