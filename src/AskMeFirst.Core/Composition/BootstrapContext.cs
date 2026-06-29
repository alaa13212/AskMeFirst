using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core.Composition;

public sealed record BootstrapContext(
    IBrowserInventory Inventory,
    IUrlLauncher Launcher,
    IBrowserProfileDetector Profiles,
    ISourceAppDetector SourceApp,
    IProcessNameNormalizer ProcessNameNormalizer,
    IConfigPathResolver ConfigPath,
    IIconProvider Icons,
    string PlatformName);