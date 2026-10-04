using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;
using Techodist.Basket.Infrastructure.Caching;
using Xunit;

namespace Techodist.Basket.UnitTests.Infrastructure;

/// <summary>
/// Сериализация в Redis идёт через отдельную модель состояния, поэтому проверяем
/// цикл «домен → состояние → домен»: любая потеря поля сломала бы корзину после перезагрузки.
/// </summary>
public sealed class BasketStateMapperTests
{
    [Fact]
    public void FromDomain_ThenToDomain_RoundTripsBasket()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.Create(basketId);

        basket.AddItem(productId, "Установка TD-60", "/images/td-60.png", Money.Rub(485_000m), 2);
        basket.AddItem(Guid.NewGuid(), "Растворитель", null, Money.Rub(1_500m), 1);

        var state = BasketStateMapper.FromDomain(basket);
        var restored = BasketStateMapper.ToDomain(state);

        Assert.Equal(basket.Id, restored.Id);
        Assert.Equal(basket.CreatedAt, restored.CreatedAt);
        Assert.Equal(basket.UpdatedAt, restored.UpdatedAt);
        Assert.Equal(basket.TotalQuantity, restored.TotalQuantity);
        Assert.Equal(basket.TotalAmount, restored.TotalAmount);
        Assert.Equal(basket.Currency, restored.Currency);

        var originalItem = basket.FindItem(productId)!;
        var restoredItem = restored.FindItem(productId)!;

        Assert.Equal(originalItem.Id, restoredItem.Id);
        Assert.Equal(originalItem.ProductName, restoredItem.ProductName);
        Assert.Equal(originalItem.ImageUrl, restoredItem.ImageUrl);
        Assert.Equal(originalItem.UnitPrice, restoredItem.UnitPrice);
        Assert.Equal(originalItem.Quantity, restoredItem.Quantity);
        Assert.Equal(originalItem.AddedAt, restoredItem.AddedAt);
    }

    [Fact]
    public void ToDomain_WithCorruptedQuantity_ClampsToLimit()
    {
        var state = new BasketState(
            Guid.NewGuid(),
            [
                new BasketItemState(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Установка TD-60",
                    null,
                    100m,
                    "RUB",
                    5_000,
                    DateTimeOffset.UtcNow),
            ],
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        var basket = BasketStateMapper.ToDomain(state);

        Assert.Equal(BasketItem.MaxQuantity, Assert.Single(basket.Items).Quantity);
    }
}
