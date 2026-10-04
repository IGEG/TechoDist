using Techodist.Order.Domain.Enums;

namespace Techodist.Order.Api.Contracts;

/// <summary>Тело запроса оформления заявки (публичная форма витрины).</summary>
public sealed record SubmitOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone = null,
    string? Comment = null,
    OrderContactChannel PreferredChannel = OrderContactChannel.Email,
    OrderPriority Priority = OrderPriority.Standard);
