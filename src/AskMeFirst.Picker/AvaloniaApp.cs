using Avalonia;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace AskMeFirst.Picker;

public sealed class AvaloniaApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        Styles.Add(new FluentTheme());
        base.OnFrameworkInitializationCompleted();
    }
}