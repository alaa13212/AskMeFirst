using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxUrlLauncher : IUrlLauncher
{
    public void Launch(Browser browser, Uri url)
    {
        ProcessStartInfo psi = new()
        {
            FileName = browser.ExecutablePath,
            ArgumentList = { url.ToString() },
            UseShellExecute = false,
        };
        Process.Start(psi);
    }
}
