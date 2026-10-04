namespace Techodist.Search.Application.Models;

/// <summary>Сортировка выдачи поиска.</summary>
public enum ProductSort
{
    /// <summary>По релевантности (значение по умолчанию): при пустом запросе — по названию.</summary>
    Relevance = 0,

    PriceAsc = 1,

    PriceDesc = 2,

    NameAsc = 3,

    NameDesc = 4,
}

/// <summary>
/// Разбор ключа сортировки из query-string витрины. Ключи совпадают с каталогом
/// (<c>price_asc</c>, <c>price_desc</c>, <c>name_desc</c>), поэтому переключение витрины
/// на поиск не меняет ссылки; неизвестный ключ трактуется как релевантность.
/// </summary>
public static class ProductSortExtensions
{
    public static ProductSort Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "price_asc" => ProductSort.PriceAsc,
        "price_desc" => ProductSort.PriceDesc,
        "name_asc" => ProductSort.NameAsc,
        "name_desc" => ProductSort.NameDesc,
        _ => ProductSort.Relevance,
    };
}
