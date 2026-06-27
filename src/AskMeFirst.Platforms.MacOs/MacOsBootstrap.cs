using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;

namespace AskMeFirst.Platforms.MacOs;

public static class MacOsBootstrap
{
    public static BootstrapContext Create()
    {
        IBrowserInventory inventory = new MacOsBrowserInventory();
        IUrlLauncher launcher = new MacOsUrlLauncher();
        IBrowserProfileDetector profiles = new MacOsBrowserProfileDetector();
        return new BootstrapContext(inventory, launcher, profiles, "macos");
    }
}