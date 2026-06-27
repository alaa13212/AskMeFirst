namespace AskMeFirst.Core.Models;

public sealed record Browser(
    string Id,
    string DisplayName,
    string ExecutablePath,
    string? Profile = null);
