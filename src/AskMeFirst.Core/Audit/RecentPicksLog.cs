namespace AskMeFirst.Core.Audit;

public sealed record RecentPickEntry(
    DateTimeOffset Timestamp,
    Uri Url,
    string BrowserId,
    string? ProfileId,
    bool RuleWritten);

public interface IRecentPicksLog
{
    void Append(RecentPickEntry entry);
}

public sealed class NoOpRecentPicksLog : IRecentPicksLog
{
    public void Append(RecentPickEntry entry)
    {
    }
}