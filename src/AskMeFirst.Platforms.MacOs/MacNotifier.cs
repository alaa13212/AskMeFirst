using System.Diagnostics;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacNotifier(ILogger logger) : INotifier
{
    public void Show(string title, string message)
    {
        try
        {
            using Process p = new();
            p.StartInfo.FileName = "osascript";
            p.StartInfo.ArgumentList.Add("-e");
            p.StartInfo.ArgumentList.Add($"display notification \"{message.Replace("\"", "\\\"")}\" with title \"{title.Replace("\"", "\\\"")}\"");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }
        catch (Exception ex)
        {
            logger.LogWarn($"macOS notification failed: {ex.Message}");
        }
    }
}