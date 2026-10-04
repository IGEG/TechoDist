namespace Techodist.Basket.Application.Dtos;

/// <summary>Корзина гостя вместе с итогами (суммарным количеством и стоимостью).</summary>
public sealed record BasketDto(
    Guid BasketId,
    IReadOnlyList<BasketItemDto> Items,
    int TotalQuantity,
    decimal TotalAmount,
    string Currency);
