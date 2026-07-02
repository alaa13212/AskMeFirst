using AskMeFirst.Commands;
using AskMeFirst.Core.Abstractions;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class InstallCommandTests
{
    [Fact]
    public void RegisterSuccess_TryOpenSettingsSucceeds_ReturnsZero()
    {
        FakeRegistrar registrar = new()
        {
            RegisterResult = new(Success: true, Message: "Registered as default browser candidate."),
            OpenOsSettingsResult = true,
        };
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = cmd.Execute(["install"], TestCommandContext.Build(registrar, logger));

        Assert.Equal(0, code);
        Assert.Equal(1, registrar.RegisterCalls);
        Assert.Equal(1, registrar.OpenOsSettingsCalls);
        Assert.DoesNotContain(logger.Infos, m => m.Contains("Open the OS default-browser settings"));
    }

    [Fact]
    public void RegisterSuccess_TryOpenSettingsFails_LogsHintAndReturnsZero()
    {
        FakeRegistrar registrar = new()
        {
            RegisterResult = new(Success: true, Message: "Registered."),
            OpenOsSettingsResult = false,
        };
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = cmd.Execute(["install"], TestCommandContext.Build(registrar, logger));

        Assert.Equal(0, code);
        Assert.Equal(1, registrar.OpenOsSettingsCalls);
        Assert.Contains(logger.Infos, m => m.Contains("Open the OS default-browser settings"));
    }

    [Fact]
    public void RegisterFailure_ReturnsOne_SkipsOpenSettings()
    {
        FakeRegistrar registrar = new()
        {
            RegisterResult = new(Success: false, Message: "Write failed."),
            OpenOsSettingsResult = true,
        };
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = cmd.Execute(["install"], TestCommandContext.Build(registrar, logger));

        Assert.Equal(1, code);
        Assert.Equal(1, registrar.RegisterCalls);
        Assert.Equal(0, registrar.OpenOsSettingsCalls);
    }

    [Fact]
    public void TryOpenSettingsThrows_LogsWarn_DoesNotPropagate()
    {
        ThrowingOpenRegistrar registrar = new();
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = cmd.Execute(["install"], TestCommandContext.Build(registrar, logger));

        Assert.Equal(0, code);
        Assert.Contains(logger.Warns, m => m.Contains("Could not open OS settings"));
        Assert.Contains(logger.Infos, m => m.Contains("Open the OS default-browser settings"));
    }

    private sealed class ThrowingOpenRegistrar : IDefaultBrowserRegistrar
    {
        public Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
            => Task.FromResult(new RegistrationResult(Success: true, Message: "Registered."));

        public Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default)
            => Task.FromResult(new RegistrationResult(Success: true, Message: "Unregistered."));

        public bool TryOpenOsSettings() => throw new InvalidOperationException("URI scheme not registered");
    }
}