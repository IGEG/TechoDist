using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Common;

namespace Techodist.Search.Application.Features.ProductChanged;

/// <summary>
/// Реакция поиска на изменение товара в каталоге (ADR 0009). Индекс хранит только опубликованные
/// товары, поэтому черновик, архив и удаление убирают документ, а не помечают его признаком:
/// фильтр «IsPublished» в каждом запросе тогда не нужен, а витрина не может показать снятый товар.
/// Обработка идемпотентна (upsert/delete по идентификатору товара), поэтому повторная доставка
/// сообщения безопасна — отдельный журнал дедупликации, как в Notification, не требуется.
/// </summary>
public sealed class ProductChangedIndexer(IProductIndex index, ILogger<ProductChangedIndexer> logger)
{
    public async Task HandleAsync(
        ProductChangedIntegrationEvent changed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changed);

        if (changed.ChangeType == ProductChangeType.Deleted)
        {
            await index.DeleteAsync(changed.ProductId, cancellationToken);
            logger.LogInformation("Товар {ProductId} удалён из поискового индекса.", changed.ProductId);

            return;
        }

        if (!changed.IsPublished)
        {
            await index.DeleteAsync(changed.ProductId, cancellationToken);
            logger.LogInformation(
                "Товар {ProductId} снят с индекса: карточка не опубликована (черновик или архив).",
                changed.ProductId);

            return;
        }

        await index.UpsertAsync(ProductDocumentMapper.FromEvent(changed), cancellationToken);

        logger.LogInformation("Товар {ProductId} обновлён в поисковом индексе.", changed.ProductId);
    }
}
