namespace AskMeFirst.MgmtUi;

using Avalonia;
using AskMeFirst.MgmtUi.ViewModels;
using AskMeFirst.MgmtUi.Views;
using Avalonia.Controls.ApplicationLifetimes;

public static class MgmtUiEntryPoint
{
    public static int Run()
    {
        AppBuilder builder = AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseSkia();
        builder.StartWithClassicDesktopLifetime(System.Array.Empty<string>());
        return 0;
    }
}
