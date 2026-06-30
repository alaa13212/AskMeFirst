using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AskMeFirst.Picker.Services;

public sealed class AvaloniaScreenProvider : IScreenProvider
{
    private readonly Func<Screens?> _screensFactory;

    public AvaloniaScreenProvider(Func<Screens?> screensFactory)
    {
        _screensFactory = screensFactory;
    }

    public ScreenInfo GetScreens()
    {
        Screens? avaloniaScreens = _screensFactory();
        if (avaloniaScreens is null)
        {
            return new ScreenInfo([]);
        }

        List<ScreenBounds> all = [];
        foreach (Screen s in avaloniaScreens.All)
        {
            PixelRect b = s.Bounds;
            all.Add(new ScreenBounds(b.X, b.Y, b.Width, b.Height, s.IsPrimary));
        }
        return new ScreenInfo(all);
    }
}