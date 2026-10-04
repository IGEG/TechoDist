using Microsoft.Extensions.Diagnostics.HealthChecks;
using Techodist.Search.Infrastructure.HealthChecks;
using Techodist.Search.UnitTests.Fakes;
using Xunit;

namespace Techodist.Search.UnitTests.Infrastructure;

/// <summary>Health-check Elasticsearch: недоступный кластер — Unhealthy, доступный — Healthy.</summary>
public sealed class ElasticsearchHealthCheckTests
{
    private readonly FakeProductIndex _index = new();

    [Fact]
    public async Task CheckHealthAsync_WhenClusterAvailable_IsHealthy()
    {
        var result = await CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClusterUnavailable_IsUnhealthy()
    {
        _index.IsAvailable = false;

        var result = await CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private Task<HealthCheckResult> CheckHealthAsync()
        => new ElasticsearchHealthCheck(_index).CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration("search-elasticsearch", _ => null!, null, null),
            },
            CancellationToken.None);
}
