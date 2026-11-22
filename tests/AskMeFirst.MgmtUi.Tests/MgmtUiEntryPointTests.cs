using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class MgmtUiEntryPointTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public MgmtUiEntryPointTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-entrypoint-{Guid.NewGuid():N}");
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void BuildComposition_MissingConfig_WritesEmbeddedDefault()
    {
        MgmtUiEntryPointComposition composition = MgmtUiEntryPoint.BuildComposition(_configPath);

        Assert.True(File.Exists(_configPath));
        AppConfig loaded = ConfigLoader.LoadFromFile(_configPath);
        Assert.Equal(composition.ConfigPersister.Current.Rules.Count, loaded.Rules.Count);
    }

    [Fact]
    public void BuildComposition_ExistingConfig_LeavesAlone()
    {
        AppConfig seed = new() { Settings = new Settings { StripTracking = false } };
        ConfigLoader.WriteAtomic(seed, _configPath);
        long sizeBefore = new FileInfo(_configPath).Length;

        MgmtUiEntryPoint.BuildComposition(_configPath);

        AppConfig loaded = ConfigLoader.LoadFromFile(_configPath);
        Assert.False(loaded.Settings.StripTracking);
        Assert.Equal(sizeBefore, new FileInfo(_configPath).Length);
    }
}
