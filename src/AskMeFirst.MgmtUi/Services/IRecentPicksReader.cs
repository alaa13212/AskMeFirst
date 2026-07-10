using AskMeFirst.Core.Audit;

namespace AskMeFirst.MgmtUi.Services;

public interface IRecentPicksReader
{
    IReadOnlyList<RecentPickEntry> ReadAll();
}
