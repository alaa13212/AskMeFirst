using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Picker.Tests.Services;

public sealed class FixedSourceAppWindowLocator : ISourceAppWindowLocator
{
    private readonly ScreenBounds? _bounds;

    public FixedSourceAppWindowLocator(ScreenBounds? bounds = null)
    {
        _bounds = bounds;
    }

    public bool TryGetSourceWindowBounds(out ScreenBounds bounds)
    {
        if (_bounds != null)
        {
            bounds = _bounds;
            return true;
        }
        bounds = new ScreenBounds(0, 0, 0, 0);
        return false;
    }
}