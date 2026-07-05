using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class HelpCommand : ICommand
{
    public string Name => "--help";
    public IReadOnlyList<string> Aliases => ["-h", "-?"];
    public string Usage => "--help, -h, -?";
    public string Description => "Print usage and exit.";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        Console.WriteLine(HelpFormatter.Render(ctx.Registry));
        return Task.FromResult(0);
    }
}