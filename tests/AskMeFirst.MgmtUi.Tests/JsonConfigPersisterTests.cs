using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class JsonConfigPersisterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public JsonConfigPersisterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-mgmtui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Ctor_MissingFile_FallsBackToEmbeddedDefault()
    {
        JsonConfigPersister persister = new(_configPath, _logger);

        Assert.NotNull(persister.Current);
        Assert.False(File.Exists(_configPath));
    }

    [Fact]
    public void Save_MutateFunc_WritesUpdatedConfig()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        Rule rule = MakeRule("first");
        int raised = 0;
        persister.Changed += (_, _) => raised++;

        SaveOutcome outcome = persister.Save(c => c with { Rules = [rule] });

        Assert.True(outcome.Ok);
        Assert.Null(outcome.Error);
        Assert.Single(persister.Current.Rules);
        Assert.Equal("first", persister.Current.Rules[0].Name);
        Assert.True(File.Exists(_configPath));
        Assert.False(File.Exists(_configPath + ".tmp"));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Save_MutateFuncThrows_ReturnsFailureAndLeavesFileUntouched()
    {
        File.WriteAllText(_configPath, """{"settings":{},"rules":[],"browsers":[],"profiles":[]}""");
        string original = File.ReadAllText(_configPath);
        JsonConfigPersister persister = new(_configPath, _logger);

        SaveOutcome outcome = persister.Save(_ => throw new InvalidOperationException("nope"));

        Assert.False(outcome.Ok);
        Assert.Equal("nope", outcome.Error);
        Assert.Empty(persister.Current.Rules);
        Assert.Equal(original, File.ReadAllText(_configPath));
    }

    [Fact]
    public void Reload_RereadsFromDisk_AndFiresChanged()
    {
        JsonConfigPersister persister = new(_configPath, _logger);
        File.WriteAllText(_configPath, """
            {
              "settings": {},
              "rules": [
                { "name": "on-disk", "priority": 10, "when": { "urlMatchesAny": ["x.com"] }, "then": { "browser": "firefox" }, "origin": "user" }
              ],
              "browsers": [],
              "profiles": []
            }
            """);

        int raised = 0;
        persister.Changed += (_, _) => raised++;
        persister.Reload();

        Assert.Single(persister.Current.Rules);
        Assert.Equal("on-disk", persister.Current.Rules[0].Name);
        Assert.Equal(1, raised);
    }

    private static Rule MakeRule(string name)
    {
        return new Rule
        {
            Name = name,
            Priority = 50,
            When = new RuleWhen { UrlMatchesAny = ["example.com"] },
            Then = new RuleThen { Browser = "firefox" },
            Origin = "user",
        };
    }
}
