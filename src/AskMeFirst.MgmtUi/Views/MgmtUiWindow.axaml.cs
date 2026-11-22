using Avalonia.Controls;
using AskMeFirst.MgmtUi.ViewModels;

namespace AskMeFirst.MgmtUi.Views;

public partial class MgmtUiWindow : Window
{
    public MgmtUiWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MgmtUiWindowViewModel vm)
        {
            MgmtUiWindowViewModel.CloseDecision decision = vm.EvaluateCloseRequest();
            if (decision == MgmtUiWindowViewModel.CloseDecision.Cancel)
            {
                e.Cancel = true;
            }
        }
    }
}
