using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Techodist.Catalog.Infrastructure.Persistence;

/// <summary>
/// Фабрика контекста для design-time (создание миграций через `dotnet ef`).
/// Строку подключения можно переопределить переменной окружения CATALOG_DB.
/// </summary>
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CATALOG_DB")
            ?? "Host=localhost;Port=5433;Database=techodist_catalog;Username=techodist;Password=techodist_dev_pwd";

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName))
            .Options;

        return new CatalogDbContext(options);
    }
}
