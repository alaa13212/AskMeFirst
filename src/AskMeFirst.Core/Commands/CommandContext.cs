namespace AskMeFirst.Core.Commands;

public sealed record CommandContext(
    CommandRegistry Registry,
    IServiceProvider Services,
    IReadOnlyList<ICommand> Commands,
    bool Verbose);