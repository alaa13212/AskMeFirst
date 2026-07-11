using System.Collections.ObjectModel;
using System.Windows.Input;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class BrowserDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;
    private readonly IBrowserDiscovery _discovery;
    private int _newBrowserSeq;

    public BrowserDetailViewModel(
        MgmtUiWindowViewModel parent,
        IConfigPersister persister,
        IBrowserDiscovery discovery)
        : base(AreaId.Browsers, parent)
    {
        _persister = persister;
        _discovery = discovery;
        Placeholder = "No browsers in config yet. Pick from Detected below or click + Add.";
        AddCommand = new RelayCommand(AddBrowser);
    }

    public override string Placeholder { get; }

    public override bool SupportsAdd => true;

    public override ICommand? AddCommand { get; }

    public override bool ShowRefresh => true;

    public override ICommand? RefreshCommand => RefreshBrowsersCommand;

    public override string? ExtraSectionTitle => "Discovered (not in config yet)";

    private void AddBrowser()
    {
        _persister.Save(c =>
        {
            int seq = System.Threading.Interlocked.Increment(ref _newBrowserSeq);
            string id = $"new-browser-{seq}";
            BrowserSpec fresh = new()
            {
                Id = id,
                DisplayName = $"New browser ({seq})",
                Executable = "auto",
            };
            return c with { Browsers = [.. c.Browsers, fresh] };
        });
    }

    [RelayCommand]
    private void RefreshBrowsers()
    {
        _discovery.Refresh();
        Rebuild(_persister);
    }

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        foreach (BrowserSpec browser in persister.Current.Browsers)
        {
            Rows.Add(new BrowserRowViewModel(browser, persister));
        }
        ExtraRows.Clear();
        HashSet<string> configured = [.. persister.Current.Browsers.Select(b => b.Id)];
        foreach (DetectedBrowser detected in _discovery.Browsers)
        {
            if (configured.Contains(detected.Browser.Id))
            {
                continue;
            }
            ExtraRows.Add(new DetectedBrowserRowViewModel(detected, persister, withAddButton: true));
        }
        NotifyRowsChanged();
    }
}
