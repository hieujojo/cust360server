using System.ComponentModel.DataAnnotations;

namespace CRM.Api.Modules.DTOs;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record CreateFeedbackRequest
{
    [Required(ErrorMessage = "Type là bắt buộc")]
    [RegularExpression("^(customer|internal)$", ErrorMessage = "Type phải là customer hoặc internal")]
    public string Type { get; init; } = "internal";

    [Required(ErrorMessage = "Category là bắt buộc")]
    [RegularExpression("^(feature_request|improvement|complaint|praise|other)$")]
    public string Category { get; init; } = "other";

    [Required(ErrorMessage = "Title là bắt buộc")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title phải từ 1-200 ký tự")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Content là bắt buộc")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Content phải từ 1-2000 ký tự")]
    public string Content { get; init; } = string.Empty;

    public bool IsAnonymous { get; init; } = false;
    public string? CustomerId { get; init; }
}

public sealed record CreateReplyRequest
{
    [Required(ErrorMessage = "Content là bắt buộc")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Content phải từ 1-1000 ký tự")]
    public string Content { get; init; } = string.Empty;

    public bool IsAnonymous { get; init; } = false;
}

public sealed record UpdateFeedbackStatusRequest
{
    [Required(ErrorMessage = "Status là bắt buộc")]
    [RegularExpression("^(open|in_progress|resolved|closed)$")]
    public string Status { get; init; } = "open";
}

// ─── Response DTOs ────────────────────────────────────────────────────────────

public sealed record FeedbackReplyDTO
{
    public string Id { get; init; } = string.Empty;
    public string FeedbackId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string AuthorId { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public bool IsAnonymous { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record FeedbackDTO
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsAnonymous { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorEmail { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public List<FeedbackReplyDTO> Replies { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record PagedFeedbackResponse
{
    public List<FeedbackDTO> Items { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = new();
}

public sealed record PaginationMeta
{
    public long TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
