using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class BrowserDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;

    public BrowserDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Browsers, parent)
    {
        _persister = persister;
        Placeholder = "No browsers in config yet.";
    }

    public override string Placeholder { get; }

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
