using System.Diagnostics;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsNotifier(ILogger logger) : INotifier
{
    public void Show(string title, string message)
    {
        if (!TryMsg(title, message))
        {
            logger.LogInfo($"[notify] {title}: {message}");
        }
    }

    private static bool TryMsg(string title, string message)
    {
        try
        {
            using Process p = new();
            p.StartInfo.FileName = "msg.exe";
            p.StartInfo.ArgumentList.Add("*");
            p.StartInfo.ArgumentList.Add("/TIME:5");
            p.StartInfo.ArgumentList.Add(title);
            p.StartInfo.ArgumentList.Add(message);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }
}