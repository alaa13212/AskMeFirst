using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class CacheDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;

    public CacheDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Cache, parent)
    {
        _persister = persister;
        Placeholder = "Discovery cache uninitialized.";
    }

    public override string Placeholder { get; }

    [ObservableProperty]
    private string _cachePath = "";

    [ObservableProperty]
    private string _cacheFileSize = "0 bytes";

    public override void Rebuild(IConfigPersister persister)
    {
        CachePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(persister.ConfigPath) ?? ".",
            "discovery-cache.json");
        CacheFileSize = System.IO.File.Exists(CachePath)
            ? $"{new System.IO.FileInfo(CachePath).Length} bytes"
            : "no cache file";
    }
}
