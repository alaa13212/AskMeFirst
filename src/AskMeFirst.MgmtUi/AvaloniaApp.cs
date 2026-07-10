using System;
using AskMeFirst.MgmtUi.ViewModels;
using AskMeFirst.MgmtUi.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;

namespace AskMeFirst.MgmtUi;

public sealed class AvaloniaApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        Styles.Add(new FluentTheme());
        ResourceDictionary tokens = (ResourceDictionary) AvaloniaXamlLoader.Load(
            new Uri("avares://AskMeFirst.MgmtUi/Resources/DesignTokens.axaml"));
        Resources.MergedDictionaries.Add(tokens);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MgmtUiEntryPointComposition composition = MgmtUiEntryPoint.BuildComposition();
            MgmtUiWindowViewModel viewModel = new(composition.ConfigPersister);
            MgmtUiWindow window = new() { DataContext = viewModel };
            window.Position = new Avalonia.PixelPoint(120, 80);
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
