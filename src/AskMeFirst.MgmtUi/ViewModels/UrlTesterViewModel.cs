using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class UrlTesterViewModel : ObservableObject
{
    private readonly IConfigPersister _persister;
    private readonly IRulePreviewService _preview;
    private readonly IBrowserDiscovery _discovery;
    private readonly IUrlLauncher _urlLauncher;
    private readonly INotifier _notifier;
    private RulePreviewResult _lastResult = new(false, null);

    public UrlTesterViewModel(
        IConfigPersister persister,
        IRulePreviewService preview,
        IBrowserDiscovery discovery,
        IUrlLauncher urlLauncher,
        INotifier notifier)
    {
        _persister = persister;
        _preview = preview;
        _discovery = discovery;
        _urlLauncher = urlLauncher;
        _notifier = notifier;
        _matchSummary = "—";
    }

    [ObservableProperty]
    private string _urlInput = "";

    [ObservableProperty]
    private string _matchSummary = "—";

    [ObservableProperty]
    private bool _canLaunch;

    [ObservableProperty]
    private string _launchStatus = "";

    partial void OnUrlInputChanged(string value)
    {
        Update();
    }

    public IConfigPersister Persister => _persister;

    public void RefreshFromCurrent()
    {
        Update();
    }

    [RelayCommand]
    private void Open()
    {
        if (!_lastResult.HasMatch || _lastResult.MatchedRule is null)
        {
            LaunchStatus = "No rule to launch against.";
            return;
        }
        if (!Uri.TryCreate(UrlInput, UriKind.Absolute, out Uri? url))
        {
            LaunchStatus = "URL not absolute.";
            return;
        }
        string browserId = _lastResult.MatchedRule.Then.Browser;
        DetectedBrowser? detected = null;
        foreach (DetectedBrowser db in _discovery.Browsers)
        {
            if (string.Equals(db.Browser.Id, browserId, StringComparison.OrdinalIgnoreCase))
            {
                detected = db;
                break;
            }
        }
        if (detected is null)
        {
            LaunchStatus = $"Browser '{browserId}' is not installed.";
            _notifier.Show("AskMeFirst: browser missing",
                $"Rule '{_lastResult.MatchedRule.Name}' references '{browserId}' which isn't installed.");
            return;
        }
        try
        {
            _urlLauncher.Launch(detected.Browser, url);
            LaunchStatus = $"Launched in {detected.Browser.DisplayName}";
        }
        catch (Exception ex)
        {
            LaunchStatus = $"Launch failed: {ex.Message}";
            _notifier.Show("AskMeFirst: launch failed", ex.Message);
        }
    }

    private void Update()
    {
        if (string.IsNullOrWhiteSpace(UrlInput))
        {
            MatchSummary = "Type a URL above to preview which rule would match.";
            _lastResult = new RulePreviewResult(false, null);
            CanLaunch = false;
            return;
        }
        if (!Uri.TryCreate(UrlInput, UriKind.Absolute, out Uri? url))
        {
            MatchSummary = "Not a valid URL.";
            _lastResult = new RulePreviewResult(false, null);
            CanLaunch = false;
            return;
        }
        RulePreviewResult result = _preview.Preview(url, _persister.Current);
        _lastResult = result;
        MatchSummary = result.HasMatch && result.MatchedRule is not null
            ? $"Match · {result.MatchedRule.Name} (priority {result.MatchedRule.Priority}, → {result.MatchedRule.Then.Browser})"
            : "No rule — picker would fire.";
        CanLaunch = result.HasMatch;
    }
}
