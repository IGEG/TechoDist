using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techodist.BuildingBlocks.Messaging.Extensions;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Infrastructure.Clients;
using Techodist.Order.Infrastructure.Numbering;
using Techodist.Order.Infrastructure.Persistence;
using Techodist.Order.Infrastructure.Persistence.Repositories;

namespace Techodist.Order.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Адрес Basket API по умолчанию (dev-порт из launchSettings).</summary>
    private const string DefaultBasketBaseUrl = "http://localhost:5103";

    private static readonly TimeSpan BasketRequestTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddOrderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("OrderDb")
            ?? throw new InvalidOperationException("Не задана строка подключения 'OrderDb'.");

        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();

        // Корзина читается синхронно: без её позиций заявку не из чего собрать (ADR 0005).
        var basketBaseUrl = configuration["Services:Basket:BaseUrl"] ?? DefaultBasketBaseUrl;

        services.AddHttpClient<IBasketClient, BasketClient>(client =>
        {
            client.BaseAddress = new Uri(basketBaseUrl, UriKind.Absolute);
            client.Timeout = BasketRequestTimeout;
        });

        // RabbitMQ + transactional outbox на БД заявок: событие публикуется в той же транзакции,
        // что и заявка, и уходит в брокер уже после коммита (ADR 0003).
        services.AddTechodistMessaging(configuration, bus =>
        {
            bus.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(5);
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<OrderDbContext>("order-db");

        return services;
    }
}
