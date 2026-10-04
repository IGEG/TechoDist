using MassTransit;
using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Notification.Application.Features.OrderSubmitted;

namespace Techodist.Notification.Api.Consumers;

/// <summary>
/// Потребитель события «заявка оформлена» (публикует outbox сервиса Order, ADR 0003).
/// Тонкая обёртка: логика — в <see cref="OrderSubmittedEmailHandler"/>, поэтому её проверяют
/// юнит-тестами без брокера. Исключения не глушатся — MassTransit повторит попытку.
/// </summary>
public sealed class OrderSubmittedConsumer(
    OrderSubmittedEmailHandler handler,
    ILogger<OrderSubmittedConsumer> logger) : IConsumer<OrderSubmittedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderSubmittedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.MessageId is null)
        {
            // MassTransit проставляет MessageId сам; отсутствие означает, что сообщение
            // опубликовано в обход шины — тогда дедупликация невозможна.
            logger.LogWarning(
                "Событие заявки {OrderNumber} пришло без MessageId — дедупликация недоступна.",
                context.Message.OrderNumber);
        }

        return handler.HandleAsync(context.Message, context.MessageId, context.CancellationToken);
    }
}
