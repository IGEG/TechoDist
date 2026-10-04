using Techodist.ApiGateway.UnitTests.Infrastructure;
using Xunit;

namespace Techodist.ApiGateway.UnitTests.Gateway;

/// <summary>Адреса сервисов: маршрутизация и агрегированный health-check должны смотреть в одно место.</summary>
public sealed class GatewayServiceConfigurationTests
{
    private const string LocalCatalogAddress = "http://localhost:5101";
    private const string LocalIdentityAddress = "http://localhost:5102";
    private const string LocalBasketAddress = "http://localhost:5103";
    private const string LocalOrderAddress = "http://localhost:5104";

    [Fact]
    public void DevelopmentClusters_PointToLocalServicePorts()
    {
        var configuration = GatewayTestConfiguration.Build();

        var destinations = GatewayTestConfiguration.GetClusterDestinations(configuration);

        Assert.Equal(LocalCatalogAddress, destinations["catalog"]);
        Assert.Equal(LocalIdentityAddress, destinations["identity"]);
        Assert.Equal(LocalBasketAddress, destinations["basket"]);
        Assert.Equal(LocalOrderAddress, destinations["order"]);
    }

    [Fact]
    public void HealthAggregation_TargetsTheSameServicesAsRouting()
    {
        var configuration = GatewayTestConfiguration.Build();

        var services = GatewayTestConfiguration.GetHealthServices(configuration);

        Assert.Equal(
            new[] { LocalCatalogAddress, LocalIdentityAddress, LocalBasketAddress, LocalOrderAddress },
            services.Select(service => service.Url).Order(StringComparer.Ordinal).ToArray());

        Assert.Equal(
            new[] { "basket-api", "catalog-api", "identity-api", "order-api" },
            services.Select(service => service.Name).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void EveryCluster_IsCoveredByHealthAggregation()
    {
        var configuration = GatewayTestConfiguration.Build();

        var clusterAddresses = GatewayTestConfiguration.GetClusterDestinations(configuration)
            .Values
            .Order(StringComparer.Ordinal)
            .ToArray();

        var healthAddresses = GatewayTestConfiguration.GetHealthServices(configuration)
            .Select(service => service.Url)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Сервис без health-check выпадет из /health/ready незамеченным — держим списки синхронными.
        Assert.Equal(clusterAddresses, healthAddresses);
    }
}
