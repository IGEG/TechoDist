using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techodist.BuildingBlocks.Messaging.Extensions;
using Techodist.Notification.Api.Consumers;

namespace Techodist.Notification.Api.Extensions;

/// <summary>
/// Регистрация потребителей сервиса уведомлений. Шина, retry и автоматическая настройка
/// эндпоинтов приходят из <c>AddTechodistMessaging</c> (ADR 0003), здесь — только список
/// потребителей: у Notification нет своей БД, поэтому transactional outbox не нужен.
/// </summary>
public static class ConsumerRegistrationExtensions
{
    public static IServiceCollection AddNotificationConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddTechodistMessaging(configuration, bus =>
        {
            bus.AddConsumer<OrderSubmittedConsumer>();
            bus.AddConsumer<OrderStatusChangedConsumer>();
        });
    }
}
