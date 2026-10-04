using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Techodist.Order.Infrastructure.Persistence;

/// <summary>
/// Фабрика контекста для design-time (миграции <c>dotnet ef</c>). Строку подключения можно
/// переопределить переменной окружения ORDER_DB.
/// </summary>
internal sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ORDER_DB")
            ?? "Host=localhost;Port=5435;Database=techodist_order;Username=techodist;Password=techodist_dev_pwd";

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName))
            .Options;

        return new OrderDbContext(options);
    }
}
