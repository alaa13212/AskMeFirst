using System.Diagnostics;
using System.Globalization;
using AskMeFirst.Core.Routing;

namespace AskMeFirst.Platforms.MacOs;

public sealed class MacOsSourceAppDetector(IProcessNameNormalizer normalizer) : ISourceAppDetector
{
    public SourceApp? Detect()
    {
        int ppid = MacOsParentProcess.GetParentProcessId();
        if (ppid <= 0)
        {
            return null;
        }
        string comm = RunPs(ppid, "comm=");
        string command = RunPs(ppid, "command=");
        if (string.IsNullOrEmpty(comm))
        {
            return null;
        }
        string? bundleId = ExtractBundleId(command);
        string canonical = normalizer.Normalize(comm, bundleId, command);
        return new SourceApp(canonical, bundleId, command);
    }

    private static string RunPs(int pid, string format)
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "/bin/ps",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add(pid.ToString(CultureInfo.InvariantCulture));
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(format);
            using Process proc = Process.Start(psi)!;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(500);
            return output.Trim();
        }
        catch
        {
            return "";
        }
    }

    private static string? ExtractBundleId(string command)
    {
        int appIdx = command.IndexOf(".app/", StringComparison.Ordinal);
        if (appIdx < 0)
        {
            return null;
        }
        string appPath = command[..(appIdx + 4)];
        string plistPath = Path.Combine(appPath, "Contents", "Info.plist");
        if (!File.Exists(plistPath))
        {
            return null;
        }
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "/usr/bin/defaults",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("read");
            psi.ArgumentList.Add(plistPath);
            psi.ArgumentList.Add("CFBundleIdentifier");
            using Process proc = Process.Start(psi)!;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(500);
            string bid = output.Trim();
            return bid.Length == 0 ? null : bid;
        }
        catch
        {
            return null;
        }
    }
}
