namespace AskMeFirst.Picker.Services;

public interface ISourceAppWindowLocator
{
    bool TryGetSourceWindowBounds(out ScreenBounds bounds);
}