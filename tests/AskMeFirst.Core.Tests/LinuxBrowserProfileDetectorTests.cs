using System.Runtime.Versioning;
using AskMeFirst.Core.Models;
using AskMeFirst.Platforms.Linux;
using Xunit;

namespace AskMeFirst.Core.Tests;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("freebsd")]
public class LinuxBrowserProfileDetectorTests
{
    private static string? _originalHome;

    private static RestoreHome UseHome(string homeDir)
    {
        _originalHome = Environment.GetEnvironmentVariable("HOME");
        Environment.SetEnvironmentVariable("HOME", homeDir);
        return new RestoreHome();
    }

    private sealed class RestoreHome : IDisposable
    {
        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HOME", _originalHome);
        }
    }

    [Fact]
    public void Detect_ChromeFlatpak_FindsProfiles()
    {
        string tempHome = Path.Combine(Path.GetTempPath(), $"askmefirst-pd-test-{Guid.NewGuid():N}");
        string configRoot = Path.Combine(tempHome, ".var", "app", "com.google.Chrome", "config", "google-chrome");
        Directory.CreateDirectory(Path.Combine(configRoot, "Default"));
        Directory.CreateDirectory(Path.Combine(configRoot, "Profile 1"));
        File.WriteAllText(Path.Combine(configRoot, "Local State"), """
            {"profile":{"info_cache":{"Default":{"name":"Work"},"Profile 1":{"name":"Personal"}}}}
            """);

        try
        {
            using (UseHome(tempHome))
            {
                Browser browser = new()
                {
                    Id = "chrome",
                    DisplayName = "Google Chrome",
                    ExecutablePath = "/usr/bin/flatpak",
                    FlatpakAppId = "com.google.Chrome",
                };
                LinuxBrowserProfileDetector detector = new();
                IReadOnlyList<BrowserProfile> profiles = detector.Detect(browser);

                Assert.Equal(2, profiles.Count);
                BrowserProfile defaultProfile = Assert.Single(profiles, p => p.IsDefault);
                Assert.Equal("Default", defaultProfile.DirectoryName);
                Assert.Equal("Work", defaultProfile.Name);
                BrowserProfile profile1 = Assert.Single(profiles, p => p.DirectoryName == "Profile 1");
                Assert.Equal("Personal", profile1.Name);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempHome, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Detect_OperaGx_FindsDefaultProfile()
    {
        string tempHome = Path.Combine(Path.GetTempPath(), $"askmefirst-pd-test-{Guid.NewGuid():N}");
        string configRoot = Path.Combine(tempHome, ".var", "app", "com.opera.opera-gx", "config", "opera-gx");
        Directory.CreateDirectory(Path.Combine(configRoot, "Default"));

        try
        {
            using (UseHome(tempHome))
            {
                Browser browser = new()
                {
                    Id = "opera-gx",
                    DisplayName = "Opera GX",
                    ExecutablePath = "/usr/bin/flatpak",
                    FlatpakAppId = "com.opera.opera-gx",
                };
                LinuxBrowserProfileDetector detector = new();
                IReadOnlyList<BrowserProfile> profiles = detector.Detect(browser);

                BrowserProfile defaultProfile = Assert.Single(profiles);
                Assert.True(defaultProfile.IsDefault);
                Assert.Equal("Default", defaultProfile.DirectoryName);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempHome, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Detect_Firefox_PrefersDotMozillaButFallsBackToConfig()
    {
        string tempHome = Path.Combine(Path.GetTempPath(), $"askmefirst-pd-test-{Guid.NewGuid():N}");
        string altRoot = Path.Combine(tempHome, ".config", "mozilla", "firefox");
        Directory.CreateDirectory(altRoot);
        File.WriteAllText(Path.Combine(altRoot, "profiles.ini"),
            "[Profile0]\nName=default-release\nIsRelative=1\nPath=abc.default-release\nDefault=1\n");

        try
        {
            using (UseHome(tempHome))
            {
                Browser browser = new()
                {
                    Id = "firefox",
                    DisplayName = "Firefox",
                    ExecutablePath = "/usr/bin/firefox",
                };
                LinuxBrowserProfileDetector detector = new();
                IReadOnlyList<BrowserProfile> profiles = detector.Detect(browser);

                BrowserProfile defaultProfile = Assert.Single(profiles);
                Assert.Equal("abc.default-release", defaultProfile.DirectoryName);
                Assert.Equal("default-release", defaultProfile.Name);
                Assert.True(defaultProfile.IsDefault);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempHome, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Detect_ChromiumSnap_FindsProfiles()
    {
        string tempHome = Path.Combine(Path.GetTempPath(), $"askmefirst-pd-test-{Guid.NewGuid():N}");
        string snapRoot = Path.Combine(tempHome, "snap", "chromium", "current", ".config", "chromium");
        Directory.CreateDirectory(Path.Combine(snapRoot, "Default"));

        try
        {
            using (UseHome(tempHome))
            {
                Browser browser = new()
                {
                    Id = "chromium",
                    DisplayName = "Chromium",
                    ExecutablePath = "/snap/bin/chromium",
                };
                LinuxBrowserProfileDetector detector = new();
                IReadOnlyList<BrowserProfile> profiles = detector.Detect(browser);

                BrowserProfile defaultProfile = Assert.Single(profiles);
                Assert.True(defaultProfile.IsDefault);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempHome, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Detect_UnknownBrowser_ReturnsEmpty()
    {
        string tempHome = Path.Combine(Path.GetTempPath(), $"askmefirst-pd-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempHome);

        try
        {
            using (UseHome(tempHome))
            {
                Browser browser = new()
                {
                    Id = "lynx",
                    DisplayName = "Lynx",
                    ExecutablePath = "/usr/bin/lynx",
                };
                LinuxBrowserProfileDetector detector = new();
                IReadOnlyList<BrowserProfile> profiles = detector.Detect(browser);

                Assert.Empty(profiles);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempHome, recursive: true);
            }
            catch
            {
            }
        }
    }
}