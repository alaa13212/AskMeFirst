using AskMeFirst.Core.Paths;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class SelfExecutableTests
{
    [Fact]
    public void IsSelf_ReturnsTrueForCurrentProcessPath()
    {
        Assert.True(SelfExecutable.IsSelf(Environment.ProcessPath));
    }

    [Fact]
    public void IsSelf_ReturnsFalseForNullOrEmpty()
    {
        Assert.False(SelfExecutable.IsSelf(null));
        Assert.False(SelfExecutable.IsSelf(""));
    }

    [Fact]
    public void IsSelf_ReturnsFalseForOtherPath()
    {
        Assert.False(SelfExecutable.IsSelf("/usr/bin/chromium"));
    }

    [Fact]
    public void IsSelf_ReturnsFalseForBareName()
    {
        Assert.False(SelfExecutable.IsSelf("askmefirst"));
    }

    [Fact]
    public void IsSelf_HandlesRelativePath()
    {
        string? self = Environment.ProcessPath;
        Assert.NotNull(self);
        string relative = Path.GetFileName(self);
        Assert.False(SelfExecutable.IsSelf(relative));
    }
}