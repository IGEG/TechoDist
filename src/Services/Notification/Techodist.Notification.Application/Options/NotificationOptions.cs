namespace Techodist.Notification.Application.Options;

/// <summary>Настройки писем (секция конфигурации <c>Notifications</c>).</summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Ящик магазина: получатель письма о новой заявке.</summary>
    public string StoreEmail { get; set; } = "orders@techodist.local";

    /// <summary>Имя магазина в подписи писем клиенту.</summary>
    public string StoreName { get; set; } = "Techodist";

    /// <summary>Отправлять ли клиенту подтверждение сразу после оформления заявки.</summary>
    public bool SendCustomerConfirmation { get; set; } = true;
}
