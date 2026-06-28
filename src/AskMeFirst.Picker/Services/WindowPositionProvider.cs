using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace AskMeFirst.Picker.Services;

public readonly record struct WindowPosition(int X, int Y)
{
    public PixelPoint ToPixelPoint() => new(X, Y);
}

public static class WindowPositionProvider
{
    public const int DefaultWidth = 720;
    public const int DefaultHeight = 440;

    public static WindowPosition ComputeCenterOfPrimaryScreen()
    {
        IReadOnlyList<Screen>? screens = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Screens.All;
        if (screens is { Count: > 0 })
        {
            Screen primary = screens[0];
            PixelRect bounds = primary.Bounds;
            int x = bounds.X + (bounds.Width - DefaultWidth) / 2;
            int y = bounds.Y + (bounds.Height - DefaultHeight) / 2;
            return new WindowPosition(x, y);
        }

        return new WindowPosition(100, 100);
    }
}