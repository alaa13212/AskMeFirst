using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Routing;

public static class PickerOptions
{
    public static IReadOnlyList<PickerBrowserOption> Build(
        IReadOnlyList<Browser> browsers,
        IBrowserProfileDetector profiles)
    {
        List<PickerBrowserOption> options = [];
        foreach (Browser browser in browsers)
        {
            IReadOnlyList<BrowserProfile> detected = profiles.Detect(browser);
            if (detected.Count == 0)
            {
                options.Add(new PickerBrowserOption(browser, null));
                continue;
            }

            foreach (BrowserProfile profile in detected)
            {
                options.Add(new PickerBrowserOption(browser with { Profile = profile }, profile));
            }
        }
        return options;
    }
}