namespace Techodist.Notification.Infrastructure.Options;

/// <summary>
/// Настройки SMTP (секция конфигурации <c>Smtp</c>). В dev — MailHog из
/// <c>deploy/docker-compose/docker-compose.infrastructure.yml</c>: логин и пароль не нужны,
/// письма видны в веб-интерфейсе <c>http://localhost:8025</c>.
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    /// <summary>Использовать STARTTLS (в dev-ловушке MailHog шифрование выключено).</summary>
    public bool UseStartTls { get; set; }

    /// <summary>Логин SMTP; пусто — анонимная отправка (MailHog).</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>Адрес отправителя в письмах.</summary>
    public string FromAddress { get; set; } = "no-reply@techodist.local";

    /// <summary>Имя отправителя в письмах.</summary>
    public string FromName { get; set; } = "Techodist";

    /// <summary>Таймаут соединения с SMTP, секунды.</summary>
    public int TimeoutSeconds { get; set; } = 15;
}
