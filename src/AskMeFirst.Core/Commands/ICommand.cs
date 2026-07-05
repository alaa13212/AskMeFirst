namespace AskMeFirst.Core.Commands;

public interface ICommand
{
    string Name { get; }

    IReadOnlyList<string> Aliases => [];

    string Usage => Name;

    string Description => "";

    Task<int> Execute(string[] args, CommandContext ctx);
}