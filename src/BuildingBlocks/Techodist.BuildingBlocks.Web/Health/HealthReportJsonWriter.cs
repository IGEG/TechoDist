using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Techodist.BuildingBlocks.Web.Health;

/// <summary>
/// JSON-представление health-отчёта: агрегированный статус и состояние каждой проверки.
/// Удобно и для мониторинга, и для ручной проверки (в отличие от текстового «Healthy»).
/// </summary>
public static class HealthReportJsonWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = (int)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = (int)entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
            }),
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, SerializerOptions),
            context.RequestAborted);
    }
}
