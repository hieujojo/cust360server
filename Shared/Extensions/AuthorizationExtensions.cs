using Microsoft.AspNetCore.Authorization;
using CRM.Api.Shared.Authorization;
using CRM.Api.Shared.Constants;

namespace CRM.Api.Shared.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddRoleBasedAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, RoleHandler>();

        services.AddAuthorization(options =>
        {
            // OwnerOnly: chỉ Owner (role = 1)
            options.AddPolicy(Policies.OwnerOnly, p => 
                p.AddRequirements(new RoleRequirement(Roles.Owner)));
            
            // AdminOrAbove: Owner (1) hoặc Admin (2) - role <= 2
            options.AddPolicy(Policies.AdminOrAbove, p => 
                p.AddRequirements(new RoleRequirement(Roles.Admin)));
            
            // AnyRole: tất cả authenticated users (role <= 3)
            options.AddPolicy(Policies.AnyRole, p => 
                p.AddRequirements(new RoleRequirement(Roles.User)));
        });

        return services;
    }
}
