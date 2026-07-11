namespace AskMeFirst.MgmtUi.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class MgmtUiWindowViewModel : ObservableObject
{
    private readonly IConfigPersister _persister;
    private readonly IRecentPicksReader _recentPicksReader;
    private readonly IRulePreviewService _rulePreview;
    private readonly IConfigFileWatcher _watcher;
    private readonly IBrowserDiscovery _discovery;
    private readonly IPlatformIntegration _platformIntegration;
    private readonly INotifier _notifier;
    private readonly IUrlLauncher _urlLauncher;

    public MgmtUiWindowViewModel(
        IConfigPersister persister,
        IRecentPicksReader recentPicksReader,
        IRulePreviewService rulePreview,
        IConfigFileWatcher watcher,
        IBrowserDiscovery discovery,
        IPlatformIntegration platformIntegration,
        INotifier notifier,
        IUrlLauncher urlLauncher)
    {
        _persister = persister;
        _recentPicksReader = recentPicksReader;
        _rulePreview = rulePreview;
        _watcher = watcher;
        _discovery = discovery;
        _platformIntegration = platformIntegration;
        _notifier = notifier;
        _urlLauncher = urlLauncher;
        _title = "AskMeFirst Settings";
        _versionLabel = "v0.2.0 · config";
        _banner = "Alt+letter to open an area · Esc to back out · Alt+←/→ to step through areas.";

        IReadOnlyList<AreaDescriptor> all = AreaCatalog.All;
        Areas = new ObservableCollection<AreaCardViewModel>();
        foreach (AreaDescriptor descriptor in all)
        {
            Areas.Add(new AreaCardViewModel(descriptor, this));
        }

        UrlTester = new UrlTesterViewModel(persister, rulePreview);
        Tree = new ObservableCollection<TreeNodeViewModel>();
        RebuildTree();
        ActiveDetail = null;

        _persister.Changed += (_, _) => OnConfigChanged();
        _watcher.Changed += (_, _) => OnWatcherChanged();
        _watcher.Start();

        discovery.Refresh();
    }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _versionLabel;

    [ObservableProperty]
    private string _banner;

    [ObservableProperty]
    private object? _activeDetail;

    [ObservableProperty]
    private ConflictPromptViewModel? _pendingConflict;

    [ObservableProperty]
    private string _platformStatus = "";

    public ObservableCollection<AreaCardViewModel> Areas { get; }

    public ObservableCollection<TreeNodeViewModel> Tree { get; }

    public UrlTesterViewModel UrlTester { get; }

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

    [RelayCommand]
    private void Escape()
    {
        if (ActiveDetail is EntityDetailViewModel entity)
        {
            BackToArea(entity.ParentArea);
            return;
        }
        if (ActiveDetail is AreaDetailViewModel)
        {
            DrillTo(null);
        }
    }

    [RelayCommand]
    private void Install()
    {
        PlatformStatus = "Installing...";
        _ = RunInstallAsync();
    }

    [RelayCommand]
    private void Uninstall()
    {
        PlatformStatus = "Uninstalling...";
        _ = RunUninstallAsync();
    }

    private async Task RunInstallAsync()
    {
        RegistrationResult result = await _platformIntegration.InstallAsync();
        PlatformStatus = result.Success
            ? "Installed — open OS settings to finish."
            : $"Install failed: {result.Message}";
        _notifier.Show(
            result.Success ? "AskMeFirst installed" : "AskMeFirst install failed",
            result.Message);
    }

    private async Task RunUninstallAsync()
    {
        RegistrationResult result = await _platformIntegration.UninstallAsync();
        PlatformStatus = result.Success
            ? "Uninstalled."
            : $"Uninstall failed: {result.Message}";
        _notifier.Show(
            result.Success ? "AskMeFirst uninstalled" : "AskMeFirst uninstall failed",
            result.Message);
    }

    [RelayCommand]
    private void DrillByMnemonic(string? mnemonic)
    {
        if (string.IsNullOrEmpty(mnemonic))
        {
            return;
        }
        foreach (AreaDescriptor descriptor in AreaCatalog.All)
        {
            if (string.Equals(descriptor.Mnemonic, mnemonic, StringComparison.OrdinalIgnoreCase))
            {
                DrillTo(descriptor.Id);
                return;
            }
        }
    }

    [RelayCommand]
    private void NextArea()
    {
        StepArea(1);
    }

    [RelayCommand]
    private void PrevArea()
    {
        StepArea(-1);
    }

    private void StepArea(int delta)
    {
        IReadOnlyList<AreaDescriptor> all = AreaCatalog.All;
        int count = all.Count;
        AreaId? current = GetActiveAreaId();
        int currentIndex = current.HasValue
            ? all.ToList().FindIndex(d => d.Id == current.Value)
            : -1;
        int nextIndex;
        if (currentIndex < 0)
        {
            nextIndex = delta > 0 ? 0 : count - 1;
        }
        else
        {
            nextIndex = ((currentIndex + delta) % count + count) % count;
        }
        DrillTo(all[nextIndex].Id);
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
        UrlTester.RefreshFromCurrent();
        OnPropertyChanged(nameof(HasDrafts));
    }

    private void OnWatcherChanged()
    {
        if (HasDrafts)
        {
            if (PendingConflict is null)
            {
                PendingConflict = new ConflictPromptViewModel(_persister.ConfigPath);
                _notifier.Show(
                    "AskMeFirst: config changed on disk",
                    "You have unsaved drafts. Keep them or load the new file.");
            }
            return;
        }
        _persister.Reload();
    }

    public void ResolveConflictAndReload()
    {
        ConflictPromptViewModel? prompt = PendingConflict;
        if (prompt is null)
        {
            return;
        }
        PendingConflict = null;
        if (prompt.DiscardDrafts)
        {
            DiscardAllDrafts();
            _persister.Reload();
        }
    }

    private void DiscardAllDrafts()
    {
        if (ActiveDetail is AreaDetailViewModel area)
        {
            foreach (RowViewModelBase row in area.Rows)
            {
                if (row is DraftableRowViewModel draftable)
                {
                    draftable.DiscardCommand.Execute(null);
                }
            }
        }
        else if (ActiveDetail is EntityDetailViewModel entity)
        {
            foreach (DraftableRowViewModel row in entity.Rows)
            {
                row.DiscardCommand.Execute(null);
            }
        }
    }

    public enum CloseDecision
    {
        Proceed,
        Cancel,
        SaveAndClose,
        DiscardAndClose,
    }

    public CloseDecision EvaluateCloseRequest()
    {
        if (!HasDrafts)
        {
            return CloseDecision.Proceed;
        }
        return CloseDecision.Cancel;
    }

    public CloseDecision ApplyCloseDecision(CloseDecision decision)
    {
        if (decision == CloseDecision.SaveAndClose)
        {
            if (!SaveAllDrafts())
            {
                return CloseDecision.Cancel;
            }
            return CloseDecision.Proceed;
        }
        if (decision == CloseDecision.DiscardAndClose)
        {
            DiscardAllDrafts();
            return CloseDecision.Proceed;
        }
        if (decision == CloseDecision.Proceed)
        {
            return CloseDecision.Proceed;
        }
        return CloseDecision.Cancel;
    }

    private bool SaveAllDrafts()
    {
        if (ActiveDetail is AreaDetailViewModel area)
        {
            DraftableRowViewModel[] snapshot = [.. RowsWithDrafts(area.Rows)];
            foreach (DraftableRowViewModel draftable in snapshot)
            {
                draftable.SaveCommand.Execute(null);
            }
            return snapshot.Length > 0;
        }
        if (ActiveDetail is EntityDetailViewModel entity)
        {
            DraftableRowViewModel[] snapshot = [.. RowsWithDrafts(entity.Rows)];
            foreach (DraftableRowViewModel draftable in snapshot)
            {
                draftable.SaveCommand.Execute(null);
            }
            return snapshot.Length > 0;
        }
        return false;
    }

    private static IEnumerable<DraftableRowViewModel> RowsWithDrafts(IEnumerable<RowViewModelBase> rows)
    {
        foreach (RowViewModelBase row in rows)
        {
            if (row is DraftableRowViewModel draftable && draftable.HasDraft)
            {
                yield return draftable;
            }
        }
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
            AreaId.Browsers => new BrowserDetailViewModel(this, _persister, _discovery),
            AreaId.Profiles => new ProfileDetailViewModel(this, _persister),
            AreaId.Rules => new RuleDetailViewModel(this, _persister),
            AreaId.Usage => new UsageDetailViewModel(this, _recentPicksReader),
            AreaId.Suggestions => new SuggestionsDetailViewModel(this, _recentPicksReader),
            AreaId.Unshortener => new StringListDetailViewModel(areaId, this, StringListKind.UnshortenDomain, "Unshortener", _persister),
            AreaId.Tracking => new StringListDetailViewModel(areaId, this, StringListKind.TrackingParam, "Tracking params", _persister),
            AreaId.Cache => new CacheDetailViewModel(this, _persister, _discovery),
            _ => new NoFormsDetailViewModel(areaId, this, $"Detail view for {areaId}."),
        };
    }
}
