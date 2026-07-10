using System.Collections.ObjectModel;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public abstract partial class EntityDetailViewModel : ObservableObject
{
    private readonly MgmtUiWindowViewModel _parent;

    protected EntityDetailViewModel(string entityId, MgmtUiWindowViewModel parent)
    {
        EntityId = entityId;
        _parent = parent;
    }

    public string EntityId { get; }

    public AreaId ParentArea { get; protected set; }

    [ObservableProperty]
    private string _title = "";

    public ObservableCollection<DraftableRowViewModel> Rows { get; } = new();

    public string? LastError { get; private set; }

    [RelayCommand]
    private void BackToArea()
    {
        _parent.BackToArea(ParentArea);
    }

    public abstract string Label { get; }

    public abstract void Rebuild(IConfigPersister persister);

    protected void NotifyErrors(string? error)
    {
        LastError = error;
        OnPropertyChanged(nameof(LastError));
    }
}
