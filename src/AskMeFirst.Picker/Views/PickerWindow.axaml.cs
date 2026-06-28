using AskMeFirst.Picker.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace AskMeFirst.Picker;

public sealed partial class PickerWindow : Window
{
    public PickerWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        KeyDown += OnKeyDown;
    }

    public PickerWindow(PickerWindowViewModel viewModel)
    {
        DataContext = viewModel;
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is PickerWindowViewModel vm)
        {
            vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}