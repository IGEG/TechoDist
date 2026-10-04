using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Techodist.Notification.Application.Abstractions;
using Techodist.Notification.Application.Email;
using Techodist.Notification.Infrastructure.Options;

namespace Techodist.Notification.Infrastructure.Email;

/// <summary>
/// Отправка письма через SMTP (MailKit). Соединение не пулится: письма редкие (оформление
/// заявки и смена статуса), зато каждое письмо уходит с чистым состоянием сессии, а ошибка
/// соединения сразу становится исключением — MassTransit повторит попытку (ADR 0003).
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = options.Value;
        var mime = BuildMimeMessage(message, settings);

        using var client = new SmtpClient();
        client.Timeout = (int)TimeSpan.FromSeconds(settings.TimeoutSeconds).TotalMilliseconds;

        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await client.AuthenticateAsync(
                settings.Username,
                settings.Password ?? string.Empty,
                cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation(
            "Письмо «{Subject}» отправлено на {Recipient} через {Host}:{Port}.",
            message.Subject,
            message.To,
            settings.Host,
            settings.Port);
    }

    private static MimeMessage BuildMimeMessage(EmailMessage message, SmtpOptions settings)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.Body };

        return mime;
    }
}
