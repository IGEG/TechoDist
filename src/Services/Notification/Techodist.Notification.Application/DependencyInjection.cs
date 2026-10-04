using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techodist.Notification.Application.Features.OrderStatusChanged;
using Techodist.Notification.Application.Features.OrderSubmitted;
using Techodist.Notification.Application.Options;

namespace Techodist.Notification.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрация слоя Application: настройки писем и обработчики интеграционных событий.
    /// Обработчики — обычные scoped-сервисы (не MediatR): их вызывает транспортный слой
    /// MassTransit, а не шина команд, поэтому лишнего слоя с сообщениями не заводим.
    /// </summary>
    public static IServiceCollection AddNotificationApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        services.AddScoped<OrderSubmittedEmailHandler>();
        services.AddScoped<OrderStatusChangedEmailHandler>();

        return services;
    }
}
