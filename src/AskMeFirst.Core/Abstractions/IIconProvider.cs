using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Abstractions;

public interface IIconProvider
{
    byte[]? GetBrowserIconPng(string browserId, string executablePath, string? iconName = null);

    byte[]? GetProfileIconPng(string browserId, BrowserProfile profile);
}