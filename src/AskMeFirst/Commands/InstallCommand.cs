using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class InstallCommand : ICommand
{
    public string Name => "install";
    public string Usage => "install";
    public string Description => "Register AskMeFirst as the default browser for http/https URLs.";

    public async Task<int> Execute(string[] args, CommandContext ctx)
    {
        ILogger logger = ctx.Resolve<ILogger>();
        IDefaultBrowserRegistrar registrar = ctx.Resolve<IDefaultBrowserRegistrar>();
        IOsSettingsOpener settingsOpener = ctx.Resolve<IOsSettingsOpener>();

        RegistrationResult result = await registrar.RegisterAsync();

        Console.WriteLine(result.Message);

        if (!result.Success)
        {
            return 1;
        }

        bool opened = false;
        try
        {
            opened = settingsOpener.TryOpen();
        }
        catch (Exception ex)
        {
            logger.LogWarn($"Could not open OS settings: {ex.Message}");
        }

        if (!opened)
        {
            logger.LogInfo("Open the OS default-browser settings manually to finish setup.");
        }
        return 0;
    }
}