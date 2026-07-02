namespace AskMeFirst.Core.Abstractions;

public interface IDefaultBrowserRegistrar
{
    Task<RegistrationResult> RegisterAsync(CancellationToken ct = default);

    Task<RegistrationResult> UnregisterAsync(CancellationToken ct = default);

    bool TryOpenOsSettings();
}