using CRM.Api.Shared.Models;

namespace CRM.Api.Shared.Extensions;

public static class CurrentUserExtensions
{
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddScoped<CurrentUser>();
        return services;
    }
}
