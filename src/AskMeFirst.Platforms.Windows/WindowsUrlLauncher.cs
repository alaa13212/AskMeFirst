using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsUrlLauncher : IUrlLauncher
{
    public void Launch(Browser browser, Uri url)
    {
        ProcessStartInfo psi = new()
        {
            FileName = browser.ExecutablePath,
            Arguments = $"\"{url}\"",
            UseShellExecute = true,
        };
        Process.Start(psi);
    }
}
