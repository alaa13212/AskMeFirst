using AskMeFirst.Core.Models;

namespace AskMeFirst.Core.Abstractions;

public interface IUrlLauncher
{
    void Launch(Browser browser, Uri url);
}
