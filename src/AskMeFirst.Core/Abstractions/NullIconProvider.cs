using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Abstractions;

public sealed class NullIconProvider : IIconProvider
{
    public byte[]? GetBrowserIconPng(string browserId, string executablePath) => null;

    public byte[]? GetProfileIconPng(string browserId, BrowserProfile profile) => null;
}