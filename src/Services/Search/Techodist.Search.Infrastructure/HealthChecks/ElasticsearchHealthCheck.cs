using Microsoft.Extensions.Diagnostics.HealthChecks;
using Techodist.Search.Application.Abstractions;

namespace Techodist.Search.Infrastructure.HealthChecks;

/// <summary>
/// Проверка доступности Elasticsearch для <c>/health/live</c>: без кластера сервис поиска
/// бесполезен. Работает через абстракцию <see cref="IProductIndex"/>, поэтому проверка
/// покрывается юнит-тестом с подменным индексом (как SmtpHealthCheck в Notification).
/// </summary>
internal sealed class ElasticsearchHealthCheck(IProductIndex index) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (await index.IsAvailableAsync(cancellationToken))
        {
            return HealthCheckResult.Healthy("Elasticsearch доступен.");
        }

        return HealthCheckResult.Unhealthy("Elasticsearch недоступен.");
    }
}
