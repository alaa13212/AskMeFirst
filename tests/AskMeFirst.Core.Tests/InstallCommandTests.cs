using AskMeFirst.Commands;
using AskMeFirst.Core.Abstractions;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class InstallCommandTests
{
    [Fact]
    public async Task RegisterSuccess_TryOpenSettingsSucceeds_ReturnsZero()
    {
        FakeRegistrar registrar = new()
        {
            RegisterResult = new(Success: true, Message: "Registered as default browser candidate."),
        };
        FakeOsSettingsOpener settingsOpener = new()
        {
            TryOpenResult = true,
        };
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = await cmd.Execute(["install"], TestCommandContext.Build(registrar, logger, settingsOpener));

        Assert.Equal(0, code);
        Assert.Equal(1, registrar.RegisterCalls);
        Assert.Equal(1, settingsOpener.TryOpenCalls);
        Assert.DoesNotContain(logger.Infos, m => m.Contains("Open the OS default-browser settings"));
    }

    [Fact]
    public async Task RegisterSuccess_TryOpenSettingsFails_LogsHintAndReturnsZero()
    {
        FakeRegistrar registrar = new()
        {
            RegisterResult = new(Success: true, Message: "Registered."),
        };
        FakeOsSettingsOpener settingsOpener = new()
        {
            TryOpenResult = false,
        };
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = await cmd.Execute(["install"], TestCommandContext.Build(registrar, logger, settingsOpener));

        Assert.Equal(0, code);
        Assert.Equal(1, settingsOpener.TryOpenCalls);
        Assert.Contains(logger.Infos, m => m.Contains("Open the OS default-browser settings"));
    }

    [Fact]
    public async Task RegisterFailure_ReturnsOne_SkipsOpenSettings()
    {
        FakeRegistrar registrar = new()
        {
            RegisterResult = new(Success: false, Message: "Write failed."),
        };
        FakeOsSettingsOpener settingsOpener = new();
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = await cmd.Execute(["install"], TestCommandContext.Build(registrar, logger, settingsOpener));

        Assert.Equal(1, code);
        Assert.Equal(1, registrar.RegisterCalls);
        Assert.Equal(0, settingsOpener.TryOpenCalls);
    }

    [Fact]
    public async Task TryOpenSettingsThrows_LogsWarn_DoesNotPropagate()
    {
        ThrowingOpenOsSettingsOpener settingsOpener = new();
        FakeRegistrar registrar = new();
        FakeLogger logger = new();
        InstallCommand cmd = new();

        int code = await cmd.Execute(["install"], TestCommandContext.Build(registrar, logger, settingsOpener));

        Assert.Equal(0, code);
        Assert.Contains(logger.Warns, m => m.Contains("Could not open OS settings"));
        Assert.Contains(logger.Infos, m => m.Contains("Open the OS default-browser settings"));
    }

    private sealed class ThrowingOpenOsSettingsOpener : IOsSettingsOpener
    {
        public bool TryOpen() => throw new InvalidOperationException("URI scheme not registered");
    }
}