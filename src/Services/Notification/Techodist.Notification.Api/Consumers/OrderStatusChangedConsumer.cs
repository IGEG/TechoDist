using MassTransit;
using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Notification.Application.Features.OrderStatusChanged;

namespace Techodist.Notification.Api.Consumers;

/// <summary>
/// Потребитель события «статус заявки изменён»: клиент получает письмо с новым статусом.
/// Логика — в <see cref="OrderStatusChangedEmailHandler"/>; повторная доставка того же
/// <c>MessageId</c> письмо не дублирует (ADR 0003).
/// </summary>
public sealed class OrderStatusChangedConsumer(
    OrderStatusChangedEmailHandler handler,
    ILogger<OrderStatusChangedConsumer> logger) : IConsumer<OrderStatusChangedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderStatusChangedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.MessageId is null)
        {
            logger.LogWarning(
                "Смена статуса заявки {OrderNumber} пришла без MessageId — дедупликация недоступна.",
                context.Message.OrderNumber);
        }

        return handler.HandleAsync(context.Message, context.MessageId, context.CancellationToken);
    }
}
