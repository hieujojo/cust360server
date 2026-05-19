using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Repositories;
using CRM.Api.Modules.Services;

namespace CRM.Api.Modules;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // User & Auth
        services.AddScoped<IUserRepository,     UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserService,        UserService>();
        services.AddScoped<IAuditLogService,    AuditLogService>();
        services.AddScoped<IAuthService,        AuthService>();

        // Department & Team
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ITeamRepository,       TeamRepository>();
        services.AddScoped<IDepartmentService,    DepartmentService>();
        services.AddScoped<ITeamService,          TeamService>();

        return services;
    }

    /// <summary>Tạo MongoDB indexes khi startup.</summary>
    public static async Task EnsureIdentityIndexesAsync(this IServiceProvider services)
    {
        using var scope  = services.CreateScope();
        var userRepo     = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var auditLogRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var deptRepo     = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();
        var teamRepo     = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        await userRepo.EnsureIndexesAsync();
        await auditLogRepo.EnsureIndexesAsync();
        await deptRepo.EnsureIndexesAsync();
        await teamRepo.EnsureIndexesAsync();
    }
}
