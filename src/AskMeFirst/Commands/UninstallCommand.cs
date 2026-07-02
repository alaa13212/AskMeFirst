using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class UninstallCommand : ICommand
{
    public string Name => "uninstall";
    public string Usage => "uninstall";
    public string Description => "Remove AskMeFirst default-browser registration.";

    public int Execute(string[] args, CommandContext ctx)
    {
        RegistrationResult result = ctx.DefaultBrowserRegistrar
            .UnregisterAsync()
            .GetAwaiter()
            .GetResult();

        Console.WriteLine(result.Message);
        return result.Success ? 0 : 1;
    }
}