using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techodist.BuildingBlocks.Core.Diagnostics;
using Xunit;

namespace Techodist.BuildingBlocks.Observability.UnitTests.Observability;

/// <summary>
/// Подключение наблюдаемости целиком: расширения <c>AddTechodistObservability</c> /
/// <c>UseTechodistObservability</c> собирают реальный хост (ADR 0008), поэтому проверяем
/// и настройки в DI, и живой эндпоинт <c>/metrics</c>, по которому ходит Prometheus.
/// </summary>
public sealed class ObservabilityExtensionsTests
{
    [Fact]
    public void AddTechodistObservability_BindsNormalizedOptionsFromConfiguration()
    {
        var builder = CreateBuilder(prometheusEnabled: true, otlpEndpoint: " ", lokiUri: " http://localhost:3100 ");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observability:PrometheusPath"] = "internal/metrics",
            ["Observability:TraceSamplingRatio"] = "2",
        });

        builder.AddTechodistObservability("observability-test-api");

        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<ObservabilityOptions>();

        Assert.Null(options.OtlpEndpoint);
        Assert.False(options.HasOtlpEndpoint);
        Assert.Equal("http://localhost:3100", options.LokiUri);
        Assert.True(options.HasLoki);
        Assert.Equal("/internal/metrics", options.EffectivePrometheusPath);
        Assert.Equal(1d, options.TraceSamplingRatio);
    }

    [Fact]
    public async Task MetricsEndpoint_ExposesBusinessMetricsForPrometheus()
    {
        await using var app = await StartApiAsync(prometheusEnabled: true);

        // Метрика записывается в том же процессе: данные попадают в экспортёр без коллектора.
        TechodistDiagnostics.OrderSubmitted(1500.25m, "TST", "observability-tests");

        var payload = await ScrapeAsync(app, "/metrics");

        Assert.Contains("techodist_orders_submitted_total", payload);
        Assert.Contains("currency=\"TST\"", payload);
        Assert.Contains("channel=\"observability-tests\"", payload);
    }

    [Fact]
    public async Task MetricsEndpoint_IsNotMappedWhenPrometheusIsDisabled()
    {
        await using var app = await StartApiAsync(prometheusEnabled: false);

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var response = await client.GetAsync(new Uri("/metrics", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationBuilder CreateBuilder(bool prometheusEnabled, string otlpEndpoint, string lokiUri)
    {
        var builder = WebApplication.CreateBuilder();

        // Порт 0 — Kestrel сам выберет свободный, поэтому тесты не конфликтуют друг с другом.
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ObservabilityOptions.SectionName + ":OtlpEndpoint"] = otlpEndpoint,
            [ObservabilityOptions.SectionName + ":LokiUri"] = lokiUri,
            [ObservabilityOptions.SectionName + ":PrometheusEnabled"] =
                prometheusEnabled.ToString(CultureInfo.InvariantCulture),
        });

        return builder;
    }

    private static async Task<WebApplication> StartApiAsync(bool prometheusEnabled)
    {
        // Коллектора и Loki рядом нет: пустые эндпоинты отключают экспорт, и тесты не шумят в лог.
        var builder = CreateBuilder(prometheusEnabled, otlpEndpoint: string.Empty, lokiUri: string.Empty);

        builder.AddTechodistObservability("observability-test-api");

        var app = builder.Build();
        app.UseTechodistObservability();

        await app.StartAsync();

        return app;
    }

    private static async Task<string> ScrapeAsync(WebApplication app, string path)
    {
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        return await client.GetStringAsync(new Uri(path, UriKind.Relative));
    }
}
