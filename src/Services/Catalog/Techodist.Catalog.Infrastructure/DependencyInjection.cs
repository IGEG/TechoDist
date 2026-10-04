using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Infrastructure.Caching;
using Techodist.Catalog.Infrastructure.Persistence;
using Techodist.Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("CatalogDb")
            ?? throw new InvalidOperationException("Не задана строка подключения 'CatalogDb'.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>("catalog-db");

        return services;
    }
}
