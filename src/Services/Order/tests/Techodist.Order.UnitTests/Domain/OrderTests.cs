using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Domain;

/// <summary>Правила агрегата заявки: контакты, суммы и воронка статусов без онлайн-оплаты.</summary>
public sealed class OrderTests
{
    [Fact]
    public void Create_WithoutItems_IsRejected()
    {
        // Пустая заявка — письмо «менеджер свяжется» без товара: оформлять нечего.
        var exception = Assert.Throws<ArgumentException>(() => OrderTestData.Order(items: []));

        Assert.Equal("items", exception.ParamName);
    }

    [Fact]
    public void Create_WithoutCustomerName_IsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(() => OrderTestData.Order(customerName: "   "));

        Assert.Equal("customerName", exception.ParamName);
    }

    [Fact]
    public void Create_WithoutCustomerEmail_IsRejected()
    {
        // E-mail обязателен всегда: подтверждение заявки уходит письмом (онлайн-оплаты нет).
        var exception = Assert.Throws<ArgumentException>(() => OrderTestData.Order(customerEmail: string.Empty));

        Assert.Equal("customerEmail", exception.ParamName);
    }

    [Fact]
    public void Create_TrimsContactsAndDropsBlankOptionalFields()
    {
        var order = OrderTestData.Order(
            customerName: "  Иван Петров ",
            customerEmail: " ivan@example.com ",
            customerPhone: "   ",
            comment: "  Позвоните после 18:00  ");

        Assert.Equal("Иван Петров", order.CustomerName);
        Assert.Equal("ivan@example.com", order.CustomerEmail);
        Assert.Null(order.CustomerPhone);
        Assert.Equal("Позвоните после 18:00", order.Comment);
    }

    [Fact]
    public void Create_WithoutBasket_DoesNotStoreEmptyGuid()
    {
        // Guid.Empty — это «корзины не было», а не реальный идентификатор.
        Assert.Null(OrderTestData.Order(basketId: Guid.Empty).BasketId);
    }

    [Fact]
    public void Create_KeepsBasketLinkAndStartsPending()
    {
        var basketId = Guid.NewGuid();

        var order = OrderTestData.Order(basketId: basketId, priority: OrderPriority.Urgent);

        Assert.Equal(basketId, order.BasketId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(OrderPriority.Urgent, order.Priority);
        Assert.Equal(order.CreatedAt, order.UpdatedAt);
    }

    [Fact]
    public void TotalsAndCurrency_AreDerivedFromItems()
    {
        var order = OrderTestData.Order(items:
        [
            OrderTestData.Item(price: 485_000m, quantity: 2),
            OrderTestData.Item(price: 1_000.5m, quantity: 3),
        ]);

        Assert.Equal(5, order.TotalQuantity);
        Assert.Equal(973_001.5m, order.TotalAmount);
        Assert.Equal("RUB", order.Currency);
        Assert.Equal(2, order.Items.Count);
    }
}
