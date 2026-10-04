using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Techodist.Notification.Application.Abstractions;
using Techodist.Notification.Infrastructure.Email;
using Techodist.Notification.Infrastructure.HealthChecks;
using Techodist.Notification.Infrastructure.Idempotency;
using Techodist.Notification.Infrastructure.Options;

namespace Techodist.Notification.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрация инфраструктуры: SMTP-отправка (MailKit), журнал обработанных сообщений
    /// и health-check почтового сервера.
    /// </summary>
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<ProcessedMessageStoreOptions>(
            configuration.GetSection(ProcessedMessageStoreOptions.SectionName));

        // Единый источник времени для сервиса: журнал дедупликации измеряет сроки хранения
        // через TimeProvider, поэтому в тестах время управляемое.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Журнал дедупликации общий на процесс: сообщение приходит один раз, а обработчик
        // резолвится в новом scope — singleton держит отметки между сообщениями.
        services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();

        services.AddHealthChecks()
            .AddCheck<SmtpHealthCheck>("notification-smtp");

        return services;
    }
}
