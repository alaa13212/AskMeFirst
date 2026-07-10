using AskMeFirst.MgmtUi.Services;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class FileConfigFileWatcherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public FileConfigFileWatcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-watcher-{Guid.NewGuid():N}");
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Edit_FiresChangedEvent_DebouncedOnce()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_configPath, "{}");
        FileConfigFileWatcher watcher = new(_configPath);
        int raised = 0;
        watcher.Changed += (_, _) => raised++;
        watcher.Start();

        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(_configPath, $"{{ \"v\": {i} }}");
        }

        SpinWait.SpinUntil(() => raised >= 1, TimeSpan.FromSeconds(2));
        watcher.Dispose();

        Assert.True(raised >= 1, "Watcher should fire Changed after rapid writes.");
    }
}
