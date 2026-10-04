namespace Techodist.Catalog.Application.Models;

/// <summary>Фильтр списка товаров (для постраничного чтения каталога).</summary>
public sealed record ProductListFilter(
    Guid? CategoryId = null,
    string? SolventType = null,
    string? Search = null,
    bool OnlyPublished = true,
    int Page = 1,
    int PageSize = 12,
    string? Sort = null)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 12,
        > 100 => 100,
        _ => PageSize,
    };
}
