using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class DraftableStringRowViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public DraftableStringRowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-strings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Save_ReplacesValueAtIndex()
    {
        SeedUnshortener("foo.com", "bar.com");
        JsonConfigPersister persister = new(_configPath, _logger);
        DraftableStringRowViewModel row = new(0, "foo.com", StringListKind.UnshortenDomain, persister);

        row.Value = "foo.example.com";
        row.SaveCommand.Execute(null);

        Assert.Equal(["foo.example.com", "bar.com"], persister.Current.UnshortenDomainsExtra);
    }

    [Fact]
    public void Save_BlankValue_RemovesFromList()
    {
        SeedUnshortener("foo.com", "bar.com");
        JsonConfigPersister persister = new(_configPath, _logger);
        DraftableStringRowViewModel row = new(1, "bar.com", StringListKind.UnshortenDomain, persister);

        row.Value = "";
        row.SaveCommand.Execute(null);

        Assert.Single(persister.Current.UnshortenDomainsExtra);
        Assert.Equal("foo.com", persister.Current.UnshortenDomainsExtra[0]);
    }

    [Fact]
    public void DeleteCommand_RemovesAtIndex_AndFiresChanged()
    {
        SeedTracking("utm_a", "utm_b", "fbclid");
        JsonConfigPersister persister = new(_configPath, _logger);
        DraftableStringRowViewModel row = new(1, "utm_b", StringListKind.TrackingParam, persister);

        row.DeleteCommand.Execute(null);

        Assert.Equal(2, persister.Current.TrackingParamsExtra.Count);
        Assert.Equal("utm_a", persister.Current.TrackingParamsExtra[0]);
        Assert.Equal("fbclid", persister.Current.TrackingParamsExtra[1]);
    }

    private void SeedUnshortener(params string[] domains)
    {
        AppConfig seed = new() { UnshortenDomainsExtra = domains };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private void SeedTracking(params string[] names)
    {
        AppConfig seed = new() { TrackingParamsExtra = names };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }
}
