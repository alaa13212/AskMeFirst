using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Commands;

public sealed record CommandContext(
    ILogger Logger,
    IBrowserInventory Inventory,
    IUrlLauncher Launcher,
    IBrowserProfileDetector Profiles,
    Core.Config.AppConfig AppConfig,
    string PlatformName,
    CommandRegistry Registry);

public interface ICommand
{
    string Name { get; }

    IReadOnlyList<string> Aliases => [];

    string Usage => Name;

    string Description => "";

    int Execute(string[] args, CommandContext ctx);
}