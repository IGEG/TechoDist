using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Notification.Application.Abstractions;
using Techodist.Notification.Application.Options;
using Techodist.Notification.Application.Templates;

namespace Techodist.Notification.Application.Features.OrderStatusChanged;

/// <summary>
/// Реакция на событие «статус заявки изменён»: клиент получает письмо с новым статусом
/// и комментарием менеджера. Идемпотентность — по <c>MessageId</c>, как в письме о заявке.
/// </summary>
public sealed class OrderStatusChangedEmailHandler(
    IEmailSender emails,
    IProcessedMessageStore processedMessages,
    IOptions<NotificationOptions> options,
    ILogger<OrderStatusChangedEmailHandler> logger)
{
    public async Task HandleAsync(
        OrderStatusChangedIntegrationEvent message,
        Guid? messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (messageId is { } id && await processedMessages.HasProcessedAsync(id, cancellationToken))
        {
            logger.LogInformation(
                "Смена статуса заявки {OrderNumber} (MessageId {MessageId}) уже обработана — письмо не отправляется повторно.",
                message.OrderNumber,
                id);

            return;
        }

        await emails.SendAsync(
            OrderEmailTemplates.StatusChange(message, options.Value.StoreName),
            cancellationToken);

        if (messageId is { } processedId)
        {
            await processedMessages.MarkProcessedAsync(processedId, cancellationToken);
        }

        logger.LogInformation(
            "Клиент {CustomerEmail} уведомлён о смене статуса заявки {OrderNumber}: {OldStatus} -> {NewStatus}.",
            message.CustomerEmail,
            message.OrderNumber,
            message.OldStatus,
            message.NewStatus);
    }
}
