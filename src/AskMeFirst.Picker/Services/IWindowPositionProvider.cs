namespace AskMeFirst.Picker.Services;

public sealed record WindowSize(int Width, int Height);

public sealed record WindowPosition(int X, int Y);

public interface IWindowPositionProvider
{
    WindowPosition Compute(WindowSize windowSize);
}
