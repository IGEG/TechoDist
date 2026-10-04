namespace EcoTech.BuildingBlocks.Messaging.IntegrationEvents;

/// <summary>
/// Интеграционное событие: статус заявки изменён менеджером в админке.
/// </summary>
public sealed record OrderStatusChangedIntegrationEvent
{
    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = default!;

    public string CustomerEmail { get; init; } = default!;

    public string CustomerName { get; init; } = default!;

    public string OldStatus { get; init; } = default!;

    public string NewStatus { get; init; } = default!;

    public string? ManagerComment { get; init; }

    public DateTimeOffset ChangedAt { get; init; }
}
