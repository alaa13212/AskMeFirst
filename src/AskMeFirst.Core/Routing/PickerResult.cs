using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public abstract record PickerResult;

public sealed record Cancelled : PickerResult;

public sealed record Launched(Browser Browser, Uri Url) : PickerResult;