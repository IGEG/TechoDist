using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;
using Techodist.BuildingBlocks.Core.Results;
using Xunit;

namespace Techodist.Basket.UnitTests.Domain;

public sealed class BasketItemTests
{
    [Fact]
    public void Create_WithValidData_TrimsSnapshot()
    {
        var productId = Guid.NewGuid();

        var item = BasketItem.Create(productId, "  Установка TD-60  ", "  /images/td-60.png  ", Money.Rub(100m), 2);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Установка TD-60", item.ProductName);
        Assert.Equal("/images/td-60.png", item.ImageUrl);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(200m, item.LineTotal);
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        Assert.Throws<ArgumentException>(() => BasketItem.Create(Guid.Empty, "Установка TD-60", null, Money.Rub(100m), 1));
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => BasketItem.Create(Guid.NewGuid(), "   ", null, Money.Rub(100m), 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(BasketItem.MaxQuantity + 1)]
    public void Create_QuantityOutOfRange_Throws(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), quantity));
    }

    [Fact]
    public void ChangeQuantity_WithinRange_Succeeds()
    {
        var item = BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 1);

        var result = item.ChangeQuantity(BasketItem.MaxQuantity);

        Assert.True(result.IsSuccess);
        Assert.Equal(BasketItem.MaxQuantity, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(BasketItem.MaxQuantity + 1)]
    public void ChangeQuantity_OutOfRange_FailsWithValidation(int quantity)
    {
        var item = BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 1);

        var result = item.ChangeQuantity(quantity);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("basket.item.quantity_out_of_range", result.Error.Code);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public void AddQuantity_CapsAtMaxQuantity()
    {
        var item = BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), BasketItem.MaxQuantity - 2);

        var quantity = item.AddQuantity(10);

        Assert.Equal(BasketItem.MaxQuantity, quantity);
        Assert.Equal(BasketItem.MaxQuantity, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddQuantity_NonPositive_Throws(int delta)
    {
        var item = BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.AddQuantity(delta));
    }

    [Fact]
    public void RefreshSnapshot_UpdatesNameImageAndPrice()
    {
        var item = BasketItem.Create(Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 3);

        item.RefreshSnapshot("Установка TD-60 v2", "/images/v2.png", Money.Rub(90m));

        Assert.Equal("Установка TD-60 v2", item.ProductName);
        Assert.Equal("/images/v2.png", item.ImageUrl);
        Assert.Equal(270m, item.LineTotal);
    }

    [Fact]
    public void Rehydrate_KeepsIdAndClampsQuantity()
    {
        var id = Guid.NewGuid();
        var addedAt = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);

        var item = BasketItem.Rehydrate(id, Guid.NewGuid(), "Установка TD-60", null, Money.Rub(100m), 500, addedAt);

        Assert.Equal(id, item.Id);
        Assert.Equal(BasketItem.MaxQuantity, item.Quantity);
        Assert.Equal(addedAt, item.AddedAt);
    }
}
