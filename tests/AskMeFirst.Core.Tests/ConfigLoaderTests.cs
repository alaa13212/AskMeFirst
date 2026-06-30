using AskMeFirst.Core.Config;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadDefault_ReturnsValidConfig()
    {
        AppConfig config = ConfigLoader.LoadDefault();

        Assert.NotNull(config);
        Assert.NotNull(config.Settings);
        Assert.True(config.Settings.StripTracking);
    }

    [Fact]
    public void LoadDefault_BrowsersListIsEmpty()
    {
        AppConfig config = ConfigLoader.LoadDefault();
        Assert.Empty(config.Browsers);
    }
}