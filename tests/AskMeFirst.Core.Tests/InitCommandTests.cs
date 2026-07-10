using AskMeFirst.Commands;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class InitCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly FakeLogger _logger = new();
    private readonly FakePathResolver _paths;
    private readonly CommandRegistry _registry;
    private readonly CommandContext _ctx;

    public InitCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"askmefirst-init-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
        _paths = new FakePathResolver(_configPath);

        _registry = new CommandRegistry();
        ServiceCollection services = new();
        services.AddSingleton<IConfigPathResolver>(_paths);
        services.AddSingleton<ILogger>(_logger);
        ServiceProvider provider = services.BuildServiceProvider();
        _ctx = new CommandContext(_registry, provider, false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task Init_AbsentFile_WritesSample()
    {
        InitCommand cmd = new();
        Assert.False(File.Exists(_configPath));

        int code = await cmd.Execute(["init"], _ctx);

        Assert.Equal(0, code);
        Assert.True(File.Exists(_configPath));
        string content = File.ReadAllText(_configPath);
        Assert.Contains("askmefirst", content);
        Assert.Contains("rules", content);
    }

    [Fact]
    public async Task Init_ExistingFile_DoesNotOverwrite()
    {
        File.WriteAllText(_configPath, "// user content");
        InitCommand cmd = new();

        int code = await cmd.Execute(["init"], _ctx);

        Assert.Equal(0, code);
        Assert.Equal("// user content", File.ReadAllText(_configPath));
    }

    private sealed class FakePathResolver : IConfigPathResolver
    {
        public FakePathResolver(string path)
        {
            DefaultConfigPath = path;
        }

        public string DefaultConfigPath { get; }
    }
}
