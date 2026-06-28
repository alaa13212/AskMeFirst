using System.Runtime.InteropServices;

namespace AskMeFirst.Platforms.Linux;

internal static class LinuxParentProcess
{
    [DllImport("libc", EntryPoint = "getppid")]
    private static extern int getppid();

    public static int GetParentProcessId()
    {
        try
        {
            return getppid();
        }
        catch
        {
            return 0;
        }
    }
}