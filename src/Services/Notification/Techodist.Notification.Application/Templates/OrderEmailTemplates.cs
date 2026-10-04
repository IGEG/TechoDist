using System.Globalization;
using System.Text;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Notification.Application.Email;

namespace Techodist.Notification.Application.Templates;

/// <summary>
/// Текстовые шаблоны писем по заявкам. Шаблон — чистая функция «событие → письмо»,
/// поэтому проверяется юнит-тестами без SMTP и брокера, а отправкой занимается
/// <see cref="Abstractions.IEmailSender"/>. Суммы печатаются в рублях: заявка ведётся
/// в единственной валюте <c>RUB</c>, а в событии есть только сумма.
/// </summary>
public static class OrderEmailTemplates
{
    private const string DateFormat = "dd.MM.yyyy HH:mm";

    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pending"] = "ожидает подтверждения",
            ["Confirmed"] = "подтверждена",
            ["InProgress"] = "в работе",
            ["Completed"] = "выполнена",
            ["Cancelled"] = "отменена",
        };

    /// <summary>
    /// Письмо магазину о новой заявке: заявку должен увидеть менеджер, поэтому в теле —
    /// контакты покупателя, состав заявки и итог.
    /// </summary>
    public static EmailMessage StoreNotification(OrderSubmittedIntegrationEvent message, string storeEmail)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeEmail);

        var body = new StringBuilder();

        body.AppendLine($"Новая заявка {message.OrderNumber} от {FormatDate(message.SubmittedAt)}.");
        body.AppendLine();
        body.AppendLine($"Покупатель: {message.CustomerName}");
        body.AppendLine($"E-mail: {message.CustomerEmail}");
        body.AppendLine($"Телефон: {OrNotSpecified(message.CustomerPhone)}");
        body.AppendLine($"Предпочтительный способ связи: {ChannelLabel(message.PreferredChannel)}");
        body.AppendLine($"Срочность: {PriorityLabel(message.Priority)}");
        body.AppendLine();
        body.AppendLine("Состав заявки:");
        AppendItems(body, message.Items);
        body.AppendLine();
        body.AppendLine($"Итого: {FormatAmount(message.TotalAmount)}");

        if (!string.IsNullOrWhiteSpace(message.Comment))
        {
            body.AppendLine();
            body.AppendLine($"Комментарий клиента: {message.Comment.Trim()}");
        }

        body.AppendLine();
        body.AppendLine("Следующий шаг: подтвердите заявку в админ-панели — клиент получит письмо о смене статуса.");

        return new EmailMessage(
            storeEmail,
            $"Новая заявка {message.OrderNumber} — {FormatAmount(message.TotalAmount)}",
            body.ToString().TrimEnd());
    }

    /// <summary>Подтверждение клиенту: заявка принята, менеджер свяжется по указанному каналу.</summary>
    public static EmailMessage CustomerConfirmation(OrderSubmittedIntegrationEvent message, string storeName)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);

        var body = new StringBuilder();

        body.AppendLine($"Здравствуйте, {message.CustomerName}!");
        body.AppendLine();
        body.AppendLine($"Мы получили вашу заявку {message.OrderNumber} от {FormatDate(message.SubmittedAt)} " +
                        "и передали её менеджеру магазина.");
        body.AppendLine();
        body.AppendLine("Состав заявки:");
        AppendItems(body, message.Items);
        body.AppendLine();
        body.AppendLine($"Итого: {FormatAmount(message.TotalAmount)}");
        body.AppendLine();
        body.AppendLine($"Менеджер свяжется с вами по указанному контакту ({ChannelLabel(message.PreferredChannel)}). " +
                        $"Статус заявки можно проверить на сайте по номеру {message.OrderNumber}.");
        body.AppendLine();
        body.AppendLine("С уважением,");
        body.AppendLine(storeName);

        return new EmailMessage(
            message.CustomerEmail,
            $"Заявка {message.OrderNumber} принята",
            body.ToString().TrimEnd());
    }

    /// <summary>Уведомление клиенту о смене статуса заявки менеджером.</summary>
    public static EmailMessage StatusChange(OrderStatusChangedIntegrationEvent message, string storeName)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);

        var body = new StringBuilder();

        body.AppendLine($"Здравствуйте, {message.CustomerName}!");
        body.AppendLine();
        body.AppendLine($"Статус вашей заявки {message.OrderNumber} изменился: " +
                        $"{StatusLabel(message.OldStatus)} → {StatusLabel(message.NewStatus)} " +
                        $"({FormatDate(message.ChangedAt)}).");

        if (!string.IsNullOrWhiteSpace(message.ManagerComment))
        {
            body.AppendLine();
            body.AppendLine($"Комментарий менеджера: {message.ManagerComment.Trim()}");
        }

        body.AppendLine();
        body.AppendLine("С уважением,");
        body.AppendLine(storeName);

        return new EmailMessage(
            message.CustomerEmail,
            $"Заявка {message.OrderNumber}: {StatusLabel(message.NewStatus)}",
            body.ToString().TrimEnd());
    }

    /// <summary>Русская подпись статуса заявки; неизвестный статус печатается как есть.</summary>
    public static string StatusLabel(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "без изменений";
        }

        var normalized = status.Trim();

        return StatusLabels.TryGetValue(normalized, out var label) ? label : normalized;
    }

    private static void AppendItems(StringBuilder body, IReadOnlyList<OrderItemDto> items)
    {
        if (items.Count == 0)
        {
            body.AppendLine("- позиции не указаны");
            return;
        }

        foreach (var item in items)
        {
            body.AppendLine(
                $"- {item.ProductName}: {item.Quantity} шт. × {FormatAmount(item.UnitPrice)} = " +
                $"{FormatAmount(item.UnitPrice * item.Quantity)}");
        }
    }

    private static string FormatAmount(decimal amount) => $"{amount.ToString("0.00", Culture)} ₽";

    private static string FormatDate(DateTimeOffset value) => value.ToString(DateFormat, Culture);

    private static string OrNotSpecified(string? value)
        => string.IsNullOrWhiteSpace(value) ? "не указан" : value.Trim();

    private static string ChannelLabel(OrderChannel channel) => channel switch
    {
        OrderChannel.Phone => "телефон",
        OrderChannel.Email => "e-mail",
        _ => "не указан",
    };

    private static string PriorityLabel(OrderPriority priority)
        => priority == OrderPriority.Urgent ? "срочная" : "обычная";
}
