using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ConfigValidatorTests
{
    private static AppConfig Config(
        IReadOnlyList<ProfileSpec>? profiles = null,
        IReadOnlyList<Rule>? rules = null,
        IReadOnlyList<BrowserSpec>? browsers = null)
    {
        return new AppConfig
        {
            Profiles = profiles ?? [],
            Rules = rules ?? [],
            Browsers = browsers ?? [],
        };
    }

    private static ProfileSpec Spec(string id, string browserId = "firefox")
    {
        return new ProfileSpec { Id = id, BrowserId = browserId, Name = id };
    }

    private static Rule Rule(string? profileId, string name = "rule")
    {
        return new Rule
        {
            Name = name,
            Priority = 100,
            When = new(),
            Then = new() { Browser = "firefox", ProfileId = profileId },
        };
    }

    private static BrowserSpec Browser(string id, string? profileId = null)
    {
        return new BrowserSpec { Id = id, DisplayName = id, ProfileId = profileId };
    }

    [Fact]
    public void EmptyConfig_IsValid()
    {
        FakeLogger logger = new();
        Assert.True(ConfigValidator.Validate(Config(), logger));
        Assert.Empty(logger.Errors);
    }

    [Fact]
    public void UniqueProfileIds_IsValid()
    {
        FakeLogger logger = new();
        AppConfig config = Config(
            profiles: [Spec("a"), Spec("b"), Spec("c")],
            rules: [Rule("a"), Rule("b")]);
        Assert.True(ConfigValidator.Validate(config, logger));
    }

    [Fact]
    public void DuplicateProfileIds_ReportsError()
    {
        FakeLogger logger = new();
        AppConfig config = Config(profiles: [Spec("dup"), Spec("dup")]);
        Assert.False(ConfigValidator.Validate(config, logger));
        Assert.Contains(logger.Errors, e => e.Contains("dup") && e.Contains("more than once"));
    }

    [Fact]
    public void DuplicateProfileIds_DifferentCase_ReportsError()
    {
        FakeLogger logger = new();
        AppConfig config = Config(profiles: [Spec("foo"), Spec("FOO")]);
        Assert.False(ConfigValidator.Validate(config, logger));
    }

    [Fact]
    public void RuleReferencesDeclaredProfile_IsValid()
    {
        FakeLogger logger = new();
        AppConfig config = Config(
            profiles: [Spec("work-profile")],
            rules: [Rule("work-profile")]);
        Assert.True(ConfigValidator.Validate(config, logger));
    }

    [Fact]
    public void RuleReferencesUndeclaredProfile_ReportsError()
    {
        FakeLogger logger = new();
        AppConfig config = Config(
            profiles: [Spec("work-profile")],
            rules: [Rule("ghost-profile", name: "WorkAppsToGhost")]);
        Assert.False(ConfigValidator.Validate(config, logger));
        Assert.Contains(logger.Errors, e =>
            e.Contains("WorkAppsToGhost") && e.Contains("ghost-profile"));
    }

    [Fact]
    public void BrowserReferencesDeclaredProfile_IsValid()
    {
        FakeLogger logger = new();
        AppConfig config = Config(
            profiles: [Spec("chrome-personal-profile", browserId: "chrome-personal")],
            browsers: [Browser("chrome-personal", "chrome-personal-profile")]);
        Assert.True(ConfigValidator.Validate(config, logger));
    }

    [Fact]
    public void BrowserReferencesUndeclaredProfile_ReportsError()
    {
        FakeLogger logger = new();
        AppConfig config = Config(
            profiles: [Spec("chrome-personal-profile")],
            browsers: [Browser("chrome-personal", "ghost-profile")]);
        Assert.False(ConfigValidator.Validate(config, logger));
        Assert.Contains(logger.Errors, e =>
            e.Contains("chrome-personal") && e.Contains("ghost-profile"));
    }

    [Fact]
    public void MultipleErrors_AllReported()
    {
        FakeLogger logger = new();
        AppConfig config = Config(
            profiles: [Spec("dup"), Spec("dup")],
            rules: [Rule("ghost1"), Rule("ghost2")]);
        Assert.False(ConfigValidator.Validate(config, logger));
        Assert.True(logger.Errors.Count >= 3);
    }
}