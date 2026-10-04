using Techodist.Order.Domain.Enums;
using IntegrationEvents = Techodist.BuildingBlocks.Messaging.IntegrationEvents;

namespace Techodist.Order.Application.Common;

/// <summary>
/// Проекция заявки в интеграционные события (ADR 0003). Событие несёт готовый снимок заявки,
/// поэтому Notification не обращается к Order — обмен только через брокер.
/// </summary>
internal static class OrderEventsMapper
{
    public static IntegrationEvents.OrderSubmittedIntegrationEvent ToSubmittedEvent(this OrderAggregate order) => new()
    {
        OrderId = order.Id,
        OrderNumber = order.Number.Value,
        CustomerName = order.CustomerName,
        CustomerEmail = order.CustomerEmail,
        CustomerPhone = order.CustomerPhone,
        Comment = order.Comment,
        TotalAmount = order.TotalAmount,
        Items = order.Items
            .Select(item => new IntegrationEvents.OrderItemDto(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice.Amount))
            .ToList(),
        PreferredChannel = order.PreferredChannel == OrderContactChannel.Phone
            ? IntegrationEvents.OrderChannel.Phone
            : IntegrationEvents.OrderChannel.Email,
        Priority = order.Priority == OrderPriority.Urgent
            ? IntegrationEvents.OrderPriority.Urgent
            : IntegrationEvents.OrderPriority.Standard,
        SubmittedAt = order.CreatedAt,
    };

    public static IntegrationEvents.OrderStatusChangedIntegrationEvent ToStatusChangedEvent(
        this OrderAggregate order,
        OrderStatus oldStatus,
        string? managerComment) => new()
    {
        OrderId = order.Id,
        OrderNumber = order.Number.Value,
        CustomerEmail = order.CustomerEmail,
        CustomerName = order.CustomerName,
        OldStatus = oldStatus.ToString(),
        NewStatus = order.Status.ToString(),
        ManagerComment = managerComment,
        ChangedAt = order.UpdatedAt,
    };
}
