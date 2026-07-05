using System.Diagnostics;
using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.MacOs;

[SupportedOSPlatform("osx")]
public sealed class MacOsSettingsOpener : IOsSettingsOpener
{
    public bool TryOpen()
    {
        try
        {
            using Process? p = Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { "x-apple.systempreferences:com.apple.preference.general?DefaultWebBrowser" },
                UseShellExecute = false,
            });
            return p is not null;
        }
        catch
        {
            return false;
        }
    }
}
