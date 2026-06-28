using AskMeFirst.Picker.ViewModels;
using Avalonia.Controls;

namespace AskMeFirst.Picker;

public sealed partial class PickerWindow : Window
{
    public PickerWindow()
    {
        InitializeComponent();
    }

    public PickerWindow(PickerWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }
}