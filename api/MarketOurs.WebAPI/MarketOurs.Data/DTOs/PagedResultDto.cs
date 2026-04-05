namespace MarketOurs.Data.DTOs;

public class PaginationParams
{
    private const int MaxPageSize = 50;

    public int PageIndex { get; set; } = 1;

    public int PageSize
    {
        get;
        set => field = value > MaxPageSize ? MaxPageSize : value;
    } = 10;

    public string? Keyword { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static PagedResultDto<T> Success(List<T> items, int count, int pageIndex, int pageSize)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            TotalCount = count,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}