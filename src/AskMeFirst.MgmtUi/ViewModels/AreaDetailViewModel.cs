using System.Collections.ObjectModel;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public abstract partial class AreaDetailViewModel : ObservableObject
{
    private readonly MgmtUiWindowViewModel _parent;

    protected AreaDetailViewModel(AreaId areaId, MgmtUiWindowViewModel parent)
    {
        AreaId = areaId;
        AreaDescriptor? descriptor = AreaCatalog.Find(areaId);
        _title = descriptor?.DetailTitle ?? areaId.ToString();
        _parent = parent;
    }

    public AreaId AreaId { get; }

    [ObservableProperty]
    private string _title;

    public ObservableCollection<RowViewModelBase> Rows { get; } = new();

    public bool HasRows => Rows.Count > 0;

    public abstract string Placeholder { get; }

    public bool HasDrafts
    {
        get
        {
            foreach (RowViewModelBase row in Rows)
            {
                if (row is DraftableRowViewModel draftable && draftable.HasDraft)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        _parent.DrillTo(null);
    }

    protected void NotifyRowsChanged()
    {
        OnPropertyChanged(nameof(HasRows));
    }

    public abstract void Rebuild(IConfigPersister persister);
}
