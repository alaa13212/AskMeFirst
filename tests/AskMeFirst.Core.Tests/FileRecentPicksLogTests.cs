using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Config;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class FileRecentPicksLogTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public FileRecentPicksLogTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-picks-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Append_AppendsOneLinePerEntry()
    {
        FileRecentPicksLog log = new(_configPath, _logger);

        log.Append(MakeEntry("https://a.example.com"));
        log.Append(MakeEntry("https://b.example.com"));

        string path = Path.Combine(_tempDir, "recent-picks.jsonl");
        Assert.True(File.Exists(path));
        string[] lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Contains("https://a.example.com", lines[0]);
        Assert.Contains("https://b.example.com", lines[1]);
        Assert.Contains("\"RuleWritten\":true", lines[0]);
    }

    [Fact]
    public void Append_FileMissing_CreatesDirectoryAndFile()
    {
        string nestedDir = Path.Combine(_tempDir, "deep", "nested");
        string nestedConfig = Path.Combine(nestedDir, "config.json");
        FileRecentPicksLog log = new(nestedConfig, _logger);

        log.Append(MakeEntry("https://example.com"));

        string expectedPath = Path.Combine(nestedDir, "recent-picks.jsonl");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void Append_AppendsAreConcurrentSafe_AppendOnlyOnExistingFile()
    {
        FileRecentPicksLog log = new(_configPath, _logger);
        log.Append(MakeEntry("https://first.example.com"));

        string path = Path.Combine(_tempDir, "recent-picks.jsonl");
        File.SetLastWriteTime(path, DateTime.UtcNow.AddSeconds(-1));

        log.Append(MakeEntry("https://second.example.com"));

        Assert.Equal(2, File.ReadAllLines(path).Length);
    }

    [Fact]
    public void NoOpRecentPicksLog_DoesNothing()
    {
        NoOpRecentPicksLog log = new();
        log.Append(MakeEntry("https://example.com"));
        Assert.False(File.Exists(Path.Combine(_tempDir, "recent-picks.jsonl")));
    }

    private static RecentPickEntry MakeEntry(string url, bool ruleWritten = true)
    {
        return new RecentPickEntry(
            Timestamp: DateTimeOffset.UtcNow,
            Url: new Uri(url),
            SourceApp: "slack",
            BrowserId: "firefox-work",
            ProfileId: "firefox-work-profile",
            RuleWritten: ruleWritten);
    }
}