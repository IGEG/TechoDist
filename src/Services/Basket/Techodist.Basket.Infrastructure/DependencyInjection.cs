using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Infrastructure.Caching;
using Techodist.Basket.Infrastructure.Clients;
using Techodist.Basket.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.Basket.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Адрес Catalog API по умолчанию (dev-порт из launchSettings).</summary>
    private const string DefaultCatalogBaseUrl = "http://localhost:5101";

    private static readonly TimeSpan CatalogRequestTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddBasketInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.Configure<BasketStorageOptions>(configuration.GetSection(BasketStorageOptions.SectionName));
        services.AddScoped<IBasketRepository, RedisBasketRepository>();

        // Снимок товара берётся напрямую в Catalog: синхронный вызов для интерактивной операции
        // (добавление в корзину) — сознательный отказ от асинхронности, чтобы покупатель сразу
        // видел актуальную цену (см. docs/adr/0005-anonymous-basket-redis-cookie.md).
        var catalogBaseUrl = configuration["Services:Catalog:BaseUrl"] ?? DefaultCatalogBaseUrl;

        services.AddHttpClient<ICatalogProductClient, CatalogProductClient>(client =>
        {
            client.BaseAddress = new Uri(catalogBaseUrl, UriKind.Absolute);
            client.Timeout = CatalogRequestTimeout;
        });

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("basket-redis");

        return services;
    }
}
