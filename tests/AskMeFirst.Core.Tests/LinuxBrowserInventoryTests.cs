using System.Runtime.Versioning;
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
}