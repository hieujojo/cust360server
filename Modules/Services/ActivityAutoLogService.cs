using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

public sealed class ActivityAutoLogService : IActivityAutoLogService
{
    private const string DealStageChanged = "DEAL_STAGE_CHANGED";
    private const string TicketCreated = "TICKET_CREATED";
    private const string TicketResolved = "TICKET_RESOLVED";

    private readonly IActivityRepository _activityRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly CurrentUser _currentUser;

    public ActivityAutoLogService(
        IActivityRepository activityRepo,
        ICustomerRepository customerRepo,
        CurrentUser currentUser)
    {
        _activityRepo = activityRepo;
        _customerRepo = customerRepo;
        _currentUser = currentUser;
    }

    public async Task LogDealStageChangedAsync(
        string customerId,
        string dealId,
        string dealTitle,
        string oldStage,
        string newStage,
        CancellationToken ct = default)
    {
        if (string.Equals(oldStage, newStage, StringComparison.OrdinalIgnoreCase))
            return;

        await InsertSystemEventAsync(customerId, dealId, DealStageChanged, new Dictionary<string, string>
        {
            { "dealId", dealId },
            { "dealTitle", dealTitle },
            { "oldStage", oldStage },
            { "newStage", newStage }
        }, ct);
    }

    public Task LogTicketCreatedAsync(string customerId, string ticketId, string ticketTitle, CancellationToken ct = default)
        => InsertSystemEventAsync(customerId, null, TicketCreated, new Dictionary<string, string>
        {
            { "ticketId", ticketId },
            { "ticketTitle", ticketTitle }
        }, ct);

    public Task LogTicketResolvedAsync(string customerId, string ticketId, string ticketTitle, CancellationToken ct = default)
        => InsertSystemEventAsync(customerId, null, TicketResolved, new Dictionary<string, string>
        {
            { "ticketId", ticketId },
            { "ticketTitle", ticketTitle }
        }, ct);

    private async Task InsertSystemEventAsync(
        string customerId,
        string? dealId,
        string systemEvent,
        Dictionary<string, string> metadata,
        CancellationToken ct)
    {
        try
        {
            var customer = await _customerRepo.FindByIdAsync(customerId, ct);
            if (customer == null) return;

            var now = DateTime.UtcNow;
            var activity = new Activity
            {
                organizationId = _currentUser.OrganizationId,
                customerId = customer.id,
                dealId = dealId,
                departmentId = customer.departmentId,
                type = ActivityTypes.System,
                source = ActivitySources.System,
                isAutoSync = true,
                createdBy = _currentUser.IsAuthenticated ? _currentUser.UserId : "system",
                occurredAt = now,
                createdAt = now,
                updatedAt = now,
                systemEvent = systemEvent,
                metadata = metadata,
                summary = BuildSummary(systemEvent, metadata)
            };

            await _activityRepo.InsertAsync(activity, ct);
        }
        catch
        {
            // Auto-log must not break primary operations
        }
    }

    private static string BuildSummary(string systemEvent, Dictionary<string, string> metadata) =>
        systemEvent switch
        {
            DealStageChanged =>
                $"Deal \"{metadata.GetValueOrDefault("dealTitle")}\" chuyển từ {metadata.GetValueOrDefault("oldStage")} sang {metadata.GetValueOrDefault("newStage")}",
            TicketCreated =>
                $"Ticket mới: {metadata.GetValueOrDefault("ticketTitle")}",
            TicketResolved =>
                $"Ticket đã xử lý: {metadata.GetValueOrDefault("ticketTitle")}",
            _ => systemEvent
        };
}
