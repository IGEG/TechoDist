using Techodist.Search.Application.Models;

namespace Techodist.Search.Application.Abstractions;

/// <summary>
/// Источник товаров каталога — публичные read-эндпоинты Catalog API. Нужен для реконсиляции
/// индекса: события из брокера могли не дойти (сервис поиска был недоступен), поэтому при старте
/// и по расписанию индекс догоняет каталог (ADR 0009).
/// </summary>
public interface ICatalogProductSource
{
    /// <summary>Все опубликованные товары каталога (постраничное чтение внутри реализации).</summary>
    Task<IReadOnlyList<CatalogProductSnapshot>> ListPublishedProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>Имена категорий по идентификаторам — нужны для денормализации документа.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(CancellationToken cancellationToken = default);
}
