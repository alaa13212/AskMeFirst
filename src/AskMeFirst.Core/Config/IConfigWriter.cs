namespace AskMeFirst.Core.Config;

public interface IConfigWriter
{
    void AppendRule(Rule rule);
}