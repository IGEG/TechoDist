using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Techodist.BuildingBlocks.Web.Health;

/// <summary>
/// Пробы сервиса (ADR 0007/0008): <c>/health/live</c> — живёт ли процесс, <c>/health/ready</c> —
/// доступны ли зависимости (БД, Redis, брокер). По readiness оркестратор решает, пускать ли трафик,
/// поэтому liveness намеренно не трогает внешние системы: иначе перезапустится здоровый под.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>Проба живости процесса.</summary>
    public const string LivePath = "/health/live";

    /// <summary>Проба готовности: все зарегистрированные проверки зависимостей.</summary>
    public const string ReadyPath = "/health/ready";

    public static WebApplication MapTechodistHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthReportJsonWriter.WriteAsync,
        });

        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            ResponseWriter = HealthReportJsonWriter.WriteAsync,
        });

        return app;
    }
}
