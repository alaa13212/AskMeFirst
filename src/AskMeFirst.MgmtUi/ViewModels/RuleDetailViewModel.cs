using System.Windows.Input;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class RuleDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;
    private int _newRuleSeq;

    public RuleDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Rules, parent)
    {
        _persister = persister;
        Placeholder = "No rules configured yet. Suggestions will land here as you pick.";
        AddCommand = new RelayCommand(AddRule);
    }

    public override string Placeholder { get; }

    public override bool SupportsAdd => true;

    public override ICommand? AddCommand { get; }

    private void AddRule()
    {
        _persister.Save(c =>
        {
            int seq = System.Threading.Interlocked.Increment(ref _newRuleSeq);
            Rule fresh = new()
            {
                Name = seq == 1 ? "New rule" : $"New rule ({seq})",
                Priority = 100,
                When = new RuleWhen { UrlMatchesAny = ["example.com"] },
                Then = new RuleThen { Browser = "firefox" },
                Origin = "user",
            };
            return c with { Rules = [.. c.Rules, fresh] };
        });
    }

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
