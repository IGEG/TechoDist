using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Techodist.Notification.Infrastructure.Options;

namespace Techodist.Notification.Infrastructure.HealthChecks;

/// <summary>
/// Проверка доступности SMTP для <c>/health/live</c>: без почтового сервера сервис
/// уведомлений бесполезен, поэтому «живость» определяется именно соединением с SMTP
/// (MailHog в dev). Проверяется TCP-доступность, а не полный SMTP-диалог: он заметно дороже,
/// а ошибки отправки всё равно видны в логах потребителей и в MassTransit.
/// </summary>
public sealed class SmtpHealthCheck(IOptions<SmtpOptions> options) : IHealthCheck
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var target = $"{settings.Host}:{settings.Port}";

        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ConnectTimeout);

            await client.ConnectAsync(settings.Host, settings.Port, timeout.Token);

            return HealthCheckResult.Healthy($"SMTP {target} доступен.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy($"SMTP {target} недоступен.", exception);
        }
    }
}
