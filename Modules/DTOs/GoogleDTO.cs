namespace CRM.Api.Modules.DTOs;

public sealed class GoogleConnectionStatusResponse
{
    public bool Connected { get; init; }
    public string? Email { get; init; }
    public bool CalendarSyncEnabled { get; init; }
    public bool GmailSyncEnabled { get; init; }
}
