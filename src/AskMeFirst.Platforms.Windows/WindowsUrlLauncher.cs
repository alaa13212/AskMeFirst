using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsUrlLauncher(ILogger logger) : IUrlLauncher
{
    public void Launch(Browser browser, Uri url)
    {
        string[] args = browser.LaunchStrategy.BuildArguments(url, browser.Profile);
        logger.LogInfo($"$ {browser.ExecutablePath} {string.Join(' ', args)}");

        ProcessStartInfo psi = new()
        {
            FileName = browser.ExecutablePath,
            UseShellExecute = true,
        };
        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);
    }
}