namespace Techodist.Notification.Infrastructure.Idempotency;

/// <summary>
/// Настройки журнала обработанных сообщений (секция конфигурации
/// <c>Notifications:ProcessedMessages</c>).
/// </summary>
public sealed class ProcessedMessageStoreOptions
{
    public const string SectionName = "Notifications:ProcessedMessages";

    /// <summary>
    /// Сколько хранить отметку об обработке сообщения. Просроченная отметка не считается
    /// признаком дубликата: повторная доставка через сутки — это уже новое письмо.
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Верхняя граница журнала: самые старые отметки вытесняются.</summary>
    public int Capacity { get; set; } = 10_000;
}
