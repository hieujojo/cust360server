using CRM.Api.Shared.Models;
using CRM.Api.Modules.Interfaces.Repositories;

namespace CRM.Api.Shared.Middleware;

/// <summary>Extract JWT claims → CurrentUser. Reject nếu isActive = false.</summary>
public sealed class OrgResolverMiddleware
{
    private readonly RequestDelegate _next;

    public OrgResolverMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        CurrentUser currentUser,
        IUserRepository userRepo)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            currentUser.UserId         = context.User.FindFirst("sub")?.Value             ?? string.Empty;
            currentUser.OrganizationId = context.User.FindFirst("organizationId")?.Value ?? string.Empty;
            currentUser.Email          = context.User.FindFirst("email")?.Value          ?? string.Empty;
            currentUser.DepartmentId   = context.User.FindFirst("departmentId")?.Value;
            currentUser.TeamId         = context.User.FindFirst("teamId")?.Value;

            var roleClaim = context.User.FindFirst("role")?.Value;
            if (roleClaim != null && int.TryParse(roleClaim, out var role))
                currentUser.Role = role;

            // Security: reject nếu user đã bị deactivate (không chỉ dựa vào claim trong token)
            // Lý do: token được issue khi user active vẫn có thể còn hạn sau khi admin deactivate.
            var dbUser = await userRepo.FindByIdAsync(currentUser.UserId);
            if (dbUser is null || !dbUser.isActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    type   = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    title  = "Unauthorized",
                    status = 401,
                    detail = "Tài khoản của bạn đã bị vô hiệu hóa."
                });
                return;
            }
        }

        await _next(context);
    }
}
