using System.Diagnostics;
using Xunit;

namespace AskMeFirst.Tests;

public class CliTests
{
    [Fact]
    public void NoArgs_ReturnsNonZero()
    {
        (int exitCode, _, _) = Run("");
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void Version_PrintsVersionAndExitsZero()
    {
        (int exitCode, string stdout, _) = Run("--version");

        Assert.Equal(0, exitCode);
        Assert.Contains("askmefirst", stdout);
        Assert.Contains("0.1.0", stdout);
    }

    [Fact]
    public void Version_ShortFlag_AlsoWorks()
    {
        (int exitCode, string stdout, _) = Run("-v");

        Assert.Equal(0, exitCode);
        Assert.Contains("askmefirst", stdout);
    }

    [Fact]
    public void Help_PrintsUsageAndExitsZero()
    {
        (int exitCode, string stdout, _) = Run("--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", stdout);
        Assert.Contains("--version", stdout);
    }

    [Fact]
    public void Help_ShortFlag_AlsoWorks()
    {
        (int exitCode, string stdout, _) = Run("-h");

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", stdout);
    }

    [Fact]
    public void UnknownCommand_ReturnsNonZero()
    {
        (int exitCode, _, string stderr) = Run("--not-a-real-flag");

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Unknown argument", stderr);
    }

    [Fact]
    public void Bench_PrintsPlaceholderAndExitsZero()
    {
        (int exitCode, string stdout, _) = Run("--bench");

        Assert.Equal(0, exitCode);
        Assert.Contains("placeholder", stdout, StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExitCode, string StdOut, string StdErr) Run(string args)
    {
        string dll = typeof(Program).Assembly.Location;
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(dll);
        foreach (string a in args.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            psi.ArgumentList.Add(a);
        }

        using Process p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout, stderr);
    }
}
