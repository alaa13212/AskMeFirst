namespace AskMeFirst.MgmtUi.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class MgmtUiWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AskMeFirst Settings";

    [ObservableProperty]
    private string _bodyMessage = "MgmtUi scaffold online — phase 7 areas land in subsequent commits.";
}
