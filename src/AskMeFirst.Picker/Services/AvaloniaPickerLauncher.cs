using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using AskMeFirst.Picker.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace AskMeFirst.Picker.Services;

public sealed class AvaloniaPickerLauncher : IPickerLauncher
{
    private readonly IConfigWriter? _configWriter;
    private readonly ILogger _logger;

    public AvaloniaPickerLauncher(ILogger logger, IConfigWriter? configWriter = null)
    {
        _logger = logger;
        _configWriter = configWriter;
    }

    public PickerResult Show(PickerRequest request)
    {
        using PickerWindowViewModel viewModel = new(request, _logger, _configWriter);

        BuildAvaloniaApp()
            .AfterSetup(_ =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    WindowPosition position = WindowPositionProvider.ComputeCenterOfPrimaryScreen();
                    PickerWindow window = new(viewModel)
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Position = position.ToPixelPoint(),
                        Width = WindowPositionProvider.DefaultWidth,
                        Height = WindowPositionProvider.DefaultHeight,
                    };
                    window.Closed += (_, _) =>
                    {
                        viewModel.CancelIfNotDone();
                        desktop.Shutdown();
                    };
                    desktop.MainWindow = window;
                }
            })
            .StartWithClassicDesktopLifetime(Array.Empty<string>());

        _logger.LogInfo($"Picker closed. Status: {viewModel.Status}");
        return viewModel.Result;
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseSkia();
}