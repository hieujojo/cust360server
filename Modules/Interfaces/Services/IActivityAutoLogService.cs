namespace CRM.Api.Modules.Interfaces.Services;

/// <summary>Internal service for system-generated timeline entries (P1).</summary>
public interface IActivityAutoLogService
{
    Task LogDealStageChangedAsync(
        string customerId,
        string dealId,
        string dealTitle,
        string oldStage,
        string newStage,
        CancellationToken ct = default);

    /// <summary>TODO: call when Ticket module is implemented.</summary>
    Task LogTicketCreatedAsync(string customerId, string ticketId, string ticketTitle, CancellationToken ct = default);

    /// <summary>TODO: call when Ticket module is implemented.</summary>
    Task LogTicketResolvedAsync(string customerId, string ticketId, string ticketTitle, CancellationToken ct = default);
}
