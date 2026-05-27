using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Repositories;
using CRM.Api.Modules.Services;

namespace CRM.Api.Modules;

public static class CustomerModule
{
    public static IServiceCollection AddCustomerModule(this IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerCodeGenerator, CustomerCodeGenerator>();
        services.AddScoped<AtlasSearchService>();
        
        return services;
    }

    public static async Task EnsureCustomerIndexesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        await repo.EnsureIndexesAsync();
    }
}
