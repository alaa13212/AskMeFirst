using AskMeFirst.Core.Audit;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class UsageEntryViewModel : RowViewModelBase
{
    public UsageEntryViewModel(RecentPickEntry entry)
    {
        Entry = entry;
    }

    public RecentPickEntry Entry { get; }

    public string Url => Entry.Url?.ToString() ?? "";

    public string Browser => Entry.BrowserId;

    public string Profile => Entry.ProfileId ?? "—";

    public string When => Entry.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    public override string Label => $"{When} · {Browser}{(string.IsNullOrEmpty(Entry.ProfileId) ? "" : "/" + Profile)} · {Url}";
}
