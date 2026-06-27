using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;

namespace AskMeFirst.Platforms.Windows;

public static class WindowsBootstrap
{
    public static BootstrapContext Create()
    {
        IBrowserInventory inventory = new WindowsBrowserInventory();
        IUrlLauncher launcher = new WindowsUrlLauncher();
        return new BootstrapContext(inventory, launcher, "windows");
    }
}
