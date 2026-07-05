namespace AskMeFirst.Core.Commands;

public sealed record CommandContext(
    CommandRegistry Registry,
    IServiceProvider Services,
    bool Verbose);