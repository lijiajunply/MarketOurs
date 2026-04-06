namespace MarketOurs.Data.DTOs;

/// <summary>
/// 分页查询参数
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 50;

    /// <summary>
    /// 当前页码 (从1开始)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数量 (最大限制 50)
    /// </summary>
    public int PageSize
    {
        get;
        set => field = value > MaxPageSize ? MaxPageSize : value;
    } = 10;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// 分页结果通用封装对象
/// </summary>
/// <typeparam name="T">数据项类型</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// 当前页数据项列表
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// 快速构造成功的查询结果
    /// </summary>
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