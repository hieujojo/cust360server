using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Repositories;

namespace CRM.Api.Modules;

public static class FeedbackModule
{
    public static IServiceCollection AddFeedbackModule(this IServiceCollection services)
    {
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        
        return services;
    }

    public static async Task EnsureFeedbackIndexesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFeedbackRepository>();
        await repo.EnsureIndexesAsync();
    }
}
