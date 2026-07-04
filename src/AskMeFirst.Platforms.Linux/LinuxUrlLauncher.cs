using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxUrlLauncher(ILogger logger) : IUrlLauncher
{
    public void Launch(Browser browser, Uri url)
    {
        string[] args = browser.LaunchStrategy.BuildArguments(url, browser.Profile, browser.NewWindow);
        logger.LogInfo($"$ {browser.ExecutablePath} {string.Join(' ', args)}");

        ProcessStartInfo psi = new()
        {
            FileName = browser.ExecutablePath,
            UseShellExecute = false,
        };
        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);
    }
}