using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class UrlTesterViewModel : ObservableObject
{
    private readonly IConfigPersister _persister;
    private readonly IRulePreviewService _preview;

    public UrlTesterViewModel(IConfigPersister persister, IRulePreviewService preview)
    {
        _persister = persister;
        _preview = preview;
        _matchSummary = "—";
    }

    [ObservableProperty]
    private string _urlInput = "";

    [ObservableProperty]
    private string _matchSummary = "—";

    partial void OnUrlInputChanged(string value)
    {
        Update();
    }

    public IConfigPersister Persister => _persister;

    public void RefreshFromCurrent()
    {
        Update();
    }

    private void Update()
    {
        if (string.IsNullOrWhiteSpace(UrlInput))
        {
            MatchSummary = "Type a URL above to preview which rule would match.";
            return;
        }
        if (!Uri.TryCreate(UrlInput, UriKind.Absolute, out Uri? url))
        {
            MatchSummary = "Not a valid URL.";
            return;
        }
        RulePreviewResult result = _preview.Preview(url, _persister.Current);
        MatchSummary = result.HasMatch && result.MatchedRule is not null
            ? $"Match · {result.MatchedRule.Name} (priority {result.MatchedRule.Priority}, → {result.MatchedRule.Then.Browser})"
            : "No rule — picker would fire.";
    }
}
