using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Core.Commands;

public sealed record CommandContext(
    ILogger Logger,
    IBrowserInventory Inventory,
    IUrlLauncher Launcher,
    IBrowserProfileDetector Profiles,
    ISourceAppDetector SourceApp,
    IProcessNameNormalizer ProcessNameNormalizer,
    IConfigPathResolver ConfigPath,
    AppConfig AppConfig,
    TimeProvider TimeProvider,
    string PlatformName,
    CommandRegistry Registry,
    RuleRouter Router,
    IPickerLauncher PickerLauncher,
    IRecentPicksLog RecentPicks,
    INotifier Notifier);
