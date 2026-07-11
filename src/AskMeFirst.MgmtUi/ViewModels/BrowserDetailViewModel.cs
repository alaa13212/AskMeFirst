using System.Windows.Input;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class BrowserDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;
    private int _newBrowserSeq;

    public BrowserDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Browsers, parent)
    {
        _persister = persister;
        Placeholder = "No browsers in config yet. Use the discovery pane to add one.";
        AddCommand = new RelayCommand(AddBrowser);
    }

    public override string Placeholder { get; }

    public override bool SupportsAdd => true;

    public override ICommand? AddCommand { get; }

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

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        foreach (BrowserSpec browser in persister.Current.Browsers)
        {
            Rows.Add(new BrowserRowViewModel(browser, persister));
        }
        NotifyRowsChanged();
    }
}
