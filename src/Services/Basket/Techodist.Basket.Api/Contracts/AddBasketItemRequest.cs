namespace Techodist.Basket.Api.Contracts;

/// <summary>Тело запроса на добавление товара в корзину.</summary>
public sealed record AddBasketItemRequest(Guid ProductId, int Quantity = 1);
