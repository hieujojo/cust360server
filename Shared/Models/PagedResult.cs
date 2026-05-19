namespace CRM.Api.Shared.Models;

/// <summary>Pagination wrapper cho list endpoints.</summary>
public sealed class PagedResult<T>
{
    public List<T> Items    { get; init; } = [];
    public long    Total    { get; init; }
    public int     Page     { get; init; }
    public int     PageSize { get; init; }

    public int  TotalPages      => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    public bool HasNextPage     => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Create(List<T> items, long total, int page, int pageSize)
        => new() { Items = items, Total = total, Page = page, PageSize = pageSize };

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
        => new() { Items = [], Total = 0, Page = page, PageSize = pageSize };
}
