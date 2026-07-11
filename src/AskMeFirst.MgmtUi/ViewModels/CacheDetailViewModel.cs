using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class CacheDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;
    private readonly IBrowserDiscovery _discovery;
    private string _cachePath = "";
    private string _cacheFileSize = "0 bytes";
    private string _lastRefreshed = "never";
    private int _discoveredBrowserCount;

    public CacheDetailViewModel(
        MgmtUiWindowViewModel parent,
        IConfigPersister persister,
        IBrowserDiscovery discovery)
        : base(AreaId.Cache, parent)
    {
        _persister = persister;
        _discovery = discovery;
        Placeholder = "Discovery cache uninitialized.";
    }

    public override string Placeholder { get; }

    public string CachePath => _cachePath;

    public string CacheFileSize => _cacheFileSize;

    public string LastRefreshed => _lastRefreshed;

    public int DiscoveredBrowserCount => _discoveredBrowserCount;

    public override string? ExtraSectionTitle => "Discovered browsers";

    public override void Rebuild(IConfigPersister persister)
    {
        _cachePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(persister.ConfigPath) ?? ".",
            "discovery-cache.json");
        _cacheFileSize = System.IO.File.Exists(_cachePath)
            ? $"{new System.IO.FileInfo(_cachePath).Length} bytes"
            : "no cache file";
        ExtraRows.Clear();
        foreach (DetectedBrowser detected in _discovery.Browsers)
        {
            ExtraRows.Add(new DetectedBrowserRowViewModel(detected, persister, withAddButton: false));
        }
        _discoveredBrowserCount = ExtraRows.Count;
        _lastRefreshed = _discovery.LastRefreshedAt is null
            ? "never"
            : _discovery.LastRefreshedAt.Value.LocalDateTime.ToString(
                "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(CachePath));
        OnPropertyChanged(nameof(CacheFileSize));
        OnPropertyChanged(nameof(LastRefreshed));
        OnPropertyChanged(nameof(DiscoveredBrowserCount));
        NotifyRowsChanged();
    }

    [RelayCommand]
    private void Refresh()
    {
        _discovery.Refresh();
        Rebuild(_persister);
    }
}
