namespace AskMeFirst.Core.Models;

public sealed record BrowserProfile(
    string Name,
    string DirectoryName,
    bool IsDefault,
    string? GroupId = null,
    string? GroupName = null);