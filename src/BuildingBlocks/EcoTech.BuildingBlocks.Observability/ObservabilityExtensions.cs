using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace EcoTech.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Включает структурное логирование (Serilog → консоль) и телеметрию
    /// OpenTelemetry (трейсы + метрики через OTLP-экспортёр).
    /// </summary>
    public static WebApplicationBuilder AddEcoTechObservability(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Host.UseSerilog((_, _, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console());

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        return builder;
    }
}
