using Techodist.BuildingBlocks.Core.Diagnostics;

namespace Techodist.BuildingBlocks.Observability;

/// <summary>
/// Настройки наблюдаемости (секция <c>Observability</c> конфигурации, переопределяется переменными
/// окружения вида <c>Observability__OtlpEndpoint</c>). Значения по умолчанию рассчитаны на локальный
/// стек из <c>deploy/docker-compose/docker-compose.observability.yml</c> (ADR 0008).
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Observability";

    /// <summary>Названия инструментов MassTransit (трейсы/метрики публикации и потребления сообщений).</summary>
    public const string MassTransitInstrumentationName = "MassTransit";

    /// <summary>OTLP-эндпоинт коллектора. Пусто — трейсы/метрики наружу не уходят (только Prometheus/логи).</summary>
    public string? OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>Отдавать ли <c>/metrics</c> для скрейпа Prometheus'ом.</summary>
    public bool PrometheusEnabled { get; set; } = true;

    /// <summary>Путь эндпоинта метрик (нормализуется к виду с ведущим «/»).</summary>
    public string PrometheusPath { get; set; } = "/metrics";

    /// <summary>Адрес Loki. Пусто — сервис пишет только в консоль.</summary>
    public string? LokiUri { get; set; }

    /// <summary>Логировать ли каждый HTTP-запрос (одна запись Serilog на запрос: method, path, status, ms).</summary>
    public bool RequestLoggingEnabled { get; set; } = true;

    /// <summary>Доля сэмплируемых трейсов: 0 — ничего, 1 — все.</summary>
    public double TraceSamplingRatio { get; set; } = 1d;

    /// <summary>Дополнительные ActivitySource'ы (сервисные библиотеки) сверх стандартных.</summary>
    public string[] AdditionalActivitySources { get; set; } = [];

    /// <summary>Дополнительные Meter'ы сверх стандартных.</summary>
    public string[] AdditionalMeters { get; set; } = [];

    /// <summary>Есть ли куда экспортировать трейсы/метрики по OTLP.</summary>
    public bool HasOtlpEndpoint => !string.IsNullOrWhiteSpace(OtlpEndpoint);

    /// <summary>Настроен ли экспорт логов в Loki.</summary>
    public bool HasLoki => !string.IsNullOrWhiteSpace(LokiUri);

    /// <summary>Путь метрик с ведущим «/» (<c>metrics</c> → <c>/metrics</c>).</summary>
    public string EffectivePrometheusPath =>
        string.IsNullOrWhiteSpace(PrometheusPath) ? "/metrics" : "/" + PrometheusPath.Trim().TrimStart('/');

    /// <summary>Подписываемые источники трейсов: свои активности + MassTransit + дополнительные.</summary>
    public IReadOnlyList<string> EffectiveActivitySources =>
        Distinct([TechodistDiagnostics.ActivitySourceName, MassTransitInstrumentationName], AdditionalActivitySources);

    /// <summary>Подписываемые метры: бизнес-метрики + MassTransit + дополнительные.</summary>
    public IReadOnlyList<string> EffectiveMeters =>
        Distinct([TechodistDiagnostics.MeterName, MassTransitInstrumentationName], AdditionalMeters);

    /// <summary>
    /// Приводит значения из конфигурации к рабочему виду: обрезает пробелы, чинит путь, гасит
    /// невалидную долю сэмплирования. Вызывается один раз при старте сервиса.
    /// </summary>
    public void Normalize()
    {
        OtlpEndpoint = string.IsNullOrWhiteSpace(OtlpEndpoint) ? null : OtlpEndpoint.Trim();
        LokiUri = string.IsNullOrWhiteSpace(LokiUri) ? null : LokiUri.Trim();

        if (string.IsNullOrWhiteSpace(PrometheusPath))
        {
            PrometheusPath = "/metrics";
        }

        if (double.IsNaN(TraceSamplingRatio) || TraceSamplingRatio < 0d)
        {
            TraceSamplingRatio = 0d;
        }
        else if (TraceSamplingRatio > 1d)
        {
            TraceSamplingRatio = 1d;
        }

        AdditionalActivitySources = Clean(AdditionalActivitySources);
        AdditionalMeters = Clean(AdditionalMeters);
    }

    private static IReadOnlyList<string> Distinct(IEnumerable<string> defaults, IEnumerable<string> additional)
    {
        var names = new List<string>();

        foreach (var name in defaults.Concat(additional))
        {
            var trimmed = name?.Trim();

            if (!string.IsNullOrEmpty(trimmed) && !names.Contains(trimmed, StringComparer.Ordinal))
            {
                names.Add(trimmed);
            }
        }

        return names;
    }

    private static string[] Clean(IEnumerable<string>? values) =>
        Distinct([], values ?? []).ToArray();
}
