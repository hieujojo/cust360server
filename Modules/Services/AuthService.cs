using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using CRM.Api.Infrastructure.Email;
using CRM.Api.Infrastructure.Settings;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

/// <summary>Xử lý xác thực: login, sinh JWT, forgot/reset password.</summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository      _userRepo;
    private readonly IAuditLogService     _auditLogService;
    private readonly IEmailService        _emailService;
    private readonly JwtSettings          _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    private const int ResetTokenExpiryMinutes = 15;

    public AuthService(
        IUserRepository userRepo,
        IAuditLogService auditLogService,
        IEmailService emailService,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthService> logger)
    {
        _userRepo        = userRepo;
        _auditLogService = auditLogService;
        _emailService    = emailService;
        _jwtSettings     = jwtOptions.Value;
        _logger          = logger;
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    public async Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email, ct);

        // Dùng thông báo chung — không tiết lộ email có tồn tại hay không
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.password))
            return ServiceResult<LoginResponse>.Fail("INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.");

        if (!user.isActive)
            return ServiceResult<LoginResponse>.Fail("ACCOUNT_INACTIVE", "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Admin.");

        var expiresAt   = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);
        var accessToken = GenerateAccessToken(
            user.id, user.organizationId, user.role,
            user.departmentId, user.teamId, user.email, user.isActive, expiresAt);

        _logger.LogInformation("🔍 [AuthService] Generated JWT for user: email={Email}, role={Role}, orgId={OrgId}",
            user.email, user.role, user.organizationId);

        await _auditLogService.LogAsync(
            organizationId:  user.organizationId,
            action:          AuditActions.UserLoggedIn,
            targetUserId:    user.id,
            targetUserEmail: user.email,
            ipAddress:       ipAddress,
            userAgent:       userAgent,
            ct:              ct);

        _logger.LogInformation("Login success: {Email}", user.email);

        return ServiceResult<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresAt   = expiresAt,
            User        = user.ToResponse()
        });
    }

    // ─── Forgot Password ─────────────────────────────────────────────────────

    /// <summary>
    /// Tạo reset token (JWT 15 phút) và gửi email link.
    /// Luôn trả Ok để tránh email enumeration attack.
    /// </summary>
    public async Task<ServiceResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? clientBaseUrl = null,
        CancellationToken ct = default)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email, ct);

        if (user is null || !user.isActive)
        {
            _logger.LogInformation("ForgotPassword: email not found or inactive — {Email}", request.Email);
            return ServiceResult.Ok(); // Không tiết lộ email có tồn tại hay không
        }

        var expiry     = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes);
        var resetToken = GenerateResetToken(user.id, user.email, expiry);

        await _userRepo.SetResetTokenAsync(user.id, resetToken, expiry, ct);

        var baseUrl   = clientBaseUrl?.TrimEnd('/') ?? "http://localhost:5192";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        _ = Task.Run(async () =>
            await _emailService.SendPasswordResetLinkAsync(user.email, user.displayName, resetLink, CancellationToken.None));

        _logger.LogInformation("ForgotPassword: reset link sent to {Email}", user.email);
        return ServiceResult.Ok();
    }

    // ─── Reset Password by Token ─────────────────────────────────────────────

    /// <summary>Xác thực reset token và đặt mật khẩu mới.</summary>
    public async Task<ServiceResult> ResetPasswordByTokenAsync(
        ResetPasswordByTokenRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        // 1. Validate chữ ký + expiry của JWT
        var principal = ValidateResetToken(request.Token);
        if (principal is null)
            return ServiceResult.Fail("INVALID_TOKEN", "Token không hợp lệ hoặc đã hết hạn.");

        // 2. Tìm user theo token đang lưu trong DB (đảm bảo token chưa dùng)
        var user = await _userRepo.FindByResetTokenAsync(request.Token, ct);
        if (user is null)
            return ServiceResult.Fail("TOKEN_USED", "Token đã được sử dụng hoặc không tồn tại.");

        if (user.passwordResetExpiry < DateTime.UtcNow)
            return ServiceResult.Fail("TOKEN_EXPIRED", "Token đã hết hạn. Vui lòng yêu cầu lại.");

        // 3. Đặt mật khẩu mới + xóa token
        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        var update  = MongoDB.Driver.Builders<User>.Update
            .Set(x => x.password,  newHash)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await _userRepo.UpdateAsync(user.id, update, ct);
        await _userRepo.ClearResetTokenAsync(user.id, ct);

        await _auditLogService.LogAsync(
            organizationId:  user.organizationId,
            action:          AuditActions.UserPasswordReset,
            targetUserId:    user.id,
            targetUserEmail: user.email,
            ipAddress:       ipAddress,
            userAgent:       userAgent,
            ct:              ct);

        _logger.LogInformation("ResetPassword: password reset via token for {Email}", user.email);
        return ServiceResult.Ok();
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    private string GenerateAccessToken(
        string userId, string organizationId, int role,
        string? departmentId, string? teamId, string email, bool isActive, DateTime expiresAt)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sub",            userId),
            new("organizationId", organizationId),
            new("role",           role.ToString()),
            new("email",          email),
            new("isActive",       isActive.ToString().ToLower()),
        };

        if (!string.IsNullOrEmpty(departmentId))
            claims.Add(new Claim("departmentId", departmentId));

        if (!string.IsNullOrEmpty(teamId))
            claims.Add(new Claim("teamId", teamId));

        _logger.LogInformation("🔍 [AuthService] JWT Claims: {Claims}",
            string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

        var token = new JwtSecurityToken(
            issuer:             _jwtSettings.Issuer,
            audience:           _jwtSettings.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateResetToken(string userId, string email, DateTime expiry)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwtSettings.Issuer,
            audience:           "password-reset",
            claims:             [new Claim("sub", userId), new Claim("email", email), new Claim("purpose", "reset")],
            notBefore:          DateTime.UtcNow,
            expires:            expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidateResetToken(string token)
    {
        try
        {
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var result = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = true,
                ValidIssuer              = _jwtSettings.Issuer,
                ValidateAudience         = true,
                ValidAudience            = "password-reset",
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            }, out _);

            // Đảm bảo đây là reset token, không phải access token bị dùng nhầm
            return result.FindFirstValue("purpose") == "reset" ? result : null;
        }
        catch
        {
            return null;
        }
    }
}
