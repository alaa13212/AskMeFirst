using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class RuleDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;

    public RuleDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Rules, parent)
    {
        _persister = persister;
        Placeholder = "No rules configured yet. Suggestions will land here as you pick.";
    }

    public override string Placeholder { get; }

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        foreach (Rule rule in persister.Current.Rules)
        {
            Rows.Add(new RuleRowViewModel(rule, persister));
        }
        NotifyRowsChanged();
    }
}
