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
        Assert.EndsWith(Path.Combine("Mozilla", "Firefox", "Profiles"), root.Replace('/', Path.DirectorySeparatorChar));
    }
}