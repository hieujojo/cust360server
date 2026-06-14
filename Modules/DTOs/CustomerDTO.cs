namespace CRM.Api.Modules.DTOs;

// ============================================================================
// REQUESTS
// ============================================================================

/// <summary>POST /api/customers</summary>
public sealed class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "Website";
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>
    /// Nếu null → backend tự gán _currentUser.UserId.
    /// Admin/Owner có thể truyền lên để gán cho nhân viên khác.
    /// </summary>
    public string? OwnerId { get; set; }
    /// <summary>Custom fields dạng key-value. Tối đa 10KB.</summary>
    public Dictionary<string, object>? CustomFields { get; set; }
    
    public List<CreateContactRequest>? Contacts { get; set; }
}

/// <summary>PUT /api/customers/{id}</summary>
public sealed class UpdateCustomerRequest
{
    public string? Name { get; set; }
    public string? Source { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? OwnerId { get; set; }
    public string? DepartmentId { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>PUT /api/customers/{id}/status</summary>
public sealed record UpdateCustomerStatusRequest(string NewStatus);

/// <summary>PUT /api/customers/{id}/owner</summary>
public sealed record UpdateCustomerOwnerRequest(string NewOwnerId);

/// <summary>GET /api/customers?status=...&ownerId=...&phone=...&page=1&pageSize=20</summary>
public sealed class CustomerListFilterRequest
{
    public string? Status { get; set; }
    public string? OwnerId { get; set; }
    public string? Phone { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>name | status | createdAt | owner. Default: createdAt.</summary>
    public string SortBy { get; set; } = "createdAt";

    /// <summary>asc | desc. Default: desc.</summary>
    public string SortDir { get; set; } = "desc";
}

// ============================================================================
// RESPONSES
// ============================================================================

/// <summary>Response đầy đủ cho single customer.</summary>
public sealed class CustomerResponse
{
    public string Id { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string? OwnerAvatarUrl { get; init; }
    public string DepartmentId { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public List<ContactResponse> Contacts { get; init; } = [];
    public Dictionary<string, object>? CustomFields { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Response gọn cho list view.</summary>
public sealed class CustomerSummaryResponse
{
    public string Id { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string? OwnerAvatarUrl { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Response cho GET /api/customers (list + pagination).</summary>
public sealed class CustomerListResponse
{
    public List<CustomerSummaryResponse> Items { get; init; } = [];
    public PaginationMetadata Pagination { get; init; } = new();
}

public sealed class PaginationMetadata
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext { get; init; }
}

/// <summary>Response cho GET /api/customers/stats.</summary>
public sealed class CustomerStatsResponse
{
    public long Total { get; init; }
    public long Lead { get; init; }
    public long Active { get; init; }
    public long Churned { get; init; }
}

/// <summary>Response cho GET /api/customers/search.</summary>
public sealed class CustomerSearchResponse
{
    public List<CustomerSearchResultResponse> Results { get; init; } = [];
    public int TotalCount { get; init; }
    public string Query { get; init; } = string.Empty;
}

public sealed class CustomerSearchResultResponse
{
    public string Id { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public float Score { get; init; }
}

// ============================================================================
// CUSTOMER 360 VIEW RESPONSES
// ============================================================================

/// <summary>GET /api/customers/{id}/360 — Tổng hợp toàn bộ thông tin customer.</summary>
public sealed class Customer360ViewResponse
{
    public CustomerInfoTabResponse Info { get; init; } = new();
    public Customer360TabsResponse Tabs { get; init; } = new();
    public Customer360SidebarResponse Sidebar { get; init; } = new();
}

public sealed class Customer360TabsResponse
{
    public List<ContactResponse> Contacts { get; init; } = [];

    /// <summary>Placeholder — sẽ tích hợp module Deals sau.</summary>
    public List<object> Deals { get; init; } = [];

    /// <summary>Placeholder — sẽ tích hợp module Timeline sau.</summary>
    public List<object> Timeline { get; init; } = [];

    /// <summary>Placeholder — sẽ tích hợp module Tickets sau.</summary>
    public List<object> Tickets { get; init; } = [];
}

public sealed class Customer360SidebarResponse
{
    public List<QuickActionResponse> QuickActions { get; init; } = [];

    /// <summary>Placeholder — sẽ tính khi có module Deals.</summary>
    public int OpenDealsCount { get; init; } = 0;

    /// <summary>Placeholder — sẽ tính khi có module Tickets.</summary>
    public int ActiveTicketsCount { get; init; } = 0;
}

public sealed class CustomerInfoTabResponse
{
    public string Id { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string? OwnerAvatarUrl { get; init; }
    public string DepartmentId { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public Dictionary<string, object>? CustomFields { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class QuickActionResponse
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
}
