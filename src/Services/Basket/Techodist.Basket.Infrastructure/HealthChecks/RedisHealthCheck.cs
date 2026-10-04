using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Techodist.Basket.Infrastructure.HealthChecks;

/// <summary>
/// Проверка доступности Redis для <c>/health/live</c>: корзина живёт только в Redis,
/// поэтому «живость» Basket API определяется именно этим хранилищем.
/// </summary>
internal sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    private const string ProbeKey = "health:basket-redis";
    private static readonly TimeSpan ProbeTtl = TimeSpan.FromSeconds(30);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetStringAsync(
                ProbeKey,
                DateTimeOffset.UtcNow.ToString("O"),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ProbeTtl },
                cancellationToken);

            var value = await cache.GetStringAsync(ProbeKey, cancellationToken);

            return value is null
                ? HealthCheckResult.Unhealthy("Redis принял запись, но не вернул контрольное значение.")
                : HealthCheckResult.Healthy("Redis доступен.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis недоступен.", exception);
        }
    }
}
