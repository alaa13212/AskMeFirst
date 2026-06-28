namespace AskMeFirst.Picker.ViewModels;

public enum RememberKind
{
    Once,
    AlwaysExactHost,
    AlwaysWildcardHost,
    AlwaysSource,
    SourcePlusHost,
}

public sealed class RememberOptionViewModel
{
    public RememberOptionViewModel(
        RememberKind kind,
        string label,
        bool isAvailable = true,
        string unavailableReason = "",
        string? displayUrlPattern = null,
        string? displaySourceName = null)
    {
        Kind = kind;
        Label = label;
        IsAvailable = isAvailable;
        UnavailableReason = unavailableReason;
        DisplayUrlPattern = displayUrlPattern;
        DisplaySourceName = displaySourceName;
        ItemOpacity = isAvailable ? 1.0 : 0.5;
        IsReasonVisible = !isAvailable;
    }

    public RememberKind Kind { get; }

    public string Label { get; }

    public bool IsAvailable { get; }

    public string UnavailableReason { get; }

    public string? DisplayUrlPattern { get; }

    public string? DisplaySourceName { get; }

    public double ItemOpacity { get; }

    public bool IsReasonVisible { get; }
}