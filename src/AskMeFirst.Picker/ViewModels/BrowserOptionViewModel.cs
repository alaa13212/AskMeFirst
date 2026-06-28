using System.Globalization;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Picker.ViewModels;

public sealed class BrowserOptionViewModel
{
    public BrowserOptionViewModel(Browser browser, BrowserProfile? profile, int hotkey)
    {
        Browser = browser;
        Profile = profile;
        Hotkey = hotkey;

        DisplayLabel = profile is null
            ? browser.DisplayName
            : $"{browser.DisplayName} [{profile.Name}]";

        HotkeyLabel = hotkey >= 1 && hotkey <= 9 ? hotkey.ToString(CultureInfo.InvariantCulture) : "";
    }

    public Browser Browser { get; }

    public BrowserProfile? Profile { get; }

    public string DisplayLabel { get; }

    public string HotkeyLabel { get; }

    public int Hotkey { get; }
}