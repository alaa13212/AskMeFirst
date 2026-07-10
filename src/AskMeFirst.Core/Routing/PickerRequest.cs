using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public sealed record PickerRequest(
    Uri OriginalUrl,
    Task<string?>? UnshortenTask,
    IReadOnlyList<PickerBrowserOption> AvailableBrowsers);

public sealed record PickerBrowserOption(Browser Browser, BrowserProfile? Profile);