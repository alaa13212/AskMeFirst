using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsUrlLauncher : IUrlLauncher
{
    public void Launch(Browser browser, Uri url)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "/usr/bin/open",
            ArgumentList = { "-a", browser.ExecutablePath, url.ToString() },
            UseShellExecute = false,
        };
        Process.Start(psi);
    }
}
