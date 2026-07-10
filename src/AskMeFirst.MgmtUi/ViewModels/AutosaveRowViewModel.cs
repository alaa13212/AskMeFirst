using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public abstract partial class AutosaveRowViewModel : RowViewModelBase
{
    private readonly IConfigPersister _persister;

    protected AutosaveRowViewModel(IConfigPersister persister)
    {
        _persister = persister;
    }

    public string? LastError { get; private set; }

    protected IConfigPersister Persister => _persister;

    protected void TryAutosave(Func<AppConfig, AppConfig> mutate)
    {
        SaveOutcome outcome = _persister.Save(mutate);
        if (outcome.Ok)
        {
            LastError = null;
            OnPropertyChanged(nameof(LastError));
            return;
        }
        LastError = outcome.Error;
        OnPropertyChanged(nameof(LastError));
    }

    protected void NotifyAutosaveError()
    {
        OnPropertyChanged(nameof(LastError));
    }
}
