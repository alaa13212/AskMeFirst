using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AskMeFirst.Picker.Tests.TestAppBuilder))]

namespace AskMeFirst.Picker.Tests;

public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<AvaloniaApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            })
            .AfterSetup(_ =>
            {
                if (Application.Current!.Styles.Count == 0)
                {
                    Application.Current.Styles.Add(new FluentTheme());
                }
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            });
}