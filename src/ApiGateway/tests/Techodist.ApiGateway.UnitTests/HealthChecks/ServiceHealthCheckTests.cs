using System.Net;
using Techodist.ApiGateway.Configuration;
using Techodist.ApiGateway.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Techodist.ApiGateway.UnitTests.HealthChecks;

public sealed class ServiceHealthCheckTests
{
    private static readonly GatewayServiceEndpoint CatalogEndpoint = new("catalog-api", "http://localhost:5101");

    [Theory]
    [InlineData("http://localhost:5101")]
    [InlineData("http://localhost:5101/")]
    public async Task HealthyService_ReturnsHealthyAndCallsLivenessEndpoint(string address)
    {
        Uri? requestedUri = null;
        var check = new ServiceHealthCheck(
            new StubHttpClientFactory(request =>
            {
                requestedUri = request.RequestUri;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }),
            new GatewayServiceEndpoint("catalog-api", address));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("http://localhost:5101/health/live", requestedUri?.ToString());
    }

    [Fact]
    public async Task FailingService_ReturnsUnhealthyWithStatusCode()
    {
        var check = new ServiceHealthCheck(
            new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            CatalogEndpoint);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("503", result.Description);
    }

    [Fact]
    public async Task UnreachableService_ReturnsUnhealthyInsteadOfThrowing()
    {
        var check = new ServiceHealthCheck(
            new StubHttpClientFactory(_ => throw new HttpRequestException("нет соединения")),
            CatalogEndpoint);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("http://localhost:5101/health/live", result.Description);
    }

    private sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> response)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(response), disposeHandler: true);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(response(request));
    }
}
