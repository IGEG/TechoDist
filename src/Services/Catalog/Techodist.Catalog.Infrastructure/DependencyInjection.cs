using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Infrastructure.Caching;
using Techodist.Catalog.Infrastructure.Persistence;
using Techodist.Catalog.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techodist.BuildingBlocks.Messaging.Extensions;

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

        // RabbitMQ + transactional outbox на БД каталога: событие об изменении товара публикуется
        // в той же транзакции, что и запись товара, и уходит в брокер уже после коммита (ADR 0003).
        // Потребитель — Search, который обновляет индекс Elasticsearch (ADR 0009).
        services.AddTechodistMessaging(configuration, bus =>
        {
            bus.AddEntityFrameworkOutbox<CatalogDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(5);
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>("catalog-db");

        return services;
    }
}
