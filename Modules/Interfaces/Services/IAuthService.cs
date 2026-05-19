using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

/// <summary>
/// Interface cho AuthService. Hợp đồng xác thực (login, JWT).
/// Mục đích: Loose coupling, dễ test Controller.
/// </summary>
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>Gửi email chứa link reset mật khẩu. Luôn trả về Ok để tránh email enumeration.</summary>
    Task<ServiceResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? clientBaseUrl = null,
        CancellationToken ct = default);

    /// <summary>Xác thực token và đặt mật khẩu mới.</summary>
    Task<ServiceResult> ResetPasswordByTokenAsync(
        ResetPasswordByTokenRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);
}
