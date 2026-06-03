using CRM.Api.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CRM.Api.Shared.Authorization;

/// <summary>Đọc claim "role" từ JWT, so sánh với MaxRole.</summary>
public sealed class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement
    )
    {
        var roleClaim = context.User.FindFirst("role")?.Value;

        if (roleClaim != null && int.TryParse(roleClaim, out var role))
        {
            if (role <= requirement.MaxRole && Roles.IsValid(role))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
