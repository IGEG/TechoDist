using Techodist.Order.Domain.Enums;

namespace Techodist.Order.Api.Contracts;

/// <summary>Тело запроса смены статуса заявки (админ-панель).</summary>
public sealed record ChangeOrderStatusRequest(
    OrderStatus Status,
    string? ManagerComment = null);
