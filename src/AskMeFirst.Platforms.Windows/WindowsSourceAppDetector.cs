using System.Diagnostics;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsSourceAppDetector(IProcessNameNormalizer normalizer) : ISourceAppDetector
{
    public SourceApp? Detect()
    {
        int ppid = WindowsParentProcess.GetParentProcessId();
        if (ppid <= 0)
        {
            return null;
        }
        try
        {
            using Process process = Process.GetProcessById(ppid);
            string rawName = process.ProcessName;
            string exePath = process.MainModule?.FileName ?? "";
            string canonical = normalizer.Normalize(rawName, bundleId: null, executablePath: exePath);
            return new SourceApp(canonical, BundleId: null, exePath);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
