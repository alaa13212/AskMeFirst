using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Composition;

public sealed record BootstrapContext(
    IBrowserInventory Inventory,
    IUrlLauncher Launcher,
    string PlatformName);
