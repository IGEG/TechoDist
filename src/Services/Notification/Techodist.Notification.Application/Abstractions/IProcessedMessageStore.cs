namespace Techodist.Notification.Application.Abstractions;

/// <summary>
/// Журнал уже обработанных сообщений: дедупликация по <c>MessageId</c> (ADR 0003 — доставка
/// at-least-once, значит потребитель обязан быть идемпотентным).
/// </summary>
public interface IProcessedMessageStore
{
    /// <summary>Обрабатывалось ли сообщение с таким идентификатором раньше.</summary>
    Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает сообщение обработанным. Вызывается только после успешной отправки писем:
    /// если отметка проставлена, а письмо не ушло, повторная доставка сообщения уже не отправит его.
    /// </summary>
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
