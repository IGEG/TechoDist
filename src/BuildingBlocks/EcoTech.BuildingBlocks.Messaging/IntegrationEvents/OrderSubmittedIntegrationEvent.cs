namespace EcoTech.BuildingBlocks.Messaging.IntegrationEvents;

/// <summary>Позиция заявки (снимок на момент оформления).</summary>
public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public enum OrderChannel
{
    Unknown = 0,
    Email = 1,
    Phone = 2,
}

public enum OrderPriority
{
    Standard = 0,
    Urgent = 1,
}

/// <summary>
/// Интеграционное событие: заявка оформлена гостем и требует обработки
/// (отправка письма магазину и подтверждения клиенту).
/// </summary>
public sealed record OrderSubmittedIntegrationEvent
{
    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = default!;

    public string CustomerName { get; init; } = default!;

    public string CustomerEmail { get; init; } = default!;

    public string? CustomerPhone { get; init; }

    public string? Comment { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];

    public OrderChannel PreferredChannel { get; init; } = OrderChannel.Email;

    public OrderPriority Priority { get; init; } = OrderPriority.Standard;

    public DateTimeOffset SubmittedAt { get; init; }
}
