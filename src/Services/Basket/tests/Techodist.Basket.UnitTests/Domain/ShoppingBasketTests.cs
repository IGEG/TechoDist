using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;
using Techodist.BuildingBlocks.Core.Results;
using Xunit;

namespace Techodist.Basket.UnitTests.Domain;

public sealed class ShoppingBasketTests
{
    [Fact]
    public void Create_WithoutId_GeneratesIdAndTimestamps()
    {
        var basket = ShoppingBasket.Create();

        Assert.NotEqual(Guid.Empty, basket.Id);
        Assert.Equal(basket.CreatedAt, basket.UpdatedAt);
        Assert.True(basket.IsEmpty);
        Assert.Equal(0, basket.TotalQuantity);
        Assert.Equal(0m, basket.TotalAmount);
    }

    [Fact]
    public void Create_WithCookieId_UsesIt()
    {
        var basketId = Guid.NewGuid();

        var basket = ShoppingBasket.Create(basketId);

        Assert.Equal(basketId, basket.Id);
    }

    [Fact]
    public void AddItem_NewProduct_AddsPositionAndTotals()
    {
        var basket = ShoppingBasket.Create();
        var productId = Guid.NewGuid();

        var result = basket.AddItem(productId, "Установка TD-60", "/images/td-60.png", Money.Rub(485_000m), 2);

        Assert.True(result.IsSuccess);

        var item = Assert.Single(basket.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(970_000m, basket.TotalAmount);
        Assert.Equal(2, basket.TotalQuantity);
        Assert.Equal("RUB", basket.Currency);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncreasesQuantityAndRefreshesPrice()
    {
        var basket = ShoppingBasket.Create();
        var productId = Guid.NewGuid();

        basket.AddItem(productId, "Установка TD-60", "/images/td-60.png", Money.Rub(485_000m), 1);
        basket.AddItem(productId, "Установка TD-60 (акция)", "/images/td-60-new.png", Money.Rub(450_000m), 2);

        var item = Assert.Single(basket.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(450_000m, item.UnitPrice.Amount);
        Assert.Equal("Установка TD-60 (акция)", item.ProductName);
        Assert.Equal(1_350_000m, basket.TotalAmount);
    }

    [Fact]
    public void AddItem_BeyondMaxQuantity_CapsAtLimit()
    {
        var basket = ShoppingBasket.Create();
        var productId = Guid.NewGuid();

        basket.AddItem(productId, "Установка TD-60", null, Money.Rub(100m), BasketItem.MaxQuantity);
        basket.AddItem(productId, "Установка TD-60", null, Money.Rub(100m), 5);

        var item = Assert.Single(basket.Items);
        Assert.Equal(BasketItem.MaxQuantity, item.Quantity);
    }

    [Fact]
    public void AddItem_BeyondDistinctItemsLimit_FailsWithConflict()
    {
        var basket = ShoppingBasket.Create();

        for (var index = 0; index < ShoppingBasket.MaxDistinctItems; index++)
        {
            basket.AddItem(Guid.NewGuid(), $"Товар {index}", null, Money.Rub(1_000m));
        }

        var result = basket.AddItem(Guid.NewGuid(), "Лишний товар", null, Money.Rub(1_000m));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("basket.items.limit_reached", result.Error.Code);
        Assert.Equal(ShoppingBasket.MaxDistinctItems, basket.Items.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(BasketItem.MaxQuantity + 1)]
    public void AddItem_InvalidQuantity_FailsWithValidation(int quantity)
    {
        var basket = ShoppingBasket.Create();

        var result = basket.AddItem(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), quantity);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(basket.Items);
    }

    [Fact]
    public void AddItem_EmptyProductId_Fails()
    {
        var basket = ShoppingBasket.Create();

        var result = basket.AddItem(Guid.Empty, "Установка TD-60", null, Money.Rub(100m));

        Assert.True(result.IsFailure);
        Assert.Equal("basket.item.product_required", result.Error.Code);
    }

    [Fact]
    public void ChangeItemQuantity_UpdatesQuantity()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.Create();
        basket.AddItem(productId, "Установка TD-60", null, Money.Rub(100m), 1);

        var result = basket.ChangeItemQuantity(productId, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, basket.FindItem(productId)!.Quantity);
        Assert.Equal(400m, basket.TotalAmount);
    }

    [Fact]
    public void ChangeItemQuantity_UnknownProduct_FailsWithNotFound()
    {
        var basket = ShoppingBasket.Create();

        var result = basket.ChangeItemQuantity(Guid.NewGuid(), 2);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("basket.item.not_found", result.Error.Code);
    }

    [Fact]
    public void ChangeItemQuantity_OutOfRange_FailsWithValidation()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.Create();
        basket.AddItem(productId, "Установка TD-60", null, Money.Rub(100m), 3);

        var result = basket.ChangeItemQuantity(productId, 0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(3, basket.FindItem(productId)!.Quantity);
    }

    [Fact]
    public void RemoveItem_DeletesPosition_AndKeepsOtherItems()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var basket = ShoppingBasket.Create();
        basket.AddItem(first, "Установка TD-60", null, Money.Rub(100m), 2);
        basket.AddItem(second, "Растворитель", null, Money.Rub(50m), 1);

        var result = basket.RemoveItem(first);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(basket.Items);
        Assert.Equal(second, item.ProductId);
        Assert.Equal(50m, basket.TotalAmount);
    }

    [Fact]
    public void RemoveItem_UnknownProduct_FailsWithNotFound()
    {
        var basket = ShoppingBasket.Create();

        var result = basket.RemoveItem(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("basket.item.not_found", result.Error.Code);
    }

    [Fact]
    public void Clear_EmptiesBasket_AndIsIdempotent()
    {
        var basket = ShoppingBasket.Create();
        basket.AddItem(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 3);

        basket.Clear();
        basket.Clear();

        Assert.True(basket.IsEmpty);
        Assert.Equal(0, basket.TotalQuantity);
        Assert.Equal(0m, basket.TotalAmount);
    }

    [Fact]
    public void FindItem_ReturnsNull_WhenProductIsAbsent()
    {
        var basket = ShoppingBasket.Create();

        Assert.Null(basket.FindItem(Guid.NewGuid()));
    }

    [Fact]
    public void AddItem_UpdatesTimestamp()
    {
        var basket = ShoppingBasket.Create();
        var createdAt = basket.CreatedAt;

        basket.AddItem(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m));

        Assert.True(basket.UpdatedAt >= createdAt);
    }

    [Fact]
    public void Rehydrate_RestoresItemsAndTimestamps()
    {
        var basketId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(1);
        var item = BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 2);

        var basket = ShoppingBasket.Rehydrate(basketId, [item], createdAt, updatedAt);

        Assert.Equal(basketId, basket.Id);
        Assert.Equal(createdAt, basket.CreatedAt);
        Assert.Equal(updatedAt, basket.UpdatedAt);
        Assert.Equal(200m, basket.TotalAmount);
    }
}
