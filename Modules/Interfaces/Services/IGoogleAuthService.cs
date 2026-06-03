using CRM.Api.Modules.DTOs;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IGoogleAuthService
{
    string GetAuthorizationUrl();
    Task HandleCallbackAsync(string code, string state, CancellationToken ct = default);
    Task<GoogleConnectionStatusResponse> GetStatusAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
