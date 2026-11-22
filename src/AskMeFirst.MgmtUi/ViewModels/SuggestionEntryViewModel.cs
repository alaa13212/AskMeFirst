using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class SuggestionEntryViewModel : AutosaveRowViewModel
{
    private readonly IConfigPersister _persister;
    private readonly string _host;
    private readonly int _visitCount;
    private readonly string _mostCommonBrowserId;

    public SuggestionEntryViewModel(
        string host,
        int visitCount,
        string mostCommonBrowserId,
        IConfigPersister persister)
        : base(persister)
    {
        _persister = persister;
        _host = host;
        _visitCount = visitCount;
        _mostCommonBrowserId = mostCommonBrowserId;
    }

    public string Host => _host;

    public int VisitCount => _visitCount;

    public string MostCommonBrowser => _mostCommonBrowserId;

    public override string Label => $"{_host} — picked {_visitCount}× (mostly {_mostCommonBrowserId})";

    [RelayCommand]
    private void AddRule()
    {
        _persister.Save(c =>
        {
            Rule rule = new()
            {
                Name = $"Pick {_host} in {_mostCommonBrowserId}",
                Priority = 50,
                When = new RuleWhen { UrlMatchesAny = [_host] },
                Then = new RuleThen { Browser = _mostCommonBrowserId },
                Origin = "remember",
            };
            return c with { Rules = [.. c.Rules, rule] };
        });
    }
}
