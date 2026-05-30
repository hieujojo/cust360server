namespace CRM.Api.Modules.DTOs;

public sealed class QuotationItemRequest
{
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class QuotationItemResponse
{
    public string Description { get; init; } = string.Empty;
    public string? Category { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total { get; init; }
}

public sealed class CreateQuotationRequest
{
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public List<QuotationItemRequest>? Items { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public sealed class UpdateQuotationRequest
{
    public decimal? TotalValue { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public List<QuotationItemRequest>? Items { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public sealed class QuotationResponse
{
    public string Id { get; init; } = string.Empty;
    public string DealId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public decimal TotalValue { get; init; }
    public string Currency { get; init; } = "VND";
    public string Status { get; init; } = "Draft";
    public string? Notes { get; init; }
    public List<QuotationItemResponse> Items { get; init; } = new();
    public int Version { get; init; }
    public DateTime? ValidUntil { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
