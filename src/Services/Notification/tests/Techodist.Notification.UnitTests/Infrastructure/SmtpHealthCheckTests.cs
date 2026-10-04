using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Techodist.Notification.Infrastructure.HealthChecks;
using Techodist.Notification.Infrastructure.Options;
using Xunit;

namespace Techodist.Notification.UnitTests.Infrastructure;

/// <summary>
/// Health-check SMTP: сервис уведомлений считается живым только при доступном почтовом
/// сервере (в dev — MailHog). Проверяется TCP-доступность порта без SMTP-диалога.
/// </summary>
public sealed class SmtpHealthCheckTests
{
    [Fact]
    public async Task Check_WhenSmtpPortIsOpen_IsHealthy()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var (check, context) = Create(Port(listener));

        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("доступен", result.Description);
    }

    [Fact]
    public async Task Check_WhenSmtpPortIsClosed_IsUnhealthy()
    {
        var (check, context) = Create(FreePort());

        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    private static (SmtpHealthCheck Check, HealthCheckContext Context) Create(int port)
    {
        var check = new SmtpHealthCheck(Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = port,
        }));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "notification-smtp",
                check,
                failureStatus: HealthStatus.Unhealthy,
                tags: null),
        };

        return (check, context);
    }

    private static int Port(TcpListener listener) => ((IPEndPoint)listener.LocalEndpoint).Port;

    /// <summary>Свободный порт: слушатель открывается и сразу закрывается — соединение получит отказ.</summary>
    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = Port(listener);
        listener.Stop();

        return port;
    }
}
