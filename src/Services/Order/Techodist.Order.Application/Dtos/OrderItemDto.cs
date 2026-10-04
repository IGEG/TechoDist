namespace Techodist.Order.Application.Dtos;

/// <summary>Позиция заявки для админки и письма-подтверждения.</summary>
public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);
