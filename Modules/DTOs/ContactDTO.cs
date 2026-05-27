namespace CRM.Api.Modules.DTOs;

// ============================================================================
// REQUESTS
// ============================================================================

/// <summary>POST /api/customers/{customerId}/contacts</summary>
public sealed class CreateContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; } = false;
}

// ============================================================================
// RESPONSES
// ============================================================================

public sealed class ContactResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Role { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool IsPrimary { get; init; }
    public DateTime CreatedAt { get; init; }
}
