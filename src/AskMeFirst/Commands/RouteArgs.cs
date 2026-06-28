namespace AskMeFirst.Commands;

public sealed record RouteArgs(Uri Url, string? BrowserId, string? ProfileId, bool Verbose);