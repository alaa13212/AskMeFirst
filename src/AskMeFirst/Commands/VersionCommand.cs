using AskMeFirst.Core.Commands;

namespace AskMeFirst.Commands;

public sealed class VersionCommand : ICommand
{
    public string Name => "--version";
    public IReadOnlyList<string> Aliases => ["-V"];
    public string Usage => "--version, -V";
    public string Description => "Print version and exit.";

    public int Execute(string[] args, CommandContext ctx)
    {
        Console.WriteLine($"{ProgramInfo.ExecutableName} {ProgramInfo.Version}");
        return 0;
    }
}