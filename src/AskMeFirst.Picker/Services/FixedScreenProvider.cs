namespace AskMeFirst.Picker.Services;

public sealed class FixedScreenProvider : IScreenProvider
{
    private readonly ScreenInfo _screens;

    public FixedScreenProvider(ScreenInfo screens)
    {
        _screens = screens;
    }

    public ScreenInfo GetScreens() => _screens;
}