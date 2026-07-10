using AskMeFirst.Core.Commands;
using AskMeFirst.MgmtUi;

namespace AskMeFirst.Commands;

public sealed class ConfigCommand : ICommand
{
    public string Name => "config";

    public string Usage => "config";

    public string Description => "Open the AskMeFirst settings window to manage browsers, profiles, and rules.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        return Task.FromResult(MgmtUiEntryPoint.Run());
    }
}
