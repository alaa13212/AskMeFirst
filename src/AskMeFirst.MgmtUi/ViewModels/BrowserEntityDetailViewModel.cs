using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class BrowserEntityDetailViewModel : EntityDetailViewModel
{
    private readonly IConfigPersister _persister;

    public BrowserEntityDetailViewModel(
        string browserId,
        MgmtUiWindowViewModel parent,
        IConfigPersister persister)
        : base(browserId, parent)
    {
        _persister = persister;
        ParentArea = AreaId.Browsers;
        Title = $"Browser · {browserId}";
    }

    public override string Label => EntityId;

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        BrowserSpec? browser = Find();
        if (browser is null)
        {
            NotifyErrors($"Browser '{EntityId}' no longer exists.");
            return;
        }
        NotifyErrors(null);
        Rows.Add(new BrowserRowViewModel(browser, persister));
    }

    private BrowserSpec? Find()
    {
        foreach (BrowserSpec browser in _persister.Current.Browsers)
        {
            if (browser.Id == EntityId)
            {
                return browser;
            }
        }
        return null;
    }
}
