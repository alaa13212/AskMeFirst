using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Composition;

namespace AskMeFirst.Platforms.Linux;

public static class LinuxBootstrap
{
    public static BootstrapContext Create()
    {
        IBrowserInventory inventory = new LinuxBrowserInventory();
        IUrlLauncher launcher = new LinuxUrlLauncher();
        return new BootstrapContext(inventory, launcher, "linux");
    }
}
