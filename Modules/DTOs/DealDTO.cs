namespace CRM.Api.Modules.DTOs;

public sealed class DealListFilterRequest
{
    public string? Stage { get; set; }
    public string? Owner { get; set; }
    public string? CustomerId { get; set; }
    public string? Sort { get; set; } = "updatedAt:desc";
    public string? Search { get; set; }
}

public sealed class DealStatsResponse
{
    public long TotalCount { get; init; }
    public long WonCount { get; init; }
    public long OpenCount => TotalCount - WonCount;
}

public sealed class CreateDealRequest
{
    public string Title { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime? ExpectedCloseDate { get; set; }
    public string? Owner { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public string? Notes { get; set; }
    public List<string>? Contacts { get; set; }
    public List<string>? Quotations { get; set; }
}

public sealed class UpdateDealRequest
{
    public string? Title { get; set; }
    public string? Customer { get; set; }
    public decimal? Value { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public string? Currency { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string? Owner { get; set; }
    public string? Stage { get; set; }
    public int? Probability { get; set; }
    public string? Notes { get; set; }
    public List<string>? Contacts { get; set; }
    public List<string>? Quotations { get; set; }
}

public sealed class ChangeDealStageRequest
{
    public string Stage { get; set; } = string.Empty;
}

public sealed class DealResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public decimal ExpectedRevenue { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTime? ExpectedCloseDate { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public int Probability { get; init; }
    public string? Notes { get; init; }
    public List<DealStageHistoryResponse> StageHistory { get; init; } = [];
    public List<string> Contacts { get; init; } = [];
    public List<string> Quotations { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class DealStageHistoryResponse
{
    public string Stage { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
}

public sealed class PipelineStageResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
    public string Color { get; init; } = string.Empty;
    public int DefaultProbability { get; init; }
    public int StuckThreshold { get; init; }
}

public sealed class UpsertPipelineStageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#2563eb";
    public int DefaultProbability { get; set; } = 0;
    public int StuckThreshold { get; set; } = 7;
}

public sealed class ReorderPipelineStagesRequest
{
    public List<string> StageIds { get; set; } = [];
}

