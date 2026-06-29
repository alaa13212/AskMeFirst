using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.MacOs;

public static class MacOsBootstrap
{
    public static BootstrapContext Create()
    {
        IBrowserInventory inventory = new MacOsBrowserInventory();
        IUrlLauncher launcher = new MacOsUrlLauncher();
        IBrowserProfileDetector profiles = new MacOsBrowserProfileDetector();
        IProcessNameNormalizer normalizer = new MacOsProcessNameNormalizer();
        ISourceAppDetector sourceApp = new MacOsSourceAppDetector(normalizer);
        IConfigPathResolver configPath = new MacOsConfigPathResolver();
        IIconProvider icons = new NullIconProvider();
        return new BootstrapContext(inventory, launcher, profiles, sourceApp, normalizer, configPath, icons, "macos");
    }
}