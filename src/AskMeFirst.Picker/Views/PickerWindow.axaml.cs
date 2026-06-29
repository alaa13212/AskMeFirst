using System.ComponentModel;
using AskMeFirst.Picker.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AskMeFirst.Picker;

public sealed partial class PickerWindow : Window
{
    private int _browserCursor;
    private int _rememberCursor;

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
        List<Button> browsers = FindBrowserButtons();
        if (browsers.Count == 0)
        {
            return;
        }
        _browserCursor = 0;
        browsers[0].Focus();
    }

    private List<Button> FindBrowserButtons()
    {
        return this.GetVisualDescendants().OfType<Button>()
            .Where(b => b.Name == "PART_BrowserButton")
            .ToList();
    }

    private List<RadioButton> FindRememberRadios()
    {
        return this.GetVisualDescendants().OfType<RadioButton>().ToList();
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

        if (e.Key is Key.Down or Key.Up)
        {
            SyncCursorToFocus();
            MoveInFocusedSection(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right)
        {
            SyncCursorToFocus();
            if (IsBrowserFocused())
            {
                FocusRememberAt(_rememberCursor);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Left)
        {
            SyncCursorToFocus();
            if (IsRememberFocused())
            {
                FocusBrowserAt(_browserCursor);
            }
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

    private void SyncCursorToFocus()
    {
        List<Button> browsers = FindBrowserButtons();
        for (int i = 0; i < browsers.Count; i++)
        {
            if (browsers[i].IsFocused)
            {
                _browserCursor = i;
                return;
            }
        }
        List<RadioButton> radios = FindRememberRadios();
        for (int i = 0; i < radios.Count; i++)
        {
            if (radios[i].IsFocused)
            {
                _rememberCursor = i;
                return;
            }
        }
    }

    private bool IsBrowserFocused() => FindBrowserButtons().Any(b => b.IsFocused);
    private bool IsRememberFocused() => FindRememberRadios().Any(r => r.IsFocused);

    private void FocusBrowserAt(int index)
    {
        List<Button> browsers = FindBrowserButtons();
        if (browsers.Count == 0)
        {
            return;
        }
        int clamped = ((index % browsers.Count) + browsers.Count) % browsers.Count;
        _browserCursor = clamped;
        browsers[clamped].Focus();
    }

    private void FocusRememberAt(int index)
    {
        List<RadioButton> radios = FindRememberRadios();
        if (radios.Count == 0)
        {
            return;
        }
        int clamped = ((index % radios.Count) + radios.Count) % radios.Count;
        _rememberCursor = clamped;
        radios[clamped].Focus();
    }

    private void MoveInFocusedSection(int delta)
    {
        List<Button> browsers = FindBrowserButtons();
        if (browsers.Any(b => b.IsFocused))
        {
            int next = ((_browserCursor + delta) % browsers.Count + browsers.Count) % browsers.Count;
            FocusBrowserAt(next);
            return;
        }
        List<RadioButton> radios = FindRememberRadios();
        if (radios.Any(r => r.IsFocused))
        {
            int next = ((_rememberCursor + delta) % radios.Count + radios.Count) % radios.Count;
            FocusRememberAt(next);
        }
    }
}
