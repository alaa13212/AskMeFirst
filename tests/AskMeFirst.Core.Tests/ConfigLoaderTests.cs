using AskMeConfig = AskMeFirst.Core.Config.Config;
using AskMeFirst.Core.Config;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadDefault_ReturnsValidConfig()
    {
        AskMeConfig config = ConfigLoader.LoadDefault();

        Assert.NotNull(config);
        Assert.NotNull(config.Settings);
        Assert.Equal("system", config.Settings.DefaultBrowserId);
        Assert.True(config.Settings.StripTracking);
    }

    [Fact]
    public void LoadDefault_BrowsersListIsEmpty()
    {
        AskMeConfig config = ConfigLoader.LoadDefault();
        Assert.Empty(config.Browsers);
    }
}
