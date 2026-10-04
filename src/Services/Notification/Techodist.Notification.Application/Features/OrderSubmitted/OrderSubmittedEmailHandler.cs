using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Techodist.BuildingBlocks.Core.Diagnostics;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Notification.Application.Abstractions;
using Techodist.Notification.Application.Email;
using Techodist.Notification.Application.Options;
using Techodist.Notification.Application.Templates;

namespace Techodist.Notification.Application.Features.OrderSubmitted;

/// <summary>
/// Реакция на событие «заявка оформлена»: письмо магазину и подтверждение клиенту.
/// Потребитель идемпотентен (ADR 0003): сначала проверяется журнал обработанных сообщений,
/// отметка ставится только после успешной отправки — так повторная доставка не дублирует
/// письма, а упавшая отправка не теряется (сообщение будет повторено брокером).
/// </summary>
public sealed class OrderSubmittedEmailHandler(
    IEmailSender emails,
    IProcessedMessageStore processedMessages,
    IOptions<NotificationOptions> options,
    ILogger<OrderSubmittedEmailHandler> logger)
{
    public async Task HandleAsync(
        OrderSubmittedIntegrationEvent message,
        Guid? messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (await IsDuplicateAsync(messageId, cancellationToken))
        {
            logger.LogInformation(
                "Заявка {OrderNumber} (MessageId {MessageId}) уже обработана — письма не отправляются повторно.",
                message.OrderNumber,
                messageId);

            return;
        }

        var settings = options.Value;

        await EmailTelemetry.SendAsync(
            emails,
            logger,
            TechodistDiagnostics.NotificationKinds.StoreSubmitted,
            message.OrderNumber,
            OrderEmailTemplates.StoreNotification(message, settings.StoreEmail),
            cancellationToken);

        if (settings.SendCustomerConfirmation)
        {
            await EmailTelemetry.SendAsync(
                emails,
                logger,
                TechodistDiagnostics.NotificationKinds.CustomerConfirmation,
                message.OrderNumber,
                OrderEmailTemplates.CustomerConfirmation(message, settings.StoreName),
                cancellationToken);
        }

        await MarkProcessedAsync(messageId, cancellationToken);

        logger.LogInformation(
            "Письма по заявке {OrderNumber} отправлены: магазин {StoreEmail}, клиент {CustomerEmail}.",
            message.OrderNumber,
            settings.StoreEmail,
            message.CustomerEmail);
    }

    private async Task<bool> IsDuplicateAsync(Guid? messageId, CancellationToken cancellationToken)
        => messageId is { } id && await processedMessages.HasProcessedAsync(id, cancellationToken);

    private Task MarkProcessedAsync(Guid? messageId, CancellationToken cancellationToken)
        => messageId is { } id ? processedMessages.MarkProcessedAsync(id, cancellationToken) : Task.CompletedTask;
}
