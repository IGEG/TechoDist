using Techodist.BuildingBlocks.Core.Diagnostics;
using Xunit;

namespace Techodist.BuildingBlocks.Observability.UnitTests.Observability;

/// <summary>
/// Настройки наблюдаемости: значения из конфигурации приходят строками, поэтому их нужно
/// нормализовать до рабочего вида (ADR 0008) — иначе сервис упадёт на старте провайдера
/// телеметрии или будет скрейпиться Prometheus'ом по пустому пути.
/// </summary>
public sealed class ObservabilityOptionsTests
{
    [Fact]
    public void Defaults_AreLocalStackFriendly()
    {
        var options = new ObservabilityOptions();

        Assert.Equal("http://localhost:4317", options.OtlpEndpoint);
        Assert.True(options.HasOtlpEndpoint);
        Assert.True(options.PrometheusEnabled);
        Assert.Equal("/metrics", options.EffectivePrometheusPath);
        Assert.True(options.RequestLoggingEnabled);
        Assert.Equal(1d, options.TraceSamplingRatio);
        Assert.False(options.HasLoki);
    }

    [Fact]
    public void Normalize_TrimsEndpointsAndFixesMetricsPath()
    {
        var options = new ObservabilityOptions
        {
            OtlpEndpoint = "  http://collector:4317  ",
            LokiUri = " http://loki:3100 ",
            PrometheusPath = " internal/metrics ",
        };

        options.Normalize();

        Assert.Equal("http://collector:4317", options.OtlpEndpoint);
        Assert.Equal("http://loki:3100", options.LokiUri);
        Assert.True(options.HasLoki);
        Assert.Equal("/internal/metrics", options.EffectivePrometheusPath);
    }

    [Fact]
    public void Normalize_BlankEndpoints_TurnExportOff()
    {
        var options = new ObservabilityOptions
        {
            // Так сервис в CI/тестах отключает экспорт: пустая переменная окружения побеждает default.
            OtlpEndpoint = "   ",
            LokiUri = string.Empty,
            PrometheusPath = "  ",
        };

        options.Normalize();

        Assert.Null(options.OtlpEndpoint);
        Assert.Null(options.LokiUri);
        Assert.False(options.HasOtlpEndpoint);
        Assert.False(options.HasLoki);
        Assert.Equal("/metrics", options.EffectivePrometheusPath);
    }

    [Theory]
    [InlineData(2d, 1d)]
    [InlineData(0.25d, 0.25d)]
    [InlineData(-1d, 0d)]
    [InlineData(double.NaN, 0d)]
    public void Normalize_ClampsSamplingRatio(double configured, double expected)
    {
        var options = new ObservabilityOptions { TraceSamplingRatio = configured };

        options.Normalize();

        Assert.Equal(expected, options.TraceSamplingRatio);
    }

    [Fact]
    public void Normalize_CleansAdditionalSourcesAndDropsDuplicates()
    {
        var options = new ObservabilityOptions
        {
            AdditionalActivitySources = [" Custom.Source ", "", "MassTransit", "Custom.Source"],
            AdditionalMeters = ["  ", "Custom.Meter", TechodistDiagnostics.MeterName],
        };

        options.Normalize();

        // Пробелы и повторы внутри списка убираются; имена, дублирующие стандартные, остаются,
        // но в подписке провайдера (Effective*) они не повторяются.
        Assert.Equal(["Custom.Source", ObservabilityOptions.MassTransitInstrumentationName], options.AdditionalActivitySources);
        Assert.Equal(["Custom.Meter", TechodistDiagnostics.MeterName], options.AdditionalMeters);

        Assert.Equal(
            [TechodistDiagnostics.ActivitySourceName, ObservabilityOptions.MassTransitInstrumentationName, "Custom.Source"],
            options.EffectiveActivitySources);

        Assert.Equal(
            [TechodistDiagnostics.MeterName, ObservabilityOptions.MassTransitInstrumentationName, "Custom.Meter"],
            options.EffectiveMeters);
    }

    [Fact]
    public void EffectiveSources_AlwaysIncludeTechodistAndMassTransitOnce()
    {
        var options = new ObservabilityOptions();

        options.Normalize();

        Assert.Equal(
            [TechodistDiagnostics.ActivitySourceName, ObservabilityOptions.MassTransitInstrumentationName],
            options.EffectiveActivitySources);

        Assert.Equal(
            [TechodistDiagnostics.MeterName, ObservabilityOptions.MassTransitInstrumentationName],
            options.EffectiveMeters);
    }

    [Fact]
    public void EffectiveSources_PutDefaultsFirstSoProviderAlwaysSubscribesThem()
    {
        var options = new ObservabilityOptions
        {
            AdditionalActivitySources = ["Custom.Source"],
            AdditionalMeters = ["Custom.Meter"],
        };

        options.Normalize();

        Assert.Equal(
            [TechodistDiagnostics.ActivitySourceName, ObservabilityOptions.MassTransitInstrumentationName, "Custom.Source"],
            options.EffectiveActivitySources);

        Assert.Equal(
            [TechodistDiagnostics.MeterName, ObservabilityOptions.MassTransitInstrumentationName, "Custom.Meter"],
            options.EffectiveMeters);
    }
}
