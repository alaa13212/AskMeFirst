using System.Diagnostics;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsUrlLauncher : IUrlLauncher
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

    private static void LaunchAppBundle(Browser browser, Uri url)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "/usr/bin/open",
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-a");
        psi.ArgumentList.Add(browser.ExecutablePath);
        string[] strategyArgs = browser.LaunchStrategy.BuildArguments(url, browser.Profile);
        if (strategyArgs.Length > 0)
        {
            psi.ArgumentList.Add("--args");
            foreach (string arg in strategyArgs)
            {
                psi.ArgumentList.Add(arg);
            }
        }
        Process.Start(psi);
    }

    private static void LaunchExecutable(Browser browser, Uri url)
    {
        ProcessStartInfo psi = new()
        {
            FileName = browser.ExecutablePath,
            UseShellExecute = false,
        };
        foreach (string arg in browser.LaunchStrategy.BuildArguments(url, browser.Profile))
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);
    }
}