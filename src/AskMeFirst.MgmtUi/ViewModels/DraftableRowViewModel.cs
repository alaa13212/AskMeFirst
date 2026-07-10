using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public abstract partial class DraftableRowViewModel : ObservableObject
{
    private readonly IConfigPersister _persister;

    protected DraftableRowViewModel(IConfigPersister persister)
    {
        _persister = persister;
    }

    public abstract string Label { get; }

    public abstract bool HasDraft { get; }

    public string? LastError { get; private set; }

    [RelayCommand(CanExecute = nameof(CanMutate))]
    public void Save()
    {
        SaveOutcome outcome = _persister.Save(BuildMutator());
        if (outcome.Ok)
        {
            LastError = null;
            OnSaveCommitted();
            OnPropertyChanged(nameof(LastError));
            OnPropertyChanged(nameof(HasDraft));
            DiscardCommand.NotifyCanExecuteChanged();
            return;
        }
        LastError = outcome.Error;
        OnPropertyChanged(nameof(LastError));
    }

    [RelayCommand(CanExecute = nameof(CanMutate))]
    public void Discard()
    {
        ResetDrafts();
        LastError = null;
        OnPropertyChanged(nameof(LastError));
        OnPropertyChanged(nameof(HasDraft));
        SaveCommand.NotifyCanExecuteChanged();
        DiscardCommand.NotifyCanExecuteChanged();
    }

    public bool CanMutate() => HasDraft;

    protected abstract Func<AppConfig, AppConfig> BuildMutator();

    protected abstract void ResetDrafts();

    protected abstract void OnSaveCommitted();

    protected void NotifyDraftStateChanged()
    {
        OnPropertyChanged(nameof(HasDraft));
        SaveCommand.NotifyCanExecuteChanged();
        DiscardCommand.NotifyCanExecuteChanged();
    }
}
