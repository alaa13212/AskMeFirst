using AskMeFirst.Core.Abstractions;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.Tests;

internal sealed class StubPlatformIntegration : IPlatformIntegration
{
    public RegistrationResult InstallResult { get; set; } = new(true, "Installed.");

    public RegistrationResult UninstallResult { get; set; } = new(true, "Uninstalled.");

    public int InstallCount { get; private set; }

    public int UninstallCount { get; private set; }

    public Task<RegistrationResult> InstallAsync(CancellationToken ct = default)
    {
        InstallCount++;
        return Task.FromResult(InstallResult);
    }

    public Task<RegistrationResult> UninstallAsync(CancellationToken ct = default)
    {
        UninstallCount++;
        return Task.FromResult(UninstallResult);
    }
}
