using AskMeFirst.Core.Audit;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class SuggestionsDetailViewModel : AreaDetailViewModel
{
    private readonly IRecentPicksReader _reader;

    public SuggestionsDetailViewModel(
        MgmtUiWindowViewModel parent,
        IRecentPicksReader reader)
        : base(AreaId.Suggestions, parent)
    {
        _reader = reader;
        Placeholder = "Open a few URLs in different browsers — suggestions show up here.";
    }

    public override string Placeholder { get; }

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        IReadOnlyList<RecentPickEntry> picks = _reader.ReadAll();
        Dictionary<string, (int Count, Dictionary<string, int> Browsers)> groups = new(StringComparer.OrdinalIgnoreCase);
        foreach (RecentPickEntry pick in picks)
        {
            string host = pick.Url?.Host ?? "";
            if (string.IsNullOrEmpty(host))
            {
                continue;
            }
            if (!groups.TryGetValue(host, out (int Count, Dictionary<string, int> Browsers) entry))
            {
                entry = (0, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
            }
            entry.Count++;
            entry.Browsers[pick.BrowserId] = entry.Browsers.GetValueOrDefault(pick.BrowserId) + 1;
            groups[host] = entry;
        }
        List<(string Host, int Count, string MostCommon)> top = groups
            .Select(kvp => (
                kvp.Key,
                kvp.Value.Count,
                kvp.Value.Browsers.OrderByDescending(kv => kv.Value).First().Key))
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToList();
        foreach ((string Host, int Count, string MostCommon) tuple in top)
        {
            Rows.Add(new SuggestionEntryViewModel(tuple.Host, tuple.Count, tuple.MostCommon, persister));
        }
        NotifyRowsChanged();
    }
}
