using Techodist.Search.Application.Index;
using Techodist.Search.Application.Models;

namespace Techodist.Search.Application.Abstractions;

/// <summary>
/// Поисковый индекс товаров (Elasticsearch). Абстракция держит слой Application независимым
/// от транспорта: юнит-тесты подставляют подменный индекс, а запросы Elasticsearch живут
/// в Infrastructure (ADR 0009).
/// </summary>
public interface IProductIndex
{
    /// <summary>Создаёт индекс с маппингом, если его ещё нет (идемпотентно).</summary>
    Task EnsureIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>Поиск по опубликованным товарам с фильтрами, сортировкой и пагинацией.</summary>
    Task<ProductIndexPage> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Вставляет или обновляет документ товара (идемпотентно — по идентификатору товара).</summary>
    Task UpsertAsync(ProductDocument document, CancellationToken cancellationToken = default);

    /// <summary>Удаляет документ товара; отсутствие документа ошибкой не считается.</summary>
    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Доступен ли кластер (health-check сервиса).</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
