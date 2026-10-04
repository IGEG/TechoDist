using Techodist.Notification.Application.Email;

namespace Techodist.Notification.Application.Abstractions;

/// <summary>
/// Отправка письма. Реализация — SMTP через MailKit (в dev письма ловит MailHog),
/// ошибки доставки не глушатся: исключение уходит в MassTransit, который повторит попытку.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
