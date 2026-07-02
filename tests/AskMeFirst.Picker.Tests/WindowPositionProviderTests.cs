using AskMeFirst.Core.Abstractions;
using AskMeFirst.Picker.Services;
using AskMeFirst.Picker.Tests.Services;
using Xunit;

namespace AskMeFirst.Picker.Tests;

public class WindowPositionProviderTests
{
    [Fact]
    public void NoSourceLocator_CentersOnPrimaryScreen()
    {
        ScreenInfo screens = new([new ScreenBounds(0, 0, 1920, 1080)]);
        FixedScreenProvider screenProvider = new(screens);
        NullSourceAppWindowLocator locator = new();
        WindowPositionProvider provider = new(screenProvider, locator);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal((1920 - 720) / 2, pos.X);
        Assert.Equal((1080 - 440) / 2, pos.Y);
    }

    [Fact]
    public void WithSourceBounds_CentersOverSource()
    {
        ScreenInfo screens = new([new ScreenBounds(0, 0, 1920, 1080)]);
        FixedScreenProvider screenProvider = new(screens);
        ScreenBounds source = new(X: 500, Y: 300, Width: 800, Height: 600);
        FixedSourceAppWindowLocator locator = new(source);
        WindowPositionProvider provider = new(screenProvider, locator);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal(500 + (800 - 720) / 2, pos.X);
        Assert.Equal(300 + (600 - 440) / 2, pos.Y);
    }

    [Fact]
    public void SourceBounds_TakePrecedenceOverScreens()
    {
        ScreenInfo screens = new([new ScreenBounds(0, 0, 1920, 1080)]);
        FixedScreenProvider screenProvider = new(screens);
        ScreenBounds source = new(X: 1000, Y: 100, Width: 400, Height: 300);
        FixedSourceAppWindowLocator locator = new(source);
        WindowPositionProvider provider = new(screenProvider, locator);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal(1000 + (400 - 720) / 2, pos.X);
        Assert.Equal(100 + (300 - 440) / 2, pos.Y);
    }

    [Fact]
    public void WindowLargerThanArea_CentersWithNegativeOffsets()
    {
        ScreenInfo screens = new([new ScreenBounds(0, 0, 1000, 800)]);
        FixedScreenProvider screenProvider = new(screens);
        NullSourceAppWindowLocator locator = new();
        WindowPositionProvider provider = new(screenProvider, locator);

        WindowPosition pos = provider.Compute(new WindowSize(1200, 900));

        Assert.Equal((1000 - 1200) / 2, pos.X);
        Assert.Equal((800 - 900) / 2, pos.Y);
    }

    [Fact]
    public void EmptyScreensList_UsesFallback()
    {
        ScreenInfo screens = new([]);
        FixedScreenProvider screenProvider = new(screens);
        NullSourceAppWindowLocator locator = new();
        WindowPositionProvider provider = new(screenProvider, locator);

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
        NullSourceAppWindowLocator locator = new();
        WindowPositionProvider provider = new(screenProvider, locator);

        WindowPosition pos = provider.Compute(new WindowSize(720, 440));

        Assert.Equal((1920 - 720) / 2, pos.X);
        Assert.Equal((1080 - 440) / 2, pos.Y);
    }
}