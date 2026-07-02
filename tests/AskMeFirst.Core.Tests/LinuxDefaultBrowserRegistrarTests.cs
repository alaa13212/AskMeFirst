using System.Runtime.Versioning;
using System.Text;
using AskMeFirst.Platforms.Linux;
using Xunit;

namespace AskMeFirst.Core.Tests;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("freebsd")]
public class LinuxDefaultBrowserRegistrarTests
{
    [Fact]
    public void BuildDesktopFile_HasNoUtf8Bom()
    {
        string content = LinuxDefaultBrowserRegistrar.BuildDesktopFile("/usr/bin/askmefirst");

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        Assert.False(
            bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "Desktop entry must not start with a UTF-8 BOM; xdg-mime rejects it as a parse error.");
    }

    [Fact]
    public void BuildDesktopFile_StartsWithDesktopEntryHeader()
    {
        string content = LinuxDefaultBrowserRegistrar.BuildDesktopFile("/usr/bin/askmefirst");

        Assert.StartsWith("[Desktop Entry]", content);
    }

    [Fact]
    public void BuildDesktopFile_ContainsRequiredFields()
    {
        string content = LinuxDefaultBrowserRegistrar.BuildDesktopFile("/opt/askmefirst/askmefirst");

        Assert.Contains("Type=Application", content);
        Assert.Contains("Name=AskMeFirst", content);
        Assert.Contains("Exec=/opt/askmefirst/askmefirst %u", content);
        Assert.Contains("MimeType=x-scheme-handler/http;x-scheme-handler/https;", content);
        Assert.Contains("Terminal=false", content);
        Assert.Contains("Categories=Network;WebBrowser;", content);
    }
}