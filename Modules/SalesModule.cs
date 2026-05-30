using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Repositories;
using CRM.Api.Modules.Services;

namespace CRM.Api.Modules;

public static class SalesModule
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<IPipelineStageService, PipelineStageService>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddScoped<IQuotationService, QuotationService>();
        return services;
    }

    public static async Task EnsureSalesIndexesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dealRepo = scope.ServiceProvider.GetRequiredService<IDealRepository>();
        await dealRepo.EnsureIndexesAsync();
    }
}

