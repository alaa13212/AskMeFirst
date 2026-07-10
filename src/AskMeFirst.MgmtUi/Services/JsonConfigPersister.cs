using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;

namespace AskMeFirst.MgmtUi.Services;

public sealed class JsonConfigPersister : IConfigPersister
{
    private readonly string _configPath;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private AppConfig _current;

    public JsonConfigPersister(string configPath, ILogger logger)
    {
        _configPath = configPath;
        _logger = logger;
        _current = ConfigLoader.LoadOrDefault(configPath);
    }

    public event EventHandler? Changed;

    public string ConfigPath => _configPath;

    public AppConfig Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Reload()
    {
        AppConfig loaded;
        lock (_gate)
        {
            loaded = ConfigLoader.LoadOrDefault(_configPath);
            _current = loaded;
        }
        RaiseChanged();
        _logger.LogInfo($"Reloaded config from {_configPath}.");
    }

    public SaveOutcome Save(Func<AppConfig, AppConfig> mutate)
    {
        AppConfig next;
        try
        {
            next = mutate(_current);
        }
        catch (Exception ex)
        {
            return SaveOutcome.Failure(ex.Message);
        }

        try
        {
            ConfigLoader.WriteAtomic(next, _configPath);
        }
        catch (Exception ex)
        {
            return SaveOutcome.Failure(ex.Message);
        }

        lock (_gate)
        {
            _current = next;
        }
        RaiseChanged();
        _logger.LogInfo($"Persisted config to {_configPath}.");
        return SaveOutcome.Success();
    }

    private void RaiseChanged()
    {
        EventHandler? handler = Changed;
        if (handler is not null)
        {
            handler(this, EventArgs.Empty);
        }
    }
}
