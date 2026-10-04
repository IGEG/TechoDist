using Techodist.ApiGateway.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Techodist.ApiGateway.HealthChecks;

/// <summary>
/// Проверка доступности сервиса через его liveness-эндпоинт <c>/health/live</c>.
/// Шлюз использует её только для агрегированного <c>/health/ready</c> (ADR 0007).
/// </summary>
public sealed class ServiceHealthCheck(
    IHttpClientFactory httpClientFactory,
    GatewayServiceEndpoint endpoint) : IHealthCheck
{
    /// <summary>Имя именованного <see cref="HttpClient"/> с коротким таймаутом.</summary>
    public const string HttpClientName = "gateway-health";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var client = httpClientFactory.CreateClient(HttpClientName);
        var url = $"{endpoint.Url.TrimEnd('/')}{GatewayConstants.LivePath}";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{(int)response.StatusCode} {url}")
                : HealthCheckResult.Unhealthy($"{(int)response.StatusCode} {url}");
        }
        catch (Exception exception)
        {
            // Сервис может быть просто не поднят (dev): это Unhealthy, а не ошибка шлюза.
            return HealthCheckResult.Unhealthy($"{url} недоступен: {exception.Message}");
        }
    }
}
