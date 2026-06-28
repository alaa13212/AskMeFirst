namespace AskMeFirst.Core.Config;

public sealed record Rule
{
    public string Name { get; init; } = "";

    public int Priority { get; init; }

    public RuleWhen When { get; init; } = new();

    public RuleThen Then { get; init; } = new();
}