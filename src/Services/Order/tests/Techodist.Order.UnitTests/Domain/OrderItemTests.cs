using Techodist.Order.Domain.Entities;
using Techodist.Order.Domain.ValueObjects;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Domain;

/// <summary>Позиция заявки — снимок товара: его нельзя создать «полупустым».</summary>
public sealed class OrderItemTests
{
    [Fact]
    public void Create_WithoutProduct_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => OrderItem.Create(
            Guid.Empty,
            OrderTestData.DefaultProductName,
            OrderTestData.DefaultImageUrl,
            Money.Rub(OrderTestData.DefaultPrice),
            1));

        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void Create_WithoutProductName_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => OrderItem.Create(
            Guid.NewGuid(),
            "  ",
            OrderTestData.DefaultImageUrl,
            Money.Rub(OrderTestData.DefaultPrice),
            1));

        Assert.Equal("productName", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(OrderItem.MaxQuantity + 1)]
    public void Create_QuantityOutOfRange_IsRejected(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderItem.Create(
            Guid.NewGuid(),
            OrderTestData.DefaultProductName,
            OrderTestData.DefaultImageUrl,
            Money.Rub(OrderTestData.DefaultPrice),
            quantity));
    }

    [Fact]
    public void Create_TrimsSnapshotFields()
    {
        var item = OrderItem.Create(
            Guid.NewGuid(),
            "  Установка TD120 ",
            " /images/products/td-120.png ",
            Money.Rub(OrderTestData.DefaultPrice),
            OrderItem.MaxQuantity);

        Assert.Equal("Установка TD120", item.ProductName);
        Assert.Equal("/images/products/td-120.png", item.ImageUrl);
        Assert.Equal(OrderItem.MaxQuantity, item.Quantity);
    }

    [Fact]
    public void Create_WithoutImage_IsAllowed()
    {
        // Товар мог быть снят с продажи вместе с картинкой — снимок остаётся валидным.
        var item = OrderTestData.Item(imageUrl: null);

        Assert.Null(item.ImageUrl);
    }

    [Fact]
    public void LineTotal_IsPriceTimesQuantity()
    {
        var item = OrderTestData.Item(price: 485_000m, quantity: 3);

        Assert.Equal(1_455_000m, item.LineTotal);
        Assert.Equal(Money.Rub(485_000m), item.UnitPrice);
    }
}
