using AskMeFirst.Core.Abstractions;

namespace AskMeFirst.MgmtUi.Services;

public interface IPlatformIntegration
{
    Task<RegistrationResult> InstallAsync(CancellationToken ct = default);

    Task<RegistrationResult> UninstallAsync(CancellationToken ct = default);
}

public sealed class NullPlatformIntegration : IPlatformIntegration
{
    public static readonly NullPlatformIntegration Instance = new();

    private NullPlatformIntegration()
    {
    }

    public Task<RegistrationResult> InstallAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new RegistrationResult(false, "Platform integration unavailable in this context."));
    }

    public Task<RegistrationResult> UninstallAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new RegistrationResult(false, "Platform integration unavailable in this context."));
    }
}

public sealed class PlatformIntegrationAdapter : IPlatformIntegration
{
    private readonly IDefaultBrowserRegistrar _registrar;

    public PlatformIntegrationAdapter(IDefaultBrowserRegistrar registrar)
    {
        _registrar = registrar;
    }

    public Task<RegistrationResult> InstallAsync(CancellationToken ct = default)
    {
        return _registrar.RegisterAsync(ct);
    }

    public Task<RegistrationResult> UninstallAsync(CancellationToken ct = default)
    {
        return _registrar.UnregisterAsync(ct);
    }
}
