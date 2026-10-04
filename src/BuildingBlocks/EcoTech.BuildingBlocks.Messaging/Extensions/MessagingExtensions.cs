using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoTech.BuildingBlocks.Messaging.Extensions;

public static class MessagingExtensions
{
    /// <summary>
    /// Регистрирует MassTransit с транспортом RabbitMQ, повторными попытками и
    /// автоматической настройкой эндпоинтов. Потребители/саги настраиваются
    /// вызывающим кодом через <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddEcoTechMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMassTransit(bus =>
        {
            configure?.Invoke(bus);

            bus.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;

                cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                cfg.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
