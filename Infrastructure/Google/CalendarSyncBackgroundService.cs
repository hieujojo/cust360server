using CRM.Api.Infrastructure.Settings;
using CRM.Api.Modules.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace CRM.Api.Infrastructure.Google;

public sealed class CalendarSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly GoogleSettings _settings;

    public CalendarSyncBackgroundService(
        IServiceProvider services,
        IOptions<GoogleSettings> settings
    )
    {
        _services = services;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId))
        {
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(5, _settings.CalendarSyncIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<IGoogleSyncService>();
                await sync.SyncAllConnectedUsersAsync(stoppingToken);
            }
            catch (Exception)
            {
                //
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
