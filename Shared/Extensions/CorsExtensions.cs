namespace CRM.Api.Shared.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddDefaultCors(this IServiceCollection services, IConfiguration config)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        config.GetSection("AllowedOrigins").Get<string[]>()
                        ?? ["http://localhost:3000"])
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
