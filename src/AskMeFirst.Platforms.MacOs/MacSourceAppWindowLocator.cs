using System.Diagnostics;
using System.Runtime.Versioning;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.MacOs;

[SupportedOSPlatform("osx")]
public sealed class MacSourceAppWindowLocator : ISourceAppWindowLocator
{
    public bool TryGetSourceWindowBounds(out ScreenBounds bounds)
    {
        bounds = new ScreenBounds(0, 0, 0, 0);

        try
        {
            using Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };
            p.StartInfo.ArgumentList.Add("-e");
            p.StartInfo.ArgumentList.Add(
                "tell application \"System Events\" to tell (first process whose frontmost is true) to get {position, size} of window 1");

            if (!p.Start())
            {
                return false;
            }
            string output = p.StandardOutput.ReadToEnd().Trim();
            if (!p.WaitForExit(2000) || p.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                return false;
            }

            if (!TryParseBounds(output, out bounds))
            {
                return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseBounds(string output, out ScreenBounds bounds)
    {
        bounds = new ScreenBounds(0, 0, 0, 0);
        string cleaned = output.Trim().Trim('{', '}');
        string[] parts = cleaned.Split(',');
        if (parts.Length != 4)
        {
            return false;
        }
        if (!int.TryParse(parts[0].Trim(), out int x))
        {
            return false;
        }
        if (!int.TryParse(parts[1].Trim(), out int y))
        {
            return false;
        }
        if (!int.TryParse(parts[2].Trim(), out int w))
        {
            return false;
        }
        if (!int.TryParse(parts[3].Trim(), out int h))
        {
            return false;
        }
        if (w <= 0 || h <= 0)
        {
            return false;
        }
        bounds = new ScreenBounds(x, y, w, h);
        return true;
    }
}