namespace AskMeFirst.Core.Commands;

public interface ICommand
{
    string Name { get; }

    IReadOnlyList<string> Aliases => [];

    string Usage => Name;

    string Description => "";

    int Execute(string[] args, CommandContext ctx);
}