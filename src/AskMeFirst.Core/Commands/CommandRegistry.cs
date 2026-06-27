namespace AskMeFirst.Core.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _byName = new(StringComparer.Ordinal);
    private ICommand? _default;

    public CommandRegistry Register(ICommand command)
    {
        _byName[command.Name] = command;
        foreach (string alias in command.Aliases)
        {
            _byName[alias] = command;
        }
        return this;
    }

    public CommandRegistry RegisterDefault(ICommand command)
    {
        if (_default is not null)
        {
            throw new InvalidOperationException(
                $"Default command already set to '{_default.Name}'.");
        }
        _default = command;
        Register(_default);
        return this;
    }

    public IReadOnlyList<ICommand> All()
    {
        return _byName.Values
            .Distinct()
            .OrderBy(c => c == _default ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();
    }

    public ICommand Resolve(string? firstArg)
    {
        if (firstArg is not null && _byName.TryGetValue(firstArg, out ICommand? match))
        {
            return match;
        }

        if (_default is not null)
        {
            return _default;
        }

        throw new CommandNotFoundException(firstArg ?? "<no-args>");
    }
}
