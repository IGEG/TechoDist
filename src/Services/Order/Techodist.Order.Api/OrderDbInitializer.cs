using Microsoft.EntityFrameworkCore;
using Techodist.Order.Infrastructure.Persistence;

namespace Techodist.Order.Api;

/// <summary>
/// Применяет миграции БД заявок при старте сервиса: таблицы outbox создаются той же
/// миграцией, так что сервис сразу готов публиковать события (ADR 0003). Отдельного
/// сидирования нет — заявки появляются от гостей, а не из демо-данных.
/// </summary>
public static class OrderDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        logger.LogInformation("Применение миграций Order...");

        await db.Database.MigrateAsync();
    }
}
