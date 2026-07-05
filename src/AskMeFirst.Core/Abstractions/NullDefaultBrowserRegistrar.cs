namespace AskMeFirst.Core.Abstractions;

public sealed class NullDefaultBrowserRegistrar : IDefaultBrowserRegistrar
{
    public Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new RegistrationResult(
            Success: false,
            Message: "No default-browser registrar is registered for this platform."));
    }

    public Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new RegistrationResult(
            Success: false,
            Message: "No default-browser registrar is registered for this platform."));
    }
}