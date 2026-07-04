using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsUrlLauncher(ILogger logger) : IUrlLauncher
{
    public void Launch(Browser browser, Uri url)
    {
        if (browser.ExecutablePath.EndsWith(".app", StringComparison.Ordinal))
        {
            LaunchAppBundle(browser, url);
        }
        else
        {
            LaunchExecutable(browser, url);
        }
    }

    private void LaunchAppBundle(Browser browser, Uri url)
    {
        string[] strategyArgs = browser.LaunchStrategy.BuildArguments(url, browser.Profile, browser.NewWindow);
        List<string> full = ["open", "-a", browser.ExecutablePath];
        if (strategyArgs.Length > 0)
        {
            full.Add("--args");
            full.AddRange(strategyArgs);
        }
        logger.LogInfo($"$ /usr/bin/{string.Join(' ', full)}");

        ProcessStartInfo psi = new()
        {
            FileName = "/usr/bin/open",
            UseShellExecute = false,
        };
        foreach (string arg in full)
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);
    }

    private void LaunchExecutable(Browser browser, Uri url)
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