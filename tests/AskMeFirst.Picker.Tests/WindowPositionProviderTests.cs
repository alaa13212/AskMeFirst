using AskMeFirst.Core.Abstractions;
using AskMeFirst.Picker.Services;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class WindowPositionProviderTests
{
    [Fact]
    public void CentersOnPrimaryScreen()
    {
        ScreenInfo screens = new([new ScreenBounds(0, 0, 1920, 1080)]);
        FixedScreenProvider screenProvider = new(screens);
        WindowPositionProvider provider = new(screenProvider);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal((1920 - 720) / 2, pos.X);
        Assert.Equal((1080 - 440) / 2, pos.Y);
    }

    [Fact]
    public void WindowLargerThanArea_CentersWithNegativeOffsets()
    {
        ScreenInfo screens = new([new ScreenBounds(0, 0, 1000, 800)]);
        FixedScreenProvider screenProvider = new(screens);
        WindowPositionProvider provider = new(screenProvider);

        WindowPosition pos = provider.Compute(new WindowSize(1200, 900));

        Assert.Equal((1000 - 1200) / 2, pos.X);
        Assert.Equal((800 - 900) / 2, pos.Y);
    }

    [Fact]
    public void EmptyScreensList_UsesFallback()
    {
        ScreenInfo screens = new([]);
        FixedScreenProvider screenProvider = new(screens);
        WindowPositionProvider provider = new(screenProvider);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal((1920 - 720) / 2, pos.X);
        Assert.Equal((1080 - 440) / 2, pos.Y);
    }

    [Fact]
    public void MultiScreen_UsesPrimaryScreen()
    {
        ScreenInfo screens = new(
        [
            new ScreenBounds(1920, 0, 2560, 1440, IsPrimary: false),
            new ScreenBounds(0, 0, 1920, 1080, IsPrimary: true),
        ]);
        FixedScreenProvider screenProvider = new(screens);
        WindowPositionProvider provider = new(screenProvider);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal((1920 - 720) / 2, pos.X);
        Assert.Equal((1080 - 440) / 2, pos.Y);
    }
}
