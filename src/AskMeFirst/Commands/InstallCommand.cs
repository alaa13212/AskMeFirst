using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class InstallCommand : ICommand
{
    public string Name => "install";
    public string Usage => "install";
    public string Description => "Register AskMeFirst as the default browser for http/https URLs.";

    public int Execute(string[] args, CommandContext ctx)
    {
        RegistrationResult result = ctx.DefaultBrowserRegistrar
            .RegisterAsync()
            .GetAwaiter()
            .GetResult();

        Console.WriteLine(result.Message);

        if (!result.Success)
        {
            return 1;
        }

        bool opened = false;
        try
        {
            opened = ctx.DefaultBrowserRegistrar.TryOpenOsSettings();
        }
        catch (Exception ex)
        {
            ctx.Logger.LogWarn($"Could not open OS settings: {ex.Message}");
        }

        if (!opened)
        {
            ctx.Logger.LogInfo("Open the OS default-browser settings manually to finish setup.");
        }
        return 0;
    }
}