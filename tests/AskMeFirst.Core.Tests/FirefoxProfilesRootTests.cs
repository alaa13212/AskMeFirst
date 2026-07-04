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
        if (OperatingSystem.IsWindows())
        {
            Assert.EndsWith(@"Mozilla\Firefox\Profiles", root.Replace('/', Path.DirectorySeparatorChar));
        }
        else if (OperatingSystem.IsMacOS())
        {
            Assert.EndsWith("Firefox/Profiles", root.Replace('/', Path.DirectorySeparatorChar));
        }
        else
        {
            Assert.True(
                root.EndsWith(".mozilla/firefox", StringComparison.Ordinal)
                || root.EndsWith(".config/mozilla/firefox", StringComparison.Ordinal),
                $"Expected Linux root to end with .mozilla/firefox or .config/mozilla/firefox, got: {root}");
        }
    }
}