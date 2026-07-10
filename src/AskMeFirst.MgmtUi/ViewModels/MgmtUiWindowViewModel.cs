namespace AskMeFirst.MgmtUi.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using AskMeFirst.MgmtUi.Areas;
using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class MgmtUiWindowViewModel : ObservableObject
{
    public MgmtUiWindowViewModel()
    {
        _title = "AskMeFirst Settings";
        _versionLabel = "v0.2.0 · config";
        _banner = "Phase 7 · dashboard online — click a card or tree leaf to drill.";

        IReadOnlyList<AreaDescriptor> all = AreaCatalog.All;
        Areas = new ObservableCollection<AreaCardViewModel>();
        foreach (AreaDescriptor descriptor in all)
        {
            Areas.Add(new AreaCardViewModel(descriptor, this));
        }

        TreeNodeViewModel[] built = BuildTree(all, this);
        Tree = new ObservableCollection<TreeNodeViewModel>(built);
        ActiveDetail = null;
    }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _versionLabel;

    [ObservableProperty]
    private string _banner;

    [ObservableProperty]
    private AreaDetailViewModel? _activeDetail;

    public ObservableCollection<AreaCardViewModel> Areas { get; }

    public ObservableCollection<TreeNodeViewModel> Tree { get; }

    public bool IsDashboard => ActiveDetail is null;

    public void DrillTo(AreaId? areaId)
    {
        if (areaId.HasValue)
        {
            ActiveDetail = new AreaDetailViewModel(areaId.Value, this);
        }
        else
        {
            ActiveDetail = null;
        }
        OnPropertyChanged(nameof(IsDashboard));
    }

    private static TreeNodeViewModel[] BuildTree(IReadOnlyList<AreaDescriptor> all, MgmtUiWindowViewModel root)
    {
        TreeNodeViewModel inventory = new("Inventory", null, root) { IsExpanded = true };
        inventory.Children.Add(new TreeNodeViewModel("Browsers", AreaId.Browsers, root));
        inventory.Children.Add(new TreeNodeViewModel("Profiles", AreaId.Profiles, root));

        TreeNodeViewModel rules = new("Rules", null, root);
        rules.Children.Add(new TreeNodeViewModel("Active", AreaId.Rules, root));

        TreeNodeViewModel usage = new("Usage", null, root) { IsExpanded = true };
        usage.Children.Add(new TreeNodeViewModel("Suggestions", AreaId.Suggestions, root));
        usage.Children.Add(new TreeNodeViewModel("Recent picks", AreaId.Usage, root));

        TreeNodeViewModel advanced = new("Advanced", null, root);
        advanced.Children.Add(new TreeNodeViewModel("Unshortener", AreaId.Unshortener, root));
        advanced.Children.Add(new TreeNodeViewModel("Tracking params", AreaId.Tracking, root));
        advanced.Children.Add(new TreeNodeViewModel("Cache", AreaId.Cache, root));

        return new TreeNodeViewModel[] { inventory, rules, usage, advanced };
    }
}
