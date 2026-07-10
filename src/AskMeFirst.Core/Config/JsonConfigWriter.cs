using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Core.Config;

public sealed class JsonConfigWriter(string configPath, ILogger logger) : IConfigWriter
{
    private readonly object _gate = new();

    public void AppendRule(Rule rule)
    {
        lock (_gate)
        {
            AppConfig config = ConfigLoader.LoadOrDefault(configPath);

            List<Rule> updated = [.. config.Rules, rule];
            AppConfig next = config with { Rules = updated };

            ConfigLoader.WriteAtomic(next, configPath);
            logger.LogInfo($"Appended rule '{rule.Name}' (priority {rule.Priority}, origin {rule.Origin}).");
        }
    }
}
