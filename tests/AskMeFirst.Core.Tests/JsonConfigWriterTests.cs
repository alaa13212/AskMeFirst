using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class JsonConfigWriterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public JsonConfigWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-cfg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void AppendRule_ToMissingFile_CreatesFileWithRule()
    {
        JsonConfigWriter writer = new(_configPath, _logger);

        writer.AppendRule(SampleRule("Slack → firefox-work", priority: 50));

        Assert.True(File.Exists(_configPath));
        AppConfig loaded = ConfigLoader.LoadFromFile(_configPath);
        Assert.Single(loaded.Rules);
        Assert.Equal("Slack → firefox-work", loaded.Rules[0].Name);
        Assert.Equal("remember", loaded.Rules[0].Origin);
    }

    [Fact]
    public void AppendRule_ToExistingFile_AddsToRulesList_KeepsOtherFields()
    {
        AppConfig seed = new()
        {
            Settings = new Settings { StripTracking = false },
            Rules = [UserRule("Existing user rule")],
        };
        ConfigLoaderLoadAndWrite(seed);

        JsonConfigWriter writer = new(_configPath, _logger);
        writer.AppendRule(SampleRule("Picked Slack", priority: 50));

        AppConfig loaded = ConfigLoader.LoadFromFile(_configPath);
        Assert.Equal(2, loaded.Rules.Count);
        Assert.Equal("Existing user rule", loaded.Rules[0].Name);
        Assert.Equal("user", loaded.Rules[0].Origin);
        Assert.Equal("Picked Slack", loaded.Rules[1].Name);
        Assert.Equal("remember", loaded.Rules[1].Origin);
        Assert.False(loaded.Settings.StripTracking);
    }

    [Fact]
    public void AppendRule_AtomicReplace_LeavesNoTempFileOnSuccess()
    {
        JsonConfigWriter writer = new(_configPath, _logger);

        writer.AppendRule(SampleRule("first"));
        writer.AppendRule(SampleRule("second"));

        Assert.True(File.Exists(_configPath));
        Assert.False(File.Exists(_configPath + ".tmp"));
    }

    [Fact]
    public void AppendRule_JsoncInput_CommentsStrippedOnRewrite()
    {
        File.WriteAllText(_configPath, """
            {
              // top-level comment
              "settings": { "StripTracking": true },
              "rules": []
            }
            """);

        JsonConfigWriter writer = new(_configPath, _logger);
        writer.AppendRule(SampleRule("first"));

        string content = File.ReadAllText(_configPath);
        Assert.DoesNotContain("//", content);
        Assert.DoesNotContain(".tmp", _configPath);
    }

    private void ConfigLoaderLoadAndWrite(AppConfig seed)
    {
        using FileStream fs = File.Create(_configPath);
        System.Text.Json.JsonSerializer.Serialize(fs, seed, ConfigJsonContext.Default.AppConfig);
    }

    private static Rule UserRule(string name)
    {
        return SampleRule(name, priority: 100, origin: "user");
    }

    private static Rule SampleRule(string name, int priority = 50, string origin = "remember")
    {
        Browser browser = TestBrowser.Make("firefox-work", "Firefox Work", "/ff");
        return new Rule
        {
            Name = name,
            Priority = priority,
            When = new RuleWhen { ProcessIn = ["slack"] },
            Then = new RuleThen { Browser = browser.Id },
            Origin = origin,
        };
    }
}