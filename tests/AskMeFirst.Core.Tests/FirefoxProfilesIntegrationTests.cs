using AskMeFirst.Core.Models;
using AskMeFirst.Core.Profiles;
using Xunit;
using Xunit.Abstractions;

namespace AskMeFirst.Core.Tests;

public class FirefoxProfilesIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public FirefoxProfilesIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parser_OnRealUserFirefoxData_PrintsAllProfiles()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string profilesIni = Path.Combine(appData, "Mozilla", "Firefox", "profiles.ini");
        string groupsRoot = Path.Combine(appData, "Mozilla", "Firefox", "Profile Groups");

        if (!File.Exists(profilesIni))
        {
            return;
        }

        IReadOnlyList<BrowserProfile> profiles = FirefoxProfilesParser.Parse(profilesIni, groupsRoot);

        _output.WriteLine($"Total profiles: {profiles.Count}");
        foreach (BrowserProfile p in profiles)
        {
            _output.WriteLine($"  Name='{p.Name}' Dir='{p.DirectoryName}' IsDefault={p.IsDefault} GroupId={p.GroupId ?? "<null>"} GroupName={p.GroupName ?? "<null>"}");
        }
    }

    [Fact]
    public void Parser_OnRealUserFirefoxData_IncludesSqliteOnlyProfiles()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string profilesIni = Path.Combine(appData, "Mozilla", "Firefox", "profiles.ini");
        string groupsRoot = Path.Combine(appData, "Mozilla", "Firefox", "Profile Groups");

        if (!File.Exists(profilesIni))
        {
            return;
        }

        IReadOnlyList<BrowserProfile> profiles = FirefoxProfilesParser.Parse(profilesIni, groupsRoot);

        Assert.True(profiles.Count >= 4,
            $"Expected at least 4 Firefox profiles (2 groups x ≥2 sub-profiles), got {profiles.Count}: " +
            string.Join(", ", profiles.Select(p => $"'{p.Name}'@{p.DirectoryName}")));
    }

    [Fact]
    public void Parser_OnRealUserFirefoxData_DoesNotShowOriginalProfilePlaceholder()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string profilesIni = Path.Combine(appData, "Mozilla", "Firefox", "profiles.ini");
        string groupsRoot = Path.Combine(appData, "Mozilla", "Firefox", "Profile Groups");

        if (!File.Exists(profilesIni))
        {
            return;
        }

        IReadOnlyList<BrowserProfile> profiles = FirefoxProfilesParser.Parse(profilesIni, groupsRoot);

        foreach (BrowserProfile profile in profiles)
        {
            Assert.False(
                string.Equals(profile.Name, "Original profile", StringComparison.OrdinalIgnoreCase),
                $"Profile '{profile.DirectoryName}' still shows Firefox's default placeholder name 'Original profile'");
        }
    }
}
