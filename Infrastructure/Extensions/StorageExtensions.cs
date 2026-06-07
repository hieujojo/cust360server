using CRM.Api.Infrastructure.Settings;
using CRM.Api.Infrastructure.Storage;

namespace CRM.Api.Infrastructure.Extensions;

public static class StorageExtensions
{
    public static IServiceCollection AddCloudinaryStorage(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<CloudinarySettings>(config.GetSection(CloudinarySettings.SectionName));
        services.AddScoped<ICloudinaryStorageService, CloudinaryStorageService>();
        return services;
    }
}
