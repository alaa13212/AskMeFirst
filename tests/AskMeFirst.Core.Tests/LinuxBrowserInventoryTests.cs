using System.Runtime.Versioning;
using AskMeFirst.Core.Launch;
using AskMeFirst.Core.Models;
using AskMeFirst.Platforms.Linux;
using Xunit;

namespace AskMeFirst.Core.Tests;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("freebsd")]
public class LinuxBrowserInventoryTests
{
    [Fact]
    public void Discover_FiltersOutSelfExecutable()
    {
        string? selfPath = Environment.ProcessPath;
        Assert.NotNull(selfPath);

        string tempRoot = Path.Combine(Path.GetTempPath(), $"askmefirst-inv-test-{Guid.NewGuid():N}");
        string appsDir = Path.Combine(tempRoot, "applications");
        Directory.CreateDirectory(appsDir);

        try
        {
            string desktopPath = Path.Combine(appsDir, "askmefirst.desktop");
            File.WriteAllText(desktopPath,
                $"[Desktop Entry]\n" +
                $"Type=Application\n" +
                $"Name=AskMeFirst\n" +
                $"Exec={selfPath} %u\n" +
                $"Categories=Network;WebBrowser;\n");

            Environment.SetEnvironmentVariable("XDG_DATA_HOME", tempRoot);

            LinuxBrowserInventory inventory = new();
            IReadOnlyList<Browser> browsers = inventory.Discover();

            Assert.DoesNotContain(browsers, b =>
                string.Equals(b.DisplayName, "AskMeFirst", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Discover_OperaAndOperaGx_AreDistinct()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"askmefirst-inv-test-{Guid.NewGuid():N}");
        string appsDir = Path.Combine(tempRoot, "applications");
        Directory.CreateDirectory(appsDir);

        try
        {
            string realBinary = Path.Combine(tempRoot, "opera");
            File.WriteAllText(realBinary, "");
            File.WriteAllText(Path.Combine(appsDir, "com.opera.Opera.desktop"),
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=Opera\n" +
                $"Exec={realBinary} %u\n" +
                "Categories=Network;WebBrowser;\n");
            File.WriteAllText(Path.Combine(appsDir, "com.opera.opera-gx.desktop"),
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=Opera GX\n" +
                $"Exec={realBinary} %u\n" +
                "Categories=Network;WebBrowser;\n");

            Environment.SetEnvironmentVariable("XDG_DATA_HOME", tempRoot);

            LinuxBrowserInventory inventory = new();
            IReadOnlyList<Browser> browsers = inventory.Discover();

            Browser opera = Assert.Single(browsers, b => b.DisplayName == "Opera");
            Browser operaGx = Assert.Single(browsers, b => b.DisplayName == "Opera GX");
            Assert.Equal("opera", opera.Id);
            Assert.Equal("opera-gx", operaGx.Id);
            Assert.IsType<ChromiumLaunchStrategy>(BrowserLaunchStrategies.For("opera-gx"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Discover_FiltersOutNoDisplayEntries()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"askmefirst-inv-test-{Guid.NewGuid():N}");
        string appsDir = Path.Combine(tempRoot, "applications");
        Directory.CreateDirectory(appsDir);

        try
        {
            string realBinary = Path.Combine(tempRoot, "keditbookmarks");
            File.WriteAllText(realBinary, "");
            File.WriteAllText(Path.Combine(appsDir, "keditbookmarks.desktop"),
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=Bookmark Editor\n" +
                "NoDisplay=true\n" +
                $"Exec={realBinary} %u\n" +
                "Categories=Qt;KDE;Network;WebBrowser;\n");

            Environment.SetEnvironmentVariable("XDG_DATA_HOME", tempRoot);

            LinuxBrowserInventory inventory = new();
            IReadOnlyList<Browser> browsers = inventory.Discover();

            Assert.DoesNotContain(browsers, b => b.DisplayName == "Bookmark Editor");
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void Discover_FlatpakDesktopEntry_SetsFlatpakAppId()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"askmefirst-inv-test-{Guid.NewGuid():N}");
        string appsDir = Path.Combine(tempRoot, "applications");
        Directory.CreateDirectory(appsDir);

        try
        {
            string realBinary = Path.Combine(tempRoot, "flatpak");
            File.WriteAllText(realBinary, "");
            File.WriteAllText(Path.Combine(appsDir, "com.opera.opera-gx.desktop"),
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=Opera GX\n" +
                $"Exec={realBinary} run --branch=stable com.opera.opera-gx @@u %U @@\n" +
                "Categories=Network;WebBrowser;\n");

            Environment.SetEnvironmentVariable("XDG_DATA_HOME", tempRoot);

            LinuxBrowserInventory inventory = new();
            IReadOnlyList<Browser> browsers = inventory.Discover();

            Browser operaGx = Assert.Single(browsers, b => b.Id == "opera-gx");
            Assert.Equal("com.opera.opera-gx", operaGx.FlatpakAppId);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }
}