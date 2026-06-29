using System.ComponentModel;
using AskMeFirst.Picker.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AskMeFirst.Picker;

public sealed partial class PickerWindow : Window
{
    public PickerWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        KeyDown += OnKeyDown;
        Opened += OnOpened;
    }

    public PickerWindow(PickerWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PickerWindowViewModel.Status))
        {
            return;
        }
        if (DataContext is PickerWindowViewModel vm && vm.Status == PickerStatus.Done)
        {
            Close();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        FocusFirstBrowser();
    }

    private void FocusFirstBrowser()
    {
        Button? first = FindBrowserButtons().FirstOrDefault();
        first?.Focus();
    }

    private IEnumerable<Button> FindBrowserButtons()
    {
        return this.GetVisualDescendants().OfType<Button>()
            .Where(b => b.Name == "PART_BrowserButton");
    }

    private IEnumerable<RadioButton> FindRememberRadios()
    {
        return this.GetVisualDescendants().OfType<RadioButton>();
    }

    private IEnumerable<InputElement> FindNavigableItems()
    {
        foreach (Button button in FindBrowserButtons())
        {
            yield return button;
        }
        foreach (RadioButton radio in FindRememberRadios())
        {
            yield return radio;
        }
    }

    private void OnBrowserClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BrowserOptionViewModel opt }
            && DataContext is PickerWindowViewModel vm)
        {
            int idx = vm.BrowserOptions.IndexOf(opt);
            if (idx >= 0)
            {
                vm.SelectedBrowserIndex = idx;
                vm.CommitCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not PickerWindowViewModel vm)
        {
            return;
        }

        int? hotkeyIndex = e.Key switch
        {
            Key.D1 => 0,
            Key.D2 => 1,
            Key.D3 => 2,
            Key.D4 => 3,
            Key.D5 => 4,
            Key.D6 => 5,
            Key.D7 => 6,
            Key.D8 => 7,
            Key.D9 => 8,
            _ => null,
        };

        if (hotkeyIndex is int idx && idx < vm.BrowserOptions.Count)
        {
            vm.SelectedBrowserIndex = idx;
            vm.CommitCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Down or Key.Right)
        {
            MoveFocus(1);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Up or Key.Left)
        {
            MoveFocus(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (vm.CommitCommand.CanExecute(null))
            {
                vm.CommitCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void MoveFocus(int delta)
    {
        List<InputElement> items = FindNavigableItems().ToList();
        if (items.Count == 0)
        {
            return;
        }

        int currentIdx = items.FindIndex(item => item.IsFocused);
        int nextIdx = currentIdx < 0
            ? 0
            : ((currentIdx + delta) % items.Count + items.Count) % items.Count;
        items[nextIdx].Focus();
    }
}