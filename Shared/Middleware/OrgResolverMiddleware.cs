using CRM.Api.Shared.Models;

namespace CRM.Api.Shared.Middleware;

/// <summary>Extract JWT claims → CurrentUser. Reject nếu isActive = false.</summary>
public sealed class OrgResolverMiddleware
{
    private readonly RequestDelegate _next;

    public OrgResolverMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser)
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

            // Security: reject token nếu user bị deactivate
            var isActiveClaim = context.User.FindFirst("isActive")?.Value;
            if (isActiveClaim == "false")
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
