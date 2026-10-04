namespace Techodist.Catalog.Application.Models;

/// <summary>Постраничный результат.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => Page < TotalPages;

    public bool HasPrevious => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        => new(items, page, pageSize, totalCount);

    public static PagedResult<T> Empty(int page, int pageSize)
        => new([], page, pageSize, 0);
}
