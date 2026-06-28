namespace AskMeFirst.Core.Config;

public sealed class NoOpConfigWriter : IConfigWriter
{
    public void AppendRule(Rule rule)
    {
    }
}