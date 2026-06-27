using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Abstractions;

public interface IBrowserProfileDetector
{
    IReadOnlyList<BrowserProfile> Detect(string browserId);
}