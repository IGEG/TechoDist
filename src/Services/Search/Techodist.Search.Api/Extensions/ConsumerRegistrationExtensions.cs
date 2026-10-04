using Techodist.BuildingBlocks.Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techodist.Search.Api.Consumers;

namespace Techodist.Search.Api.Extensions;

/// <summary>
/// Регистрация потребителей сервиса поиска. Шина, retry и автоматическая настройка эндпоинтов
/// приходят из <c>AddTechodistMessaging</c> (ADR 0003); здесь — только список потребителей:
/// своего хранилища у поиска нет, поэтому transactional outbox не нужен.
/// </summary>
public static class ConsumerRegistrationExtensions
{
    public static IServiceCollection AddSearchConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddTechodistMessaging(configuration, bus =>
        {
            bus.AddConsumer<ProductChangedConsumer>();
        });
    }
}
