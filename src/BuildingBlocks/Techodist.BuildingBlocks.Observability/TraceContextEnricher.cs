using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Techodist.BuildingBlocks.Observability;

/// <summary>
/// Обогащает лог-событие идентификаторами активного трейса (<c>TraceId</c>/<c>SpanId</c>), поэтому
/// запись в Loki связывается с трейсом в Jaeger (ADR 0008). Если активности нет (старт сервиса,
/// фоновые задачи без спана) — свойства не добавляются, лог остаётся валидным.
/// </summary>
public sealed class TraceContextEnricher : ILogEventEnricher
{
    /// <summary>Имя свойства с 32-символьным W3C trace-id.</summary>
    public const string TraceIdPropertyName = "TraceId";

    /// <summary>Имя свойства с 16-символьным W3C span-id.</summary>
    public const string SpanIdPropertyName = "SpanId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        // Иерархический формат (по умолчанию у Activity вне ASP.NET Core) не даёт W3C-идентификаторов,
        // а значения по умолчанию пусты — такие события не обогащаем.
        var activity = Activity.Current;

        if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(TraceIdPropertyName, activity.TraceId.ToHexString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(SpanIdPropertyName, activity.SpanId.ToHexString()));
    }
}
