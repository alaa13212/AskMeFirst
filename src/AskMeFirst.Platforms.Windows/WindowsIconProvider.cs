using System.Runtime.InteropServices;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using Microsoft.Win32;
using SkiaSharp;

namespace AskMeFirst.Platforms.Windows;

public sealed class WindowsIconProvider : IIconProvider
{
    private const int IconSize = 32;
    private const string DefaultIconSubKey = @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet";

    public byte[]? GetBrowserIconPng(string browserId, string executablePath)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(executablePath))
        {
            return null;
        }

        if (!File.Exists(executablePath))
        {
            return null;
        }

        string iconPath = executablePath;
        int index = 0;
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                $@"{DefaultIconSubKey}\{browserId}\DefaultIcon");
            object? value = key?.GetValue(null);
            if (value is string raw && !string.IsNullOrEmpty(raw))
            {
                (iconPath, index) = ParseDefaultIcon(raw, executablePath);
            }
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
        {
            return null;
        }

        return ExtractIconAsPng(iconPath, index);
    }

    public byte[]? GetProfileIconPng(string browserId, BrowserProfile profile)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(profile.DirectoryName))
        {
            return null;
        }

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            byte[]? groupsAvatar = TryFirefoxGroupsAvatar(profile);
            if (groupsAvatar is not null)
            {
                return groupsAvatar;
            }
        }

        string? fullDir = ResolveProfileDir(browserId, profile);
        if (fullDir is null || !Directory.Exists(fullDir))
        {
            return null;
        }

        string[] candidates = browserId.ToLowerInvariant() switch
        {
            "chrome" or "edge" or "brave" or "chromium" =>
                new[] { "Google Profile Picture.png", "Profile Picture.png", "avatar.png" },
            "firefox" =>
                new[] { "avatar.png", "Profile.png" },
            _ => Array.Empty<string>(),
        };

        foreach (string name in candidates)
        {
            string full = Path.Combine(fullDir, name);
            if (File.Exists(full))
            {
                try
                {
                    return File.ReadAllBytes(full);
                }
                catch
                {
                }
            }
        }
        return null;
    }

    private static byte[]? TryFirefoxGroupsAvatar(BrowserProfile profile)
    {
        string? appData = Environment.GetEnvironmentVariable("APPDATA");
        if (string.IsNullOrEmpty(appData))
        {
            return null;
        }

        string groupsRoot = Path.Combine(appData, @"Mozilla\Firefox\Profile Groups");
        if (!Directory.Exists(groupsRoot))
        {
            return null;
        }

        string dirName = ExtractTailSegment(profile.DirectoryName);
        string profilesIni = Path.Combine(appData, @"Mozilla\Firefox\profiles.ini");
        string? storeId = FindFirefoxStoreId(profilesIni, dirName);
        if (string.IsNullOrEmpty(storeId))
        {
            return null;
        }

        return FirefoxProfileAvatarReader.ReadAvatarPng(groupsRoot, storeId, dirName);
    }

    private static string? FindFirefoxStoreId(string profilesIni, string profileDir)
    {
        if (!File.Exists(profilesIni))
        {
            return null;
        }

        string currentSection = "";
        string? currentPath = null;
        string? currentStoreId = null;
        bool inProfile = false;

        foreach (string line in File.ReadAllLines(profilesIni))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                if (inProfile && currentPath is not null && currentStoreId is not null &&
                    MatchesProfilePath(currentPath, profileDir))
                {
                    return currentStoreId;
                }
                currentSection = trimmed[1..^1];
                inProfile = currentSection.StartsWith("Profile", StringComparison.OrdinalIgnoreCase);
                currentPath = null;
                currentStoreId = null;
                continue;
            }

            int eq = trimmed.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }
            string key = trimmed[..eq].Trim();
            string value = trimmed[(eq + 1)..].Trim();
            if (key.Equals("Path", StringComparison.OrdinalIgnoreCase))
            {
                currentPath = value;
            }
            else if (key.Equals("StoreID", StringComparison.OrdinalIgnoreCase))
            {
                currentStoreId = value;
            }
        }

        if (inProfile && currentPath is not null && currentStoreId is not null &&
            MatchesProfilePath(currentPath, profileDir))
        {
            return currentStoreId;
        }
        return null;
    }

    private static bool MatchesProfilePath(string iniPath, string profileDir)
    {
        if (string.IsNullOrEmpty(iniPath))
        {
            return false;
        }
        string last = iniPath.Replace('/', '\\');
        int slash = last.LastIndexOf('\\');
        string tail = slash >= 0 ? last[(slash + 1)..] : last;
        return string.Equals(tail, profileDir, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveProfileDir(string browserId, BrowserProfile profile)
    {
        if (Path.IsPathRooted(profile.DirectoryName))
        {
            return profile.DirectoryName;
        }

        string dirName = ExtractTailSegment(profile.DirectoryName);

        string? localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        string? appData = Environment.GetEnvironmentVariable("APPDATA");

        if (string.Equals(browserId, "firefox", StringComparison.OrdinalIgnoreCase))
        {
            string? firefoxRoot = appData is null ? null : Path.Combine(appData, @"Mozilla\Firefox\Profiles");
            if (firefoxRoot is not null)
            {
                string candidate = Path.Combine(firefoxRoot, dirName);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        if (browserId is "chrome" or "edge" or "brave" or "chromium")
        {
            string subdir = browserId switch
            {
                "chrome" => @"Google\Chrome\User Data",
                "edge" => @"Microsoft\Edge\User Data",
                "brave" => @"BraveSoftware\Brave-Browser\User Data",
                _ => null!,
            };
            if (localAppData is not null && subdir is not null)
            {
                string candidate = Path.Combine(localAppData, subdir, dirName);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        return null;
    }

    private static string ExtractTailSegment(string path)
    {
        string normalized = path.Replace('/', '\\');
        int lastSlash = normalized.LastIndexOf('\\');
        return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
    }

    private static (string Path, int Index) ParseDefaultIcon(string raw, string fallbackExe)
    {
        int lastComma = raw.LastIndexOf(',');
        if (lastComma > 0)
        {
            string pathPart = raw[..lastComma].Trim().Trim('"');
            if (int.TryParse(raw[(lastComma + 1)..].Trim(), out int idx))
            {
                if (string.IsNullOrEmpty(pathPart))
                {
                    return (fallbackExe, idx);
                }
                return (pathPart, idx);
            }
        }
        return (raw.Trim().Trim('"'), 0);
    }

    private static byte[]? ExtractIconAsPng(string path, int index)
    {
        IntPtr hIcon = ExtractIcon(IntPtr.Zero, path, index);
        if (hIcon == IntPtr.Zero || hIcon.ToInt64() <= 0)
        {
            return null;
        }

        try
        {
            return HIconToPng(hIcon, IconSize);
        }
        catch
        {
            return null;
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static byte[]? HIconToPng(IntPtr hIcon, int size)
    {
        IntPtr screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            return null;
        }
        IntPtr memDc = CreateCompatibleDC(screenDc);
        if (memDc == IntPtr.Zero)
        {
            _ = ReleaseDC(IntPtr.Zero, screenDc);
            return null;
        }

        BITMAPINFO bmi = new()
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = size,
                biHeight = -size,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB,
            },
        };

        IntPtr bits = IntPtr.Zero;
        IntPtr dib = CreateDIBSection(memDc, ref bmi, DIB_RGB_COLORS, out bits, IntPtr.Zero, 0);
        if (dib == IntPtr.Zero || bits == IntPtr.Zero)
        {
            DeleteDC(memDc);
            _ = ReleaseDC(IntPtr.Zero, screenDc);
            return null;
        }

        try
        {
            IntPtr old = SelectObject(memDc, dib);
            try
            {
                DrawIconEx(memDc, 0, 0, hIcon, size, size, 0, IntPtr.Zero, DI_NORMAL);
            }
            finally
            {
                SelectObject(memDc, old);
            }

            int byteCount = size * size * 4;
            byte[] pixels = new byte[byteCount];
            Marshal.Copy(bits, pixels, 0, byteCount);

            for (int i = 0; i < byteCount; i += 4)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }

            using SKBitmap skBitmap = new(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
            Marshal.Copy(pixels, 0, skBitmap.GetPixels(), byteCount);
            using SKImage skImage = SKImage.FromBitmap(skBitmap);
            using SKData data = skImage.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        finally
        {
            DeleteObject(dib);
            DeleteDC(memDc);
            _ = ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private const uint BI_RGB = 0;
    private const uint DIB_RGB_COLORS = 0;
    private const uint DI_NORMAL = 0x0003;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DrawIconEx(IntPtr hdcDest, int xLeft, int yTop, IntPtr hIcon,
        int cxWidth, int cyHeight, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage,
        out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public uint bmiColors;
    }
}