namespace AskMeFirst.MgmtUi.Areas;

public enum AreaId
{
    Browsers,
    Profiles,
    Rules,
    Usage,
    Suggestions,
    Unshortener,
    Tracking,
    Cache,
}

public sealed record AreaDescriptor(
    AreaId Id,
    string Title,
    string Mnemonic,
    string Icon,
    string Summary,
    string DetailTitle);
