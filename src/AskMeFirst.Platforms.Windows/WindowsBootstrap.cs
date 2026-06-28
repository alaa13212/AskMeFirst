using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Windows;

public static class WindowsBootstrap
{
    public static BootstrapContext Create()
    {
        IBrowserInventory inventory = new WindowsBrowserInventory();
        IUrlLauncher launcher = new WindowsUrlLauncher();
        IBrowserProfileDetector profiles = new WindowsBrowserProfileDetector();
        IProcessNameNormalizer normalizer = new WindowsProcessNameNormalizer();
        ISourceAppDetector sourceApp = new WindowsSourceAppDetector(normalizer);
        IConfigPathResolver configPath = new WindowsConfigPathResolver();
        return new BootstrapContext(inventory, launcher, profiles, sourceApp, normalizer, configPath, "windows");
    }
}