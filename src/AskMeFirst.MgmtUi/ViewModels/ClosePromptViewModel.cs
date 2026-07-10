using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class ClosePromptViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _saveDrafts;

    [ObservableProperty]
    private bool _discardDrafts;

    [ObservableProperty]
    private bool _cancelled;

    [RelayCommand]
    private void Save()
    {
        SaveDrafts = true;
        DiscardDrafts = false;
        Cancelled = false;
    }

    [RelayCommand]
    private void Discard()
    {
        SaveDrafts = false;
        DiscardDrafts = true;
        Cancelled = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        SaveDrafts = false;
        DiscardDrafts = false;
        Cancelled = true;
    }

    public bool Resolved => SaveDrafts || DiscardDrafts || Cancelled;
}
