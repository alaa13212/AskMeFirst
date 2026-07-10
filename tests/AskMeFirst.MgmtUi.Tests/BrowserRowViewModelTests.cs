using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class BrowserRowViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public BrowserRowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-browsers-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Constructor_CopiesSource_NoDraft()
    {
        BrowserRowViewModel row = new(MakeBrowser("firefox", "Firefox", "auto"), Persister());

        Assert.Equal("Firefox", row.DisplayName);
        Assert.Equal("auto", row.Executable);
        Assert.False(row.HasDraft);
        Assert.False(row.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Save_PersistsDraft_AndClears()
    {
        SeedConfig(MakeBrowser("firefox", "Firefox", "auto"));
        JsonConfigPersister persister = new(_configPath, _logger);
        BrowserRowViewModel row = new(persister.Current.Browsers[0], persister);

        row.DisplayName = "Firefox Dev";
        row.Executable = "/usr/bin/firefox";
        row.SaveCommand.Execute(null);

        Assert.True(string.IsNullOrEmpty(row.LastError));
        Assert.False(row.HasDraft);
        Assert.Equal("Firefox Dev", persister.Current.Browsers[0].DisplayName);
        Assert.Equal("/usr/bin/firefox", persister.Current.Browsers[0].Executable);
    }

    [Fact]
    public void Discard_RevertsDrafts()
    {
        SeedConfig(MakeBrowser("firefox", "Firefox", "auto"));
        JsonConfigPersister persister = new(_configPath, _logger);
        BrowserRowViewModel row = new(persister.Current.Browsers[0], persister);

        row.DisplayName = "Firefox Dev";
        row.DiscardCommand.Execute(null);

        Assert.False(row.HasDraft);
        Assert.Equal("Firefox", row.DisplayName);
    }

    private JsonConfigPersister Persister() => new(_configPath, _logger);

    private void SeedConfig(params BrowserSpec[] browsers)
    {
        AppConfig seed = new() { Browsers = [.. browsers] };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private static BrowserSpec MakeBrowser(string id, string displayName, string executable)
    {
        return new BrowserSpec { Id = id, DisplayName = displayName, Executable = executable };
    }
}
