using System.Runtime.InteropServices;
using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsSourceAppWindowLocator : ISourceAppWindowLocator
{
    public bool TryGetSourceWindowBounds(out ScreenBounds bounds)
    {
        bounds = new ScreenBounds(0, 0, 0, 0);

        int parentPid = WindowsParentProcess.GetParentProcessId();
        if (parentPid <= 0)
        {
            return false;
        }

        RECT largest = default;
        bool found = false;
        long largestArea = 0;

        EnumWindowsProc callback = (hWnd, lParam) =>
        {
            _ = GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != (uint)parentPid)
            {
                return true;
            }
            if (!IsWindowVisible(hWnd))
            {
                return true;
            }
            if (!GetWindowRect(hWnd, out RECT r))
            {
                return true;
            }
            int width = r.Right - r.Left;
            int height = r.Bottom - r.Top;
            if (width <= 0 || height <= 0)
            {
                return true;
            }
            long area = (long)width * height;
            if (area > largestArea)
            {
                largestArea = area;
                largest = r;
                found = true;
            }
            return true;
        };

        EnumWindows(callback, IntPtr.Zero);

        if (!found)
        {
            return false;
        }

        bounds = new ScreenBounds(
            X: largest.Left,
            Y: largest.Top,
            Width: largest.Right - largest.Left,
            Height: largest.Bottom - largest.Top);
        return true;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
}