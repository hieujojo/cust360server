namespace CRM.Api.Modules.DTOs;

public sealed class ActivityListFilterRequest
{
    public string? CustomerId { get; set; }
    public string? DealId { get; set; }
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

public sealed class CreateActivityRequest
{
    public string Type { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? DealId { get; set; }
    public DateTime? OccurredAt { get; set; }

    public string? Outcome { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Note { get; set; }

    public string? Subject { get; set; }
    public string? Summary { get; set; }

    public string? Location { get; set; }
    public List<string>? Attendees { get; set; }
    public string? NextSteps { get; set; }

    public string? Body { get; set; }
}

public sealed class UpdateActivityRequest
{
    public DateTime? OccurredAt { get; set; }

    public string? Outcome { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Note { get; set; }

    public string? Subject { get; set; }
    public string? Summary { get; set; }

    public string? Location { get; set; }
    public List<string>? Attendees { get; set; }
    public string? NextSteps { get; set; }

    public string? Body { get; set; }
}

public sealed class ActivityResponse
{
    public string Id { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string? DealId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public bool IsAutoSync { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string CreatedByName { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public DateTime CreatedAt { get; init; }

    public string? Outcome { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Note { get; init; }
    public string? Subject { get; init; }
    public string? Summary { get; init; }
    public string? Direction { get; init; }
    public string? Location { get; init; }
    public List<string> Attendees { get; init; } = [];
    public string? NextSteps { get; init; }
    public string? Body { get; init; }
    public string? SystemEvent { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public sealed class ActivityListResponse
{
    public List<ActivityResponse> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}
