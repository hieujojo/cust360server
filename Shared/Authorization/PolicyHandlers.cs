using Microsoft.AspNetCore.Authorization;
using CRM.Api.Shared.Constants;

namespace CRM.Api.Shared.Authorization;

/// <summary>Đọc claim "role" từ JWT, so sánh với MaxRole.</summary>
public sealed class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly ILogger<RoleHandler> _logger;

    public RoleHandler(ILogger<RoleHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        // 🔍 DEBUG: Log all claims
        var allClaims = string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}"));
        _logger.LogInformation("🔍 [RoleHandler] All claims: {Claims}", allClaims);
        _logger.LogInformation("🔍 [RoleHandler] Required MaxRole: {MaxRole}", requirement.MaxRole);

        var roleClaim = context.User.FindFirst("role")?.Value;
        _logger.LogInformation("🔍 [RoleHandler] Found role claim: {RoleClaim}", roleClaim ?? "NULL");

        if (roleClaim != null && int.TryParse(roleClaim, out var role))
        {
            _logger.LogInformation("🔍 [RoleHandler] Parsed role: {Role}", role);
            _logger.LogInformation("🔍 [RoleHandler] Is valid role: {IsValid}", Roles.IsValid(role));
            _logger.LogInformation("🔍 [RoleHandler] Check: {Role} <= {MaxRole} = {Result}", role, requirement.MaxRole, role <= requirement.MaxRole);

            if (role <= requirement.MaxRole && Roles.IsValid(role))
            {
                _logger.LogInformation("✅ [RoleHandler] Authorization SUCCEEDED");
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("❌ [RoleHandler] Authorization FAILED: role={Role}, maxRole={MaxRole}, isValid={IsValid}", 
                    role, requirement.MaxRole, Roles.IsValid(role));
            }
        }
        else
        {
            _logger.LogWarning("❌ [RoleHandler] Authorization FAILED: No valid role claim found");
        }

        return Task.CompletedTask;
    }
}
