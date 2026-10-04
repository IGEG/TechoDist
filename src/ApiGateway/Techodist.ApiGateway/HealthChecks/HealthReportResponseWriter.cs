using Techodist.BuildingBlocks.Web.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Techodist.ApiGateway.HealthChecks;

/// <summary>
/// JSON-представление health-отчёта шлюза (агрегированный статус и состояние каждого сервиса).
/// Формат общий с сервисными <c>/health/*</c> — см. <see cref="HealthReportJsonWriter"/>,
/// чтобы Grafana/алерты разбирали ответы одинаково.
/// </summary>
public static class HealthReportResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        HealthReportJsonWriter.WriteAsync(context, report);
}

