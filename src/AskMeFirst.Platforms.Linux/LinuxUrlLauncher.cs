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
            UseShellExecute = false,
        };
        foreach (string arg in browser.LaunchStrategy.BuildArguments(url, browser.Profile, browser.NewWindow))
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);
    }
}