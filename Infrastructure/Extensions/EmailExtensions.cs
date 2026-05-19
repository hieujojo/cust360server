using CRM.Api.Infrastructure.Email;
using CRM.Api.Infrastructure.Settings;

namespace CRM.Api.Infrastructure.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, EmailService>();
        return services;
    }
}
