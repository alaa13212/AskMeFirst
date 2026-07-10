namespace AskMeFirst.MgmtUi.ViewModels;

using AskMeFirst.MgmtUi.Areas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class AreaDetailViewModel : ObservableObject
{
    public AreaDetailViewModel(AreaId areaId, MgmtUiWindowViewModel parent)
    {
        AreaId = areaId;
        AreaDescriptor? descriptor = AreaCatalog.Find(areaId);
        _title = descriptor?.DetailTitle ?? areaId.ToString();
        _parent = parent;
    }

    public AreaId AreaId { get; }

    [ObservableProperty]
    private string _title;

    public string BodyMessage => "Detail view for " + AreaId + " — form controls land in the next commit.";

    [RelayCommand]
    private void Back()
    {
        _parent.DrillTo(null);
    }

    private readonly MgmtUiWindowViewModel _parent;
}
