using AskMeFirst.Core.Paths;
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
        string resolvedExe = ResolveExe(ppid);
        if (SelfExecutable.IsSelf(resolvedExe))
        {
            return null;
        }
        string canonical = normalizer.Normalize(rawName, bundleId: null, executablePath: resolvedExe);
        return new SourceApp(canonical, BundleId: null, resolvedExe);
    }

    private static string ResolveExe(int ppid)
    {
        string exeSymlink = $"/proc/{ppid}/exe";
        try
        {
            FileSystemInfo? target = File.ResolveLinkTarget(exeSymlink, returnFinalTarget: true);
            return target?.FullName ?? "";
        }
        catch
        {
            return "";
        }
    }
}
