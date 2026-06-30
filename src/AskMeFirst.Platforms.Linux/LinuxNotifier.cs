using System.Diagnostics;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxNotifier(ILogger logger) : INotifier
{
    public void Show(string title, string message)
    {
        try
        {
            using Process p = new();
            p.StartInfo.FileName = "notify-send";
            p.StartInfo.Arguments = $"--urgency=critical \"{title}\" \"{message}\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }
        catch (Exception ex)
        {
            logger.LogWarn($"Linux notification failed: {ex.Message}");
        }
    }
}