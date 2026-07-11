using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi;

namespace AskMeFirst.Commands;

public sealed class ConfigCommand : ICommand
{
    public string Name => "config";

    public string Usage => "config";

    public string Description => "Open the AskMeFirst settings window to manage browsers, profiles, and rules.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        IConfigPathResolver pathResolver = ctx.Resolve<IConfigPathResolver>();
        IServiceProvider services = ctx.Services;
        IBrowserInventory? inventory = services.GetService(typeof(IBrowserInventory)) as IBrowserInventory;
        IBrowserProfileDetector? detector = services.GetService(typeof(IBrowserProfileDetector)) as IBrowserProfileDetector;
        return Task.FromResult(MgmtUiEntryPoint.Run(
            pathResolver.DefaultConfigPath,
            inventory,
            detector));
    }
}
