using System.Diagnostics;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacNotifier(ILogger logger) : INotifier
{
    public void Show(string title, string message)
    {
        try
        {
            string escapedTitle = title.Replace("\"", "\\\"");
            string escapedMessage = message.Replace("\"", "\\\"");
            using Process p = new();
            p.StartInfo.FileName = "osascript";
            p.StartInfo.Arguments = $"-e \"display notification \\\"{escapedMessage}\\\" with title \\\"{escapedTitle}\\\"\"";
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