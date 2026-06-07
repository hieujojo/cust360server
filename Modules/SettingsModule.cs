using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Services;

namespace CRM.Api.Modules;

public static class SettingsModule
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationSettingsService, OrganizationSettingsService>();
        return services;
    }
}
