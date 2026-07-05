using AskMeFirst.Commands;
using AskMeFirst.Core.Abstractions;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class UninstallCommandTests
{
    [Fact]
    public async Task UnregisterSuccess_ReturnsZero()
    {
        FakeRegistrar registrar = new()
        {
            UnregisterResult = new(Success: true, Message: "Unregistered."),
        };
        FakeLogger logger = new();
        UninstallCommand cmd = new();

        int code = await cmd.Execute(["uninstall"], TestCommandContext.Build(registrar, logger));

        Assert.Equal(0, code);
        Assert.Equal(1, registrar.UnregisterCalls);
    }

    [Fact]
    public async Task UnregisterFailure_ReturnsOne()
    {
        FakeRegistrar registrar = new()
        {
            UnregisterResult = new(Success: false, Message: "Not installed."),
        };
        FakeLogger logger = new();
        UninstallCommand cmd = new();

        int code = await cmd.Execute(["uninstall"], TestCommandContext.Build(registrar, logger));

        Assert.Equal(1, code);
        Assert.Equal(1, registrar.UnregisterCalls);
    }
}