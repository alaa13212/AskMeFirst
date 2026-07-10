namespace AskMeFirst.MgmtUi.ViewModels;

using System.Collections.ObjectModel;
using AskMeFirst.MgmtUi.Areas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class TreeNodeViewModel : ObservableObject
{
    public TreeNodeViewModel(string label, AreaId? targetArea, MgmtUiWindowViewModel root)
    {
        Label = label;
        TargetArea = targetArea;
        _root = root;
        Children = new ObservableCollection<TreeNodeViewModel>();
    }

    public string Label { get; }

    public AreaId? TargetArea { get; }

    public ObservableCollection<TreeNodeViewModel> Children { get; }

    public bool IsLeaf => TargetArea.HasValue;

    public bool HasChildren => Children.Count > 0;

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void Drill()
    {
        if (TargetArea.HasValue)
        {
            _root.DrillTo(TargetArea.Value);
        }
    }

    private readonly MgmtUiWindowViewModel _root;
}
