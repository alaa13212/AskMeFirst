using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.Linux;

public sealed class LinuxSourceAppDetector(IProcessNameNormalizer normalizer) : ISourceAppDetector
{
    public SourceApp? Detect()
    {
        int ppid = LinuxParentProcess.GetParentProcessId();
        if (ppid <= 0)
        {
            return null;
        }
        string commPath = $"/proc/{ppid}/comm";
        if (!File.Exists(commPath))
        {
            return null;
        }
        string rawName;
        try
        {
            rawName = File.ReadAllText(commPath).Trim();
        }
        catch (IOException)
        {
            return null;
        }
        if (rawName.Length == 0)
        {
            return null;
        }
        string exePath = $"/proc/{ppid}/exe";
        string resolvedExe = "";
        try
        {
            if (File.Exists(exePath))
            {
                resolvedExe = Path.GetFullPath(exePath);
            }
        }
        catch
        {
            resolvedExe = "";
        }
        string canonical = normalizer.Normalize(rawName, bundleId: null, executablePath: resolvedExe);
        return new SourceApp(canonical, BundleId: null, resolvedExe);
    }
}
