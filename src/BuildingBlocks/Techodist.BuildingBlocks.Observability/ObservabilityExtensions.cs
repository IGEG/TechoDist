using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using Techodist.BuildingBlocks.Core.Diagnostics;

namespace Techodist.BuildingBlocks.Observability;

/// <summary>
/// Подключение наблюдаемости сервиса (ADR 0008): структурные логи Serilog (консоль + Loki),
/// трейсы OpenTelemetry (OTLP → Collector → Jaeger) и метрики (эндпоинт <c>/metrics</c> для Prometheus),
/// а также лог-запись на каждый HTTP-запрос.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>Значение лейбла <c>app</c> в Loki — все сервисы Techodist.</summary>
    public const string ApplicationLabelValue = "techodist";

    private const string ServiceNameProperty = "service";
    private const string EnvironmentProperty = "environment";
    private const string ApplicationProperty = "app";

    private static readonly string[] LokiLabelProperties =
        [ServiceNameProperty, EnvironmentProperty, ApplicationProperty];

    /// <summary>
    /// Включает логи, трейсы и метрики сервиса. Настройки берутся из секции <c>Observability</c>
    /// (значения по умолчанию рассчитаны на локальный стек из docker-compose.observability.yml).
    /// </summary>
    public static WebApplicationBuilder AddTechodistObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        options.Normalize();

        // Опции нужны и на этапе сборки пайплайна (UseTechodistObservability: /metrics, request-logging).
        builder.Services.AddSingleton(options);

        ConfigureLogging(builder, serviceName, options);
        ConfigureTelemetry(builder.Services, serviceName, builder.Environment.EnvironmentName, options);

        return builder;
    }

    /// <summary>
    /// Подключает к пайплайну то, что можно включить только на <see cref="WebApplication"/>:
    /// эндпоинт скрейпа <c>/metrics</c> и логирование HTTP-запросов.
    /// </summary>
    public static WebApplication UseTechodistObservability(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetService<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (options.RequestLoggingEnabled)
        {
            app.UseSerilogRequestLogging(logging =>
            {
                logging.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} \u2192 {StatusCode} \u0437\u0430 {Elapsed:0.0} \u043c\u0441";
                logging.GetLevel = (httpContext, _, exception) =>
                    exception is not null || httpContext.Response.StatusCode >= 500
                        ? LogEventLevel.Error
                        : LogEventLevel.Information;
            });
        }

        if (options.PrometheusEnabled)
        {
            app.MapPrometheusScrapingEndpoint(options.EffectivePrometheusPath);
        }

        return app;
    }

    private static void ConfigureLogging(
        WebApplicationBuilder builder,
        string serviceName,
        ObservabilityOptions options)
    {
        builder.Host.UseSerilog((context, _, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                // Информационные логи ASP.NET Core дублируют request-logging — только предупреждения.
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.With(new TraceContextEnricher())
                .Enrich.WithProperty(ServiceNameProperty, serviceName)
                .Enrich.WithProperty(EnvironmentProperty, context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty(ApplicationProperty, ApplicationLabelValue)
                .WriteTo.Console();

            if (options.HasLoki)
            {
                // Логи уезжают прямо в Loki (JSON по умолчанию), а service/environment/app
                // становятся лейблами потока — по ним строятся фильтры и дашборды Grafana.
                configuration.WriteTo.GrafanaLoki(
                    options.LokiUri!,
                    propertiesAsLabels: LokiLabelProperties,
                    restrictedToMinimumLevel: LogEventLevel.Information);
            }
        });
    }

    private static void ConfigureTelemetry(
        IServiceCollection services,
        string serviceName,
        string environment,
        ObservabilityOptions options)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: TechodistDiagnostics.Version)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.namespace"] = ApplicationLabelValue,
                    ["service.instance.id"] = $"{Environment.MachineName}-{Environment.ProcessId}",
                    ["deployment.environment"] = environment,
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(options.TraceSamplingRatio)))
                    .AddAspNetCoreInstrumentation(aspNetCore => aspNetCore.RecordException = true)
                    .AddHttpClientInstrumentation()
                    // EF Core: sql-текст не пишем (может содержать персональные данные) — только длительность.
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(options.EffectiveActivitySources.ToArray());

                if (options.HasOtlpEndpoint)
                {
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.OtlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                // Метрики забирает Prometheus скрейпом /metrics: бизнес-метрики Techodist,
                // ASP.NET Core/HttpClient и runtime (.NET GC, ThreadPool, аллокации).
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(options.EffectiveMeters.ToArray());

                if (options.PrometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }
            });
    }
}
