namespace AskMeFirst.Core.Commands;

public sealed class CommandNotFoundException(string name) : Exception(name);