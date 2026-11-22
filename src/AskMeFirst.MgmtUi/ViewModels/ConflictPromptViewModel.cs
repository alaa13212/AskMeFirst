using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class ConflictPromptViewModel : ObservableObject
{
    public ConflictPromptViewModel(string configPath)
    {
        Header = $"Config changed on disk: {configPath}";
        Detail = "You have unsaved edits. Keep your changes, or discard them and load the new file?";
    }

    public string Header { get; }

    public string Detail { get; }

    [ObservableProperty]
    private bool _keepDrafts;

    [ObservableProperty]
    private bool _discardDrafts;

    [RelayCommand]
    private void Keep()
    {
        KeepDrafts = true;
        DiscardDrafts = false;
    }

    [RelayCommand]
    private void Discard()
    {
        KeepDrafts = false;
        DiscardDrafts = true;
    }

    public bool Resolved => KeepDrafts || DiscardDrafts;
}
