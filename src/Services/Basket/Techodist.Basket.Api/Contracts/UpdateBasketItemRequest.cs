namespace Techodist.Basket.Api.Contracts;

/// <summary>Тело запроса на изменение количества товара в корзине.</summary>
public sealed record UpdateBasketItemRequest(int Quantity);
