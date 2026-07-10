using AskMeFirst.Core.Audit;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.Tests;

internal sealed class EmptyRecentPicksReader : IRecentPicksReader
{
    public IReadOnlyList<RecentPickEntry> ReadAll() => [];
}
