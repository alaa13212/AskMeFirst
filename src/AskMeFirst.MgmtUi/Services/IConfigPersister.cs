using AskMeFirst.Core.Config;

namespace AskMeFirst.MgmtUi.Services;

public interface IConfigPersister
{
    event EventHandler? Changed;

    string ConfigPath { get; }

    AppConfig Current { get; }

    void Reload();

    SaveOutcome Save(Func<AppConfig, AppConfig> mutate);
}
