using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace AskMeFirst.Picker;

public sealed class AvaloniaApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        Styles.Add(new FluentTheme());
        ResourceDictionary tokens = (ResourceDictionary) AvaloniaXamlLoader.Load(
            new Uri("avares://AskMeFirst.Picker/Resources/DesignTokens.axaml"));
        Resources.MergedDictionaries.Add(tokens);
        base.OnFrameworkInitializationCompleted();
    }
}
