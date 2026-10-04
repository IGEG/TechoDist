using Techodist.Catalog.Application.Models;

namespace Techodist.Catalog.Application.Common;

/// <summary>
/// Ключи кэша каталога. Страницы списка товаров кэшируются «поколениями»: в ключ входит
/// версия, которую меняет любое изменение товара (<see cref="CatalogCacheInvalidator"/>).
/// Так устаревшие страницы становятся недостижимыми сразу после правки в админке, а не через TTL,
/// и при этом не нужен перебор ключей (SCAN недоступен через <c>IDistributedCache</c>).
/// </summary>
public static class CatalogCacheKeys
{
    /// <summary>Ключ текущей версии списка товаров (значение — тики последнего изменения каталога).</summary>
    public const string ProductListVersion = "catalog:products:version";

    /// <summary>Ключ страницы списка товаров для конкретной версии каталога.</summary>
    public static string ProductList(ProductListFilter filter, long version) =>
        $"catalog:products:v{version}:{filter.CategoryId}:{filter.SolventType}:{filter.Search}:{filter.NormalizedPage}:{filter.NormalizedPageSize}:{filter.Sort}";
}
