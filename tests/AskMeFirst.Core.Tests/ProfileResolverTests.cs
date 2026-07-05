using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ProfileResolverTests
{
    private static Browser Browser(string id = "firefox", string displayName = "Firefox")
    {
        return new Browser
        {
            Id = id,
            DisplayName = displayName,
            ExecutablePath = "/path",
        };
    }

    private static ProfileSpec Spec(string id, string browserId, string? name = null, string? directory = null)
    {
        return new ProfileSpec
        {
            Id = id,
            BrowserId = browserId,
            Name = name,
            Directory = directory,
        };
    }

    [Fact]
    public void NoProfileId_NoDetectedProfiles_ReturnsUnchanged()
    {
        FakeProfileDetector detector = new();
        FakeLogger logger = new();
        ProfileResolver resolver = new(detector, [], logger);

        Browser result = resolver.Resolve(Browser(), profileId: null);
        Assert.Null(result.Profile);
    }

    [Fact]
    public void NoProfileId_DetectedProfiles_UsesDefaultProfile()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("Work", "work-dir", IsDefault: true),
                    new BrowserProfile("Personal", "personal-dir", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(detector, [], logger);

        Browser result = resolver.Resolve(Browser(), profileId: null);
        Assert.Equal("Work", result.Profile!.Name);
    }

    [Fact]
    public void NoProfileId_DetectedProfiles_NoDefault_UsesFirst()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("A", "a-dir", IsDefault: false),
                    new BrowserProfile("B", "b-dir", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(detector, [], logger);

        Browser result = resolver.Resolve(Browser(), profileId: null);
        Assert.Equal("A", result.Profile!.Name);
    }

    [Fact]
    public void ProfileId_Declared_MatchesByName()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("Work", "work-dir", IsDefault: true),
                    new BrowserProfile("Personal", "personal-dir", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(
            detector,
            [Spec("work-profile", "firefox", name: "Work")],
            logger);

        Browser result = resolver.Resolve(Browser(), profileId: "work-profile");
        Assert.Equal("Work", result.Profile!.Name);
        Assert.Empty(logger.Errors);
        Assert.Empty(logger.Warns);
    }

    [Fact]
    public void ProfileId_Declared_MatchesByDirectory()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("Work", "work-dir", IsDefault: true),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(
            detector,
            [Spec("work-profile", "firefox", directory: "work-dir")],
            logger);

        Browser result = resolver.Resolve(Browser(), profileId: "work-profile");
        Assert.Equal("Work", result.Profile!.Name);
    }

    [Fact]
    public void ProfileId_Declared_NotDetected_LogsWarningAndFallsBack()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("Work", "work-dir", IsDefault: true),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(
            detector,
            [Spec("ghost-profile", "firefox", directory: "ghost-dir")],
            logger);

        Browser result = resolver.Resolve(Browser(), profileId: "ghost-profile");
        Assert.Equal("Work", result.Profile!.Name);
        Assert.Contains(logger.Warns, w => w.Contains("ghost-profile"));
    }

    [Fact]
    public void ProfileId_NotDeclared_LogsWarningAndFallsBack()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] = [new BrowserProfile("Work", "work-dir", IsDefault: true)],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(detector, [], logger);

        Browser result = resolver.Resolve(Browser(), profileId: "undeclared");
        Assert.Equal("Work", result.Profile!.Name);
        Assert.Contains(logger.Warns, w => w.Contains("undeclared"));
    }

    [Fact]
    public void ProfileId_MatchesDetectedDirectory_UsesDetected()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("default", "gfs9hajj.default", IsDefault: true),
                    new BrowserProfile("Work", "Work", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(detector, [], logger);

        Browser result = resolver.Resolve(Browser(), profileId: "gfs9hajj.default");
        Assert.Equal("default", result.Profile!.Name);
        Assert.Equal("gfs9hajj.default", result.Profile!.DirectoryName);
        Assert.Empty(logger.Errors);
        Assert.Empty(logger.Warns);
    }

    [Fact]
    public void ProfileId_MatchesDetectedName_UsesDetected()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] =
                [
                    new BrowserProfile("default", "gfs9hajj.default", IsDefault: true),
                    new BrowserProfile("Work", "Work", IsDefault: false),
                ],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(detector, [], logger);

        Browser result = resolver.Resolve(Browser(), profileId: "Work");
        Assert.Equal("Work", result.Profile!.Name);
        Assert.Empty(logger.Errors);
        Assert.Empty(logger.Warns);
    }

    [Fact]
    public void ProfileId_BrowserIdMismatch_LogsErrorAndFallsBack()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] = [new BrowserProfile("Work", "work-dir", IsDefault: true)],
                ["chrome"] = [new BrowserProfile("Default", "Default", IsDefault: true)],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(
            detector,
            [Spec("chrome-profile", "chrome", directory: "Default")],
            logger);

        Browser result = resolver.Resolve(Browser("firefox"), profileId: "chrome-profile");
        Assert.Equal("Work", result.Profile!.Name);
        Assert.Contains(logger.Errors, e => e.Contains("chrome-profile") && e.Contains("firefox"));
    }

    [Fact]
    public void ProfileId_IsCaseInsensitive()
    {
        FakeProfileDetector detector = new()
        {
            Profiles =
            {
                ["firefox"] = [new BrowserProfile("Work", "work-dir", IsDefault: true)],
            },
        };
        FakeLogger logger = new();
        ProfileResolver resolver = new(
            detector,
            [Spec("Work-Profile", "firefox", name: "Work")],
            logger);

        Browser result = resolver.Resolve(Browser(), profileId: "work-profile");
        Assert.Equal("Work", result.Profile!.Name);
    }
}