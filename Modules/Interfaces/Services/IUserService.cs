using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

/// <summary>
/// Interface cho UserService. Hợp đồng business logic quản lý User.
/// Mục đích: Loose coupling, dễ test Controller (mock service), tuân thủ SOLID.
/// </summary>
public interface IUserService
{
    Task<ServiceResult<UserResponse>> CreateUserAsync(
        CreateUserRequest request,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default);

    Task<ServiceResult<UserResponse>> UpdateUserAsync(
        string userId, UpdateUserRequest request,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default);

    Task<ServiceResult> ToggleUserStatusAsync(
        string userId, ToggleUserStatusRequest request,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default);

    Task<ServiceResult<UserResponse>> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<PagedResult<UserResponse>> GetPagedAsync(GetUsersRequest request, CancellationToken ct = default);
    Task<ServiceResult<List<UserResponse>>> GetAllAsync(CancellationToken ct = default);

    Task<ServiceResult> ChangePasswordAsync(
        string userId, ChangePasswordRequest request,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default);

    Task<ServiceResult> ResetPasswordAsync(
        string userId, string newPassword,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default);
}
