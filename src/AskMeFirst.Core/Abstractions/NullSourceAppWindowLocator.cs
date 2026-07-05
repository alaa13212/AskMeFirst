namespace AskMeFirst.Core.Abstractions;

public sealed class NullSourceAppWindowLocator : ISourceAppWindowLocator
{
    public bool TryGetSourceWindowBounds(out ScreenBounds bounds)
    {
        bounds = new ScreenBounds(0, 0, 0, 0);
        return false;
    }
}