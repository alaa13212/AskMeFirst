using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.Picker.ViewModels;

public enum RememberKind
{
    Once,
    AlwaysExactHost,
    AlwaysWildcardHost,
    AlwaysSource,
    SourcePlusHost,
}

public sealed partial class RememberOptionViewModel : ObservableObject
{
    public RememberOptionViewModel(
        RememberKind kind,
        string label,
        bool isAvailable = true,
        string unavailableReason = "")
    {
        Kind = kind;
        Label = label;
        IsAvailable = isAvailable;
        UnavailableReason = unavailableReason;
        ItemOpacity = isAvailable ? 1.0 : 0.5;
        IsReasonVisible = !isAvailable;
    }

    public RememberKind Kind { get; }

    public string Label { get; }

    public bool IsAvailable { get; }

    public string UnavailableReason { get; }

    public double ItemOpacity { get; }

    public bool IsReasonVisible { get; }

    [ObservableProperty]
    private bool _isSelected;
}