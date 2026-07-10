using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Picker.Services;

public sealed class WindowPositionProvider(IScreenProvider screens) : IWindowPositionProvider
{
    public WindowPosition Compute(WindowSize windowSize)
    {
        ScreenBounds primary = screens.GetScreens().Primary;
        return CenterOver(primary, windowSize);
    }

    private static WindowPosition CenterOver(ScreenBounds area, WindowSize windowSize)
    {
        int x = area.X + (area.Width - windowSize.Width) / 2;
        int y = area.Y + (area.Height - windowSize.Height) / 2;
        return new WindowPosition(x, y);
    }
}
