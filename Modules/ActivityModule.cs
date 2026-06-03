using CRM.Api.Infrastructure.Google;
using CRM.Api.Infrastructure.Settings;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Repositories;
using CRM.Api.Modules.Services;

namespace CRM.Api.Modules;

public static class ActivityModule
{
    public static IServiceCollection AddActivityModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<GoogleSettings>(configuration.GetSection(GoogleSettings.SectionName));
        services.AddSingleton<IGoogleTokenProtector, GoogleTokenProtector>();

        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IActivityAutoLogService, ActivityAutoLogService>();
        services.AddScoped<IUserGoogleTokenRepository, UserGoogleTokenRepository>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IGoogleSyncService, GoogleSyncService>();

        services.AddHostedService<CalendarSyncBackgroundService>();

        return services;
    }

    public static async Task EnsureActivityIndexesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var activityRepo = scope.ServiceProvider.GetRequiredService<IActivityRepository>();
        await activityRepo.EnsureIndexesAsync();

        var googleRepo = scope.ServiceProvider.GetRequiredService<IUserGoogleTokenRepository>();
        await googleRepo.EnsureIndexesAsync();
    }
}
