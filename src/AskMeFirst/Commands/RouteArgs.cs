namespace AskMeFirst.Commands;

public sealed record RouteArgs(Uri Url, string? BrowserId, string? ProfileName, bool Verbose);