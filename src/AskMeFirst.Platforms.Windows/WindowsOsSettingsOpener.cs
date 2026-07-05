using System.Diagnostics;
using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsOsSettingsOpener : IOsSettingsOpener
{
    public bool TryOpen()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:defaultapps",
                UseShellExecute = true,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
