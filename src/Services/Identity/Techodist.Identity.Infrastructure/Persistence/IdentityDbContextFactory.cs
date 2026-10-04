using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Techodist.Identity.Infrastructure.Persistence;

/// <summary>
/// Фабрика контекста для design-time (создание миграций через `dotnet ef`).
/// Строку подключения можно переопределить переменной окружения IDENTITY_DB.
/// </summary>
internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("IDENTITY_DB")
            ?? "Host=localhost;Port=5434;Database=techodist_identity;Username=techodist;Password=techodist_dev_pwd";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName))
            .Options;

        return new IdentityDbContext(options);
    }
}
