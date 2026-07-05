namespace AskMeFirst.Core.Abstractions;

public interface ISourceAppWindowLocator
{
    bool TryGetSourceWindowBounds(out ScreenBounds bounds);
}