using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public abstract class RowViewModelBase : ObservableObject
{
    public abstract string Label { get; }
}
