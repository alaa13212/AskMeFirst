using System.Runtime.InteropServices;

namespace AskMeFirst.Platforms.Windows;

internal static class WindowsParentProcess
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    private const int ProcessBasicInformation = 0;

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        out PROCESS_BASIC_INFORMATION processInformation,
        int processInformationLength,
        out int returnLength);

    public static int GetParentProcessId()
    {
        IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().Handle;
        int status = NtQueryInformationProcess(
            handle,
            ProcessBasicInformation,
            out PROCESS_BASIC_INFORMATION info,
            Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(),
            out _);
        if (status != 0)
        {
            return 0;
        }
        return info.InheritedFromUniqueProcessId.ToInt32();
    }
}