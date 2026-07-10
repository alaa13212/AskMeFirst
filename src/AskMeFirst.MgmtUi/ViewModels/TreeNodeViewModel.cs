namespace AskMeFirst.MgmtUi.ViewModels;

using System.Collections.ObjectModel;
using AskMeFirst.MgmtUi.Areas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class TreeNodeViewModel : ObservableObject
{
    public TreeNodeViewModel(string label, AreaId? targetArea, string? targetEntityId, MgmtUiWindowViewModel root)
    {
        Label = label;
        TargetArea = targetArea;
        TargetEntityId = targetEntityId;
        _root = root;
        Children = new ObservableCollection<TreeNodeViewModel>();
    }

    public string Label { get; }

    public AreaId? TargetArea { get; }

    public string? TargetEntityId { get; }

    public ObservableCollection<TreeNodeViewModel> Children { get; }

    public bool IsLeaf => TargetArea.HasValue && TargetEntityId is null;

    public bool IsEntityLeaf => TargetEntityId is not null;

    public bool HasChildren => Children.Count > 0;

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void Drill()
    {
        if (TargetEntityId is not null && TargetArea.HasValue)
        {
            _root.DrillToEntity(TargetArea.Value, TargetEntityId);
        }
        else if (TargetArea.HasValue)
        {
            _root.DrillTo(TargetArea.Value);
        }
    }

    private readonly MgmtUiWindowViewModel _root;
}
