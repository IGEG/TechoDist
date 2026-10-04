using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;

namespace Techodist.Basket.UnitTests.Fakes;

/// <summary>Общие данные для тестов корзины: цена установки и позиция «по умолчанию».</summary>
internal static class BasketTestData
{
    public const decimal DefaultPrice = 485_000m;
    public const string DefaultProductName = "Установка TD-60";
    public const string DefaultImageUrl = "/images/products/td-60.png";

    public static CatalogProduct Product(
        Guid productId,
        string name = DefaultProductName,
        decimal price = DefaultPrice,
        bool isAvailable = true,
        string? imageUrl = DefaultImageUrl)
        => new(productId, name, imageUrl, price, "RUB", isAvailable);

    public static ShoppingBasket Basket(
        Guid basketId,
        Guid productId,
        decimal price = DefaultPrice,
        int quantity = 1)
    {
        var basket = ShoppingBasket.Create(basketId);
        basket.AddItem(productId, DefaultProductName, DefaultImageUrl, Money.Rub(price), quantity);

        return basket;
    }
}
