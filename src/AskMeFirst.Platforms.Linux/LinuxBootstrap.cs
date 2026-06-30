using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Logging;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Linux;

public static class LinuxBootstrap
{
    public static BootstrapContext Create()
    {
        ConsoleLogger logger = new(verbose: false);
        IBrowserInventory inventory = new LinuxBrowserInventory();
        IUrlLauncher launcher = new LinuxUrlLauncher();
        IBrowserProfileDetector profiles = new LinuxBrowserProfileDetector();
        IProcessNameNormalizer normalizer = new LinuxProcessNameNormalizer();
        ISourceAppDetector sourceApp = new LinuxSourceAppDetector(normalizer);
        IConfigPathResolver configPath = new LinuxConfigPathResolver();
        IIconProvider icons = new LinuxIconProvider();
        INotifier notifier = new LinuxNotifier(logger);
        return new BootstrapContext(inventory, launcher, profiles, sourceApp, normalizer, configPath, icons, notifier, "linux");
    }
}