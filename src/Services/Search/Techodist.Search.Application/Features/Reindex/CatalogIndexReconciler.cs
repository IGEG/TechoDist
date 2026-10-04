using Microsoft.Extensions.Logging;
using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Common;

namespace Techodist.Search.Application.Features.Reindex;

/// <summary>
/// Сверка индекса с каталогом (реконсиляция). Событие может потеряться: сервис поиска был
/// недоступен, брокер перезапускался, индекс подняли с нуля. Поэтому при старте и по расписанию
/// сервис читает опубликованные товары публичным API каталога и доводит индекс до каталога (ADR 0009).
/// Операция идемпотентна и не удаляет лишние документы: снятые товары исчезают по событию,
/// а полная пересборка — операция сопровождения (удалить индекс, сервис наполнит его заново).
/// </summary>
public sealed class CatalogIndexReconciler(
    ICatalogProductSource catalog,
    IProductIndex index,
    ILogger<CatalogIndexReconciler> logger)
{
    public async Task<int> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        // Индекс должен существовать с явным маппингом: первый upsert в несуществующий индекс
        // создал бы его с динамическим маппингом, и полнотекстовый поиск по словуформам не работал бы.
        await index.EnsureIndexAsync(cancellationToken);

        var products = await catalog.ListPublishedProductsAsync(cancellationToken);
        var categoryNames = await catalog.GetCategoryNamesAsync(cancellationToken);
        var updatedAt = DateTimeOffset.UtcNow;

        foreach (var product in products)
        {
            categoryNames.TryGetValue(product.CategoryId, out var categoryName);

            await index.UpsertAsync(
                ProductDocumentMapper.FromSnapshot(product, categoryName, updatedAt),
                cancellationToken);
        }

        logger.LogInformation(
            "Индекс поиска согласован с каталогом: {ProductCount} опубликованных товаров, {CategoryCount} категорий.",
            products.Count,
            categoryNames.Count);

        return products.Count;
    }
}
