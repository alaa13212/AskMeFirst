namespace AskMeFirst.MgmtUi.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class MgmtUiWindowViewModel : ObservableObject
{
    private readonly IConfigPersister _persister;
    private readonly IRecentPicksReader _recentPicksReader;

    public MgmtUiWindowViewModel(IConfigPersister persister, IRecentPicksReader recentPicksReader)
    {
        _persister = persister;
        _recentPicksReader = recentPicksReader;
        _title = "AskMeFirst Settings";
        _versionLabel = "v0.2.0 · config";
        _banner = "Phase 7 · dashboard online — click a card or tree leaf to drill.";

        IReadOnlyList<AreaDescriptor> all = AreaCatalog.All;
        Areas = new ObservableCollection<AreaCardViewModel>();
        foreach (AreaDescriptor descriptor in all)
        {
            Areas.Add(new AreaCardViewModel(descriptor, this));
        }

        Tree = new ObservableCollection<TreeNodeViewModel>();
        RebuildTree();
        ActiveDetail = null;

        _persister.Changed += (_, _) => OnConfigChanged();
    }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _versionLabel;

    [ObservableProperty]
    private string _banner;

    [ObservableProperty]
    private object? _activeDetail;

    public ObservableCollection<AreaCardViewModel> Areas { get; }

    public ObservableCollection<TreeNodeViewModel> Tree { get; }

    public bool IsDashboard => ActiveDetail is null;

    public bool HasDrafts
    {
        get
        {
            if (ActiveDetail is AreaDetailViewModel area)
            {
                return area.HasDrafts;
            }
            if (ActiveDetail is EntityDetailViewModel entity)
            {
                foreach (DraftableRowViewModel row in entity.Rows)
                {
                    if (row.HasDraft)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public void DrillTo(AreaId? areaId)
    {
        if (areaId.HasValue)
        {
            AreaDetailViewModel detail = CreateAreaDetail(areaId.Value);
            detail.Rebuild(_persister);
            ActiveDetail = detail;
            RebuildTree();
        }
        else
        {
            ActiveDetail = null;
        }
        OnPropertyChanged(nameof(IsDashboard));
        OnPropertyChanged(nameof(HasDrafts));
    }

    public void DrillToEntity(AreaId areaId, string entityId)
    {
        EntityDetailViewModel? entity = areaId switch
        {
            AreaId.Browsers => new BrowserEntityDetailViewModel(entityId, this, _persister),
            AreaId.Profiles => new ProfileEntityDetailViewModel(entityId, this, _persister),
            _ => null,
        };
        if (entity is null)
        {
            return;
        }
        entity.Rebuild(_persister);
        ActiveDetail = entity;
        RebuildTree();
        OnPropertyChanged(nameof(IsDashboard));
        OnPropertyChanged(nameof(HasDrafts));
    }

    public void BackToArea(AreaId areaId)
    {
        DrillTo(areaId);
    }

    public void RebuildActiveArea()
    {
        if (ActiveDetail is AreaDetailViewModel area)
        {
            area.Rebuild(_persister);
        }
        else if (ActiveDetail is EntityDetailViewModel entity)
        {
            entity.Rebuild(_persister);
        }
        RebuildTree();
        OnPropertyChanged(nameof(HasDrafts));
    }

    private void OnConfigChanged()
    {
        RebuildActiveArea();
        RebuildTree();
        OnPropertyChanged(nameof(HasDrafts));
    }

    private void RebuildTree()
    {
        Tree.Clear();
        TreeNodeViewModel inventory = new("Inventory", null, null, this) { IsExpanded = true };
        TreeNodeViewModel browsersNode = new("Browsers", AreaId.Browsers, null, this);
        PopulateEntityChildren(browsersNode, AreaId.Browsers,
            _persister.Current.Browsers.Select(b => (Id: b.Id, Label: b.DisplayName)));
        inventory.Children.Add(browsersNode);
        TreeNodeViewModel profilesNode = new("Profiles", AreaId.Profiles, null, this);
        PopulateEntityChildren(profilesNode, AreaId.Profiles,
            _persister.Current.Profiles.Select(p => (Id: p.Id, Label: p.DisplayName ?? p.Name ?? p.Id)));
        inventory.Children.Add(profilesNode);

        TreeNodeViewModel rulesNode = new("Rules", null, null, this);
        rulesNode.Children.Add(new TreeNodeViewModel("Active", AreaId.Rules, null, this));

        TreeNodeViewModel usageNode = new("Usage", null, null, this) { IsExpanded = true };
        usageNode.Children.Add(new TreeNodeViewModel("Suggestions", AreaId.Suggestions, null, this));
        usageNode.Children.Add(new TreeNodeViewModel("Recent picks", AreaId.Usage, null, this));

        TreeNodeViewModel advanced = new("Advanced", null, null, this);
        advanced.Children.Add(new TreeNodeViewModel("Unshortener", AreaId.Unshortener, null, this));
        advanced.Children.Add(new TreeNodeViewModel("Tracking params", AreaId.Tracking, null, this));
        advanced.Children.Add(new TreeNodeViewModel("Cache", AreaId.Cache, null, this));

        Tree.Add(inventory);
        Tree.Add(rulesNode);
        Tree.Add(usageNode);
        Tree.Add(advanced);

        AutoExpandActiveArea();
    }

    private void PopulateEntityChildren(
        TreeNodeViewModel parent,
        AreaId areaId,
        IEnumerable<(string Id, string Label)> entities)
    {
        foreach ((string Id, string Label) entity in entities)
        {
            parent.Children.Add(new TreeNodeViewModel(entity.Label, areaId, entity.Id, this));
        }
    }

    private void AutoExpandActiveArea()
    {
        AreaId? activeArea = GetActiveAreaId();
        if (activeArea is null)
        {
            return;
        }
        foreach (TreeNodeViewModel rootNode in Tree)
        {
            foreach (TreeNodeViewModel child in rootNode.Children)
            {
                if (child.TargetArea == activeArea && child.Children.Count > 0)
                {
                    child.IsExpanded = true;
                }
            }
        }
    }

    private AreaId? GetActiveAreaId()
    {
        if (ActiveDetail is AreaDetailViewModel area)
        {
            return area.AreaId;
        }
        if (ActiveDetail is EntityDetailViewModel entity)
        {
            return entity.ParentArea;
        }
        return null;
    }

    private AreaDetailViewModel CreateAreaDetail(AreaId areaId)
    {
        return areaId switch
        {
            AreaId.Browsers => new BrowserDetailViewModel(this, _persister),
            AreaId.Profiles => new ProfileDetailViewModel(this, _persister),
            AreaId.Rules => new RuleDetailViewModel(this, _persister),
            AreaId.Usage => new UsageDetailViewModel(this, _recentPicksReader),
            AreaId.Suggestions => new SuggestionsDetailViewModel(this, _recentPicksReader),
            AreaId.Unshortener => new StringListDetailViewModel(areaId, this, StringListKind.UnshortenDomain, "Unshortener", _persister),
            AreaId.Tracking => new StringListDetailViewModel(areaId, this, StringListKind.TrackingParam, "Tracking params", _persister),
            AreaId.Cache => new CacheDetailViewModel(this, _persister),
            _ => new NoFormsDetailViewModel(areaId, this, $"Detail view for {areaId}."),
        };
    }
}
