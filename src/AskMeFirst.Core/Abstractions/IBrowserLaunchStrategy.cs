using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Abstractions;

public interface IBrowserLaunchStrategy
{
    string[] BuildArguments(Uri url, BrowserProfile? profile);
}