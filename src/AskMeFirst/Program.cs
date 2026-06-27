using AskMeFirst.Commands;
using AskMeFirst.Core;
using AskMeFirst.Core.Commands;

namespace AskMeFirst;

internal static class Program
{
    private static int Main()
    {
        string[] cli = Environment.GetCommandLineArgs();
        string[] userArgs = cli.Length > 1 ? cli[1..] : [];

        if (userArgs.Length == 0)
        {
            Console.Error.WriteLine(HelpFormatter.Render(BuildRegistry()));
            return 1;
        }

        try
        {
            CommandRegistry registry = BuildRegistry();
            CommandContext ctx = Composition.Bootstrap(IsVerboseRequested(userArgs), registry);

            ICommand command = registry.Resolve(userArgs[0]);
            return command.Execute(userArgs, ctx);
        }
        catch (CliArgsException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Console.Error.WriteLine($"Run '{ProgramInfo.ExecutableName} --help' for usage.");
            return 1;
        }
        catch (CommandNotFoundException)
        {
            Console.Error.WriteLine($"error: unknown command '{userArgs[0]}'.");
            Console.Error.WriteLine($"Run '{ProgramInfo.ExecutableName} --help' for usage.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"fatal: {ex.Message}");
            return 99;
        }
    }

    private static CommandRegistry BuildRegistry()
    {
        return new CommandRegistry()
            .Register(new VersionCommand())
            .Register(new HelpCommand())
            .Register(new BenchCommand())
            .Register(new ListCommand())
            .RegisterDefault(new RouteCommand());
    }

    private static bool IsVerboseRequested(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg is "--verbose" or "-v")
            {
                return true;
            }
        }
        return false;
    }
}