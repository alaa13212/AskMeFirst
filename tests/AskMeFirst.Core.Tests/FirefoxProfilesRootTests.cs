using AskMeFirst.Core.Profiles;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class FirefoxProfilesRootTests
{
    [Fact]
    public void Get_ReturnsNonEmptyAbsolutePath()
    {
        string root = FirefoxProfilesRoot.Get();
        Assert.False(string.IsNullOrEmpty(root));
        Assert.True(Path.IsPathRooted(root), $"Firefox profiles root must be absolute, got: {root}");
    }

    [Fact]
    public void Get_ReturnsPlatformCorrectPath()
    {
        string root = FirefoxProfilesRoot.Get();
        string expectedTail = OperatingSystem.IsWindows()
            ? @"Mozilla\Firefox\Profiles"
            : OperatingSystem.IsMacOS()
                ? "Firefox/Profiles"
                : ".mozilla/firefox/Profiles";
        Assert.EndsWith(expectedTail, root.Replace('/', Path.DirectorySeparatorChar));
    }
}