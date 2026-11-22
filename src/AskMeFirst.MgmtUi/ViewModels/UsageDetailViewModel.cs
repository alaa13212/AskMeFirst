using System.Collections.ObjectModel;
using AskMeFirst.Core.Audit;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class UsageDetailViewModel : AreaDetailViewModel
{
    private readonly IRecentPicksReader _reader;

    public UsageDetailViewModel(
        MgmtUiWindowViewModel parent,
        IRecentPicksReader reader)
        : base(AreaId.Usage, parent)
    {
        _reader = reader;
        Placeholder = "No recent picks recorded yet.";
    }

    public override string Placeholder { get; }

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        foreach (RecentPickEntry entry in _reader.ReadAll())
        {
            Rows.Add(new UsageEntryViewModel(entry));
        }
        NotifyRowsChanged();
    }
}
