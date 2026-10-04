using MassTransit;
using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Search.Application.Features.ProductChanged;

namespace Techodist.Search.Api.Consumers;

/// <summary>
/// Потребитель события «товар каталога изменился» (публикует outbox сервиса Catalog, ADR 0003).
/// Тонкая обёртка: правила индексации живут в <see cref="ProductChangedIndexer"/>, поэтому они
/// проверяются юнит-тестами без брокера. Исключения не глушатся — MassTransit повторит доставку,
/// а обработка идемпотентна (upsert/delete по идентификатору товара).
/// </summary>
public sealed class ProductChangedConsumer(
    ProductChangedIndexer indexer,
    ILogger<ProductChangedConsumer> logger) : IConsumer<ProductChangedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ProductChangedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        logger.LogInformation(
            "Получено событие об изменении товара {ProductId} ({ChangeType}).",
            context.Message.ProductId,
            context.Message.ChangeType);

        return indexer.HandleAsync(context.Message, context.CancellationToken);
    }
}
