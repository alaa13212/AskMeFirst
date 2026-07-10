using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class RuleRowViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();

    public RuleRowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-rules-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Constructor_CopiesValuesFromSource_NoDraft()
    {
        Rule source = MakeRule("Slack → firefox", priority: 50, urlRegex: ".*slack.*");

        RuleRowViewModel row = new(source, Persister());

        Assert.Equal("Slack → firefox", row.Name);
        Assert.Equal(50, row.Priority);
        Assert.Equal(".*slack.*", row.UrlRegex);
        Assert.False(row.HasDraft);
        Assert.False(row.SaveCommand.CanExecute(null));
        Assert.False(row.DiscardCommand.CanExecute(null));
    }

    [Fact]
    public void EditAnyField_SetsHasDraft_AndEnablesCommands()
    {
        Rule source = MakeRule("name", priority: 50, urlRegex: "");
        RuleRowViewModel row = new(source, Persister());

        row.Name = "renamed";
        Assert.True(row.HasDraft);
        Assert.True(row.SaveCommand.CanExecute(null));
        Assert.True(row.DiscardCommand.CanExecute(null));
    }

    [Fact]
    public void Save_PersistsDraft_AndClearsDraftState()
    {
        SeedConfig(MakeRule("name", priority: 50, urlRegex: ""));
        JsonConfigPersister persister = new(_configPath, _logger);
        Rule source = persister.Current.Rules[0];
        RuleRowViewModel row = new(source, persister);

        row.Name = "renamed";
        row.Priority = 75;
        row.UrlRegex = ".*github.*";
        row.SaveCommand.Execute(null);

        Assert.True(string.IsNullOrEmpty(row.LastError));
        Assert.False(row.HasDraft);
        Assert.False(row.SaveCommand.CanExecute(null));
        Assert.Single(persister.Current.Rules);
        Assert.Equal("renamed", persister.Current.Rules[0].Name);
        Assert.Equal(75, persister.Current.Rules[0].Priority);
        Assert.Equal(".*github.*", persister.Current.Rules[0].When!.UrlRegex);
    }

    [Fact]
    public void Discard_RevertsDrafts_AndKeepsSourceUntouched()
    {
        JsonConfigPersister persister = Persister();
        Rule source = MakeRule("name", priority: 50, urlRegex: "");
        RuleRowViewModel row = new(source, persister);

        row.Name = "renamed";
        row.Priority = 75;
        row.UrlRegex = ".*github.*";
        row.DiscardCommand.Execute(null);

        Assert.False(row.HasDraft);
        Assert.True(string.IsNullOrEmpty(row.LastError));
        Assert.Equal("name", row.Name);
        Assert.Equal(50, row.Priority);
        Assert.Equal("", row.UrlRegex);
        Assert.Empty(persister.Current.Rules);
    }

    [Fact]
    public void Save_OnMutateThrowing_SetsLastError_KeepsDraftsDirty()
    {
        ConfigPersisterStub persister = new() { ThrowOnSave = new InvalidOperationException("disk full") };
        RuleRowViewModel row = new(MakeRule("name"), persister);

        row.Name = "anything";
        row.SaveCommand.Execute(null);

        Assert.False(string.IsNullOrEmpty(row.LastError));
        Assert.Contains("disk full", row.LastError);
        Assert.True(row.HasDraft);
        Assert.True(row.SaveCommand.CanExecute(null));
    }

    private JsonConfigPersister Persister()
    {
        return new JsonConfigPersister(_configPath, _logger);
    }

    private void SeedConfig(params Rule[] rules)
    {
        AppConfig seed = new() { Rules = [.. rules] };
        ConfigLoader.WriteAtomic(seed, _configPath);
    }

    private static Rule MakeRule(string name, int priority = 50, string urlRegex = "")
    {
        return new Rule
        {
            Name = name,
            Priority = priority,
            When = string.IsNullOrEmpty(urlRegex) ? new RuleWhen() : new RuleWhen { UrlRegex = urlRegex },
            Then = new RuleThen { Browser = "firefox" },
            Origin = "user",
        };
    }
}

internal sealed class ConfigPersisterStub : IConfigPersister
{
    public AppConfig Current { get; private set; } = new();

    public Exception? ThrowOnSave { get; set; }

    public int ReloadCount { get; private set; }

    public event EventHandler? Changed;

    public void Reload()
    {
        ReloadCount++;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public SaveOutcome Save(Func<AppConfig, AppConfig> mutate)
    {
        if (ThrowOnSave is not null)
        {
            return SaveOutcome.Failure(ThrowOnSave.Message);
        }
        Current = mutate(Current);
        Changed?.Invoke(this, EventArgs.Empty);
        return SaveOutcome.Success();
    }
}
