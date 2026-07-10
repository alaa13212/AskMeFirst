using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Audit;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace AskMeFirst.Picker.Services;

public sealed class AvaloniaPickerLauncher : IPickerLauncher
{
    private readonly IConfigWriter? _configWriter;
    private readonly ILogger _logger;
    private readonly IWindowPositionProvider _positionProvider;
    private readonly IIconProvider _icons;
    private readonly IRecentPicksLog _recentPicks;

    public AvaloniaPickerLauncher(
        ILogger logger,
        IConfigWriter? configWriter = null,
        IIconProvider? icons = null,
        IRecentPicksLog? recentPicks = null)
        : this(
            logger,
            configWriter,
            icons ?? new NullIconProvider(),
            recentPicks ?? new NoOpRecentPicksLog(),
            new WindowPositionProvider(new AvaloniaScreenProvider(TryGetCurrentScreens)))
    {
    }

    public AvaloniaPickerLauncher(
        ILogger logger,
        IConfigWriter? configWriter,
        IIconProvider icons,
        IRecentPicksLog recentPicks,
        IWindowPositionProvider positionProvider)
    {
        _logger = logger;
        _configWriter = configWriter;
        _icons = icons;
        _recentPicks = recentPicks;
        _positionProvider = positionProvider;
    }

    public PickerResult Show(PickerRequest request)
    {
        using PickerWindowViewModel viewModel = new(request, _logger, _configWriter, _icons, _recentPicks);

        BuildAvaloniaApp()
            .AfterSetup(_ =>
            {
                IClassicDesktopStyleApplicationLifetime? desktop =
                    Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                if (desktop is null)
                {
                    return;
                }

                if (Application.Current!.Styles.Count == 0)
                {
                    Application.Current.Styles.Add(new FluentTheme());
                }
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;

                const int width = 720;
                const int height = 440;
                WindowPosition pos = _positionProvider.Compute(new WindowSize(width, height));

                PickerWindow window = new(viewModel)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Position = new PixelPoint(pos.X, pos.Y),
                    Width = width,
                    Height = height,
                };
                window.Closed += (_, _) =>
                {
                    viewModel.CancelIfNotDone();
                    desktop.Shutdown();
                };
                desktop.MainWindow = window;
            })
            .StartWithClassicDesktopLifetime(Array.Empty<string>());

        _logger.LogInfo($"Picker closed. Status: {viewModel.Status}");
        return viewModel.Result;
    }

    private static Screens? TryGetCurrentScreens()
    {
        IClassicDesktopStyleApplicationLifetime? desktop =
            Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        Window? window = desktop?.MainWindow;
        return window?.Screens;
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseSkia();
}
