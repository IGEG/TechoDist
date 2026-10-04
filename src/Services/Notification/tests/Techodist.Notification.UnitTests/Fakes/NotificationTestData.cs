using Techodist.BuildingBlocks.Messaging.IntegrationEvents;

namespace Techodist.Notification.UnitTests.Fakes;

/// <summary>Тестовые события заявок: один «полный» сценарий и производные от него варианты.</summary>
internal static class NotificationTestData
{
    public const string OrderNumber = "TD-20261004-00001";

    public const string CustomerName = "Иван Петров";

    public const string CustomerEmail = "ivan.petrov@example.com";

    public const string StoreEmail = "orders@techodist.local";

    public static readonly DateTimeOffset SubmittedAt = new(2026, 10, 4, 9, 30, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset ChangedAt = new(2026, 10, 4, 12, 5, 0, TimeSpan.Zero);

    public static readonly Guid OrderId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    /// <summary>Заявка из двух позиций на 48 500 ₽ со срочностью и комментарием клиента.</summary>
    public static OrderSubmittedIntegrationEvent SubmittedEvent(
        IReadOnlyList<OrderItemDto>? items = null,
        string? phone = "+7 999 123-45-67",
        string? comment = "Позвонить после 18:00",
        OrderChannel channel = OrderChannel.Phone,
        OrderPriority priority = OrderPriority.Urgent,
        decimal totalAmount = 48_500.00m) => new()
        {
            OrderId = OrderId,
            OrderNumber = OrderNumber,
            CustomerName = CustomerName,
            CustomerEmail = CustomerEmail,
            CustomerPhone = phone,
            Comment = comment,
            TotalAmount = totalAmount,
            Items = items ?? DefaultItems(),
            PreferredChannel = channel,
            Priority = priority,
            SubmittedAt = SubmittedAt,
        };

    public static OrderStatusChangedIntegrationEvent StatusChangedEvent(
        string oldStatus = "Pending",
        string newStatus = "Confirmed",
        string? managerComment = "Согласовали сроки поставки.") => new()
        {
            OrderId = OrderId,
            OrderNumber = OrderNumber,
            CustomerEmail = CustomerEmail,
            CustomerName = CustomerName,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ManagerComment = managerComment,
            ChangedAt = ChangedAt,
        };

    private static IReadOnlyList<OrderItemDto> DefaultItems() =>
    [
        new OrderItemDto(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), "Установка регенерации Р-100", 1, 45_000.00m),
        new OrderItemDto(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), "Картридж угольный", 2, 1_750.00m),
    ];
}
