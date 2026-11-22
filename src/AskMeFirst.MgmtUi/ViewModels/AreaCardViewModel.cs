namespace AskMeFirst.MgmtUi.ViewModels;

using AskMeFirst.MgmtUi.Areas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class AreaCardViewModel : ObservableObject
{
    public AreaCardViewModel(AreaDescriptor descriptor, MgmtUiWindowViewModel parent)
    {
        Descriptor = descriptor;
        _parent = parent;
    }

    public AreaDescriptor Descriptor { get; }

    public string Title => Descriptor.Title;

    public string IconGlyph => Descriptor.Icon;

    public string Summary => Descriptor.Summary;

    [RelayCommand]
    private void Drill()
    {
        _parent.DrillTo(Descriptor.Id);
    }

    private readonly MgmtUiWindowViewModel _parent;
}
