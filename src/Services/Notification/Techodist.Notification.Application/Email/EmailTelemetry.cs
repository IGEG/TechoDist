using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Core.Diagnostics;
using Techodist.Notification.Application.Abstractions;

namespace Techodist.Notification.Application.Email;

/// <summary>
/// Единая точка отправки писем с телеметрией (ADR 0008): спан <c>notification.send</c> и
/// бизнес-метрики <c>notifications.sent</c>/<c>notifications.failed</c>. Обработчики остаются
/// обычными классами без зависимости от телеметрии, а тесты не меняются: логика отправки
/// не отличается от прямого вызова <see cref="IEmailSender.SendAsync"/>, исключение не глушится
/// (MassTransit обязан повторить доставку).
/// </summary>
internal static class EmailTelemetry
{
    public static async Task SendAsync(
        IEmailSender emails,
        ILogger logger,
        string kind,
        string orderNumber,
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        using var activity = TechodistDiagnostics.StartActivity(TechodistDiagnostics.ActivityNames.NotificationSend);
        activity?.SetTag("notification.kind", kind);
        activity?.SetTag("order.number", orderNumber);

        try
        {
            await emails.SendAsync(message, cancellationToken);
            TechodistDiagnostics.NotificationSent(kind);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            TechodistDiagnostics.NotificationFailed(kind);

            logger.LogError(
                exception,
                "Не удалось отправить письмо ({Kind}) по заявке {OrderNumber} — доставка будет повторена.",
                kind,
                orderNumber);

            throw;
        }
    }
}
