using Techodist.ApiGateway.Configuration;
using Techodist.ApiGateway.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Techodist.ApiGateway.Extensions;

/// <summary>
/// Агрегация health-check сервисов (ADR 0007): <c>/health/live</c> — жив ли сам шлюз,
/// <c>/health/ready</c> — доступны ли downstream-сервисы.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var endpoints = configuration
            .GetSection(GatewayConstants.ServicesSectionName)
            .Get<GatewayServiceEndpoint[]>() ?? [];

        services.AddHttpClient(ServiceHealthCheck.HttpClientName, client => client.Timeout = HealthCheckTimeout);

        var builder = services.AddHealthChecks();

        foreach (var endpoint in endpoints)
        {
            builder.Add(new HealthCheckRegistration(
                name: endpoint.Name,
                factory: provider => new ServiceHealthCheck(provider.GetRequiredService<IHttpClientFactory>(), endpoint),
                failureStatus: null,
                tags: [GatewayConstants.ReadyTag],
                timeout: HealthCheckTimeout));
        }

        return services;
    }

    public static WebApplication MapGatewayHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // liveness: проверок нет — эндпоинт жив, пока живёт процесс (не зависит от сервисов).
        app.MapHealthChecks(GatewayConstants.LivePath, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthReportResponseWriter.WriteAsync,
        }).DisableRateLimiting();

        app.MapHealthChecks(GatewayConstants.ReadyPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(GatewayConstants.ReadyTag),
            ResponseWriter = HealthReportResponseWriter.WriteAsync,
        }).DisableRateLimiting();

        return app;
    }
}
