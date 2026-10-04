namespace Techodist.Basket.Application.Dtos;

/// <summary>Позиция корзины для витрины.</summary>
public sealed record BasketItemDto(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);
