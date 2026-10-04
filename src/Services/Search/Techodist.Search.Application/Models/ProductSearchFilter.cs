namespace Techodist.Search.Application.Models;

/// <summary>
/// Фильтр поиска товаров. Нормализация параметров (страница, размер, пробелы в запросе)
/// выполняется здесь, а не в контроллере: правило одно и для витрины, и для тестов (ADR 0009).
/// </summary>
public sealed record ProductSearchFilter(
    string? Query = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    ProductSort Sort = ProductSort.Relevance,
    int Page = 1,
    int PageSize = 12)
{
    /// <summary>Предел размера страницы: защита от выкачивания всего индекса одним запросом.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Размер страницы по умолчанию — как в каталоге витрины.</summary>
    public const int DefaultPageSize = 12;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };

    /// <summary>Запрос без учёта регистра и лишних пробелов; пустой означает «всё подряд».</summary>
    public string? NormalizedQuery => string.IsNullOrWhiteSpace(Query) ? null : Query.Trim();

    /// <summary>min &gt; max — диапазон цен пуст, в Elasticsearch ходить незачем.</summary>
    public bool HasInvertedPriceRange => MinPrice is { } min && MaxPrice is { } max && min > max;
}
