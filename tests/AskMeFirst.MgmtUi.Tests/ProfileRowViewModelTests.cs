using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class ProfileRowViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public ProfileRowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-profiles-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Constructor_NoDraft_DisplayNameAndPinnedFromSource()
    {
        ProfileRowViewModel row = new(MakeProfile(pinned: false, displayName: "Work"), Persister());

        Assert.Equal("Work", row.DisplayName);
        Assert.False(row.Pinned);
        Assert.False(row.HasDraft);
    }

    [Fact]
    public void TogglePinned_AutosavesImmediately()
    {
        SeedConfig(MakeProfile(pinned: false, displayName: "Work"));
        JsonConfigPersister persister = new(_configPath, _logger);
        ProfileRowViewModel row = new(persister.Current.Profiles[0], persister);

        row.Pinned = true;

        Assert.True(persister.Current.Profiles[0].Pinned);
        Assert.False(row.HasDraft);
    }

    [Fact]
    public void DisplayName_EditSetDraft_SavePersists()
    {
        SeedConfig(MakeProfile(pinned: false, displayName: "Work"));
        JsonConfigPersister persister = new(_configPath, _logger);
        ProfileRowViewModel row = new(persister.Current.Profiles[0], persister);

        row.DisplayName = "Work-2026";
        Assert.True(row.HasDraft);
        row.SaveCommand.Execute(null);

        Assert.False(row.HasDraft);
        Assert.Equal("Work-2026", persister.Current.Profiles[0].DisplayName);
    }

    private JsonConfigPersister Persister() => new(_configPath, _logger);

    private void SeedConfig(params ProfileSpec[] profiles)
    {
        AppConfig seed = new() { Profiles = [.. profiles] };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private static ProfileSpec MakeProfile(bool pinned, string displayName)
    {
        return new ProfileSpec
        {
            Id = "firefox-1",
            BrowserId = "firefox",
            Name = "firefox-1",
            DisplayName = displayName,
            Pinned = pinned,
        };
    }
}
